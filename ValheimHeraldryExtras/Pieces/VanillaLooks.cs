using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace ValheimHeraldryExtras.Pieces
{
    /// <summary>
    ///     Vanilla materials, picked by name, to replace the bundle's placeholders. The model's material slots are
    ///     named in Blender: Wood, Iron (or Metal), Cloth.
    /// </summary>
    internal static class VanillaLooks
    {
        /// <summary>Unpainted cloth, the same linen colour the heraldry mod uses.</summary>
        private static readonly Color32 Linen = new Color32(214, 204, 178, 255);

        private static Dictionary<string, Material> cache;

        public static Material For(string placeholderName)
        {
            var all = All();
            string key = Clean(placeholderName);
            if (all.TryGetValue(key, out var m) && m != null) return m;
            Jotunn.Logger.LogWarning($"[HeraldryExtras] No vanilla look for material slot '{placeholderName}'; using wood.");
            return all["wood"];
        }

        /// <summary>Swaps every renderer's placeholder materials under <paramref name="root"/>.</summary>
        public static void Apply(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(m => For(m != null ? m.name : "")).ToArray();
        }

        private static string Clean(string name)
        {
            name = (name ?? "").Replace(" (Instance)", "").Trim();
            int dot = name.LastIndexOf('.');  // Blender's .001 duplicates
            if (dot > 0 && name.Substring(dot + 1).All(char.IsDigit)) name = name.Substring(0, dot);
            return name.ToLowerInvariant();
        }

        private static Dictionary<string, Material> All()
        {
            if (cache != null && cache.Values.All(m => m != null)) return cache;
            var wood = From("woodwall", m => m.name.StartsWith("woodwall", StringComparison.OrdinalIgnoreCase));
            var metal = From("iron_wall_1x1", m => m.name.StartsWith("metalwall", StringComparison.OrdinalIgnoreCase)) ?? wood;
            var cloth = StillCloth(From("piece_banner01", m => m.HasProperty("_PushDistance") && !m.name.StartsWith("woodwall", StringComparison.OrdinalIgnoreCase)));
            cache = new Dictionary<string, Material>
            {
                { "wood", wood }, { "iron", metal }, { "metal", metal }, { "cloth", cloth ?? wood },
            };
            return cache;
        }

        private static Material From(string prefabName, Func<Material, bool> pick)
        {
            var p = PrefabManager.Instance.GetPrefab(prefabName);
            var mats = p != null
                ? p.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).ToList()
                : new List<Material>();
            // By name: a piece's first renderer is often its snow layer
            var found = mats.FirstOrDefault(pick) ?? mats.FirstOrDefault(m => m.name.IndexOf("snow", StringComparison.OrdinalIgnoreCase) < 0);
            Jotunn.Logger.LogDebug($"[HeraldryExtras] Material from {prefabName}: {(found != null ? $"'{found.name}' ({found.shader.name})" : "none")}.");
            return found;
        }

        /// <summary>
        ///     The vanilla banner cloth, but still: a cloth pinned to a wall behind a shield mustn't flap (it would poke
        ///     through the shield). Vanilla emblem stripped; plain linen until painted.
        /// </summary>
        private static Material StillCloth(Material vanilla)
        {
            if (vanilla == null)
            {
                Jotunn.Logger.LogWarning("[HeraldryExtras] No vanilla banner cloth found; cloth will look like wood.");
                return null;
            }
            var m = new Material(vanilla) { name = "ValheimHeraldryExtras_Cloth" };
            foreach (var p in new[] { "_RippleDistance", "_PushDistance", "_PushClothMode", "_SwayDistance" })
                if (m.HasProperty(p)) m.SetFloat(p, 0f);
            if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", null);   // the emblem's relief lives in the normal map
            if (m.HasProperty("_MetalTex")) m.SetTexture("_MetalTex", null);
            m.mainTextureScale = Vector2.one;
            m.mainTextureOffset = Vector2.zero;
            if (m.HasProperty("_Color")) m.color = Color.white;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var linen = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "ValheimHeraldryExtras_Linen" };
                linen.SetPixels32(Enumerable.Repeat(Linen, 16).ToArray());
                linen.Apply(false, true);
                m.mainTexture = linen;
            }
            return m;
        }
    }
}
