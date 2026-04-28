using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerMaze
{
    public class TierCelebrationScreenController : MonoBehaviour, IPointerClickHandler
    {
        private const float TapCooldownSeconds = 0.35f;

        private Text eyebrowText;
        private Text titleText;
        private Text subtitleText;
        private Text rewardLabelText;
        private Text rewardText;
        private Text autoReturnText;
        private Image rewardPanel;
        private Image badgeImage;
        private Action pendingOnContinue;
        private float armedAtRealtime;

        public void Initialize(Font font, ThemeDefinition theme)
        {
            Image bg = UIManager.CreateImage("TierCelebrationBg", transform, UIStyle.MenuBg);
            UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.raycastTarget = true;

            Image dimmer = UIManager.CreateImage("Dimmer", bg.transform, new Color(0.05f, 0.02f, 0.10f, 0.42f));
            UIManager.Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dimmer.raycastTarget = false;

            Image panel = UIManager.CreateCard("TierPanel", bg.transform, new Color(0.20f, 0.10f, 0.32f, 0.92f), new Color(1f, 0.84f, 0.48f, 0.18f));
            UICandySkin.ApplyCandyButton(panel, "logo_jelly_panel", new Vector4(220f, 190f, 220f, 190f), 220f);
            panel.rectTransform.anchorMin = new Vector2(0.08f, 0.16f);
            panel.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;

            Image panelGlow = UIManager.CreateImage("PanelGlow", panel.transform, new Color(1f, 0.74f, 0.24f, 0.22f));
            UIManager.Stretch(panelGlow.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -18f));
            panelGlow.raycastTarget = false;

            eyebrowText = UIManager.CreateText("Eyebrow", panel.transform, font, 14, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.64f, 0.92f), UIFontRole.Popup);
            eyebrowText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(eyebrowText.rectTransform, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.90f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(eyebrowText, 12, 14, UIFontRole.Popup);

            titleText = UIManager.CreateText("Title", panel.transform, font, 56, TextAnchor.MiddleCenter, UIStyle.Gold, UIFontRole.Popup);
            UIManager.Stretch(titleText.rectTransform, new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(titleText, 26, 56, UIFontRole.Popup);
            titleText.fontStyle = FontStyle.Bold;

            badgeImage = UIManager.CreateImage("TierBadge", panel.transform, new Color(1f, 1f, 1f, 0f));
            UIManager.Stretch(badgeImage.rectTransform, new Vector2(0.34f, 0.40f), new Vector2(0.66f, 0.62f), Vector2.zero, Vector2.zero);
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            subtitleText = UIManager.CreateText("Subtitle", panel.transform, font, 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.86f), UIFontRole.Popup);
            subtitleText.fontStyle = FontStyle.Bold;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIManager.Stretch(subtitleText.rectTransform, new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.39f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(subtitleText, 14, 18, UIFontRole.Popup);

            rewardPanel = UIManager.CreateCard("RewardPanel", panel.transform, new Color(0.30f, 0.16f, 0.04f, 0.92f), new Color(1f, 0.82f, 0.38f, 0.22f));
            UICandySkin.ApplyCandyButton(rewardPanel, "button_yellow", new Vector4(120f, 120f, 120f, 120f), 100f);
            rewardPanel.rectTransform.anchorMin = new Vector2(0.16f, 0.14f);
            rewardPanel.rectTransform.anchorMax = new Vector2(0.84f, 0.27f);
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
            UIManager.Stretch(autoReturnText.rectTransform, new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.12f), Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(autoReturnText, 11, 13, UIFontRole.Popup);

            ApplyLocalizedBaseTexts();
            UIManager.ApplyPopupTextRoles(transform);
        }

        public void SetState(int tierIndex, int bonusEmber, bool isLastChapter, Action onContinue)
        {
            pendingOnContinue = onContinue;

            if (eyebrowText != null)
            {
                eyebrowText.text = UILanguage.Translate("TIER TAMAMLANDI", "TIER CLEARED", "NIVEL DE TIER COMPLETADO");
            }

            string template = UILanguage.Translate("TIER {0} USTASI!", "TIER {0} MASTER!", "MAESTRO DE TIER {0}!");
            titleText.text = string.Format(template, tierIndex);

            if (subtitleText != null)
            {
                subtitleText.text = isLastChapter
                    ? UILanguage.Translate("Tum bolumler tamamlandi!", "All chapters completed!", "Todos los niveles completados!")
                    : UILanguage.Translate("Bir sonraki tier acildi.", "Next tier unlocked.", "Siguiente tier desbloqueado.");
            }

            rewardText.text = $"+{bonusEmber} EMBER";

            if (badgeImage != null)
            {
                Sprite badge = Resources.Load<Sprite>($"TowerMaze/UITheme/tier_badges/tier_{tierIndex}");
                if (badge != null)
                {
                    badgeImage.sprite = badge;
                    badgeImage.color = Color.white;
                }
                else
                {
                    badgeImage.sprite = null;
                    Color tierColor = Color.HSVToRGB(Mathf.Repeat(tierIndex / 10f, 1f), 0.78f, 0.95f);
                    tierColor.a = 0.55f;
                    badgeImage.color = tierColor;
                }
            }

            ArmTapDismiss();
        }

        private void ApplyLocalizedBaseTexts()
        {
            if (rewardLabelText != null)
            {
                rewardLabelText.text = UILanguage.Translate("BONUS EMBER", "BONUS EMBER", "BONO EMBER");
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
            if (Time.realtimeSinceStartup < armedAtRealtime) return;
            Action callback = pendingOnContinue;
            pendingOnContinue = null;
            callback?.Invoke();
        }
    }
}
