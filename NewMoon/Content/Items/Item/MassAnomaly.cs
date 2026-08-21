using BepInEx.Configuration;
using R2API;
using RoR2;
using NewMoon.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static NewMoon.Modules.Language.Styling;
using System.Linq;
using static RoR2.Items.BaseItemBodyBehavior;
using RoR2.Items;
using MoreStats;
using RainrotSharedUtils;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace NewMoon.Items
{
    class MassAnomaly : ItemBase<MassAnomaly>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return NewMoonPlugin.DoPillarItemDrop;
        }
        public override string ConfigName => "Items : Commencement : Relic of Mass";
        public static BuffDef beetleArmor;
        public static int maxBeetleArmorStacks = 3;
        public static int durationPerBeetleArmor = 3;
        public static int armorPerBuffBase = 50;
        public static int armorPerBuffStack = 25;
        public static float retaliateCrippleDuration = 9f;

        public override string ItemName => "Relic of Mass";

        public override string ItemLangTokenName => "MASSANOMALY";

        public override string ItemPickupDesc => "Periodically gain protection from damage.";

        public override string ItemFullDescription => $"After not taking damage for {UtilityColor($"{durationPerBeetleArmor}")} seconds, " +
            $"gain a layer of {DamageColor("Chimera Armor")}, up to {UtilityColor($"{maxBeetleArmorStacks}")} times. " +
            $"Each layer of {DamageColor("Chimera Armor")} " +
            $"increases {HealingColor("armor")} by {HealingColor($"{armorPerBuffBase}")} {StackText("+" + armorPerBuffStack)}. " +
            $"Taking damage while protected strips 1 layer of {DamageColor("Chimera Armor")}, " +
            $"{DamageColor("Crippling")} the enemy who attacked you for {retaliateCrippleDuration}s.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override GameObject ItemModel => LoadDropPrefab("PickupMassAnomaly");

        public override Sprite ItemIcon => LoadItemIcon("texIconMassAnomaly");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal, ItemTag.ObjectiveRelated };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            base.Init();
            beetleArmor = Content.CreateAndAddBuff("bdAnomalyArmor",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion(),
                Color.cyan, true, false);
        }

        public override void PostInit()
        {
            base.PostInit();

            CraftableDef craftable = ScriptableObject.CreateInstance<CraftableDef>();
            craftable.name = "CRAFTABLE_" + this.ItemLangTokenName;
            craftable.pickup = this.ItemsDef;
            craftable.itemIndex = this.ItemsDef.itemIndex;

            //gesture, neturonum, stone pauldron, meteor, beads

            List<RecipeIngredient> ingredientsL = new List<RecipeIngredient>();
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_AutoCastEquipment.AutoCastEquipment_asset, "relic of mass - gesture of the drowned");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_TransferDebuffOnHit.TransferDebuffOnHit_asset, "relic of mass - neutronium weight");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarTrinket.LunarTrinket_asset, "relic of mass - beads of fealty");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset, "relic of mass - stone flux pauldron");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Meteor.Meteor_asset, "relic of mass - glowing meteorite");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_PersonalShield.PersonalShield_asset, "relic of mass - psg");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_WardOnLevel.WardOnLevel_asset, "relic of mass - warbanner");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FlatHealth.FlatHealth_asset, "relic of mass - meat");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_OutOfCombatArmor.OutOfCombatArmor_asset, "relic of mass - opal");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ArmorPlate.ArmorPlate_asset, "relic of mass - armor plating");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_DelayedDamage.DelayedDamage_asset, "relic of mass - warped echo");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_SlowOnHit.SlowOnHit_asset, "relic of mass - chronobauble");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_ExtraShrineItem.ExtraShrineItem_asset, "relic of mass - chance doll");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_GainArmor.GainArmor_asset, "relic of mass - jade elephant");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Blackhole.Blackhole_asset, "relic of mass - primordial cube");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GummyClone.GummyClone_asset, "relic of mass - goobo jr");

            craftable.AddAllRecipePermutations(ingredientsL.ToArray(), CraftingUtils.VanillaBossKeys);
            Content.AddCraftableDef(craftable);

            //RecipeIngredient neutronium = new RecipeIngredient();
            //neutronium.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_TransferDebuffOnHit.TransferDebuffOnHit_asset).WaitForCompletion();
            //neutronium.type = IngredientTypeIndex.AssetReference;
            //RecipeIngredient rachis = new RecipeIngredient();
            //rachis.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RandomDamageZone.RandomDamageZone_asset).WaitForCompletion();
            //rachis.type = IngredientTypeIndex.AssetReference;
            //RecipeIngredient stoneflux = new RecipeIngredient();
            //stoneflux.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset).WaitForCompletion();
            //stoneflux.type = IngredientTypeIndex.AssetReference;
            //RecipeIngredient meteor = new RecipeIngredient();
            //meteor.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Meteor.Meteor_asset).WaitForCompletion();
            //meteor.type = IngredientTypeIndex.AssetReference;
            //
            //RecipeIngredient anyQuest = new RecipeIngredient();
            //anyQuest.requiredTags = new ItemTag[] { ItemTag.ObjectiveRelated };
            //anyQuest.forbiddenTags = new ItemTag[] { ItemTag.Count };
            //anyQuest.type = IngredientTypeIndex.AnyItem;
            //anyQuest.itemTier = ItemTier.Boss;
            //RecipeIngredient[] anyWithTags = Tools.GetAllIngredientsWithTags(
            //    required: new ItemTag[] { ItemTag.FoodRelated },
            //    forbidden: new ItemTag[] { ItemTag.Count },
            //    maxTier: 1
            //    );
            //
            //craftable.recipes = new Recipe[0];
            //craftable.AddAllRecipePermutations(anyWithTags.Concat(new RecipeIngredient[] { neutronium, rachis, stoneflux/*, meteor*/ }).ToArray(), new RecipeIngredient[] { anyQuest });
            //Content.AddCraftableDef(craftable);
        }

        public override void Hooks()
        {
            //On.RoR2.HealthComponent.TakeDamageProcess += BackstabDamageReduction;
            GetStatCoefficients += ArmorBoost;
        }

        private void ArmorBoost(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            int buffCount = sender.GetBuffCount(beetleArmor);
            if (itemCount > 0 && buffCount > 0)
            {
                int armorPerBuff = armorPerBuffBase + armorPerBuffStack * (itemCount - 1);
                args.armorAdd += armorPerBuff * buffCount;
            }
        }
    }
    public class MassAnomalyBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => MassAnomaly.instance.ItemsDef;

        float beetleArmorInterval => MassAnomaly.durationPerBeetleArmor;
        float beetleArmorStopwatch;
        void OnBeetleProtectionGained()
        {
            GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
        }
        void OnBeetleProtectionCleared()
        {
            GlobalEventManager.onServerDamageDealt -= OnServerDamageDealt;
        }

        private void OnServerDamageDealt(DamageReport damageReport)
        {
            if (damageReport.victimBody != this.body || damageReport.attackerBody == this.body)
                return;
            if (damageReport.damageInfo.procCoefficient == 0)
                return;
            if (damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.Silent))
                return;

            int buffCount = body.GetBuffCount(MassAnomaly.beetleArmor);
            if (buffCount > 0)
            {
                body.RemoveBuff(MassAnomaly.beetleArmor);
                if (damageReport.attackerBody)
                    damageReport.attackerBody.AddTimedBuff(RoR2Content.Buffs.Cripple, MassAnomaly.retaliateCrippleDuration);
            }
            if (buffCount <= 1)
            {
                OnBeetleProtectionCleared();
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            int buffCount = body.GetBuffCount(MassAnomaly.beetleArmor);
            if (buffCount >= MassAnomaly.maxBeetleArmorStacks)
            {
                beetleArmorStopwatch = beetleArmorInterval;
                return;
            }
            beetleArmorStopwatch -= Time.fixedDeltaTime;
            if (beetleArmorStopwatch <= 0)
            {
                beetleArmorStopwatch += beetleArmorInterval;
                body.AddBuff(MassAnomaly.beetleArmor);
                if (buffCount == 0)
                    OnBeetleProtectionGained();
            }
        }

        private void OnDisable()
        {
            if (!NetworkServer.active)
                return;
            int buffCount = body.GetBuffCount(MassAnomaly.beetleArmor);
            if (buffCount > 0)
            {
                while (buffCount > 0)
                {
                    buffCount--;
                    this.body.RemoveBuff(MassAnomaly.beetleArmor);
                }
                OnBeetleProtectionCleared();
            }
        }
    }
}
