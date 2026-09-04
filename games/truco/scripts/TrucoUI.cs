using Godot;
using System.Collections.Generic;

namespace GameHub.Games.Truco;

/// <summary>
/// Full Truco UI with cards, table, tombos, scoreboard, truco overlay.
/// Styled to match the cyberpunk poker theme.
/// </summary>
public partial class TrucoUI : Control
{
    private TrucoGameManager _game;

    // ===== UI REFS =====
    private Label _scoreLabel;
    private Label _viraLabel;
    private Label _stakesLabel;
    private Label _statusLabel;
    private HBoxContainer _playerHandContainer;
    private HBoxContainer _tablePlayerRow;
    private HBoxContainer _tableOpponentRow;
    private HBoxContainer _tombosContainer;
    private Button _trucoBtn;
    private Button _cutDeckBtn;
    private CenterContainer _viraCardContainer;
    private Control _penaOverlay;
    private Label _penaLabel;
    private Button _givePenaBtn;
    private Button _skipPenaBtn;
    private Button _keepPenaBtn;
    private Button _tomboPenaBtn;
    private Control _dealAnimationLayer;
    private PanelContainer _deckStackVisual;

    // Overlay
    private Control _trucoOverlay;
    private Label _trucoOverlayLabel;
    private Button _acceptBtn;
    private Button _declineBtn;
    private Button _raiseBtn;
    private TextureRect _trucoPortrait;

    // Hand end overlay
    private Control _handOverlay;
    private Label _handOverlayTitle;
    private Label _handOverlaySubtitle;
    private Button _nextHandBtn;
    private Button _backMenuBtn;

    private List<PanelContainer> _cardPanels = new();

    // ===== THEME COLORS =====
    private static readonly Color BgDark = new(0.05f, 0.05f, 0.09f);
    private static readonly Color PanelBg = new(0.09f, 0.09f, 0.15f);
    private static readonly Color CardBg = new(0.09f, 0.12f, 0.22f);
    private static readonly Color CardBorder = new(0.17f, 0.2f, 0.35f);
    private static readonly Color CardHoverBg = new(0.12f, 0.15f, 0.28f);
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
    private static readonly Color TrucoOrange = new(1f, 0.6f, 0.1f);

    // ===== LIFECYCLE =====

    public override void _Ready()
    {
        _game = GetNode<TrucoGameManager>("../../GameManager");
        BuildUI();
        ConnectSignals();
        _game.StartMatch();
    }

    private void ConnectSignals()
    {
        _game.HandDealt += OnHandDealt;
        _game.DeckShuffled += OnDeckShuffled;
        _game.DeckCut += OnDeckCut;
        _game.PenaAvailable += OnPenaAvailable;
        _game.PenaDelivered += OnPenaDelivered;
        _game.PenaResolved += OnPenaResolved;
        _game.DistributionStarted += OnDistributionStarted;
        _game.HandCleanupStarted += OnHandCleanupStarted;
        _game.ScoreUpdated += OnScoreUpdated;
        _game.ViraRevealed += OnViraRevealed;
        _game.TrucoCalled += OnTrucoCalled;
        _game.CardPlayed += OnCardPlayed;
        _game.RoundResolved += OnRoundResolved;
        _game.HandEnded += OnHandEnded;
        _game.GameEnded += OnGameEnded;
        _game.PhaseChanged += OnPhaseChanged;
    }

    public override void _ExitTree()
    {
        if (_game != null)
        {
            _game.HandDealt -= OnHandDealt;
            _game.DeckShuffled -= OnDeckShuffled;
            _game.DeckCut -= OnDeckCut;
            _game.PenaAvailable -= OnPenaAvailable;
            _game.PenaDelivered -= OnPenaDelivered;
            _game.PenaResolved -= OnPenaResolved;
            _game.DistributionStarted -= OnDistributionStarted;
            _game.HandCleanupStarted -= OnHandCleanupStarted;
            _game.ScoreUpdated -= OnScoreUpdated;
            _game.ViraRevealed -= OnViraRevealed;
            _game.TrucoCalled -= OnTrucoCalled;
            _game.CardPlayed -= OnCardPlayed;
            _game.RoundResolved -= OnRoundResolved;
            _game.HandEnded -= OnHandEnded;
            _game.GameEnded -= OnGameEnded;
            _game.PhaseChanged -= OnPhaseChanged;
        }
    }

    // ===== UI BUILD =====

