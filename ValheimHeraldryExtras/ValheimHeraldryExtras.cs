using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace ValheimHeraldryExtras
{
    /// <summary>
    /// Companion to Valheim Heraldry Mod: extra paintable pieces and items.
    /// Painting is done by the main mod through its tagged-face convention
    /// (ValheimHeraldryMod_Face, _Face_1.._4, _CentrePoint): this mod only
    /// supplies models and registers them. Without the main mod the pieces
    /// still work, unpainted.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(HeraldryModGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    internal class ValheimHeraldryExtras : BaseUnityPlugin
    {
        public const string PluginGUID = "com.hippotech.valheimheraldryextras";
        public const string PluginName = "ValheimHeraldryExtras";
        public const string PluginVersion = "0.0.1";

        /// <summary>GUID of Valheim Heraldry Mod (soft dependency).</summary>
        public const string HeraldryModGUID = "com.hippotech.valheimheraldrymod";

        /// <summary>Prefix for every name another mod could see or collide with.</summary>
        public const string Prefix = "ValheimHeraldryExtras_";

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        public static bool HeraldryModLoaded { get; private set; }

        private void Awake()
        {
            HeraldryModLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(HeraldryModGUID);
            Jotunn.Logger.LogInfo(HeraldryModLoaded
                ? "Valheim Heraldry Mod found: pieces will be paintable."
                : "Valheim Heraldry Mod not found: pieces will be plain.");
        }
    }
}
