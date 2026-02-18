using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using R2API;
using R2API.Utils;
using UnityEngine;
using RoR2.ExpansionManagement;
using System.Runtime.CompilerServices;
using RoR2;
using UnityEngine.AddressableAssets;
using RoR2.ContentManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using NewMoon.Modules;
using NewMoon.Items;
using NewMoon.Artifacts;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace NewMoon
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.EliteAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ProcTypeAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(guid, modName, version)]
    public partial class NewMoonPlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "NewMoon";
        public const string version = "2.0.0";

        public static NewMoonPlugin instance;
        public static AssetBundle mainAssetBundle => CommonAssets.mainAssetBundle;

        internal static event Action onLoaded;

        public static ItemTierDef lunarRelicTierDef;

        #region config
        public static bool DoPillarItemDrop = false;
        #endregion
        void Awake()
        {
            instance = this;

            Modules.Config.Init();
            Log.Init(Logger);

            //lunarRelicTierDef = ScriptableObject.CreateInstance<ItemTierDef>();
            //lunarRelicTierDef.name = "Lunar Relic";
            //lunarRelicTierDef.colorIndex = ColorCatalog.ColorIndex.LunarItem;
            //lunarRelicTierDef.darkColorIndex = ColorCatalog.ColorIndex.LunarItemDark;
            //lunarRelicTierDef.canRebirth = false;
            //lunarRelicTierDef.canScrap = false;
            //lunarRelicTierDef.isDroppable = true;
            //lunarRelicTierDef.pickupRules = ItemTierDef.PickupRules.Default;
            //LoadAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.LunarTierDef_asset, (lunarTierDef) =>
            //{
            //    lunarRelicTierDef.bgIconTexture = lunarTierDef.bgIconTexture;
            //    lunarRelicTierDef.dropletDisplayPrefab = lunarTierDef.dropletDisplayPrefab;
            //    lunarRelicTierDef.highlightPrefab = lunarTierDef.highlightPrefab;
            //});
            //lunarRelicTierDef.tier = ItemTier.AssignedAtRuntime;
            //Content.AddTierDef(lunarRelicTierDef);

            Modules.Language.Init();
            Modules.Hooks.Init();
            Modules.CommonAssets.Init();
            Modules.EliteModule.Init();
            //Modules.Spawnlists.Init();

            DoPillarItemDrop = GetConfigBool(true, "Global : Endgame Items",
                "Setting to TRUE will enable the Moon Relic items, " +
                "which drop from pillars, and all features that rely on these items.");
            if (DoPillarItemDrop)
            {
                MakePillarsFun();
            }
            if(GetConfigBool(true, "Global : Mithrix : Lunar Exploders Spawn On Ramps", 
                "Setting to TRUE will cause Lunar Exploders to spawn on arena ramps during the fight."))
            {
                LunarExplodersDuringBrother();
            }

            InitializeContent();

            onLoaded.Invoke();

            Modules.Config.Save();

            // this has to be last
            new Modules.ContentPacks().Initialize();
        }

        #region content initialization
        private void InitializeContent()
        {
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

            //BeginInitializing<SurvivorBase>(allTypes, "SwanSongSurvivors.txt");

            ///items
            ///interactables
            ///skills
            ///equipment
            ///elites
            ///artifacts
            ///scavengers
            //BeginInitializing<ReworkBase>(allTypes, "SwanSongReworks.txt");

            BeginInitializing<ItemBase>(allTypes, "NewMoonItems.txt");

            //BeginInitializing<EquipmentBase>(allTypes, "SwanSongEquipment.txt");

            //BeginInitializing<EliteEquipmentBase>(allTypes, "SwanSongElites.txt");

            //BeginInitializing<InteractableBase>(allTypes, "SwanSongInteractables.txt");

            BeginInitializing<ArtifactBase>(allTypes, "NewMoonArtifacts.txt");

            //BeginInitializing<SkillBase>(allTypes, "SwanSongSkills.txt");

            //BeginInitializing<TwistedScavengerBase>(allTypes, "SwanSongScavengers.txt");
        }

        private void BeginInitializing<T>(Type[] allTypes, string fileName = "") where T : SharedBase
        {
            Type baseType = typeof(T);
            //base types must be a base and not abstract
            if (!baseType.IsAbstract)
            {
                Log.Error(Log.Combine() + "Incorrect BaseType: " + baseType.Name);
                return;
            }


            IEnumerable<Type> objTypesOfBaseType = allTypes.Where(type => !type.IsAbstract && type.IsSubclassOf(baseType));

            if (objTypesOfBaseType.Count() <= 0)
                return;

            IEnumerable<SharedBase> objsOfBaseType =
                objTypesOfBaseType
                    .Select((objType) => (T)System.Activator.CreateInstance(objType))
                    .OrderBy((sharedBase) => sharedBase.loadOrder);

            Log.Debug(Log.Combine(baseType.Name) + "Initializing");

            foreach (SharedBase obj in objsOfBaseType)
            {
                string s = Log.Combine(baseType.Name, obj.ConfigName);
                if (ValidateBaseType(obj as SharedBase))
                {
                    Log.Debug(s + "Validated");
                    InitializeBaseType(obj as SharedBase);
                    Log.Debug(s + "Initialized");
                }
            }

            if (!string.IsNullOrEmpty(fileName))
                Modules.Language.TryPrintOutput(fileName);
        }

        bool ValidateBaseType(SharedBase obj)
        {
            bool enabled = obj.isEnabled;
            if (obj.forcePrerequisites)
                return enabled && obj.GetPrerequisites();

            return obj.Bind(enabled, "Should This Content Be Enabled") && obj.GetPrerequisites();
        }
        void InitializeBaseType(SharedBase obj)
        {
            obj.Init();
        }
        #endregion

        #region config
        public static bool GetConfigBool(bool defaultValue, string packetTitle, string desc = "")
        {
            return ConfigManager.DualBindToConfig<bool>(packetTitle, Modules.Config.MyConfig, "Should This Content Be Enabled", defaultValue, desc);
        }
        #endregion

        #region loading and modifying content
        public static Sprite TryLoadSpriteFromBundle(string path, AssetBundle assetBundle = null, bool fallBackOnWrench = false)
        {
            Sprite s = TryLoadFromBundle<Sprite>(path, assetBundle);
            if (s)
                return s;

            s = Addressables.LoadAssetAsync<Sprite>(
                    (fallBackOnWrench ? RoR2BepInExPack.GameAssetPaths.RoR2_Base_Core.texNullIcon_png
                    : RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texWIPIcon_png)
                    ).WaitForCompletion();//Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
            return s;
        }
        public static T TryLoadFromBundle<T>(string path, AssetBundle assetBundle = null) where T : UnityEngine.Object
        {
            if (assetBundle == null)
                assetBundle = NewMoonPlugin.mainAssetBundle;

            if (assetBundle && !string.IsNullOrWhiteSpace(path))
            {
                if (assetBundle.Contains(path))
                    return assetBundle.LoadAsset<T>(path);
            }
            return null;
        }
        public static AssetReferenceT<T> LoadAsync<T>(string guid, Action<T> callback) where T : UnityEngine.Object
        {
            void onCompleted(AsyncOperationHandle<T> handle)
            {
                if (!(handle.Result is T) || handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load asset [{handle.DebugName}] : {handle.OperationException}");
                    return;
                }

                callback(handle.Result);
            }

            AssetReferenceT<T> ref1 = new AssetReferenceT<T>(guid);
            AsyncOperationHandle<T> handle = AssetAsyncReferenceManager<T>.LoadAsset(ref1);

            if (callback == null)
            {
                return ref1;
            }

            if (handle.IsDone)
            {
                onCompleted(handle);
                return ref1;
            }

            handle.Completed += onCompleted;
            return ref1;
        }
        public static void RemoveCraftingRecipe(string guid)
        {
            LoadAsync<CraftableDef>(guid, (craftableDef) =>
            {
                craftableDef.recipes = new Recipe[] { };
            });
        }
        public static void RetierItemAsync(string itemGuid, ItemTier tier = ItemTier.NoTier, Action<ItemDef> callback = null)
        {
            AssetReferenceT<ItemDef> ref1 = new AssetReferenceT<ItemDef>(itemGuid);
            AssetAsyncReferenceManager<ItemDef>.LoadAsset(ref1).Completed += (ctx) =>
            {
                ItemDef itemDef = ctx.Result;
                itemDef.tier = tier;
                itemDef.deprecatedTier = tier;

                if (callback != null)
                    callback.Invoke(itemDef);
            };
        }
        public static void RemoveEquipmentAsync(string equipmentGuid, Action<EquipmentDef> callback = null)
        {
            AssetReferenceT<EquipmentDef> ref1 = new AssetReferenceT<EquipmentDef>(equipmentGuid);
            AssetAsyncReferenceManager<EquipmentDef>.LoadAsset(ref1).Completed += (ctx) =>
            {
                EquipmentDef equipDef = ctx.Result;
                equipDef.canDrop = false;
                equipDef.canBeRandomlyTriggered = false;
                equipDef.enigmaCompatible = false;
                equipDef.dropOnDeathChance = 0;

                if (callback != null)
                    callback.Invoke(equipDef);
            };
        }
        #endregion
    }
}
