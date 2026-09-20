using UnityEditor;
using UnityEngine;

namespace EchoOfTheVoid.Editor
{
    public class PixelArtPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Contains("Assets/Art/Sprites/"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f; // Standard 16x16 from GDD
                importer.filterMode = FilterMode.Point; // Crisp pixel art
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
            }
        }
    }

    public static class AssetSetupUtility
    {
        [MenuItem("Tools/Echo of the Void/Setup Pixel Art Textures")]
        public static void ReimportAllPixelAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16f;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                    Debug.Log($"[PixelArtSetup] Configured: {path}");
                }
            }
            AssetDatabase.Refresh();
            Debug.Log("[PixelArtSetup] Finished configuring all pixel art assets.");
        }
    }
}
