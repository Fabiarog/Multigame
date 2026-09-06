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
    private Button _pokerTile, _trucoTile, _fodinhaTile, _playButton;
    private Control _lanActions;
    private Label _gameDescription;

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
        foreach (var page in new[] { _mainMenuPanel, _settingsPanel, _lobbyPanel, _collectiblesPanel, _accessibilityPanel }) AddChild(page);
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
        root.AddThemeConstantOverride("margin_top", 28);
        root.AddThemeConstantOverride("margin_bottom", 28);
        var column = Column(18);
        root.AddChild(column);
        var header = Row(10);
        header.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/ui/club-mark.svg"),
            CustomMinimumSize = new Vector2(42, 42), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        var brand = Column(0);
        brand.AddChild(Display("MultiGame", 29));
        brand.AddChild(Eyebrow("C L U B E   D E   C A R T A S", 10));
        header.AddChild(brand);
        header.AddChild(Expand());
        header.AddChild(Action("Coleção", () => ShowMenu(HubState.Collectibles)));
        header.AddChild(Action("Ajustes", () => ShowMenu(HubState.Settings)));
        header.AddChild(Action("Acessibilidade", () => ShowMenu(HubState.Accessibility)));
        header.AddChild(Action("Sair", () => GetTree().Quit()));
        column.AddChild(header);
        column.AddChild(Rule());
        var body = Row(38);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddChild(body);
        var left = Column(13);
        left.CustomMinimumSize = new Vector2(530, 0);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(left);
        left.AddChild(Eyebrow("ENTRE. A MESA É SUA."));
        var title = Display("A sorte é só\no começo.", 66);
        title.AddThemeConstantOverride("line_spacing", -12);
        left.AddChild(title);
        left.AddChild(Paragraph("Boas mãos. Grandes blefes. Mais uma rodada.\nEscolha seu jogo e desafie a mesa.", 16));
        var choices = Row(10);
        left.AddChild(choices);
        _pokerTile = GameTile("♠", "Pôquer", "ROGUELIKE", "01");
        _trucoTile = GameTile("♣", "Truco", "BLEFE & MANILHA", "02");
        _fodinhaTile = GameTile("♦", "Fodinha", "PALPITES & VIDAS", "03");
        choices.AddChild(_pokerTile); choices.AddChild(_trucoTile); choices.AddChild(_fodinhaTile);
        _fodinhaTile.Pressed += () => SelectGame("fodinha");
        _pokerTile.Pressed += () => SelectGame("poker_roguelike");
        _trucoTile.Pressed += () => SelectGame("truco");
        var options = Row(10);
        left.AddChild(options);
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
        left.AddChild(Expand(true));
        _playButton = Action("Sentar à mesa   →", LaunchGame, true);
        _playButton.CustomMinimumSize = new Vector2(0, 52);
        left.AddChild(_playButton);

        var right = Column(12);
        right.CustomMinimumSize = new Vector2(520, 0);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.AddChild(right);
        var frame = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
        frame.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Border, 1, 8));
        frame.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/ui/club-table.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore });
        right.AddChild(frame);
        _gameDescription = Paragraph("", 15);
        _gameDescription.CustomMinimumSize = new Vector2(0, 45);
        right.AddChild(_gameDescription);
        var lan = Row(10);
        _lanActions = lan;
        lan.AddChild(Action("Criar sala LAN", HostSelectedLobby));
        lan.AddChild(Action("Entrar em sala", JoinSelectedLobby));
        foreach (Control control in lan.GetChildren()) control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        right.AddChild(lan);
        column.AddChild(Rule());
        var footer = Row(10);
        footer.AddChild(Eyebrow("PÔQUER  /  TRUCO  /  ROGUELIKE", 11));
        footer.AddChild(Expand());
        footer.AddChild(Eyebrow("EDIÇÃO CLUBE  /  0.2", 11));
        column.AddChild(footer);
        SelectGame("poker_roguelike");
        return root;
    }

    private void SelectGame(string id)
    {
        _selectedGameId = id;
        bool poker = id == "poker_roguelike";
        _pokerTile.AddThemeStyleboxOverride("normal", ClubTheme.Box(poker ? ClubTheme.Green : ClubTheme.Panel, poker ? Gold : ClubTheme.Border, 12, 6));
        _trucoTile.AddThemeStyleboxOverride("normal", ClubTheme.Box(id == "truco" ? ClubTheme.Green : ClubTheme.Panel, id == "truco" ? Gold : ClubTheme.Border, 12, 6));
        _fodinhaTile.AddThemeStyleboxOverride("normal", ClubTheme.Box(id == "fodinha" ? ClubTheme.Green : ClubTheme.Panel, id == "fodinha" ? Gold : ClubTheme.Border, 12, 6));
        _gameDescription.Text = poker ? "01 / PÔQUER ROGUELIKE\nCombine cartas, supere metas e fortaleça sua próxima mão." : "02 / TRUCO\nCorte o baralho, descubra a manilha e sustente seu blefe.";
        if (id == "fodinha") _gameDescription.Text = "03 / FODINHA · SOLO COM 3 IAs\nCinco vidas. Dê seu palpite e ganhe exatamente o que prometeu.";
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
        root.AddThemeConstantOverride("margin_left", 120);
        root.AddThemeConstantOverride("margin_right", 120);
        root.AddThemeConstantOverride("margin_top", 36);
        root.AddThemeConstantOverride("margin_bottom", 36);
        var panel = new PanelContainer();
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
