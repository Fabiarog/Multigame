using Godot;
using System.Collections.Generic;
using GameHub.Core.Visuals;

namespace GameHub.Games.Truco;

/// <summary>
/// Full Truco UI with cards, table, tombos, scoreboard, truco overlay.
/// Club table presentation; game rules and event flow stay in the manager.
/// </summary>
public partial class TrucoUI : Control
{
    private TrucoGameManager _game;
    private TableStage _stage;

    // ===== UI REFS =====
    private Label _scoreLabel;
    private Label _viraLabel;
    private Label _stakesLabel;
    private Label _statusLabel;
    private HBoxContainer _playerHandContainer;
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
    private Label _trucoCallDescription;
    private Label _dealerLabel;
    private Label _handCountLabel;
    private Label _roundLabel;

    // Hand end overlay
    private Control _handOverlay;
    private Label _handOverlayTitle;
    private Label _handOverlayEyebrow;
    private Label _handOverlaySubtitle;
    private Button _nextHandBtn;
    private Button _backMenuBtn;

    private List<PanelContainer> _cardPanels = new();

    // ===== THEME COLORS =====
    private static readonly Color PanelBg = ClubTheme.Panel;
    private static readonly Color Gold = ClubTheme.Gold;
    private static readonly Color Accent = new("#e29a86");
    private static readonly Color SuccessGreen = new("#a6caaa");
    private static readonly Color TextPrimary = ClubTheme.Paper;
    private static readonly Color TextSecondary = ClubTheme.Muted;

    // ===== LIFECYCLE =====

