using UnityEngine;

namespace TowerMaze.WeatherSystem
{
    /// <summary>
    /// Lightweight bridge that polls the live PlayerController + GameConfig to
    /// derive the current zone, then forwards changes to WeatherManager. Lets
    /// the weather system follow gameplay progression without modifying
    /// RunSystems. Disable / remove this component if you wire zone changes
    /// from a more direct source (e.g. an explicit OnZoneChanged event).
    /// </summary>
    public sealed class WeatherZoneBridge : MonoBehaviour
    {
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameConfig gameConfig;
        [Tooltip("Seconds between zone reads. Low frequency keeps the cost trivial; bumping it lower has no visible benefit because zones span tens of meters.")]
        [SerializeField] private float pollIntervalSeconds = 0.5f;

        private float nextPollTime;
        private int lastZone = -1;

        private void Awake()
        {
            if (weatherManager == null) weatherManager = FindAnyObjectByType<WeatherManager>();
            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
            if (gameConfig == null) gameConfig = Resources.Load<GameConfig>("TowerMaze/GameConfig");
        }

        private void Update()
        {
            if (weatherManager == null || playerController == null || gameConfig == null) return;
            if (Time.unscaledTime < nextPollTime) return;
            nextPollTime = Time.unscaledTime + pollIntervalSeconds;

            // Mirror RunSystems.GetCurrentZoneIndex but +1 so we report the
            // zone the player is currently inside (1-based for theme mapping).
            float zoneHeight = Mathf.Max(0.01f, gameConfig.ZoneHeight);
            int zone = Mathf.Max(1, Mathf.FloorToInt(playerController.HeightOnTower / zoneHeight) + 1);
            if (zone == lastZone) return;
            lastZone = zone;
            weatherManager.UpdateWeatherByZone(zone);
        }
    }
}
