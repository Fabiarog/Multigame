using Godot;
using GameHub.Core.Visuals;
using GameHub.Core.Registry;
using GameHub.Core.Networking;
using GameHub.Core.Systems;
using GameHub.Core.Graphics;

namespace GameHub.Hub.Scripts;

public enum HubState
{
    MainMenu,
    Settings,
    Lobby,
    Collectibles,
    Accessibility,
    Tutorial
}

/// <summary>
/// Controller for the Main Hub Scene.
/// Builds and manages all Hub UI screens programmatically:
/// Main Menu, Settings, and Lobby.
/// </summary>
public partial class HubMain : Control
{
    private HubState _currentState = HubState.MainMenu;

    // Panels
    private Control _mainMenuPanel;
    private Control _settingsPanel;
    private Control _lobbyPanel;
    private Control _collectiblesPanel;
    private Control _accessibilityPanel;

    // Settings controls
    private LineEdit _nicknameEdit;
    private OptionButton _baseSelect;
    private OptionButton _shirtSelect;
    private OptionButton _pantsSelect;
    private OptionButton _hairSelect;
    private OptionButton _characterSelect;
    private OptionButton _musicTrackSelect;
    private HSlider _masterSlider;
    private HSlider _musicSlider;
    private HSlider _sfxSlider;
    private CheckButton _fullscreenToggle;
    private Label _masterValueLabel;
    private Label _musicValueLabel;
    private Label _sfxValueLabel;
    private CheckButton _screenShakeToggle;
    private CheckButton _reduceMotionToggle;
    private OptionButton _colorblindSelect;
    private OptionButton _colorblindScopeSelect;

    // Video controls (Módulo 1)
    private OptionButton _resolutionSelect;
    private OptionButton _displayModeSelect;
    private HSlider _renderScaleSlider;
    private Label _renderScaleValueLabel;
    private CheckButton _vsyncToggle;

    // Graphics / Ray Tracing controls (Módulo 2)
    private CheckButton _rtMasterToggle;
    private CheckButton _rtaoToggle;
    private OptionButton _rtaoQualitySelect;
    private CheckButton _rtReflectionsToggle;
    private OptionButton _rtReflectionsQualitySelect;
    private CheckButton _rtgiToggle;
    private OptionButton _rtgiQualitySelect;
    private Label _gpuInfoLabel;
    private Label _rtWarningLabel;

    // Lobby controls
    private VBoxContainer _playerListBox;
    private Button _readyBtn;
    private Button _startBtn;
    private LineEdit _ipEdit;
    private Label _lobbyStatusLabel;
    private HBoxContainer _ipRow;
    private OptionButton _teamAssignmentSelect;
    private bool _isHosting = false;
    private string _selectedGameId = "poker_roguelike";

    private static readonly Color PanelBg = ClubTheme.Panel;
    private static readonly Color Gold = ClubTheme.Gold;
    private static readonly Color Accent = ClubTheme.Gold;
    private static readonly Color BtnPurple = ClubTheme.Panel;
    private static readonly Color BtnPurpleHover = ClubTheme.Green;
    private static readonly Color BtnGreen = ClubTheme.Green;
    private static readonly Color BtnGreenHover = ClubTheme.Green.Lightened(.12f);
    private static readonly Color BtnRed = ClubTheme.Red.Darkened(.25f);
    private static readonly Color BtnRedHover = ClubTheme.Red;
    private static readonly Color SuccessGreen = new("#9cc299");
    private static readonly Color TextPrimary = ClubTheme.Paper;
    private static readonly Color TextSecondary = ClubTheme.Muted;

    public override void _Ready()
    {
        BuildUI();
        ShowMenu(HubState.MainMenu);
        GD.Print("[HubMain] Hub initialized successfully.");
    }

