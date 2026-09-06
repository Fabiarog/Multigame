using Godot;
using System.Collections.Generic;
using System.Linq;
using GameHub.Core.Visuals;

namespace GameHub.Games.PokerRoguelike;

/// <summary>Presentation for the Poker run. Game state remains in PokerGameManager.</summary>
public partial class PokerUI : Control
{
    private PokerGameManager _game;
    private Label _roundLabel, _scoreLabel, _targetLabel, _handsLabel, _discardsLabel;
    private Label _relicsLabel, _opponentsLabel, _resultLabel, _breakdownLabel, _walletLabel;
    private Label _bossName, _selectionLabel;
    private TextureRect _bossPortrait;
    private ProgressBar _scoreBar;
    private HBoxContainer _dealerCardContainer, _cardContainer;
    private Control _tableArea, _tableCardLayer;
    private Label _scorePopup;
    private Button _playBtn, _discardBtn;
    private readonly List<PanelContainer> _cardPanels = new();
    private readonly List<PanelContainer> _dealerCardPanels = new();
    private bool _ignoreInput;
    private Tween _popupTween;
    private TableStage _stage;
    private int _presentedRound = -1;

    private Control _overlayPanel, _shopOverlay, _tutorialPanel;
    private Label _overlayTitle, _overlaySubtitle, _goldLabel, _shopMessage;
    private Button _nextRoundBtn, _newGameBtn, _backMenuBtn;
    private Button _buyMultBtn, _buyHandBtn, _buyDiscardBtn, _shopNextRoundBtn;
    private Label _tutorialTitle, _tutorialDesc;
    private Core.AI.TutorialController _tutorialController;

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
        var backdrop = new ClubBackdrop { ShowTable = false, MouseFilter = MouseFilterEnum.Ignore };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var edge in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, 24);
        AddChild(margin);
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        margin.AddChild(columns);
        BuildSidebar(columns);

        var main = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        main.AddThemeConstantOverride("separation", 12);
        columns.AddChild(main);
        var heading = new HBoxContainer();
        heading.AddChild(DisplayLabel("Pôquer", 40));
        heading.AddChild(Spacer());
        var mode = ClubTheme.Label("ROGUELIKE  /  CORRIDA DE PONTOS", 13, ClubTheme.Muted);
        mode.VerticalAlignment = VerticalAlignment.Center;
        heading.AddChild(mode);
        main.AddChild(heading);

        var dealerPanel = Panel(new Color("102c26"), ClubTheme.Border, 12);
        var dealerRow = new HBoxContainer();
        dealerRow.AddThemeConstantOverride("separation", 14);
        dealerPanel.AddChild(dealerRow);
        _bossPortrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(92, 92),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        var chroma = ResourceLoader.Load<Shader>("res://assets/shaders/CanvasChromaKey.gdshader");
        if (chroma != null)
            _bossPortrait.Material = new ShaderMaterial { Shader = chroma };
        dealerRow.AddChild(_bossPortrait);
        var bossInfo = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        bossInfo.AddThemeConstantOverride("separation", 5);
        bossInfo.AddChild(ClubTheme.Label("CHEFE DA MESA", 12, ClubTheme.Gold));
        _bossName = ClubTheme.Label("Tartaruga", 24);
        bossInfo.AddChild(_bossName);
        _opponentsLabel = ClubTheme.Label("1 bot à mesa", 13, ClubTheme.Muted);
        bossInfo.AddChild(_opponentsLabel);
        dealerRow.AddChild(bossInfo);
        dealerRow.AddChild(Spacer());
        var rack = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        rack.AddThemeConstantOverride("separation", 5);
        _dealerCardContainer = new HBoxContainer();
        _dealerCardContainer.AddThemeConstantOverride("separation", 6);
        rack.AddChild(_dealerCardContainer);
        var dealerHint = ClubTheme.Label("CARTAS DO CHEFE", 11, ClubTheme.Muted);
        dealerHint.HorizontalAlignment = HorizontalAlignment.Center;
        rack.AddChild(dealerHint);
        dealerRow.AddChild(rack);
        main.AddChild(dealerPanel);

        _tableArea = new Control
        {
            CustomMinimumSize = new Vector2(0, 140),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        main.AddChild(_tableArea);
        _stage = new TableStage { SeatCount = 2, RivalIndex = 2 };
        _tableArea.AddChild(_stage);
        var tableHint = ClubTheme.Label("FORME SUA MÃO. SUPERE A META.", 12, new Color("718c77"));
        tableHint.HorizontalAlignment = HorizontalAlignment.Center;
        tableHint.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        tableHint.VerticalAlignment = VerticalAlignment.Bottom;
        _tableArea.AddChild(tableHint);

        var tray = Panel(ClubTheme.Panel, ClubTheme.Border, 16);
        var trayBox = new VBoxContainer();
        trayBox.AddThemeConstantOverride("separation", 10);
        tray.AddChild(trayBox);
        var handHeading = new HBoxContainer();
        handHeading.AddChild(ClubTheme.Label("SUA MÃO", 13, ClubTheme.Gold));
        handHeading.AddChild(Spacer());
        _selectionLabel = ClubTheme.Label("0 / 5 selecionadas", 13, ClubTheme.Muted);
        handHeading.AddChild(_selectionLabel);
        trayBox.AddChild(handHeading);
        var cardCenter = new CenterContainer();
        _cardContainer = new HBoxContainer();
        _cardContainer.AddThemeConstantOverride("separation", 10);
        cardCenter.AddChild(_cardContainer);
        trayBox.AddChild(cardCenter);
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);
        var preview = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        preview.AddThemeConstantOverride("separation", 2);
        _resultLabel = ClubTheme.Label("Escolha suas cartas", 18);
        _breakdownLabel = ClubTheme.Label("De 1 a 5 cartas para formar uma combinação.", 13, ClubTheme.Muted);
        _breakdownLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        preview.AddChild(_resultLabel);
        preview.AddChild(_breakdownLabel);
        actions.AddChild(preview);
        _discardBtn = ClubTheme.Button("Descartar");
        _discardBtn.CustomMinimumSize = new Vector2(132, 46);
        _discardBtn.Pressed += OnDiscardPressed;
        actions.AddChild(_discardBtn);
        _playBtn = ClubTheme.Button("Jogar mão", true);
        _playBtn.CustomMinimumSize = new Vector2(156, 46);
        _playBtn.Pressed += OnPlayPressed;
        actions.AddChild(_playBtn);
        trayBox.AddChild(actions);
        main.AddChild(tray);

        // Overlay cards use global coordinates; layout controls remain in their containers.
        _tableCardLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _tableCardLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_tableCardLayer);
        _scorePopup = ClubTheme.Label("", 24, ClubTheme.Gold);
        _scorePopup.HorizontalAlignment = HorizontalAlignment.Center;
        _scorePopup.MouseFilter = MouseFilterEnum.Ignore;
        _scorePopup.Visible = false;
        _tableCardLayer.AddChild(_scorePopup);
        BuildResultOverlay();
        BuildShopOverlay();
        BuildTutorialPanel();
        AccessibilityVisuals.AddGlobalFilter(this);
    }

    private void BuildSidebar(HBoxContainer columns)
    {
        var side = Panel(ClubTheme.Panel, ClubTheme.Border, 20);
        side.CustomMinimumSize = new Vector2(260, 0);
        columns.AddChild(side);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        side.AddChild(box);
        box.AddChild(ClubTheme.Label("A SUA CORRIDA", 12, ClubTheme.Gold));
        _roundLabel = ClubTheme.Label("Rodada 1 / 8", 24);
        box.AddChild(_roundLabel);
        box.AddChild(ClubTheme.Label("Alcance a meta antes\nde acabar suas mãos.", 14, ClubTheme.Muted));
        box.AddChild(Rule());
        box.AddChild(ClubTheme.Label("PONTOS NA MESA", 12, ClubTheme.Muted));
        _scoreLabel = ClubTheme.Label("0", 44, ClubTheme.Paper);
        _scoreLabel.AddThemeFontOverride("font", ClubTheme.MonoFont);
        box.AddChild(_scoreLabel);
        _targetLabel = ClubTheme.Label("META  300", 17, ClubTheme.Gold);
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
        _handsLabel = ClubTheme.Label("MÃOS  4", 19);
        _discardsLabel = ClubTheme.Label("DESCARTES  3", 19);
        _walletLabel = ClubTheme.Label("FICHAS  0", 19, ClubTheme.Gold);
        box.AddChild(_handsLabel);
        box.AddChild(_discardsLabel);
        box.AddChild(_walletLabel);
        box.AddChild(Rule());
        box.AddChild(ClubTheme.Label("RELÍQUIAS ATIVAS", 12, ClubTheme.Gold));
        _relicsLabel = ClubTheme.Label("Ás de sorte\n+1 Mult ao jogar um Ás", 14, ClubTheme.Muted);
        _relicsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _relicsLabel.CustomMinimumSize = new Vector2(210, 54);
        box.AddChild(_relicsLabel);
        box.AddChild(Spacer());
        var exit = ClubTheme.Button("Voltar ao clube");
        exit.Pressed += ReturnToHub;
        box.AddChild(exit);
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
            _game.StartNewGame();
        };
        box.AddChild(_newGameBtn);
        _backMenuBtn = ClubTheme.Button("Voltar ao clube");
        _backMenuBtn.Pressed += ReturnToHub;
        box.AddChild(_backMenuBtn);
    }

    private void BuildShopOverlay()
    {
        var box = CreateModal(out _shopOverlay, new Vector2(880, 0));
        box.AddChild(ClubTheme.Label("ENTRE RODADAS", 12, ClubTheme.Gold));
        var heading = new HBoxContainer();
        heading.AddChild(DisplayLabel("A banca", 42));
        heading.AddChild(Spacer());
        _goldLabel = ClubTheme.Label("0 fichas", 24, ClubTheme.Gold);
        heading.AddChild(_goldLabel);
        box.AddChild(heading);
        box.AddChild(ClubTheme.Label("Invista suas fichas. Os bônus duram até o fim desta corrida.", 16, ClubTheme.Muted));
        var offers = new HBoxContainer();
        offers.AddThemeConstantOverride("separation", 16);
        _buyMultBtn = ShopOffer(offers, "×", "+1 Multiplicador", "Mais força em toda\ncombinação jogada.", 10, "multiplier");
        _buyHandBtn = ShopOffer(offers, "01", "+1 Mão", "Uma jogada a mais\nem cada nova rodada.", 15, "hand");
        _buyDiscardBtn = ShopOffer(offers, "↻", "+1 Descarte", "Mais uma chance de\nrenovar suas cartas.", 8, "discard");
        box.AddChild(offers);
        _shopMessage = ClubTheme.Label("", 14, ClubTheme.Muted);
        box.AddChild(_shopMessage);
        _shopNextRoundBtn = ClubTheme.Button("Próxima rodada  →", true);
        _shopNextRoundBtn.Pressed += () =>
        {
            _shopOverlay.Visible = false;
            _game.StartNextRound();
        };
        box.AddChild(_shopNextRoundBtn);
    }

    private Button ShopOffer(HBoxContainer row, string icon, string title, string description, int cost, string type)
    {
        var panel = Panel(ClubTheme.Ink, ClubTheme.Border, 18);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 14);
        box.AddChild(ClubTheme.Label(icon, 40, ClubTheme.Gold));
        box.AddChild(ClubTheme.Label(title, 19));
        box.AddChild(ClubTheme.Label(description, 14, ClubTheme.Muted));
        var buy = ClubTheme.Button($"Comprar · {cost} fichas");
        buy.Pressed += () =>
        {
            if (!_game.BuyUpgrade(type, cost)) return;
            RefreshShopUI();
            _shopMessage.Text = $"{title} adquirido. O bônus já está ativo para a próxima rodada.";
        };
        box.AddChild(buy);
        panel.AddChild(box);
        row.AddChild(panel);
        return buy;
    }

    private void RefreshShopUI()
    {
        _goldLabel.Text = $"{_game.Gold} fichas";
        _buyMultBtn.Disabled = _game.Gold < 10;
        _buyHandBtn.Disabled = _game.Gold < 15;
        _buyDiscardBtn.Disabled = _game.Gold < 8;
        _shopMessage.Text = $"Bônus: +{_game.BaseMultiplierBonus} Mult · +{_game.ExtraHands} mãos · +{_game.ExtraDiscards} descartes";
        _shopNextRoundBtn.Text = _game.CurrentRound >= PokerGameManager.MaxRounds ? "Concluir a corrida  →" : "Próxima rodada  →";
        RefreshResources();
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

    private void OnHandDealt()
    {
        _stage.ClearPlayedCards();
        _ignoreInput = _game.CurrentPhase != PokerGameManager.GamePhase.PlayerTurn;
        ClearTableCards();
        ClearContainer(_cardContainer);
        _cardPanels.Clear();
        var hand = _game.GetPlayerHand();
        for (int i = 0; i < hand.Count; i++)
        {
            var panel = CreateCardPanel(hand[i], i);
            _cardContainer.AddChild(panel);
            _cardPanels.Add(panel);
            FadeIn(panel, .28f + i * .035f);
            int order = i;
            Callable.From(() => AnimateDealToHand(panel, order)).CallDeferred();
        }
        RefreshDealerDisplay();
        RefreshBoss();
        RefreshResources();
        UpdateActionButtons();
        RefreshPreview();
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
        tween.TweenInterval(order * .035f);
        tween.TweenProperty(back, "position", target.GlobalPosition, .28f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(back, "scale", Vector2.One, .28f);
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
            string theme = rival == 6 ? "barao_lounge" : "dama_salon";
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
            Core.Systems.AudioManager.Instance?.PlayMusic(rival == 6 ? "midnight-baron" : "velvet-table");
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
            _stage.PlayCard(1, hand[i].ToString(), .15f + i * .08f);
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
        if (passed) Core.Systems.AudioManager.Instance?.PlaySound("win");
        _tutorialPanel.Visible = false;
        _overlayTitle.Text = passed ? $"Mesa {round} vencida" : "A mesa levou a melhor";
        _overlayTitle.AddThemeColorOverride("font_color", passed ? ClubTheme.Gold : new Color("#e29a86"));
        _overlaySubtitle.Text = passed
            ? $"{_game.RoundScore:N0} de {_game.RoundTarget:N0} pontos.\nVocê tem {_game.Gold} fichas para investir na banca."
            : $"{_game.RoundScore:N0} de {_game.RoundTarget:N0} pontos. Tente uma nova combinação de estratégias.";
        _nextRoundBtn.Visible = passed;
        _newGameBtn.Visible = !passed;
        _overlayPanel.Visible = true;
        if (passed) _nextRoundBtn.GrabFocus(); else _newGameBtn.GrabFocus();
    }

    private void OnGameEnded(bool won, int totalScore)
    {
        if (won) CharacterProgress.RecordWin();
        _shopOverlay.Visible = false;
        _tutorialPanel.Visible = false;
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
        _ignoreInput = (PokerGameManager.GamePhase)phase != PokerGameManager.GamePhase.PlayerTurn;
        UpdateActionButtons();
    }

    private void OnPlayPressed()
    {
        if (_ignoreInput || _stage.IsPresenting || !_game.CanPlayHand()) return;
        _ignoreInput = true;
        _stage.React(0);
        Core.Systems.AudioManager.Instance?.PlaySound("play");
        UpdateActionButtons();
        AnimatePlayedCards();
        _game.PlayHand();
    }

    private void OnDiscardPressed()
    {
        if (!_ignoreInput && !_stage.IsPresenting && _game.CanDiscard()) _game.DiscardCards();
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
            _stage.PlayCard(0, hand[index].ToString(), i * .06f);
        }
        _stage.SetCardCount(0, hand.Count-selected.Count);
    }

    private void ShowScorePopup(string handName, int score)
    {
        _popupTween?.Kill();
        _scorePopup.Text = $"{handName}  +{score:N0}";
        var area = _tableArea.GetGlobalRect();
        _scorePopup.Position = new Vector2(area.Position.X, area.Position.Y + 4);
        _scorePopup.Size = new Vector2(area.Size.X, 36);
        _scorePopup.Visible = true;
        _scorePopup.Modulate = Colors.White;
        _popupTween = CreateTween();
        _popupTween.TweenInterval(1.1f);
        _popupTween.TweenProperty(_scorePopup, "modulate:a", 0.0f, MotionDuration(0.2f));
    }

    private void ClearTableCards()
    {
        if (_tableCardLayer == null) return;
        _popupTween?.Kill();
        _scorePopup.Visible = false;
        foreach (var child in _tableCardLayer.GetChildren())
        {
            if (child == _scorePopup) continue;
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
