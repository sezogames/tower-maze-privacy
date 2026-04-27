using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerMaze
{
    public class ChapterCompleteScreenController : MonoBehaviour, IPointerClickHandler
    {
        private const float TapCooldownSeconds = 0.35f;

        private Text eyebrowText;
        private Text titleText;
        private Text subtitleText;
        private Text statsLabelText;
        private Text statsText;
        private Text rewardLabelText;
        private Text rewardText;
        private Text autoReturnText;
        private Image rewardPanel;
        private Action pendingOnMenu;
        private float armedAtRealtime;

        public void Initialize(Font font, ThemeDefinition theme)
        {
            Image bg = UIManager.CreateImage("ChapterCompleteBg", transform, UIStyle.MenuBg);
            UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.raycastTarget = true;

            Image dimmer = UIManager.CreateImage("Dimmer", bg.transform, new Color(0.05f, 0.02f, 0.10f, 0.42f));
            UIManager.Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dimmer.raycastTarget = false;

            Image panel = UIManager.CreateCard("ResultPanel", bg.transform, new Color(0.20f, 0.10f, 0.32f, 0.92f), new Color(1f, 0.84f, 0.48f, 0.18f));
            UICandySkin.ApplyCandyButton(panel, "logo_jelly_panel", new Vector4(220f, 190f, 220f, 190f), 220f);
            panel.rectTransform.anchorMin = new Vector2(0.08f, 0.16f);
            panel.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;

            Image panelGlow = UIManager.CreateImage("PanelGlow", panel.transform, new Color(1f, 0.74f, 0.24f, 0.16f));
            UIManager.Stretch(panelGlow.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -18f));
            panelGlow.raycastTarget = false;

            eyebrowText = UIManager.CreateText("Eyebrow", panel.transform, font, 14, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.64f, 0.92f), UIFontRole.Popup);
            eyebrowText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(eyebrowText.rectTransform, new Vector2(0.12f, 0.82f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(eyebrowText, 12, 14, UIFontRole.Popup);

            titleText = UIManager.CreateText("Title", panel.transform, font, 52, TextAnchor.MiddleCenter, UIStyle.Gold, UIFontRole.Popup);
            UIManager.Stretch(titleText.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(titleText, 24, 52, UIFontRole.Popup);
            titleText.fontStyle = FontStyle.Bold;

            subtitleText = UIManager.CreateText("Subtitle", panel.transform, font, 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.82f), UIFontRole.Popup);
            subtitleText.fontStyle = FontStyle.Bold;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIManager.Stretch(subtitleText.rectTransform, new Vector2(0.10f, 0.56f), new Vector2(0.90f, 0.65f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(subtitleText, 14, 18, UIFontRole.Popup);

            Image statsPanel = UIManager.CreateCard("StatsPanel", panel.transform, new Color(0.11f, 0.06f, 0.18f, 0.74f), new Color(1f, 1f, 1f, 0.08f));
            statsPanel.rectTransform.anchorMin = new Vector2(0.10f, 0.35f);
            statsPanel.rectTransform.anchorMax = new Vector2(0.90f, 0.52f);
            statsPanel.rectTransform.offsetMin = statsPanel.rectTransform.offsetMax = Vector2.zero;

            statsLabelText = UIManager.CreateText("StatsLabel", statsPanel.transform, font, 14, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.62f), UIFontRole.Popup);
            statsLabelText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(statsLabelText.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(statsLabelText, 12, 14, UIFontRole.Popup);

            statsText = UIManager.CreateText("Stats", statsPanel.transform, font, 34, TextAnchor.MiddleCenter, UIStyle.TextPrimary, UIFontRole.Popup);
            UIManager.Stretch(statsText.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.64f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(statsText, 18, 34, UIFontRole.Popup);
            statsText.fontStyle = FontStyle.Bold;

            rewardPanel = UIManager.CreateCard("RewardPanel", panel.transform, new Color(0.30f, 0.16f, 0.04f, 0.92f), new Color(1f, 0.82f, 0.38f, 0.22f));
            UICandySkin.ApplyCandyButton(rewardPanel, "button_yellow", new Vector4(120f, 120f, 120f, 120f), 100f);
            rewardPanel.rectTransform.anchorMin = new Vector2(0.10f, 0.17f);
            rewardPanel.rectTransform.anchorMax = new Vector2(0.90f, 0.31f);
            rewardPanel.rectTransform.offsetMin = rewardPanel.rectTransform.offsetMax = Vector2.zero;

            rewardLabelText = UIManager.CreateText("RewardLabel", rewardPanel.transform, font, 14, TextAnchor.MiddleCenter, new Color(0.40f, 0.18f, 0.02f, 0.88f), UIFontRole.Popup);
            rewardLabelText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(rewardLabelText.rectTransform, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(rewardLabelText, 12, 14, UIFontRole.Popup);

            rewardText = UIManager.CreateText("Reward", rewardPanel.transform, font, 32, TextAnchor.MiddleCenter, new Color(0.28f, 0.12f, 0.02f, 1f), UIFontRole.Popup);
            UIManager.Stretch(rewardText.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(rewardText, 18, 32, UIFontRole.Popup);
            rewardText.fontStyle = FontStyle.Bold;

            autoReturnText = UIManager.CreateText("AutoReturn", panel.transform, font, 13, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.62f), UIFontRole.Popup);
            autoReturnText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(autoReturnText.rectTransform, new Vector2(0.10f, 0.07f), new Vector2(0.90f, 0.13f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(autoReturnText, 11, 13, UIFontRole.Popup);

            ApplyLocalizedBaseTexts();
            UIManager.ApplyPopupTextRoles(transform);
        }

        public void SetState(int chapterIndex, float reachedHeight, float targetHeight,
            int coinsRewarded, bool nextUnlocked, bool isLastChapter,
            Action onMenu, Action onNextChapter, Action onChapterSelect)
        {
            pendingOnMenu = onMenu;

            if (eyebrowText != null)
            {
                eyebrowText.text = UILanguage.Translate("BOLUM TEMIZLENDI", "STAGE CLEARED", "NIVEL SUPERADO");
            }

            titleText.text = string.Format(UILanguage.Translate("BOLUM {0} TAMAMLANDI!", "LEVEL {0} COMPLETE!", "NIVEL {0} COMPLETADO!"), chapterIndex);
            if (subtitleText != null)
            {
                subtitleText.text = UILanguage.Translate("Tirmanisi kapattin, odul hazir.", "You sealed the climb and banked the reward.", "Cerraste la subida y aseguraste la recompensa.");
            }

            statsText.text = $"{reachedHeight:0}m / {targetHeight:0}m";
            rewardText.text = $"+{coinsRewarded} COIN";

            if (rewardPanel != null)
            {
                rewardPanel.color = new Color(1f, 0.84f, 0.34f, 0.96f);
            }

            ArmTapDismiss();
        }

        public void SetFailState(int chapterIndex, float reachedHeight, float targetHeight, int coinsRewarded, Action onReturn)
        {
            pendingOnMenu = onReturn;

            if (eyebrowText != null)
            {
                eyebrowText.text = UILanguage.Translate("BOLUM OZETI", "RUN SUMMARY", "RESUMEN DE LA PARTIDA");
            }

            titleText.text = string.Format(UILanguage.Translate("BOLUM {0}", "LEVEL {0}", "NIVEL {0}"), chapterIndex);
            if (subtitleText != null)
            {
                subtitleText.text = UILanguage.Translate("Bu tur burada bitti ama topladigin coinler sayildi.", "This climb ended here, but your collected coins still count.", "La subida termino aqui, pero las monedas recogidas si cuentan.");
            }

            statsText.text = $"{reachedHeight:0}m / {targetHeight:0}m";
            rewardText.text = $"+{Mathf.Max(0, coinsRewarded)} COIN";

            if (rewardPanel != null)
            {
                rewardPanel.color = new Color(0.98f, 0.68f, 0.26f, 0.96f);
            }

            ArmTapDismiss();
        }

        private void ApplyLocalizedBaseTexts()
        {
            if (statsLabelText != null)
            {
                statsLabelText.text = UILanguage.Translate("ILERLEME", "PROGRESS", "PROGRESO");
            }

            if (rewardLabelText != null)
            {
                rewardLabelText.text = UILanguage.Translate("KAZANILAN COIN", "COINS EARNED", "MONEDAS GANADAS");
            }

            if (autoReturnText != null)
            {
                autoReturnText.text = UILanguage.Translate("Devam etmek icin ekrana dokun", "Tap anywhere to continue", "Toca la pantalla para continuar");
            }
        }

        private void ArmTapDismiss()
        {
            armedAtRealtime = Time.realtimeSinceStartup + TapCooldownSeconds;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.realtimeSinceStartup < armedAtRealtime)
            {
                return;
            }

            Action callback = pendingOnMenu;
            pendingOnMenu = null;
            callback?.Invoke();
        }
    }
}
