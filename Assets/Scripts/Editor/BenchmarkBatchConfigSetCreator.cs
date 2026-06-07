using Benchmark.Data;
using UnityEditor;
using UnityEngine;

namespace Benchmark.EditorTools
{
    public static class BenchmarkBatchConfigSetCreator
    {
        private const string DefaultFolderPath = "Assets/Benchmark/Configs";
        private const string DefaultAssetPath =
            DefaultFolderPath + "/RecommendedBenchmarkBatchConfigSet.asset";

        [MenuItem("Tools/Benchmark/Create Recommended Batch Config Set")]
        public static void CreateRecommendedBatchConfigSet()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Benchmark"))
            {
                AssetDatabase.CreateFolder("Assets", "Benchmark");
            }

            if (!AssetDatabase.IsValidFolder(DefaultFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/Benchmark", "Configs");
            }

            BenchmarkBatchConfigSet asset =
                ScriptableObject.CreateInstance<BenchmarkBatchConfigSet>();

            asset.FillRecommendedMobileResearchConfigs();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultAssetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log("Recommended benchmark batch config set created: " + assetPath);
        }
    }
}