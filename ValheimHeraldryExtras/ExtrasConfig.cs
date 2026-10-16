using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace ValheimHeraldryExtras
{
    /// <summary>All config entries in one place. Admin-only entries are synced from the server by Jötunn.</summary>
    internal static class ExtrasConfig
    {
        public const string DefaultMartialDisplayRecipe = "LeatherScraps:2, Wood:4, Resin:2";

        private static ConfigEntry<string> martialDisplayRecipe;
        private static ConfigEntry<int> martialDisplayComfort;

        /// <summary>Raised when a piece setting changes (admin edit in game, or the server's values arriving).</summary>
        public static event Action PieceSettingsChanged;

        public static string MartialDisplayRecipe => martialDisplayRecipe?.Value ?? DefaultMartialDisplayRecipe;
        public static int MartialDisplayComfort => martialDisplayComfort?.Value ?? 1;

        public static void Bind(ConfigFile config)
        {
            martialDisplayRecipe = config.Bind("MartialDisplay", "Recipe", DefaultMartialDisplayRecipe,
                new ConfigDescription(
                    "What the Martial Display costs to build, as item:amount pairs separated by commas (prefab names, e.g. " +
                    "'LeatherScraps:2, Wood:4, Resin:2'). Needs a workbench nearby. Synced from the server; only admins can change it.",
                    null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            martialDisplayComfort = config.Bind("MartialDisplay", "Comfort", 1,
                new ConfigDescription(
                    "Comfort the Martial Display gives. It has a comfort group of its own, so it adds to banners, item stands " +
                    "and the rest; several Martial Displays still count once. Synced from the server; only admins can change it.",
                    new AcceptableValueRange<int>(0, 10), new ConfigurationManagerAttributes { IsAdminOnly = true }));

            martialDisplayRecipe.SettingChanged += (_, __) => PieceSettingsChanged?.Invoke();
            martialDisplayComfort.SettingChanged += (_, __) => PieceSettingsChanged?.Invoke();
        }

        /// <summary>"Wood:4, Resin:2" → item → amount. A bare name counts one; junk is skipped and logged.</summary>
        public static List<KeyValuePair<string, int>> ParseRecipe(string text, string what)
        {
            var result = new List<KeyValuePair<string, int>>();
            foreach (var raw in (text ?? "").Split(','))
            {
                string part = raw.Trim();
                if (part.Length == 0) continue;
                int colon = part.IndexOf(':');
                string item = (colon < 0 ? part : part.Substring(0, colon)).Trim();
                int amount = 1;
                if (colon >= 0 && (!int.TryParse(part.Substring(colon + 1).Trim(), out amount) || amount < 1))
                {
                    Jotunn.Logger.LogWarning($"[HeraldryExtras] {what} recipe: '{part}' isn't item:amount; skipped.");
                    continue;
                }
                if (item.Length > 0) result.Add(new KeyValuePair<string, int>(item, amount));
            }
            return result;
        }
    }
}
