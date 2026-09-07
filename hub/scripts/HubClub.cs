using Godot;
using GameHub.Core.Networking;
using GameHub.Core.Registry;
using GameHub.Core.Systems;
using GameHub.Core.Visuals;

namespace GameHub.Hub.Scripts;

public partial class HubMain
{
    private OptionButton _modeSelect, _botSelect, _difficultySelect, _teamSelect;
    private Control _botField, _difficultyField, _teamField;
    private Button _pokerTile, _trucoTile, _fodinhaTile, _playButton, _step1ActionBtn;
    private Control _lanActions;
    private Label _gameDescription;

    private VBoxContainer _homeMenuContainer, _creationContainer;
    private VBoxContainer _step1Game, _step2Config, _step3Map;
    private Button _mapClassicTile, _mapBaraoTile, _mapDamaTile, _mapCyberTile;
    private Label _stepIndicatorLabel;
    private string _selectedMapId = "classic_club";
    private int _creationStep = 1;

    private void BuildUI()
    {
        Theme = ClubTheme.Create();
        AudioManager.Instance?.PlayMusic("midnight-club");
        AddChild(new ClubBackdrop { ShowTable = false });
        _mainMenuPanel = BuildMainMenu();
        _settingsPanel = FramePage(BuildSettingsPanel());
        _lobbyPanel = FramePage(BuildLobbyPanel());
        _collectiblesPanel = FramePage(BuildCollectiblesPanel());
        _accessibilityPanel = BuildAccessibilityPanel();
        _creditsPanel = FramePage(BuildCreditsPanel());
        foreach (var page in new[] { _mainMenuPanel, _settingsPanel, _lobbyPanel, _collectiblesPanel, _accessibilityPanel, _creditsPanel }) AddChild(page);
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LobbyUpdated += OnLobbyUpdated;
            LobbyManager.Instance.MatchStarting += OnMatchStarting;
        }
        AccessibilityVisuals.AddGlobalFilter(this);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") && _currentState != HubState.MainMenu)
        {
            if (_currentState == HubState.Lobby) LobbyManager.Instance?.LeaveLobby();
            ShowMenu(HubState.MainMenu);
            GetViewport().SetInputAsHandled();
        }
    }

    private Control BuildMainMenu()
    {
        var root = new MarginContainer { Name = "MainMenu" };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("margin_left", 36);
        root.AddThemeConstantOverride("margin_right", 36);
        root.AddThemeConstantOverride("margin_top", 16);
        root.AddThemeConstantOverride("margin_bottom", 16);
        var column = Column(10);
        root.AddChild(column);

        // Header with brand
        var header = Row(10);
        header.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/ui/club-mark.svg"),
            CustomMinimumSize = new Vector2(36, 36), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        var brand = Column(0);
        brand.AddChild(Display("MultiGame", 26));
        brand.AddChild(Eyebrow("C L U B E   D E   C A R T A S", 9));
        header.AddChild(brand);
        header.AddChild(Expand());
        header.AddChild(Eyebrow("EDIÇÃO CLUBE · 0.2", 11));
        column.AddChild(header);
        column.AddChild(Rule());

        var body = Row(32);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddChild(body);

        var left = Column(8);
        left.CustomMinimumSize = new Vector2(540, 0);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(left);

        // ==================== HOME MENU (VERTICAL LIST) ====================
        _homeMenuContainer = Column(8);
        _homeMenuContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        left.AddChild(_homeMenuContainer);

        var homeEyebrow = Eyebrow("BEM-VINDO AO CLUBE");
        _homeMenuContainer.AddChild(homeEyebrow);

        var homeTitle = Display("A sorte é só\no começo.", 42);
        homeTitle.AddThemeConstantOverride("line_spacing", -6);
        _homeMenuContainer.AddChild(homeTitle);
        _homeMenuContainer.AddChild(Paragraph("Boas mãos. Grandes blefes. Mais uma rodada.\nEscolha uma das opções abaixo para começar.", 13));

        _homeMenuContainer.AddChild(CreateFixedSpacer(4));

        var navButtons = Column(6);
        _homeMenuContainer.AddChild(navButtons);

        var btnPlay = Action("Jogar", SwitchToGameCreation, true);
        btnPlay.CustomMinimumSize = new Vector2(0, 46);
        navButtons.AddChild(btnPlay);

        var btnLobby = Action("Entrar em sala", JoinSelectedLobby);
        btnLobby.CustomMinimumSize = new Vector2(0, 36);
        navButtons.AddChild(btnLobby);

        var btnSettings = Action("Ajustes & Configurações", () => ShowMenu(HubState.Settings));
        btnSettings.CustomMinimumSize = new Vector2(0, 36);
        navButtons.AddChild(btnSettings);

        var btnAccess = Action("Acessibilidade", () => ShowMenu(HubState.Accessibility));
        btnAccess.CustomMinimumSize = new Vector2(0, 36);
        navButtons.AddChild(btnAccess);

        var btnCollect = Action("Coleção", () => ShowMenu(HubState.Collectibles));
        btnCollect.CustomMinimumSize = new Vector2(0, 36);
        navButtons.AddChild(btnCollect);

        var btnCredits = Action("Créditos", () => ShowMenu(HubState.Credits));
        btnCredits.CustomMinimumSize = new Vector2(0, 36);
        navButtons.AddChild(btnCredits);

        var btnExit = Action("Sair", () => GetTree().Quit());
        btnExit.CustomMinimumSize = new Vector2(0, 36);
        navButtons.AddChild(btnExit);

        _homeMenuContainer.AddChild(Expand(true));

        // ==================== CREATION CONTAINER ====================
        _creationContainer = Column(12);
        _creationContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        _creationContainer.Visible = false;
        left.AddChild(_creationContainer);

        var creationTopBar = Row(10);
        var backToHomeBtn = Action("← Voltar ao Menu", SwitchToHomeMenu);
        backToHomeBtn.CustomMinimumSize = new Vector2(160, 38);
        creationTopBar.AddChild(backToHomeBtn);
        creationTopBar.AddChild(Expand());
        _stepIndicatorLabel = Eyebrow("ETAPA 1 DE 3 · ESCOLHA DO JOGO");
        _stepIndicatorLabel.VerticalAlignment = VerticalAlignment.Center;
        creationTopBar.AddChild(_stepIndicatorLabel);
        _creationContainer.AddChild(creationTopBar);

        // ==================== STEP 1: GAME SELECTION ====================
        _step1Game = Column(12);
        _step1Game.SizeFlagsVertical = SizeFlags.ExpandFill;
        _creationContainer.AddChild(_step1Game);

        var title1 = Display("Escolha seu Jogo", 42);
        _step1Game.AddChild(title1);
        _step1Game.AddChild(Paragraph("Selecione a modalidade que você deseja disputar.", 14));

        var choices = Row(10);
        _step1Game.AddChild(choices);
        _pokerTile = GameTile("♠", "Pôquer", "ROGUELIKE", "01");
        _trucoTile = GameTile("♣", "Truco", "BLEFE & MANILHA", "02");
        _fodinhaTile = GameTile("♦", "Fodinha", "PALPITES & VIDAS", "03");
        choices.AddChild(_pokerTile); choices.AddChild(_trucoTile); choices.AddChild(_fodinhaTile);
        _fodinhaTile.Pressed += () => SelectGame("fodinha");
        _pokerTile.Pressed += () => SelectGame("poker_roguelike");
        _trucoTile.Pressed += () => SelectGame("truco");

        _step1Game.AddChild(Expand(true));
        _step1ActionBtn = Action("Iniciar Pôquer Roguelike   →", OnStep1ActionPressed, true);
        _step1ActionBtn.CustomMinimumSize = new Vector2(0, 50);
        _step1Game.AddChild(_step1ActionBtn);

        // ==================== STEP 2: RULES & BOTS ====================
        _step2Config = Column(12);
        _step2Config.SizeFlagsVertical = SizeFlags.ExpandFill;
        _step2Config.Visible = false;
        _creationContainer.AddChild(_step2Config);

        var title2 = Display("Regras da Mesa", 42);
        _step2Config.AddChild(title2);
        _step2Config.AddChild(Paragraph("Defina o modo de partida, quantidade de oponentes e nível de desafio.", 14));

        var options = Row(10);
        _step2Config.AddChild(options);
        _modeSelect = Select("Solo", "Tutorial (IA)", "Sala LAN");
        _botSelect = Select("1 bot", "2 bots", "3 bots");
        _difficultySelect = Select("Fácil", "Normal", "Difícil");
        _difficultySelect.Selected = 1;
        _teamSelect = Select("1 × 1", "2 × 2", "3 × 3");
        options.AddChild(Field("MODO DE JOGO", _modeSelect));
        _botField = Field("OPONENTES", _botSelect);
        _difficultyField = Field("DIFICULDADE", _difficultySelect);
        _teamField = Field("EQUIPES", _teamSelect);
        options.AddChild(_botField); options.AddChild(_difficultyField); options.AddChild(_teamField);
        _modeSelect.ItemSelected += _ => UpdateGameOptions();
        _teamSelect.ItemSelected += _ => GameRegistry.TrucoTeamSize = _teamSelect.Selected + 1;

        _step2Config.AddChild(Expand(true));

        var navRow2 = Row(12);
        var backTo1Btn = Action("← Escolher Jogo", () => SetCreationStep(1));
        backTo1Btn.CustomMinimumSize = new Vector2(170, 48);
        navRow2.AddChild(backTo1Btn);
        var nextToMapBtn = Action("Escolher Salão 3D   →", () => SetCreationStep(3), true);
        nextToMapBtn.CustomMinimumSize = new Vector2(0, 48);
        nextToMapBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        navRow2.AddChild(nextToMapBtn);
        _step2Config.AddChild(navRow2);

        // ==================== STEP 3: 3D ROOM / MAP SELECTION ====================
        _step3Map = Column(12);
        _step3Map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _step3Map.Visible = false;
        _creationContainer.AddChild(_step3Map);

        var title3 = Display("Salão da Mesa 3D", 42);
        _step3Map.AddChild(title3);
        _step3Map.AddChild(Paragraph("Escolha a atmosfera onde você desafiará seus rivais.", 14));

        var mapGrid = new GridContainer { Columns = 2 };
        mapGrid.AddThemeConstantOverride("h_separation", 10);
        mapGrid.AddThemeConstantOverride("v_separation", 10);
        _step3Map.AddChild(mapGrid);

        _mapClassicTile = MapTile("🏛", "Salão Clássico", "Lareira, estantes e veludo.", "classic_club");
        _mapBaraoTile = MapTile("🦉", "Lounge do Barão", "Arcos góticos, luar e púrpura.", "barao_lounge");
        _mapDamaTile = MapTile("🌹", "Salão da Dama", "Art Nouveau, champanhe e rosas.", "dama_salon");
        _mapCyberTile = MapTile("⚡", "Cassino Cyber", "Skyline neon, painéis e holograma.", "cyber_casino");
        mapGrid.AddChild(_mapClassicTile); mapGrid.AddChild(_mapBaraoTile);
        mapGrid.AddChild(_mapDamaTile); mapGrid.AddChild(_mapCyberTile);

        _step3Map.AddChild(Expand(true));

        var navRow3 = Row(12);
        var backTo2Btn = Action("← Voltar às Regras", () => SetCreationStep(2));
        backTo2Btn.CustomMinimumSize = new Vector2(170, 50);
        navRow3.AddChild(backTo2Btn);
        _playButton = Action("Sentar à mesa   →", LaunchGame, true);
        _playButton.CustomMinimumSize = new Vector2(0, 50);
        _playButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        navRow3.AddChild(_playButton);
        _step3Map.AddChild(navRow3);

        // ==================== RIGHT PANEL ====================
        var right = Column(8);
        right.CustomMinimumSize = new Vector2(510, 0);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(right);
        var frame = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
        frame.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Border, 1, 8));
        frame.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/ui/club-table.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore });
        right.AddChild(frame);
        _gameDescription = Paragraph("", 13);
        _gameDescription.CustomMinimumSize = new Vector2(0, 34);
        right.AddChild(_gameDescription);
        var lan = Row(10);
        _lanActions = lan;
        var hostBtn = Action("Criar sala LAN", HostSelectedLobby);
        hostBtn.CustomMinimumSize = new Vector2(0, 36);
        lan.AddChild(hostBtn);
        var joinBtn = Action("Entrar em sala", JoinSelectedLobby);
        joinBtn.CustomMinimumSize = new Vector2(0, 36);
        lan.AddChild(joinBtn);
        foreach (Control control in lan.GetChildren()) control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        right.AddChild(lan);
        column.AddChild(Rule());
        var footer = Row(10);
        footer.AddChild(Eyebrow("PÔQUER  /  TRUCO  /  FODINHA", 11));
        footer.AddChild(Expand());
        footer.AddChild(Eyebrow("MULTI-GAME ECOSYSTEM", 11));
        column.AddChild(footer);

        if (SettingsManager.Instance != null && !string.IsNullOrWhiteSpace(SettingsManager.Instance.RoomTheme))
            _selectedMapId = SettingsManager.Instance.RoomTheme;

        SelectGame("poker_roguelike");
        UpdateMapTiles();
        return root;
    }

    private void SwitchToGameCreation()
    {
        _homeMenuContainer.Visible = false;
        _creationContainer.Visible = true;
        SetCreationStep(1);
    }

    private void SwitchToHomeMenu()
    {
        _homeMenuContainer.Visible = true;
        _creationContainer.Visible = false;
        UpdateGameDescription();
    }

    private void OnStep1ActionPressed()
    {
        if (_selectedGameId == "poker_roguelike")
        {
            // Direct launch! Poker adapts maps automatically per boss!
            LaunchGame();
        }
        else
        {
            SetCreationStep(2);
        }
    }

    private void SetCreationStep(int step)
    {
        _creationStep = Mathf.Clamp(step, 1, 3);
        _step1Game.Visible = _creationStep == 1;
        _step2Config.Visible = _creationStep == 2;
        _step3Map.Visible = _creationStep == 3;

        _stepIndicatorLabel.Text = _creationStep switch
        {
            1 => _selectedGameId == "poker_roguelike" ? "MODO ROGUELIKE · ENTRADA DIRETA" : "ETAPA 1 DE 3 · ESCOLHA DO JOGO",
            2 => "ETAPA 2 DE 3 · REGRAS & OPONENTES",
            3 => "ETAPA 3 DE 3 · SALÃO DA MESA 3D",
            _ => "CRIAR PARTIDA"
        };

        if (_creationStep == 3)
        {
            _gameDescription.Text = GetMapDescription(_selectedMapId);
            UpdateMapTiles();
        }
        else
        {
            UpdateGameDescription();
        }
    }

    private Button MapTile(string icon, string name, string subtitle, string mapId)
    {
        var btn = ClubTheme.Button("");
        btn.CustomMinimumSize = new Vector2(245, 84);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var box = Column(3);
        box.MouseFilter = MouseFilterEnum.Ignore;
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        box.OffsetLeft = 14; box.OffsetTop = 10; box.OffsetRight = -10; box.OffsetBottom = -8;
        box.AddChild(ClubTheme.Label(icon + "  " + name, 17));
        box.AddChild(Paragraph(subtitle, 12));
        btn.AddChild(box);
        btn.Pressed += () => SelectMap(mapId);
        return btn;
    }

    private void SelectMap(string mapId)
    {
        _selectedMapId = mapId;
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.RoomTheme = mapId;
            SettingsManager.Instance.SaveSettings();
        }
        _gameDescription.Text = GetMapDescription(mapId);
        UpdateMapTiles();
    }

    private void UpdateMapTiles()
    {
        string cur = SettingsManager.Instance?.RoomTheme ?? _selectedMapId;
        UpdateTileHighlight(_mapClassicTile, cur == "classic_club");
        UpdateTileHighlight(_mapBaraoTile, cur == "barao_lounge");
        UpdateTileHighlight(_mapDamaTile, cur == "dama_salon");
        UpdateTileHighlight(_mapCyberTile, cur == "cyber_casino");
    }

    private void UpdateTileHighlight(Button btn, bool selected)
    {
        if (btn == null) return;
        btn.AddThemeStyleboxOverride("normal", ClubTheme.Box(selected ? ClubTheme.Green : ClubTheme.Panel, selected ? Gold : ClubTheme.Border, 12, 6));
    }

    private static string GetMapDescription(string mapId) => mapId switch
    {
        "barao_lounge" => "02 / LOUNGE DO BARÃO\nSantuário gótico noturno com grandes vitrais ogivais, luar e púrpura imperial.",
        "dama_salon"   => "03 / SALÃO DA DAMA\nRequinte Belle Époque com espelhos ovais dourados, carrinho de champanhe e rosas.",
        "cyber_casino" => "04 / CASSINO CYBER\nSky-lounge VIP com vista panorâmica da metrópole neon, painéis de carbono e holografia.",
        _              => "01 / SALÃO CLÁSSICO\nTradição britânica em mogno nobre, grande lareira de tijolos e estantes de livros."
    };

    private void UpdateGameDescription()
    {
        bool poker = _selectedGameId == "poker_roguelike";
        if (poker) _gameDescription.Text = "01 / PÔQUER ROGUELIKE\nCombine cartas, supere metas e fortaleça sua próxima mão.";
        else if (_selectedGameId == "truco") _gameDescription.Text = "02 / TRUCO\nCorte o baralho, descubra a manilha e sustente seu blefe.";
        else if (_selectedGameId == "fodinha") _gameDescription.Text = "03 / FODINHA · SOLO COM 3 IAs\nCinco vidas. Dê seu palpite e ganhe exatamente o que prometeu.";
    }

    private void SelectGame(string id)
    {
        _selectedGameId = id;
        bool poker = id == "poker_roguelike";
        _pokerTile.AddThemeStyleboxOverride("normal", ClubTheme.Box(poker ? ClubTheme.Green : ClubTheme.Panel, poker ? Gold : ClubTheme.Border, 12, 6));
        _trucoTile.AddThemeStyleboxOverride("normal", ClubTheme.Box(id == "truco" ? ClubTheme.Green : ClubTheme.Panel, id == "truco" ? Gold : ClubTheme.Border, 12, 6));
        _fodinhaTile.AddThemeStyleboxOverride("normal", ClubTheme.Box(id == "fodinha" ? ClubTheme.Green : ClubTheme.Panel, id == "fodinha" ? Gold : ClubTheme.Border, 12, 6));

        if (_step1ActionBtn != null)
        {
            _step1ActionBtn.Text = poker ? "Iniciar Pôquer Roguelike   →" : "Configurar Partida   →";
        }

        if (_stepIndicatorLabel != null && _creationStep == 1)
        {
            _stepIndicatorLabel.Text = poker ? "MODO ROGUELIKE · ENTRADA DIRETA" : "ETAPA 1 DE 3 · ESCOLHA DO JOGO";
        }

        UpdateGameDescription();
        UpdateGameOptions();
    }

    private void UpdateGameOptions()
    {
        bool poker = _selectedGameId == "poker_roguelike";
        _modeSelect.SetItemDisabled(1, !poker);
        if (!poker && _modeSelect.Selected == 1) _modeSelect.Selected = 0;
        _botField.Visible = _difficultyField.Visible = poker && _modeSelect.Selected == 0;
        bool fodinha = _selectedGameId == "fodinha";
        _modeSelect.SetItemDisabled(2, fodinha);
        if (fodinha) _modeSelect.Selected = 0;
        _teamField.Visible = _selectedGameId == "truco";
        if (_lanActions != null) _lanActions.Visible = !fodinha;
        if (_playButton != null) _playButton.Text = _modeSelect.Selected == 2 ? "Abrir sala LAN   →" : "Sentar à mesa   →";
    }

    private void ApplyGameSelection()
    {
        GameRegistry.IsTutorialMode = _modeSelect.Selected == 1 && _selectedGameId == "poker_roguelike";
        GameRegistry.SoloBotCount = _botSelect.Selected + 1;
        GameRegistry.SelectedSoloDifficulty = (GameRegistry.SoloDifficulty)_difficultySelect.Selected;
        GameRegistry.TrucoTeamSize = _teamSelect.Selected + 1;
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.RoomTheme = _selectedMapId;
            SettingsManager.Instance.SaveSettings();
        }
    }

    private void LaunchGame()
    {
        ApplyGameSelection();
        if (_modeSelect.Selected == 2) { JoinSelectedLobby(); return; }
        if (GameRegistry.Instance.AvailableGames.TryGetValue(_selectedGameId, out var definition))
            GetTree().ChangeSceneToFile(definition.MainScenePath);
    }

    private void HostSelectedLobby()
    {
        ApplyGameSelection();
        GameRegistry.IsTutorialMode = false;
        _isHosting = true;
        bool truco = _selectedGameId == "truco";
        LobbyManager.Instance?.HostLobby(_selectedGameId, truco ? GameRegistry.TrucoTeamSize * 2 : 6, LobbyState.TurnMode.Sequential, truco ? 12 : 300, "casino");
        ShowMenu(HubState.Lobby);
    }

    private void JoinSelectedLobby()
    {
        ApplyGameSelection();
        GameRegistry.IsTutorialMode = false;
        _isHosting = false;
        ShowMenu(HubState.Lobby);
    }

    private static Control FramePage(Control content)
    {
        var root = new MarginContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("margin_left", 60);
        root.AddThemeConstantOverride("margin_right", 60);
        root.AddThemeConstantOverride("margin_top", 18);
        root.AddThemeConstantOverride("margin_bottom", 18);
        var panel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Panel, ClubTheme.Border, 0, 12));
        root.AddChild(panel);
        content.Visible = true;
        panel.AddChild(content);
        return root;
    }

    private static Button GameTile(string suit, string title, string subtitle, string number)
    {
        var button = ClubTheme.Button("");
        button.CustomMinimumSize = new Vector2(162, 106);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.TooltipText = title;
        var box = Column(5);
        box.MouseFilter = MouseFilterEnum.Ignore;
        box.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        box.OffsetLeft = 15; box.OffsetTop = 12; box.OffsetRight = -10; box.OffsetBottom = -8;
        box.AddChild(ClubTheme.Label(suit + "  " + title, 21));
        box.AddChild(Eyebrow(subtitle, 10));
        box.AddChild(ClubTheme.Label(number, 11, TextSecondary));
        button.AddChild(box);
        return button;
    }
    private static VBoxContainer Column(int gap) { var c = new VBoxContainer(); c.AddThemeConstantOverride("separation", gap); return c; }
    private static HBoxContainer Row(int gap) { var r = new HBoxContainer(); r.AddThemeConstantOverride("separation", gap); return r; }
    private static Control Expand(bool vertical = false) => new Control { SizeFlagsHorizontal = vertical ? SizeFlags.Fill : SizeFlags.ExpandFill, SizeFlagsVertical = vertical ? SizeFlags.ExpandFill : SizeFlags.Fill, MouseFilter = MouseFilterEnum.Ignore };
    private static Control Rule() => new ColorRect { Color = ClubTheme.Border, CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore };
    private static Button Action(string text, System.Action action, bool primary = false) { var b = ClubTheme.Button(text, primary); b.Pressed += action; return b; }
    private static Label Display(string text, int size) { var l = ClubTheme.Label(text, size); l.AddThemeFontOverride("font", ClubTheme.DisplayFont); return l; }
    private static Label Eyebrow(string text, int size = 12) { var l = ClubTheme.Label(text, size, Gold); l.AddThemeFontOverride("font", ClubTheme.MonoFont); return l; }
    private static Label Paragraph(string text, int size) { var l = ClubTheme.Label(text, size, TextSecondary); l.AutowrapMode = TextServer.AutowrapMode.WordSmart; l.SizeFlagsHorizontal = SizeFlags.ExpandFill; return l; }
    private static OptionButton Select(params string[] options) { var s = new OptionButton { CustomMinimumSize = new Vector2(140, 40), SizeFlagsHorizontal = SizeFlags.ExpandFill }; foreach (string option in options) s.AddItem(option); return s; }
    private static Control Field(string label, Control input) { var c = Column(6); c.SizeFlagsHorizontal = SizeFlags.ExpandFill; c.AddChild(Eyebrow(label, 10)); c.AddChild(input); return c; }
}
