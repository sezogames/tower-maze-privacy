using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerMaze
{
    internal static class BallSkinTextureLibrary
    {
        private static readonly Dictionary<string, Texture2D> TextureCache = new();

        public static Texture2D LoadTexture(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            if (TextureCache.TryGetValue(resourcePath, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                texture = LoadTextureFromFolder(resourcePath);
            }

            TextureCache[resourcePath] = texture;
            return texture;
        }

        private static Texture2D LoadTextureFromFolder(string resourcePath)
        {
            int separatorIndex = resourcePath.LastIndexOf('/');
            if (separatorIndex <= 0 || separatorIndex >= resourcePath.Length - 1)
            {
                return null;
            }

            string folderPath = resourcePath[..separatorIndex];
            string requestedName = resourcePath[(separatorIndex + 1)..];
            Texture2D[] textures = Resources.LoadAll<Texture2D>(folderPath);
            if (textures == null || textures.Length == 0)
            {
                return null;
            }

            Texture2D exactMatch = null;
            Texture2D colorMatch = null;
            for (int index = 0; index < textures.Length; index++)
            {
                Texture2D candidate = textures[index];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.name, requestedName, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatch = candidate;
                    break;
                }

                if (colorMatch == null &&
                    (candidate.name.Contains("_Color", StringComparison.OrdinalIgnoreCase) ||
                    candidate.name.Contains("Color", StringComparison.OrdinalIgnoreCase)))
                {
                    colorMatch = candidate;
                }
            }

            return exactMatch ?? colorMatch ?? textures[0];
        }
    }
}
