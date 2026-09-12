using Godot;
using System.Collections.Generic;
using System.Linq;
using GameHub.Core.Visuals;

namespace GameHub.Games.PokerRoguelike;

/// <summary>Presentation for the Poker run. Game state remains in PokerGameManager.</summary>
public partial class PokerUI : Control
{
    private const float HandDealInterval = .12f;
    private const float HandDealFlight = .42f;
    private PokerGameManager _game;
    private Label _roundLabel, _scoreLabel, _targetLabel, _handsLabel, _discardsLabel;
    private Label _relicsLabel, _opponentsLabel, _resultLabel, _breakdownLabel, _walletLabel;
    private Label _bossName, _selectionLabel;
    private TextureRect _bossPortrait;
    private ProgressBar _scoreBar;
    private HBoxContainer _dealerCardContainer, _cardContainer;
    private Control _tableArea, _tableCardLayer;
    private Label _scorePopup;
    private PanelContainer _handCelebrationBanner;
    private Label _handTitleLabel, _handSubtitleLabel;
    private Button _playBtn, _discardBtn;
    private readonly List<PanelContainer> _cardPanels = new();
    private readonly List<PanelContainer> _dealerCardPanels = new();
    private bool _ignoreInput;
    private Tween _popupTween;
    private TableStage _stage;
    private int _presentedRound = -1;

    private Control _overlayPanel, _shopOverlay, _tutorialPanel, _hud;
    private Label _overlayTitle, _overlaySubtitle, _goldLabel, _shopMessage;
    private Button _nextRoundBtn, _newGameBtn, _backMenuBtn;
    private Button _buyMultBtn, _buyHandBtn, _buyDiscardBtn, _shopNextRoundBtn;
    private Label _tutorialTitle, _tutorialDesc;
    private Core.AI.TutorialController _tutorialController;

    // Balatro Shop State & Controls
    private readonly List<RelicManager.RelicId> _shopJokers = new();
    private readonly List<bool> _shopJokersPurchased = new();
    private string _shopVoucherType = "multiplier";
    private int _shopVoucherCost = 10;
    private bool _shopVoucherPurchased = false;
    private bool _shopBuffoonPackPurchased = false;
    private bool _shopCelestialPackPurchased = false;

    private Label _shopScoreValLabel;
    private Label _shopChipsLabel;
    private Label _shopMultLabel;
    private Label _shopHandsLabel;
    private Label _shopDiscardsLabel;
    private Label _shopAnteLabel;
    private Label _shopRoundLabel;
    private Label _shopDeckCountLabel;
    private Label _shopJokerSlotsLabel;
    private HBoxContainer _shopJokerSlotsContainer;
    private HBoxContainer _shopJokerCardsContainer;
    private Button _rerollBtn;
    private Label _voucherTitleLabel;
    private Label _voucherDescLabel;
    private Button _voucherBuyBtn;
    private Button _buffoonBuyBtn;
    private Button _celestialBuyBtn;

