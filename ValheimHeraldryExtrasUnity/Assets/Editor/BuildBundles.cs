using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: Assets > Build Heraldry Extras Bundle
// Builds every asset bundle (set the bundle name in each asset's Inspector,
// bottom right) into <repo>/ValheimHeraldryExtras/AssetBundles for Windows.
public static class BuildBundles
{
    [MenuItem("Assets/Build Heraldry Extras Bundle")]
    public static void Build()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ValheimHeraldryExtras/AssetBundles"));
        Directory.CreateDirectory(outDir);
        BuildPipeline.BuildAssetBundles(outDir, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        Debug.Log("Asset bundles built to " + outDir);
        EditorUtility.RevealInFinder(outDir);
    }
}
