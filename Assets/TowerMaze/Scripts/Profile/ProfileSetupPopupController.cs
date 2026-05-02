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
        [SerializeField] private FirebaseCloudManager firebaseCloud;
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
        private bool submissionInProgress;

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

            // Cloud nickname is optional — if Firebase isn't wired we just skip the
            // profanity / duplicate / suggestion path and only do local profile setup.
            if (firebaseCloud == null)
            {
                firebaseCloud = FindAnyObjectByType<FirebaseCloudManager>();
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
            overlay.color = new Color(0.015f, 0.008f, 0.04f, 0.92f);
            overlay.raycastTarget = true;

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Card: deeper navy + warm gold rim. Bumped height from 1120 -> 1240 so the
            // VerticalLayoutGroup can lay out children without clipping the bottom.
            GameObject card = CreateUIObject("ProfileCard", transform);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(900f, 1240f);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.06f, 0.04f, 0.14f, 0.99f);
            cardImage.raycastTarget = true;
            Outline cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.78f, 0.30f, 0.55f);
            cardOutline.effectDistance = new Vector2(3f, -3f);
            Shadow cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            cardShadow.effectDistance = new Vector2(0f, -16f);

            // Soft warm glow inset for depth — premium feel without obscuring text.
            GameObject glow = CreateUIObject("InnerGlow", card.transform);
            Image glowImage = glow.AddComponent<Image>();
            glowImage.color = new Color(1f, 0.62f, 0.18f, 0.06f);
            glowImage.raycastTarget = false;
            Stretch(glow.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(28f, 28f), new Vector2(-28f, -28f));

            VerticalLayoutGroup vertical = card.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(56, 56, 60, 50);
            vertical.spacing = 24f;
            // childControlHeight must be TRUE for LayoutElement.preferredHeight to be
            // honored — otherwise VLG falls back to each child's RectTransform.sizeDelta
            // which defaults to (100, 100) and breaks the layout.
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childAlignment = TextAnchor.UpperCenter;

            TextMeshProUGUI title = CreateTMPText("Title", card.transform, GetTitleText(), 60f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.38f, 1f));
            Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.16f, 0.06f, 0.02f, 0.85f);
            titleShadow.effectDistance = new Vector2(2f, -3f);
            AddLayout(title.gameObject, -1f, 84f);

            TextMeshProUGUI subtitle = CreateTMPText("Subtitle", card.transform, GetSubtitleText(), 26f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 1f, 0.88f));
            AddLayout(subtitle.gameObject, -1f, 44f);

            nameInput = CreateInputField(card.transform);
            AddLayout(nameInput.gameObject, -1f, 100f);

            TextMeshProUGUI pickLabel = CreateTMPText("AvatarLabel", card.transform, GetPickLabelText(), 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.74f, 0.22f, 0.95f));
            pickLabel.characterSpacing = 6f;
            AddLayout(pickLabel.gameObject, -1f, 36f);

            GameObject grid = CreateUIObject("AvatarGrid", card.transform);
            avatarGridParent = grid.transform;
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(170f, 170f);
            gridLayout.spacing = new Vector2(18f, 18f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            AddLayout(grid, -1f, 546f);

            continueButton = CreatePremiumButton("ContinueButton", card.transform, GetContinueText(), new Color(1f, 0.55f, 0.10f, 1f), new Color(0.16f, 0.07f, 0.01f, 1f));
            AddLayout(continueButton.gameObject, -1f, 104f);

            errorText = CreateTMPText("ErrorText", card.transform, string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.42f, 0.42f, 1f));
            AddLayout(errorText.gameObject, -1f, 40f);
            HideError();
        }

        private static string GetTitleText()
        {
            return UILanguage.Translate("PROFILINI OLUSTUR", "CREATE YOUR PROFILE", "CREA TU PERFIL");
        }

        private static string GetSubtitleText()
        {
            return UILanguage.Translate(
                "Isim ve avatar sec",
                "Choose your name and avatar",
                "Elige tu nombre y avatar");
        }

        private static string GetPickLabelText()
        {
            return UILanguage.Translate("AVATARINI SEC", "PICK YOUR AVATAR", "ELIGE TU AVATAR");
        }

        private static string GetContinueText()
        {
            return UILanguage.Translate("DEVAM", "CONTINUE", "CONTINUAR");
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
            if (submissionInProgress) return;

            if (profileManager == null)
            {
                ShowError("Profile system is not ready.");
                return;
            }

            string requestedName = nameInput != null ? nameInput.text : string.Empty;
            if (!ValidateName(requestedName))
            {
                ShowError(UILanguage.Translate(
                    "2-14 karakter kullan.",
                    "Use 2-14 characters.",
                    "Usa 2-14 caracteres."));
                return;
            }

            // Local profanity gate: even when Firebase is offline, never allow a
            // bad name through. The blocklist runs against the normalized form
            // (uppercase, A-Z/0-9/_) so leetspeak roots still match the entries
            // ProfanityFilter knows about.
            string normalizedForFilter = NormalizeForProfanity(requestedName);
            if (ProfanityFilter.IsProfane(normalizedForFilter))
            {
                ShowError(UILanguage.Translate(
                    "BU ISIM UYGUN DEGIL. BASKA BIR ISIM DENE.",
                    "THIS NAME ISN'T ALLOWED. TRY ANOTHER ONE.",
                    "ESTE NOMBRE NO ESTA PERMITIDO. PRUEBA OTRO."));
                return;
            }

            if (firebaseCloud == null)
            {
                // No cloud wiring — fall back to the original local-only flow.
                FinalizeProfileSetup(requestedName);
                return;
            }

            // Online path: ask Firebase to reserve the nickname. The same call
            // surfaces "SUGGEST:USTAB42" on duplicates so we can offer a one-tap
            // alternative without forcing the player to invent a new name.
            submissionInProgress = true;
            SetContinueButtonInteractable(false);
            ShowMessage(
                UILanguage.Translate("ISIM KONTROL EDILIYOR...", "CHECKING NAME...", "COMPROBANDO NOMBRE..."),
                new Color(0.78f, 0.88f, 1f, 0.92f));

            firebaseCloud.TrySetNickname(requestedName, (success, message) =>
            {
                submissionInProgress = false;
                SetContinueButtonInteractable(true);

                if (success)
                {
                    FinalizeProfileSetup(requestedName);
                    return;
                }

                if (!string.IsNullOrEmpty(message) && message.StartsWith(FirebaseCloudManager.SuggestionPrefix))
                {
                    string suggestion = message.Substring(FirebaseCloudManager.SuggestionPrefix.Length);
                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        if (nameInput != null)
                        {
                            nameInput.text = suggestion;
                        }
                        string suggestionTemplate = UILanguage.Translate(
                            "ALINMIS. {0} UYGUN — DEVAM'A BAS",
                            "TAKEN. {0} IS FREE — TAP CONTINUE",
                            "OCUPADO. {0} ESTA LIBRE — PULSA CONTINUAR");
                        ShowMessage(string.Format(suggestionTemplate, suggestion), new Color(1f, 0.86f, 0.42f, 1f));
                        return;
                    }
                }

                ShowError(string.IsNullOrEmpty(message)
                    ? UILanguage.Translate(
                        "ISIM KULLANILAMIYOR. BASKA DENE.",
                        "NAME NOT AVAILABLE. TRY ANOTHER ONE.",
                        "NOMBRE NO DISPONIBLE. PRUEBA OTRO.")
                    : message);
            });
        }

        // Mirror of FirebaseCloudManager.NormalizeNickname so the profanity check
        // sees the same string Firebase will see (uppercase, alphanumeric + underscore).
        private static string NormalizeForProfanity(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string upper = value.Trim().ToUpperInvariant();
            System.Text.StringBuilder sb = new System.Text.StringBuilder(upper.Length);
            for (int i = 0; i < upper.Length; i++)
            {
                char c = upper[i];
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void SetContinueButtonInteractable(bool interactable)
        {
            if (continueButton != null) continueButton.interactable = interactable;
        }

        private void FinalizeProfileSetup(string playerName)
        {
            if (!profileManager.SetPlayerName(playerName))
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
            background.color = new Color(0.025f, 0.06f, 0.14f, 1f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.30f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
            // Inner shadow for depth — makes the input feel inset rather than flat.
            Shadow innerShadow = root.AddComponent<Shadow>();
            innerShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            innerShadow.effectDistance = new Vector2(0f, -4f);

            GameObject viewport = CreateUIObject("Viewport", root.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, Vector2.zero, Vector2.one, new Vector2(32f, 12f), new Vector2(-32f, -12f));
            viewport.AddComponent<RectMask2D>();

            TextMeshProUGUI inputText = CreateTMPText("Text", viewport.transform, string.Empty, 36f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.98f, 0.92f, 1f));
            Stretch(inputText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inputText.raycastTarget = false;

            string placeholderText = UILanguage.Translate("Ismini gir", "Enter your name", "Escribe tu nombre");
            TextMeshProUGUI placeholder = CreateTMPText("Placeholder", viewport.transform, placeholderText, 32f, FontStyles.Bold | FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color(1f, 1f, 1f, 0.32f));
            Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholder.raycastTarget = false;

            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.textViewport = viewportRect;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.characterLimit = 14;
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
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -10f);
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.92f, 0.42f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Highlight overlay along the top to fake a glossy gradient. Lightweight,
            // costs nothing more than an extra Image.
            GameObject highlight = CreateUIObject("Highlight", root.transform);
            Image highlightImage = highlight.AddComponent<Image>();
            highlightImage.color = new Color(1f, 1f, 1f, 0.18f);
            highlightImage.raycastTarget = false;
            Stretch(highlight.GetComponent<RectTransform>(), new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI text = CreateTMPText("Label", root.transform, label, 38f, FontStyles.Bold, TextAlignmentOptions.Center, textColor);
            text.characterSpacing = 8f;
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
            text.textWrappingMode = TextWrappingModes.Normal;
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