    private void BuildUI()
    {
        // Keep the casino visible across the whole canvas. The 3D backdrop was
        // only visible in a narrow band because it sat behind the table.
        var backdrop = new TextureRect();
        backdrop.Texture = ResourceLoader.Load<Texture2D>("res://assets/sprites/backgrounds/cyber_casino.jpg");
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        backdrop.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        backdrop.StretchMode = TextureRect.StretchModeEnum.Scale;
        backdrop.Modulate = new Color(1, 1, 1, 0.38f);
        backdrop.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(backdrop);

        var bg = new ColorRect();
        bg.Color = new Color(0, 0, 0, 0.2f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        _dealAnimationLayer = new Control();
        _dealAnimationLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _dealAnimationLayer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_dealAnimationLayer);

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

        var titleLabel = CreateLabel($"♣ TRUCO {_game.TeamSize}v{_game.TeamSize}", 8, Gold);
        topBar.AddChild(titleLabel);

        topBar.AddChild(CreateExpandSpacer());

        _viraLabel = CreateLabel("Vira: ?", 8, TrucoOrange);
        topBar.AddChild(_viraLabel);

        topBar.AddChild(CreateExpandSpacer());

        _stakesLabel = CreateLabel("Aposta: 1", 8, Accent);
        topBar.AddChild(_stakesLabel);

        topBar.AddChild(CreateExpandSpacer());

        var backBtn = CreateStyledButton("Menu", BtnRed, BtnRedHover, new Vector2(40, 16));
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        topBar.AddChild(backBtn);

        mainVBox.AddChild(topBar);

        // --- Score bar ---
        var scoreRow = new HBoxContainer();
        scoreRow.AddThemeConstantOverride("separation", 10);

        _scoreLabel = CreateLabel("Nós: 0 × Eles: 0", 8, TextPrimary);
        scoreRow.AddChild(_scoreLabel);

        scoreRow.AddChild(CreateExpandSpacer());

        // Tombos indicator
        var tombosLabel = CreateLabel("Tombos:", 7, TextSecondary);
        scoreRow.AddChild(tombosLabel);

        _tombosContainer = new HBoxContainer();
        _tombosContainer.AddThemeConstantOverride("separation", 4);
        for (int i = 0; i < 3; i++)
        {
            var dot = CreateLabel("○", 10, TextSecondary);
            dot.Name = $"Tombo{i}";
            _tombosContainer.AddChild(dot);
        }
        scoreRow.AddChild(_tombosContainer);

        mainVBox.AddChild(scoreRow);

        // --- Separator ---
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 2);
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = new Color(0.2f, 0.2f, 0.35f);
        sepStyle.ContentMarginTop = 1;
        sepStyle.ContentMarginBottom = 1;
        sep.AddThemeStyleboxOverride("separator", sepStyle);
        mainVBox.AddChild(sep);

        // --- Opponent played cards ---
        var oppLabel = CreateLabel("Oponente", 7, TextSecondary);
        oppLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainVBox.AddChild(oppLabel);

        var oppCenter = new CenterContainer();
        oppCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _tableOpponentRow = new HBoxContainer();
        _tableOpponentRow.AddThemeConstantOverride("separation", 8);
        oppCenter.AddChild(_tableOpponentRow);
        mainVBox.AddChild(oppCenter);

        // --- Spacer (table area) with Vira Card ---
        var tableArea = new MarginContainer();
        tableArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        tableArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        var viraHBox = new HBoxContainer();
        viraHBox.Alignment = BoxContainer.AlignmentMode.Center;
        viraHBox.AddThemeConstantOverride("separation", 15);
        tableArea.AddChild(viraHBox);

        var viraTextVBox = new VBoxContainer();
        viraTextVBox.Alignment = BoxContainer.AlignmentMode.Center;
        var vTitle = CreateLabel("CARTA DO TOMBO", 6, TrucoOrange);
        vTitle.HorizontalAlignment = HorizontalAlignment.Center;
        viraTextVBox.AddChild(vTitle);
        _viraLabel = CreateLabel("(Vira)", 6, Gold);
        _viraLabel.HorizontalAlignment = HorizontalAlignment.Center;
        viraTextVBox.AddChild(_viraLabel);
        viraHBox.AddChild(viraTextVBox);

        _viraCardContainer = new CenterContainer();
        viraHBox.AddChild(_viraCardContainer);

        mainVBox.AddChild(tableArea);

        _statusLabel = CreateLabel("Sua vez!", 8, SuccessGreen);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(_statusLabel);

        mainVBox.AddChild(CreateExpandSpacer());

        // --- Player played cards ---
        var playerPlayedLabel = CreateLabel("Suas jogadas", 7, TextSecondary);
        playerPlayedLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainVBox.AddChild(playerPlayedLabel);

        var playerCenter = new CenterContainer();
        playerCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _tablePlayerRow = new HBoxContainer();
        _tablePlayerRow.AddThemeConstantOverride("separation", 8);
        playerCenter.AddChild(_tablePlayerRow);
        mainVBox.AddChild(playerCenter);

