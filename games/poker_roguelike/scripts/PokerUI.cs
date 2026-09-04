using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GameHub.Games.PokerRoguelike;

/// <summary>
/// Programmatically-built UI for the Poker Roguelike game.
/// Handles all visual display, card interaction, and score presentation.
/// </summary>
public partial class PokerUI : Control
{
    private PokerGameManager _game;

    // ===== UI REFS =====
    private Label _titleLabel;
    private Label _roundLabel;
    private Label _scoreLabel;
    private Label _targetLabel;
    private Label _handsLabel;
    private Label _discardsLabel;
    private Label _relicsLabel;
    private Label _opponentsLabel;
    private Label _resultLabel;
    private Label _breakdownLabel;
    private ProgressBar _scoreBar;
    private HBoxContainer _dealerCardContainer;
    private HBoxContainer _cardContainer;
    private Control _tableCardLayer;
    private Label _scorePopup;
    private Button _playBtn;
    private Button _discardBtn;

    // Overlay
    private Control _overlayPanel;
    private Label _overlayTitle;
    private Label _overlaySubtitle;
    private Button _nextRoundBtn;
    private Button _newGameBtn;
    private Button _backMenuBtn;
    private TextureRect _bossDefeatPortrait;

    // Card tracking
    private List<PanelContainer> _cardPanels = new();
    private List<PanelContainer> _dealerCardPanels = new();
    private HashSet<int> _hoveredCards = new();
    private bool _ignoreInput = false;

    // Tutorial
    private Control _tutorialPanel;
    private Label _tutorialTitle;
    private Label _tutorialDesc;
    private Button _tutorialNextBtn;
    private Button _tutorialSkipBtn;
    private Core.AI.TutorialController _tutorialController;

    // Shop
    private Control _shopOverlay;
    private Label _goldLabel;
    private Button _buyMultBtn;
    private Button _buyHandBtn;
    private Button _buyDiscardBtn;
    private Button _shopNextRoundBtn;

    // ===== THEME COLORS =====
    private static readonly Color BgDark = new(0.05f, 0.05f, 0.09f);
    private static readonly Color PanelBg = new(0.09f, 0.09f, 0.15f);
    private static readonly Color CardBg = new(0.09f, 0.12f, 0.22f);
    private static readonly Color CardBorder = new(0.17f, 0.2f, 0.35f);
    private static readonly Color CardHoverBg = new(0.12f, 0.15f, 0.28f);
    private static readonly Color CardHoverBorder = new(0.3f, 0.34f, 0.55f);
    private static readonly Color CardSelectedBg = new(0.11f, 0.14f, 0.26f);
    private static readonly Color Gold = new(0.97f, 0.82f, 0.38f);
    private static readonly Color Accent = new(0.91f, 0.27f, 0.37f);
    private static readonly Color BtnPurple = new(0.32f, 0.2f, 0.51f);
    private static readonly Color BtnPurpleHover = new(0.42f, 0.29f, 0.71f);
    private static readonly Color BtnGreen = new(0.13f, 0.55f, 0.33f);
    private static readonly Color BtnGreenHover = new(0.17f, 0.7f, 0.42f);
    private static readonly Color BtnRed = new(0.65f, 0.15f, 0.2f);
    private static readonly Color BtnRedHover = new(0.8f, 0.2f, 0.25f);
    private static readonly Color SuccessGreen = new(0.18f, 0.8f, 0.44f);
    private static readonly Color TextPrimary = new(0.93f, 0.93f, 0.95f);
    private static readonly Color TextSecondary = new(0.5f, 0.5f, 0.65f);
    private static readonly Color RedSuit = new(1f, 0.4f, 0.4f);
    private static readonly Color BlackSuit = new(0.88f, 0.88f, 0.93f);
    private static readonly Color BarBg = new(0.08f, 0.08f, 0.14f);
    private static readonly Color BarFill = new(0.97f, 0.82f, 0.38f);

    // ===== LIFECYCLE =====

    public override void _Ready()
    {
        _game = GetNode<PokerGameManager>("../../GameManager");
        BuildUI();
        ConnectSignals();

        // Check for tutorial mode
        _tutorialController = GetNodeOrNull<Core.AI.TutorialController>("../../TutorialController");
        if (_tutorialController != null)
        {
            _tutorialController.TutorialStepChanged += OnTutorialStep;
            _tutorialController.TutorialComplete += OnTutorialComplete;
            _tutorialPanel.Visible = true;
        }

        _game.StartNewGame();
    }

