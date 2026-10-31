using BepInEx;
using EntityStates.MoonElevator;
using NewMoon.Items;
using RoR2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using NewMoon.Components;
using System;
using EntityStates.Missions.Moon;
using static NewMoon.Modules.Language.Styling;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace NewMoon
{
	public partial class NewMoonPlugin
	{
		public static int requiredPillarCt = 2;
		public static int maxPillarCt = 4;
		//public static int pillarsToSpawn = 6;

		public static float pillarDropOffset = 2.5f;
		public static float pillarDropForce = 20f;
		public static int baseRewardCount = 1;
		public static bool scaleRewardsByPlayerCount = true;
		private static bool isBossRush = false;

		void MakePillarsFun()
		{
			//On.RoR2.MoonBatteryMissionController.OnBatteryCharged += PillarsDropItems;
			On.RoR2.MoonBatteryMissionController.Awake += ReduceRequiredPillars;
			On.RoR2.MoonBatteryMissionController.OnEnable += MoonBossRush;
			On.EntityStates.MoonElevator.Inactive.OnEnter += PillarSkip;
			On.RoR2.MoonBatteryMissionController.OnBatteryCharged += ChangePillarRequirement;
			On.RoR2.UI.MoonBatteryMissionObjectiveTracker.GenerateString += ChangePillarObjective;
			On.EntityStates.Missions.GeodeSecretMission.GeodeSecretMissionRewardState.DropRewards += ReplaceGeodeSecretMissionRewards;

			NewMoonPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryBlood_prefab, 
				(ctx) => AddPillarItemDrop(ctx, PillarItemDropper.PillarType.Blood));
			NewMoonPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryDesign_prefab, 
				(ctx) => AddPillarItemDrop(ctx, PillarItemDropper.PillarType.Blood));
			NewMoonPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryMass_prefab, 
				(ctx) => AddPillarItemDrop(ctx, PillarItemDropper.PillarType.Blood));
			NewMoonPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatterySoul_prefab, 
				(ctx) => AddPillarItemDrop(ctx, PillarItemDropper.PillarType.Blood));
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryBlood_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Blood);
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryDesign_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Design);
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryMass_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Mass);
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatterySoul_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Soul);
			void AddPillarItemDrop(GameObject pillarPrefab, PillarItemDropper.PillarType pillarType)
			{
				PillarItemDropper dropper = pillarPrefab.AddComponent<PillarItemDropper>();
				dropper.pillarType = pillarType;
			}

			IL.RoR2.Karma.CollectLunarItems.onInventoryChangedGlobal += HideRelicsFromKarmaTrigger;
		}

		private void HideRelicsFromKarmaTrigger(ILContext il)
        {
			ILCursor c = new ILCursor(il);

			bool b = c.TryGotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetTotalItemCountOfTier)))
				&& c.TryGotoNext(MoveType.Before,
				x => x.MatchStloc(out _)
				);
			if(b == false) 
			{
				NewMoonPlugin.DebugBreakpoint(nameof(HideRelicsFromKarmaTrigger));
				return;
			}
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<int, Inventory, int>>((countIn, inv) =>
			{
				countIn -= inv.GetItemCountEffective(BloodAnomaly.instance.ItemsDef);
				countIn -= inv.GetItemCountEffective(SoulAnomaly.instance.ItemsDef);
				countIn -= inv.GetItemCountEffective(MassAnomaly.instance.ItemsDef);
				countIn -= inv.GetItemCountEffective(DesignAnomaly.instance.ItemsDef);
				return countIn;
			});
        }

        private string ChangePillarObjective(On.RoR2.UI.MoonBatteryMissionObjectiveTracker.orig_GenerateString orig, RoR2.UI.MoonBatteryMissionObjectiveTracker self)
		{
			MoonBatteryMissionController moonBatteryMissionController = (MoonBatteryMissionController)self.sourceDescriptor.source;
			self.numChargedBatteries = moonBatteryMissionController.numChargedBatteries;
			int requiredCt = isBossRush ? 0 : requiredPillarCt;
			string required = requiredCt.ToString();
			if (requiredCt != maxPillarCt)
				required += "..?";
			return string.Format(Language.GetString(moonBatteryMissionController.objectiveToken), self.numChargedBatteries, required);
		}

        #region meridian
        private void ReplaceGeodeSecretMissionRewards(On.EntityStates.Missions.GeodeSecretMission.GeodeSecretMissionRewardState.orig_DropRewards orig, EntityStates.Missions.GeodeSecretMission.GeodeSecretMissionRewardState self)
		{
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			if (participatingPlayerCount > 0 && self.gameObject && self.geodeSecretMissionController.rewardDropTable)
			{
				int num = self.geodeSecretMissionController.numberOfRewardsSpawned;
				if (self.geodeSecretMissionController.increaseRewardPerPlayer)
				{
					num *= participatingPlayerCount;
				}
				float angle = 360f / (float)num;
				Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
				Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
				Vector3 position = self.geodeSecretMissionController.rewardSpawnLocation.transform.position;
				int i = 0;
				while (i < num)
				{
					PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
					{
						pickupIndex = PickupCatalog.FindPickupIndex(self.geodeSecretMissionController.rewardDisplayTier),
						pickerOptions = new PickupPickerController.Option[4] { 
							TPkp(MassAnomaly.instance.ItemsDef), 
							TPkp(DesignAnomaly.instance.ItemsDef), 
							TPkp(BloodAnomaly.instance.ItemsDef),
							TPkp(SoulAnomaly.instance.ItemsDef) 
						},
						rotation = Quaternion.identity,
						prefabOverride = self.geodeSecretMissionController.rewardPrefab,
						maxOptionCount = 2
					}, position, vector);
					i++;
					vector = rotation * vector;
				}
			}
			PickupPickerController.Option TPkp(ItemDef itemDef)
            {
				return new PickupPickerController.Option
				{
					available = true,
					pickup = new UniquePickup { pickupIndex = PickupCatalog.itemIndexToPickupIndex[(int)itemDef.itemIndex] }
				};
			}
		}
        #endregion

		private void ServerUpdatePillarsAndElevators(MoonBatteryMissionController self, bool wasCharged, bool hasMetRequirement, bool hasMetMax)
        {
			if (hasMetRequirement)
			{
				if (wasCharged == false)
				{
					Chat.ServerAttemptBroadcastChat($"{MithrixStyle("A path forward is cleared...")}");
				}
				for (int k = 0; k < self.elevatorStateMachines.Length; k++)
				{
					self.elevatorStateMachines[k].SetNextState(new InactiveToReady());
				}
			}
			else
				return;
            if (hasMetMax)
			{
				for (int i = 0; i < self.batteryHoldoutZones.Length; i++)
				{
					HoldoutZoneController zone = self.batteryHoldoutZones[i];
					if (zone.enabled)
					{
						zone.FullyChargeHoldoutZone();
						zone.onCharged.RemoveListener(new UnityAction<HoldoutZoneController>(self.OnBatteryCharged));
					}
					if (zone.TryGetComponent(out PillarItemDropper dropper))
						dropper.disabled = true;
				}
				self.batteryHoldoutZones = new HoldoutZoneController[0];
				for (int j = 0; j < self.batteryStateMachines.Length; j++)
				{
					if (!(self.batteryStateMachines[j].state is MoonBatteryComplete))
					{
						self.batteryStateMachines[j].SetNextState(new MoonBatteryDisabled());
					}
				}
			}
        }

        private void ChangePillarRequirement(On.RoR2.MoonBatteryMissionController.orig_OnBatteryCharged orig, MoonBatteryMissionController self, HoldoutZoneController holdoutZone)
		{
			int requiredBatteries = requiredPillarCt;
			int maxBatteries = maxPillarCt;
			if (isBossRush)
			{
				requiredBatteries = 0;
				maxBatteries = maxBatteries - requiredPillarCt;
			}
			if (!NetworkServer.active)
				return;
			bool wasCharged = self._numChargedBatteries >= requiredPillarCt;

			if (holdoutZone != null)
            {
				self.Network_numChargedBatteries = self._numChargedBatteries + 1;
			}


			ServerUpdatePillarsAndElevators(self, wasCharged, self._numChargedBatteries >= requiredBatteries, self._numChargedBatteries >= maxBatteries);
		}

		private void PillarSkip(On.EntityStates.MoonElevator.Inactive.orig_OnEnter orig, Inactive self)
		{
			orig(self);
			if (isBossRush || (MoonBatteryMissionController.instance && MoonBatteryMissionController.instance.numChargedBatteries >= MoonBatteryMissionController.instance.numRequiredBatteries))
			{
				self.outer.SetNextState(new InactiveToReady());
			}
		}

		private void MoonBossRush(On.RoR2.MoonBatteryMissionController.orig_OnEnable orig, MoonBatteryMissionController self)
		{
			orig(self);
			if (NewMoonPlugin.TeamHasRelics(TeamIndex.Player) && NetworkServer.active)
			{
				isBossRush = true;
				Chat.ServerAttemptBroadcastChat($"{MithrixStyle("The King of Nothing welcomes you...")}");
				ServerUpdatePillarsAndElevators(self, wasCharged: false, hasMetRequirement: true, hasMetMax: false);
			}
		}

		private void ReduceRequiredPillars(On.RoR2.MoonBatteryMissionController.orig_Awake orig, MoonBatteryMissionController self)
		{
			isBossRush = false;

			orig(self);

			foreach(GameObject pillarObject in self.moonBatteries)
			{
				PillarItemDropper dropper = pillarObject.GetComponent<PillarItemDropper>();

				//backup pillar type determination
                if (!dropper)
				{
					dropper = pillarObject.AddComponent<PillarItemDropper>();

					PillarItemDropper.PillarType pickup = PillarItemDropper.PillarType.None;
					string fullName = pillarObject.name;
					string pillarType = fullName.Substring(11, 4);
					switch (pillarType)
					{
						default:
							break;
						case "Mass":
							pickup = PillarItemDropper.PillarType.Mass;
							break;
						case "Desi":
							pickup = PillarItemDropper.PillarType.Design;
							break;
						case "Bloo":
							pickup = PillarItemDropper.PillarType.Blood;
							break;
						case "Soul":
							pickup = PillarItemDropper.PillarType.Soul;
							break;
					}

					dropper.pillarType = pickup;
				}

				dropper.shouldDropItem = true;
            }
		}

		private void PillarsDropItems(On.RoR2.MoonBatteryMissionController.orig_OnBatteryCharged orig, RoR2.MoonBatteryMissionController self, RoR2.HoldoutZoneController holdoutZone)
		{
			//Debug.Log("A");
			int participatingPlayerCount = Run.instance.participatingPlayerCount;

			if (participatingPlayerCount != 0)
            {
				//Debug.Log("B");
				DropPillarItemFromHoldout(holdoutZone, participatingPlayerCount);
            }

            self.Network_numChargedBatteries = self._numChargedBatteries + 1;
			if (self._numChargedBatteries >= self._numRequiredBatteries && NetworkServer.active)
			{
				for (int i = 0; i < self.batteryHoldoutZones.Length; i++)
				{
					if (self.batteryHoldoutZones[i].enabled)
					{
						self.batteryHoldoutZones[i].FullyChargeHoldoutZone();
						self.batteryHoldoutZones[i].onCharged.RemoveListener(new UnityAction<HoldoutZoneController>(self.OnBatteryCharged));
					}
				}
				/*self.batteryHoldoutZones = new HoldoutZoneController[0];
				for (int j = 0; j < self.batteryStateMachines.Length; j++)
				{
					if (!(self.batteryStateMachines[j].state is MoonBatteryComplete))
					{
						self.batteryStateMachines[j].SetNextState(new MoonBatteryDisabled());
					}
				}*/
				for (int k = 0; k < self.elevatorStateMachines.Length; k++)
				{
					self.elevatorStateMachines[k].SetNextState(new InactiveToReady());
				}
			}
		}
		public static void DropPillarItemFromHoldout(HoldoutZoneController holdoutZone)
		{
			Log.Warning(holdoutZone.gameObject.name + "3");
			DropPillarItemFromHoldout(holdoutZone, Run.instance ? Run.instance.participatingPlayerCount : 0);
        }

        public static void DropPillarItemFromHoldout(HoldoutZoneController holdoutZone, int participatingPlayerCount)
		{
			Log.Warning(holdoutZone.gameObject.name + "4");
			if (participatingPlayerCount == 0)
				return;
			Vector3 dropPosition = holdoutZone.gameObject.transform.position + Vector3.up * pillarDropOffset;
			PickupIndex pickupIndex = GetPickupIndexFromPillarType(holdoutZone.gameObject);

            if (pickupIndex != PickupIndex.none)
			{
				Log.Warning(holdoutZone.gameObject.name + "5");
				int num = baseRewardCount;
                if (scaleRewardsByPlayerCount)
                {
                    num *= participatingPlayerCount;
                }

                float angle = 360f / (float)num;
                Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up)
                    * (Vector3.up * pillarDropForce + Vector3.forward * 5f);
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                int i = 0;
                while (i < num)
				{
					Log.Warning(holdoutZone.gameObject.name + "6");
					PickupDropletController.CreatePickupDroplet(new UniquePickup(pickupIndex), dropPosition, vector, false);
                    i++;
                    vector = rotation * vector;
                }
            }
        }

        public static PickupIndex GetPickupIndexFromPillarType(GameObject pillar)
		{
			ItemBase pickup = null;
			string fullName = pillar.name;
			string pillarType = fullName.Substring(11, 4);
			switch (pillarType)
			{
				default:
					break;
				case "Mass":
					pickup = (MassAnomaly.instance);
					break;
				case "Desi":
					pickup = (DesignAnomaly.instance);
					break;
				case "Bloo":
					pickup = (BloodAnomaly.instance);
					break;
				case "Soul":
					pickup = (SoulAnomaly.instance);
					break;
			}
			if(pickup != null)
			{
				ItemIndex itemsIndex = pickup.ItemsDef.itemIndex;
				return PickupCatalog.FindPickupIndex(itemsIndex);
			}
			Debug.Log("No pickup index found!");
			return PickupIndex.none;
		}
	}
}
