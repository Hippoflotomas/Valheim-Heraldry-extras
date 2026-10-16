using System.IO;
using UnityEngine;

namespace ValheimHeraldryExtras
{
    /// <summary>
    ///     The mod's own asset bundle, embedded in the DLL (built in ValheimHeraldryExtrasUnity with
    ///     Assets → Build Heraldry Extras Bundle). Loaded once; it only holds meshes, materials and empties.
    /// </summary>
    internal static class Bundle
    {
        private const string Resource = "ValheimHeraldryExtras.heraldryextras";
        private static AssetBundle bundle;
        private static bool tried;

        public static AssetBundle Get()
        {
            if (tried) return bundle;
            tried = true;
            var asm = typeof(Bundle).Assembly;
            using (var stream = asm.GetManifestResourceStream(Resource))
            {
                if (stream == null)
                {
                    Jotunn.Logger.LogError($"[HeraldryExtras] Embedded bundle '{Resource}' missing; build it in Unity and rebuild the mod.");
                    return null;
                }
                var data = new byte[stream.Length];
                int read = 0;
                while (read < data.Length)
                {
                    int n = stream.Read(data, read, data.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
                // LoadFromMemory rather than Jötunn's LoadAssetBundleFromResources (that one closes its stream early)
                bundle = AssetBundle.LoadFromMemory(data);
            }
            if (bundle == null) Jotunn.Logger.LogError("[HeraldryExtras] The embedded bundle couldn't be loaded.");
            else Jotunn.Logger.LogDebug($"[HeraldryExtras] Bundle loaded: {string.Join(", ", bundle.GetAllAssetNames())}");
            return bundle;
        }

        public static GameObject Prefab(string name)
        {
            var b = Get();
            var p = b != null ? b.LoadAsset<GameObject>(name) : null;
            if (b != null && p == null) Jotunn.Logger.LogError($"[HeraldryExtras] No prefab '{name}' in the bundle.");
            return p;
        }
    }
}
