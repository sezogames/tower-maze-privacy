using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public static class UICandySkin
    {
        private static readonly Dictionary<string, Sprite> SlicedSpriteCache = new();
        private static readonly Dictionary<string, Sprite> SimpleSpriteCache = new();

        public static void ApplyCandyPanel(Image image)
        {
            ApplyCandyButton(image, "logo_jelly_panel", new Vector4(220f, 190f, 220f, 190f), 220f);
        }

        public static void ApplyCandyRow(Image image)
        {
            ApplyCandyButton(image, "logo_jelly_row", new Vector4(170f, 150f, 170f, 150f), 220f);
        }

        public static void ApplyCandyButton(Image image, string textureName, Vector4 slice, float pixelsPerUnit = 100f)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = LoadSlicedSprite(textureName, slice, pixelsPerUnit);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnit / 100f;
            image.color = Color.white;
        }

        public static void ApplyCandyOrb(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSprite("logo_jelly_orb", 100f);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        public static Sprite GetSprite(string textureName, float pixelsPerUnit = 100f)
        {
            return LoadSimpleSprite(textureName, pixelsPerUnit);
        }

        private static Sprite LoadSlicedSprite(string textureName, Vector4 slice, float pixelsPerUnit)
        {
            string key = $"{textureName}:{slice.x}:{slice.y}:{slice.z}:{slice.w}:{pixelsPerUnit}";
            if (SlicedSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>($"TowerMaze/UITheme/{textureName}");
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                slice);
            SlicedSpriteCache[key] = sprite;
            return sprite;
        }

        private static Sprite LoadSimpleSprite(string textureName, float pixelsPerUnit)
        {
            string key = $"{textureName}:{pixelsPerUnit}";
            if (SimpleSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>($"TowerMaze/UITheme/{textureName}");
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            SimpleSpriteCache[key] = sprite;
            return sprite;
        }
    }
}
