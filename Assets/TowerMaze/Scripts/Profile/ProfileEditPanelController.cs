using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class ProfileEditPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerProfileManager profileManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private TextMeshProUGUI currentNameText;
        [SerializeField] private Image currentAvatarImage;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Transform avatarGridParent;
        [SerializeField] private AvatarSelectionButton avatarButtonPrefab;
        [SerializeField] private Button changeAvatarButton;
        [SerializeField] private Button changeNameButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private List<Sprite> avatarSprites = new();
        [SerializeField] private int selectedAvatarIndex;

        private readonly List<AvatarSelectionButton> avatarButtons = new();
        private Action onClosed;
        private bool layoutBuilt;

        // Reserve CanvasGroup at Awake so any external script (UIManager wiring,
        // profile button overlays) can access it before BuildLayout runs. Same
        // fix as ProfileSetupPopupController — both panels are added via
        // AddComponent on bare GameObjects, so the layout-time fallback at line
        // 156 isn't soon enough.
        private void Awake()
        {
            if (GetComponent<CanvasGroup>() == null)
            {
                gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Initialize(PlayerProfileManager manager, Action closedCallback = null)
        {
            Initialize(manager, economyManager, closedCallback);
        }

        public void Initialize(PlayerProfileManager manager, EconomyManager economy, Action closedCallback = null)
        {
            profileManager = manager != null ? manager : profileManager;
            if (profileManager == null)
            {
                profileManager = FindAnyObjectByType<PlayerProfileManager>();
            }

            economyManager = economy != null ? economy : economyManager;
            if (economyManager == null)
            {
                economyManager = FindAnyObjectByType<EconomyManager>();
            }

            onClosed = closedCallback;
            BuildLayout();
            LoadAvatarSpritesIfNeeded();
            PopulateAvatarGrid();
            WireButtons();
            RefreshFromProfile();
            HideError();
        }

        public void Open()
        {
            Initialize(profileManager, onClosed);
            gameObject.SetActive(true);
        }

        public void SaveChanges()
        {
            if (profileManager == null)
            {
                ShowError("Profile system is not ready.");
                return;
            }

            string newName = nameInput != null ? nameInput.text : string.Empty;
            if (!profileManager.IsValidPlayerName(newName))
            {
                ShowError("Enter a name between 2-14 characters.");
                return;
            }

            profileManager.SetPlayerName(newName);
            if (!profileManager.SetAvatar(selectedAvatarIndex))
            {
                ShowError("Unlock this avatar first.");
                return;
            }

            HideError();
            gameObject.SetActive(false);
            onClosed?.Invoke();
        }

        public void ChangeAvatar(int index)
        {
            if (avatarSprites.Count == 0)
            {
                selectedAvatarIndex = 0;
            }
            else
            {
                selectedAvatarIndex = Mathf.Clamp(index, 0, avatarSprites.Count - 1);
            }

            RefreshAvatarSelection();
            RefreshPreview();
        }

        private void HandleAvatarPurchaseRequested(int index)
        {
            if (profileManager == null)
            {
                ShowError("Profile system is not ready.");
                return;
            }

            if (!profileManager.TryUnlockAvatar(index, economyManager, out string message))
            {
                ShowError(message);
                return;
            }

            PopulateAvatarGrid();
            ChangeAvatar(index);
            ShowMessage("Avatar unlocked!", new Color(0.54f, 1f, 0.58f, 1f));
        }

        public void ChangeName(string newName)
        {
            if (nameInput != null)
            {
                nameInput.text = PlayerProfileManager.SanitizePlayerName(newName);
            }
        }

        public void SetAvatarSprites(List<Sprite> sprites)
        {
            avatarSprites = sprites ?? new List<Sprite>();
            PopulateAvatarGrid();
            RefreshAvatarSelection();
            RefreshPreview();
        }

        public void BuildLayout()
        {
            if (layoutBuilt)
            {
                return;
            }

            layoutBuilt = true;
            RectTransform root = EnsureRectTransform(gameObject);
            Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0.025f, 0.012f, 0.07f, 0.72f);
            overlay.raycastTarget = true;

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            GameObject card = CreateUIObject("ProfileEditCard", transform);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 1080f);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.105f, 0.067f, 0.235f, 0.98f);
            cardImage.raycastTarget = true;
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.86f, 1f, 0.62f);
            outline.effectDistance = new Vector2(3f, -3f);
            Shadow shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -12f);

            VerticalLayoutGroup vertical = card.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(48, 48, 46, 44);
            vertical.spacing = 18f;
            vertical.childControlHeight = false;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childAlignment = TextAnchor.UpperCenter;

            TextMeshProUGUI title = CreateTMPText("Title", card.transform, "Player Profile", 52f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.78f, 1f));
            AddLayout(title.gameObject, -1f, 70f);

            GameObject preview = CreateUIObject("CurrentProfilePreview", card.transform);
            HorizontalLayoutGroup previewLayout = preview.AddComponent<HorizontalLayoutGroup>();
            previewLayout.padding = new RectOffset(20, 20, 12, 12);
            previewLayout.spacing = 22f;
            previewLayout.childAlignment = TextAnchor.MiddleLeft;
            previewLayout.childControlHeight = true;
            previewLayout.childControlWidth = false;
            Image previewBg = preview.AddComponent<Image>();
            previewBg.color = new Color(0.035f, 0.16f, 0.22f, 1f);
            AddLayout(preview, -1f, 172f);

            GameObject avatarFrame = CreateUIObject("CurrentAvatarFrame", preview.transform);
            Image avatarFrameImage = avatarFrame.AddComponent<Image>();
            avatarFrameImage.color = new Color(0.08f, 0.22f, 0.36f, 1f);
            Outline avatarOutline = avatarFrame.AddComponent<Outline>();
            avatarOutline.effectColor = new Color(1f, 0.78f, 0.24f, 0.88f);
            avatarOutline.effectDistance = new Vector2(4f, -4f);
            AddLayout(avatarFrame, 140f, 140f);
            currentAvatarImage = CreateUIObject("CurrentAvatar", avatarFrame.transform).AddComponent<Image>();
            currentAvatarImage.preserveAspect = true;
            Stretch(currentAvatarImage.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            currentNameText = CreateTMPText("CurrentName", preview.transform, "Player", 34f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            currentNameText.textWrappingMode = TextWrappingModes.NoWrap;
            AddLayout(currentNameText.gameObject, 520f, 120f);

            nameInput = CreateInputField(card.transform);
            AddLayout(nameInput.gameObject, -1f, 86f);

            GameObject actionRow = CreateUIObject("ActionRow", card.transform);
            HorizontalLayoutGroup actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 18f;
            actionLayout.childControlHeight = true;
            actionLayout.childControlWidth = true;
            actionLayout.childForceExpandHeight = true;
            actionLayout.childForceExpandWidth = true;
            AddLayout(actionRow, -1f, 76f);

            changeNameButton = CreatePremiumButton("ChangeNameButton", actionRow.transform, "CHANGE NAME", new Color(0.10f, 0.22f, 0.42f, 1f), Color.white, 24f);
            changeAvatarButton = CreatePremiumButton("ChangeAvatarButton", actionRow.transform, "CHANGE AVATAR", new Color(0.10f, 0.22f, 0.42f, 1f), Color.white, 24f);

            TextMeshProUGUI label = CreateTMPText("AvatarLabel", card.transform, "AVATARS", 23f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.74f, 0.22f, 1f));
            AddLayout(label.gameObject, -1f, 34f);

            GameObject grid = CreateUIObject("AvatarGrid", card.transform);
            avatarGridParent = grid.transform;
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150f, 150f);
            gridLayout.spacing = new Vector2(16f, 16f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            AddLayout(grid, -1f, 492f);

            GameObject footer = CreateUIObject("FooterRow", card.transform);
            HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 18f;
            footerLayout.childControlHeight = true;
            footerLayout.childControlWidth = true;
            footerLayout.childForceExpandHeight = true;
            footerLayout.childForceExpandWidth = true;
            AddLayout(footer, -1f, 86f);

            closeButton = CreatePremiumButton("CloseButton", footer.transform, "CLOSE", new Color(0.18f, 0.12f, 0.32f, 1f), Color.white, 28f);
            saveButton = CreatePremiumButton("SaveButton", footer.transform, "SAVE", new Color(1f, 0.48f, 0.08f, 1f), new Color(1f, 0.92f, 0.64f, 1f), 30f);

            errorText = CreateTMPText("ErrorText", card.transform, "Enter a name between 2-14 characters.", 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.36f, 0.36f, 1f));
            AddLayout(errorText.gameObject, -1f, 36f);
            HideError();
        }

        private void WireButtons()
        {
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(SaveChanges);
                saveButton.onClick.AddListener(SaveChanges);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (changeNameButton != null)
            {
                changeNameButton.onClick.RemoveListener(FocusNameInput);
                changeNameButton.onClick.AddListener(FocusNameInput);
            }

            if (changeAvatarButton != null)
            {
                changeAvatarButton.onClick.RemoveListener(RefreshAvatarSelection);
                changeAvatarButton.onClick.AddListener(RefreshAvatarSelection);
            }
        }

        private void RefreshFromProfile()
        {
            if (profileManager != null)
            {
                selectedAvatarIndex = Mathf.Max(0, profileManager.SelectedAvatarIndex);
                if (nameInput != null)
                {
                    nameInput.text = profileManager.PlayerName;
                }
            }

            RefreshAvatarSelection();
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (profileManager != null && currentNameText != null)
            {
                currentNameText.text = string.IsNullOrWhiteSpace(nameInput != null ? nameInput.text : string.Empty)
                    ? profileManager.PlayerName
                    : PlayerProfileManager.SanitizePlayerName(nameInput.text);
            }

            if (currentAvatarImage != null && avatarSprites.Count > 0)
            {
                int index = Mathf.Clamp(selectedAvatarIndex, 0, avatarSprites.Count - 1);
                currentAvatarImage.sprite = avatarSprites[index];
            }
        }

        private void RefreshAvatarSelection()
        {
            for (int index = 0; index < avatarButtons.Count; index++)
            {
                AvatarSelectionButton button = avatarButtons[index];
                if (button != null)
                {
                    button.SetSelected(button.AvatarIndex == selectedAvatarIndex);
                }
            }
        }

        private void PopulateAvatarGrid()
        {
            if (avatarGridParent == null)
            {
                return;
            }

            foreach (Transform child in avatarGridParent)
            {
                Destroy(child.gameObject);
            }

            avatarButtons.Clear();
            for (int index = 0; index < avatarSprites.Count; index++)
            {
                AvatarSelectionButton avatarButton = avatarButtonPrefab != null
                    ? Instantiate(avatarButtonPrefab, avatarGridParent)
                    : CreateAvatarButton(avatarGridParent);

                bool locked = profileManager != null
                    ? !profileManager.IsAvatarUnlocked(index)
                    : ProfileAvatarLibrary.IsPremiumAvatarIndex(index);
                int price = profileManager != null ? profileManager.GetAvatarPrice(index) : ProfileAvatarLibrary.GetAvatarPrice(index);
                avatarButton.Setup(index, avatarSprites[index], ChangeAvatar, locked, price, HandleAvatarPurchaseRequested);
                avatarButtons.Add(avatarButton);
            }
        }

        private void LoadAvatarSpritesIfNeeded()
        {
            if (avatarSprites != null && avatarSprites.Count > 0)
            {
                return;
            }

            avatarSprites = ProfileAvatarLibrary.LoadSprites();
        }

        private void FocusNameInput()
        {
            nameInput?.ActivateInputField();
        }

        private void Close()
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = new Color(1f, 0.36f, 0.36f, 1f);
                errorText.gameObject.SetActive(true);
            }
        }

        private void ShowMessage(string message, Color color)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.color = color;
                errorText.gameObject.SetActive(true);
            }
        }

        private void HideError()
        {
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        private AvatarSelectionButton CreateAvatarButton(Transform parent)
        {
            GameObject root = CreateUIObject("AvatarButton", parent);
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.06f, 0.17f, 0.31f, 1f);
            Button button = root.AddComponent<Button>();
            ConfigureButtonColors(button);

            GameObject avatar = CreateUIObject("AvatarImage", root.transform);
            Image avatarImage = avatar.AddComponent<Image>();
            avatarImage.preserveAspect = true;
            Stretch(avatar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            GameObject frame = CreateUIObject("SelectedFrame", root.transform);
            Image frameImage = frame.AddComponent<Image>();
            frameImage.color = new Color(1f, 0.78f, 0.18f, 0.18f);
            Outline frameOutline = frame.AddComponent<Outline>();
            frameOutline.effectColor = new Color(1f, 0.88f, 0.26f, 1f);
            frameOutline.effectDistance = new Vector2(7f, -7f);
            Stretch(frame.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject lockOverlay = CreateUIObject("LockedOverlay", root.transform);
            Image lockImage = lockOverlay.AddComponent<Image>();
            lockImage.color = new Color(0.02f, 0.015f, 0.045f, 0.72f);
            Stretch(lockOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI lockTitle = CreateTMPText("LockTitle", lockOverlay.transform, "LOCKED", 19f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.32f, 1f));
            Stretch(lockTitle.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.55f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI price = CreateTMPText("PriceText", lockOverlay.transform, "COIN 2500", 21f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(price.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            AvatarSelectionButton avatarButton = root.AddComponent<AvatarSelectionButton>();
            avatarButton.SetReferences(avatarImage, frame, button, lockOverlay, price);
            return avatarButton;
        }

        private static TMP_InputField CreateInputField(Transform parent)
        {
            GameObject root = CreateUIObject("NameInput", parent);
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.035f, 0.11f, 0.2f, 1f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.75f, 0.22f, 0.88f);
            outline.effectDistance = new Vector2(3f, -3f);

            GameObject viewport = CreateUIObject("Viewport", root.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, Vector2.zero, Vector2.one, new Vector2(28f, 10f), new Vector2(-28f, -10f));
            viewport.AddComponent<RectMask2D>();

            TextMeshProUGUI inputText = CreateTMPText("Text", viewport.transform, string.Empty, 32f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            Stretch(inputText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inputText.raycastTarget = false;

            TextMeshProUGUI placeholder = CreateTMPText("Placeholder", viewport.transform, "Enter name", 32f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.36f));
            Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholder.raycastTarget = false;

            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.textViewport = viewportRect;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.characterLimit = 18;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static Button CreatePremiumButton(string name, Transform parent, string label, Color baseColor, Color textColor, float fontSize)
        {
            GameObject root = CreateUIObject(name, parent);
            Image image = root.AddComponent<Image>();
            image.color = baseColor;
            Button button = root.AddComponent<Button>();
            ConfigureButtonColors(button);
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -7f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.75f);
            outline.effectDistance = new Vector2(3f, -3f);

            TextMeshProUGUI text = CreateTMPText("Label", root.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void ConfigureButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.86f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.9f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.9f);
            button.colors = colors;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateUIObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static RectTransform EnsureRectTransform(GameObject obj)
        {
            return obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
        }

        private static void AddLayout(GameObject obj, float width, float height)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
            }

            layout.preferredHeight = height;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
