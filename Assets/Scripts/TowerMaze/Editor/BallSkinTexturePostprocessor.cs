using System;
using UnityEditor;
using UnityEngine;

namespace TowerMaze.Editor
{
    public sealed class BallSkinTexturePostprocessor : AssetPostprocessor
    {
        private const string BallSkinRoot = "Assets/Resources/TowerMaze/BallSkins/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(BallSkinRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (assetImporter is not TextureImporter importer)
            {
                return;
            }

            string fileName = System.IO.Path.GetFileName(assetPath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 6;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            if (fileName.Contains("_NormalGL", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("_NormalDX", StringComparison.OrdinalIgnoreCase))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                return;
            }

            if (fileName.Contains("_Emission", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("_Roughness", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("_Metalness", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("_Displacement", StringComparison.OrdinalIgnoreCase))
            {
                importer.sRGBTexture = false;
            }
        }
    }
}