    public override void _ExitTree()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LobbyUpdated -= OnLobbyUpdated;
            LobbyManager.Instance.MatchStarting -= OnMatchStarting;
        }
    }

    // ===== UI CONSTRUCTION =====

    // ==================== SETTINGS ====================

    private Control BuildSettingsPanel()
    {
        var margin = new MarginContainer { Name = "SettingsPanel" };
        foreach (var edge in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, 28);
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 16);
        margin.AddChild(page);
        var heading = ClubTheme.Label("Seu lugar à mesa", 40);
        heading.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        page.AddChild(heading);
        page.AddChild(ClubTheme.Label("Ajuste seu perfil, o som e a janela do jogo.", 16, ClubTheme.Muted));

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 20);
        page.AddChild(columns);
        var profilePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        profilePanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Border, 18));
        columns.AddChild(profilePanel);
        var profile = new VBoxContainer();
        profile.AddThemeConstantOverride("separation", 10);
        profilePanel.AddChild(profile);
        profile.AddChild(ClubTheme.Label("PERFIL", 13, ClubTheme.Gold));
        profile.AddChild(ClubTheme.Label("Apelido", 15));
        _nicknameEdit = new LineEdit
        {
            Text = "Jogador", PlaceholderText = "Como você quer ser chamado?",
            CustomMinimumSize = new Vector2(0, 40), SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        profile.AddChild(_nicknameEdit);
        profile.AddChild(CreateFixedSpacer(2));
        // Preserve the metadata used by settings saves and future wardrobe items.
        var legacy = new VBoxContainer { Visible = false };
        legacy.AddChild(CreateAvatarSelectorRow("Corpo", out _baseSelect, "Padrão", "default_base"));
        legacy.AddChild(CreateAvatarSelectorRow("Camisa", out _shirtSelect, "Padrão", "default_shirt"));
        legacy.AddChild(CreateAvatarSelectorRow("Calça", out _pantsSelect, "Padrão", "default_pants"));
        legacy.AddChild(CreateAvatarSelectorRow("Cabelo", out _hairSelect, "Padrão", "default_hair"));
        profile.AddChild(legacy);
        profile.AddChild(ClubTheme.Label("SEU PERSONAGEM", 13, Gold));
        _characterSelect = new OptionButton { CustomMinimumSize = new Vector2(0, 40) };
        foreach (var name in CharacterCatalog.Names) _characterSelect.AddItem(name);
        profile.AddChild(_characterSelect);
        var portrait = new TextureRect { Texture = CharacterCatalog.Portrait(0), CustomMinimumSize = new Vector2(0, 120),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        profile.AddChild(portrait);
        var characterNote = ClubTheme.Label(CharacterCatalog.Descriptions[0], 13, TextSecondary);
        characterNote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        profile.AddChild(characterNote);
        _characterSelect.ItemSelected += index => { portrait.Texture = CharacterCatalog.Portrait((int)index); characterNote.Text = CharacterCatalog.Descriptions[(int)index]; };
        var wardrobeNote = ClubTheme.Label("Personagens visuais · sem bônus de jogabilidade.", 13, ClubTheme.Muted);
        wardrobeNote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        profile.AddChild(wardrobeNote);

        var devicePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        devicePanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Border, 18));
        columns.AddChild(devicePanel);
        var deviceScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        deviceScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        devicePanel.AddChild(deviceScroll);
        var device = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        device.AddThemeConstantOverride("separation", 16);
        deviceScroll.AddChild(device);
        device.AddChild(ClubTheme.Label("SOM DA MESA", 13, ClubTheme.Gold));
        device.AddChild(CreateSliderRow("Geral", 0, 100, 100, out _masterSlider, out _masterValueLabel));
        device.AddChild(CreateSliderRow("Música", 0, 100, 80, out _musicSlider, out _musicValueLabel));
        device.AddChild(CreateSliderRow("Efeitos", 0, 100, 100, out _sfxSlider, out _sfxValueLabel));
        _musicTrackSelect = new OptionButton { CustomMinimumSize = new Vector2(0, 40) };
        _musicTrackSelect.AddItem("Trilha da mesa (automática)");
        foreach (var name in AudioManager.TrackNames) _musicTrackSelect.AddItem(name);
        device.AddChild(_musicTrackSelect);
        device.AddChild(CreateFixedSpacer(4));

        // ── VÍDEO (Módulo 1) ──────────────────────────────────────
        device.AddChild(ClubTheme.Label("VÍDEO", 13, ClubTheme.Gold));

        // Legacy toggle (kept for backward compat; hidden if new manager is available)
        _fullscreenToggle = new CheckButton { Text = "Tela cheia", ButtonPressed = true, CustomMinimumSize = new Vector2(0, 42), Visible = false };
        device.AddChild(_fullscreenToggle);

        // Resolution selector
        device.AddChild(ClubTheme.Label("Resolução", 15));
        _resolutionSelect = new OptionButton { CustomMinimumSize = new Vector2(0, 40), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        device.AddChild(_resolutionSelect);

        // Display mode selector
        device.AddChild(ClubTheme.Label("Modo de exibição", 15));
        _displayModeSelect = new OptionButton { CustomMinimumSize = new Vector2(0, 40), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _displayModeSelect.AddItem("Janela");
        _displayModeSelect.AddItem("Tela cheia exclusiva");
        _displayModeSelect.AddItem("Tela cheia sem bordas");
        device.AddChild(_displayModeSelect);

        // Render scale slider
        device.AddChild(CreateSliderRow("Escala de render.", 50, 100, 100, out _renderScaleSlider, out _renderScaleValueLabel));

        // VSync toggle
        _vsyncToggle = new CheckButton { Text = "VSync", ButtonPressed = true, CustomMinimumSize = new Vector2(0, 42) };
        device.AddChild(_vsyncToggle);

        device.AddChild(CreateFixedSpacer(6));

        // ── GRÁFICOS / RAY TRACING (Módulo 2) ────────────────────
        device.AddChild(ClubTheme.Label("GRÁFICOS", 13, ClubTheme.Gold));

        // GPU info
        _gpuInfoLabel = ClubTheme.Label("GPU: detectando…", 13, ClubTheme.Muted);
        _gpuInfoLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        device.AddChild(_gpuInfoLabel);

        // RT warning (hidden by default)
        _rtWarningLabel = ClubTheme.Label("", 13, ClubTheme.Red);
        _rtWarningLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _rtWarningLabel.Visible = false;
        device.AddChild(_rtWarningLabel);

        // RT master toggle
        _rtMasterToggle = new CheckButton { Text = "Ray Tracing", ButtonPressed = false, CustomMinimumSize = new Vector2(0, 42) };
        _rtMasterToggle.Toggled += (pressed) => UpdateRtSubToggles(pressed);
        device.AddChild(_rtMasterToggle);

        // RTAO
        var rtaoRow = new HBoxContainer();
        rtaoRow.AddThemeConstantOverride("separation", 8);
        _rtaoToggle = new CheckButton { Text = "Oclusão ambiente (AO)", ButtonPressed = false, CustomMinimumSize = new Vector2(0, 38), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rtaoRow.AddChild(_rtaoToggle);
        _rtaoQualitySelect = CreateQualitySelector();
        rtaoRow.AddChild(_rtaoQualitySelect);
        device.AddChild(rtaoRow);

        // RT Reflections
        var reflRow = new HBoxContainer();
        reflRow.AddThemeConstantOverride("separation", 8);
        _rtReflectionsToggle = new CheckButton { Text = "Reflexos", ButtonPressed = false, CustomMinimumSize = new Vector2(0, 38), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        reflRow.AddChild(_rtReflectionsToggle);
        _rtReflectionsQualitySelect = CreateQualitySelector();
        reflRow.AddChild(_rtReflectionsQualitySelect);
        device.AddChild(reflRow);

        // RTGI
        var giRow = new HBoxContainer();
        giRow.AddThemeConstantOverride("separation", 8);
        _rtgiToggle = new CheckButton { Text = "Iluminação global (GI)", ButtonPressed = false, CustomMinimumSize = new Vector2(0, 38), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        giRow.AddChild(_rtgiToggle);
        _rtgiQualitySelect = CreateQualitySelector();
        giRow.AddChild(_rtgiQualitySelect);
        device.AddChild(giRow);

        device.AddChild(CreateFixedSpacer(4));

        var accessNote = ClubTheme.Label("Filtros de cor e redução de movimento ficam no menu Acessibilidade.", 14, ClubTheme.Muted);
        accessNote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        device.AddChild(accessNote);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);
        var feedback = ClubTheme.Label("As alterações serão aplicadas ao salvar.", 14, ClubTheme.Muted);
        feedback.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        feedback.VerticalAlignment = VerticalAlignment.Center;
        actions.AddChild(feedback);
        var back = ClubTheme.Button("Voltar");
        back.CustomMinimumSize = new Vector2(120, 44);
        back.Pressed += () => ShowMenu(HubState.MainMenu);
        actions.AddChild(back);
        var save = ClubTheme.Button("Salvar alterações", true);
        save.CustomMinimumSize = new Vector2(180, 44);
        save.Pressed += () =>
        {
            SaveSettings();
            feedback.Text = "Configurações salvas.";
        };
        actions.AddChild(save);
        page.AddChild(actions);
        return margin;
    }

    // ==================== LOBBY ====================

    private Control BuildLobbyPanel()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 80);
        margin.AddThemeConstantOverride("margin_right", 80);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        margin.Name = "LobbyPanel";

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        // Title
        var title = CreateLabel("Sala", 14, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);
        var lanNote = ClubTheme.Label("LAN em desenvolvimento: as partidas ainda não são sincronizadas.", 14, TextSecondary);
        lanNote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        lanNote.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(lanNote);

        // Status
        _lobbyStatusLabel = CreateLabel("Aguardando...", 8, TextSecondary);
        _lobbyStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_lobbyStatusLabel);

        vbox.AddChild(CreateFixedSpacer(4));

        // IP input row (for joining)
        _ipRow = new HBoxContainer();
        _ipRow.Alignment = BoxContainer.AlignmentMode.Center;
        _ipRow.AddThemeConstantOverride("separation", 16);

        var ipLabel = CreateLabel("IP:", 8, TextPrimary);
        _ipRow.AddChild(ipLabel);

        _ipEdit = new LineEdit();
        _ipEdit.Text = "127.0.0.1";
        _ipEdit.CustomMinimumSize = new Vector2(200, 40);
        _ipEdit.AddThemeFontSizeOverride("font_size", 16);
        _ipRow.AddChild(_ipEdit);

        var connectBtn = CreateStyledButton("Conectar", BtnGreen, BtnGreenHover, new Vector2(120, 40));
        connectBtn.AddThemeFontSizeOverride("font_size", 16);
        connectBtn.Pressed += () =>
        {
            LobbyManager.Instance?.JoinLobby(_ipEdit.Text);
            _lobbyStatusLabel.Text = $"Conectando a {_ipEdit.Text}...";
        };
        _ipRow.AddChild(connectBtn);
        vbox.AddChild(_ipRow);

        var teamModeRow = new HBoxContainer();
        teamModeRow.Alignment = BoxContainer.AlignmentMode.Center;
        teamModeRow.AddThemeConstantOverride("separation", 16);
        teamModeRow.AddChild(CreateLabel("Equipes:", 8, TextPrimary));
        _teamAssignmentSelect = new OptionButton();
        _teamAssignmentSelect.AddItem("Sortear", (int)LobbyState.TeamAssignmentMode.Random);
        _teamAssignmentSelect.AddItem("Escolher", (int)LobbyState.TeamAssignmentMode.HostChooses);
        _teamAssignmentSelect.CustomMinimumSize = new Vector2(180, 40);
        _teamAssignmentSelect.ItemSelected += index =>
        {
            if (_isHosting)
                LobbyManager.Instance?.SetTeamAssignment((LobbyState.TeamAssignmentMode)(int)_teamAssignmentSelect.GetItemId((int)index));
        };
        teamModeRow.AddChild(_teamAssignmentSelect);
        vbox.AddChild(teamModeRow);

        // Player list
        vbox.AddChild(CreateSectionLabel("Jogadores"));

        var playerScroll = new ScrollContainer();
        playerScroll.CustomMinimumSize = new Vector2(0, 200);
        playerScroll.SizeFlagsVertical = SizeFlags.ExpandFill;

        // Player list panel background
        var playerPanel = new PanelContainer();
        playerPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        playerPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        var playerPanelStyle = new StyleBoxFlat();
        playerPanelStyle.BgColor = ClubTheme.Ink;
        playerPanelStyle.SetCornerRadiusAll(4);
        playerPanelStyle.ContentMarginLeft = 8;
        playerPanelStyle.ContentMarginRight = 8;
        playerPanelStyle.ContentMarginTop = 6;
        playerPanelStyle.ContentMarginBottom = 6;
        playerPanel.AddThemeStyleboxOverride("panel", playerPanelStyle);

        _playerListBox = new VBoxContainer();
        _playerListBox.AddThemeConstantOverride("separation", 8);
        _playerListBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        playerPanel.AddChild(_playerListBox);
        playerScroll.AddChild(playerPanel);
        vbox.AddChild(playerScroll);

        // Action buttons
        var actionRow = new HBoxContainer();
        actionRow.Alignment = BoxContainer.AlignmentMode.Center;
        actionRow.AddThemeConstantOverride("separation", 20);

        _readyBtn = CreateStyledButton("Pronto", BtnGreen, BtnGreenHover, new Vector2(120, 40));
        _readyBtn.Pressed += () => LobbyManager.Instance?.ToggleReady();
        actionRow.AddChild(_readyBtn);

        _startBtn = CreateStyledButton("Iniciar", BtnPurple, BtnPurpleHover, new Vector2(120, 40));
        _startBtn.Pressed += () => LobbyManager.Instance?.TryStartMatch();
        actionRow.AddChild(_startBtn);

        var lobbyBackBtn = CreateStyledButton("Voltar", BtnRed, BtnRedHover, new Vector2(120, 40));
        lobbyBackBtn.Pressed += () =>
        {
            LobbyManager.Instance?.LeaveLobby();
            ShowMenu(HubState.MainMenu);
        };
        actionRow.AddChild(lobbyBackBtn);

        vbox.AddChild(actionRow);

        return margin;
    }

    private Control BuildCollectiblesPanel()
    {
        var margin = new MarginContainer();
        foreach (var edge in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, 28);
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 16);
        margin.AddChild(page);
        page.AddChild(ClubTheme.Label("ACERVO DO CLUBE", 12, ClubTheme.Gold));
        var title = ClubTheme.Label("Peças da casa", 40);
        title.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        page.AddChild(title);
        page.AddChild(ClubTheme.Label("Uma amostra das cartas e ilustrações que dão vida às mesas.", 16, ClubTheme.Muted));

        var gallery = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        gallery.AddThemeConstantOverride("separation", 18);
        page.AddChild(gallery);
        string[] titles = { "Papel & ouro", "Rostos da mesa", "Noites de neon" };
        string[] labels = { "CARTAS EM USO", "ILUSTRAÇÃO DO ACERVO", "CENÁRIO DO ACERVO" };
        string[] descriptions =
        {
            "Faces legíveis e versos de feltro. O novo baralho acompanha as mesas de Pôquer e Truco.",
            "O retrato do Truco faz parte das ilustrações existentes, junto dos sprites de personagens e chefes.",
            "O salão neon permanece no acervo para futuras variações de ambiente e mesas temáticas."
        };
        for (int i = 0; i < titles.Length; i++)
        {
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            card.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Border, 18));
            gallery.AddChild(card);
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 12);
            card.AddChild(box);
            var artwork = new CenterContainer { CustomMinimumSize = new Vector2(0, 176) };
            if (i == 0)
            {
                var cards = new HBoxContainer();
                cards.AddThemeConstantOverride("separation", 12);
                cards.AddChild(new PlayingCard { RankText = "A", SuitSymbol = "♠", CustomMinimumSize = new Vector2(82, 118) });
                cards.AddChild(new PlayingCard { FaceDown = true, CustomMinimumSize = new Vector2(82, 118) });
                artwork.AddChild(cards);
            }
            else
            {
                var picture = new TextureRect
                {
                    Texture = ResourceLoader.Load<Texture2D>(i == 1 ? "res://assets/sprites/portraits/truco_player.jpg" : "res://assets/sprites/backgrounds/neon_lounge.jpg"),
                    CustomMinimumSize = new Vector2(244, 176),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                artwork.AddChild(picture);
            }
            box.AddChild(artwork);
            box.AddChild(ClubTheme.Label(labels[i], 11, ClubTheme.Gold));
            box.AddChild(ClubTheme.Label(titles[i], 23));
            var description = ClubTheme.Label(descriptions[i], 14, ClubTheme.Muted);
            description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            description.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            box.AddChild(description);
        }
        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 20);
        var note = ClubTheme.Label("Este é o acervo atual. Coleções desbloqueáveis e recompensas persistentes estão previstas para uma próxima etapa.", 14, ClubTheme.Muted);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        note.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        note.VerticalAlignment = VerticalAlignment.Center;
        footer.AddChild(note);
        var back = ClubTheme.Button("Voltar ao clube");
        back.CustomMinimumSize = new Vector2(176, 44);
        back.Pressed += () => ShowMenu(HubState.MainMenu);
        footer.AddChild(back);
        page.AddChild(footer);
        return margin;
    }

    private Control BuildAccessibilityPanel()
    {
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(600, 420);
        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.SetCornerRadiusAll(8);
        style.SetBorderWidthAll(2);
        style.BorderColor = Gold;
        style.ContentMarginLeft = 20;
        style.ContentMarginRight = 20;
        style.ContentMarginTop = 16;
        style.ContentMarginBottom = 16;
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 20);
        panel.AddChild(box);
        var title = CreateLabel("Acessibilidade", 14, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);

        _screenShakeToggle = new CheckButton { Text = "Reduzir tremores de tela" };
        _screenShakeToggle.AddThemeFontSizeOverride("font_size", 16);
        box.AddChild(_screenShakeToggle);
        _reduceMotionToggle = new CheckButton { Text = "Reduzir animações" };
        _reduceMotionToggle.AddThemeFontSizeOverride("font_size", 16);
        box.AddChild(_reduceMotionToggle);

        var colorRow = new HBoxContainer();
        colorRow.AddChild(CreateLabel("Cores:", 8, TextPrimary));
        _colorblindSelect = new OptionButton();
        _colorblindSelect.AddItem("Padrão", 0);
        _colorblindSelect.AddItem("Protanopia", 1);
        _colorblindSelect.AddItem("Deuteranopia", 2);
        _colorblindSelect.AddItem("Tritanopia", 3);
        _colorblindSelect.CustomMinimumSize = new Vector2(220, 40);
        colorRow.AddChild(_colorblindSelect);
        box.AddChild(colorRow);

        var scopeRow = new HBoxContainer();
        scopeRow.AddChild(CreateLabel("Aplicar em:", 8, TextPrimary));
        _colorblindScopeSelect = new OptionButton();
        _colorblindScopeSelect.AddItem("Somente cartas", 1);
        _colorblindScopeSelect.AddItem("Tela inteira", 0);
        _colorblindScopeSelect.CustomMinimumSize = new Vector2(220, 40);
        scopeRow.AddChild(_colorblindScopeSelect);
        box.AddChild(scopeRow);
        box.AddChild(CreateLabel("As opções são salvas para todas as partidas.", 7, TextSecondary));
        box.AddChild(CreateFixedSpacer(8));

        var buttons = new HBoxContainer();
        buttons.Alignment = BoxContainer.AlignmentMode.Center;
        var save = CreateStyledButton("Salvar", BtnGreen, BtnGreenHover, new Vector2(140, 40));
        save.Pressed += SaveAccessibilitySettings;
        buttons.AddChild(save);
        var back = CreateStyledButton("Voltar", BtnPurple, BtnPurpleHover, new Vector2(140, 40));
        back.Pressed += () => ShowMenu(HubState.MainMenu);
        buttons.AddChild(back);
        box.AddChild(buttons);
        return center;
    }

    // ==================== STATE MANAGEMENT ====================

    private void ShowMenu(HubState state)
    {
        _currentState = state;
        _mainMenuPanel.Visible = state == HubState.MainMenu;
        _settingsPanel.Visible = state == HubState.Settings;
        _lobbyPanel.Visible = state == HubState.Lobby;
        _collectiblesPanel.Visible = state == HubState.Collectibles;
        _accessibilityPanel.Visible = state == HubState.Accessibility;
        var active = state switch
        {
            HubState.Settings => _settingsPanel, HubState.Lobby => _lobbyPanel,
            HubState.Collectibles => _collectiblesPanel, HubState.Accessibility => _accessibilityPanel,
            _ => _mainMenuPanel
        };
        if (SettingsManager.Instance?.ReduceMotion != true)
        {
            active.Modulate = new Color(1, 1, 1, 0);
            CreateTween().TweenProperty(active, "modulate:a", 1f, .18f);
        }

        if (state == HubState.Settings)
        {
            LoadSettingsToUI();
        }
        if (state == HubState.Lobby)
        {
            RefreshLobbyUI();
            _ipRow.Visible = !_isHosting;
            if (_teamAssignmentSelect.GetParent() is Control teamConfig)
                teamConfig.Visible = LobbyManager.Instance?.CurrentLobby?.SelectedGameId == "truco";
            _startBtn.Visible = _isHosting;
            _lobbyStatusLabel.Text = _isHosting ? "Sala criada · aguardando jogadores..." : "Digite o IP do anfitrião e conecte";
        }
        if (state == HubState.Accessibility)
        {
            LoadAccessibilitySettingsToUI();
        }

        GD.Print($"[HubMain] Switching UI to: {state}");
    }

    // ==================== SETTINGS LOGIC ====================

    private void LoadSettingsToUI()
    {
        if (SettingsManager.Instance == null) return;
        _nicknameEdit.Text = SettingsManager.Instance.PlayerNickname;
        _characterSelect.Select(CharacterCatalog.Find(SettingsManager.Instance.CharacterId));
        _characterSelect.EmitSignal(OptionButton.SignalName.ItemSelected, _characterSelect.Selected);
        _musicTrackSelect.Select(Mathf.Clamp(SettingsManager.Instance.MusicTrack, 0, 3));
        
        SelectAvatarItem(_baseSelect, SettingsManager.Instance.AvatarBase);
        SelectAvatarItem(_shirtSelect, SettingsManager.Instance.AvatarShirt);
        SelectAvatarItem(_pantsSelect, SettingsManager.Instance.AvatarPants);
        SelectAvatarItem(_hairSelect, SettingsManager.Instance.AvatarHair);

        _masterSlider.Value = SettingsManager.Instance.MasterVolume * 100;
        _musicSlider.Value = SettingsManager.Instance.MusicVolume * 100;
        _sfxSlider.Value = SettingsManager.Instance.SfxVolume * 100;
        _fullscreenToggle.ButtonPressed = SettingsManager.Instance.IsFullscreen;

        LoadVideoSettingsToUI();
        LoadGraphicsSettingsToUI();
    }

    private void SaveSettings()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.PlayerNickname = string.IsNullOrWhiteSpace(_nicknameEdit.Text) ? "Jogador" : _nicknameEdit.Text.Trim();
        SettingsManager.Instance.CharacterId = CharacterCatalog.Ids[Mathf.Clamp(_characterSelect.Selected, 0, 3)];
        SettingsManager.Instance.MusicTrack = _musicTrackSelect.Selected;
        
        SettingsManager.Instance.AvatarBase = GetSelectedAvatarId(_baseSelect, "default_base");
        SettingsManager.Instance.AvatarShirt = GetSelectedAvatarId(_shirtSelect, "default_shirt");
        SettingsManager.Instance.AvatarPants = GetSelectedAvatarId(_pantsSelect, "default_pants");
        SettingsManager.Instance.AvatarHair = GetSelectedAvatarId(_hairSelect, "default_hair");

        SettingsManager.Instance.MasterVolume = (float)_masterSlider.Value / 100f;
        SettingsManager.Instance.MusicVolume = (float)_musicSlider.Value / 100f;
        SettingsManager.Instance.SfxVolume = (float)_sfxSlider.Value / 100f;
        SettingsManager.Instance.IsFullscreen = _fullscreenToggle.ButtonPressed;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplySettings();

        SaveVideoSettings();
        SaveGraphicsSettings();

        GD.Print("[HubMain] Settings saved.");
    }

    private void LoadAccessibilitySettingsToUI()
    {
        if (SettingsManager.Instance == null) return;
        _screenShakeToggle.ButtonPressed = !SettingsManager.Instance.ScreenShakeEnabled;
        _reduceMotionToggle.ButtonPressed = SettingsManager.Instance.ReduceMotion;
        _colorblindSelect.Selected = SettingsManager.Instance.ColorblindMode;
        for (int i = 0; i < _colorblindScopeSelect.ItemCount; i++)
        {
            if (_colorblindScopeSelect.GetItemId(i) == SettingsManager.Instance.ColorblindScope)
                _colorblindScopeSelect.Selected = i;
        }
    }

    private void SaveAccessibilitySettings()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.ScreenShakeEnabled = !_screenShakeToggle.ButtonPressed;
        SettingsManager.Instance.ReduceMotion = _reduceMotionToggle.ButtonPressed;
        SettingsManager.Instance.ColorblindMode = _colorblindSelect.Selected;
        SettingsManager.Instance.ColorblindScope = _colorblindScopeSelect.GetItemId(_colorblindScopeSelect.Selected);
        SettingsManager.Instance.SaveSettings();
        GetTree().ReloadCurrentScene();
    }

    // ==================== LOBBY LOGIC ====================

    private void OnLobbyUpdated()
    {
        RefreshLobbyUI();
    }

    private void OnMatchStarting(string scenePath)
    {
        GD.Print($"[HubMain] Match starting: {scenePath}");
        GetTree().ChangeSceneToFile(scenePath);
    }

    private void RefreshLobbyUI()
    {
        if (LobbyManager.Instance?.CurrentLobby == null) return;

        // Clear player list
        while (_playerListBox.GetChildCount() > 0)
        {
            var child = _playerListBox.GetChild(0);
            _playerListBox.RemoveChild(child);
            child.QueueFree();
        }

        var lobby = LobbyManager.Instance.CurrentLobby;
        foreach (var kvp in lobby.PlayerSlots)
        {
            var slot = kvp.Value;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 16);

            var nameLabel = CreateLabel(slot.PlayerName, 8, TextPrimary);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(nameLabel);

            var statusLabel = CreateLabel(
                $"T{slot.Team} · {(slot.IsReady ? "✓ Pronto" : "Aguardando")}",
                8,
                slot.IsReady ? SuccessGreen : TextSecondary
            );
            row.AddChild(statusLabel);

            if (_isHosting && lobby.TeamAssignment == LobbyState.TeamAssignmentMode.HostChooses)
            {
                var teamButton = CreateStyledButton($"Equipe {slot.Team}", BtnPurple, BtnPurpleHover, new Vector2(124, 36));
                long playerId = slot.PeerId;
                int currentTeam = slot.Team;
                teamButton.Pressed += () => LobbyManager.Instance?.SetPlayerTeam(playerId, currentTeam == 1 ? 2 : 1);
                row.AddChild(teamButton);
            }

            _playerListBox.AddChild(row);
        }

        _lobbyStatusLabel.Text = $"Jogadores: {lobby.PlayerSlots.Count}/{lobby.MaxPlayers}";
        _startBtn.Visible = _isHosting;
    }

    // ==================== HELPERS ====================

    private HBoxContainer CreateAvatarSelectorRow(string labelText, out OptionButton selector, string optionText, string optionId)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        var label = CreateLabel(labelText, 8, TextPrimary);
        label.CustomMinimumSize = new Vector2(140, 0);
        row.AddChild(label);

        selector = new OptionButton();
        selector.AddItem(optionText);
        selector.SetItemMetadata(0, optionId);
        selector.CustomMinimumSize = new Vector2(240, 40);
        selector.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(selector);
        return row;
    }

    private static string GetSelectedAvatarId(OptionButton selector, string fallback)
    {
        if (selector.Selected < 0) return fallback;
        return selector.GetItemMetadata(selector.Selected).AsString();
    }

    private static void SelectAvatarItem(OptionButton selector, string id)
    {
        for (int i = 0; i < selector.ItemCount; i++)
        {
            if (selector.GetItemMetadata(i).AsString() == id)
            {
                selector.Selected = i;
                return;
            }
        }
        selector.Selected = 0;
    }

    private Label CreateLabel(string text, int fontSize, Color color)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", Mathf.Max(14, fontSize * 2));
        lbl.MouseFilter = MouseFilterEnum.Ignore;
        lbl.AddThemeColorOverride("font_color", color);
        return lbl;
    }

    private Label CreateSectionLabel(string text)
    {
        var lbl = CreateLabel(text, 8, Accent);
        lbl.AddThemeColorOverride("font_color", Accent);
        return lbl;
    }

    private Control CreateFixedSpacer(float height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        return spacer;
    }

    private HBoxContainer CreateSliderRow(string label, float min, float max, float value,
        out HSlider slider, out Label valueLabel)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        var lbl = CreateLabel(label, 8, TextPrimary);
        lbl.CustomMinimumSize = new Vector2(80, 0);
        row.AddChild(lbl);

        slider = new HSlider();
        slider.MinValue = min;
        slider.MaxValue = max;
        slider.Value = value;
        slider.Step = 1;
        slider.CustomMinimumSize = new Vector2(200, 24);
        slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(slider);

        valueLabel = CreateLabel($"{(int)value}%", 8, TextSecondary);
        valueLabel.CustomMinimumSize = new Vector2(50, 0);
        row.AddChild(valueLabel);

        // Live update label
        var vl = valueLabel;
        slider.ValueChanged += (double v) => vl.Text = $"{(int)v}%";

        return row;
    }

    private Button CreateStyledButton(string text, Color normalColor, Color hoverColor, Vector2 minSize)
    {
        var button = ClubTheme.Button(text, normalColor == BtnGreen);
        button.CustomMinimumSize = new Vector2(minSize.X, Mathf.Max(40, minSize.Y));
        return button;
    }

    // ==================== VIDEO / GRAPHICS HELPERS ====================

    /// <summary>
    /// Creates an OptionButton with Low/Medium/High/Ultra quality options.
    /// </summary>
    private static OptionButton CreateQualitySelector()
    {
        var select = new OptionButton { CustomMinimumSize = new Vector2(130, 38) };
        select.AddItem("Baixo");
        select.AddItem("Médio");
        select.AddItem("Alto");
        select.AddItem("Ultra");
        select.Selected = 1; // Medium default
        return select;
    }

    /// <summary>
    /// Enables/disables the RT sub-toggles based on the master toggle.
    /// </summary>
    private void UpdateRtSubToggles(bool masterEnabled)
    {
        if (_rtaoToggle != null) _rtaoToggle.Disabled = !masterEnabled;
        if (_rtaoQualitySelect != null) _rtaoQualitySelect.Disabled = !masterEnabled;
        if (_rtReflectionsToggle != null) _rtReflectionsToggle.Disabled = !masterEnabled;
        if (_rtReflectionsQualitySelect != null) _rtReflectionsQualitySelect.Disabled = !masterEnabled;
        if (_rtgiToggle != null) _rtgiToggle.Disabled = !masterEnabled;
        if (_rtgiQualitySelect != null) _rtgiQualitySelect.Disabled = !masterEnabled;
    }

    /// <summary>
    /// Populates the video UI controls from VideoSettingsManager.
    /// </summary>
    private void LoadVideoSettingsToUI()
    {
        var vsm = VideoSettingsManager.Instance;
        if (vsm == null) return;

        // Populate resolution options from available presets.
        _resolutionSelect.Clear();
        for (int i = 0; i < vsm.AvailablePresets.Count; i++)
            _resolutionSelect.AddItem(vsm.AvailablePresets[i].ToString());
        _resolutionSelect.Selected = Mathf.Clamp(vsm.ResolutionIndex, 0, vsm.AvailablePresets.Count - 1);

        _displayModeSelect.Selected = (int)vsm.CurrentDisplayMode;
        _renderScaleSlider.Value = vsm.RenderScale * 100f;
        _vsyncToggle.ButtonPressed = vsm.VSyncEnabled;
    }

    /// <summary>
    /// Writes the video UI state to VideoSettingsManager, applies, and saves.
    /// </summary>
    private void SaveVideoSettings()
    {
        var vsm = VideoSettingsManager.Instance;
        if (vsm == null) return;

        vsm.ResolutionIndex = _resolutionSelect.Selected;
        vsm.CurrentDisplayMode = (VideoSettingsManager.DisplayMode)_displayModeSelect.Selected;
        vsm.RenderScale = (float)_renderScaleSlider.Value / 100f;
        vsm.VSyncEnabled = _vsyncToggle.ButtonPressed;

        vsm.ApplyVideoSettings();
        vsm.SaveVideoSettings();
    }

    /// <summary>
    /// Populates the graphics/RT UI controls from GraphicsQualityManager.
    /// </summary>
    private void LoadGraphicsSettingsToUI()
    {
        // GPU info label
        if (HardwareCapabilityDetector.HasProbed)
        {
            _gpuInfoLabel.Text = $"GPU: {HardwareCapabilityDetector.GpuName}";
            if (!HardwareCapabilityDetector.SupportsRayTracing)
            {
                _rtWarningLabel.Text = "⚠ GPU sem suporte a Ray Tracing detectada. Efeitos RT indisponíveis.";
                _rtWarningLabel.Visible = true;
            }
            else
            {
                _rtWarningLabel.Visible = false;
            }
        }

        var gqm = GraphicsQualityManager.Instance;
        if (gqm == null) return;

        var rt = gqm.RtSettings;
        _rtMasterToggle.ButtonPressed = rt.RayTracingEnabled;
        _rtaoToggle.ButtonPressed = rt.RtaoEnabled;
        _rtaoQualitySelect.Selected = (int)rt.RtaoQuality;
        _rtReflectionsToggle.ButtonPressed = rt.RtReflectionsEnabled;
        _rtReflectionsQualitySelect.Selected = (int)rt.RtReflectionsQuality;
        _rtgiToggle.ButtonPressed = rt.RtgiEnabled;
        _rtgiQualitySelect.Selected = (int)rt.RtgiQuality;

        UpdateRtSubToggles(rt.RayTracingEnabled);

        // Disable the master toggle entirely if hardware doesn't support it.
        if (!HardwareCapabilityDetector.SupportsRayTracing)
        {
            _rtMasterToggle.Disabled = true;
            UpdateRtSubToggles(false);
        }
    }

    /// <summary>
    /// Writes the graphics/RT UI state to GraphicsQualityManager, applies, and saves.
    /// </summary>
    private void SaveGraphicsSettings()
    {
        var gqm = GraphicsQualityManager.Instance;
        if (gqm == null) return;

        var rt = gqm.RtSettings;
        rt.RayTracingEnabled = _rtMasterToggle.ButtonPressed;
        rt.RtaoEnabled = _rtaoToggle.ButtonPressed;
        rt.RtaoQuality = (RayTracingSettings.RtQualityLevel)_rtaoQualitySelect.Selected;
        rt.RtReflectionsEnabled = _rtReflectionsToggle.ButtonPressed;
        rt.RtReflectionsQuality = (RayTracingSettings.RtQualityLevel)_rtReflectionsQualitySelect.Selected;
        rt.RtgiEnabled = _rtgiToggle.ButtonPressed;
        rt.RtgiQuality = (RayTracingSettings.RtQualityLevel)_rtgiQualitySelect.Selected;

        gqm.ApplyGraphicsSettings();
        gqm.SaveGraphicsSettings();
    }
}