    private void ConnectSignals()
    {
        _game.HandDealt += OnHandDealt;
        _game.ScoreUpdated += OnScoreUpdated;
        _game.HandScored += OnHandScored;
        _game.RoundEnded += OnRoundEnded;
        _game.GameEnded += OnGameEnded;
        _game.PhaseChanged += OnPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_game != null)
        {
            _game.HandDealt -= OnHandDealt;
            _game.ScoreUpdated -= OnScoreUpdated;
            _game.HandScored -= OnHandScored;
            _game.RoundEnded -= OnRoundEnded;
            _game.GameEnded -= OnGameEnded;
            _game.PhaseChanged -= OnPhaseChanged;
        }
        if (_tutorialController != null)
        {
            _tutorialController.TutorialStepChanged -= OnTutorialStep;
            _tutorialController.TutorialComplete -= OnTutorialComplete;
        }
    }

    // ===== UI CONSTRUCTION =====

    private void BuildUI()
    {
        // A full-screen 2D backdrop keeps the casino visible on every aspect
        // ratio; the 3D Sprite3D is retained only as scene depth.
        var backdrop = new TextureRect();
        backdrop.Texture = ResourceLoader.Load<Texture2D>("res://assets/sprites/backgrounds/cyber_casino.jpg");
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        backdrop.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        // Scale fills every viewport. The old aspect-preserving mode left the
        // casino as a thin strip behind the 3D table on some resolutions.
        backdrop.StretchMode = TextureRect.StretchModeEnum.Scale;
        backdrop.Modulate = new Color(1, 1, 1, 0.55f);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        // Slight darkening keeps controls legible without replacing the scene.
        var bg = new ColorRect();
        bg.Color = new Color(0, 0, 0, 0.35f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        _tableCardLayer = new Control();
        _tableCardLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _tableCardLayer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_tableCardLayer);

        _scorePopup = CreateLabel("", 16, Gold);
        _scorePopup.HorizontalAlignment = HorizontalAlignment.Center;
        _scorePopup.Size = new Vector2(280, 52);
        _scorePopup.Visible = false;
        _scorePopup.MouseFilter = MouseFilterEnum.Ignore;
        _tableCardLayer.AddChild(_scorePopup);

        // Main layout
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 5);
        margin.AddChild(mainVBox);

        // --- Top bar ---
        var topBar = new HBoxContainer();
        topBar.AddThemeConstantOverride("separation", 8);

        _titleLabel = CreateLabel("♠ POKER ROGUELIKE", 8, Gold);
        topBar.AddChild(_titleLabel);

        topBar.AddChild(CreateExpandSpacer());

        var backBtn = CreateStyledButton("Menu", BtnRed, BtnRedHover, new Vector2(40, 16));
        backBtn.AddThemeFontSizeOverride("font_size", 8);
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        topBar.AddChild(backBtn);

        mainVBox.AddChild(topBar);

        // --- Decorative line ---
        var separator = new HSeparator();
        separator.AddThemeConstantOverride("separation", 2);
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = new Color(0.2f, 0.2f, 0.35f);
        sepStyle.ContentMarginTop = 1;
        sepStyle.ContentMarginBottom = 1;
        separator.AddThemeStyleboxOverride("separator", sepStyle);
        mainVBox.AddChild(separator);

        // --- Score row ---
        var scoreRow = new HBoxContainer();
        scoreRow.AddThemeConstantOverride("separation", 10);

        _roundLabel = CreateLabel("Rodada 1", 8, TextPrimary);
        scoreRow.AddChild(_roundLabel);

        scoreRow.AddChild(CreateExpandSpacer());

        _scoreLabel = CreateLabel("0", 12, Gold);
        scoreRow.AddChild(_scoreLabel);

        var slashLabel = CreateLabel("/", 8, TextSecondary);
        scoreRow.AddChild(slashLabel);

        _targetLabel = CreateLabel("300", 9, TextSecondary);
        scoreRow.AddChild(_targetLabel);

        scoreRow.AddChild(CreateExpandSpacer());

        mainVBox.AddChild(scoreRow);

        // --- Score bar ---
        _scoreBar = new ProgressBar();
        _scoreBar.CustomMinimumSize = new Vector2(0, 8);
        _scoreBar.MinValue = 0;
        _scoreBar.MaxValue = 300;
        _scoreBar.Value = 0;
        _scoreBar.ShowPercentage = false;
        _scoreBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var barBgStyle = new StyleBoxFlat();
        barBgStyle.BgColor = BarBg;
        barBgStyle.SetCornerRadiusAll(4);
        _scoreBar.AddThemeStyleboxOverride("background", barBgStyle);

        var barFillStyle = new StyleBoxFlat();
        barFillStyle.BgColor = BarFill;
        barFillStyle.SetCornerRadiusAll(4);
        _scoreBar.AddThemeStyleboxOverride("fill", barFillStyle);

        mainVBox.AddChild(_scoreBar);

        // --- Resource row ---
        var resRow = new HBoxContainer();
        resRow.AddThemeConstantOverride("separation", 8);

        _handsLabel = CreateLabel("Mãos: 4", 8, TextSecondary);
        resRow.AddChild(_handsLabel);

        resRow.AddChild(CreateExpandSpacer());

        _discardsLabel = CreateLabel("Descartes: 3", 8, TextSecondary);
        resRow.AddChild(_discardsLabel);

        mainVBox.AddChild(resRow);

        _relicsLabel = CreateLabel("Relíquias: —", 7, new Color(0.62f, 0.5f, 0.92f));
        _relicsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _relicsLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(_relicsLabel);

        _opponentsLabel = CreateLabel("Oponente: 1 bot", 7, TextSecondary);
        _opponentsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _opponentsLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(_opponentsLabel);

        // --- Dealer Cards ---
        var dealerCenter = new CenterContainer();
        dealerCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _dealerCardContainer = new HBoxContainer();
        _dealerCardContainer.AddThemeConstantOverride("separation", 5);
        dealerCenter.AddChild(_dealerCardContainer);
        mainVBox.AddChild(dealerCenter);

        // --- Flexible spacer (pushes everything below to bottom) ---
        mainVBox.AddChild(CreateExpandSpacer());

        // --- Action buttons (above cards) ---
        var actionCenter = new CenterContainer();
        actionCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 14);

        _playBtn = CreateStyledButton("Jogar Mão", BtnGreen, BtnGreenHover, new Vector2(70, 20));
        _playBtn.Pressed += OnPlayPressed;
        actionRow.AddChild(_playBtn);

        _discardBtn = CreateStyledButton("Descartar", BtnPurple, BtnPurpleHover, new Vector2(70, 20));
        _discardBtn.Pressed += OnDiscardPressed;
        actionRow.AddChild(_discardBtn);

        actionCenter.AddChild(actionRow);
        mainVBox.AddChild(actionCenter);

        // --- Spacer ---
        mainVBox.AddChild(CreateFixedSpacer(4));

        // --- Card row (Player) ---
        var cardCenter = new CenterContainer();
        cardCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _cardContainer = new HBoxContainer();
        _cardContainer.AddThemeConstantOverride("separation", 5);
        cardCenter.AddChild(_cardContainer);
        mainVBox.AddChild(cardCenter);

        // --- Result label ---
        _resultLabel = CreateLabel("", 10, SuccessGreen);
        _resultLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _resultLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(_resultLabel);

        _breakdownLabel = CreateLabel("Selecione cartas e jogue sua mão!", 8, TextSecondary);
        _breakdownLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _breakdownLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(_breakdownLabel);

        // --- Overlay ---
        BuildOverlay();

        // --- Shop Overlay ---
        BuildShopOverlay();

        // --- Tutorial Panel ---
        BuildTutorialPanel();
    }

    private void BuildOverlay()
    {
        _overlayPanel = new ColorRect();
        ((ColorRect)_overlayPanel).Color = new Color(0, 0, 0, 0.75f);
        _overlayPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _overlayPanel.Visible = false;
        _overlayPanel.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_overlayPanel);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _overlayPanel.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(280, 180);

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = PanelBg;
        panelStyle.SetCornerRadiusAll(8);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = Gold;
        panelStyle.ContentMarginLeft = 30;
        panelStyle.ContentMarginRight = 30;
        panelStyle.ContentMarginTop = 20;
        panelStyle.ContentMarginBottom = 20;
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(hbox);

        _bossDefeatPortrait = new TextureRect();
        _bossDefeatPortrait.CustomMinimumSize = new Vector2(100, 100);
        _bossDefeatPortrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _bossDefeatPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _bossDefeatPortrait.Texture = ResourceLoader.Load<Texture2D>("res://assets/sprites/portraits/truco_player.jpg");
        _bossDefeatPortrait.Visible = false; // Only show on win
        hbox.AddChild(_bossDefeatPortrait);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddChild(vbox);

        _overlayTitle = CreateLabel("", 14, Gold);
        _overlayTitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_overlayTitle);

        _overlaySubtitle = CreateLabel("", 8, TextSecondary);
        _overlaySubtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_overlaySubtitle);

        vbox.AddChild(CreateFixedSpacer(6));

        _nextRoundBtn = CreateStyledButton("Próxima Rodada", BtnGreen, BtnGreenHover, new Vector2(100, 22));
        _nextRoundBtn.Pressed += () =>
        {
            _overlayPanel.Visible = false;
            _resultLabel.Text = "";
            _breakdownLabel.Text = "Selecione cartas e jogue sua mão!";
            _game.StartNextRound();
        };
        vbox.AddChild(_nextRoundBtn);

        _newGameBtn = CreateStyledButton("Nova Partida", BtnPurple, BtnPurpleHover, new Vector2(100, 22));
        _newGameBtn.Pressed += () =>
        {
            _overlayPanel.Visible = false;
            _resultLabel.Text = "";
            _breakdownLabel.Text = "Selecione cartas e jogue sua mão!";
            _game.StartNewGame();
        };
        vbox.AddChild(_newGameBtn);

        _backMenuBtn = CreateStyledButton("Voltar ao Menu", BtnRed, BtnRedHover, new Vector2(100, 22));
        _backMenuBtn.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        vbox.AddChild(_backMenuBtn);
    }

    private void BuildShopOverlay()
    {
        _shopOverlay = new ColorRect();
        ((ColorRect)_shopOverlay).Color = new Color(0, 0, 0, 0.85f);
        _shopOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _shopOverlay.Visible = false;
        _shopOverlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_shopOverlay);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _shopOverlay.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(280, 180);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = PanelBg;
        panelStyle.SetCornerRadiusAll(8);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = Gold;
        panelStyle.ContentMarginLeft = 24;
        panelStyle.ContentMarginRight = 24;
        panelStyle.ContentMarginTop = 16;
        panelStyle.ContentMarginBottom = 16;
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(vbox);

        var title = CreateLabel("LOJA (SHOP)", 14, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _goldLabel = CreateLabel("Fichas (Gold): 0", 10, SuccessGreen);
        _goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_goldLabel);

        vbox.AddChild(CreateFixedSpacer(6));

        // Upgrades
        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 12);
        grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

        // Upgrade 1
        var multBox = new VBoxContainer();
        multBox.Alignment = BoxContainer.AlignmentMode.Center;
        multBox.AddChild(CreateLabel("+1 Multiplicador", 8, Accent));
        _buyMultBtn = CreateStyledButton("Comprar (10g)", BtnPurple, BtnPurpleHover, new Vector2(80, 20));
        _buyMultBtn.Pressed += () => TryBuy("multiplier", 10);
        multBox.AddChild(_buyMultBtn);
        grid.AddChild(multBox);

        // Upgrade 2
        var handBox = new VBoxContainer();
        handBox.Alignment = BoxContainer.AlignmentMode.Center;
        handBox.AddChild(CreateLabel("+1 Mão/Rodada", 8, Accent));
        _buyHandBtn = CreateStyledButton("Comprar (15g)", BtnPurple, BtnPurpleHover, new Vector2(80, 20));
        _buyHandBtn.Pressed += () => TryBuy("hand", 15);
        handBox.AddChild(_buyHandBtn);
        grid.AddChild(handBox);

        // Upgrade 3
        var discardBox = new VBoxContainer();
        discardBox.Alignment = BoxContainer.AlignmentMode.Center;
        discardBox.AddChild(CreateLabel("+1 Descarte/Rodada", 8, Accent));
        _buyDiscardBtn = CreateStyledButton("Comprar (8g)", BtnPurple, BtnPurpleHover, new Vector2(80, 20));
        _buyDiscardBtn.Pressed += () => TryBuy("discard", 8);
        discardBox.AddChild(_buyDiscardBtn);
        grid.AddChild(discardBox);

        vbox.AddChild(grid);
        vbox.AddChild(CreateFixedSpacer(10));

        _shopNextRoundBtn = CreateStyledButton("Próxima Rodada", BtnGreen, BtnGreenHover, new Vector2(100, 24));
        _shopNextRoundBtn.Pressed += () =>
        {
            _shopOverlay.Visible = false;
            _game.StartNextRound();
        };
        _shopNextRoundBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(_shopNextRoundBtn);
    }

    private void TryBuy(string type, int cost)
    {
        if (_game.BuyUpgrade(type, cost))
        {
            RefreshShopUI();
            // Optional: play sound or particle
        }
    }

    private void RefreshShopUI()
    {
        _goldLabel.Text = $"Fichas (Gold): {_game.Gold}";
        _buyMultBtn.Disabled = _game.Gold < 10;
        _buyHandBtn.Disabled = _game.Gold < 15;
        _buyDiscardBtn.Disabled = _game.Gold < 8;
    }

    private void BuildTutorialPanel()
    {
        _tutorialPanel = new PanelContainer();
        _tutorialPanel.SetAnchorsPreset(LayoutPreset.BottomWide);
        _tutorialPanel.AnchorTop = 0.72f;
        _tutorialPanel.AnchorBottom = 0.95f;
        _tutorialPanel.AnchorLeft = 0.15f;
        _tutorialPanel.AnchorRight = 0.85f;
        _tutorialPanel.OffsetTop = 0;
        _tutorialPanel.OffsetBottom = 0;
        _tutorialPanel.OffsetLeft = 0;
        _tutorialPanel.OffsetRight = 0;
        _tutorialPanel.Visible = false;
        _tutorialPanel.MouseFilter = MouseFilterEnum.Stop;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.06f, 0.06f, 0.12f, 0.92f);
        panelStyle.SetCornerRadiusAll(8);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = new Color(0.3f, 0.7f, 1f);
        panelStyle.ContentMarginLeft = 16;
        panelStyle.ContentMarginRight = 16;
        panelStyle.ContentMarginTop = 10;
        panelStyle.ContentMarginBottom = 10;
        _tutorialPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_tutorialPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        _tutorialPanel.AddChild(vbox);

        // Icon + title row
        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 6);
        var icon = CreateLabel("📖", 10, new Color(0.3f, 0.7f, 1f));
        titleRow.AddChild(icon);
        _tutorialTitle = CreateLabel("Tutorial", 10, new Color(0.3f, 0.7f, 1f));
        titleRow.AddChild(_tutorialTitle);
        vbox.AddChild(titleRow);

        _tutorialDesc = CreateLabel("", 7, TextPrimary);
        _tutorialDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _tutorialDesc.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddChild(_tutorialDesc);

        var btnRow = new HBoxContainer();
        btnRow.Alignment = BoxContainer.AlignmentMode.End;
        btnRow.AddThemeConstantOverride("separation", 8);

        _tutorialSkipBtn = CreateStyledButton("Pular Tutorial", BtnRed, BtnRedHover, new Vector2(70, 18));
        _tutorialSkipBtn.Pressed += () =>
        {
            _tutorialPanel.Visible = false;
        };
        btnRow.AddChild(_tutorialSkipBtn);

        _tutorialNextBtn = CreateStyledButton("Próximo ➜", BtnGreen, BtnGreenHover, new Vector2(70, 18));
        _tutorialNextBtn.Pressed += () =>
        {
            _tutorialController?.AdvanceStep();
        };
        btnRow.AddChild(_tutorialNextBtn);

        vbox.AddChild(btnRow);
    }

    private void OnTutorialStep(int step, string title, string desc)
    {
        _tutorialTitle.Text = title;
        _tutorialDesc.Text = desc;
        _tutorialPanel.Visible = true;
    }

    private void OnTutorialComplete()
    {
        _tutorialPanel.Visible = false;
    }

    // ===== CARD DISPLAY =====

    private void RefreshDealerDisplay()
    {
        while (_dealerCardContainer.GetChildCount() > 0)
        {
            var child = _dealerCardContainer.GetChild(0);
            _dealerCardContainer.RemoveChild(child);
            child.QueueFree();
        }
        _dealerCardPanels.Clear();

        // Spawn 5 face-down cards for the dealer
        var tween = CreateTween();
        tween.SetParallel(true);
        for (int i = 0; i < 5; i++)
        {
            var panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(30, 44);
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.1f, 0.12f, 0.2f);
            style.BorderColor = new Color(0.3f, 0.3f, 0.45f);
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(4);
            panel.AddThemeStyleboxOverride("panel", style);
            
            // Add a cross hatch or back design
            var backLabel = new Label();
            backLabel.Text = "✧";
            backLabel.HorizontalAlignment = HorizontalAlignment.Center;
            backLabel.VerticalAlignment = VerticalAlignment.Center;
            backLabel.AddThemeFontSizeOverride("font_size", 14);
            backLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
            panel.AddChild(backLabel);

            _dealerCardContainer.AddChild(panel);
            _dealerCardPanels.Add(panel);

            // Animation
            panel.Modulate = new Color(1, 1, 1, 0);
            panel.Position = new Vector2(panel.Position.X, -30); // Start above
            float delay = i * 0.1f;
            tween.TweenProperty(panel, "modulate", new Color(1, 1, 1, 1), 0.3f).SetDelay(delay);
            tween.TweenProperty(panel, "position:y", 0.0f, 0.3f).SetDelay(delay).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        }
    }

    private void RefreshCardDisplay()
    {
        // Clear existing cards
        while (_cardContainer.GetChildCount() > 0)
        {
            var child = _cardContainer.GetChild(0);
            _cardContainer.RemoveChild(child);
            child.QueueFree();
        }
        _cardPanels.Clear();
        _hoveredCards.Clear();

        // Create new card panels
        var hand = _game.GetPlayerHand();
        var tween = CreateTween();
        tween.SetParallel(true);
        
        for (int i = 0; i < hand.Count; i++)
        {
            var panel = CreateCardPanel(hand[i], i);
            _cardContainer.AddChild(panel);
            _cardPanels.Add(panel);
            
            // Slide in animation
            panel.Modulate = new Color(1, 1, 1, 0);
            panel.Position = new Vector2(panel.Position.X, 50); // Start below
            
            float delay = i * 0.05f;
            tween.TweenProperty(panel, "modulate", new Color(1, 1, 1, 1), 0.3f).SetDelay(delay).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(panel, "position:y", 0.0f, 0.3f).SetDelay(delay).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        UpdateActionButtons();
    }

    private PanelContainer CreateCardPanel(CardData card, int index)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(30, 44);
        panel.MouseFilter = MouseFilterEnum.Stop;
        panel.MouseDefaultCursorShape = CursorShape.PointingHand;

        // Content
        var vbox = new VBoxContainer();
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", -2);

        bool isRed = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds;
        var suitColor = isRed ? RedSuit : BlackSuit;

        var rankLbl = new Label();
        rankLbl.Text = card.GetRankString();
        rankLbl.HorizontalAlignment = HorizontalAlignment.Center;
        rankLbl.AddThemeFontSizeOverride("font_size", 9);
        rankLbl.AddThemeColorOverride("font_color", suitColor);
        rankLbl.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(rankLbl);

        var suitLbl = new Label();
        suitLbl.Text = card.GetSuitSymbol();
        suitLbl.HorizontalAlignment = HorizontalAlignment.Center;
        suitLbl.AddThemeFontSizeOverride("font_size", 12);
        suitLbl.AddThemeColorOverride("font_color", suitColor);
        suitLbl.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(suitLbl);

        panel.AddChild(vbox);

        // Initial style
        ApplyCardStyle(panel, false, false);

        // Input handling
        int cardIdx = index;
        panel.GuiInput += (@event) =>
        {
            if (_ignoreInput) return;
            if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                ToggleCardSelection(cardIdx);
            }
        };
        panel.MouseEntered += () =>
        {
            _hoveredCards.Add(cardIdx);
            bool selected = _game.GetSelectedIndices().Contains(cardIdx);
            ApplyCardStyle(_cardPanels[cardIdx], selected, true);
        };
        panel.MouseExited += () =>
        {
            _hoveredCards.Remove(cardIdx);
            bool selected = _game.GetSelectedIndices().Contains(cardIdx);
            ApplyCardStyle(_cardPanels[cardIdx], selected, false);
        };

        return panel;
    }

    private void ApplyCardStyle(PanelContainer panel, bool selected, bool hovered)
    {
        var style = new StyleBoxFlat();

        if (selected)
        {
            style.BgColor = hovered ? new Color(0.14f, 0.17f, 0.32f) : CardSelectedBg;
            style.BorderColor = Gold;
            style.SetBorderWidthAll(2);
        }
        else
        {
            style.BgColor = hovered ? CardHoverBg : CardBg;
            style.BorderColor = hovered ? CardHoverBorder : CardBorder;
            style.SetBorderWidthAll(1);
        }

        style.SetCornerRadiusAll(4);
        style.ContentMarginLeft = 4;
        style.ContentMarginRight = 4;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;

        panel.AddThemeStyleboxOverride("panel", style);
    }

    private void ToggleCardSelection(int index)
    {
        if (_game.CurrentPhase != PokerGameManager.GamePhase.PlayerTurn) return;

        _game.ToggleCard(index);

        // Sync visual with actual state
        bool isSelected = _game.GetSelectedIndices().Contains(index);
        bool isHovered = _hoveredCards.Contains(index);
        ApplyCardStyle(_cardPanels[index], isSelected, isHovered);

        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        int selectedCount = _game.GetSelectedIndices().Count;
        
        _playBtn.Disabled = !_game.CanPlayHand();
        _discardBtn.Disabled = !_game.CanDiscard();

        // Update button text with counts
        _playBtn.Text = selectedCount > 0 ? $"Jogar Mão ({selectedCount})" : "Jogar Mão";
        _discardBtn.Text = $"Descartar ({_game.DiscardsRemaining})";

        // Update resource labels
        _handsLabel.Text = $"Mãos: {_game.HandsRemaining}/{PokerGameManager.HandsPerRound}";
        _discardsLabel.Text = $"Descartes: {_game.DiscardsRemaining}/{PokerGameManager.DiscardsPerRound}";
        _relicsLabel.Text = _game.Relics?.GetHudText() ?? "Relíquias: —";
        _opponentsLabel.Text = $"Mesa: {_game.OpponentCount} bot{(_game.OpponentCount > 1 ? "s" : "")}";
    }

    // ===== EVENT HANDLERS =====

    private void OnHandDealt()
    {
        ClearTableCards();
        RefreshCardDisplay();
        RefreshDealerDisplay();
    }

    private void OnScoreUpdated(int roundScore, int roundTarget)
    {
        _roundLabel.Text = $"Rodada {_game.CurrentRound}/{PokerGameManager.MaxRounds}";
        _scoreLabel.Text = roundScore.ToString();
        _targetLabel.Text = roundTarget.ToString();
        _scoreBar.MaxValue = roundTarget;
        _scoreBar.Value = Mathf.Min(roundScore, roundTarget);
    }

    private void OnHandScored(string handName, int score, string breakdown)
    {
        _resultLabel.Text = $"{handName}!  +{score}";
        _breakdownLabel.Text = breakdown;
        ShowScorePopup(handName, score, breakdown);

        // Reveal dealer cards
        var tween = CreateTween();
        tween.SetParallel(true);
        for (int i = 0; i < _dealerCardPanels.Count; i++)
        {
            var panel = _dealerCardPanels[i];
            int cardIndex = i;
            // Set pivot for flip animation
            panel.PivotOffset = new Vector2(panel.Size.X / 2, panel.Size.Y / 2);
            float delay = i * 0.1f;
            
            // Flip animation
            tween.TweenProperty(panel, "scale:x", 0.0f, 0.15f).SetDelay(delay);
            tween.TweenCallback(Callable.From(() => {
                if (cardIndex < _game.GetDealerHand().Count)
                    RevealDealerCard(panel, _game.GetDealerHand()[cardIndex]);
            })).SetDelay(delay + 0.15f);
            tween.TweenProperty(panel, "scale:x", 1.0f, 0.15f).SetDelay(delay + 0.15f);
        }
    }

    private void RevealDealerCard(PanelContainer panel, CardData card)
    {
        while (panel.GetChildCount() > 0)
        {
            var child = panel.GetChild(0);
            panel.RemoveChild(child);
            child.QueueFree();
        }

        bool isRed = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds;
        var color = isRed ? RedSuit : BlackSuit;
        var cardContent = new VBoxContainer();
        cardContent.Alignment = BoxContainer.AlignmentMode.Center;
        cardContent.AddThemeConstantOverride("separation", -2);

        var rank = CreateLabel(card.GetRankString(), 9, color);
        rank.HorizontalAlignment = HorizontalAlignment.Center;
        cardContent.AddChild(rank);

        var suit = CreateLabel(card.GetSuitSymbol(), 12, color);
        suit.HorizontalAlignment = HorizontalAlignment.Center;
        cardContent.AddChild(suit);
        panel.AddChild(cardContent);
    }

    private void OnRoundEnded(int round, bool passed)
    {
        _bossDefeatPortrait.Visible = false;
        
        if (passed)
        {
            _overlayTitle.Text = $"Rodada {round} Completa!";
            _overlayTitle.AddThemeColorOverride("font_color", SuccessGreen);
            _overlaySubtitle.Text = $"Pontuação: {_game.RoundScore}/{_game.RoundTarget}";
            _nextRoundBtn.Visible = true;
            _newGameBtn.Visible = false;
        }
        else
        {
            _overlayTitle.Text = "Rodada Falhou";
            _overlayTitle.AddThemeColorOverride("font_color", Accent);
            _overlaySubtitle.Text = $"Pontuação: {_game.RoundScore}/{_game.RoundTarget}";
            _nextRoundBtn.Visible = false;
            _newGameBtn.Visible = true;
        }
        _overlayPanel.Visible = true;
    }

    private void OnGameEnded(bool won, int totalScore)
    {
        if (won)
        {
            _overlayTitle.Text = "Vitória!";
            _overlayTitle.AddThemeColorOverride("font_color", Gold);
            _overlaySubtitle.Text = $"Pontuação total: {totalScore}";
            _bossDefeatPortrait.Visible = true;
        }
        else
        {
            _overlayTitle.Text = "Derrota";
            _overlayTitle.AddThemeColorOverride("font_color", Accent);
            _overlaySubtitle.Text = $"Pontuação: {_game.RoundScore}/{_game.RoundTarget}";
            _bossDefeatPortrait.Visible = false;
        }
        _nextRoundBtn.Visible = false;
        _newGameBtn.Visible = true;
        _overlayPanel.Visible = true;
    }

    private void OnPhaseChanged(int phase)
    {
        _ignoreInput = (PokerGameManager.GamePhase)phase != PokerGameManager.GamePhase.PlayerTurn;
        UpdateActionButtons();
    }

    // ===== BUTTON HANDLERS =====

    private void OnPlayPressed()
    {
        AnimatePlayedCards();
        _game.PlayHand();
    }

    private void AnimatePlayedCards()
    {
        var selected = _game.GetSelectedIndices().OrderBy(index => index).ToList();
        if (selected.Count == 0) return;

        ClearTableCards();
        var hand = _game.GetPlayerHand();
        float duration = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.01f : 0.42f;
        var center = GetViewportRect().Size / 2f;

        for (int i = 0; i < selected.Count; i++)
        {
            int index = selected[i];
            if (index >= hand.Count || index >= _cardPanels.Count) continue;

            var source = _cardPanels[index].GetGlobalRect();
            var tableCard = CreateTableCard(hand[index]);
            tableCard.Position = source.GetCenter() - tableCard.Size / 2f;
            tableCard.Rotation = Mathf.DegToRad(-9 + i * 4);
            _tableCardLayer.AddChild(tableCard);

            float spacing = 42f;
            var target = center + new Vector2((i - (selected.Count - 1) / 2f) * spacing, 8);
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(tableCard, "position", target - tableCard.Size / 2f, duration)
                .SetDelay(i * 0.07f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(tableCard, "scale", new Vector2(1.22f, 1.22f), duration)
                .SetDelay(i * 0.07f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
    }

    private PanelContainer CreateTableCard(CardData card)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(34, 50);
        panel.Size = panel.CustomMinimumSize;
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.96f, 0.95f, 0.88f);
        style.BorderColor = Gold;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", style);

        bool isRed = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds;
        var color = isRed ? new Color(0.75f, 0.12f, 0.16f) : new Color(0.08f, 0.1f, 0.18f);
        var box = new VBoxContainer();
        box.Alignment = BoxContainer.AlignmentMode.Center;
        box.AddThemeConstantOverride("separation", -3);
        var rank = CreateLabel(card.GetRankString(), 10, color);
        rank.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(rank);
        var suit = CreateLabel(card.GetSuitSymbol(), 14, color);
        suit.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(suit);
        panel.AddChild(box);
        return panel;
    }

    private void ShowScorePopup(string handName, int score, string breakdown)
    {
        _scorePopup.Text = $"{handName}\n+{score}  {breakdown}";
        _scorePopup.Position = GetViewportRect().Size / 2f + new Vector2(-140, -84);
        _scorePopup.Modulate = new Color(1, 1, 1, 0);
        _scorePopup.Scale = new Vector2(0.65f, 0.65f);
        _scorePopup.Visible = true;
        float duration = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.01f : 0.25f;
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_scorePopup, "modulate:a", 1.0f, duration);
        tween.TweenProperty(_scorePopup, "scale", Vector2.One, duration).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Chain().TweenInterval(0.8f);
        tween.TweenProperty(_scorePopup, "modulate:a", 0.0f, duration);
        tween.TweenCallback(Callable.From(() => _scorePopup.Visible = false));
    }

    private void ClearTableCards()
    {
        if (_tableCardLayer == null) return;
        foreach (var child in _tableCardLayer.GetChildren())
        {
            if (child != _scorePopup)
                child.QueueFree();
        }
    }

    private void OnDiscardPressed()
    {
        _game.DiscardCards();
    }

    // ===== HELPERS =====

    private Label CreateLabel(string text, int fontSize, Color color)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", color);
        return lbl;
    }

    private Control CreateExpandSpacer()
    {
        var spacer = new Control();
        spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        return spacer;
    }

    private Control CreateFixedSpacer(float height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        return spacer;
    }

    private Button CreateStyledButton(string text, Color normalColor, Color hoverColor, Vector2 minSize)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = minSize;
        btn.AddThemeFontSizeOverride("font_size", 8);
        btn.AddThemeColorOverride("font_color", TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.35f, 0.35f, 0.4f));

        var normal = new StyleBoxFlat();
        normal.BgColor = normalColor;
        normal.SetCornerRadiusAll(4);
        normal.ContentMarginLeft = 6;
        normal.ContentMarginRight = 6;
        normal.ContentMarginTop = 3;
        normal.ContentMarginBottom = 3;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = normal.Duplicate() as StyleBoxFlat;
        hover.BgColor = hoverColor;
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = normal.Duplicate() as StyleBoxFlat;
        pressed.BgColor = normalColor.Darkened(0.3f);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        var disabled = normal.Duplicate() as StyleBoxFlat;
        disabled.BgColor = new Color(0.15f, 0.15f, 0.2f);
        btn.AddThemeStyleboxOverride("disabled", disabled);

        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        return btn;
    }
}
