namespace TowerMaze.WeatherSystem
{
    /// <summary>
    /// Premium weather themes that wrap the tower with a backdrop, particle layer,
    /// lighting, fog, and (optionally) a block surface override.
    /// Order is significant: zone-based selection in WeatherManager indexes against
    /// these values, and PlayerPrefs keys in WeatherUnlockSystem use the enum name.
    /// </summary>
    public enum WeatherType
    {
        Sunny = 0,
        Snow = 1,
        Rain = 2,
        Fog = 3,
        StarryNight = 4,
    }
}
