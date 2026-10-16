using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ValheimHeraldryExtras.Pieces
{
    /// <summary>
    ///     The Martial Display: a wall-mounted round shield with crossed axes over a cloth (Hippo's model). Both the
    ///     shield face (ValheimHeraldryMod_Face_1) and the cloth (ValheimHeraldryMod_Face_2) are tagged faces, so
    ///     Valheim Heraldry Mod paints them through its tagged-piece path, two patterns on one piece. That makes this
    ///     the test of that path. Nothing here paints anything.
    /// </summary>
    internal static class MartialDisplay
    {
        public const string PrefabName = ValheimHeraldryExtras.Prefix + "MartialDisplay";
        private const string BundlePrefab = "MartialDisplay";
        private const string Source = "piece_banner01"; // a banner's Piece, WearNTear, ZNetView and effects

        /// <summary>
        ///     A comfort group of its own. Vanilla's groups are 0–11 (None … Leisure); the game only sorts and
        ///     compares groups as numbers, so any value works. Large and arbitrary so other mods' inventions won't
        ///     collide.
        /// </summary>
        public const Piece.ComfortGroup ComfortGroup = (Piece.ComfortGroup)7461;

        public static void Register()
        {
            try
            {
                Add();
            }
            catch (System.Exception ex)
            {
                Jotunn.Logger.LogError($"[HeraldryExtras] The Martial Display couldn't be registered: {ex}");
            }
        }

        private static void Add()
        {
            var model = Bundle.Prefab(BundlePrefab);
            if (model == null) return;

            ValheimHeraldryExtras.Localization.AddTranslation("English", new Dictionary<string, string>
            {
                { PrefabName, "Martial Display" },
                { PrefabName + "_description", "A shield and crossed axes over a cloth, for the wall. Paint your arms on shield and cloth." },
            });

            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, Source);
            if (prefab == null) throw new System.Exception("couldn't clone " + Source);

            // Out with the vanilla banner's looks and colliders, in with the model
            foreach (Transform child in prefab.transform.Cast<Transform>().ToList())
                Object.DestroyImmediate(child.gameObject);
            var visual = Object.Instantiate(model, prefab.transform, false);
            visual.name = "Model";
            visual.transform.localPosition = Vector3.zero;  // the model's own origin is the wall point
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            VanillaLooks.Apply(visual);

            // One box over the whole model: something to hover, hit and rest against the wall
            var bounds = LocalBounds(prefab.transform, visual);
            int pieceLayer = LayerMask.NameToLayer("piece");
            var colliderObject = new GameObject("collider") { layer = pieceLayer };
            colliderObject.transform.SetParent(prefab.transform, false);
            var box = colliderObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = new Vector3(Mathf.Max(bounds.size.x, 0.05f), Mathf.Max(bounds.size.y, 0.05f), Mathf.Max(bounds.size.z, 0.05f));

            var wnt = prefab.GetComponent<WearNTear>();
            if (wnt != null)
            {
                wnt.m_new = visual;
                wnt.m_worn = visual;
                wnt.m_broken = visual;
                wnt.m_fragmentRoots = new GameObject[0];
            }

            var piece = prefab.GetComponent<Piece>();
            piece.m_name = "$" + PrefabName;
            piece.m_description = "$" + PrefabName + "_description";
            piece.m_notOnFloor = true;              // wall only
            piece.m_comfort = ExtrasConfig.MartialDisplayComfort;
            piece.m_comfortGroup = ComfortGroup;
            piece.m_comfortObject = null;

            var config = new PieceConfig
            {
                Name = piece.m_name,
                Description = piece.m_description,
                PieceTable = PieceTables.Hammer,
                Category = ValheimHeraldryExtras.HammerCategory,
                CraftingStation = CraftingStations.Workbench,
                Requirements = ExtrasConfig.ParseRecipe(ExtrasConfig.MartialDisplayRecipe, "Martial Display")
                    .Select(kv => new RequirementConfig(kv.Key, kv.Value, 0, true)).ToArray(),
            };
            var icon = Icon(prefab);
            if (icon != null) config.Icon = icon;
            PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, config));

            ExtrasConfig.PieceSettingsChanged += ApplySettings;
            SynchronizationManager.OnConfigurationSynchronized += (_, __) => ApplySettings();

            Jotunn.Logger.LogInfo($"[HeraldryExtras] Registered the Martial Display ({bounds.size.x:0.00} x {bounds.size.y:0.00} x {bounds.size.z:0.00} m, " +
                                  $"centre {bounds.center}); faces: {string.Join(", ", visual.GetComponentsInChildren<Renderer>(true).Select(r => r.name))}.");
        }

        /// <summary>Recipe and comfort from the config (server values, admin changes). Placed pieces keep their comfort until reloaded.</summary>
        private static void ApplySettings()
        {
            var piece = PieceManager.Instance.GetPiece(PrefabName)?.Piece;
            if (piece == null) return;
            piece.m_comfort = ExtrasConfig.MartialDisplayComfort;
            if (ObjectDB.instance == null) return;
            var resources = new List<Piece.Requirement>();
            foreach (var kv in ExtrasConfig.ParseRecipe(ExtrasConfig.MartialDisplayRecipe, "Martial Display"))
            {
                var item = ObjectDB.instance.GetItemPrefab(kv.Key)?.GetComponent<ItemDrop>();
                if (item == null)
                {
                    Jotunn.Logger.LogWarning($"[HeraldryExtras] Martial Display recipe: no item '{kv.Key}'; left out.");
                    continue;
                }
                resources.Add(new Piece.Requirement { m_resItem = item, m_amount = kv.Value, m_recover = true });
            }
            if (resources.Count > 0) piece.m_resources = resources.ToArray(); // never free by accident
        }

        /// <summary>The model's mesh bounds in the prefab's own space (renderers' bounds are empty while inactive).</summary>
        private static Bounds LocalBounds(Transform root, GameObject visual)
        {
            var toRoot = root.worldToLocalMatrix;
            Bounds b = default;
            bool first = true;
            foreach (var f in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null) continue;
                var mb = f.sharedMesh.bounds;
                var m = toRoot * f.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        private static Sprite Icon(GameObject prefab)
        {
            try
            {
                return RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab)
                {
                    Rotation = Quaternion.Euler(0f, 180f, 0f),
                    Width = 128,
                    Height = 128,
                });
            }
            catch (System.Exception ex)
            {
                Jotunn.Logger.LogWarning($"[HeraldryExtras] Couldn't render the Martial Display's icon ({ex.Message}).");
                return null;
            }
        }
    }
}
