using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerMaze
{
    public static class ProfileAvatarLibrary
    {
        public const int StarterPoolSize = 4;
        private const string ResourcePath = "TowerMaze/Avatars";

        private static readonly string[] DefaultNames =
        {
            "Brave Explorer",
            "Ninja Runner",
            "Robot Climber",
            "Wizard Kid",
            "Astronaut",
            "Fire Hero",
            "Ice Hero",
            "Fox Mascot",
            "Cat Adventurer",
            "Knight",
            "Cyber Racer",
            "Golden Champion"
        };

        // Starter Pool: 0, 1, 2, 3 (Free to pick one, others cost 1500)
        // Coin Pool: 4, 5, 6, 7, 8
        // Premium IAP: 9, 10, 11

        // Reprice 2026-05: progressive coin ladder so each tier feels noticeably
        // more expensive than the last; premium IAP avatars dropped to casual
        // standard tiers so the top tier is reachable for normal players.
        private static readonly Dictionary<int, int> EmberPrices = new()
        {
            { 0, 1500 }, { 1, 1500 }, { 2, 1500 }, { 3, 1500 },
            { 4, 2500 }, { 5, 3500 }, { 6, 5000 }, { 7, 7500 }, { 8, 10000 }
        };

        private static readonly Dictionary<int, string> IAPProductIds = new()
        {
            { 9, "towermaze_avatar_premium_tier1" },
            { 10, "towermaze_avatar_premium_tier2" },
            { 11, "towermaze_avatar_premium_tier3" }
        };

        private static readonly Dictionary<int, string> IAPPrices = new()
        {
            { 9, "$1.99" },
            { 10, "$4.99" },
            { 11, "$9.99" }
        };

        public static List<Sprite> LoadSprites()
        {
            return Resources.LoadAll<Sprite>(ResourcePath)
                .Where(sprite => sprite != null)
                .OrderBy(sprite => sprite.name)
                .ToList();
        }

        public static List<AvatarData> BuildDefaultData(IReadOnlyList<Sprite> sprites)
        {
            List<AvatarData> data = new();
            if (sprites == null) return data;

            for (int index = 0; index < sprites.Count; index++)
            {
                string avatarName = index < DefaultNames.Length ? DefaultNames[index] : $"Avatar {index + 1}";
                bool isIAP = IAPProductIds.ContainsKey(index);
                bool isEmber = EmberPrices.ContainsKey(index);
                int price = GetAvatarPrice(index);
                string iapId = isIAP ? IAPProductIds[index] : "";
                string priceLabel = isIAP ? IAPPrices[index] : (isEmber ? $"COIN {price}" : "FREE");

                data.Add(new AvatarData(
                    index, 
                    avatarName, 
                    sprites[index], 
                    premium: isIAP, 
                    unlocked: !isIAP && !isEmber, 
                    price: price, 
                    iap: isIAP, 
                    productId: iapId, 
                    label: priceLabel));
            }

            return data;
        }

        public static bool IsPremiumAvatarIndex(int avatarIndex)
        {
            return IAPProductIds.ContainsKey(avatarIndex);
        }

        public static int GetAvatarPrice(int avatarIndex)
        {
            return EmberPrices.TryGetValue(avatarIndex, out int price) ? price : 0;
        }

        public static bool IsDefaultUnlocked(int avatarIndex)
        {
            // Initially, only the first 4 (starter pool) are "unlocked" for selection in Profile Setup.
            // But actually, we want the player to pick ONE. 
            // So we'll let the Manager handle the persistence.
            return avatarIndex < StarterPoolSize;
        }
    }
}
