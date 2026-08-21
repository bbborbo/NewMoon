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
using EntityStates.Mage;
using RainrotSharedUtils;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace NewMoon.Items
{
    class DesignAnomaly : ItemBase<DesignAnomaly>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return NewMoonPlugin.DoPillarItemDrop;
        }
        #region config
        public override string ConfigName => "Items : Commencement : Relic of Design";
        public static BuffDef HoverChargeBuff;
        public static BuffDef HoverActiveBuff;
        public static float rechargeDelay = 2f;
        public static int rechargePerFrameBase = 1;
        public static int rechargePerFrameStack = 1;
        public static int maxHoverChargeBase = 60;
        public static int maxHoverChargeStack = 30;
        public static float movementSpeedWhileHoveringBase = 2f;
        public static float movementSpeedWhileHoveringStack = 0.5f;
        #endregion
        public override string ItemName => "Relic of Design";

        public override string ItemLangTokenName => "DESIGNANOMALY";

        public override string ItemPickupDesc => "Double jump. Hold JUMP while falling to hover.";

        public override string ItemFullDescription => $"Gain an additional air jump. While airborne, holding JUMP allows you to float in the air with " +
            $"{UtilityColor($"+{movementSpeedWhileHoveringBase.AsPercent()}")} movement speed {StackText($"+{movementSpeedWhileHoveringStack.AsPercent()}")} " +
            $"for up to {UtilityColor(((float)maxHoverChargeBase / 60f).ToString())} seconds {StackText("+" + ((float)maxHoverChargeStack / 60f).ToString())}. " +
            $"{StackColor($"(Recharges {((float)rechargePerFrameStack / (float)rechargePerFrameBase).AsPercent()} faster per stack)")}";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override GameObject ItemModel => LoadDropPrefab("PickupDesignAnomaly");

        public override Sprite ItemIcon => LoadItemIcon("texIconDesignAnomaly");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal, ItemTag.ObjectiveRelated };


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            HoverChargeBuff = Content.CreateAndAddBuff(
                "bdDesignHoverCharge",
                Addressables.LoadAssetAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.bdOnFire_asset).WaitForCompletion().iconSprite,
                Color.grey,
                canStack: true,
                isDebuff: false,
                BuffDef.StackingDisplayMethod.Percentage,
                isHidden: false
                );
            HoverActiveBuff = Content.CreateAndAddBuff(
                "bdDesignHoverActive",
                Addressables.LoadAssetAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.bdOnFire_asset).WaitForCompletion().iconSprite,
                Color.grey,
                canStack: false,
                isDebuff: false,
                BuffDef.StackingDisplayMethod.Percentage,
                isHidden: false
                );
            base.Init();
        }
        public override void PostInit()
        {
            base.PostInit();

            CraftableDef craftable = ScriptableObject.CreateInstance<CraftableDef>();
            craftable.name = "CRAFTABLE_" + this.ItemLangTokenName;
            craftable.pickup = this.ItemsDef;
            craftable.itemIndex = this.ItemsDef.itemIndex;

            List<RecipeIngredient> ingredientsL = new List<RecipeIngredient>();
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_MonstersOnShrineUse.MonstersOnShrineUse_asset, "relic of design - defiant gouge");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RandomDamageZone.RandomDamageZone_asset, "relic of design - mercurial rachis");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FocusConvergence.FocusConvergence_asset, "relic of design - focused convergence");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarPotion.LunarPotion_asset, "relic of design - spinel tonic");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Hoof.Hoof_asset, "relic of design - goat hood");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Mushroom.Mushroom_asset, "relic of design - bustling fungus");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_SprintBonus.SprintBonus_asset, "relic of design - energy drink");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_SpeedBoostPickup.SpeedBoostPickup_asset, "relic of design - elusive antlers");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Feather.Feather_asset, "relic of design - hopoo feather");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_SprintOutOfCombat.SprintOutOfCombat_asset, "relic of design - red whip");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_JumpBoost.JumpBoost_asset, "relic of design - wax quail");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_KnockBackHitEnemies.KnockBackHitEnemies_asset, "relic of design - breaching fin");
            ingredientsL.TryLoadAndAddIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_JumpDamageStrike.JumpDamageStrike_asset, "relic of design - faraday spur");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Jetpack.Jetpack_asset, "relic of design - milky chrysalis");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FireBallDash.FireBallDash_asset, "relic of design - volcanic egg");
            ingredientsL.TryLoadAndAddIngredient<EquipmentDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_TeamWarCry.TeamWarCry_asset, "relic of design - gorags opus");

            craftable.AddAllRecipePermutations(ingredientsL.ToArray(), CraftingUtils.VanillaBossKeys);
            Content.AddCraftableDef(craftable);

            //RecipeIngredient gouge = new RecipeIngredient();
            //gouge.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_MonstersOnShrineUse.MonstersOnShrineUse_asset).WaitForCompletion();
            //gouge.type = IngredientTypeIndex.AssetReference;
            //RecipeIngredient trans = new RecipeIngredient();
            //trans.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShieldOnly.ShieldOnly_asset).WaitForCompletion();
            //trans.type = IngredientTypeIndex.AssetReference;
            //RecipeIngredient purity = new RecipeIngredient();
            //purity.pickup = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarBadLuck.LunarBadLuck_asset).WaitForCompletion();
            //purity.type = IngredientTypeIndex.AssetReference;
            //
            //RecipeIngredient anyQuest = new RecipeIngredient();
            //anyQuest.requiredTags = new ItemTag[] { ItemTag.ObjectiveRelated };
            //anyQuest.forbiddenTags = new ItemTag[] { ItemTag.Count };
            //anyQuest.type = IngredientTypeIndex.AnyItem;
            //anyQuest.itemTier = ItemTier.Boss;
            //RecipeIngredient[] anyWithTags = Tools.GetAllIngredientsWithTags(
            //    required: new ItemTag[] { ItemTag.MobilityRelated },
            //    forbidden: new ItemTag[] { ItemTag.Count },
            //    maxTier: 2
            //    );
            //
            //craftable.recipes = new Recipe[0];
            //craftable.AddAllRecipePermutations(anyWithTags.Concat(new RecipeIngredient[] { gouge, trans, purity }).ToArray(), new RecipeIngredient[] { anyQuest });
            //Content.AddCraftableDef(craftable);
        }

        public override void Hooks()
        {
            On.EntityStates.GenericCharacterMain.FixedUpdate += Hover;
            GetStatCoefficients += HoverMoveSpeed;
        }

        private void HoverMoveSpeed(CharacterBody sender, StatHookEventArgs args)
        {
            int ct = GetCount(sender);
            if (ct > 0)
            {
                args.jumpCountAdd += 1;
                if (sender.HasBuff(HoverActiveBuff))
                {
                    args.moveSpeedMultAdd += movementSpeedWhileHoveringBase + movementSpeedWhileHoveringStack * (ct - 1);
                }
            }
        }

        private void Hover(On.EntityStates.GenericCharacterMain.orig_FixedUpdate orig, EntityStates.GenericCharacterMain self)
        {
            orig(self);
            if (self.hasCharacterMotor && self.hasInputBank && self.isAuthority)
            {
                CharacterBody body = self.characterBody;
                CharacterMotor motor = self.characterMotor;
                if (body)
                {
                    bool jumpInputDown = self.inputBank.jump.down && !self.characterMotor.isGrounded;
                    if (jumpInputDown == true && body.HasBuff(HoverChargeBuff))
                    {
                        float verticalVelocity = motor.velocity.y;
                        if(verticalVelocity <= float.Epsilon)
                        {
                            verticalVelocity = Mathf.MoveTowards(verticalVelocity, 0, JetpackOn.hoverAcceleration * self.GetDeltaTime() * 2f); //yay magic numbers yay yay yay
                            motor.velocity = new Vector3(motor.velocity.x, Mathf.Min(verticalVelocity, 0), motor.velocity.z);
                            body.RemoveBuff(DesignAnomaly.HoverChargeBuff);
                            if (!body.HasBuff(DesignAnomaly.HoverChargeBuff))
                            {
                                UpdateJetpackDownState(body, false);
                            }
                            else if (!body.HasBuff(DesignAnomaly.HoverActiveBuff))
                                UpdateJetpackDownState(body, true);
                        }
                    }
                    if (jumpInputDown != self.inputBank.jump.wasDown)
                    {
                        UpdateJetpackDownState(body, jumpInputDown);
                    }
                }
            }
        }

        private static void UpdateJetpackDownState(CharacterBody body, bool isDown)
        {
            if(body && body.inventory && body.inventory.GetItemCountEffective(DesignAnomaly.instance.ItemsDef) > 0)
            {
                if (body.TryGetComponent(out DesignAnomalyBehavior designAnomaly))
                    designAnomaly.UpdateInputState(isDown);
            }
        }
    }

    public class DesignAnomalyBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => DesignAnomaly.instance?.ItemsDef ?? null;

        public bool isHoverButtonHeld = false;
        private float rechargeDelayCountdown = 0;

        public void UpdateInputState(bool isDown)
        {
            isHoverButtonHeld = isDown;
            if(isDown && IsFalling(body))
            {
                if(!body.HasBuff(DesignAnomaly.HoverActiveBuff))
                    body.AddBuff(DesignAnomaly.HoverActiveBuff);
            }
            else if (body.HasBuff(DesignAnomaly.HoverActiveBuff))
                body.RemoveBuff(DesignAnomaly.HoverActiveBuff);
        }

        private static bool IsFalling(CharacterBody body)
        {
            return body.characterMotor.velocity.y <= 0.2f && !body.characterMotor.isGrounded;
        }

        private void FixedUpdate()
        {
            if (isHoverButtonHeld)
            {
                if(IsFalling(body))
                {
                    rechargeDelayCountdown = DesignAnomaly.rechargeDelay;
                    return;
                }
            }
            if(rechargeDelayCountdown > 0)
            {
                rechargeDelayCountdown -= Time.fixedDeltaTime;
                return;
            }
            int recharge = DesignAnomaly.rechargePerFrameBase + DesignAnomaly.rechargePerFrameStack * (stack - 1);
            int maxCharge = DesignAnomaly.maxHoverChargeBase + DesignAnomaly.maxHoverChargeStack * (stack - 1);
            for(int i = 0; i < recharge; i++)
            {
                if (body.GetBuffCount(DesignAnomaly.HoverChargeBuff) >= maxCharge)
                    break;
                body.AddBuff(DesignAnomaly.HoverChargeBuff);
            }
        }

        private void OnEnable()
        {
            int maxCharge = DesignAnomaly.maxHoverChargeBase + DesignAnomaly.maxHoverChargeStack * (stack - 1);
            for (int i = body.GetBuffCount(DesignAnomaly.HoverChargeBuff); i < maxCharge; i++)
            {
                body.AddBuff(DesignAnomaly.HoverChargeBuff);
            }
        }
        private void OnDisable()
        {
            for (int i = body.GetBuffCount(DesignAnomaly.HoverChargeBuff); i == 0; i--)
            {
                body.RemoveBuff(DesignAnomaly.HoverChargeBuff);
            }
        }
    }
}
