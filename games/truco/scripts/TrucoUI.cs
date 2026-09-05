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

    public override void _Ready()
    {
        _game = GetNode<TrucoGameManager>("../../GameManager");
        Core.Systems.AudioManager.Instance?.PlayMusic("last-manilha");
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
        Theme = ClubTheme.Create();
        var backdrop = new ClubBackdrop { ShowTable = false };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(margin);
        var main = Column(14);
        margin.AddChild(main);

        var header = new HBoxContainer { CustomMinimumSize = new Vector2(0, 64) };
        header.AddThemeConstantOverride("separation", 20);
        main.AddChild(header);
        var title = Column(0);
        title.AddChild(ClubTheme.Label("MULTI GAME  /  CLUBE DE CARTAS", 12, Gold));
        var gameTitle = ClubTheme.Label("Truco", 40, TextPrimary);
        gameTitle.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        title.AddChild(gameTitle);
        header.AddChild(title);
        header.AddChild(CreateExpandSpacer());
        var mode = Column(2);
        mode.Alignment = BoxContainer.AlignmentMode.Center;
        mode.AddChild(ClubTheme.Label($"MESA {_game.TeamSize} × {_game.TeamSize}", 17, TextPrimary));
        mode.AddChild(ClubTheme.Label("A primeira equipe a 12 vence", 13, TextSecondary));
        header.AddChild(mode);
        var stakesPanel = NewPanel(new Color(0.18f, 0.19f, 0.12f), Gold, 16);
        _stakesLabel = ClubTheme.Label("VALE 1 PONTO", 18, Gold);
        _stakesLabel.VerticalAlignment = VerticalAlignment.Center;
        stakesPanel.AddChild(_stakesLabel);
        header.AddChild(stakesPanel);
        var back = ClubTheme.Button("Voltar ao clube");
        back.CustomMinimumSize = new Vector2(166, 44);
        back.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        back.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        header.AddChild(back);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 18);
        main.AddChild(body);

        var scoreboard = NewPanel(PanelBg, ClubTheme.Border, 20);
        scoreboard.CustomMinimumSize = new Vector2(226, 0);
        body.AddChild(scoreboard);
        var scoreBox = Column(12);
        scoreboard.AddChild(scoreBox);
        scoreBox.AddChild(ClubTheme.Label("PLACAR DA PARTIDA", 12, Gold));
        var teamNames = new HBoxContainer();
        teamNames.AddChild(ClubTheme.Label("NÓS", 14, SuccessGreen));
        teamNames.AddChild(CreateExpandSpacer());
        teamNames.AddChild(ClubTheme.Label("ELES", 14, Accent));
        scoreBox.AddChild(teamNames);
        _scoreLabel = ClubTheme.Label("00 : 00", 40, TextPrimary);
        _scoreLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _scoreLabel.AddThemeFontOverride("font", ClubTheme.MonoFont);
        scoreBox.AddChild(_scoreLabel);
        scoreBox.AddChild(ClubTheme.Label("META  /  12 PONTOS", 12, TextSecondary));
        scoreBox.AddChild(new HSeparator());
        scoreBox.AddChild(ClubTheme.Label("TOMBOS DA MÃO", 12, Gold));
        _tombosContainer = new HBoxContainer();
        _tombosContainer.AddThemeConstantOverride("separation", 12);
        for (int i = 0; i < 3; i++)
        {
            var dot = ClubTheme.Label($"{i + 1} ○", 20, TextSecondary);
            dot.Name = $"Tombo{i}";
            dot.MouseFilter = MouseFilterEnum.Pass;
            _tombosContainer.AddChild(dot);
        }
        scoreBox.AddChild(_tombosContainer);
        scoreBox.AddChild(ClubTheme.Label("Melhor de três tombos", 13, TextSecondary));
        scoreBox.AddChild(CreateExpandSpacer());
        _dealerLabel = ClubTheme.Label("DISTRIBUIDOR\nVocê", 14, TextSecondary);
        _dealerLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        scoreBox.AddChild(_dealerLabel);

        var table = Column(8);
        table.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        table.Alignment = BoxContainer.AlignmentMode.Center;
        var tableSurface = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tableSurface.AddThemeStyleboxOverride("panel", ClubTheme.Box(Colors.Transparent, ClubTheme.Border, 18, 12));
        _stage = new TableStage { SeatCount = _game.TeamSize * 2, RivalIndex = 1 };
        tableSurface.AddChild(_stage);
        tableSurface.AddChild(table);
        body.AddChild(tableSurface);
        var opponentLabel = ClubTheme.Label("ADVERSÁRIO  /  CARTAS NA MESA", 12, TextSecondary);
        opponentLabel.HorizontalAlignment = HorizontalAlignment.Center;
        table.AddChild(opponentLabel);
        _tableOpponentRow = BuildCardRow(table, 16);
        var tableCenter = Column(2);
        tableCenter.SizeFlagsVertical = SizeFlags.ExpandFill;
        tableCenter.Alignment = BoxContainer.AlignmentMode.Center;
        table.AddChild(tableCenter);
        _roundLabel = ClubTheme.Label("TOMBO 01 / 03", 12, Gold);
        _roundLabel.HorizontalAlignment = HorizontalAlignment.Center;
        tableCenter.AddChild(_roundLabel);
        _statusLabel = ClubTheme.Label("Preparando o baralho...", 17, TextPrimary);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusLabel.CustomMinimumSize = new Vector2(0, 44);
        _statusLabel.VerticalAlignment = VerticalAlignment.Center;
        tableCenter.AddChild(_statusLabel);
        _tablePlayerRow = BuildCardRow(table, 16);
        var playerLabel = ClubTheme.Label("VOCÊ  /  CARTAS NA MESA", 12, TextSecondary);
        playerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        table.AddChild(playerLabel);

        var viraPanel = NewPanel(PanelBg, ClubTheme.Border, 18);
        viraPanel.CustomMinimumSize = new Vector2(210, 0);
        body.AddChild(viraPanel);
        var viraBox = Column(10);
        viraPanel.AddChild(viraBox);
        viraBox.AddChild(ClubTheme.Label("A CARTA DO TOMBO", 12, Gold));
        _viraCardContainer = new CenterContainer();
        _viraCardContainer.CustomMinimumSize = new Vector2(0, 144);
        viraBox.AddChild(_viraCardContainer);
        _viraLabel = ClubTheme.Label("Vira ainda fechada", 16, TextPrimary);
        _viraLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _viraLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _viraLabel.CustomMinimumSize = new Vector2(0, 50);
        viraBox.AddChild(_viraLabel);
        viraBox.AddChild(CreateExpandSpacer());
        viraBox.AddChild(new HSeparator());
        viraBox.AddChild(ClubTheme.Label("FORÇA DAS MANILHAS", 11, TextSecondary));
        var order = ClubTheme.Label("♦  <  ♠  <  ♥  <  ♣", 21, Gold);
        order.HorizontalAlignment = HorizontalAlignment.Center;
        viraBox.AddChild(order);
        var orderHint = ClubTheme.Label("Do ouros ao zap", 12, TextSecondary);
        orderHint.HorizontalAlignment = HorizontalAlignment.Center;
        viraBox.AddChild(orderHint);

        var handPanel = NewPanel(PanelBg, ClubTheme.Border, 14);
        handPanel.CustomMinimumSize = new Vector2(0, 184);
        main.AddChild(handPanel);
        var handRow = new HBoxContainer();
        handRow.AddThemeConstantOverride("separation", 24);
        handPanel.AddChild(handRow);
        var handInfo = Column(8);
        handInfo.CustomMinimumSize = new Vector2(206, 0);
        handInfo.Alignment = BoxContainer.AlignmentMode.Center;
        handRow.AddChild(handInfo);
        handInfo.AddChild(ClubTheme.Label("SUA MÃO", 12, Gold));
        _handCountLabel = ClubTheme.Label("O corte abre a mesa.", 17, TextPrimary);
        handInfo.AddChild(_handCountLabel);
        var handHint = ClubTheme.Label("Na sua vez, clique em uma carta para jogar.", 13, TextSecondary);
        handHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        handInfo.AddChild(handHint);
        var handCenter = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        handRow.AddChild(handCenter);
        _playerHandContainer = new HBoxContainer();
        _playerHandContainer.AddThemeConstantOverride("separation", 14);
        handCenter.AddChild(_playerHandContainer);
        var actions = Column(10);
        actions.CustomMinimumSize = new Vector2(220, 0);
        actions.Alignment = BoxContainer.AlignmentMode.Center;
        handRow.AddChild(actions);
        _trucoBtn = ClubTheme.Button("TRUCO!", true);
        _trucoBtn.CustomMinimumSize = new Vector2(220, 48);
        _trucoBtn.Pressed += () => _game.RequestTruco();
        actions.AddChild(_trucoBtn);
        _cutDeckBtn = ClubTheme.Button("Cortar o baralho", true);
        _cutDeckBtn.CustomMinimumSize = new Vector2(220, 48);
        _cutDeckBtn.Pressed += () => _game.CutDeck();
        _cutDeckBtn.Visible = false;
        actions.AddChild(_cutDeckBtn);
        var raiseHint = ClubTheme.Label("1 → 3 → 6 → 9 → 12", 13, TextSecondary);
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
            CustomMinimumSize = new Vector2(148, 148),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = ResourceLoader.Load<Texture2D>("res://assets/sprites/portraits/truco_player.jpg"),
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
        back.CustomMinimumSize = new Vector2(92, 132);
        _viraCardContainer.AddChild(back);
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
        _handCountLabel.Text = _game.PlayerHand.Count == 1 ? "1 carta na mão" : $"{_game.PlayerHand.Count} cartas na mão";
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
        RefreshPlayerHand();
        RefreshTableCards();
        RefreshTombos();
        _trucoOverlay.Visible = false;
        _handOverlay.Visible = false;
        UpdateTrucoButton();
    }

    private void OnDeckShuffled()
    {
        Core.Systems.AudioManager.Instance?.PlaySound("shuffle");
        _stakesLabel.Text = "VALE 1 PONTO";
        _viraLabel.Text = "Vira ainda fechada";
        _handCountLabel.Text = "O corte abre a mesa.";
        _dealerLabel.Text = $"DISTRIBUIDOR\n{_game.GetSeatName(_game.DealerSeatIndex)}";
        ClearContainer(_playerHandContainer);
        _cardPanels.Clear();
        RefreshTableCards();
        RefreshTombos();
        ShowClosedVira();
        _statusLabel.Text = "Baralho embaralhado — corte para distribuir";
        _statusLabel.AddThemeColorOverride("font_color", Gold);
        _cutDeckBtn.Visible = true;
        AnimateShuffle();
    }

    private void OnDeckCut(int cutPosition)
    {
        Core.Systems.AudioManager.Instance?.PlaySound("cut");
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

    private async void AnimateShuffle()
    {
        _cutDeckBtn.Disabled = true;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInsideTree() || _game.CurrentPhase != TrucoGameManager.TrucoPhase.Cutting) return;
        ClearDealAnimationLayer();
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true)
        {
            _deckStackVisual = CreateAnimatedCardBack();
            _deckStackVisual.Position = GetDeckScreenPosition() - _deckStackVisual.Size / 2f;
            _dealAnimationLayer.AddChild(_deckStackVisual);
            _cutDeckBtn.Disabled = false;
            return;
        }
        _cutDeckBtn.Disabled = true;
        Vector2 center = GetDeckScreenPosition();
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
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
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
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        if (_game.PenaCard == null) return;
        var card = CreateCardPanel(_game.PenaCard, -1);
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Size = card.CustomMinimumSize;
        card.Position = GetDeckScreenPosition() - card.Size / 2f;
        _dealAnimationLayer.AddChild(card);
        var target = GetSeatScreenPosition(_game.PenaRecipientSeatIndex) - card.Size / 2f;
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(card, "position", target, MotionDuration(0.38f)).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "rotation", Mathf.DegToRad(-8), MotionDuration(0.38f));
    }

    private void AnimatePenaResolution(bool kept)
    {
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        if (kept) return;
        // A declined pena returns from the ally and settles at the vira/tombo.
        var cards = _dealAnimationLayer.GetChildren();
        if (cards.Count == 0) return;
        if (cards[cards.Count - 1] is not Control card) return;
        var target = _viraCardContainer.GetGlobalRect().GetCenter() - card.Size / 2f;
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(card, "position", target, MotionDuration(0.4f)).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "rotation", Mathf.DegToRad(7), MotionDuration(0.4f));
    }

    private void AnimateDistribution(int cardCount, int penaRecipientSeat, bool penaKept)
    {
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        int totalSeats = _game.TeamSize * 2;
        Vector2 deckPosition = GetDeckScreenPosition();
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
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true)
        {
            ClearDealAnimationLayer();
            return;
        }
        Vector2 center = GetDeckScreenPosition();
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
            var panel = CreateCardPanel(_game.ViraCard, -1);
            panel.MouseFilter = MouseFilterEnum.Ignore; // Vira card is not clickable
            _viraCardContainer.AddChild(panel);
        }
    }

    private void OnTrucoCalled(int stakes, bool byPlayer)
    {
        _stage.React(byPlayer ? 0 : 1);
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
        Core.Systems.AudioManager.Instance?.PlaySound("play");
        int seat = _game.LastPlayedSeatIndex;
        _stage.React(seat);
        _stage.SetCardCount(seat, _game.GetSeatCardCount(seat));
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        var data = who == 0 ? _game.PlayerPlayed[roundIdx] : _game.OpponentPlayed[roundIdx];
        var row = who == 0 ? _tablePlayerRow : _tableOpponentRow;
        var targetCard = row.GetChild<Control>(roundIdx);
        targetCard.Modulate = new Color(1, 1, 1, 0);
        var flying = CreateSmallCard(data, who != 0);
        flying.Size = new Vector2(76, 108);
        flying.Position = seat == 0 ? _playerHandContainer.GetGlobalRect().GetCenter() : _stage.SeatScreenPosition(seat);
        flying.PivotOffset = flying.Size / 2;
        _dealAnimationLayer.AddChild(flying);
        var tween = CreateTween();
        tween.TweenProperty(flying, "position", targetCard.GlobalPosition, .34f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(flying, "rotation", .08f, .34f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (IsInstanceValid(targetCard)) targetCard.Modulate = Colors.White;
            if (IsInstanceValid(flying)) flying.QueueFree();
        }));
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
        if (playerWon) Core.Systems.AudioManager.Instance?.PlaySound("win");
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
        _roundLabel.Text = $"TOMBO {_game.CurrentRound + 1:00} / 03";
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
            _statusLabel.Text = $"{_game.GetSeatName(_game.ActiveSeatIndex)} pensando...";
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
