using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public class ChapterSelectScreenController : MonoBehaviour
    {
        private Image[] _cellImages = new Image[50];
        private Text[] _labelTexts = new Text[50];
        private Text[] _heightTexts = new Text[50];
        private Button[] _buttons = new Button[50];
        private GameObject[] _lockOverlays = new GameObject[50];
        private Text[] _starTexts = new Text[50];

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
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            UIManager.Stretch(viewportRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            viewportGo.AddComponent<Image>().color = Color.clear;
            scrollRect.viewport = viewportRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            float cellWidth = (Screen.width - 60f) / 5f;
            grid.cellSize = new Vector2(cellWidth, cellWidth);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            scrollRect.content = contentRt;

            for (int i = 0; i < 50; i++)
            {
                int capturedIndex = i + 1;

                var cellGo = new GameObject($"Chapter_{capturedIndex}");
                cellGo.transform.SetParent(contentGo.transform, false);
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

                _cellImages[i] = cellImg;
                _labelTexts[i] = labelTxt;
                _heightTexts[i] = heightTxt;
                _buttons[i] = cellBtn;
                _lockOverlays[i] = lockGo;
                _starTexts[i] = starTxt;
            }

            Refresh(chapterManager);
        }

        public void Refresh(ChapterManager chapterManager)
        {
            for (int i = 0; i < 50; i++)
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
        }
    }
}
