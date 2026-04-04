using System;
using UnityEngine;

namespace TowerMaze
{
    internal static class UILanguage
    {
        internal const string DefaultCode = "EN";
        internal static readonly string[] SupportedCodes = { "EN", "TR", "ES" };

        internal static void EnsureDefaultLanguage()
        {
            if (PlayerPrefs.HasKey("Language"))
            {
                return;
            }

            PlayerPrefs.SetString("Language", DefaultCode);
            PlayerPrefs.Save();
        }

        internal static string GetLanguageCode()
        {
            string code = PlayerPrefs.GetString("Language", DefaultCode);
            if (string.IsNullOrWhiteSpace(code))
            {
                return DefaultCode;
            }

            code = code.Trim().ToUpperInvariant();
            for (int index = 0; index < SupportedCodes.Length; index++)
            {
                if (string.Equals(SupportedCodes[index], code, StringComparison.Ordinal))
                {
                    return code;
                }
            }

            return DefaultCode;
        }

        internal static void SetLanguageCode(string code)
        {
            string normalizedCode = NormalizeCode(code);
            PlayerPrefs.SetString("Language", normalizedCode);
            PlayerPrefs.Save();
        }

        internal static string Translate(string tr, string en, string es)
        {
            return GetLanguageCode() switch
            {
                "TR" => tr,
                "ES" => es,
                _ => en,
            };
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return DefaultCode;
            }

            code = code.Trim().ToUpperInvariant();
            for (int index = 0; index < SupportedCodes.Length; index++)
            {
                if (string.Equals(SupportedCodes[index], code, StringComparison.Ordinal))
                {
                    return code;
                }
            }

            return DefaultCode;
        }
    }
}
