using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using UnityEngine.AddressableAssets;
using NewMoon.Modules;
using System.Linq;
using RoR2.Projectile;
using static MoreStats.OnHit;
using static R2API.ProcTypeAPI;
using static NewMoon.Modules.Language.Styling;
using EntityStates.LunarWisp;

namespace NewMoon.Items
{
    class SoulAnomaly : ItemBase<SoulAnomaly>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return NewMoonPlugin.DoPillarItemDrop;
        }
        #region config
        public override string ConfigName => "Items : Commencement : Relic of Soul";

        [AutoConfig("Base Movement Speed Multiplier", 0.2f)]
        public static float baseMoveSpeedAdd = 0.2f;
        [AutoConfig("Max Bonus Movement Speed Multiplier", 1.3f)]
        public static float maxMoveMultAdd = 1.3f;
        [AutoConfig("Max Bonus Attack Speed Multiplier", 1.3f)]
        public static float maxAttackMultAdd = 1.3f;

        public static float procChanceMin = 5f;
        public static float procChanceBase = 3f;
        public static float procChanceStack = 3f;

        public static float soulProjectileDamageCoefficient = 6f;
        #endregion

        public static ModdedProcType soulProjectileProcType;

        public static GameObject soulProjectile;

        public static BuffDef spiritBuff;
        public override string ItemName => "Relic of Soul";

        public override string ItemLangTokenName => "SOULANOMALY";

        public override string ItemPickupDesc => "Fire a soul projectile that triggers on-kill effects.";

        public override string ItemFullDescription => $"" +
            $"{DamageColor(procChanceMin + "%")} chance on hit to " +
            $"release a screaming soul as a projectile, dealing " +
            $"{DamageColor(soulProjectileDamageCoefficient.AsPercent() + " BASE damage")} and " +
            $"{UtilityColor("force-triggering")} all {UtilityColor("on-kill effects")} upon impact. " +
            $"Copies the source attack's damage type. " +
            $"Every {DamageColor(1f.AsPercent())} attack damage dealt increases activation chance " +
            $"by {DamageColor(procChanceBase + "%")} {StackText($"+{procChanceStack}%")}.";
            //$"Gain {Tools.ConvertDecimal(baseMoveSpeedAdd)} movement speed. " +
            //$"For every missing <style=cIsHealth>{100 / (float)SoulAnomalyBehavior.maxBuffCount}% of max health</style>, " +
            //$"increase movement speed by <style=cIsDamage>{Tools.ConvertDecimal(maxMoveMultAdd / SoulAnomalyBehavior.maxBuffCount)}</style> " +
            //$"<style=cStack>(+{Tools.ConvertDecimal(maxMoveMultAdd / SoulAnomalyBehavior.maxBuffCount)} per stack)</style> " +
            //$"and attack speed by <style=cIsDamage>{Tools.ConvertDecimal(maxAttackMultAdd / SoulAnomalyBehavior.maxBuffCount)}</style> " +
            //$"<style=cStack>(+{Tools.ConvertDecimal(maxAttackMultAdd / SoulAnomalyBehavior.maxBuffCount)} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override GameObject ItemModel => LoadDropPrefab("PickupSoulAnomaly");

        public override Sprite ItemIcon => LoadItemIcon("texIconSoulAnomaly");
        public override ItemTag[] ItemTags { get; } = new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal, ItemTag.ObjectiveRelated };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            soulProjectileProcType = ReserveProcType();
            spiritBuff = Content.CreateAndAddBuff(
                "bdSpiritSpeed",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion(),
                Color.cyan,
                true, false
                );

            base.Init();

            NewMoonPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarWisp.LunarWispTrackingBomb_prefab, CreateSoulProjectile);
        }

        private void CreateSoulProjectile(GameObject lunarWispTrackingBomb)
        {
            soulProjectile = lunarWispTrackingBomb.InstantiateClone("SoulRelicProjectile", true);

            HealthComponent hc;
            if(soulProjectile.TryGetComponent(out hc))
            {
                hc.globalDeathEventChanceCoefficient = 0;
            }

            ProjectileGrantOnKillOnDestroy onKillTrigger = soulProjectile.AddComponent<ProjectileGrantOnKillOnDestroy>();
            onKillTrigger.healthComponent = hc;

            if(soulProjectile.TryGetComponent(out ProjectileDirectionalTargetFinder pdtf))
            {
                pdtf.lookRange = 100f;
                pdtf.lookCone = 35f;
            }

            Content.AddProjectilePrefab(soulProjectile);
        }

        public override void Hooks()
        {
            GetHitBehavior += FireSoulProjectileOnHit;
            //On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            //GetStatCoefficients += SpiritSpeedBoosts;
        }

        private void FireSoulProjectileOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (damageInfo.procChainMask.HasModdedProc(soulProjectileProcType))
                return;
            bool damageSourcedFromSkill = damageInfo.damageType.IsDamageSourceSkillBased || damageInfo.damageType.damageSource == DamageSource.Equipment;
            if (damageSourcedFromSkill == false)
                return;

            int relicCt = GetCount(attackerBody);
            if (relicCt <= 0)
                return;

            float damageCoefficient = damageInfo.damage /= attackerBody.damage;
            float procRate = NewMoonPlugin.GetProcRate(damageInfo);//damageInfo.procCoefficient;

            float procChance = Util.ConvertAmplificationPercentageIntoReductionPercentage((procChanceMin + GetStackValue(procChanceBase, procChanceStack, relicCt) * damageCoefficient) * procRate);
            if(Util.CheckRoll(procChance, attackerBody.master))
            {
                Ray aimRay = attackerBody.inputBank.GetAimRay();
                FireProjectileInfo fpi = new FireProjectileInfo
                {
                    projectilePrefab = soulProjectile,
                    crit = damageInfo.crit,
                    damage = attackerBody.damage * soulProjectileDamageCoefficient,
                    damageTypeOverride = damageInfo.damageType,
                    damageColorIndex = DamageColorIndex.Item,
                    force = 0,
                    owner = attackerBody.gameObject,
                    position = aimRay.origin,
                    rotation = Util.QuaternionSafeLookRotation((victimBody.corePosition - attackerBody.corePosition).normalized)
                };
                ProcChainMask mask = damageInfo.procChainMask;
                mask.AddModdedProc(soulProjectileProcType);
                fpi.procChainMask = mask;
                ProjectileManager.instance.FireProjectile(fpi);
            }
        }

        public override void PostInit()
        {
            base.PostInit();

            CraftableDef craftable = ScriptableObject.CreateInstance<CraftableDef>();
            craftable.name = "CRAFTABLE_" + this.ItemLangTokenName;
            craftable.pickup = this.ItemsDef;
            craftable.itemIndex = this.ItemsDef.itemIndex;

            RecipeIngredient glass = new RecipeIngredient();
            glass.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarDagger.LunarDagger_asset).WaitForCompletion();
            glass.type = IngredientTypeIndex.AssetReference;
            RecipeIngredient crown = new RecipeIngredient();
            crown.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_GoldOnHit.GoldOnHit_asset).WaitForCompletion();
            crown.type = IngredientTypeIndex.AssetReference;
            RecipeIngredient lightflux = new RecipeIngredient();
            lightflux.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_HalfAttackSpeedHalfCooldowns.HalfAttackSpeedHalfCooldowns_asset).WaitForCompletion();
            lightflux.type = IngredientTypeIndex.AssetReference;
            RecipeIngredient effigy = new RecipeIngredient();
            effigy.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_CrippleWard.CrippleWard_asset).WaitForCompletion();
            effigy.type = IngredientTypeIndex.AssetReference;

            RecipeIngredient[] anyQuest = Tools.GetAllBossKeyIngredients();
            //    new RecipeIngredient();
            //anyQuest.requiredTags = new ItemTag[] { ItemTag.ObjectiveRelated };
            //anyQuest.forbiddenTags = new ItemTag[] { ItemTag.Count };
            //anyQuest.type = IngredientTypeIndex.AnyItem;
            //anyQuest.itemTier = ItemTier.Boss;
            RecipeIngredient[] anyWithTags = Tools.GetAllIngredientsWithTags(
                required: new ItemTag[] { ItemTag.Damage },
                forbidden: new ItemTag[] { },
                maxTier: 3
                );

            craftable.recipes = new Recipe[0];
            craftable.AddAllRecipePermutations(anyWithTags.Concat(new RecipeIngredient[] { glass, crown, lightflux/*, effigy*/ }).ToArray(), anyQuest);
            Content.AddCraftableDef(craftable);
        }

        private void SpiritSpeedBoosts(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                float buffFraction = (float)sender.GetBuffCount(SoulAnomaly.spiritBuff) / (float)SoulAnomalyBehavior.maxBuffCount;
                //Debug.Log(buffFraction);

                args.attackSpeedMultAdd += buffFraction * maxAttackMultAdd * itemCount;
                args.moveSpeedMultAdd += (buffFraction * maxMoveMultAdd * itemCount) + baseMoveSpeedAdd;
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.healthComponent != null)
                {
                    self.AddItemBehavior<SoulAnomalyBehavior>(GetCount(self));
                }
            }
        }
    }
    public class SoulAnomalyBehavior : CharacterBody.ItemBehavior
    {
        HealthComponent healthComponent;
        BuffIndex buffIndex = SoulAnomaly.spiritBuff.buffIndex;
        public static int maxBuffCount = 10;
        int buffCount = 0;

        private void Start()
        {
            healthComponent = body.healthComponent;
            buffCount = body.GetBuffCount(buffIndex);
        }
        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            float missingHealthFraction = 1 - (healthComponent.health + healthComponent.shield) / healthComponent.fullCombinedHealth;
            int newBuffCount = Mathf.CeilToInt(missingHealthFraction * maxBuffCount);
            if (newBuffCount > buffCount && buffCount < maxBuffCount)
            {
                this.body.AddBuff(buffIndex);
                buffCount++;
            }
            else if (newBuffCount < buffCount && buffCount > 0)
            {
                this.body.RemoveBuff(buffIndex);
                buffCount--;
            }
        }
    }
}
