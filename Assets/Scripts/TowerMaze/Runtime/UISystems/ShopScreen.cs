using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

namespace TowerMaze
{
    public sealed class ShopScreenController : MonoBehaviour
    {
        // State
        private Font _font;
        private ThemeDefinition _theme;
        private EconomyManager _economyManager;
        private Action _onClose;
        private Action _onClaimCoinBoost;
        private Action _onRestorePurchases;
        private Action<ShopCatalogType, string> _onItemSelected;
        private Action _buttonClickSound;

        // UI references
        private Text _coinPillText;
        private Button[] _tabBtns;
        private CanvasGroup _contentGroup;
        private ScrollRect _contentScroll;
        private Transform _contentList;
        private Text _adRewardButtonLabel;

        // Data
        private int _currentTab = 0;
        private IReadOnlyList<BallSkinDefinition> _ballSkins = Array.Empty<BallSkinDefinition>();
        private IReadOnlyList<TowerSkinDefinition> _towerSkins = Array.Empty<TowerSkinDefinition>();
        private IReadOnlyList<CoinPackOffer> _coinOffers = Array.Empty<CoinPackOffer>();

        // Texture cache
        private static Texture2D _texSingle;
        private static Texture2D _texStack;
        private static Texture2D _texBag;
        private static Texture2D _texChest;
        private static Texture2D _texNoAds;