    public override async void _Ready()
    {
        _game = GetNode<TrucoGameManager>("../../GameManager");
        Core.Systems.AudioManager.Instance?.PlayMusic("last-manilha");
        BuildUI();
        ConnectSignals();
        if (_stage != null)
        {
            await _stage.PlayEntrance(false);
        }
        if (IsInsideTree()) _game.StartMatch();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_stage != null && _stage.IsPresenting) return;
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Escape)
            {
                GetViewport().SetInputAsHandled();
                _stage?.ToggleInGameSettings();
                return;
            }
            if (key.Keycode == Key.M)
            {
                GetViewport().SetInputAsHandled();
                _stage?.CycleNextRoomTheme();
                return;
            }
            if (key.Keycode == Key.C)
            {
                GetViewport().SetInputAsHandled();
                _stage?.ToggleCameraMode();
                return;
            }
            if ((key.Keycode == Key.Space || key.Keycode == Key.Enter) && _cutDeckBtn != null && _cutDeckBtn.Visible && !_cutDeckBtn.Disabled)
            {
                GetViewport().SetInputAsHandled();
                _game.CutDeck();
                return;
            }
        }
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
        _game.TrucoResponded += OnTrucoResponded;
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
            _game.TrucoResponded -= OnTrucoResponded;
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
        Theme = ClubTheme.Create();

        // 3D TableStage fills 100% of the screen directly as the immersive canvas
        int playerChar = CharacterCatalog.Find(Core.Systems.SettingsManager.Instance?.CharacterId ?? "corvo");
        int rival = (playerChar == 2) ? 6 : 2;
        _stage = new TableStage { SeatCount = _game.TeamSize * 2, RivalIndex = rival };
        _stage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _stage.MouseFilter = MouseFilterEnum.Pass;
        AddChild(_stage);

        // Transparent HUD Overlay layer
        var hud = new Control { Name = "HudOverlay", MouseFilter = MouseFilterEnum.Ignore };
        hud.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(hud);

        // ==================== TOP LEFT: COMPACT SCOREBOARD ====================
        var topLeft = new MarginContainer();
        topLeft.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        topLeft.OffsetLeft = 18; topLeft.OffsetTop = 16;
        hud.AddChild(topLeft);

        var scorePanel = NewPanel(new Color(0.03f, 0.07f, 0.055f, 0.90f), ClubTheme.Border, 12);
        scorePanel.CustomMinimumSize = new Vector2(230, 0);
        topLeft.AddChild(scorePanel);
        var scoreBox = Column(5);
        scorePanel.AddChild(scoreBox);

        var headerRow = new HBoxContainer();
        headerRow.AddChild(ClubTheme.Label($"MESA {_game.TeamSize} × {_game.TeamSize}", 11, Gold));
        headerRow.AddChild(CreateExpandSpacer());
        headerRow.AddChild(ClubTheme.Label("META 12", 11, TextSecondary));
        scoreBox.AddChild(headerRow);

        var scoreRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        scoreRow.AddThemeConstantOverride("separation", 10);
        scoreRow.AddChild(ClubTheme.Label("NÓS", 13, SuccessGreen));
        _scoreLabel = ClubTheme.Label("00 : 00", 26, TextPrimary);
        _scoreLabel.AddThemeFontOverride("font", ClubTheme.MonoFont);
        scoreRow.AddChild(_scoreLabel);
        scoreRow.AddChild(ClubTheme.Label("ELES", 13, Accent));
        scoreBox.AddChild(scoreRow);

        var stakesRow = new HBoxContainer();
        _stakesLabel = ClubTheme.Label("VALE 1 PONTO", 12, Gold);
        stakesRow.AddChild(_stakesLabel);
        stakesRow.AddChild(CreateExpandSpacer());
        _roundLabel = ClubTheme.Label("TOMBO 01 / 03", 11, TextSecondary);
        stakesRow.AddChild(_roundLabel);
        scoreBox.AddChild(stakesRow);

        _tombosContainer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _tombosContainer.AddThemeConstantOverride("separation", 8);
        for (int i = 0; i < 3; i++)
        {
            var dot = ClubTheme.Label($"{i + 1} ○", 15, TextSecondary);
            dot.Name = $"Tombo{i}";
            dot.MouseFilter = MouseFilterEnum.Pass;
            _tombosContainer.AddChild(dot);
        }
        scoreBox.AddChild(_tombosContainer);

        _dealerLabel = ClubTheme.Label("DISTRIBUI: Você · CORTA: Adv 2", 11, TextSecondary);
        _dealerLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scoreBox.AddChild(_dealerLabel);

        // ==================== TOP RIGHT: COMPACT VIRA & MENU ====================
        var topRight = new MarginContainer();
        topRight.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        topRight.OffsetRight = -18; topRight.OffsetTop = 16;
        topRight.GrowHorizontal = GrowDirection.Begin;
        hud.AddChild(topRight);

        var viraPanel = NewPanel(new Color(0.03f, 0.07f, 0.055f, 0.90f), ClubTheme.Border, 10);
        topRight.AddChild(viraPanel);
        var viraRow = new HBoxContainer();
        viraRow.AddThemeConstantOverride("separation", 12);
        viraPanel.AddChild(viraRow);

        _viraCardContainer = new CenterContainer { CustomMinimumSize = new Vector2(64, 92) };
        viraRow.AddChild(_viraCardContainer);

        var viraInfo = Column(4);
        viraInfo.Alignment = BoxContainer.AlignmentMode.Center;
        viraRow.AddChild(viraInfo);

        _viraLabel = ClubTheme.Label("Vira ainda fechada", 12, TextPrimary);
        _viraLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _viraLabel.CustomMinimumSize = new Vector2(150, 0);
        viraInfo.AddChild(_viraLabel);

        var order = ClubTheme.Label("♦  <  ♠  <  ♥  <  ♣", 13, Gold);
        viraInfo.AddChild(order);
        var orderHint = ClubTheme.Label("Do ouros ao zap", 10, TextSecondary);
        viraInfo.AddChild(orderHint);

        var back = ClubTheme.Button("Voltar ao clube");
        back.CustomMinimumSize = new Vector2(140, 30);
        back.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        viraInfo.AddChild(back);

        // ==================== BOTTOM: POV HAND & STATUS MESSAGE ====================
        var bottomMargin = new MarginContainer();
        bottomMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        bottomMargin.OffsetLeft = 20; bottomMargin.OffsetRight = -20; bottomMargin.OffsetBottom = -14;
        bottomMargin.GrowVertical = GrowDirection.Begin;
        hud.AddChild(bottomMargin);

        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 16);
        bottomRow.Alignment = BoxContainer.AlignmentMode.Center;
        bottomMargin.AddChild(bottomRow);

        var dummyLeft = new Control { CustomMinimumSize = new Vector2(170, 0) };
        bottomRow.AddChild(dummyLeft);

        var handCol = Column(6);
        handCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        handCol.Alignment = BoxContainer.AlignmentMode.Center;
        bottomRow.AddChild(handCol);

        var handCenter = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        handCol.AddChild(handCenter);

        _playerHandContainer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _playerHandContainer.AddThemeConstantOverride("separation", 14);
        handCenter.AddChild(_playerHandContainer);

        // Message directly below cards in the POV perspective
        var statusBadge = new PanelContainer();
        statusBadge.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.02f, 0.06f, 0.045f, 0.90f), ClubTheme.Border, 10, 5));
        var statusHBox = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        statusHBox.AddThemeConstantOverride("separation", 14);
        statusBadge.AddChild(statusHBox);

        _statusLabel = ClubTheme.Label("Preparando o baralho...", 14, Gold);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        statusHBox.AddChild(_statusLabel);

        _handCountLabel = ClubTheme.Label("3 cartas", 12, TextSecondary);
        statusHBox.AddChild(_handCountLabel);
        handCol.AddChild(statusBadge);

        // Action controls (TRUCO / CORTAR)
        var actions = Column(6);
        actions.CustomMinimumSize = new Vector2(170, 0);
        actions.Alignment = BoxContainer.AlignmentMode.Center;
        bottomRow.AddChild(actions);

        _trucoBtn = ClubTheme.Button("TRUCO!", true);
        _trucoBtn.CustomMinimumSize = new Vector2(170, 44);
        _trucoBtn.Pressed += () => _game.RequestTruco();
        actions.AddChild(_trucoBtn);

        _cutDeckBtn = ClubTheme.Button("Cortar o baralho", true);
        _cutDeckBtn.CustomMinimumSize = new Vector2(170, 44);
        _cutDeckBtn.Pressed += () => _game.CutDeck();
        _cutDeckBtn.Visible = false;
        actions.AddChild(_cutDeckBtn);

        var raiseHint = ClubTheme.Label("1 → 3 → 6 → 9 → 12", 11, TextSecondary);
        raiseHint.HorizontalAlignment = HorizontalAlignment.Center;
        actions.AddChild(raiseHint);

        _dealAnimationLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _dealAnimationLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_dealAnimationLayer);
        RefreshTableCards();
        ShowClosedVira();
        BuildTrucoOverlay();
        BuildHandOverlay();
        BuildPenaOverlay();
        AccessibilityVisuals.AddGlobalFilter(this);
    }

    private void BuildPenaOverlay()
    {
        var box = BuildDialog(out _penaOverlay, 560);
        var title = ClubTheme.Label("A PENA", 32, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);
        _penaLabel = ClubTheme.Label("", 20, TextPrimary);
        _penaLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _penaLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _penaLabel.CustomMinimumSize = new Vector2(490, 90);
        box.AddChild(_penaLabel);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 12);
        box.AddChild(row);
        _givePenaBtn = DialogButton("Entregar a pena", true);
        _givePenaBtn.Pressed += () => _game.GivePena();
        row.AddChild(_givePenaBtn);
        _skipPenaBtn = DialogButton("Sem pena");
        _skipPenaBtn.Pressed += () => _game.ResolvePena(false);
        row.AddChild(_skipPenaBtn);
        _keepPenaBtn = DialogButton("Ficar com a carta", true);
        _keepPenaBtn.Pressed += () => _game.ResolvePena(true);
        _keepPenaBtn.Visible = false;
        row.AddChild(_keepPenaBtn);
        _tomboPenaBtn = DialogButton("Virar tombo");
        _tomboPenaBtn.Pressed += () => _game.ResolvePena(false);
        _tomboPenaBtn.Visible = false;
        row.AddChild(_tomboPenaBtn);
    }

    private void BuildTrucoOverlay()
    {
        var box = BuildDialog(out _trucoOverlay, 640);
        var eyebrow = ClubTheme.Label("O DESAFIO ESTÁ NA MESA", 12, Gold);
        eyebrow.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(eyebrow);
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", 24);
        box.AddChild(content);
        var portraitFrame = NewPanel(ClubTheme.Ink, ClubTheme.Border, 4);
        content.AddChild(portraitFrame);
        _trucoPortrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(168, 168),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = CharacterCatalog.TrucoCallSprite(0, 3),
            MouseFilter = MouseFilterEnum.Ignore
        };
        portraitFrame.AddChild(_trucoPortrait);
        var call = Column(8);
        call.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        call.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddChild(call);
        _trucoOverlayLabel = ClubTheme.Label("TRUCO!", 46, Gold);
        _trucoOverlayLabel.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        call.AddChild(_trucoOverlayLabel);
        _trucoCallDescription = ClubTheme.Label("A mão agora vale 3 pontos.", 18, TextPrimary);
        _trucoCallDescription.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        call.AddChild(_trucoCallDescription);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 12);
        box.AddChild(buttons);
        _acceptBtn = DialogButton("Aceitar", true);
        _acceptBtn.Pressed += () => _game.RespondToTruco(true, false);
        buttons.AddChild(_acceptBtn);
        _raiseBtn = DialogButton("Seis!");
        _raiseBtn.Pressed += () => _game.RespondToTruco(false, true);
        buttons.AddChild(_raiseBtn);
        _declineBtn = DialogButton("Correr");
        _declineBtn.Pressed += () => _game.RespondToTruco(false, false);
        buttons.AddChild(_declineBtn);
    }

    private void BuildHandOverlay()
    {
        var box = BuildDialog(out _handOverlay, 540);
        _handOverlayEyebrow = ClubTheme.Label("FIM DA MÃO", 12, Gold);
        _handOverlayEyebrow.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(_handOverlayEyebrow);
        _handOverlayTitle = ClubTheme.Label("", 36, Gold);
        _handOverlayTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _handOverlayTitle.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        box.AddChild(_handOverlayTitle);
        _handOverlaySubtitle = ClubTheme.Label("", 19, TextSecondary);
        _handOverlaySubtitle.HorizontalAlignment = HorizontalAlignment.Center;
        _handOverlaySubtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _handOverlaySubtitle.CustomMinimumSize = new Vector2(472, 52);
        box.AddChild(_handOverlaySubtitle);
        _nextHandBtn = DialogButton("Próxima mão", true);
        _nextHandBtn.Pressed += () =>
        {
            _handOverlay.Visible = false;
            _game.StartNewHand();
        };
        box.AddChild(_nextHandBtn);
        _backMenuBtn = DialogButton("Voltar ao clube");
        _backMenuBtn.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        box.AddChild(_backMenuBtn);
    }

    private VBoxContainer BuildDialog(out Control overlay, float width)
    {
        overlay = new ColorRect { Color = new Color(0.015f, 0.035f, 0.03f, 0.90f), MouseFilter = MouseFilterEnum.Stop, Visible = false };
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(overlay);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.AddChild(center);
        var panel = NewPanel(PanelBg, Gold, 32);
        panel.CustomMinimumSize = new Vector2(width, 0);
        center.AddChild(panel);
        var box = Column(20);
        panel.AddChild(box);
        return box;
    }

    private Button DialogButton(string text, bool primary = false)
    {
        var button = ClubTheme.Button(text, primary);
        button.CustomMinimumSize = new Vector2(148, 48);
        return button;
    }

    private static VBoxContainer Column(int separation)
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", separation);
        return column;
    }

    private static PanelContainer NewPanel(Color fill, Color border, int padding)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(fill, border, padding, 10));
        return panel;
    }

    private static HBoxContainer BuildCardRow(VBoxContainer parent, int separation)
    {
        var center = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(center);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", separation);
        center.AddChild(row);
        return row;
    }

    private void ShowClosedVira()
    {
        ClearContainer(_viraCardContainer);
        var back = CreateAnimatedCardBack();
        back.CustomMinimumSize = new Vector2(64, 92);
        _viraCardContainer.AddChild(back);
    }

    // ===== REFRESH =====

    private void RefreshPlayerHand(bool animateDeal = false)
    {
        while (_playerHandContainer.GetChildCount() > 0)
        {
            var child = _playerHandContainer.GetChild(0);
            _playerHandContainer.RemoveChild(child);
            child.QueueFree();
        }
        _cardPanels.Clear();

        bool shouldAnimate = animateDeal && (Core.Systems.SettingsManager.Instance?.ReduceMotion != true);
        for (int i = 0; i < _game.PlayerHand.Count; i++)
        {
            var card = _game.PlayerHand[i];
            var panel = CreateCardPanel(card, i);
            _playerHandContainer.AddChild(panel);
            _cardPanels.Add(panel);

            if (shouldAnimate)
            {
                panel.Modulate = new Color(1, 1, 1, 0);
                panel.Scale = new Vector2(0.82f, 0.82f);
                panel.PivotOffset = panel.CustomMinimumSize / 2f;
                int cardOrder = i;
                var tween = CreateTween();
                tween.TweenInterval(cardOrder * 0.06f);
                tween.TweenProperty(panel, "modulate:a", 1f, 0.20f);
                tween.Parallel().TweenProperty(panel, "scale", Vector2.One, 0.24f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }
        _handCountLabel.Text = _game.PlayerHand.Count == 1 ? "1 carta na mão" : $"{_game.PlayerHand.Count} cartas na mão";
    }

    private void RefreshTableCards()
    {
        // 2D cards overlaid on the table felt are removed; cards are rendered directly in 3D on the table.
    }

    private void RefreshTombos()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_tombosContainer.GetChild(i) is Label dot)
            {
                int w = _game.RoundWinners[i];
                if (w == 0) { dot.Text = $"{i + 1} ✓"; dot.TooltipText = "Tombo da nossa equipe"; dot.AddThemeColorOverride("font_color", SuccessGreen); }
                else if (w == 1) { dot.Text = $"{i + 1} ×"; dot.TooltipText = "Tombo dos adversários"; dot.AddThemeColorOverride("font_color", Accent); }
                else if (w == 2) { dot.Text = $"{i + 1} ="; dot.TooltipText = "Tombo empatado"; dot.AddThemeColorOverride("font_color", Gold); }
                else { dot.Text = $"{i + 1} ·"; dot.TooltipText = "Tombo ainda não disputado"; dot.AddThemeColorOverride("font_color", TextSecondary); }
            }
        }
    }

    // ===== EVENT HANDLERS =====

    private void OnHandDealt()
    {
        ClearDealAnimationLayer();
        _stakesLabel.Text = _game.CurrentStakes == 1 ? "VALE 1 PONTO" : $"VALE {_game.CurrentStakes} PONTOS";
        RefreshPlayerHand(true);
        RefreshTableCards();
        RefreshTombos();
        _trucoOverlay.Visible = false;
        _handOverlay.Visible = false;
        UpdateTrucoButton();
        for (int s = 0; s < _game.TeamSize * 2; s++)
            _stage.SetCardCount(s, 3);
    }

    private void OnDeckShuffled()
    {
        Core.Systems.AudioManager.Instance?.PlaySound("shuffle");
        _stakesLabel.Text = "VALE 1 PONTO";
        _viraLabel.Text = "Vira ainda fechada";
        _handCountLabel.Text = "O corte abre a mesa.";
        _dealerLabel.Text = $"DISTRIBUI: {_game.GetSeatName(_game.DealerSeatIndex)}\nCORTA: {_game.GetSeatName(_game.CutterSeatIndex)}";
        ClearContainer(_playerHandContainer);
        _cardPanels.Clear();
        _stage.ClearPlayedCards();
        for (int seat = 0; seat < _game.TeamSize * 2; seat++) _stage.SetCardCount(seat, 0);
        RefreshTableCards();
        RefreshTombos();
        ShowClosedVira();
        _statusLabel.Text = $"{_game.GetSeatName(_game.DealerSeatIndex)} embaralhando…";
        _statusLabel.AddThemeColorOverride("font_color", Gold);
        _cutDeckBtn.Visible = false;
        _stage?.PlayGesture(_game.DealerSeatIndex, "shuffle");
        AnimateShuffle();
    }

    private void OnDeckCut(int cutPosition)
    {
        Core.Systems.AudioManager.Instance?.PlaySound("cut");
        _statusLabel.Text = $"{_game.GetSeatName(_game.CutterSeatIndex)} cortou o baralho.";
        _cutDeckBtn.Visible = false;
        _stage?.AnimateDeck(true);
        _stage?.PlayTableAction(_game.CutterSeatIndex);
        _stage?.PlayGesture(_game.CutterSeatIndex, "cut_deck");
    }

    private void OnPenaAvailable(string recipient)
    {
        bool canOffer = _game.CutterIsPlayer;
        _penaLabel.Text = canOffer
            ? $"Você pode entregar a pena para\n{recipient}."
            : $"{_game.GetSeatName(_game.CutterSeatIndex)} está decidindo sobre a pena para\n{recipient}...";
        _givePenaBtn.Visible = canOffer;
        _skipPenaBtn.Visible = canOffer;
        _givePenaBtn.Disabled = !canOffer;
        _skipPenaBtn.Disabled = !canOffer;
        _keepPenaBtn.Visible = false;
        _tomboPenaBtn.Visible = false;
        _penaOverlay.Visible = true;
    }

    private void OnPenaDelivered(string cardDisplay, string recipient)
    {
        _penaLabel.Text = _game.PenaDecisionIsLocal
            ? $"Você recebeu {cardDisplay}.\nFica com a carta ou ela vira tombo?"
            : $"{recipient} recebeu {cardDisplay}.\nA IA está decidindo se guarda a pena...";
        _givePenaBtn.Visible = false; _skipPenaBtn.Visible = false;
        _keepPenaBtn.Visible = _game.PenaDecisionIsLocal; _tomboPenaBtn.Visible = _game.PenaDecisionIsLocal;
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
        Core.Systems.AudioManager.Instance?.PlaySound("deal");
        _statusLabel.Text = penaKept
            ? "Pena guardada: o aliado receberá só mais 2 cartas."
            : $"{_game.GetSeatName(_game.DealerSeatIndex)} distribuindo 3 cartas para cada jogador…";
        _stage?.PlayTableAction(_game.DealerSeatIndex);
        _stage?.PlayGesture(_game.DealerSeatIndex, "deal");
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
        _cutDeckBtn.Disabled = true;
        ClearDealAnimationLayer();
        _stage.AnimateDeck(false);
        _stage.PlayTableAction(_game.DealerSeatIndex);
    }

    private void AnimatePenaDelivery()
    {
        _stage.AnimatePenaDelivery(_game.PenaRecipientSeatIndex);
    }

    private void AnimatePenaResolution(bool kept)
    {
        _stage.AnimatePenaResolution(_game.PenaRecipientSeatIndex, kept);
    }

    private void AnimateDistribution(int cardCount, int penaRecipientSeat, bool penaKept)
    {
        ClearDealAnimationLayer();
        var counts = new int[_game.TeamSize * 2];
        System.Array.Fill(counts, 3);
        _stage.AnimateDeal(_game.DealerSeatIndex, counts, penaRecipientSeat, penaKept);
    }

    private void AnimateCleanupAndPass(int nextDealerSeat)
    {
        ClearDealAnimationLayer();
        _stage.AnimateDeckPass(nextDealerSeat);
    }

    private PanelContainer CreateAnimatedCardBack()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(72, 104),
            Size = new Vector2(72, 104),
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        panel.AddChild(new PlayingCard { FaceDown = true, MouseFilter = MouseFilterEnum.Ignore });
        return panel;
    }

    private Vector2 GetDeckScreenPosition() => _stage.DeckScreenPosition();

    private Vector2 GetSeatScreenPosition(int seat)
    {
        return seat == 0 ? _playerHandContainer.GetGlobalRect().GetCenter() : _stage.SeatScreenPosition(seat);
    }

    private void PulseControl(Control control)
    {
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
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
        _scoreLabel.Text = $"{team1:00} : {team2:00}";
    }

    private void OnViraRevealed(string viraDisplay, string manilhaDisplay)
    {
        _viraLabel.Text = $"Vira: {viraDisplay}\nManilha: {new TrucoCardData { Rank = _game.ManilhaRank }.GetRankString()}";
        
        ClearContainer(_viraCardContainer);
        if (_game.ViraCard != null)
        {
            var panel = CreateSmallCard(_game.ViraCard, false);
            panel.CustomMinimumSize = new Vector2(64, 92);
            panel.MouseFilter = MouseFilterEnum.Ignore; // Vira card is not clickable
            _viraCardContainer.AddChild(panel);
        }
    }

    private void OnTrucoCalled(int stakes, bool byPlayer)
    {
        int callingSeat = byPlayer ? 0 : 1;
        int callingChar = _stage != null ? _stage.CharacterAt(callingSeat) : 0;
        string trucoClip = CharacterProgress.TrucoClip(callingChar);
        _stage?.PlayGesture(callingSeat, trucoClip);
        Core.Systems.AudioManager.Instance?.PlaySound("truco");
        _stakesLabel.Text = $"VALE {stakes} PONTOS";
        _trucoCallDescription.Text = byPlayer
            ? $"Você pediu {stakes} pontos.\nAguardando a resposta..."
            : $"O adversário pediu {stakes} pontos.\nQual é a sua resposta?";

        string label = stakes switch
        {
            3 => "TRUCO!",
            6 => "SEIS!",
            9 => "NOVE!",
            12 => "DOZE!",
            _ => "TRUCO!"
        };
        _trucoOverlayLabel.Text = label;

        Color stakesColor = stakes switch
        {
            6 => new Color("#e67e22"),
            9 => new Color("#e12d41"),
            12 => new Color("#a550f0"),
            _ => Gold
        };
        _trucoOverlayLabel.AddThemeColorOverride("font_color", stakesColor);

        if (_trucoPortrait != null)
        {
            _trucoPortrait.Texture = CharacterCatalog.TrucoCallSprite(callingChar, stakes);
        }

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

    private void OnTrucoResponded(bool accepted, bool byPlayer)
    {
        int respondingSeat = byPlayer ? 0 : 1;
        _stage?.PlayGesture(respondingSeat, accepted ? "accept_truco" : "decline_truco");
        string responderName = byPlayer ? "Você" : "O adversário";
        _statusLabel.Text = accepted
            ? $"{responderName} aceitou o truco! A aposta subiu para {_game.CurrentStakes} pontos."
            : $"{responderName} correu do truco.";
        _statusLabel.AddThemeColorOverride("font_color", accepted ? Gold : Accent);
    }

    private void OnCardPlayed(int who, string cardDisplay, int roundIdx)
    {
        RefreshTableCards();
        RefreshPlayerHand();
        Core.Systems.AudioManager.Instance?.PlaySound("play");
        int seat = _game.LastPlayedSeatIndex;
        _stage.PlayTableAction(seat);
        _stage.SetCardCount(seat, _game.GetSeatCardCount(seat));
        string seatName = _game.GetSeatName(seat);
        string manilhaRankStr = _game.ManilhaRank switch
        {
            TrucoRank.Four => "4", TrucoRank.Five => "5", TrucoRank.Six => "6", TrucoRank.Seven => "7",
            TrucoRank.Queen => "Q", TrucoRank.Jack => "J", TrucoRank.King => "K",
            TrucoRank.Ace => "A", TrucoRank.Two => "2", TrucoRank.Three => "3",
            _ => ""
        };
        bool isManilha = !string.IsNullOrEmpty(manilhaRankStr) && cardDisplay.StartsWith(manilhaRankStr);
        _stage.PlayCard(seat, cardDisplay, 0, seatName, isManilha);
    }

    private void OnRoundResolved(int roundIdx, int winner)
    {
        RefreshTombos();
        string msg = winner switch
        {
            0 => "Você fez o tombo! Ponto para a nossa equipe.",
            1 => "Oponente fez o tombo!",
            _ => "Tombo empatado!"
        };
        _statusLabel.Text = msg;
        _statusLabel.AddThemeColorOverride("font_color", winner == 0 ? SuccessGreen : (winner == 1 ? Accent : Gold));
        if (winner == 0 || winner == 1)
        {
            _stage?.PlayGesture(winner, "trick_win");
            int loser = winner == 0 ? 1 : 0;
            _stage?.PlayGesture(loser, "trick_lose");
        }
        _ = _stage?.CollectRoundCardsToDiscard();
    }

    private void OnHandEnded(bool playerWon, int pointsGained)
    {
        if (playerWon) Core.Systems.AudioManager.Instance?.PlaySound("win");
        _stage?.PlayGesture(playerWon ? 0 : 1, "big_win");
        _stage?.PlayGesture(playerWon ? 1 : 0, "lose");
        _handOverlayEyebrow.Text = "FIM DA MÃO";
        _handOverlayTitle.Text = playerWon ? "Você ganhou!" : "Oponente ganhou!";
        _handOverlayTitle.AddThemeColorOverride("font_color", playerWon ? SuccessGreen : Accent);
        _handOverlaySubtitle.Text = $"Placar da partida: {_game.PlayerScore} × {_game.OpponentScore}";
        _nextHandBtn.Visible = true;
        _backMenuBtn.Visible = true;
        _handOverlay.Visible = true;
    }

    private void OnGameEnded(bool playerWon, int team1Score, int team2Score)
    {
        _handOverlayEyebrow.Text = "FIM DA PARTIDA";
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
        _cutDeckBtn.Visible = p == TrucoGameManager.TrucoPhase.Cutting && _game.CutterIsPlayer;
        _cutDeckBtn.Disabled = !_cutDeckBtn.Visible;
        _roundLabel.Text = $"TOMBO {_game.CurrentRound + 1:00} / 03";
        if (p == TrucoGameManager.TrucoPhase.PlayerTurn)
        {
            _statusLabel.Text = "Sua vez! Escolha uma carta.";
            _statusLabel.AddThemeColorOverride("font_color", SuccessGreen);
        }
        else if (p == TrucoGameManager.TrucoPhase.Cutting)
        {
            _statusLabel.Text = _game.CutterIsPlayer ? "Sua vez de cortar o baralho." : $"{_game.GetSeatName(_game.CutterSeatIndex)} cortando o baralho…";
            _statusLabel.AddThemeColorOverride("font_color", Gold);
        }
        else if (p == TrucoGameManager.TrucoPhase.OpponentTurn)
        {
            _statusLabel.Text = $"{_game.GetSeatName(_game.ActiveSeatIndex)} pensando...";
            _statusLabel.AddThemeColorOverride("font_color", TextSecondary);
            _stage?.PlayGesture(_game.ActiveSeatIndex, "think");
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
        _trucoBtn.Visible = _game.CurrentPhase != TrucoGameManager.TrucoPhase.Cutting
            && _game.CurrentPhase != TrucoGameManager.TrucoPhase.PenaDecision
            && _game.CurrentPhase != TrucoGameManager.TrucoPhase.Dealing;
        foreach (var panel in _cardPanels)
        {
            panel.MouseDefaultCursorShape = canTruco || _game.CurrentPhase == TrucoGameManager.TrucoPhase.PlayerTurn
                ? CursorShape.PointingHand : CursorShape.Arrow;
            if (panel.GetChildCount() > 0 && panel.GetChild(0) is PlayingCard face)
                SetCardHighlight(face, panel.HasFocus());
        }

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
        bool playable = index >= 0;
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(100, 144),
            MouseFilter = playable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore,
            MouseDefaultCursorShape = playable ? CursorShape.PointingHand : CursorShape.Arrow,
            FocusMode = playable ? FocusModeEnum.All : FocusModeEnum.None,
            TooltipText = $"{card.GetRankString()} de {card.GetSuitName()}" + (card.Rank == _game.ManilhaRank ? " • MANILHA" : "")
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        var face = new PlayingCard
        {
            RankText = card.GetRankString(),
            SuitSymbol = card.GetSuitSymbol(),
            Special = card.Rank == _game.ManilhaRank,
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddChild(face);
        if (!playable) return panel;

        int cardIdx = index;
        panel.GuiInput += input =>
        {
            bool activate = input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left;
            activate |= input is InputEventKey key && key.Pressed && !key.Echo && (key.Keycode == Key.Enter || key.Keycode == Key.Space);
            if (activate && _game.CurrentPhase == TrucoGameManager.TrucoPhase.PlayerTurn)
            {
                panel.AcceptEvent();
                _game.PlayerPlayCard(cardIdx);
            }
        };
        panel.MouseEntered += () => SetCardHighlight(face, true);
        panel.MouseExited += () => SetCardHighlight(face, panel.HasFocus());
        panel.FocusEntered += () => SetCardHighlight(face, true);
        panel.FocusExited += () => SetCardHighlight(face, false);
        return panel;
    }

    private void SetCardHighlight(PlayingCard card, bool highlight)
    {
        card.Selected = highlight && _game.CurrentPhase == TrucoGameManager.TrucoPhase.PlayerTurn;
        card.QueueRedraw();
    }

    private PanelContainer CreateSmallCard(TrucoCardData card, bool faceDown)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(76, 108),
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        panel.AddChild(new PlayingCard
        {
            RankText = card.GetRankString(), SuitSymbol = card.GetSuitSymbol(), FaceDown = faceDown,
            Special = card.Rank == _game.ManilhaRank, MouseFilter = MouseFilterEnum.Ignore
        });
        return panel;
    }

    private PanelContainer CreateEmptySlot()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(76, 108),
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.02f, 0.07f, 0.055f, 0.35f), ClubTheme.Border, 0, 8));
        var mark = ClubTheme.Label("♧", 28, ClubTheme.Border);
        mark.HorizontalAlignment = HorizontalAlignment.Center;
        mark.VerticalAlignment = VerticalAlignment.Center;
        mark.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(mark);
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

    private Control CreateExpandSpacer()
    {
        return new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
    }
}
