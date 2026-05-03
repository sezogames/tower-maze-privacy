using UnityEngine;

namespace TowerMaze.WeatherSystem
{
    /// <summary>
    /// PlayerPrefs-backed gate for premium weather themes. Sunny is unlocked by
    /// default; everything else stays locked until the shop / progression grants
    /// it. Pure utility — no MonoBehaviour required.
    /// </summary>
    public static class WeatherUnlockSystem
    {
        private const string KeyPrefix = "TowerMaze.WeatherUnlock.";

        public static bool IsWeatherUnlocked(WeatherType weather)
        {
            if (weather == WeatherType.Sunny) return true;
            return PlayerPrefs.GetInt(GetPrefsKey(weather), 0) == 1;
        }

        public static void UnlockWeather(WeatherType weather)
        {
            if (weather == WeatherType.Sunny) return;
            PlayerPrefs.SetInt(GetPrefsKey(weather), 1);
            PlayerPrefs.Save();
        }

        public static void LockWeather(WeatherType weather)
        {
            if (weather == WeatherType.Sunny) return;
            PlayerPrefs.DeleteKey(GetPrefsKey(weather));
            PlayerPrefs.Save();
        }

        public static string GetPrefsKey(WeatherType weather) => KeyPrefix + weather;
    }
}