        // Initialize: signature matches UIManager.cs call
        //   shopScreenController.Initialize(runtimeFont, theme, HideShop,
        //       HandleShopCoinBoost, HandleCoinStoreRestore, HandleShopAction, buttonClickSound)
        public void Initialize(
            Font font,
            ThemeDefinition themeDefinition,
            Action onClose,
            Action onClaimCoinBoost,
            Action onRestorePurchases,
            Action<ShopCatalogType, string> onSelectItem,
            Action onButtonClick = null)
        {
            _font = font; _theme = themeDefinition; _onClose = onClose;
            _onClaimCoinBoost = onClaimCoinBoost; _onRestorePurchases = onRestorePurchases;
            _onItemSelected = onSelectItem; _buttonClickSound = onButtonClick;

            // Root: Dimmed overlay background
            Image bgOverlay = UIManager.CreateImage("ShopBgOverlay", transform, new Color(0, 0, 0, 0.75f));
            UIManager.Stretch(bgOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Centered Modal Panel
            GameObject panelGo = new("ShopPanel");
            panelGo.transform.SetParent(transform, false);
            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.06f, 0.05f);
            panelRt.anchorMax = new Vector2(0.94f, 0.95f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
            Image panel = panelGo.AddComponent<Image>();
            panel.color = UIStyle.ShopBg;
            panelGo.AddComponent<CanvasGroup>(); // For future animations

            // Header row
            Image header = UIManager.CreateCard("HeaderCard", panel.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            UIManager.Stretch(header.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            
            _coinPillText = CreateGoldPill(header.transform, "CoinPill", "COIN  0", font);
            _coinPillText.rectTransform.anchorMin = new Vector2(0.05f, 0.2f);
            _coinPillText.rectTransform.anchorMax = new Vector2(0.40f, 0.8f);
            _coinPillText.rectTransform.offsetMin = _coinPillText.rectTransform.offsetMax = Vector2.zero;

            Button closeBtn = UIManager.CreateButton("CloseShop", header.transform, font, "\u00d7", UIStyle.SurfaceDark, UIStyle.TextPrimary);
            UIManager.StyleButtonLabel(closeBtn, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.Stretch((RectTransform)closeBtn.transform, new Vector2(0.88f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);
            UIManager.BindButton(closeBtn, () => _onClose?.Invoke(), _buttonClickSound);

            // Tab bar: COINS / BALLS / TOWERS
            Image tabCard = UIManager.CreateCard("TabCard", panel.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            UIManager.Stretch(tabCard.rectTransform, new Vector2(0.04f, 0.81f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            _tabBtns = new Button[3];
            _tabBtns[0] = CreateTabButton(tabCard.transform, "CoinsTab", "COINS", font, 0);
            UIManager.Stretch((RectTransform)_tabBtns[0].transform, new Vector2(0.02f, 0.12f), new Vector2(0.32f, 0.88f), Vector2.zero, Vector2.zero);
            _tabBtns[1] = CreateTabButton(tabCard.transform, "BallsTab", "BALLS", font, 1);
            UIManager.Stretch((RectTransform)_tabBtns[1].transform, new Vector2(0.35f, 0.12f), new Vector2(0.65f, 0.88f), Vector2.zero, Vector2.zero);
            _tabBtns[2] = CreateTabButton(tabCard.transform, "TowersTab", "TOWERS", font, 2);
            UIManager.Stretch((RectTransform)_tabBtns[2].transform, new Vector2(0.68f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
 
            // Content area
            Image listCard = UIManager.CreateCard("ListCard", panel.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            UIManager.Stretch(listCard.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.79f), Vector2.zero, Vector2.zero);
            _contentGroup = listCard.gameObject.AddComponent<CanvasGroup>();
            GameObject vpGo = new("Viewport"); vpGo.transform.SetParent(listCard.transform, false);
            RectTransform vpRect = EnsureRectTransform(vpGo);
            UIManager.Stretch(vpRect, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
            Image vpImg = vpGo.AddComponent<Image>(); vpImg.color = new Color(0f, 0f, 0f, 0.22f);
            Mask vpMask = vpGo.AddComponent<Mask>(); vpMask.showMaskGraphic = false;
            GameObject scrollGo = new("ScrollView"); scrollGo.transform.SetParent(vpGo.transform, false);
            RectTransform scrollRt = EnsureRectTransform(scrollGo);
            UIManager.Stretch(scrollRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _contentScroll = scrollGo.AddComponent<ScrollRect>();
            _contentScroll.horizontal = false; _contentScroll.vertical = true;
            _contentScroll.movementType = ScrollRect.MovementType.Clamped;
            _contentScroll.scrollSensitivity = 32f; _contentScroll.viewport = vpRect;
            GameObject listGo = new("ListRoot"); listGo.transform.SetParent(scrollGo.transform, false);
            RectTransform listRt = EnsureRectTransform(listGo);
            UIManager.Stretch(listRt, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            listRt.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f; vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            ContentSizeFitter csf = listGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            _contentScroll.content = listRt; _contentList = listGo.transform;
            SwitchTab(0, font, false);
        }

        public void SetState(
            int emberBalance,
            IReadOnlyList<BallSkinDefinition> skins,
            IReadOnlyList<TowerSkinDefinition> towerSkinList,
            IReadOnlyList<CoinPackOffer> coinOffers,
            EconomyManager activeEconomyManager)
        {
            _economyManager = activeEconomyManager;
            _ballSkins = skins ?? Array.Empty<BallSkinDefinition>();
            _towerSkins = towerSkinList ?? Array.Empty<TowerSkinDefinition>();
            _coinOffers = coinOffers ?? Array.Empty<CoinPackOffer>();
            // Update gold coin balance pill
            if (_coinPillText != null) _coinPillText.text = $"COIN  {emberBalance}";
            if (_adRewardButtonLabel != null && _economyManager != null)
                _adRewardButtonLabel.text = $"AD +{_economyManager.GetShopCoinBoostReward()} COIN";
            RebuildContent(_currentTab, _font);
        }

        // Gold pill with Outline component for gold border
        private Text CreateGoldPill(Transform parent, string name, string text, Font font)
        {
            Image pill = UIManager.CreateCard(name, parent, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.12f), UIStyle.Gold);
            UIManager.Stretch(pill.rectTransform, new Vector2(0.27f, 0.18f), new Vector2(0.61f, 0.82f), Vector2.zero, Vector2.zero);
            Outline outline = pill.gameObject.AddComponent<Outline>();
            outline.effectColor = UIStyle.Gold; outline.effectDistance = new Vector2(1.5f, 1.5f);
            Text label = UIManager.CreateText(name + "_Label", pill.transform, font, 22, TextAnchor.MiddleCenter, UIStyle.Gold);
            label.fontStyle = FontStyle.Bold; label.text = text;
            UIManager.Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            return label;
        }

        // Tab button: SurfaceDark bg -> Brand when active
        private Button CreateTabButton(Transform parent, string name, string label, Font font, int index)
        {
            Button btn = UIManager.CreateButton(name, parent, font, label, UIStyle.SurfaceDark, UIStyle.TextDim);
            UIManager.StyleButtonLabel(btn, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(btn, () => SwitchTab(index, font, true), _buttonClickSound);
            return btn;
        }

        // Active tab = Brand, others = SurfaceDark
        private void UpdateTabHighlight(int index)
        {
            for (int i = 0; i < _tabBtns.Length; i++)
            {
                bool active = i == index;
                UIManager.ApplyButtonSurface(_tabBtns[i].GetComponent<Image>(), active ? UIStyle.Brand : UIStyle.SurfaceDark);
                _tabBtns[i].GetComponentInChildren<Text>().color = active ? Color.white : UIStyle.TextDim;
            }
        }

        // tab 0=COINS, tab 1/2=BALLS/TOWERS
        private void RebuildContent(int index, Font font)
        {
            if (_contentList == null) return;
            for (int i = _contentList.childCount - 1; i >= 0; i--) Destroy(_contentList.GetChild(i).gameObject);
            if (_contentScroll != null) _contentScroll.verticalNormalizedPosition = 1f;
            if (index == 0)
            {
                if (_coinOffers == null || _coinOffers.Count == 0) return;
                int bestIdx = -1;
                for (int i = 0; i < _coinOffers.Count; i++) { if (_coinOffers[i].featured || _coinOffers[i].kind == StoreOfferKind.WelcomePack) { bestIdx = i; break; } }
                if (bestIdx >= 0) SpawnBestValueCard(_contentList, _coinOffers[bestIdx], font);
                int rank = 0;
                for (int i = 0; i < _coinOffers.Count; i++) { if (i == bestIdx) continue; SpawnRegularShopItem(_contentList, _coinOffers[i], font, rank); rank++; }
            }
            else BuildSkinsGrid(_contentList, font);
        }

        // UIStyle.TabSwitch coroutine when animate=true
        private void SwitchTab(int index, Font font, bool animate = true)
        {
            _currentTab = index; UpdateTabHighlight(index);
            if (animate && _contentGroup != null) StartCoroutine(UIStyle.TabSwitch(_contentGroup, () => RebuildContent(index, font)));
            else RebuildContent(index, font);
        }

        // BEST VALUE card: gradient bg + bouncing badge + GlowPulse
        private void SpawnBestValueCard(Transform list, CoinPackOffer offer, Font font)
        {
            const float height = 160f;
            GameObject cardGo = new("BestValueCard"); cardGo.transform.SetParent(list, false);
            LayoutElement le = cardGo.AddComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height;
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 1f); cardRect.anchorMax = new Vector2(1f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f); cardRect.sizeDelta = new Vector2(0f, height);
            // Gradient background
            GradientImage gradBg = cardGo.AddComponent<GradientImage>();
            gradBg.colorTop = new Color(1f, 0.75f, 0.0f, 0.30f); gradBg.colorBottom = new Color(0.55f, 0.28f, 0.0f, 0.18f); gradBg.raycastTarget = false;
            // Glow pulse
            Image glowImg = UIStyle.CreateGlow(cardRect, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.3f), 12f);
            glowImg.raycastTarget = false; StartCoroutine(UIStyle.GlowPulse(glowImg, UIStyle.Gold, 0.15f, 0.5f, 1.8f));
            // Preview
            Texture2D previewTex = GetCoinPackPreviewTexture(offer);
            GameObject previewGo = new("CoinPreview"); previewGo.transform.SetParent(cardGo.transform, false);
            RawImage previewImg = previewGo.AddComponent<RawImage>();
            previewImg.texture = previewTex != null ? previewTex : Texture2D.whiteTexture; previewImg.color = Color.white; previewImg.raycastTarget = false;
            RectTransform previewRect = (RectTransform)previewGo.transform;
            previewRect.anchorMin = new Vector2(0f, 0f); previewRect.anchorMax = new Vector2(0f, 1f);
            previewRect.offsetMin = new Vector2(16f, 12f); previewRect.offsetMax = new Vector2(116f, -12f);
            // Labels (gold text)
            string primaryText = offer.kind == StoreOfferKind.WelcomePack ? "WELCOME PACK" : FormatCoinAmount(offer.coinAmount) + " COIN";
            Text primaryLabel = UIManager.CreateText("PrimaryLabel", cardGo.transform, font, 28, TextAnchor.MiddleLeft, UIStyle.Gold);
            primaryLabel.fontStyle = FontStyle.Bold; primaryLabel.text = primaryText;
            RectTransform primaryRect = primaryLabel.rectTransform;
            primaryRect.anchorMin = new Vector2(0f, 0.5f); primaryRect.anchorMax = new Vector2(0.7f, 1f);
            primaryRect.offsetMin = new Vector2(130f, 0f); primaryRect.offsetMax = new Vector2(-12f, -8f);
            Text secondaryLabel = UIManager.CreateText("SecondaryLabel", cardGo.transform, font, 16, TextAnchor.UpperLeft, UIStyle.TextDim);
            secondaryLabel.text = GetCoinOfferDetail(offer);
            RectTransform secondaryRect = secondaryLabel.rectTransform;
            secondaryRect.anchorMin = new Vector2(0f, 0.08f); secondaryRect.anchorMax = new Vector2(0.7f, 0.5f);
            secondaryRect.offsetMin = new Vector2(130f, 8f); secondaryRect.offsetMax = new Vector2(-12f, 0f);
            // Buy button: orange gradient (UIStyle.Action)
            Button buyBtn = UIManager.CreateButton("BuyButton", cardGo.transform, font,
                offer.owned ? "OWNED" : offer.priceLabel,
                offer.owned ? UIStyle.SurfaceDark : UIStyle.Action,
                offer.owned ? UIStyle.Owned : Color.white);
            UIManager.StyleButtonLabel(buyBtn, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            RectTransform buyRect = (RectTransform)buyBtn.transform;
            buyRect.anchorMin = new Vector2(0.72f, 0.18f); buyRect.anchorMax = new Vector2(0.97f, 0.82f);
            buyRect.offsetMin = Vector2.zero; buyRect.offsetMax = Vector2.zero;
            buyBtn.interactable = offer.productType == ProductType.Consumable || !offer.owned;
            string capturedId = offer.id;
            UIManager.BindButton(buyBtn, () => { StartCoroutine(UIStyle.BuyButtonTap(buyRect)); _onItemSelected?.Invoke(ShopCatalogType.Coin, capturedId); }, _buttonClickSound);
            // Bouncing BEST VALUE gold badge
            Image badgeImg = UIManager.CreateImage("BestValueBadge", cardGo.transform, UIStyle.Gold);
            badgeImg.raycastTarget = false;
            RectTransform badgeRect = badgeImg.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 0.79f); badgeRect.anchorMax = new Vector2(0.35f, 0.99f);
            badgeRect.offsetMin = new Vector2(10f, 0f); badgeRect.offsetMax = Vector2.zero;
            Text badgeText = UIManager.CreateText("BestValueText", badgeImg.transform, font, 13, TextAnchor.MiddleCenter, new Color(0.1f, 0.04f, 0f, 1f));
            badgeText.fontStyle = FontStyle.Bold; badgeText.text = "BEST VALUE";
            UIManager.Stretch(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            StartCoroutine(UIStyle.Bounce(badgeRect, 4f, 1.3f));
            Button cardBtn = cardGo.AddComponent<Button>(); cardBtn.transition = Selectable.Transition.None;
            UIManager.BindButton(cardBtn, () => { StartCoroutine(UIStyle.BuyButtonTap(cardRect)); _onItemSelected?.Invoke(ShopCatalogType.Coin, capturedId); }, _buttonClickSound);
        }

        // Other items at 85% opacity, orange buy button
        private void SpawnRegularShopItem(Transform list, CoinPackOffer offer, Font font, int rank)
        {
            float height = offer.kind == StoreOfferKind.NoAds ? 100f : offer.kind == StoreOfferKind.ExclusiveBundle ? 128f : offer.featured ? 148f : 116f;
            GameObject rowGo = new("ShopItem_" + rank); rowGo.transform.SetParent(list, false);
            LayoutElement le = rowGo.AddComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height;
            RectTransform rowRect = EnsureRectTransform(rowGo);
            rowRect.anchorMin = new Vector2(0f, 1f); rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f); rowRect.sizeDelta = new Vector2(0f, height);
            Color cardColor = GetCoinOfferCardColor(offer); cardColor.a = 0.85f;
            Image cardBg = rowGo.AddComponent<Image>(); cardBg.color = cardColor; cardBg.raycastTarget = false;
            Texture2D previewTex = GetCoinPackPreviewTexture(offer);
            GameObject previewGo = new("CoinPreview"); previewGo.transform.SetParent(rowGo.transform, false);
            RawImage previewImg = previewGo.AddComponent<RawImage>();
            previewImg.texture = previewTex != null ? previewTex : Texture2D.whiteTexture; previewImg.color = Color.white; previewImg.raycastTarget = false;
            RectTransform previewRect = (RectTransform)previewGo.transform;
            previewRect.anchorMin = new Vector2(0f, 0f); previewRect.anchorMax = new Vector2(0f, 1f);
            previewRect.offsetMin = new Vector2(12f, 10f); previewRect.offsetMax = new Vector2(96f, -10f);
            Text primaryLabel = UIManager.CreateText("PrimaryLabel", rowGo.transform, font, GetCoinOfferPrimaryFontSize(offer), TextAnchor.MiddleLeft, Color.black);
            primaryLabel.fontStyle = FontStyle.Bold; primaryLabel.text = GetCoinOfferPrimaryText(offer);
            RectTransform primaryRect = primaryLabel.rectTransform;
            primaryRect.anchorMin = new Vector2(0f, 0.46f); primaryRect.anchorMax = new Vector2(0.68f, 0.92f);
            primaryRect.offsetMin = new Vector2(108f, 0f); primaryRect.offsetMax = new Vector2(-8f, 0f);
            Text secondaryLabel = UIManager.CreateText("SecondaryLabel", rowGo.transform, font, 14, TextAnchor.UpperLeft, new Color(0.28f, 0.24f, 0.12f, 1f));
            secondaryLabel.text = GetCoinOfferDetail(offer);
            RectTransform secondaryRect = secondaryLabel.rectTransform;
            secondaryRect.anchorMin = new Vector2(0f, 0.06f); secondaryRect.anchorMax = new Vector2(0.68f, 0.46f);
            secondaryRect.offsetMin = new Vector2(108f, 4f); secondaryRect.offsetMax = new Vector2(-8f, 0f);
            // Buy button: UIStyle.Action (orange) or Owned (UIStyle.Owned green text)
            Button buyBtn = UIManager.CreateButton("BuyButton", rowGo.transform, font,
                offer.owned ? "OWNED" : offer.priceLabel,
                offer.owned ? UIStyle.SurfaceDark : UIStyle.Action,
                offer.owned ? UIStyle.Owned : Color.white);
            UIManager.StyleButtonLabel(buyBtn, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            RectTransform buyRect = (RectTransform)buyBtn.transform;
            buyRect.anchorMin = new Vector2(0.72f, 0.18f); buyRect.anchorMax = new Vector2(0.97f, 0.82f);
            buyRect.offsetMin = Vector2.zero; buyRect.offsetMax = Vector2.zero;
            buyBtn.interactable = offer.productType == ProductType.Consumable || !offer.owned;
            string capturedId = offer.id;
            UIManager.BindButton(buyBtn, () => { StartCoroutine(UIStyle.BuyButtonTap(buyRect)); _onItemSelected?.Invoke(ShopCatalogType.Coin, capturedId); }, _buttonClickSound);
            if (!string.IsNullOrWhiteSpace(offer.badgeLabel))
            {
                bool isBest = offer.badgeLabel.Contains("BEST");
                Image badgeImg = UIManager.CreateImage("BadgePill", rowGo.transform, isBest ? UIStyle.Action : new Color(0.06f, 0.55f, 0.35f, 1f));
                badgeImg.raycastTarget = false;
                RectTransform badgeRect = badgeImg.rectTransform;
                badgeRect.anchorMin = new Vector2(0f, 0.76f); badgeRect.anchorMax = new Vector2(0.35f, 0.97f);
                badgeRect.offsetMin = new Vector2(10f, 0f); badgeRect.offsetMax = Vector2.zero;
                Text badgeText = UIManager.CreateText("BadgeText", badgeImg.transform, font, 11, TextAnchor.MiddleCenter, Color.white);
                badgeText.fontStyle = FontStyle.Bold; badgeText.text = offer.badgeLabel;
                UIManager.Stretch(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            }
            Button rowBtn = rowGo.AddComponent<Button>(); rowBtn.transition = Selectable.Transition.None;
            UIManager.BindButton(rowBtn, () => { StartCoroutine(UIStyle.ButtonPress(rowRect)); _onItemSelected?.Invoke(ShopCatalogType.Coin, capturedId); }, _buttonClickSound);
        }

        // 2-column GridLayoutGroup with SpawnSkinCard
        private void BuildSkinsGrid(Transform list, Font font)
        {
            GameObject gridGo = new("SkinsGrid"); gridGo.transform.SetParent(list, false);
            RectTransform gridRect = EnsureRectTransform(gridGo);
            gridRect.anchorMin = Vector2.zero; gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = Vector2.zero; gridRect.offsetMax = Vector2.zero;
            LayoutElement gridLE = gridGo.AddComponent<LayoutElement>(); gridLE.flexibleWidth = 1f;
            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(340f, 380f); 
            grid.spacing = new Vector2(40f, 40f);
            grid.padding = new RectOffset(20, 20, 20, 20); 
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2; 
            grid.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter gridFitter = gridGo.AddComponent<ContentSizeFitter>();
            gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; gridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            if (_currentTab == 1)
            {
                if (_economyManager == null) return;
                var sorted = _ballSkins.OrderBy(s => _economyManager.IsOwnedSkin(s.id) ? 0 : 1).ThenBy(s => s.priceEmber).ToList();
                foreach (var skin in sorted) SpawnSkinCard(gridGo.transform, skin, font);
            }
            else
            {
                if (_economyManager == null) return;
                var sorted = _towerSkins.OrderBy(s => _economyManager.IsOwnedTowerSkin(s.id) ? 0 : 1).ThenBy(s => s.priceEmber).ToList();
                foreach (var skin in sorted) SpawnSkinCard(gridGo.transform, skin, font);
            }
        }

        // Ball skin card: selected = Brand border + glow + 1.05 scale, locked = 40% alpha
        private void SpawnSkinCard(Transform parent, BallSkinDefinition skin, Font font)
        {
            if (_economyManager == null) return;
            bool owned = _economyManager.IsOwnedSkin(skin.id);
            bool equipped = _economyManager.EquippedSkinId == skin.id;
            GameObject cardGo = new("SkinCard_" + skin.id); cardGo.transform.SetParent(parent, false);
            Image cardBg = cardGo.AddComponent<Image>();
            cardBg.color = equipped ? new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.22f) : UIStyle.SurfaceDark;
            RectTransform cardRect = (RectTransform)cardGo.transform;
            if (equipped)
            {
                Outline border = cardGo.AddComponent<Outline>(); border.effectColor = UIStyle.Brand; border.effectDistance = new Vector2(2f, 2f);
                Image glowImg = UIStyle.CreateGlow(cardRect, new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.35f), 10f);
                glowImg.raycastTarget = false; StartCoroutine(UIStyle.GlowPulse(glowImg, UIStyle.Brand, 0.15f, 0.5f, 1.8f));
                cardGo.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            }
            if (!owned) { CanvasGroup cg = cardGo.AddComponent<CanvasGroup>(); cg.alpha = 0.4f; }
            Texture2D tex = BallSkinTextureLibrary.LoadTexture(skin.baseMapResourcePath);
            GameObject previewGo = new("Preview"); previewGo.transform.SetParent(cardGo.transform, false);
            RawImage preview = previewGo.AddComponent<RawImage>();
            preview.texture = tex != null ? tex : Texture2D.whiteTexture; preview.color = tex != null ? Color.white : skin.baseColor;
            preview.uvRect = new Rect(0f, 0f, Mathf.Max(1f, skin.textureScale.x), Mathf.Max(1f, skin.textureScale.y)); preview.raycastTarget = false;
            RectTransform previewRect = (RectTransform)previewGo.transform;
            previewRect.anchorMin = new Vector2(0f, 0.3f); previewRect.anchorMax = new Vector2(1f, 1f);
            previewRect.offsetMin = new Vector2(12f, 0f); previewRect.offsetMax = new Vector2(-12f, -8f);
            Text nameLabel = UIManager.CreateText("SkinName", cardGo.transform, font, 14, TextAnchor.LowerCenter, equipped ? UIStyle.Brand : UIStyle.TextPrimary);
            nameLabel.fontStyle = FontStyle.Bold; nameLabel.text = skin.displayName.ToUpperInvariant();
            RectTransform nameLabelRect = nameLabel.rectTransform;
            nameLabelRect.anchorMin = new Vector2(0f, 0f); nameLabelRect.anchorMax = new Vector2(1f, 0.3f);
            nameLabelRect.offsetMin = new Vector2(4f, 2f); nameLabelRect.offsetMax = new Vector2(-4f, -2f);
            if (owned)
            {
                Image statusPill = UIManager.CreateImage("StatusPill", cardGo.transform, UIStyle.Owned); statusPill.raycastTarget = false;
                RectTransform statusRect = statusPill.rectTransform;
                statusRect.anchorMin = new Vector2(0.1f, 0.88f); statusRect.anchorMax = new Vector2(0.9f, 0.98f);
                statusRect.offsetMin = Vector2.zero; statusRect.offsetMax = Vector2.zero;
                Text statusText = UIManager.CreateText("StatusText", statusPill.transform, font, 11, TextAnchor.MiddleCenter, Color.white);
                statusText.fontStyle = FontStyle.Bold; statusText.text = equipped ? "EQUIPPED" : "OWNED";
                UIManager.Stretch(statusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
            }
            else
            {
                string priceStr = !string.IsNullOrWhiteSpace(skin.iapProductId) ? "IAP" : skin.priceEmber + " COIN";
                Image pricePill = UIManager.CreateImage("PricePill", cardGo.transform, UIStyle.Action); pricePill.raycastTarget = false;
                RectTransform priceRect = pricePill.rectTransform;
                priceRect.anchorMin = new Vector2(0.05f, 0.87f); priceRect.anchorMax = new Vector2(0.95f, 0.98f);
                priceRect.offsetMin = Vector2.zero; priceRect.offsetMax = Vector2.zero;
                Text priceText = UIManager.CreateText("PriceText", pricePill.transform, font, 11, TextAnchor.MiddleCenter, Color.white);
                priceText.fontStyle = FontStyle.Bold; priceText.text = priceStr;
                UIManager.Stretch(priceText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
            }
            Button btn = cardGo.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            bool canAfford = owned || !string.IsNullOrWhiteSpace(skin.iapProductId) || (_economyManager != null && _economyManager.EmberBalance >= skin.priceEmber);
            btn.interactable = canAfford;
            string capturedId = skin.id;
            UIManager.BindButton(btn, () => { StartCoroutine(UIStyle.ButtonPress(cardRect)); _onItemSelected?.Invoke(ShopCatalogType.Ball, capturedId); }, _buttonClickSound);
        }

        // Tower skin card: selected = Brand border + glow + 1.05 scale, locked = 40% alpha
        private void SpawnSkinCard(Transform parent, TowerSkinDefinition skin, Font font)
        {
            if (_economyManager == null) return;
            bool owned = _economyManager.IsOwnedTowerSkin(skin.id);
            bool equipped = _economyManager.EquippedTowerSkinId == skin.id;
            GameObject cardGo = new("TowerCard_" + skin.id); cardGo.transform.SetParent(parent, false);
            Image cardBg = cardGo.AddComponent<Image>();
            cardBg.color = equipped ? new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.22f) : UIStyle.SurfaceDark;
            RectTransform cardRect = (RectTransform)cardGo.transform;
            if (equipped)
            {
                Outline border = cardGo.AddComponent<Outline>(); border.effectColor = UIStyle.Brand; border.effectDistance = new Vector2(2f, 2f);
                Image glowImg = UIStyle.CreateGlow(cardRect, new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.35f), 10f);
                glowImg.raycastTarget = false; StartCoroutine(UIStyle.GlowPulse(glowImg, UIStyle.Brand, 0.15f, 0.5f, 1.8f));
                cardGo.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            }
            if (!owned) { CanvasGroup cg = cardGo.AddComponent<CanvasGroup>(); cg.alpha = 0.4f; }
            Texture preview = BallSkinTextureLibrary.LoadTexture(skin.wallBaseMapResourcePath);
            if (preview == null && _theme != null) preview = _theme.towerWallBaseMap;
            GameObject previewGo = new("Preview"); previewGo.transform.SetParent(cardGo.transform, false);
            RawImage previewImg = previewGo.AddComponent<RawImage>();
            previewImg.texture = preview != null ? preview : Texture2D.whiteTexture; previewImg.color = preview != null ? Color.white : skin.mainPathTint;
            previewImg.uvRect = new Rect(0f, 0f, Mathf.Max(1f, skin.wallTextureScale.x), Mathf.Max(1f, skin.wallTextureScale.y)); previewImg.raycastTarget = false;
            RectTransform previewRect = (RectTransform)previewGo.transform;
            previewRect.anchorMin = new Vector2(0f, 0.3f); previewRect.anchorMax = new Vector2(1f, 1f);
            previewRect.offsetMin = new Vector2(12f, 0f); previewRect.offsetMax = new Vector2(-12f, -8f);
            Text nameLabel = UIManager.CreateText("SkinName", cardGo.transform, font, 14, TextAnchor.LowerCenter, equipped ? UIStyle.Brand : UIStyle.TextPrimary);
            nameLabel.fontStyle = FontStyle.Bold; nameLabel.text = skin.displayName.ToUpperInvariant();
            RectTransform nameLabelRect = nameLabel.rectTransform;
            nameLabelRect.anchorMin = new Vector2(0f, 0f); nameLabelRect.anchorMax = new Vector2(1f, 0.3f);
            nameLabelRect.offsetMin = new Vector2(4f, 2f); nameLabelRect.offsetMax = new Vector2(-4f, -2f);
            if (owned)
            {
                Image statusPill = UIManager.CreateImage("StatusPill", cardGo.transform, UIStyle.Owned); statusPill.raycastTarget = false;
                RectTransform statusRect = statusPill.rectTransform;
                statusRect.anchorMin = new Vector2(0.1f, 0.88f); statusRect.anchorMax = new Vector2(0.9f, 0.98f);
                statusRect.offsetMin = Vector2.zero; statusRect.offsetMax = Vector2.zero;
                Text statusText = UIManager.CreateText("StatusText", statusPill.transform, font, 11, TextAnchor.MiddleCenter, Color.white);
                statusText.fontStyle = FontStyle.Bold; statusText.text = equipped ? "EQUIPPED" : "OWNED";
                UIManager.Stretch(statusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
            }
            else
            {
                string priceStr = !string.IsNullOrWhiteSpace(skin.iapProductId) ? "IAP" : skin.priceEmber + " COIN";
                Image pricePill = UIManager.CreateImage("PricePill", cardGo.transform, UIStyle.Action); pricePill.raycastTarget = false;
                RectTransform priceRect = pricePill.rectTransform;
                priceRect.anchorMin = new Vector2(0.05f, 0.87f); priceRect.anchorMax = new Vector2(0.95f, 0.98f);
                priceRect.offsetMin = Vector2.zero; priceRect.offsetMax = Vector2.zero;
                Text priceText = UIManager.CreateText("PriceText", pricePill.transform, font, 11, TextAnchor.MiddleCenter, Color.white);
                priceText.fontStyle = FontStyle.Bold; priceText.text = priceStr;
                UIManager.Stretch(priceText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
            }
            Button btn = cardGo.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            bool canAfford = owned || !string.IsNullOrWhiteSpace(skin.iapProductId) || (_economyManager != null && _economyManager.EmberBalance >= skin.priceEmber);
            btn.interactable = canAfford;
            string capturedId = skin.id;
            UIManager.BindButton(btn, () => { StartCoroutine(UIStyle.ButtonPress(cardRect)); _onItemSelected?.Invoke(ShopCatalogType.Tower, capturedId); }, _buttonClickSound);
        }

        internal static Texture2D GetCoinPackPreviewTexture(CoinPackOffer offer)
        {
            if (offer.kind == StoreOfferKind.NoAds) return LoadTex(ref _texNoAds, "TowerMaze/ShopIcons/no_ads_icon");
            if (offer.kind == StoreOfferKind.WelcomePack) return LoadTex(ref _texChest, "TowerMaze/CoinArt/HQ/coin_hq_chest_gold");
            if (offer.kind == StoreOfferKind.ExclusiveBundle) return LoadTex(ref _texBag, "TowerMaze/CoinArt/HQ/coin_hq_bag");
            if (offer.featured || offer.coinAmount >= 100000) return LoadTex(ref _texChest, "TowerMaze/CoinArt/HQ/coin_hq_chest_gold");
            if (offer.coinAmount >= 25000) return LoadTex(ref _texBag, "TowerMaze/CoinArt/HQ/coin_hq_bag");
            if (offer.coinAmount >= 5000) return LoadTex(ref _texStack, "TowerMaze/CoinArt/HQ/coin_hq_stack");
            return LoadTex(ref _texSingle, "TowerMaze/CoinArt/HQ/coin_hq_single");
        }

        private static Texture2D LoadTex(ref Texture2D cache, string path) { if (cache == null) cache = Resources.Load<Texture2D>(path); return cache; }

        internal static Color GetCoinOfferCardColor(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack     => new Color(0.99f, 0.92f, 0.70f, 0.99f),
                StoreOfferKind.NoAds           => new Color(0.92f, 0.98f, 0.92f, 0.99f),
                StoreOfferKind.ExclusiveBundle => new Color(0.95f, 0.90f, 0.98f, 0.99f),
                _ => offer.featured ? new Color(0.98f, 0.93f, 0.70f, 0.98f) : new Color(0.97f, 0.95f, 0.84f, 0.98f),
            };
        }

        internal static Color GetCoinOfferPreviewFrameColor(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack     => new Color(0.95f, 0.85f, 0.50f, 1f),
                StoreOfferKind.NoAds           => new Color(0.85f, 0.95f, 0.85f, 1f),
                StoreOfferKind.ExclusiveBundle => new Color(0.90f, 0.85f, 0.95f, 1f),
                _ => offer.featured ? new Color(0.95f, 0.88f, 0.55f, 1f) : new Color(0.94f, 0.92f, 0.78f, 1f),
            };
        }

        internal static Color GetCoinOfferPreviewTint(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.NoAds => new Color(0.88f, 0.92f, 1f, 1f),
                _ => offer.featured || offer.kind == StoreOfferKind.WelcomePack ? Color.white : new Color(1f, 1f, 1f, 0.95f),
            };
        }

        private static string GetCoinOfferPrimaryText(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack     => "WELCOME PACK",
                StoreOfferKind.NoAds           => "NO ADS",
                StoreOfferKind.ExclusiveBundle => offer.displayName,
                _ => FormatCoinAmount(offer.coinAmount) + " COIN",
            };
        }

        private static int GetCoinOfferPrimaryFontSize(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack     => 26,
                StoreOfferKind.NoAds           => 24,
                StoreOfferKind.ExclusiveBundle => 21,
                _ => offer.featured ? 28 : 24,
            };
        }

        private static string GetCoinOfferDetail(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack     => "+" + offer.coinAmount + " COIN\n" + offer.bonusLabel,
                StoreOfferKind.NoAds           => "REMOVE POPUP ADS FOREVER\nKEEP OPTIONAL REWARD ADS",
                StoreOfferKind.ExclusiveBundle => "+" + offer.coinAmount + " COIN\n" + offer.bonusLabel,
                _ => string.IsNullOrWhiteSpace(offer.bonusLabel) ? offer.displayName : offer.displayName + "\n" + offer.bonusLabel,
            };
        }

        private static string FormatCoinAmount(int amount)
        {
            string digits = Mathf.Max(amount, 0).ToString();
            if (digits.Length <= 3) return digits;
            var sb = new System.Text.StringBuilder();
            int groupCount = 0;
            for (int i = digits.Length - 1; i >= 0; i--) { if (groupCount == 3) { sb.Insert(0, ' '); groupCount = 0; } sb.Insert(0, digits[i]); groupCount++; }
            return sb.ToString();
        }

        private RectTransform EnsureRectTransform(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }
    }
}
