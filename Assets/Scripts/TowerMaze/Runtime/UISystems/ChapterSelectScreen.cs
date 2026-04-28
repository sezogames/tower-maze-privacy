using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public class ChapterSelectScreenController : MonoBehaviour
    {
        private const int Total = ChapterManager.TotalChapters;
        private const int PerTier = ChapterManager.ChaptersPerTier;
        private const int TotalTiers = ChapterManager.TotalTiers;

        private Image[] _cellImages = new Image[Total];
        private Text[] _labelTexts = new Text[Total];
        private Text[] _heightTexts = new Text[Total];
        private Button[] _buttons = new Button[Total];
        private GameObject[] _lockOverlays = new GameObject[Total];
        private Text[] _starTexts = new Text[Total];
        private ScrollRect _scrollRect;

        public void Initialize(Font font, ThemeDefinition theme, ChapterManager chapterManager,
            Action<int> onChapterSelected, Action onBack)
        {
            var bgImg = UIManager.CreateImage("BG", transform, UIStyle.MenuBg);
            UIManager.Stretch(bgImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var titleTxt = UIManager.CreateText("Title", transform, font, 24, TextAnchor.MiddleCenter, UIStyle.TextPrimary, UIFontRole.Default);
            titleTxt.text = "BÖLÜMLER";
            titleTxt.fontStyle = FontStyle.Bold;
            UIManager.Stretch(titleTxt.rectTransform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f), Vector2.zero, Vector2.zero);

            var backGo = new GameObject("BackButton");
            backGo.transform.SetParent(transform, false);
            var backImg = backGo.AddComponent<Image>();
            backImg.color = UIStyle.Action;
            var backRt = backGo.GetComponent<RectTransform>();
            UIManager.Stretch(backRt, new Vector2(0f, 0.91f), new Vector2(0.18f, 0.99f), Vector2.zero, Vector2.zero);
            var backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            UIManager.BindButton(backBtn, onBack, null);
            var backTxt = UIManager.CreateText("BackLabel", backGo.transform, font, 20, TextAnchor.MiddleCenter, UIStyle.TextPrimary, UIFontRole.Button);
            backTxt.text = "←";
            UIManager.Stretch(backTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollGo = new GameObject("ChapterScroll");
            scrollGo.transform.SetParent(transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            UIManager.Stretch(scrollRt, new Vector2(0f, 0f), new Vector2(1f, 0.90f), Vector2.zero, Vector2.zero);
            _scrollRect = scrollGo.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 30f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            UIManager.Stretch(viewportRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            viewportGo.AddComponent<Image>().color = Color.clear;
            _scrollRect.viewport = viewportRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 14f;
            contentVlg.padding = new RectOffset(8, 8, 8, 16);
            var contentCsf = contentGo.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.content = contentRt;

            float cellWidth = (Screen.width - 60f) / 5f;

            for (int tier = 1; tier <= TotalTiers; tier++)
            {
                CreateTierHeader(contentGo.transform, font, tier);

                var tierGridGo = new GameObject($"TierGrid_{tier}");
                tierGridGo.transform.SetParent(contentGo.transform, false);
                tierGridGo.AddComponent<RectTransform>();
                var tierGrid = tierGridGo.AddComponent<GridLayoutGroup>();
                tierGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                tierGrid.constraintCount = 5;
                tierGrid.cellSize = new Vector2(cellWidth, cellWidth);
                tierGrid.spacing = new Vector2(6f, 6f);
                tierGrid.padding = new RectOffset(0, 0, 0, 0);
                var tierGridCsf = tierGridGo.AddComponent<ContentSizeFitter>();
                tierGridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                for (int k = 1; k <= PerTier; k++)
                {
                    int chapterIndex = (tier - 1) * PerTier + k;
                    int capturedIndex = chapterIndex;
                    int slot = chapterIndex - 1;

                    var cellGo = new GameObject($"Chapter_{chapterIndex}");
                    cellGo.transform.SetParent(tierGridGo.transform, false);
                    var cellImg = cellGo.AddComponent<Image>();
                    var cellBtn = cellGo.AddComponent<Button>();
                    cellBtn.targetGraphic = cellImg;
                    UIManager.BindButton(cellBtn, () => { onChapterSelected(capturedIndex); }, null);

                    var labelTxt = UIManager.CreateText("Label", cellGo.transform, font, 12, TextAnchor.UpperCenter, Color.white, UIFontRole.Button);
                    labelTxt.fontStyle = FontStyle.Bold;
                    UIManager.SetScaledBestFit(labelTxt, 8, 12, UIFontRole.Button);
                    labelTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.45f);
                    labelTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.85f);
                    labelTxt.rectTransform.offsetMin = labelTxt.rectTransform.offsetMax = Vector2.zero;

                    var heightTxt = UIManager.CreateText("Height", cellGo.transform, font, 9, TextAnchor.LowerCenter, UIStyle.TextDim);
                    heightTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.10f);
                    heightTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.45f);
                    heightTxt.rectTransform.offsetMin = heightTxt.rectTransform.offsetMax = Vector2.zero;

                    var lockGo = new GameObject("Lock");
                    lockGo.transform.SetParent(cellGo.transform, false);
                    var lockImg = lockGo.AddComponent<Image>();
                    lockImg.color = new Color(0f, 0f, 0f, 0.6f);
                    UIManager.Stretch(lockImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    var lockTxt = UIManager.CreateText("LockIcon", lockGo.transform, font, 16, TextAnchor.MiddleCenter, Color.white);
                    lockTxt.text = "🔒";
                    UIManager.Stretch(lockTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                    var starTxt = UIManager.CreateText("Star", cellGo.transform, font, 12, TextAnchor.UpperRight, UIStyle.Gold);
                    starTxt.text = "★";
                    starTxt.rectTransform.anchorMin = new Vector2(0.6f, 0.65f);
                    starTxt.rectTransform.anchorMax = new Vector2(0.98f, 0.98f);
                    starTxt.rectTransform.offsetMin = starTxt.rectTransform.offsetMax = Vector2.zero;

                    _cellImages[slot] = cellImg;
                    _labelTexts[slot] = labelTxt;
                    _heightTexts[slot] = heightTxt;
                    _buttons[slot] = cellBtn;
                    _lockOverlays[slot] = lockGo;
                    _starTexts[slot] = starTxt;
                }
            }

            Refresh(chapterManager);
        }

        private static void CreateTierHeader(Transform parent, Font font, int tier)
        {
            var headerGo = new GameObject($"TierHeader_{tier}");
            headerGo.transform.SetParent(parent, false);
            var headerLe = headerGo.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 56f;
            var headerImg = headerGo.AddComponent<Image>();
            Color tierColor = Color.HSVToRGB(Mathf.Repeat(tier / 10f, 1f), 0.55f, 0.32f);
            tierColor.a = 0.85f;
            headerImg.color = tierColor;

            var labelTxt = UIManager.CreateText("Label", headerGo.transform, font, 22, TextAnchor.MiddleCenter, UIStyle.Gold, UIFontRole.Default);
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.text = $"TIER {tier} — {GetTierName(tier)}";
            UIManager.Stretch(labelTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIManager.SetScaledBestFit(labelTxt, 14, 22, UIFontRole.Default);
        }

        private static string GetTierName(int tier)
        {
            string[] tr = { "ACEMI", "OGRENCI", "YETENEKLI", "USTA", "SAMPIYON",
                            "DEHA", "LORD", "EFSANE", "TANRI", "OLUMSUZ" };
            string[] en = { "ROOKIE", "STUDENT", "ADEPT", "MASTER", "CHAMPION",
                            "GENIUS", "LORD", "LEGEND", "GOD", "IMMORTAL" };
            string[] es = { "NOVATO", "ESTUDIANTE", "EXPERTO", "MAESTRO", "CAMPEON",
                            "GENIO", "SENOR", "LEYENDA", "DIOS", "INMORTAL" };
            int i = Mathf.Clamp(tier - 1, 0, 9);
            return UILanguage.Translate(tr[i], en[i], es[i]);
        }

        public void Refresh(ChapterManager chapterManager)
        {
            for (int i = 0; i < Total; i++)
            {
                int chapterIndex = i + 1;
                var chapter = chapterManager.GetChapter(chapterIndex);

                _labelTexts[i].text = chapter.DisplayName;
                _heightTexts[i].text = $"{chapter.TargetHeight:0}m";

                bool locked = !chapterManager.IsUnlocked(chapterIndex);
                bool completed = chapterManager.IsCompleted(chapterIndex);

                if (locked)
                {
                    _cellImages[i].color = UIStyle.SurfaceDark;
                    _lockOverlays[i].SetActive(true);
                    _starTexts[i].gameObject.SetActive(false);
                    _buttons[i].interactable = false;
                }
                else if (completed)
                {
                    _cellImages[i].color = UIStyle.Owned;
                    _lockOverlays[i].SetActive(false);
                    _starTexts[i].gameObject.SetActive(true);
                    _buttons[i].interactable = true;
                }
                else
                {
                    _cellImages[i].color = UIStyle.Action;
                    _lockOverlays[i].SetActive(false);
                    _starTexts[i].gameObject.SetActive(false);
                    _buttons[i].interactable = true;
                }
            }

            ScrollToChapter(chapterManager.UnlockedUpTo);
        }

        private void ScrollToChapter(int chapterIndex)
        {
            if (_scrollRect == null) return;
            int tier = (Mathf.Clamp(chapterIndex, 1, Total) - 1) / PerTier;
            float normalizedY = 1f - (tier / (float)TotalTiers);
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedY);
        }
    }
}