        // --- Action buttons ---
        var actionCenter = new CenterContainer();
        actionCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 14);

        _trucoBtn = CreateStyledButton("TRUCO!", TrucoOrange, new Color(1f, 0.7f, 0.2f), new Vector2(70, 22));
        _trucoBtn.Pressed += () => _game.RequestTruco();
        actionRow.AddChild(_trucoBtn);

        _cutDeckBtn = CreateStyledButton("Cortar", BtnPurple, BtnPurpleHover, new Vector2(70, 22));
        _cutDeckBtn.Pressed += () => _game.CutDeck();
        _cutDeckBtn.Visible = false;
        actionRow.AddChild(_cutDeckBtn);

        actionCenter.AddChild(actionRow);
        mainVBox.AddChild(actionCenter);

        mainVBox.AddChild(CreateFixedSpacer(4));

        // --- Player hand ---
        var handCenter = new CenterContainer();
        handCenter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _playerHandContainer = new HBoxContainer();
        _playerHandContainer.AddThemeConstantOverride("separation", 6);
        handCenter.AddChild(_playerHandContainer);
        mainVBox.AddChild(handCenter);

        // --- Status label ---
        mainVBox.AddChild(CreateFixedSpacer(2));

        // --- Overlays ---
        BuildTrucoOverlay();
        BuildHandOverlay();
        BuildPenaOverlay();
        Core.Visuals.AccessibilityVisuals.AddGlobalFilter(this);
    }

    private void BuildPenaOverlay()
    {
        _penaOverlay = new ColorRect { Color = new Color(0, 0, 0, 0.78f) };
        _penaOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _penaOverlay.Visible = false;
        _penaOverlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_penaOverlay);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _penaOverlay.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(270, 135) };
        var style = new StyleBoxFlat { BgColor = PanelBg, BorderColor = Gold };
        style.SetBorderWidthAll(2); style.SetCornerRadiusAll(8);
        style.ContentMarginLeft = 16; style.ContentMarginRight = 16;
        style.ContentMarginTop = 14; style.ContentMarginBottom = 14;
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);
        var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);
        var title = CreateLabel("PENA", 14, Gold); title.HorizontalAlignment = HorizontalAlignment.Center; box.AddChild(title);
        _penaLabel = CreateLabel("", 8, TextPrimary); _penaLabel.HorizontalAlignment = HorizontalAlignment.Center; box.AddChild(_penaLabel);
        var row = new HBoxContainer(); row.Alignment = BoxContainer.AlignmentMode.Center; row.AddThemeConstantOverride("separation", 6); box.AddChild(row);
        _givePenaBtn = CreateStyledButton("Entregar", BtnGreen, BtnGreenHover, new Vector2(62, 20));
        _givePenaBtn.Pressed += () => _game.GivePena(); row.AddChild(_givePenaBtn);
        _skipPenaBtn = CreateStyledButton("Sem pena", BtnPurple, BtnPurpleHover, new Vector2(62, 20));
        _skipPenaBtn.Pressed += () => _game.ResolvePena(false); row.AddChild(_skipPenaBtn);
        _keepPenaBtn = CreateStyledButton("Ficar", BtnGreen, BtnGreenHover, new Vector2(55, 20));
        _keepPenaBtn.Pressed += () => _game.ResolvePena(true); _keepPenaBtn.Visible = false; row.AddChild(_keepPenaBtn);
        _tomboPenaBtn = CreateStyledButton("Virar tombo", BtnRed, BtnRedHover, new Vector2(75, 20));
        _tomboPenaBtn.Pressed += () => _game.ResolvePena(false); _tomboPenaBtn.Visible = false; row.AddChild(_tomboPenaBtn);
    }

    private void BuildTrucoOverlay()
    {
        _trucoOverlay = new ColorRect();
        ((ColorRect)_trucoOverlay).Color = new Color(0, 0, 0, 0.8f);
        _trucoOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _trucoOverlay.Visible = false;
        _trucoOverlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_trucoOverlay);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _trucoOverlay.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(200, 120);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = PanelBg;
        panelStyle.SetCornerRadiusAll(8);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = TrucoOrange;
        panelStyle.ContentMarginLeft = 20;
        panelStyle.ContentMarginRight = 20;
        panelStyle.ContentMarginTop = 14;
        panelStyle.ContentMarginBottom = 14;
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(hbox);
        
        _trucoPortrait = new TextureRect();
        _trucoPortrait.CustomMinimumSize = new Vector2(100, 100);
        _trucoPortrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _trucoPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _trucoPortrait.Texture = ResourceLoader.Load<Texture2D>("res://assets/sprites/portraits/truco_player.jpg");
        hbox.AddChild(_trucoPortrait);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddChild(vbox);

        _trucoOverlayLabel = CreateLabel("TRUCO!", 14, TrucoOrange);
        _trucoOverlayLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_trucoOverlayLabel);

        var btnRow = new HBoxContainer();
        btnRow.Alignment = BoxContainer.AlignmentMode.Center;
        btnRow.AddThemeConstantOverride("separation", 8);

        _acceptBtn = CreateStyledButton("Aceitar", BtnGreen, BtnGreenHover, new Vector2(55, 18));
        _acceptBtn.Pressed += () => _game.RespondToTruco(true, false);
        btnRow.AddChild(_acceptBtn);

        _raiseBtn = CreateStyledButton("Seis!", TrucoOrange, new Color(1f, 0.7f, 0.2f), new Vector2(55, 18));
        _raiseBtn.Pressed += () => _game.RespondToTruco(false, true);
        btnRow.AddChild(_raiseBtn);

        _declineBtn = CreateStyledButton("Correr", BtnRed, BtnRedHover, new Vector2(55, 18));
        _declineBtn.Pressed += () => _game.RespondToTruco(false, false);
        btnRow.AddChild(_declineBtn);

        vbox.AddChild(btnRow);
    }

    private void BuildHandOverlay()
    {
        _handOverlay = new ColorRect();
        ((ColorRect)_handOverlay).Color = new Color(0, 0, 0, 0.75f);
        _handOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _handOverlay.Visible = false;
        _handOverlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_handOverlay);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _handOverlay.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(240, 140);
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
        vbox.AddThemeConstantOverride("separation", 6);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(vbox);

        _handOverlayTitle = CreateLabel("", 12, Gold);
        _handOverlayTitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_handOverlayTitle);

        _handOverlaySubtitle = CreateLabel("", 8, TextSecondary);
        _handOverlaySubtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_handOverlaySubtitle);

        vbox.AddChild(CreateFixedSpacer(4));

        _nextHandBtn = CreateStyledButton("Próxima Mão", BtnGreen, BtnGreenHover, new Vector2(90, 20));
        _nextHandBtn.Pressed += () =>
        {
            _handOverlay.Visible = false;
            _game.StartNewHand();
        };
        vbox.AddChild(_nextHandBtn);

        _backMenuBtn = CreateStyledButton("Voltar ao Menu", BtnRed, BtnRedHover, new Vector2(90, 20));
        _backMenuBtn.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        vbox.AddChild(_backMenuBtn);
    }

    // ===== REFRESH =====

    private void RefreshPlayerHand()
    {
        while (_playerHandContainer.GetChildCount() > 0)
        {
            var child = _playerHandContainer.GetChild(0);
            _playerHandContainer.RemoveChild(child);
            child.QueueFree();
        }
        _cardPanels.Clear();

        for (int i = 0; i < _game.PlayerHand.Count; i++)
        {
            var card = _game.PlayerHand[i];
            var panel = CreateCardPanel(card, i);
            _playerHandContainer.AddChild(panel);
            _cardPanels.Add(panel);
        }
    }

    private void RefreshTableCards()
    {
        ClearContainer(_tablePlayerRow);
        ClearContainer(_tableOpponentRow);

        for (int i = 0; i < 3; i++)
        {
            // Player played
            if (_game.PlayerPlayed[i] != null)
            {
                var card = _game.PlayerPlayed[i];
                _tablePlayerRow.AddChild(CreateSmallCard(card, false));
            }
            else
            {
                _tablePlayerRow.AddChild(CreateEmptySlot());
            }

            // Opponent played
            if (_game.OpponentPlayed[i] != null)
            {
                var card = _game.OpponentPlayed[i];
                _tableOpponentRow.AddChild(CreateSmallCard(card, false));
            }
            else
            {
                _tableOpponentRow.AddChild(CreateEmptySlot());
            }
        }
    }

    private void RefreshTombos()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_tombosContainer.GetChild(i) is Label dot)
            {
                int w = _game.RoundWinners[i];
                if (w == 0) { dot.Text = "●"; dot.AddThemeColorOverride("font_color", SuccessGreen); }
                else if (w == 1) { dot.Text = "●"; dot.AddThemeColorOverride("font_color", Accent); }
                else if (w == 2) { dot.Text = "◐"; dot.AddThemeColorOverride("font_color", Gold); }
                else { dot.Text = "○"; dot.AddThemeColorOverride("font_color", TextSecondary); }
            }
        }
    }

    // ===== EVENT HANDLERS =====

    private void OnHandDealt()
    {
        RefreshPlayerHand();
        RefreshTableCards();
        RefreshTombos();
        _trucoOverlay.Visible = false;
        _handOverlay.Visible = false;
        UpdateTrucoButton();
    }

    private void OnDeckShuffled()
    {
        _statusLabel.Text = "Baralho embaralhado — corte para distribuir";
        _statusLabel.AddThemeColorOverride("font_color", Gold);
        _cutDeckBtn.Visible = true;
        AnimateShuffle();
    }

    private void OnDeckCut(int cutPosition)
    {
        _statusLabel.Text = "Corte feito — distribuindo cartas...";
        _cutDeckBtn.Visible = false;
        AnimateCut();
    }

    private void OnPenaAvailable(string recipient)
    {
        _penaLabel.Text = $"Você pode entregar a pena para\n{recipient}.";
        _givePenaBtn.Visible = true; _skipPenaBtn.Visible = true;
        _keepPenaBtn.Visible = false; _tomboPenaBtn.Visible = false;
        _penaOverlay.Visible = true;
    }

    private void OnPenaDelivered(string cardDisplay, string recipient)
    {
        _penaLabel.Text = $"{recipient} recebeu {cardDisplay}.\nFica com a carta ou ela vira tombo?";
        _givePenaBtn.Visible = false; _skipPenaBtn.Visible = false;
        _keepPenaBtn.Visible = true; _tomboPenaBtn.Visible = true;
        AnimatePenaDelivery();
    }

    private void OnPenaResolved(bool kept, string cardDisplay)
    {
        _penaOverlay.Visible = false;
        _statusLabel.Text = kept ? $"Pena {cardDisplay} ficou com o aliado." : "A pena virou a carta do tombo.";
        AnimatePenaResolution(kept);
    }

    private void OnDistributionStarted(int cardCount, int penaRecipientSeat, bool penaKept)
    {
        _statusLabel.Text = penaKept
            ? "Pena guardada: o aliado receberá só mais 2 cartas."
            : "Distribuindo 3 cartas para cada jogador...";
        AnimateDistribution(cardCount, penaRecipientSeat, penaKept);
    }

    private void OnHandCleanupStarted(int nextDealerSeat, string nextDealerName)
    {
        _statusLabel.Text = $"Recolhendo cartas — próximo distribuidor: {nextDealerName}";
        AnimateCleanupAndPass(nextDealerSeat);
    }

    private float MotionDuration(float regular) =>
        Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.01f : regular;

    private void AnimateShuffle()
    {
        ClearDealAnimationLayer();
        _cutDeckBtn.Disabled = true;
        Vector2 center = GetViewportRect().Size / 2f + new Vector2(0, 25);
        for (int i = 0; i < 8; i++)
        {
            var card = CreateAnimatedCardBack();
            card.Position = center - card.Size / 2f + new Vector2((i % 2 == 0 ? -1 : 1) * 72, -i * 1.5f);
            card.Rotation = Mathf.DegToRad(i % 2 == 0 ? -12 : 12);
            _dealAnimationLayer.AddChild(card);
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(card, "position", center - card.Size / 2f + new Vector2(i * 0.7f, -i * 0.8f), MotionDuration(0.32f)).SetDelay(i * 0.035f);
            tween.TweenProperty(card, "rotation", 0f, MotionDuration(0.32f)).SetDelay(i * 0.035f);
        }

        _deckStackVisual = CreateAnimatedCardBack();
        _deckStackVisual.Position = center - _deckStackVisual.Size / 2f;
        _deckStackVisual.Modulate = new Color(1, 1, 1, 0);
        _dealAnimationLayer.AddChild(_deckStackVisual);
        var finish = CreateTween();
        finish.TweenInterval(MotionDuration(0.65f));
        finish.TweenProperty(_deckStackVisual, "modulate:a", 1f, MotionDuration(0.1f));
        finish.TweenCallback(Callable.From(() =>
        {
            foreach (var child in _dealAnimationLayer.GetChildren())
                if (child != _deckStackVisual) child.QueueFree();
            _cutDeckBtn.Disabled = false;
            PulseControl(_cutDeckBtn);
        }));
    }

    private void AnimateCut()
    {
        if (_deckStackVisual == null || !IsInstanceValid(_deckStackVisual)) return;
        Vector2 basePosition = _deckStackVisual.Position;
        var upperHalf = CreateAnimatedCardBack();
        upperHalf.Position = basePosition;
        _dealAnimationLayer.AddChild(upperHalf);
        var tween = CreateTween();
        tween.TweenProperty(upperHalf, "position", basePosition + new Vector2(65, -16), MotionDuration(0.18f)).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(upperHalf, "position", basePosition + new Vector2(0, -5), MotionDuration(0.18f)).SetTrans(Tween.TransitionType.Quad);
        tween.TweenCallback(Callable.From(upperHalf.QueueFree));
    }

    private void AnimatePenaDelivery()
    {
        if (_game.PenaCard == null) return;
        var card = CreateCardPanel(_game.PenaCard, -1);
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Size = new Vector2(34, 50);
        card.Position = GetViewportRect().Size / 2f - card.Size / 2f;
        _dealAnimationLayer.AddChild(card);
        var target = GetSeatScreenPosition(_game.PenaRecipientSeatIndex) - card.Size / 2f;
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(card, "position", target, MotionDuration(0.38f)).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "rotation", Mathf.DegToRad(-8), MotionDuration(0.38f));
    }

    private void AnimatePenaResolution(bool kept)
    {
        if (kept) return;
        // A declined pena returns from the ally and settles at the vira/tombo.
        var cards = _dealAnimationLayer.GetChildren();
        if (cards.Count == 0) return;
        if (cards[cards.Count - 1] is not Control card) return;
        var target = GetViewportRect().Size / 2f + new Vector2(75, -15) - card.Size / 2f;
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(card, "position", target, MotionDuration(0.4f)).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "rotation", Mathf.DegToRad(7), MotionDuration(0.4f));
    }

    private void AnimateDistribution(int cardCount, int penaRecipientSeat, bool penaKept)
    {
        int totalSeats = _game.TeamSize * 2;
        Vector2 deckPosition = GetViewportRect().Size / 2f + new Vector2(0, 25);
        int emitted = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            for (int offset = 1; offset <= totalSeats && emitted < cardCount; offset++)
            {
                int seat = (_game.DealerSeatIndex + offset) % totalSeats;
                if (penaKept && seat == penaRecipientSeat && pass == 2) continue;
                var card = CreateAnimatedCardBack();
                card.Position = deckPosition - card.Size / 2f;
                card.Scale = new Vector2(0.78f, 0.78f);
                _dealAnimationLayer.AddChild(card);
                Vector2 target = GetSeatScreenPosition(seat) + new Vector2(pass * 10 - 10, pass * -2) - card.Size / 2f;
                var tween = CreateTween();
                tween.SetParallel(true);
                float delay = emitted * 0.045f;
                tween.TweenProperty(card, "position", target, MotionDuration(0.3f)).SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(card, "rotation", Mathf.DegToRad((pass - 1) * 5), MotionDuration(0.3f)).SetDelay(delay);
                emitted++;
            }
        }
    }

    private void AnimateCleanupAndPass(int nextDealerSeat)
    {
        Vector2 center = GetViewportRect().Size / 2f + new Vector2(0, 25);
        foreach (var row in new[] { _tablePlayerRow, _tableOpponentRow })
        {
            foreach (var child in row.GetChildren())
            {
                if (child is not Control source) continue;
                var ghost = CreateAnimatedCardBack();
                ghost.Position = source.GetGlobalRect().GetCenter() - ghost.Size / 2f;
                _dealAnimationLayer.AddChild(ghost);
                CreateTween().TweenProperty(ghost, "position", center - ghost.Size / 2f, MotionDuration(0.32f)).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            }
        }
        var pass = CreateTween();
        pass.TweenInterval(MotionDuration(0.4f));
        if (_deckStackVisual == null || !IsInstanceValid(_deckStackVisual))
        {
            _deckStackVisual = CreateAnimatedCardBack();
            _deckStackVisual.Position = center - _deckStackVisual.Size / 2f;
            _dealAnimationLayer.AddChild(_deckStackVisual);
        }
        pass.TweenProperty(_deckStackVisual, "position", GetSeatScreenPosition(nextDealerSeat) - _deckStackVisual.Size / 2f, MotionDuration(0.38f)).SetTrans(Tween.TransitionType.Cubic);
        pass.TweenCallback(Callable.From(ClearDealAnimationLayer));
    }

    private PanelContainer CreateAnimatedCardBack()
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(34, 50);
        panel.Size = panel.CustomMinimumSize;
        panel.MouseFilter = MouseFilterEnum.Ignore;
        var style = new StyleBoxFlat { BgColor = new Color(0.09f, 0.12f, 0.24f), BorderColor = Gold };
        style.SetBorderWidthAll(2); style.SetCornerRadiusAll(4);
        panel.AddThemeStyleboxOverride("panel", style);
        var mark = CreateLabel("✦", 13, new Color(0.55f, 0.5f, 0.75f));
        mark.HorizontalAlignment = HorizontalAlignment.Center; mark.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(mark);
        return panel;
    }

    private Vector2 GetSeatScreenPosition(int seat)
    {
        Vector2 size = GetViewportRect().Size;
        Vector2[] positions =
        {
            new(size.X * 0.50f, size.Y * 0.84f),
            new(size.X * 0.50f, size.Y * 0.16f),
            new(size.X * 0.16f, size.Y * 0.65f),
            new(size.X * 0.84f, size.Y * 0.35f),
            new(size.X * 0.16f, size.Y * 0.35f),
            new(size.X * 0.84f, size.Y * 0.65f)
        };
        return positions[Mathf.PosMod(seat, positions.Length)];
    }

    private void PulseControl(Control control)
    {
        control.PivotOffset = control.Size / 2f;
        control.Scale = new Vector2(0.82f, 0.82f);
        CreateTween().TweenProperty(control, "scale", Vector2.One, MotionDuration(0.2f)).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void ClearDealAnimationLayer()
    {
        if (_dealAnimationLayer == null) return;
        foreach (var child in _dealAnimationLayer.GetChildren()) child.QueueFree();
        _deckStackVisual = null;
    }

    private void OnScoreUpdated(int team1, int team2)
    {
        _scoreLabel.Text = $"Nós: {team1} × Eles: {team2}";
    }

    private void OnViraRevealed(string viraDisplay, string manilhaDisplay)
    {
        _viraLabel.Text = $"Vira: {viraDisplay}\n{manilhaDisplay}";
        
        ClearContainer(_viraCardContainer);
        if (_game.ViraCard != null)
        {
            var panel = CreateCardPanel(_game.ViraCard, -1);
            panel.MouseFilter = MouseFilterEnum.Ignore; // Vira card is not clickable
            _viraCardContainer.AddChild(panel);
        }
    }

    private void OnTrucoCalled(int stakes, bool byPlayer)
    {
        _stakesLabel.Text = $"Aposta: {stakes}";

        string label = stakes switch
        {
            3 => "TRUCO!",
            6 => "SEIS!",
            9 => "NOVE!",
            12 => "DOZE!",
            _ => "TRUCO!"
        };
        _trucoOverlayLabel.Text = label;

        if (!byPlayer)
        {
            _raiseBtn.Text = stakes switch
            {
                3 => "Seis!",
                6 => "Nove!",
                9 => "Doze!",
                _ => "..."
            };
            _raiseBtn.Visible = stakes < 12;
        }

        // The portrait also punctuates a call made by the local player.  In that
        // case the response controls are hidden while the AI considers the call.
        _acceptBtn.Visible = !byPlayer;
        _declineBtn.Visible = !byPlayer;
        _raiseBtn.Visible = !byPlayer && stakes < 12;
        _trucoOverlay.Visible = true;
    }

    private void OnCardPlayed(int who, string cardDisplay, int roundIdx)
    {
        RefreshTableCards();
        RefreshPlayerHand();
    }

    private void OnRoundResolved(int roundIdx, int winner)
    {
        RefreshTombos();
        string msg = winner switch
        {
            0 => "Você ganhou o tombo!",
            1 => "Oponente ganhou o tombo!",
            _ => "Empate!"
        };
        _statusLabel.Text = msg;
        _statusLabel.AddThemeColorOverride("font_color", winner == 0 ? SuccessGreen : (winner == 1 ? Accent : Gold));
    }

    private void OnHandEnded(bool playerWon, int pointsGained)
    {
        _handOverlayTitle.Text = playerWon ? "Você ganhou!" : "Oponente ganhou!";
        _handOverlayTitle.AddThemeColorOverride("font_color", playerWon ? SuccessGreen : Accent);
        _handOverlaySubtitle.Text = $"+{pointsGained} pontos | Placar: {_game.PlayerScore} × {_game.OpponentScore}";
        _nextHandBtn.Visible = true;
        _backMenuBtn.Visible = true;
        _handOverlay.Visible = true;
    }

    private void OnGameEnded(bool playerWon, int team1Score, int team2Score)
    {
        _handOverlayTitle.Text = playerWon ? "VITÓRIA!" : "DERROTA";
        _handOverlayTitle.AddThemeColorOverride("font_color", playerWon ? Gold : Accent);
        _handOverlaySubtitle.Text = $"Placar final: {team1Score} × {team2Score}";
        _nextHandBtn.Visible = false;
        _backMenuBtn.Visible = true;
        _handOverlay.Visible = true;
    }

    private void OnPhaseChanged(int phase)
    {
        var p = (TrucoGameManager.TrucoPhase)phase;
        if (p == TrucoGameManager.TrucoPhase.PlayerTurn)
        {
            _statusLabel.Text = "Sua vez! Escolha uma carta.";
            _statusLabel.AddThemeColorOverride("font_color", SuccessGreen);
        }
        else if (p == TrucoGameManager.TrucoPhase.Cutting)
        {
            _statusLabel.Text = "Corte o baralho para começar";
            _statusLabel.AddThemeColorOverride("font_color", Gold);
        }
        else if (p == TrucoGameManager.TrucoPhase.OpponentTurn)
        {
            _statusLabel.Text = "Oponente pensando...";
            _statusLabel.AddThemeColorOverride("font_color", TextSecondary);
        }

        if (p != TrucoGameManager.TrucoPhase.TrucoRequested)
        {
            _trucoOverlay.Visible = false;
        }

        UpdateTrucoButton();
    }

    private void UpdateTrucoButton()
    {
        bool canTruco = _game.CurrentPhase == TrucoGameManager.TrucoPhase.PlayerTurn && _game.CurrentStakes < 12;
        _trucoBtn.Disabled = !canTruco;

        _trucoBtn.Text = _game.CurrentStakes switch
        {
            1 => "TRUCO!",
            3 => "SEIS!",
            6 => "NOVE!",
            9 => "DOZE!",
            _ => "TRUCO!"
        };
    }

    // ===== CARD CREATION =====

    private PanelContainer CreateCardPanel(TrucoCardData card, int index)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(36, 50);
        panel.MouseFilter = MouseFilterEnum.Stop;
        panel.MouseDefaultCursorShape = CursorShape.PointingHand;

        var style = new StyleBoxFlat();
        style.BgColor = CardBg;
        style.BorderColor = CardBorder;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        style.ContentMarginLeft = 4;
        style.ContentMarginRight = 4;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", -2);

        bool isRed = card.Suit == TrucoSuit.Hearts || card.Suit == TrucoSuit.Diamonds;
        var suitColor = Core.Visuals.AccessibilityVisuals.GetCardSuitColor(isRed, RedSuit, BlackSuit);

        // Check if manilha
        bool isManilha = card.Rank == _game.ManilhaRank;
        if (isManilha) suitColor = Gold;

        var rankLbl = new Label();
        rankLbl.Text = card.GetRankString();
        rankLbl.HorizontalAlignment = HorizontalAlignment.Center;
        rankLbl.AddThemeFontSizeOverride("font_size", 10);
        rankLbl.AddThemeColorOverride("font_color", suitColor);
        rankLbl.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(rankLbl);

        var suitLbl = new Label();
        suitLbl.Text = card.GetSuitSymbol();
        suitLbl.HorizontalAlignment = HorizontalAlignment.Center;
        suitLbl.AddThemeFontSizeOverride("font_size", 13);
        suitLbl.AddThemeColorOverride("font_color", suitColor);
        suitLbl.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(suitLbl);

        if (isManilha)
        {
            var manilhaTag = new Label();
            manilhaTag.Text = $"MANILHA • {card.GetSuitName()}";
            manilhaTag.HorizontalAlignment = HorizontalAlignment.Center;
            manilhaTag.AddThemeFontSizeOverride("font_size", 6);
            manilhaTag.AddThemeColorOverride("font_color", Gold);
            manilhaTag.MouseFilter = MouseFilterEnum.Ignore;
            vbox.AddChild(manilhaTag);
        }

        panel.AddChild(vbox);

        int cardIdx = index;
        panel.GuiInput += (@event) =>
        {
            if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                if (_game.CurrentPhase == TrucoGameManager.TrucoPhase.PlayerTurn)
                {
                    _game.PlayerPlayCard(cardIdx);
                }
            }
        };

        panel.MouseEntered += () =>
        {
            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = CardHoverBg;
            hoverStyle.BorderColor = Gold;
            hoverStyle.SetBorderWidthAll(2);
            hoverStyle.SetCornerRadiusAll(4);
            hoverStyle.ContentMarginLeft = 4;
            hoverStyle.ContentMarginRight = 4;
            hoverStyle.ContentMarginTop = 4;
            hoverStyle.ContentMarginBottom = 4;
            panel.AddThemeStyleboxOverride("panel", hoverStyle);
        };

        panel.MouseExited += () =>
        {
            panel.AddThemeStyleboxOverride("panel", style);
        };

        return panel;
    }

    private PanelContainer CreateSmallCard(TrucoCardData card, bool faceDown)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(28, 38);
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.12f, 0.14f, 0.24f);
        style.BorderColor = new Color(0.3f, 0.3f, 0.45f);
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(3);
        style.ContentMarginLeft = 2;
        style.ContentMarginRight = 2;
        style.ContentMarginTop = 2;
        style.ContentMarginBottom = 2;
        panel.AddThemeStyleboxOverride("panel", style);

        if (faceDown)
        {
            var backLbl = new Label();
            backLbl.Text = "✧";
            backLbl.HorizontalAlignment = HorizontalAlignment.Center;
            backLbl.VerticalAlignment = VerticalAlignment.Center;
            backLbl.AddThemeFontSizeOverride("font_size", 12);
            backLbl.AddThemeColorOverride("font_color", TextSecondary);
            panel.AddChild(backLbl);
        }
        else
        {
            var vbox = new VBoxContainer();
            vbox.Alignment = BoxContainer.AlignmentMode.Center;
            vbox.AddThemeConstantOverride("separation", -3);

            bool isRed = card.Suit == TrucoSuit.Hearts || card.Suit == TrucoSuit.Diamonds;
            var suitColor = Core.Visuals.AccessibilityVisuals.GetCardSuitColor(isRed, RedSuit, BlackSuit);

            var rankLbl = new Label();
            rankLbl.Text = card.GetRankString();
            rankLbl.HorizontalAlignment = HorizontalAlignment.Center;
            rankLbl.AddThemeFontSizeOverride("font_size", 8);
            rankLbl.AddThemeColorOverride("font_color", suitColor);
            vbox.AddChild(rankLbl);

            var suitLbl = new Label();
            suitLbl.Text = card.GetSuitSymbol();
            suitLbl.HorizontalAlignment = HorizontalAlignment.Center;
            suitLbl.AddThemeFontSizeOverride("font_size", 10);
            suitLbl.AddThemeColorOverride("font_color", suitColor);
            vbox.AddChild(suitLbl);

            panel.AddChild(vbox);
        }

        return panel;
    }

    private PanelContainer CreateEmptySlot()
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(28, 38);
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.06f, 0.06f, 0.1f);
        style.BorderColor = new Color(0.15f, 0.15f, 0.25f);
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(3);
        panel.AddThemeStyleboxOverride("panel", style);

        var lbl = new Label();
        lbl.Text = "·";
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment = VerticalAlignment.Center;
        lbl.AddThemeFontSizeOverride("font_size", 10);
        lbl.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.3f));
        panel.AddChild(lbl);

        return panel;
    }

    // ===== HELPERS =====

    private void ClearContainer(Control container)
    {
        while (container.GetChildCount() > 0)
        {
            var child = container.GetChild(0);
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

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
