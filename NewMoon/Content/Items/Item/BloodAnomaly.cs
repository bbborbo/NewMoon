using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using NewMoon.Modules;
using NewMoon;
using System.Linq;
using static NewMoon.Modules.Language.Styling;

namespace NewMoon.Items
{
    class BloodAnomaly : ItemBase<BloodAnomaly>
	{
		public override bool forcePrerequisites => true;
		public override bool GetPrerequisites()
		{
			return NewMoonPlugin.DoPillarItemDrop;
		}
		#region config
		public override string ConfigName => "Items : Commencement : Relic of Blood";

        [AutoConfig("Heal Fraction On Kill Base", 0.08f)]
        public static float healFractionOnKillBase = 0.08f;
        [AutoConfig("Heal Fraction On Kill Stack", 0.08f)]
        public static float healFractionOnKillStack = 0.08f;

        [AutoConfig("On-Kill Force Triggers Base", 4)]
        public static int onKillForceTriggersBase = 4;
        [AutoConfig("On-Kill Force Triggers Stack", 2)]
        public static int onKillForceTriggersStack = 2;
		#endregion
		public static BuffDef hiddenForceTriggerCount;
        public override string ItemName => "Relic of Blood";

        public override string ItemLangTokenName => "BLOODANOMALY";

        public override string ItemPickupDesc => "Heal on kill. Damaging powerful enemies force-triggers on-kill effects.";

        public override string ItemFullDescription => 
			$"On killing an enemy, immediately heal for " +
			$"{HealingColor(Tools.ConvertDecimal(healFractionOnKillBase))} {StackText($"+{Tools.ConvertDecimal(healFractionOnKillStack)}")} " +
			$"of {HealingColor("maximum health")}. Dealing damage to {UtilityColor("Champions")} will " +
			$"force-trigger {DamageColor("On-Kill")} effects up to " +
			$"{DamageColor($"{onKillForceTriggersBase}")} {StackText($"+{onKillForceTriggersStack}")} times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

		public override GameObject ItemModel => LoadDropPrefab("PickupBloodAnomaly");

		public override Sprite ItemIcon => LoadItemIcon("texIconBloodAnomaly");
		public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist , ItemTag.WorldUnique, ItemTag.CannotSteal, ItemTag.AIBlacklist, ItemTag.OnKillEffect };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
		}

		public override void Init()
		{
			hiddenForceTriggerCount = Content.CreateAndAddBuff(
				"bdHiddenRelicForceTriggerCount",
				null, Color.black, true, false);
			hiddenForceTriggerCount.isHidden = true;
			base.Init();
		}
        public override void PostInit()
        {
            base.PostInit();

			CraftableDef craftable = ScriptableObject.CreateInstance<CraftableDef>();
			craftable.pickup = this.ItemsDef;

			RecipeIngredient corpsebloom = new RecipeIngredient();
			corpsebloom.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RepeatHeal.RepeatHeal_asset).WaitForCompletion();
			corpsebloom.type = IngredientTypeIndex.AssetReference;
			RecipeIngredient gesture = new RecipeIngredient();
			gesture.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_AutoCastEquipment.AutoCastEquipment_asset).WaitForCompletion();
			gesture.type = IngredientTypeIndex.AssetReference;
			RecipeIngredient focon = new RecipeIngredient();
			focon.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FocusConvergence.FocusConvergence_asset).WaitForCompletion();
			focon.type = IngredientTypeIndex.AssetReference;

			RecipeIngredient anyQuest = new RecipeIngredient();
			anyQuest.requiredTags = new ItemTag[] { ItemTag.ObjectiveRelated };
			anyQuest.forbiddenTags = new ItemTag[] { ItemTag.Count };
			anyQuest.type = IngredientTypeIndex.AnyItem;
			RecipeIngredient anyOnKill = new RecipeIngredient();
			anyOnKill.requiredTags = new ItemTag[] { ItemTag.OnKillEffect };
			anyOnKill.forbiddenTags = new ItemTag[] { ItemTag.Count };
			anyOnKill.type = IngredientTypeIndex.AnyItem;

			craftable.AddAllRecipePermutations(new RecipeIngredient[] { corpsebloom, gesture, focon }, new RecipeIngredient[] { anyQuest, anyOnKill });
			Content.AddCraftableDef(craftable);
		}

        public override void Hooks()
        {
			GlobalEventManager.onServerDamageDealt += BloodRelicOnDamageDealt;
            GlobalEventManager.onCharacterDeathGlobal += BloodRelicOnKill;
        }

        private void BloodRelicOnDamageDealt(DamageReport damageReport)
        {
			DamageInfo damageInfo = damageReport.damageInfo;
			if (!NetworkServer.active)
				return;

			GameObject attacker = damageInfo.attacker;
			if (!attacker)
				return;

			CharacterBody victimBody = damageReport.victimBody;
			if (attacker.TryGetComponent(out CharacterBody attackerBody) && victimBody && victimBody.isChampion)
			{
				int itemCount = GetCount(attackerBody);
				int itemCountTotal = attackerBody.teamComponent ? itemCount : Util.GetItemCountForTeam(attackerBody.teamComponent.teamIndex, ItemsDef.itemIndex, false, false);
				int buffCount = victimBody.GetBuffCount(hiddenForceTriggerCount);
				if (itemCountTotal > 0)
				{
					int maxTriggers = onKillForceTriggersBase + onKillForceTriggersStack * (itemCountTotal - 1);
					float thresholdPerTrigger = 1 / ((float)maxTriggers + 1);
					float nextThreshold = thresholdPerTrigger * (buffCount + 1);

					HealthComponent victimHealthComponent = victimBody.healthComponent;
					if (victimHealthComponent.combinedHealthFraction <= 1 - nextThreshold)
					{
						victimBody.AddBuff(hiddenForceTriggerCount);
						List<CharacterBody> list = (from master in CharacterMaster.instancesList
													select master.GetBody() into body
													where body && body.teamComponent.teamIndex == TeamIndex.Player && base.GetCount(body) > 0
													select body).ToList<CharacterBody>();
						MakeFakeDeath(victimHealthComponent, damageInfo, list);
					}
				}
			}
		}

        private void BloodRelicOnKill(DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            if(attackerBody != null)
            {
                int count = GetCount(attackerBody);
                if(count > 0)
                {
					float healFraction = Util.ConvertAmplificationPercentageIntoReductionNormalized(healFractionOnKillBase + healFractionOnKillStack * (count - 1));
                    attackerBody.healthComponent.HealFraction(healFraction, new ProcChainMask());
                }
            }
		}
		private void MakeFakeDeath(HealthComponent self, DamageInfo damageInfo, List<CharacterBody> attackers)
		{
			foreach (CharacterBody characterBody in attackers)
			{
				DamageInfo damageInfo2 = new DamageInfo
				{
					attacker = ((characterBody != null) ? characterBody.gameObject : null),
					crit = false,
					damage = damageInfo.damage,
					position = damageInfo.position,
					procCoefficient = damageInfo.procCoefficient,
					damageType = damageInfo.damageType,
					damageColorIndex = damageInfo.damageColorIndex
				};
				DamageReport damageReport = new DamageReport(damageInfo2, self, damageInfo.damage, self.combinedHealth);
				GlobalEventManager.instance.OnCharacterDeath(damageReport);
			}
		}
    }
}
