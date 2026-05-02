using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class ProfileSetupPopupController : MonoBehaviour
    {
        [SerializeField] private PlayerProfileManager profileManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Transform avatarGridParent;
        [SerializeField] private AvatarSelectionButton avatarButtonPrefab;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private List<Sprite> avatarSprites = new();
        [SerializeField] private int selectedAvatarIndex;

        private readonly List<AvatarSelectionButton> avatarButtons = new();
        private List<AvatarData> avatarData = new();
        private Action onCompleted;
        private bool layoutBuilt;

        // Guarantee CanvasGroup exists before any external script (UIManager,
        // ProfileBootstrap, parent canvas wiring) tries to read it. BuildLayout still
        // adds one as a fallback, but this Awake-time placement avoids the
        // MissingComponentException seen during cold boot in Editor.
        private void Awake()
        {
            if (GetComponent<CanvasGroup>() == null)
            {
                gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Initialize()
        {
            Initialize(profileManager, economyManager, null);
        }

        public void Initialize(PlayerProfileManager manager, Action completedCallback = null)
        {
            Initialize(manager, economyManager, completedCallback);
        }

        public void Initialize(PlayerProfileManager manager, EconomyManager economy, Action completedCallback = null)
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

            onCompleted = completedCallback;
            BuildLayout();
            LoadAvatarSpritesIfNeeded();
            PopulateAvatarGrid();
            selectedAvatarIndex = GetSafeSelectedAvatarIndex(profileManager != null ? profileManager.SelectedAvatarIndex : 0);
            RefreshAvatarSelection();

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            HideError();
        }

        public void Open()
        {
            Initialize(profileManager, onCompleted);
            if (nameInput != null)
            {
                nameInput.text = string.Empty;
                nameInput.ActivateInputField();
            }

            gameObject.SetActive(true);
        }

        public void SetAvatarSprites(List<Sprite> sprites)
        {
            avatarSprites = sprites ?? new List<Sprite>();
            PopulateAvatarGrid();
            RefreshAvatarSelection();
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
            overlay.color = new Color(0.025f, 0.012f, 0.07f, 0.86f);
            overlay.raycastTarget = true;

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            GameObject card = CreateUIObject("ProfileCard", transform);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 1120f);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.105f, 0.067f, 0.235f, 0.98f);
            cardImage.raycastTarget = true;
            Outline cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.72f, 0.22f, 0.72f);
            cardOutline.effectDistance = new Vector2(3f, -3f);
            Shadow cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            cardShadow.effectDistance = new Vector2(0f, -12f);

            VerticalLayoutGroup vertical = card.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(54, 54, 52, 46);
            vertical.spacing = 22f;
            vertical.childControlHeight = false;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childAlignment = TextAnchor.UpperCenter;

            TextMeshProUGUI title = CreateTMPText("Title", card.transform, "Create Your Profile", 54f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.78f, 1f));
            AddLayout(title.gameObject, -1f, 74f);

            TextMeshProUGUI subtitle = CreateTMPText("Subtitle", card.transform, "Choose your name and avatar", 27f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.78f, 0.88f, 1f, 0.92f));
            AddLayout(subtitle.gameObject, -1f, 44f);

            nameInput = CreateInputField(card.transform);
            AddLayout(nameInput.gameObject, -1f, 92f);

            TextMeshProUGUI pickLabel = CreateTMPText("AvatarLabel", card.transform, "PICK YOUR AVATAR", 23f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.74f, 0.22f, 1f));
            AddLayout(pickLabel.gameObject, -1f, 34f);

            GameObject grid = CreateUIObject("AvatarGrid", card.transform);
            avatarGridParent = grid.transform;
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(168f, 168f);
            gridLayout.spacing = new Vector2(18f, 18f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            AddLayout(grid, -1f, 540f);

            continueButton = CreatePremiumButton("ContinueButton", card.transform, "CONTINUE", new Color(1f, 0.48f, 0.08f, 1f), new Color(1f, 0.92f, 0.64f, 1f));
            AddLayout(continueButton.gameObject, -1f, 92f);

            errorText = CreateTMPText("ErrorText", card.transform, "Enter a name between 2-14 characters.", 23f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.36f, 0.36f, 1f));
            AddLayout(errorText.gameObject, -1f, 40f);
            HideError();
        }

        public void SelectAvatar(int index)
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
        }

        public bool ValidateName(string name)
        {
            return profileManager != null && profileManager.IsValidPlayerName(name);
        }

        public void OnContinueClicked()
        {
            if (profileManager == null)
            {
                ShowError("Profile system is not ready.");
                return;
            }

            string requestedName = nameInput != null ? nameInput.text : string.Empty;
            if (!ValidateName(requestedName))
            {
                ShowError("Enter a name between 2-14 characters.");
                return;
            }

            if (!profileManager.SetPlayerName(requestedName))
            {
                ShowError("Enter a name between 2-14 characters.");
                return;
            }

            if (!profileManager.SetAvatar(selectedAvatarIndex))
            {
                ShowError("Unlock this avatar first.");
                return;
            }

            profileManager.CompleteProfileSetup();
            HideError();
            gameObject.SetActive(false);
            onCompleted?.Invoke();
        }

        public void RefreshAvatarSelection()
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
            SelectAvatar(index);
            ShowMessage("Avatar unlocked!", new Color(0.54f, 1f, 0.58f, 1f));
        }

        private void LoadAvatarSpritesIfNeeded()
        {
            if (avatarSprites != null && avatarSprites.Count > 0)
            {
                avatarData = ProfileAvatarLibrary.BuildDefaultData(avatarSprites);
                return;
            }

            avatarSprites = ProfileAvatarLibrary.LoadSprites();
            avatarData = ProfileAvatarLibrary.BuildDefaultData(avatarSprites);
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
                avatarButton.Setup(index, avatarSprites[index], SelectAvatar, locked, price, HandleAvatarPurchaseRequested);
                avatarButtons.Add(avatarButton);
            }
        }

        private AvatarSelectionButton CreateAvatarButton(Transform parent)
        {
            GameObject root = CreateUIObject("AvatarButton", parent);
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.06f, 0.17f, 0.31f, 1f);
            Button button = root.AddComponent<Button>();
            ConfigureButtonColors(button);

            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(3f, -3f);

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

            TextMeshProUGUI lockTitle = CreateTMPText("LockTitle", lockOverlay.transform, "LOCKED", 21f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.32f, 1f));
            Stretch(lockTitle.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.55f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI price = CreateTMPText("PriceText", lockOverlay.transform, "COIN 2500", 23f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(price.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            AvatarSelectionButton avatarButton = root.AddComponent<AvatarSelectionButton>();
            avatarButton.SetReferences(avatarImage, frame, button, lockOverlay, price);
            return avatarButton;
        }

        private TMP_InputField CreateInputField(Transform parent)
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

            TextMeshProUGUI inputText = CreateTMPText("Text", viewport.transform, string.Empty, 34f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            Stretch(inputText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inputText.raycastTarget = false;

            TextMeshProUGUI placeholder = CreateTMPText("Placeholder", viewport.transform, "Enter name", 34f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.36f));
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

        private Button CreatePremiumButton(string name, Transform parent, string label, Color baseColor, Color textColor)
        {
            GameObject root = CreateUIObject(name, parent);
            Image image = root.AddComponent<Image>();
            image.color = baseColor;
            Button button = root.AddComponent<Button>();
            ConfigureButtonColors(button);
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -8f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            TextMeshProUGUI text = CreateTMPText("Label", root.transform, label, 34f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
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

        private void ShowError(string message)
        {
            if (errorText == null)
            {
                return;
            }

            errorText.text = message;
            errorText.color = new Color(1f, 0.36f, 0.36f, 1f);
            errorText.gameObject.SetActive(true);
        }

        private void ShowMessage(string message, Color color)
        {
            if (errorText == null)
            {
                return;
            }

            errorText.text = message;
            errorText.color = color;
            errorText.gameObject.SetActive(true);
        }

        private void HideError()
        {
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
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
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private int GetSafeSelectedAvatarIndex(int requestedIndex)
        {
            if (avatarSprites.Count == 0)
            {
                return 0;
            }

            requestedIndex = Mathf.Clamp(requestedIndex, 0, avatarSprites.Count - 1);
            if (profileManager == null || profileManager.IsAvatarUnlocked(requestedIndex))
            {
                return requestedIndex;
            }

            for (int index = 0; index < avatarSprites.Count; index++)
            {
                if (profileManager.IsAvatarUnlocked(index))
                {
                    return index;
                }
            }

            return 0;
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