    public override void _Ready()
    {
        _game = GetNode<PokerGameManager>("../../GameManager");
        Theme = ClubTheme.Create();
        Core.Systems.AudioManager.Instance?.PlayMusic("velvet-table");
        BuildUI();
        _game.HandDealt += OnHandDealt;
        _game.ScoreUpdated += OnScoreUpdated;
        _game.HandScored += OnHandScored;
        _game.RoundEnded += OnRoundEnded;
        _game.GameEnded += OnGameEnded;
        _game.PhaseChanged += OnPhaseChanged;
        _game.StartNewGame();
        GenerateShopStock();
        // The manager adds this node deferred, so attach after it enters the tree.
        CallDeferred(nameof(AttachTutorial));
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_game))
        {
            _game.HandDealt -= OnHandDealt;
            _game.ScoreUpdated -= OnScoreUpdated;
            _game.HandScored -= OnHandScored;
            _game.RoundEnded -= OnRoundEnded;
            _game.GameEnded -= OnGameEnded;
            _game.PhaseChanged -= OnPhaseChanged;
        }
        if (IsInstanceValid(_tutorialController))
        {
            _tutorialController.TutorialStepChanged -= OnTutorialStep;
            _tutorialController.TutorialComplete -= OnTutorialComplete;
        }
    }

    private void BuildUI()
    {
        // 100% Fullscreen 3D TableStage for immersive view of the salon, boss, and cutscenes
        _stage = new TableStage { SeatCount = 2, RivalIndex = 2 };
        _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _stage.MouseFilter = MouseFilterEnum.Pass;
        AddChild(_stage);
        _tableArea = _stage;

        // Transparent HUD Overlay layer
        _hud = new Control { Name = "HudOverlay", MouseFilter = MouseFilterEnum.Ignore };
        _hud.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_hud);

        // TOP-LEFT: Compact Boss Info and Dealer Cards
        var topLeft = new MarginContainer();
        topLeft.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        topLeft.OffsetLeft = 24; topLeft.OffsetTop = 20;
        _hud.AddChild(topLeft);

        var dealerPanel = Panel(new Color(0.03f, 0.07f, 0.055f, 0.90f), ClubTheme.Border, 12);
        var dealerRow = new HBoxContainer();
        dealerRow.AddThemeConstantOverride("separation", 14);
        dealerPanel.AddChild(dealerRow);

        _bossPortrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(72, 72),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        var chroma = ResourceLoader.Load<Shader>("res://assets/shaders/CanvasChromaKey.gdshader");
        if (chroma != null)
            _bossPortrait.Material = new ShaderMaterial { Shader = chroma };
        dealerRow.AddChild(_bossPortrait);

        var bossInfo = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        bossInfo.AddThemeConstantOverride("separation", 3);
        bossInfo.AddChild(ClubTheme.Label("CHEFE DA MESA", 11, ClubTheme.Gold));
        _bossName = ClubTheme.Label("Tartaruga", 20);
        bossInfo.AddChild(_bossName);
        _opponentsLabel = ClubTheme.Label("1 bot à mesa · vença a meta", 12, ClubTheme.Muted);
        bossInfo.AddChild(_opponentsLabel);
        dealerRow.AddChild(bossInfo);
        dealerRow.AddChild(Spacer());

        var rack = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        rack.AddThemeConstantOverride("separation", 4);
        _dealerCardContainer = new HBoxContainer();
        _dealerCardContainer.AddThemeConstantOverride("separation", 6);
        rack.AddChild(_dealerCardContainer);
        var dealerHint = ClubTheme.Label("CARTAS DO CHEFE", 10, ClubTheme.Muted);
        dealerHint.HorizontalAlignment = HorizontalAlignment.Center;
        rack.AddChild(dealerHint);
        dealerRow.AddChild(rack);
        topLeft.AddChild(dealerPanel);

        // TOP-RIGHT / RIGHT SIDEBAR: Unified Run Status & Action Panel ("quadradão pro lado")
        var rightMargin = new MarginContainer();
        rightMargin.AnchorLeft = 1.0f;
        rightMargin.AnchorRight = 1.0f;
        rightMargin.AnchorTop = 0.0f;
        rightMargin.AnchorBottom = 1.0f;
        rightMargin.OffsetLeft = -310;
        rightMargin.OffsetRight = -20;
        rightMargin.OffsetTop = 20;
        rightMargin.OffsetBottom = -20;
        _hud.AddChild(rightMargin);

        var side = Panel(new Color(0.03f, 0.07f, 0.055f, 0.92f), ClubTheme.Border, 18);
        side.CustomMinimumSize = new Vector2(280, 0);
        rightMargin.AddChild(side);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        side.AddChild(box);

        box.AddChild(ClubTheme.Label("A SUA CORRIDA", 11, ClubTheme.Gold));
        _roundLabel = ClubTheme.Label("Rodada 1 / 8", 22);
        box.AddChild(_roundLabel);
        box.AddChild(Rule());

        box.AddChild(ClubTheme.Label("PONTOS NA MESA", 11, ClubTheme.Muted));
        _scoreLabel = ClubTheme.Label("0", 38, ClubTheme.Paper);
        _scoreLabel.AddThemeFontOverride("font", ClubTheme.MonoFont);
        box.AddChild(_scoreLabel);
        _targetLabel = ClubTheme.Label("META  300", 15, ClubTheme.Gold);
        box.AddChild(_targetLabel);
        _scoreBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 8),
            MinValue = 0, MaxValue = 300, ShowPercentage = false
        };
        _scoreBar.AddThemeStyleboxOverride("background", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Ink, 0, 3));
        _scoreBar.AddThemeStyleboxOverride("fill", ClubTheme.Box(ClubTheme.Gold, ClubTheme.Gold, 0, 3));
        box.AddChild(_scoreBar);
        box.AddChild(Rule());

        _handsLabel = ClubTheme.Label("MÃOS  4", 16);
        _discardsLabel = ClubTheme.Label("DESCARTES  3", 16);
        _walletLabel = ClubTheme.Label("FICHAS  0", 16, ClubTheme.Gold);
        box.AddChild(_handsLabel);
        box.AddChild(_discardsLabel);
        box.AddChild(_walletLabel);
        box.AddChild(Rule());

        box.AddChild(ClubTheme.Label("RELÍQUIAS ATIVAS", 11, ClubTheme.Gold));
        _relicsLabel = ClubTheme.Label("Ás de sorte\n+1 Mult ao jogar um Ás", 12, ClubTheme.Muted);
        _relicsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _relicsLabel.CustomMinimumSize = new Vector2(250, 38);
        box.AddChild(_relicsLabel);
        box.AddChild(Rule());

        // Combination evaluation and Actions
        _resultLabel = ClubTheme.Label("Escolha suas cartas", 16);
        box.AddChild(_resultLabel);
        _breakdownLabel = ClubTheme.Label("De 1 a 5 cartas para formar uma combinação.", 12, ClubTheme.Muted);
        _breakdownLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(_breakdownLabel);

        _selectionLabel = ClubTheme.Label("0 / 5 selecionadas", 12, ClubTheme.Gold);
        box.AddChild(_selectionLabel);

        _playBtn = ClubTheme.Button("Jogar mão", true);
        _playBtn.CustomMinimumSize = new Vector2(0, 46);
        _playBtn.Pressed += OnPlayPressed;
        box.AddChild(_playBtn);

        _discardBtn = ClubTheme.Button("Descartar");
        _discardBtn.CustomMinimumSize = new Vector2(0, 40);
        _discardBtn.Pressed += OnDiscardPressed;
        box.AddChild(_discardBtn);

        box.AddChild(Spacer());
        var exit = ClubTheme.Button("Voltar ao clube");
        exit.Pressed += ReturnToHub;
        box.AddChild(exit);

        // BOTTOM CENTER: Clean Floating Player Hand (no heavy opaque background tray)
        var bottomArea = new MarginContainer();
        bottomArea.AnchorLeft = 0.0f;
        bottomArea.AnchorRight = 1.0f;
        bottomArea.AnchorTop = 1.0f;
        bottomArea.AnchorBottom = 1.0f;
        bottomArea.OffsetLeft = 24;
        bottomArea.OffsetRight = -330; // Clear room for right-side action panel
        bottomArea.OffsetTop = -140;
        bottomArea.OffsetBottom = -16;
        bottomArea.MouseFilter = MouseFilterEnum.Ignore;
        _hud.AddChild(bottomArea);

        var cardCenter = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        _cardContainer = new HBoxContainer();
        _cardContainer.AddThemeConstantOverride("separation", 10);
        cardCenter.AddChild(_cardContainer);
        bottomArea.AddChild(cardCenter);

        // Overlay cards use global coordinates; layout controls remain in their containers.
        _tableCardLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _tableCardLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_tableCardLayer);
        _scorePopup = ClubTheme.Label("", 24, ClubTheme.Gold);
        _scorePopup.HorizontalAlignment = HorizontalAlignment.Center;
        _scorePopup.MouseFilter = MouseFilterEnum.Ignore;
        _scorePopup.Visible = false;
        _tableCardLayer.AddChild(_scorePopup);

        _handCelebrationBanner = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _handCelebrationBanner.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.03f, 0.06f, 0.05f, 0.95f), ClubTheme.Gold, 18, 12));
        var bannerBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        bannerBox.AddThemeConstantOverride("separation", 6);
        _handCelebrationBanner.AddChild(bannerBox);

        _handTitleLabel = DisplayLabel("", 34);
        _handTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        bannerBox.AddChild(_handTitleLabel);

        _handSubtitleLabel = ClubTheme.Label("", 17, ClubTheme.Gold);
        _handSubtitleLabel.AddThemeFontOverride("font", ClubTheme.MonoFont);
        _handSubtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        bannerBox.AddChild(_handSubtitleLabel);
        _tableCardLayer.AddChild(_handCelebrationBanner);

        BuildResultOverlay();
        BuildShopOverlay();
        BuildTutorialPanel();
        AccessibilityVisuals.AddGlobalFilter(this);
    }

    private void BuildResultOverlay()
    {
        var box = CreateModal(out _overlayPanel, new Vector2(580, 0));
        box.AddChild(ClubTheme.Label("FIM DA RODADA", 12, ClubTheme.Gold));
        _overlayTitle = DisplayLabel("", 42);
        _overlaySubtitle = ClubTheme.Label("", 17, ClubTheme.Muted);
        _overlaySubtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(_overlayTitle);
        box.AddChild(_overlaySubtitle);
        box.AddChild(Rule());
        _nextRoundBtn = ClubTheme.Button("Visitar a loja", true);
        _nextRoundBtn.Pressed += () =>
        {
            _overlayPanel.Visible = false;
            if (_hud != null) _hud.Visible = false;
            RefreshShopUI();
            _shopOverlay.Visible = true;
            _shopNextRoundBtn.GrabFocus();
        };
        box.AddChild(_nextRoundBtn);
        _newGameBtn = ClubTheme.Button("Começar nova corrida", true);
        _newGameBtn.Pressed += () =>
        {
            _overlayPanel.Visible = false;
            _shopOverlay.Visible = false;
            if (_hud != null) _hud.Visible = true;
            _game.StartNewGame();
        };
        box.AddChild(_newGameBtn);
        _backMenuBtn = ClubTheme.Button("Voltar ao clube");
        _backMenuBtn.Pressed += ReturnToHub;
        box.AddChild(_backMenuBtn);
    }

    private void GenerateShopStock()
    {
        _shopJokers.Clear();
        _shopJokersPurchased.Clear();
        var allRelics = System.Enum.GetValues<RelicManager.RelicId>();
        var active = _game?.Relics?.ActiveRelics ?? (IReadOnlyList<RelicManager.RelicId>)System.Array.Empty<RelicManager.RelicId>();
        var available = allRelics.Where(r => !active.Contains(r)).ToList();

        var rnd = new System.Random();
        available = available.OrderBy(_ => rnd.Next()).ToList();
        for (int i = 0; i < Mathf.Min(2, available.Count); i++)
        {
            _shopJokers.Add(available[i]);
            _shopJokersPurchased.Add(false);
        }

        string[] vouchers = { "multiplier", "hand", "discard" };
        _shopVoucherType = vouchers[rnd.Next(vouchers.Length)];
        _shopVoucherCost = _shopVoucherType == "hand" ? 15 : (_shopVoucherType == "multiplier" ? 10 : 8);
        _shopVoucherPurchased = false;
        _shopBuffoonPackPurchased = false;
        _shopCelestialPackPurchased = false;
    }

    private void BuildShopOverlay()
    {
        var box = CreateModal(out _shopOverlay, new Vector2(1060, 560));
        box.AddThemeConstantOverride("separation", 8);

        var topHeader = new HBoxContainer();
        topHeader.AddChild(ClubTheme.Label("MERCADO DE CORINGAS & MELHORIAS", 11, ClubTheme.Gold));
        topHeader.AddChild(Spacer());
        topHeader.AddChild(ClubTheme.Label("BALATRO ROGUELIKE EDITION", 11, ClubTheme.Muted));
        box.AddChild(topHeader);

        var shopBody = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        shopBody.AddThemeConstantOverride("separation", 16);
        box.AddChild(shopBody);

        // ==================== LEFT SIDEBAR ====================
        var sidebar = new VBoxContainer { CustomMinimumSize = new Vector2(230, 0) };
        sidebar.AddThemeConstantOverride("separation", 8);
        shopBody.AddChild(sidebar);

        // Neon SHOP Sign
        var neonPanel = new PanelContainer();
        neonPanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color("#1c090c"), new Color("#ff334b"), 8, 8));
        sidebar.AddChild(neonPanel);
        var neonBox = new VBoxContainer();
        neonBox.AddThemeConstantOverride("separation", 0);
        neonPanel.AddChild(neonBox);
        var neonTitle = DisplayLabel("SHOP", 34);
        neonTitle.HorizontalAlignment = HorizontalAlignment.Center;
        neonTitle.Modulate = new Color("#ffea79");
        neonBox.AddChild(neonTitle);
        var neonSub = ClubTheme.Label("Improve your run!", 11, new Color("#ffcf70"));
        neonSub.HorizontalAlignment = HorizontalAlignment.Center;
        neonBox.AddChild(neonSub);

        // Round score pill
        var scorePill = new PanelContainer();
        scorePill.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.04f, 0.08f, 0.12f), new Color("#1b4965"), 6, 4));
        sidebar.AddChild(scorePill);
        var scorePillBox = new HBoxContainer();
        scorePillBox.AddChild(ClubTheme.Label("Pontuação:", 11, ClubTheme.Muted));
        scorePillBox.AddChild(Spacer());
        _shopScoreValLabel = ClubTheme.Label("0", 12, ClubTheme.Gold);
        scorePillBox.AddChild(_shopScoreValLabel);
        scorePill.AddChild(scorePillBox);

        // Formula Box Chips x Mult
        var formulaRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        formulaRow.AddThemeConstantOverride("separation", 8);
        sidebar.AddChild(formulaRow);

        var chipBox = new PanelContainer { CustomMinimumSize = new Vector2(70, 32) };
        chipBox.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color("#154360"), new Color("#2980b9"), 4, 4));
        _shopChipsLabel = ClubTheme.Label("50", 14, Colors.White);
        _shopChipsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _shopChipsLabel.VerticalAlignment = VerticalAlignment.Center;
        chipBox.AddChild(_shopChipsLabel);
        formulaRow.AddChild(chipBox);

        var multSign = ClubTheme.Label("×", 16, Colors.White);
        multSign.VerticalAlignment = VerticalAlignment.Center;
        formulaRow.AddChild(multSign);

        var multBox = new PanelContainer { CustomMinimumSize = new Vector2(70, 32) };
        multBox.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color("#78281f"), new Color("#c0392b"), 4, 4));
        _shopMultLabel = ClubTheme.Label("1", 14, Colors.White);
        _shopMultLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _shopMultLabel.VerticalAlignment = VerticalAlignment.Center;
        multBox.AddChild(_shopMultLabel);
        formulaRow.AddChild(multBox);

        // Hands & Discards
        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", 8);
        sidebar.AddChild(statsRow);
        var handsPill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        handsPill.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.08f, 0.16f, 0.24f), new Color("#2471a3"), 5, 4));
        _shopHandsLabel = ClubTheme.Label("Mãos: 4", 11, new Color("#aed6f1"));
        _shopHandsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        handsPill.AddChild(_shopHandsLabel);
        statsRow.AddChild(handsPill);

        var discardsPill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        discardsPill.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.24f, 0.08f, 0.10f), new Color("#c0392b"), 5, 4));
        _shopDiscardsLabel = ClubTheme.Label("Desc: 2", 11, new Color("#f5b7b1"));
        _shopDiscardsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        discardsPill.AddChild(_shopDiscardsLabel);
        statsRow.AddChild(discardsPill);

        // Money
        var moneyPill = new PanelContainer();
        moneyPill.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.04f, 0.08f, 0.05f), ClubTheme.Border, 8, 6));
        sidebar.AddChild(moneyPill);
        var moneyBox = new HBoxContainer();
        moneyBox.AddChild(ClubTheme.Label("FICHAS:", 11, ClubTheme.Muted));
        moneyBox.AddChild(Spacer());
        _goldLabel = ClubTheme.Label("$ 0", 22, ClubTheme.Gold);
        _goldLabel.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        moneyBox.AddChild(_goldLabel);
        moneyPill.AddChild(moneyBox);

        // Ante & Round
        var anteRow = new HBoxContainer();
        anteRow.AddThemeConstantOverride("separation", 8);
        sidebar.AddChild(anteRow);
        var antePill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        antePill.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.20f, 0.12f, 0.04f), new Color("#d35400"), 5, 4));
        _shopAnteLabel = ClubTheme.Label("Ante: 1 / 8", 11, new Color("#f8c471"));
        _shopAnteLabel.HorizontalAlignment = HorizontalAlignment.Center;
        antePill.AddChild(_shopAnteLabel);
        anteRow.AddChild(antePill);

        var roundPill = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        roundPill.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.20f, 0.12f, 0.04f), new Color("#d35400"), 5, 4));
        _shopRoundLabel = ClubTheme.Label("Rodada: 1", 11, new Color("#f8c471"));
        _shopRoundLabel.HorizontalAlignment = HorizontalAlignment.Center;
        roundPill.AddChild(_shopRoundLabel);
        anteRow.AddChild(roundPill);

        sidebar.AddChild(Spacer());

        var runInfoBtn = ClubTheme.Button("Info da Corrida");
        runInfoBtn.CustomMinimumSize = new Vector2(0, 32);
        runInfoBtn.Pressed += ShowRunInfoPopup;
        sidebar.AddChild(runInfoBtn);

        var optionsBtn = ClubTheme.Button("Ajustes [Esc]");
        optionsBtn.CustomMinimumSize = new Vector2(0, 32);
        optionsBtn.Pressed += () => _stage?.ToggleInGameSettings();
        sidebar.AddChild(optionsBtn);

        // ==================== RIGHT SHOP MAIN ====================
        var mainShop = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        mainShop.AddThemeConstantOverride("separation", 8);
        shopBody.AddChild(mainShop);

        // Slots Top Row
        var slotsHeaderRow = new HBoxContainer();
        _shopJokerSlotsLabel = ClubTheme.Label("CORINGAS (0 / 5)", 11, ClubTheme.Gold);
        slotsHeaderRow.AddChild(_shopJokerSlotsLabel);
        slotsHeaderRow.AddChild(Spacer());
        slotsHeaderRow.AddChild(ClubTheme.Label("CONSUMÍVEIS (0 / 2)", 11, ClubTheme.Muted));
        mainShop.AddChild(slotsHeaderRow);

        var slotsRow = new HBoxContainer();
        slotsRow.AddThemeConstantOverride("separation", 8);
        mainShop.AddChild(slotsRow);

        _shopJokerSlotsContainer = new HBoxContainer();
        _shopJokerSlotsContainer.AddThemeConstantOverride("separation", 8);
        slotsRow.AddChild(_shopJokerSlotsContainer);

        slotsRow.AddChild(Spacer());

        for (int c = 0; c < 2; c++)
        {
            var consSlot = new PanelContainer { CustomMinimumSize = new Vector2(64, 76) };
            consSlot.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.04f, 0.07f, 0.06f, 0.5f), ClubTheme.Border, 4, 6));
            var lbl = ClubTheme.Label("Slot", 10, ClubTheme.Muted);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.VerticalAlignment = VerticalAlignment.Center;
            consSlot.AddChild(lbl);
            slotsRow.AddChild(consSlot);
        }

        // Center Shop Felt Panel
        var shopTable = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        shopTable.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.035f, 0.065f, 0.05f), new Color("#9b2226"), 12, 8));
        mainShop.AddChild(shopTable);

        var shopTableHBox = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        shopTableHBox.AddThemeConstantOverride("separation", 14);
        shopTable.AddChild(shopTableHBox);

        // Shop Action Buttons Column
        var shopActionsCol = new VBoxContainer { CustomMinimumSize = new Vector2(150, 0) };
        shopActionsCol.AddThemeConstantOverride("separation", 10);
        shopTableHBox.AddChild(shopActionsCol);

        _shopNextRoundBtn = ClubTheme.Button("Próxima rodada  →", true);
        _shopNextRoundBtn.CustomMinimumSize = new Vector2(150, 52);
        _shopNextRoundBtn.Pressed += () =>
        {
            _shopOverlay.Visible = false;
            if (_hud != null) _hud.Visible = true;
            _game.StartNextRound();
        };
        shopActionsCol.AddChild(_shopNextRoundBtn);

        _rerollBtn = ClubTheme.Button("Reroll  $ 5");
        _rerollBtn.CustomMinimumSize = new Vector2(150, 42);
        _rerollBtn.Pressed += OnRerollPressed;
        shopActionsCol.AddChild(_rerollBtn);

        shopActionsCol.AddChild(Spacer());

        // Center Items Grid
        var itemsCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        itemsCol.AddThemeConstantOverride("separation", 10);
        shopTableHBox.AddChild(itemsCol);

        // Row 1: Jokers for sale
        _shopJokerCardsContainer = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _shopJokerCardsContainer.AddThemeConstantOverride("separation", 12);
        itemsCol.AddChild(_shopJokerCardsContainer);

        itemsCol.AddChild(Rule());

        // Row 2: Vouchers & Packs
        var lowerShopRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        lowerShopRow.AddThemeConstantOverride("separation", 12);
        itemsCol.AddChild(lowerShopRow);

        // Voucher Card
        var voucherPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        voucherPanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.04f, 0.08f, 0.10f), ClubTheme.Border, 8, 6));
        lowerShopRow.AddChild(voucherPanel);
        var voucherVBox = new VBoxContainer();
        voucherVBox.AddThemeConstantOverride("separation", 4);
        voucherPanel.AddChild(voucherVBox);
        var voucherTag = ClubTheme.Label("CUPOM DO ANTE · $ 10", 10, new Color("#5dade2"));
        voucherVBox.AddChild(voucherTag);
        _voucherTitleLabel = ClubTheme.Label("+1 Multiplicador", 13, ClubTheme.Paper);
        voucherVBox.AddChild(_voucherTitleLabel);
        _voucherDescLabel = ClubTheme.Label("Bônus permanente na corrida.", 10, ClubTheme.Muted);
        _voucherDescLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        voucherVBox.AddChild(_voucherDescLabel);
        _voucherBuyBtn = ClubTheme.Button("Comprar · $ 10");
        _voucherBuyBtn.CustomMinimumSize = new Vector2(0, 30);
        _voucherBuyBtn.Pressed += BuyVoucherOffer;
        voucherVBox.AddChild(_voucherBuyBtn);

        // Buffoon Pack
        var buffoonPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buffoonPanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.10f, 0.05f, 0.06f), ClubTheme.Border, 8, 6));
        lowerShopRow.AddChild(buffoonPanel);
        var buffoonVBox = new VBoxContainer();
        buffoonVBox.AddThemeConstantOverride("separation", 4);
        buffoonPanel.AddChild(buffoonVBox);
        var buffoonTag = ClubTheme.Label("PACOTE ESPECIAL · $ 4", 10, new Color("#f1948a"));
        buffoonVBox.AddChild(buffoonTag);
        buffoonVBox.AddChild(ClubTheme.Label("Pacote Buffoon 🎁", 13, ClubTheme.Paper));
        var buffoonDesc = ClubTheme.Label("Abre bênçãos de coringa (+1 Mult).", 10, ClubTheme.Muted);
        buffoonDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        buffoonVBox.AddChild(buffoonDesc);
        _buffoonBuyBtn = ClubTheme.Button("Abrir Pacote · $ 4");
        _buffoonBuyBtn.CustomMinimumSize = new Vector2(0, 30);
        _buffoonBuyBtn.Pressed += BuyBuffoonPack;
        buffoonVBox.AddChild(_buffoonBuyBtn);

        // Celestial Pack
        var celestialPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        celestialPanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.04f, 0.06f, 0.12f), ClubTheme.Border, 8, 6));
        lowerShopRow.AddChild(celestialPanel);
        var celestialVBox = new VBoxContainer();
        celestialVBox.AddThemeConstantOverride("separation", 4);
        celestialPanel.AddChild(celestialVBox);
        var celestialTag = ClubTheme.Label("PACOTE CELESTIAL · $ 4", 10, new Color("#bb8fce"));
        celestialVBox.AddChild(celestialTag);
        celestialVBox.AddChild(ClubTheme.Label("Pacote Astral ✨", 13, ClubTheme.Paper));
        var celestialDesc = ClubTheme.Label("Melhora os descartes (+1 Desc).", 10, ClubTheme.Muted);
        celestialDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        celestialVBox.AddChild(celestialDesc);
        _celestialBuyBtn = ClubTheme.Button("Abrir Pacote · $ 4");
        _celestialBuyBtn.CustomMinimumSize = new Vector2(0, 30);
        _celestialBuyBtn.Pressed += BuyCelestialPack;
        celestialVBox.AddChild(_celestialBuyBtn);

        // Map legacy button references
        _buyMultBtn = _voucherBuyBtn;
        _buyHandBtn = _buffoonBuyBtn;
        _buyDiscardBtn = _celestialBuyBtn;

        // Shop Footer Bar
        var shopFooterRow = new HBoxContainer();
        mainShop.AddChild(shopFooterRow);
        _shopMessage = ClubTheme.Label("Invista suas fichas para fortalecer sua corrida.", 12, ClubTheme.Muted);
        shopFooterRow.AddChild(_shopMessage);
        shopFooterRow.AddChild(Spacer());
        _shopDeckCountLabel = ClubTheme.Label("🂠 Baralho: 52 / 52", 12, ClubTheme.Gold);
        shopFooterRow.AddChild(_shopDeckCountLabel);
    }

    private void RefreshShopUI()
    {
        _goldLabel.Text = $"$ {_game.Gold}";
        _shopScoreValLabel.Text = _game.RoundScore.ToString("N0");
        _shopChipsLabel.Text = $"{_game.BaseMultiplierBonus * 10 + 20}";
        _shopMultLabel.Text = $"{_game.BaseMultiplierBonus + 1}";
        _shopHandsLabel.Text = $"Mãos: {_game.HandsRemaining}";
        _shopDiscardsLabel.Text = $"Desc: {_game.DiscardsRemaining}";
        _shopAnteLabel.Text = $"Ante: {_game.CurrentRound} / 8";
        _shopRoundLabel.Text = $"Rodada: {_game.CurrentRound}";
        _shopDeckCountLabel.Text = $"🂠 Baralho: {_game.DeckCount} / 52";
        _rerollBtn.Disabled = _game.Gold < 5;
        _shopNextRoundBtn.Text = _game.CurrentRound >= PokerGameManager.MaxRounds ? "Concluir a corrida  →" : "Próxima rodada  →";

        // Refresh Joker Slots
        var activeJokers = _game.Relics?.ActiveRelics ?? (IReadOnlyList<RelicManager.RelicId>)System.Array.Empty<RelicManager.RelicId>();
        _shopJokerSlotsLabel.Text = $"CORINGAS ({activeJokers.Count} / {RelicManager.MaxJokers})";

        while (_shopJokerSlotsContainer.GetChildCount() > 0)
        {
            var child = _shopJokerSlotsContainer.GetChild(0);
            _shopJokerSlotsContainer.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < RelicManager.MaxJokers; i++)
        {
            var slot = new PanelContainer { CustomMinimumSize = new Vector2(72, 88) };
            if (i < activeJokers.Count)
            {
                var rId = activeJokers[i];
                slot.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.05f, 0.12f, 0.08f), ClubTheme.Gold, 4, 6));
                var slotBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
                slot.AddChild(slotBox);
                slotBox.AddChild(ClubTheme.Label(RelicManager.GetJokerIcon(rId), 22, ClubTheme.Gold));
                var nameLbl = ClubTheme.Label(RelicManager.GetJokerName(rId), 9, ClubTheme.Paper);
                nameLbl.HorizontalAlignment = HorizontalAlignment.Center;
                slotBox.AddChild(nameLbl);
                slot.TooltipText = RelicManager.GetJokerDesc(rId);
            }
            else
            {
                slot.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.02f, 0.05f, 0.04f, 0.4f), ClubTheme.Border, 4, 6));
                var emptyLbl = ClubTheme.Label("Vazio", 10, ClubTheme.Muted);
                emptyLbl.HorizontalAlignment = HorizontalAlignment.Center;
                emptyLbl.VerticalAlignment = VerticalAlignment.Center;
                slot.AddChild(emptyLbl);
            }
            _shopJokerSlotsContainer.AddChild(slot);
        }

        // Refresh Jokers for Sale
        while (_shopJokerCardsContainer.GetChildCount() > 0)
        {
            var child = _shopJokerCardsContainer.GetChild(0);
            _shopJokerCardsContainer.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < _shopJokers.Count; i++)
        {
            int idx = i;
            var jId = _shopJokers[idx];
            bool sold = _shopJokersPurchased[idx];
            int price = RelicManager.GetJokerPrice(jId);

            var cardPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cardPanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(sold ? new Color(0.04f, 0.04f, 0.04f) : new Color(0.05f, 0.09f, 0.07f), sold ? ClubTheme.Border : ClubTheme.Gold, 10, 6));
            _shopJokerCardsContainer.AddChild(cardPanel);

            var cBox = new VBoxContainer();
            cBox.AddThemeConstantOverride("separation", 4);
            cardPanel.AddChild(cBox);

            var priceBadge = ClubTheme.Label(sold ? "ESGOTADO" : $"$ {price}", 11, sold ? new Color("#c0392b") : ClubTheme.Gold);
            cBox.AddChild(priceBadge);

            var titleRow = new HBoxContainer();
            titleRow.AddChild(ClubTheme.Label(RelicManager.GetJokerIcon(jId), 18, ClubTheme.Gold));
            titleRow.AddChild(ClubTheme.Label(RelicManager.GetJokerName(jId), 13, ClubTheme.Paper));
            cBox.AddChild(titleRow);

            var descLbl = ClubTheme.Label(RelicManager.GetJokerDesc(jId), 11, ClubTheme.Muted);
            descLbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            descLbl.CustomMinimumSize = new Vector2(0, 32);
            cBox.AddChild(descLbl);

            var buyBtn = ClubTheme.Button(sold ? "Adquirido" : $"Comprar · $ {price}");
            buyBtn.CustomMinimumSize = new Vector2(0, 32);
            buyBtn.Disabled = sold || _game.Gold < price || !_game.Relics.CanAddRelic;
            buyBtn.Pressed += () => BuyJokerOffer(idx);
            cBox.AddChild(buyBtn);
        }

        // Refresh Voucher
        _voucherTitleLabel.Text = _shopVoucherType switch
        {
            "hand" => "Cupom: +1 Mão Extra",
            "discard" => "Cupom: +1 Descarte Extra",
            _ => "Cupom: +1 Multiplicador Base"
        };
        _voucherDescLabel.Text = _shopVoucherType switch
        {
            "hand" => "+1 jogada permitida a cada nova rodada.",
            "discard" => "+1 descarte extra para renovar suas mãos.",
            _ => "+1 ao multiplicador base de todas as jogadas."
        };
        _voucherBuyBtn.Text = _shopVoucherPurchased ? "ESGOTADO" : $"Comprar · $ {_shopVoucherCost}";
        _voucherBuyBtn.Disabled = _shopVoucherPurchased || _game.Gold < _shopVoucherCost;

        // Refresh Packs
        _buffoonBuyBtn.Text = _shopBuffoonPackPurchased ? "ESGOTADO" : "Abrir Pacote · $ 4";
        _buffoonBuyBtn.Disabled = _shopBuffoonPackPurchased || _game.Gold < 4;

        _celestialBuyBtn.Text = _shopCelestialPackPurchased ? "ESGOTADO" : "Abrir Pacote · $ 4";
        _celestialBuyBtn.Disabled = _shopCelestialPackPurchased || _game.Gold < 4;

        RefreshResources();
    }

    private void OnRerollPressed()
    {
        if (!_game.RerollShop(5)) return;
        Core.Systems.AudioManager.Instance?.PlaySound("shuffle");
        GenerateShopStock();
        _shopMessage.Text = "Novos itens sorteados na banca!";
        RefreshShopUI();
    }

    private void BuyJokerOffer(int index)
    {
        if (index < 0 || index >= _shopJokers.Count) return;
        var jId = _shopJokers[index];
        int price = RelicManager.GetJokerPrice(jId);
        if (!_game.BuyRelic(jId, price)) return;
        _shopJokersPurchased[index] = true;
        Core.Systems.AudioManager.Instance?.PlaySound("score");
        _shopMessage.Text = $"{RelicManager.GetJokerName(jId)} adquirido! Bônus ativo.";
        RefreshShopUI();
    }

    private void BuyVoucherOffer()
    {
        if (!_game.BuyUpgrade(_shopVoucherType, _shopVoucherCost)) return;
        _shopVoucherPurchased = true;
        Core.Systems.AudioManager.Instance?.PlaySound("score");
        _shopMessage.Text = "Cupom do Ante adquirido! Bônus permanente ativo.";
        RefreshShopUI();
    }

    private void BuyBuffoonPack()
    {
        if (_game.Gold < 4) return;
        _game.BuyUpgrade("multiplier", 4);
        _shopBuffoonPackPurchased = true;
        Core.Systems.AudioManager.Instance?.PlaySound("score");
        _shopMessage.Text = "Pacote Buffoon aberto! +1 Multiplicador obtido.";
        RefreshShopUI();
    }

    private void BuyCelestialPack()
    {
        if (_game.Gold < 4) return;
        _game.BuyUpgrade("discard", 4);
        _shopCelestialPackPurchased = true;
        Core.Systems.AudioManager.Instance?.PlaySound("score");
        _shopMessage.Text = "Pacote Celestial aberto! +1 Descarte obtido.";
        RefreshShopUI();
    }

    private void ShowRunInfoPopup()
    {
        var text = $"Progresso: Ante {_game.CurrentRound}/8 · Rodada {_game.CurrentRound}\n" +
                   $"Bônus: +{_game.BaseMultiplierBonus} Mult · +{_game.ExtraHands} Mãos · +{_game.ExtraDiscards} Descartes\n" +
                   $"Baralho: {_game.DeckCount} cartas restantes.\n" +
                   _game.Relics?.GetHudText();
        _shopMessage.Text = text;
    }

    private void BuildTutorialPanel()
    {
        var panel = Panel(ClubTheme.Panel, ClubTheme.Gold, 18);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        panel.OffsetLeft = -760;
        panel.OffsetRight = -28;
        panel.OffsetTop = 200;
        panel.OffsetBottom = 390;
        panel.Visible = false;
        _tutorialPanel = panel;
        AddChild(panel);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);
        _tutorialTitle = ClubTheme.Label("Bem-vindo à mesa", 22, ClubTheme.Gold);
        box.AddChild(_tutorialTitle);
        _tutorialDesc = ClubTheme.Label("Escolha até cinco cartas. Forme combinações para alcançar a meta da rodada.", 15);
        _tutorialDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _tutorialDesc.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(_tutorialDesc);
        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        actions.AddThemeConstantOverride("separation", 12);
        var skip = ClubTheme.Button("Fechar tutorial");
        skip.Pressed += () => _tutorialPanel.Visible = false;
        actions.AddChild(skip);
        var next = ClubTheme.Button("Continuar", true);
        next.Pressed += () => _tutorialController?.AdvanceStep();
        actions.AddChild(next);
        box.AddChild(actions);
    }

    private void AttachTutorial()
    {
        _tutorialController = GetNodeOrNull<Core.AI.TutorialController>("../../TutorialController");
        if (_tutorialController == null) return;
        _tutorialController.TutorialStepChanged += OnTutorialStep;
        _tutorialController.TutorialComplete += OnTutorialComplete;
        _tutorialPanel.Visible = true;
    }

    private void OnTutorialStep(int step, string title, string desc)
    {
        _tutorialTitle.Text = title;
        // Keep instructions aligned with the features available in this build.
        _tutorialDesc.Text = step switch
        {
            3 => "A pontuação combina fichas da mão e das cartas, multiplicadas pelo Mult. A prévia mostra o valor exato antes de jogar.",
            5 => "Sua corrida dura oito rodadas. A meta aumenta e as relíquias e compras ajudam a fortalecer suas mãos.",
            6 => "Entre rodadas, compre multiplicador, mãos extras ou descartes com as fichas que você ganhou.",
            8 => "Tutorial completo. Continue a corrida e tente vencer todas as oito rodadas.",
            _ => desc
        };
        _tutorialPanel.Visible = true;
    }

    private void OnTutorialComplete() => _tutorialPanel.Visible = false;

    private int _dealGeneration;
    private bool _dealInProgress;

    private async void OnHandDealt()
    {
        if (!IsInsideTree()) return;
        if (_hud != null) _hud.Visible = true;
        _stage.ClearPlayedCards();
        bool animateDeal = Core.Systems.SettingsManager.Instance?.ReduceMotion != true;
        int generation = ++_dealGeneration;
        _dealInProgress = animateDeal;
        _ignoreInput = _game.CurrentPhase != PokerGameManager.GamePhase.PlayerTurn || animateDeal;
        ClearTableCards();
        ClearContainer(_cardContainer);
        _cardPanels.Clear();
        var hand = _game.GetPlayerHand();
        for (int i = 0; i < hand.Count; i++)
        {
            var panel = CreateCardPanel(hand[i], i);
            _cardContainer.AddChild(panel);
            _cardPanels.Add(panel);
            FadeIn(panel, .28f + i * HandDealInterval);
            int order = i;
            Callable.From(() => AnimateDealToHand(panel, order)).CallDeferred();
        }
        RefreshDealerDisplay();
        RefreshBoss();
        RefreshResources();
        UpdateActionButtons();
        RefreshPreview();
        if (animateDeal)
        {
            float duration = HandDealFlight + Mathf.Max(0, hand.Count - 1) * HandDealInterval + .12f;
            await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree() || generation != _dealGeneration) return;
            _dealInProgress = false;
            _ignoreInput = _game.CurrentPhase != PokerGameManager.GamePhase.PlayerTurn;
            UpdateActionButtons();
        }
    }

    private void AnimateDealToHand(PanelContainer target, int order)
    {
        if (!IsInstanceValid(target) || !target.IsInsideTree() || Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        var back = WrapCard(null, new Vector2(78, 110), true);
        back.Size = back.CustomMinimumSize;
        back.Position = _stage.DeckScreenPosition() - back.Size / 2;
        back.Scale = new Vector2(.7f, .7f);
        _tableCardLayer.AddChild(back);
        var tween = CreateTween();
        tween.TweenInterval(order * HandDealInterval);
        tween.TweenProperty(back, "position", target.GlobalPosition, HandDealFlight).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(back, "scale", Vector2.One, HandDealFlight);
        tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(back)) back.QueueFree(); }));
    }

    private PanelContainer CreateCardPanel(CardData card, int index)
    {
        var panel = WrapCard(card, new Vector2(78, 110));
        panel.MouseFilter = MouseFilterEnum.Stop;
        panel.MouseDefaultCursorShape = CursorShape.PointingHand;
        panel.FocusMode = FocusModeEnum.All;
        panel.TooltipText = $"{card} — clique ou pressione Enter para selecionar";
        panel.GuiInput += input =>
        {
            if (_ignoreInput) return;
            bool click = input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left;
            bool keyboard = input.IsActionPressed("ui_accept");
            if (!click && !keyboard) return;
            _game.ToggleCard(index);
            ApplyCardStyle(panel, _game.GetSelectedIndices().Contains(index), panel.HasFocus());
            UpdateActionButtons();
            RefreshPreview();
            panel.AcceptEvent();
        };
        panel.MouseEntered += () => ApplyCardStyle(panel, _game.GetSelectedIndices().Contains(index), true);
        panel.MouseExited += () => ApplyCardStyle(panel, _game.GetSelectedIndices().Contains(index), panel.HasFocus());
        panel.FocusEntered += () => ApplyCardStyle(panel, _game.GetSelectedIndices().Contains(index), true);
        panel.FocusExited += () => ApplyCardStyle(panel, _game.GetSelectedIndices().Contains(index), false);
        return panel;
    }

    private PanelContainer WrapCard(CardData card, Vector2 size, bool faceDown = false)
    {
        var panel = new PanelContainer { CustomMinimumSize = size, MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(Colors.Transparent, Colors.Transparent, 3, 6));
        panel.AddChild(new PlayingCard
        {
            RankText = card?.GetRankString() ?? "",
            SuitSymbol = card?.GetSuitSymbol() ?? "♠",
            FaceDown = faceDown,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = size - new Vector2(6, 6)
        });
        return panel;
    }

    private void ApplyCardStyle(PanelContainer panel, bool selected, bool highlighted)
    {
        if (!IsInstanceValid(panel)) return;
        var border = selected || highlighted ? ClubTheme.Gold : Colors.Transparent;
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(Colors.Transparent, border, 3, 7));
        if (panel.GetChildOrNull<PlayingCard>(0) is PlayingCard face)
        {
            face.Selected = selected;
            face.QueueRedraw();
        }
    }

    private void RefreshDealerDisplay()
    {
        ClearContainer(_dealerCardContainer);
        _dealerCardPanels.Clear();
        foreach (var card in _game.GetDealerHand())
        {
            var panel = WrapCard(card, new Vector2(48, 68), true);
            _dealerCardContainer.AddChild(panel);
            _dealerCardPanels.Add(panel);
        }
    }

    private async void RefreshBoss()
    {
        int rival = CharacterCatalog.BossForRound(_game.CurrentRound);
        bool introduce = _presentedRound != _game.CurrentRound;
        if (introduce)
        {
            _presentedRound = _game.CurrentRound;
            _stage.SetCast(rival, Mathf.Clamp(_game.OpponentCount + 1, 2, 4));
            string theme = rival switch
            {
                7 => "barao_lounge",
                8 => "dama_salon",
                9 => "classic_club",
                10 => "cyber_casino",
                _ => "classic_club"
            };
            _stage.SetRoomTheme(theme);
        }
        _stage.SetCardCount(0, _game.GetPlayerHand().Count);
        _stage.SetCardCount(1, _game.GetDealerHand().Count);
        _bossName.Text = CharacterCatalog.Names[rival];
        _bossPortrait.Material = null;
        _bossPortrait.Texture = CharacterCatalog.Portrait(rival);
        _opponentsLabel.Text = $"{_game.OpponentCount} bot{(_game.OpponentCount == 1 ? "" : "s")} à mesa · vença a meta";
        if (introduce)
        {
            string music = rival switch
            {
                7 => "midnight-baron",
                8 => "velvet-table",
                9 => "saloon-swing",
                10 => "cyber-tango",
                _ => "midnight-baron"
            };
            Core.Systems.AudioManager.Instance?.PlayMusic(music);
            await _stage.PlayEntrance(true);
            if (IsInsideTree()) UpdateActionButtons();
        }
    }

    private void RefreshResources()
    {
        _handsLabel.Text = $"MÃOS  {_game.HandsRemaining}";
        _discardsLabel.Text = $"DESCARTES  {_game.DiscardsRemaining}";
        _walletLabel.Text = $"FICHAS  {_game.Gold}";
        _relicsLabel.Text = _game.Relics?.GetHudText().Replace("Relíquias: ", "") ?? "Nenhuma relíquia";
        _relicsLabel.TooltipText = "Ás +1 Mult: jogar pelo menos um Ás acrescenta 1 ao multiplicador da mão.";
    }

    private void UpdateActionButtons()
    {
        _playBtn.Disabled = _ignoreInput || _stage.IsPresenting || !_game.CanPlayHand();
        _discardBtn.Disabled = _ignoreInput || _stage.IsPresenting || !_game.CanDiscard();
        int count = _game.GetSelectedIndices().Count;
        _selectionLabel.Text = $"{count} / {PokerGameManager.MaxPlayCards} selecionadas";
        _playBtn.Text = count > 0 ? $"Jogar mão · {count}" : "Jogar mão";
    }

    private void RefreshPreview()
    {
        if (_ignoreInput) return;
        var hand = _game.GetPlayerHand();
        var cards = _game.GetSelectedIndices().Where(i => i >= 0 && i < hand.Count).Select(i => hand[i]).ToList();
        if (cards.Count == 0)
        {
            _resultLabel.Text = "Escolha suas cartas";
            _breakdownLabel.Text = "De 1 a 5 cartas para formar uma combinação.";
            return;
        }
        var preview = HandEvaluator.Evaluate(cards);
        preview.Mult += _game.BaseMultiplierBonus;
        _game.Relics?.ApplyHandEffects(preview, cards);
        _resultLabel.Text = preview.HandName;
        _breakdownLabel.Text = $"{preview.BaseChips + preview.CardChips} fichas × {preview.Mult} Mult = {preview.TotalScore} pontos";
    }

    private void OnScoreUpdated(int roundScore, int roundTarget)
    {
        _roundLabel.Text = $"Rodada {_game.CurrentRound} / {PokerGameManager.MaxRounds}";
        _scoreLabel.Text = roundScore.ToString("N0");
        _targetLabel.Text = $"META  {roundTarget:N0}";
        _scoreBar.MaxValue = roundTarget;
        _scoreBar.Value = Mathf.Min(roundScore, roundTarget);
        RefreshResources();
    }

    private void OnHandScored(string handName, int score, string breakdown)
    {
        _stage.SetCardCount(1, 0);
        _stage.React(1);
        Core.Systems.AudioManager.Instance?.PlaySound("score");
        _ignoreInput = true;
        UpdateActionButtons();
        _resultLabel.Text = $"{handName} · +{score} pontos";
        _breakdownLabel.Text = breakdown;
        ShowScorePopup(handName, score);
        var hand = _game.GetDealerHand();
        for (int i = 0; i < _dealerCardPanels.Count && i < hand.Count; i++)
        {
            _stage.PlayCard(1, hand[i].ToString(), .20f + i * .16f);
            var panel = _dealerCardPanels[i];
            var card = hand[i];
            if (panel.GetChildOrNull<PlayingCard>(0) is PlayingCard face)
            {
                face.RankText = card.GetRankString();
                face.SuitSymbol = card.GetSuitSymbol();
                face.FaceDown = false;
                face.QueueRedraw();
            }
        }
    }

    private void OnRoundEnded(int round, bool passed)
    {
        if (passed)
        {
            Core.Systems.AudioManager.Instance?.PlaySound("win");
            _stage?.PlayGesture(0, "big_win");
            _stage?.PlayGesture(1, "lose");
        }
        else
        {
            _stage?.PlayGesture(0, "bad_beat");
            _stage?.PlayGesture(1, "big_win");
        }
        _tutorialPanel.Visible = false;
        _overlayTitle.Text = passed ? $"Mesa {round} vencida" : "A mesa levou a melhor";
        _overlayTitle.AddThemeColorOverride("font_color", passed ? ClubTheme.Gold : new Color("#e29a86"));
        _overlaySubtitle.Text = passed
            ? $"{_game.RoundScore:N0} de {_game.RoundTarget:N0} pontos.\nVocê tem {_game.Gold} fichas para investir na banca."
            : $"{_game.RoundScore:N0} de {_game.RoundTarget:N0} pontos. Tente uma nova combinação de estratégias.";
        _nextRoundBtn.Visible = passed;
        _newGameBtn.Visible = !passed;
        if (_hud != null) _hud.Visible = false;
        _overlayPanel.Visible = true;
        if (passed) _nextRoundBtn.GrabFocus(); else _newGameBtn.GrabFocus();
    }

    private void OnGameEnded(bool won, int totalScore)
    {
        if (won)
        {
            CharacterProgress.RecordWin();
            _stage?.PlayGesture(0, "victory");
            _stage?.PlayGesture(1, "lose");
        }
        else
        {
            _stage?.PlayGesture(0, "lose");
            _stage?.PlayGesture(1, "victory");
        }
        _shopOverlay.Visible = false;
        _tutorialPanel.Visible = false;
        if (_hud != null) _hud.Visible = false;
        _overlayTitle.Text = won ? "O clube é seu." : "Fim da corrida";
        _overlayTitle.AddThemeColorOverride("font_color", won ? ClubTheme.Gold : new Color("#e29a86"));
        _overlaySubtitle.Text = won
            ? $"Oito mesas vencidas. {totalScore:N0} pontos conquistados."
            : $"Você chegou à mesa {_game.CurrentRound}.\n{_game.RoundScore:N0} de {_game.RoundTarget:N0} pontos nesta rodada.";
        _nextRoundBtn.Visible = false;
        _newGameBtn.Visible = true;
        _overlayPanel.Visible = true;
        _newGameBtn.GrabFocus();
    }

    private void OnPhaseChanged(int phase)
    {
        _ignoreInput = _dealInProgress || (PokerGameManager.GamePhase)phase != PokerGameManager.GamePhase.PlayerTurn;
        UpdateActionButtons();
    }

    private void OnPlayPressed()
    {
        if (_ignoreInput || _stage.IsPresenting || !_game.CanPlayHand()) return;
        _ignoreInput = true;
        _stage?.PlayGesture(0, "play_card_dramatic");
        Core.Systems.AudioManager.Instance?.PlaySound("play");
        UpdateActionButtons();
        AnimatePlayedCards();
        _game.PlayHand();
    }

    private void OnDiscardPressed()
    {
        if (!_ignoreInput && !_stage.IsPresenting && _game.CanDiscard())
        {
            _stage?.PlayGesture(0, "fold");
            _game.DiscardCards();
        }
    }

    private void AnimatePlayedCards()
    {
        ClearTableCards();
        var selected = _game.GetSelectedIndices().OrderBy(i => i).ToList();
        var hand = _game.GetPlayerHand();
        for (int i = 0; i < selected.Count; i++)
        {
            int index = selected[i];
            if (index >= hand.Count || index >= _cardPanels.Count) continue;
            _stage.PlayCard(0, hand[index].ToString(), i * .15f);
        }
        _stage.SetCardCount(0, hand.Count-selected.Count);
    }

    private void ShowScorePopup(string handName, int score)
    {
        _popupTween?.Kill();
        string stylizedTitle = GetAnimatedHandTitle(handName);
        Color accentColor = GetHandAccentColor(handName);

        _scorePopup.Visible = false;
        _handTitleLabel.Text = stylizedTitle;
        _handTitleLabel.AddThemeColorOverride("font_color", accentColor);
        _handSubtitleLabel.Text = $"+{score:N0} PONTOS NA BANCA";

        var area = _tableArea.GetGlobalRect();
        Vector2 bannerSize = new Vector2(460, 110);
        _handCelebrationBanner.CustomMinimumSize = bannerSize;
        _handCelebrationBanner.Size = bannerSize;
        _handCelebrationBanner.Position = new Vector2(
            area.Position.X + (area.Size.X - bannerSize.X) / 2f,
            area.Position.Y + (area.Size.Y - bannerSize.Y) / 2f
        );
        _handCelebrationBanner.PivotOffset = bannerSize / 2f;
        _handCelebrationBanner.Visible = true;
        _handCelebrationBanner.Modulate = Colors.White;

        bool reduceMotion = Core.Systems.SettingsManager.Instance?.ReduceMotion == true;
        if (reduceMotion)
        {
            _handCelebrationBanner.Scale = Vector2.One;
            _handCelebrationBanner.RotationDegrees = 0f;
            _popupTween = CreateTween();
            _popupTween.TweenInterval(1.2f);
            _popupTween.TweenProperty(_handCelebrationBanner, "modulate:a", 0.0f, 0.2f);
            _popupTween.TweenCallback(Callable.From(() => _handCelebrationBanner.Visible = false));
            return;
        }

        // Punch-scale and rotation wobble
        _handCelebrationBanner.Scale = Vector2.One * 0.15f;
        _handCelebrationBanner.RotationDegrees = -6.0f;

        _popupTween = CreateTween();
        // Punch in: 0.15 -> 1.35 in 0.22s, wobble rotation to +3 deg
        _popupTween.Parallel().TweenProperty(_handCelebrationBanner, "scale", Vector2.One * 1.35f, 0.22f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _popupTween.Parallel().TweenProperty(_handCelebrationBanner, "rotation_degrees", 3.0f, 0.22f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        // Settle: 1.35 -> 1.0 in 0.14s, rotation -> 0 deg
        _popupTween.Chain().TweenProperty(_handCelebrationBanner, "scale", Vector2.One, 0.14f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _popupTween.Parallel().TweenProperty(_handCelebrationBanner, "rotation_degrees", 0.0f, 0.14f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        // Hold banner visible
        _popupTween.TweenInterval(1.1f);

        // Fade and scale up slightly
        _popupTween.TweenProperty(_handCelebrationBanner, "modulate:a", 0.0f, 0.22f);
        _popupTween.Parallel().TweenProperty(_handCelebrationBanner, "scale", Vector2.One * 1.10f, 0.22f);
        _popupTween.TweenCallback(Callable.From(() => _handCelebrationBanner.Visible = false));
    }

    private static string GetAnimatedHandTitle(string handName)
    {
        if (handName.Contains("Royal", System.StringComparison.OrdinalIgnoreCase)) return "⚡ EXTRAORDINÁRIO! ROYAL FLUSH! ⚡";
        if (handName.Contains("Straight Flush", System.StringComparison.OrdinalIgnoreCase)) return "✦ STRAIGHT FLUSH LENDÁRIO! ✦";
        if (handName.Contains("Quadra", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Four", System.StringComparison.OrdinalIgnoreCase)) return "💥 BUM! QUADRA ESMAGADORA! 💥";
        if (handName.Contains("Full House", System.StringComparison.OrdinalIgnoreCase)) return "★ POW! FULL HOUSE! ★";
        if (handName.Contains("Flush", System.StringComparison.OrdinalIgnoreCase)) return "✦ FLUSH PURÍSSIMO! ✦";
        if (handName.Contains("Sequência", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Straight", System.StringComparison.OrdinalIgnoreCase)) return "⚔ SEQUÊNCIA EM CHEIO! ⚔";
        if (handName.Contains("Trinca", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Three", System.StringComparison.OrdinalIgnoreCase)) return "◆ TRINCA AFIADA! ◆";
        if (handName.Contains("Dois Pares", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Two Pair", System.StringComparison.OrdinalIgnoreCase)) return "♦ DOIS PARES FIRMES! ♦";
        if (handName.Contains("Par", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Pair", System.StringComparison.OrdinalIgnoreCase)) return "• UM PAR CERTEIRO •";
        return "CARTA ALTA";
    }

    private static Color GetHandAccentColor(string handName)
    {
        if (handName.Contains("Royal", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Straight Flush", System.StringComparison.OrdinalIgnoreCase))
            return new Color("#ffd700");
        if (handName.Contains("Quadra", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Full House", System.StringComparison.OrdinalIgnoreCase))
            return new Color("#ff4444");
        if (handName.Contains("Flush", System.StringComparison.OrdinalIgnoreCase) || handName.Contains("Sequência", System.StringComparison.OrdinalIgnoreCase))
            return new Color("#00e5ff");
        return ClubTheme.Gold;
    }

    private void ClearTableCards()
    {
        if (_tableCardLayer == null) return;
        _popupTween?.Kill();
        _scorePopup.Visible = false;
        if (_handCelebrationBanner != null) _handCelebrationBanner.Visible = false;
        foreach (var child in _tableCardLayer.GetChildren())
        {
            if (child == _scorePopup || child == _handCelebrationBanner) continue;
            _tableCardLayer.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void FadeIn(Control node, float delay)
    {
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        node.Modulate = new Color(1, 1, 1, 0);
        CreateTween().TweenProperty(node, "modulate:a", 1.0f, 0.18f).SetDelay(delay);
    }

    private float MotionDuration(float duration) => Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.01f : duration;

    public void OpenShop()
    {
        if (_overlayPanel != null) _overlayPanel.Visible = false;
        if (_hud != null) _hud.Visible = false;
        RefreshShopUI();
        if (_shopOverlay != null) _shopOverlay.Visible = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                _stage?.ToggleInGameSettings();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void ReturnToHub() => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");

    private VBoxContainer CreateModal(out Control overlay, Vector2 minSize)
    {
        var shade = new ColorRect { Color = new Color(0.015f, 0.04f, 0.035f, 0.94f), Visible = false, MouseFilter = MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);
        overlay = shade;
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shade.AddChild(center);
        var panel = Panel(ClubTheme.Panel, ClubTheme.Gold, 30);
        panel.CustomMinimumSize = minSize;
        center.AddChild(panel);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 18);
        panel.AddChild(box);
        return box;
    }

    private static Label DisplayLabel(string text, int size)
    {
        var label = ClubTheme.Label(text, size);
        label.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        return label;
    }

    private static PanelContainer Panel(Color fill, Color border, int padding)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(fill, border, padding, 10));
        return panel;
    }

    private static Control Rule() => new ColorRect { Color = ClubTheme.Border, CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore };

    private static Control Spacer() => new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };

    private static void ClearContainer(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
