using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace ValheimHeraldryExtras.Pieces
{
    /// <summary>
    ///     Gives the model's materials Valheim's shaders. Slots are matched by name (set in Blender):
    ///     <list type="bullet">
    ///         <item>Wood: the vanilla wood wall material with its vertex noise switched to pixel noise. The wood wall
    ///         pushes every vertex up to ~3 cm by a noise pattern based on world position (hand-hewn logs); on small
    ///         parts that moves a shield face out of its rim, differently wherever the piece stands.</item>
    ///         <item>Iron/Metal: the vanilla iron wall material as it is (its noise is pixel-only already).</item>
    ///         <item>Cloth: the vanilla banner cloth's shader (snow, rain, cutout), held still, with the look taken
    ///         from the model's own material: its colour (Blender's Base Color comes through the FBX), its texture
    ///         and its normal map (set in Unity on the extracted material).</item>
    ///     </list>
    ///     Unknown slots become wood, with a warning.
    /// </summary>
    internal static class VanillaLooks
    {
        private static Material wood, metal, vanillaCloth;
        private static readonly Dictionary<Material, Material> clothCache = new Dictionary<Material, Material>();

        /// <summary>Swaps every renderer's placeholder materials under <paramref name="root"/>.</summary>
        public static void Apply(GameObject root)
        {
            Load();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(For).ToArray();
        }

        private static Material For(Material model)
        {
            switch (Clean(model != null ? model.name : ""))
            {
                case "wood": return wood;
                case "iron":
                case "metal": return metal;
                case "cloth": return Cloth(model);
                default:
                    Jotunn.Logger.LogWarning($"[HeraldryExtras] No vanilla look for material slot '{model?.name}'; using wood.");
                    return wood;
            }
        }

        private static string Clean(string name)
        {
            name = (name ?? "").Replace(" (Instance)", "").Trim();
            int dot = name.LastIndexOf('.');  // Blender's .001 duplicates
            if (dot > 0 && name.Substring(dot + 1).All(char.IsDigit)) name = name.Substring(0, dot);
            return name.ToLowerInvariant();
        }

        private static void Load()
        {
            if (wood != null && metal != null) return;
            var woodwall = From("woodwall", m => m.name.StartsWith("woodwall", StringComparison.OrdinalIgnoreCase));
            wood = woodwall != null ? new Material(woodwall) { name = "ValheimHeraldryExtras_Wood" } : null;
            if (wood != null && wood.HasProperty("_ValueNoiseVertex")) wood.SetFloat("_ValueNoiseVertex", 0f); // pixel noise: colour only, nothing moves
            metal = From("iron_wall_1x1", m => m.name.StartsWith("metalwall", StringComparison.OrdinalIgnoreCase)) ?? wood;
            vanillaCloth = From("piece_banner01", m => m.HasProperty("_PushDistance") && !m.name.StartsWith("woodwall", StringComparison.OrdinalIgnoreCase));
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
        ///     The vanilla banner cloth's shader with the model's look. Held still: a cloth pinned to a wall behind a
        ///     shield mustn't flap (it would poke through the shield). One material per model material.
        /// </summary>
        private static Material Cloth(Material model)
        {
            if (vanillaCloth == null)
            {
                Jotunn.Logger.LogWarning("[HeraldryExtras] No vanilla banner cloth found; cloth will look like wood.");
                return wood;
            }
            if (model != null && clothCache.TryGetValue(model, out var cached) && cached != null) return cached;

            var m = new Material(vanillaCloth) { name = "ValheimHeraldryExtras_" + (model != null ? model.name : "Cloth") };
            foreach (var p in new[] { "_RippleDistance", "_PushDistance", "_PushClothMode", "_SwayDistance" })
                if (m.HasProperty(p)) m.SetFloat(p, 0f);
            m.mainTextureScale = Vector2.one;
            m.mainTextureOffset = Vector2.zero;
            if (m.HasProperty("_MetalTex")) m.SetTexture("_MetalTex", null);

            // The model's look: texture and colour, normal map (none = flat, which also removes the vanilla emblem's relief)
            var texture = model != null && model.HasProperty("_MainTex") ? model.mainTexture : null;
            var colour = model != null && model.HasProperty("_Color") ? model.color : Color.white;
            colour.a = 1f; // the cloth shader cuts out below half alpha: never let a stray alpha make it invisible
            var normal = model != null && model.HasProperty("_BumpMap") ? model.GetTexture("_BumpMap") : null;
            if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", normal);

            if (texture != null)
            {
                m.mainTexture = texture;
                if (m.HasProperty("_Color")) m.color = colour;
            }
            else
            {
                // A flat colour as a texture: when the heraldry mod paints the face it swaps the texture and resets the
                // colour to white, so a tint would be lost anyway
                if (m.HasProperty("_Color")) m.color = Color.white;
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var plain = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = m.name + "_Colour" };
                    plain.SetPixels32(Enumerable.Repeat((Color32)colour, 16).ToArray());
                    plain.Apply(false, true);
                    m.mainTexture = plain;
                }
            }
            Jotunn.Logger.LogDebug($"[HeraldryExtras] Cloth '{model?.name}': colour {colour}, texture {(texture != null ? texture.name : "none")}, " +
                                  $"normal map {(normal != null ? normal.name : "none")}.");
            if (model != null) clothCache[model] = m;
            return m;
        }
    }
}
