using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze.WeatherSystem
{
    /// <summary>
    /// One row in the weather selection panel. Reads its lock state from
    /// WeatherUnlockSystem (PlayerPrefs), shows lock / select / selected status,
    /// and on click flips WeatherManager into manual mode pinned to this theme.
    /// </summary>
    public sealed class WeatherSelectButton : MonoBehaviour
    {
        [SerializeField] private WeatherType weatherType = WeatherType.Sunny;
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI labelText;

        public WeatherType WeatherType => weatherType;

        private void Awake()
        {
            if (weatherManager == null) weatherManager = FindAnyObjectByType<WeatherManager>();
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleClick);
                selectButton.onClick.AddListener(HandleClick);
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            bool unlocked = WeatherUnlockSystem.IsWeatherUnlocked(weatherType);
            bool isSelected = weatherManager != null && weatherManager.CurrentWeather == weatherType;

            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (selectButton != null) selectButton.interactable = unlocked && !isSelected;
            if (labelText != null) labelText.text = weatherType.ToString().ToUpperInvariant();
            if (statusText != null)
            {
                statusText.text = !unlocked ? "LOCKED" : (isSelected ? "SELECTED" : "SELECT");
                statusText.color = !unlocked
                    ? new Color(1f, 0.42f, 0.42f, 0.95f)
                    : (isSelected ? new Color(0.55f, 0.92f, 0.62f, 1f) : new Color(1f, 0.96f, 0.78f, 0.95f));
            }
        }

        private void HandleClick()
        {
            if (!WeatherUnlockSystem.IsWeatherUnlocked(weatherType)) return;
            if (weatherManager == null) return;
            weatherManager.SetManualWeatherSelection(true);
            weatherManager.SetWeather(weatherType);
            // Refresh siblings so SELECTED moves with the choice.
            WeatherSelectButton[] siblings = transform.parent != null
                ? transform.parent.GetComponentsInChildren<WeatherSelectButton>(includeInactive: true)
                : new[] { this };
            for (int i = 0; i < siblings.Length; i++) siblings[i].Refresh();
        }
    }
}
