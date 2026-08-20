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
using static MoreStats.OnHit;
using static R2API.RecalculateStatsAPI;
using static RoR2.Items.BaseItemBodyBehavior;
using RoR2.Items;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

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

		public static GameObject radiusIndicatorPrefab;

		[AutoConfig("Life Steal Radius Base", 16f)]
		public static float lifeStealRadiusBase = 16f;
		[AutoConfig("Life Steal Radius Stack", 0)]
		public static float lifeStealRadiusStack = 0f;
		[AutoConfig("Life Steal Fraction Base", 0.05f)]
		public static float lifeStealAmountBase = 0.05f;
		[AutoConfig("Life Steal Fraction Stack", 0.025f)]
		public static float lifeStealAmountStack = 0.025f;
		[AutoConfig("Bleed Chance Base", 10f)]
		public static float bleedChanceBase = 10f;
		[AutoConfig("Bleed Chance Stack", 0f)]
		public static float bleedChanceStack = 0f;
		[AutoConfig("Bleed Damage Life Steal Multiplier", 0.5f)]
		public static float bleedEffectiveProcCoeff = 0.5f;
		#endregion
		public static BuffDef hiddenForceTriggerCount;
        public override string ItemName => "Relic of Blood";

        public override string ItemLangTokenName => "BLOODANOMALY";

        public override string ItemPickupDesc => "Gain life steal against nearby enemies. Bleeding enemies heal you for more.";

		public override string ItemFullDescription =>
			$"{DamageColor(bleedChanceBase + "%")} chance to {DamageColor("bleed")} an enemy. " +
			$"{HealingColor("Heal")} for {HealingColor(lifeStealAmountBase.AsPercent())} " +
			$"{StackText(lifeStealAmountStack.AsPercent())} of {DamageColor("total damage")} dealt " +
			$"against enemies within {DamageColor($"{lifeStealRadiusBase}m")}. " +
			$"Damage dealt by {DamageColor("bleed status")} heals for {DamageColor($"+{bleedEffectiveProcCoeff.AsPercent()}")} more.";
			//$"On killing an enemy, immediately heal for " +
			//$"{HealingColor(Tools.ConvertDecimal(healFractionOnKillBase))} {StackText($"+{Tools.ConvertDecimal(healFractionOnKillStack)}")} " +
			//$"of {HealingColor("maximum health")}. Dealing damage to {UtilityColor("Champions")} will " +
			//$"force-trigger {DamageColor("On-Kill")} effects up to " +
			//$"{DamageColor($"{onKillForceTriggersBase}")} {StackText($"+{onKillForceTriggersStack}")} times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

		public override GameObject ItemModel => LoadDropPrefab("PickupBloodAnomaly");

		public override Sprite ItemIcon => LoadItemIcon("texIconBloodAnomaly");
		public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist , ItemTag.WorldUnique, ItemTag.CannotSteal, ItemTag.AIBlacklist, ItemTag.OnKillEffect, ItemTag.ObjectiveRelated };

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

			NewMoonPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_NearbyDamageBonus.NearbyDamageBonusIndicator_prefab, CreateRangeIndicator);

			base.Init();
		}

        private void CreateRangeIndicator(GameObject obj)
        {
			radiusIndicatorPrefab = obj.InstantiateClone("BloodAnomalyRangeIndicator", true);

			Transform radiusSpherical = radiusIndicatorPrefab.transform.GetChild(1);
            if (radiusSpherical)
            {
				radiusSpherical.transform.localScale = Vector3.one * lifeStealRadiusBase * 2;

				if(radiusSpherical.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
                {
					Material mat = UnityEngine.Object.Instantiate(meshRenderer.material);
					mat.SetColor("_TintColor", new Color32(255,168,36,139));

					meshRenderer.material = mat;
                }
            }

			Modules.Content.AddNetworkedObjectPrefab(radiusIndicatorPrefab);
        }

        public override void PostInit()
        {
            base.PostInit();

			CraftableDef craftable = ScriptableObject.CreateInstance<CraftableDef>();
			craftable.name = "CRAFTABLE_" + this.ItemLangTokenName;
			craftable.pickup = this.ItemsDef;
			craftable.itemIndex = this.ItemsDef.itemIndex;

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
			anyQuest.itemTier = ItemTier.Boss;
			RecipeIngredient[] anyWithTags = Tools.GetAllIngredientsWithTags(
				required: new ItemTag[] { ItemTag.OnKillEffect },
				forbidden: new ItemTag[] { },
				maxTier: 3
				);

			craftable.recipes = new Recipe[0];
			craftable.AddAllRecipePermutations(new RecipeIngredient[] { corpsebloom, gesture, focon }, anyWithTags.Append(anyQuest).ToArray());
			Content.AddCraftableDef(craftable);
		}

        public override void Hooks()
        {
			//GlobalEventManager.onServerDamageDealt += BloodRelicOnDamageDealt;
			//GlobalEventManager.onCharacterDeathGlobal += BloodRelicOnKill;
			GlobalEventManager.onServerDamageDealt += BloodRelicLifeSteal;
			//GetHitBehavior += BloodRelicNearbyLifeSteal;
			GetStatCoefficients += BloodRelicBleed;
        }

        private void BloodRelicBleed(CharacterBody sender, StatHookEventArgs args)
		{
			int relicCount = GetCount(sender);
			if (relicCount <= 0)
				return;
			args.bleedChanceAdd += GetStackValue(bleedChanceBase, bleedChanceStack, relicCount);
		}

        private void BloodRelicLifeSteal(DamageReport damageReport)
		{
			CharacterBody attackerBody = damageReport.attackerBody;
			CharacterBody victimBody = damageReport.victimBody;
			DamageInfo damageInfo = damageReport.damageInfo;
			if (attackerBody == null || victimBody == null || damageInfo == null)
				return;

			float effectiveProcCoefficient = (damageInfo.dotIndex == DotController.DotIndex.Bleed) ? bleedEffectiveProcCoeff: damageInfo.procCoefficient;
			if (effectiveProcCoefficient <= 0)
				return;

			int relicCount = GetCount(attackerBody);
			if (relicCount <= 0)
				return;

			float distanceSqr = (attackerBody.corePosition - victimBody.corePosition).sqrMagnitude;
			float maxDistanceSqr = Mathf.Pow(GetStackValue(lifeStealRadiusBase, lifeStealRadiusStack, relicCount), 2);
			if (distanceSqr > maxDistanceSqr)
				return;

			float lifeStealAmt = GetStackValue(lifeStealAmountBase, lifeStealAmountStack, relicCount);
			HealOrb healOrb = new HealOrb();
			healOrb.healValue = damageInfo.damage * lifeStealAmt * effectiveProcCoefficient;
			healOrb.origin = damageInfo.position;
			healOrb.target = attackerBody.mainHurtBox;
			healOrb.overrideDuration = 0.1f;
			OrbManager.instance.AddOrb(healOrb);
		}

        private void BloodRelicNearbyLifeSteal(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
			int relicCount = GetCount(attackerBody);
			if (relicCount <= 0)
				return;

			float distanceSqr = (attackerBody.corePosition - victimBody.corePosition).sqrMagnitude;
			float maxDistanceSqr = GetStackValue(lifeStealRadiusBase, lifeStealRadiusStack, relicCount);
			if (distanceSqr > maxDistanceSqr)
				return;

			float lifeStealAmt = GetStackValue(lifeStealAmountBase, lifeStealAmountStack, relicCount);
			HealOrb healOrb = new HealOrb();
			healOrb.healValue = lifeStealAmt;
			healOrb.origin = damageInfo.position;
			healOrb.target = attackerBody.mainHurtBox;
			healOrb.overrideDuration = 0.1f;
			OrbManager.instance.AddOrb(healOrb);
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
	public class BloodAnomalyBehavior : BaseItemBodyBehavior
	{
		[BaseItemBodyBehavior.ItemDefAssociationAttribute(useOnServer = true, useOnClient = false)]
		private static ItemDef GetItemDef() => BloodAnomaly.instance?.ItemsDef ?? null;
		private void OnEnable()
		{
			this.indicatorEnabled = true;
		}

		private void OnDisable()
		{
			this.indicatorEnabled = false;
		}
		private bool indicatorEnabled
		{
			get
			{
				return this.nearbyDamageBonusIndicator;
			}
			set
			{
				if (this.indicatorEnabled == value)
				{
					return;
				}
				if (value)
				{
					this.nearbyDamageBonusIndicator = UnityEngine.Object.Instantiate<GameObject>(BloodAnomaly.radiusIndicatorPrefab, base.body.corePosition, Quaternion.identity);
					this.nearbyDamageBonusIndicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
					return;
				}
				UnityEngine.Object.Destroy(this.nearbyDamageBonusIndicator);
				this.nearbyDamageBonusIndicator = null;
			}
		}

		private GameObject nearbyDamageBonusIndicator;
	}
}
