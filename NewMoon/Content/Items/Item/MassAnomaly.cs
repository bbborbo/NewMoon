using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NewMoon.Modules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        public static int baseArmorCount = 3;
        public static int armorPerPercent = 10;
        public static int armorDecayPerSecond = armorPerPercent * 4;
        public static int armorCap = armorPerPercent * 40;

        public override string ItemName => "Relic of Mass";

        public override string ItemLangTokenName => "MASSANOMALY";

        public override string ItemPickupDesc => "Reduce damage taken from successive hits."; //Temporarily reduce damage taken after getting hit?

        public override string ItemFullDescription => $"When taking damage, " +
            $"gain <style=cIsHealing>{armorPerPercent} temporary armor</style> " +
            $"<style=cStack>(+{armorPerPercent} per stack)</style> " +
            $"per <style=cIsHealth>1%</style> of health lost. " +
            $"<style=cIsUtility>This temporary armor caps at {armorCap} " +
            $"and decays {armorDecayPerSecond} per second.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override GameObject ItemModel => LoadDropPrefab("PickupMassAnomaly");

        public override Sprite ItemIcon => LoadItemIcon("texIconMassAnomaly");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void PostInit()
        {
            base.PostInit();

            CraftableDef craftable = ScriptableObject.CreateInstance<CraftableDef>();
            craftable.name = "CRAFTABLE_" + this.ItemLangTokenName;
            craftable.pickup = this.ItemsDef;
            craftable.itemIndex = this.ItemsDef.itemIndex;

            RecipeIngredient neutronium = new RecipeIngredient();
            neutronium.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_TransferDebuffOnHit.TransferDebuffOnHit_asset).WaitForCompletion();
            neutronium.type = IngredientTypeIndex.AssetReference;
            RecipeIngredient rachis = new RecipeIngredient();
            rachis.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RandomDamageZone.RandomDamageZone_asset).WaitForCompletion();
            rachis.type = IngredientTypeIndex.AssetReference;
            RecipeIngredient stoneflux = new RecipeIngredient();
            stoneflux.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset).WaitForCompletion();
            stoneflux.type = IngredientTypeIndex.AssetReference;
            RecipeIngredient meteor = new RecipeIngredient();
            meteor.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Meteor.Meteor_asset).WaitForCompletion();
            meteor.type = IngredientTypeIndex.AssetReference;

            RecipeIngredient anyQuest = new RecipeIngredient();
            anyQuest.requiredTags = new ItemTag[] { ItemTag.ObjectiveRelated };
            anyQuest.forbiddenTags = new ItemTag[] { ItemTag.Count };
            anyQuest.type = IngredientTypeIndex.AnyItem;
            RecipeIngredient anyFood = new RecipeIngredient();
            anyFood.requiredTags = new ItemTag[] { ItemTag.FoodRelated };
            anyFood.forbiddenTags = new ItemTag[] { ItemTag.Count };
            anyFood.type = IngredientTypeIndex.AnyItem;

            craftable.recipes = new Recipe[0];
            craftable.AddAllRecipePermutations(new RecipeIngredient[] { neutronium, rachis, stoneflux, meteor }, new RecipeIngredient[] { anyQuest, anyFood });
            Content.AddCraftableDef(craftable);
        }

        public override void Hooks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += NerfAdaptiveArmor;
            IL.RoR2.HealthComponent.ServerFixedUpdate += AdaptiveArmorDecay;
            On.RoR2.HealthComponent.ServerFixedUpdate += Fuck;
            On.RoR2.HealthComponent.OnInventoryChanged += AdaptiveArmorHook;
        }

        private void Fuck(On.RoR2.HealthComponent.orig_ServerFixedUpdate orig, HealthComponent self, float deltaTime)
        {
            orig(self, deltaTime);
            if(self.itemCounts.adaptiveArmor > 0)
            {
                //Debug.Log(self.adaptiveArmorValue);
            }
        }

        private void AdaptiveArmorDecay(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdfld<HealthComponent>("adaptiveArmorValue"),
                x => x.MatchLdcR4(out _)
                //, x => x.MatchCallOrCallvirt<Time>(nameof(Time.fixedDeltaTime))
                );
            c.Index++;
            c.Next.Operand = (float)armorDecayPerSecond;
        }

        private void NerfAdaptiveArmor(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdflda<HealthComponent>("itemCounts"),
                x => x.MatchLdfld<HealthComponent.ItemCounts>("adaptiveArmor"),
                x => x.MatchLdcI4(out _)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchDiv(),
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.Next.Operand = (float)armorPerPercent;
            
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<UnityEngine.Mathf>("Min")
                );
            c.Index--;
            c.Next.Operand = (float)armorCap;
        }

        private void AdaptiveArmorHook(On.RoR2.HealthComponent.orig_OnInventoryChanged orig, RoR2.HealthComponent self)
        {
            orig(self);
            if (self.body)
            {
                self.itemCounts.adaptiveArmor = GetCount(self.body) +
                    (self.body.inventory.GetItemCountEffective(RoR2.RoR2Content.Items.AdaptiveArmor) * baseArmorCount);
            }
        }
    }
}
