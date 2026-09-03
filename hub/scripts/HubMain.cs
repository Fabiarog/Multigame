using Godot;
using GameHub.Core.Networking;
using GameHub.Core.Systems;

namespace GameHub.Hub.Scripts;

public enum HubState
{
    MainMenu,
    Settings,
    Lobby,
    Collectibles,
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

    // Settings controls
    private LineEdit _nicknameEdit;
    private OptionButton _avatarSelect;
    private HSlider _masterSlider;
    private HSlider _musicSlider;
    private HSlider _sfxSlider;
    private CheckButton _fullscreenToggle;
    private Label _masterValueLabel;
    private Label _musicValueLabel;
    private Label _sfxValueLabel;

    // Lobby controls
    private VBoxContainer _playerListBox;
    private Button _readyBtn;
    private Button _startBtn;
    private LineEdit _ipEdit;
    private Label _lobbyStatusLabel;
    private HBoxContainer _ipRow;
    private bool _isHosting = false;

    // ===== THEME COLORS =====
    private static readonly Color BgDark = new(0.05f, 0.05f, 0.09f);
    private static readonly Color PanelBg = new(0.09f, 0.09f, 0.15f);
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

    // ===== LIFECYCLE =====

    // Background elements
    private Sprite2D _bgSprite;
    private Sprite2D _neonSprite;
    private float _timePassed = 0f;
    private int _neonFrame = 0;
    private float _neonTimer = 0f;

    public override void _Ready()
    {
        BuildUI();
        ShowMenu(HubState.MainMenu);
        GD.Print("[HubMain] Hub initialized successfully.");
    }

    public override void _Process(double delta)
    {
        if (_bgSprite != null)
        {
            _timePassed += (float)delta;
            var center = GetViewportRect().Size / 2;
            _bgSprite.Position = new Vector2(center.X + Mathf.Sin(_timePassed * 0.2f) * 15f, center.Y + Mathf.Cos(_timePassed * 0.1f) * 10f);
        }

        if (_neonSprite != null)
        {
            _neonTimer += (float)delta;
            if (_neonTimer > 0.15f)
            {
                _neonTimer = 0f;
                _neonFrame = (_neonFrame + 1) % 4;
                _neonSprite.Frame = _neonFrame;
            }
        }
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

    private void BuildUI()
    {
        foreach (var child in GetChildren())
        {
            if (child is Node n) n.QueueFree();
        }

        // Animated Background
        var bgContainer = new Control();
        bgContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bgContainer);

        // Calculate scale to fill entire viewport
        var viewportSize = GetViewportRect().Size;
        var center = viewportSize / 2;

        _bgSprite = new Sprite2D();
        var bgTex = ResourceLoader.Load<Texture2D>("res://assets/sprites/backgrounds/cyber_casino/cyber_casino.jpg");
        _bgSprite.Texture = bgTex;
        _bgSprite.Position = center;
        if (bgTex != null)
        {
            float scaleX = viewportSize.X / bgTex.GetWidth();
            float scaleY = viewportSize.Y / bgTex.GetHeight();
            float bgScale = Mathf.Max(scaleX, scaleY) * 1.15f; // 15% extra to cover parallax movement
            _bgSprite.Scale = new Vector2(bgScale, bgScale);
        }
        bgContainer.AddChild(_bgSprite);

        _neonSprite = new Sprite2D();
        var neonTex = ResourceLoader.Load<Texture2D>("res://assets/sprites/backgrounds/cyber_casino/bg_anim_spritesheet.jpg");
        _neonSprite.Texture = neonTex;
        _neonSprite.Hframes = 2;
        _neonSprite.Vframes = 2;
        _neonSprite.Position = center;
        if (neonTex != null)
        {
            // Each frame is half the spritesheet in each dimension
            float frameW = neonTex.GetWidth() / 2f;
            float frameH = neonTex.GetHeight() / 2f;
            float scaleX = viewportSize.X / frameW;
            float scaleY = viewportSize.Y / frameH;
            float neonScale = Mathf.Max(scaleX, scaleY) * 1.1f;
            _neonSprite.Scale = new Vector2(neonScale, neonScale);
        }
        
        var shader = ResourceLoader.Load<Shader>("res://assets/shaders/CanvasChromaKey.gdshader");
        if (shader != null)
        {
            var mat = new ShaderMaterial();
            mat.Shader = shader;
            mat.SetShaderParameter("chroma_color", new Color(0.0f, 1.0f, 0.0f)); // Bright green to match spritesheet
            mat.SetShaderParameter("chroma_threshold", 0.35f);
            mat.SetShaderParameter("chroma_smoothing", 0.1f);
            _neonSprite.Material = mat;
        }
        else
        {
            _neonSprite.Modulate = new Color(1, 1, 1, 0.8f);
        }
        
        bgContainer.AddChild(_neonSprite);

        var darkenOverlay = new ColorRect();
        darkenOverlay.Color = new Color(0.05f, 0.05f, 0.09f, 0.5f);
        darkenOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bgContainer.AddChild(darkenOverlay);

        _mainMenuPanel = BuildMainMenu();
        _settingsPanel = BuildSettingsPanel();
        _lobbyPanel = BuildLobbyPanel();
        _collectiblesPanel = BuildCollectiblesPanel();

        AddChild(_mainMenuPanel);
        AddChild(_settingsPanel);
        AddChild(_lobbyPanel);
        AddChild(_collectiblesPanel);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LobbyUpdated += OnLobbyUpdated;
            LobbyManager.Instance.MatchStarting += OnMatchStarting;
        }
    }

    // ==================== MAIN MENU ====================

    private Control BuildMainMenu()
    {
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.Name = "MainMenu";

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        // Title
        var title = CreateLabel("GAME HUB", 24, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        title.AddThemeConstantOverride("shadow_offset_x", 2);
        title.AddThemeConstantOverride("shadow_offset_y", 2);
        vbox.AddChild(title);

        var subtitle = CreateLabel("Poker Roguelike · Multijogador LAN", 8, TextSecondary);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(subtitle);

        vbox.AddChild(CreateFixedSpacer(10));

        var playControls = new VBoxContainer();
        playControls.AddThemeConstantOverride("separation", 8);

        // Game Selection
        var gameRow = new HBoxContainer();
        gameRow.Alignment = BoxContainer.AlignmentMode.Center;
        gameRow.AddThemeConstantOverride("separation", 6);
        gameRow.AddChild(CreateLabel("Jogo:", 8, TextPrimary));
        
        var gameSelect = new OptionButton();
        gameSelect.AddItem("Poker", 0);
        gameSelect.AddItem("Truco", 1);
        gameSelect.CustomMinimumSize = new Vector2(80, 20);
        gameSelect.AddThemeFontSizeOverride("font_size", 8);
        gameRow.AddChild(gameSelect);
        playControls.AddChild(gameRow);

        // Mode Selection
        var modeRow = new HBoxContainer();
        modeRow.Alignment = BoxContainer.AlignmentMode.Center;
        modeRow.AddThemeConstantOverride("separation", 6);
        modeRow.AddChild(CreateLabel("Modo:", 8, TextPrimary));
        
        var modeSelect = new OptionButton();
        modeSelect.AddItem("Solo", 0);
        modeSelect.AddItem("Tutorial (IA)", 1);
        modeSelect.CustomMinimumSize = new Vector2(80, 20);
        modeSelect.AddThemeFontSizeOverride("font_size", 8);
        modeRow.AddChild(modeSelect);
        playControls.AddChild(modeRow);

        var playBtn = CreateStyledButton("Iniciar Jogo", BtnGreen, BtnGreenHover, new Vector2(160, 26));
        playBtn.Pressed += () =>
        {
            string game = gameSelect.Selected == 0 ? "Poker" : "Truco";
            string mode = modeSelect.Selected == 0 ? "Solo" : "Tutorial";
            GD.Print($"[HubMain] Launching {game} ({mode})...");
            
            Core.Registry.GameRegistry.IsTutorialMode = modeSelect.Selected == 1;

            if (gameSelect.Selected == 0)
                GetTree().ChangeSceneToFile("res://games/poker_roguelike/scenes/PokerGame.tscn");
            else
                GetTree().ChangeSceneToFile("res://games/truco/scenes/TrucoGame.tscn");
        };
        playControls.AddChild(playBtn);
        vbox.AddChild(playControls);

        vbox.AddChild(CreateFixedSpacer(4));

        var collectiblesBtn = CreateStyledButton("Colecionáveis", BtnPurple, BtnPurpleHover, new Vector2(160, 26));
        collectiblesBtn.Pressed += () => ShowMenu(HubState.Collectibles);
        vbox.AddChild(collectiblesBtn);

        var hostBtn = CreateStyledButton("Criar Sala LAN", BtnPurple, BtnPurpleHover, new Vector2(160, 26));
        hostBtn.Pressed += () =>
        {
            _isHosting = true;
            LobbyManager.Instance?.HostLobby("poker_roguelike", 6, LobbyState.TurnMode.Sequential, 300, "casino");
            ShowMenu(HubState.Lobby);
        };
        vbox.AddChild(hostBtn);

        var joinBtn = CreateStyledButton("Entrar em Sala", BtnPurple, BtnPurpleHover, new Vector2(160, 26));
        joinBtn.Pressed += () =>
        {
            _isHosting = false;
            ShowMenu(HubState.Lobby);
        };
        vbox.AddChild(joinBtn);

        var settingsBtn = CreateStyledButton("Configurações", BtnPurple, BtnPurpleHover, new Vector2(160, 26));
        settingsBtn.Pressed += () => ShowMenu(HubState.Settings);
        vbox.AddChild(settingsBtn);

        var quitBtn = CreateStyledButton("Sair", BtnRed, BtnRedHover, new Vector2(160, 26));
        quitBtn.Pressed += () => GetTree().Quit();
        vbox.AddChild(quitBtn);

        // Version
        vbox.AddChild(CreateFixedSpacer(6));
        var version = CreateLabel("v0.1.0", 10, new Color(0.3f, 0.3f, 0.4f));
        version.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(version);

        center.AddChild(vbox);
        return center;
    }

    // ==================== SETTINGS ====================

    private Control BuildSettingsPanel()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 60);
        margin.AddThemeConstantOverride("margin_right", 60);
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        margin.Name = "SettingsPanel";

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(vbox);

        // Title
        var title = CreateLabel("Configurações", 14, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(CreateFixedSpacer(6));

        // -- Profile --
        vbox.AddChild(CreateSectionLabel("Perfil"));

        var nickRow = new HBoxContainer();
        nickRow.AddThemeConstantOverride("separation", 8);
        var nickLabel = CreateLabel("Apelido:", 8, TextPrimary);
        nickLabel.CustomMinimumSize = new Vector2(70, 0);
        nickRow.AddChild(nickLabel);

        _nicknameEdit = new LineEdit();
        _nicknameEdit.Text = "Player";
        _nicknameEdit.PlaceholderText = "Seu apelido";
        _nicknameEdit.CustomMinimumSize = new Vector2(120, 20);
        _nicknameEdit.AddThemeFontSizeOverride("font_size", 8);
        nickRow.AddChild(_nicknameEdit);
        vbox.AddChild(nickRow);

        var avatarRow = new HBoxContainer();
        avatarRow.AddThemeConstantOverride("separation", 8);
        var avatarLabel = CreateLabel("Avatar:", 8, TextPrimary);
        avatarLabel.CustomMinimumSize = new Vector2(70, 0);
        avatarRow.AddChild(avatarLabel);

        _avatarSelect = new OptionButton();
        _avatarSelect.AddItem("Padrão", 0);
        _avatarSelect.AddItem("Tartaruga", 1);
        _avatarSelect.AddItem("Aranha", 2);
        _avatarSelect.CustomMinimumSize = new Vector2(120, 20);
        _avatarSelect.AddThemeFontSizeOverride("font_size", 8);
        avatarRow.AddChild(_avatarSelect);
        vbox.AddChild(avatarRow);

        // -- Audio --
        vbox.AddChild(CreateFixedSpacer(4));
        vbox.AddChild(CreateSectionLabel("Áudio"));

        var masterRow = CreateSliderRow("Master:", 0, 100, 100, out _masterSlider, out _masterValueLabel);
        vbox.AddChild(masterRow);

        var musicRow = CreateSliderRow("Música:", 0, 100, 80, out _musicSlider, out _musicValueLabel);
        vbox.AddChild(musicRow);

        var sfxRow = CreateSliderRow("Efeitos:", 0, 100, 100, out _sfxSlider, out _sfxValueLabel);
        vbox.AddChild(sfxRow);

        // -- Video --
        vbox.AddChild(CreateFixedSpacer(4));
        vbox.AddChild(CreateSectionLabel("Vídeo"));

        _fullscreenToggle = new CheckButton();
        _fullscreenToggle.Text = "  Tela Cheia";
        _fullscreenToggle.ButtonPressed = true;
        _fullscreenToggle.AddThemeFontSizeOverride("font_size", 8);
        vbox.AddChild(_fullscreenToggle);

        // -- Action buttons --
        vbox.AddChild(CreateFixedSpacer(10));

        var btnRow = new HBoxContainer();
        btnRow.Alignment = BoxContainer.AlignmentMode.Center;
        btnRow.AddThemeConstantOverride("separation", 12);

        var saveBtn = CreateStyledButton("Salvar", BtnGreen, BtnGreenHover, new Vector2(60, 20));
        saveBtn.Pressed += SaveSettings;
        btnRow.AddChild(saveBtn);

        var backBtn = CreateStyledButton("Voltar", BtnPurple, BtnPurpleHover, new Vector2(60, 20));
        backBtn.Pressed += () => ShowMenu(HubState.MainMenu);
        btnRow.AddChild(backBtn);

        vbox.AddChild(btnRow);

        return margin;
    }

    // ==================== LOBBY ====================

    private Control BuildLobbyPanel()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_right", 40);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        margin.Name = "LobbyPanel";

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 5);
        margin.AddChild(vbox);

        // Title
        var title = CreateLabel("Sala", 14, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // Status
        _lobbyStatusLabel = CreateLabel("Aguardando...", 8, TextSecondary);
        _lobbyStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_lobbyStatusLabel);

        vbox.AddChild(CreateFixedSpacer(4));

        // IP input row (for joining)
        _ipRow = new HBoxContainer();
        _ipRow.Alignment = BoxContainer.AlignmentMode.Center;
        _ipRow.AddThemeConstantOverride("separation", 8);

        var ipLabel = CreateLabel("IP:", 8, TextPrimary);
        _ipRow.AddChild(ipLabel);

        _ipEdit = new LineEdit();
        _ipEdit.Text = "127.0.0.1";
        _ipEdit.CustomMinimumSize = new Vector2(100, 20);
        _ipEdit.AddThemeFontSizeOverride("font_size", 8);
        _ipRow.AddChild(_ipEdit);

        var connectBtn = CreateStyledButton("Conectar", BtnGreen, BtnGreenHover, new Vector2(60, 20));
        connectBtn.AddThemeFontSizeOverride("font_size", 8);
        connectBtn.Pressed += () =>
        {
            LobbyManager.Instance?.JoinLobby(_ipEdit.Text);
            _lobbyStatusLabel.Text = $"Conectando a {_ipEdit.Text}...";
        };
        _ipRow.AddChild(connectBtn);
        vbox.AddChild(_ipRow);

        // Player list
        vbox.AddChild(CreateSectionLabel("Jogadores"));

        var playerScroll = new ScrollContainer();
        playerScroll.CustomMinimumSize = new Vector2(0, 100);
        playerScroll.SizeFlagsVertical = SizeFlags.ExpandFill;

        // Player list panel background
        var playerPanel = new PanelContainer();
        playerPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        playerPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        var playerPanelStyle = new StyleBoxFlat();
        playerPanelStyle.BgColor = new Color(0.07f, 0.07f, 0.12f);
        playerPanelStyle.SetCornerRadiusAll(4);
        playerPanelStyle.ContentMarginLeft = 8;
        playerPanelStyle.ContentMarginRight = 8;
        playerPanelStyle.ContentMarginTop = 6;
        playerPanelStyle.ContentMarginBottom = 6;
        playerPanel.AddThemeStyleboxOverride("panel", playerPanelStyle);

        _playerListBox = new VBoxContainer();
        _playerListBox.AddThemeConstantOverride("separation", 4);
        _playerListBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        playerPanel.AddChild(_playerListBox);
        playerScroll.AddChild(playerPanel);
        vbox.AddChild(playerScroll);

        // Action buttons
        var actionRow = new HBoxContainer();
        actionRow.Alignment = BoxContainer.AlignmentMode.Center;
        actionRow.AddThemeConstantOverride("separation", 10);

        _readyBtn = CreateStyledButton("Pronto", BtnGreen, BtnGreenHover, new Vector2(60, 20));
        _readyBtn.Pressed += () => LobbyManager.Instance?.ToggleReady();
        actionRow.AddChild(_readyBtn);

        _startBtn = CreateStyledButton("Iniciar", BtnPurple, BtnPurpleHover, new Vector2(60, 20));
        _startBtn.Pressed += () => LobbyManager.Instance?.TryStartMatch();
        actionRow.AddChild(_startBtn);

        var lobbyBackBtn = CreateStyledButton("Voltar", BtnRed, BtnRedHover, new Vector2(60, 20));
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
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.Visible = false;

        var panel = new PanelContainer();
        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.SetCornerRadiusAll(8);
        panel.AddThemeStyleboxOverride("panel", style);
        margin.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(vbox);

        var title = CreateLabel("COLECIONÁVEIS", 16, Gold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var subtitle = CreateLabel("Itens desbloqueados", 8, TextSecondary);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(subtitle);

        vbox.AddChild(CreateFixedSpacer(10));

        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 10);
        grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

        grid.AddChild(CreateSectionLabel("Avatares:"));
        grid.AddChild(CreateLabel("Tartaruga, Aranha", 8, TextPrimary));
        
        grid.AddChild(CreateSectionLabel("Cenários:"));
        grid.AddChild(CreateLabel("Cyber Casino", 8, TextPrimary));
        
        grid.AddChild(CreateSectionLabel("Versos de Carta:"));
        grid.AddChild(CreateLabel("Padrão", 8, TextPrimary));
        
        vbox.AddChild(grid);

        vbox.AddChild(CreateFixedSpacer(20));

        var backBtn = CreateStyledButton("Voltar", BtnRed, BtnRedHover, new Vector2(100, 26));
        backBtn.Pressed += () => ShowMenu(HubState.MainMenu);
        backBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(backBtn);

        return margin;
    }

    // ==================== STATE MANAGEMENT ====================

    private void ShowMenu(HubState state)
    {
        _currentState = state;
        _mainMenuPanel.Visible = state == HubState.MainMenu;
        _settingsPanel.Visible = state == HubState.Settings;
        _lobbyPanel.Visible = state == HubState.Lobby;
        _collectiblesPanel.Visible = state == HubState.Collectibles;

        if (state == HubState.Settings)
        {
            LoadSettingsToUI();
        }
        if (state == HubState.Lobby)
        {
            RefreshLobbyUI();
            _ipRow.Visible = !_isHosting;
            _startBtn.Visible = _isHosting;
            _lobbyStatusLabel.Text = _isHosting ? "Hosting — aguardando jogadores..." : "Digite o IP e conecte";
        }

        GD.Print($"[HubMain] Switching UI to: {state}");
    }

    // ==================== SETTINGS LOGIC ====================

    private void LoadSettingsToUI()
    {
        if (SettingsManager.Instance == null) return;
        _nicknameEdit.Text = SettingsManager.Instance.PlayerNickname;
        
        // Match avatar ID
        string avatar = SettingsManager.Instance.AvatarId;
        if (avatar == "turtle") _avatarSelect.Selected = 1;
        else if (avatar == "spider") _avatarSelect.Selected = 2;
        else _avatarSelect.Selected = 0;

        _masterSlider.Value = SettingsManager.Instance.MasterVolume * 100;
        _musicSlider.Value = SettingsManager.Instance.MusicVolume * 100;
        _sfxSlider.Value = SettingsManager.Instance.SfxVolume * 100;
        _fullscreenToggle.ButtonPressed = SettingsManager.Instance.IsFullscreen;
    }

    private void SaveSettings()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.PlayerNickname = _nicknameEdit.Text;
        
        SettingsManager.Instance.AvatarId = _avatarSelect.Selected switch {
            1 => "turtle",
            2 => "spider",
            _ => "default"
        };

        SettingsManager.Instance.MasterVolume = (float)_masterSlider.Value / 100f;
        SettingsManager.Instance.MusicVolume = (float)_musicSlider.Value / 100f;
        SettingsManager.Instance.SfxVolume = (float)_sfxSlider.Value / 100f;
        SettingsManager.Instance.IsFullscreen = _fullscreenToggle.ButtonPressed;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplySettings();
        GD.Print("[HubMain] Settings saved.");
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
            row.AddThemeConstantOverride("separation", 8);

            var nameLabel = CreateLabel(slot.PlayerName, 8, TextPrimary);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(nameLabel);

            var statusLabel = CreateLabel(
                slot.IsReady ? "✓ Pronto" : "Aguardando",
                8,
                slot.IsReady ? SuccessGreen : TextSecondary
            );
            row.AddChild(statusLabel);

            _playerListBox.AddChild(row);
        }

        _lobbyStatusLabel.Text = $"Jogadores: {lobby.PlayerSlots.Count}/{lobby.MaxPlayers}";
        _startBtn.Visible = LobbyManager.Instance.IsHost;
    }

    // ==================== HELPERS ====================

    private Label CreateLabel(string text, int fontSize, Color color)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
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
        row.AddThemeConstantOverride("separation", 8);

        var lbl = CreateLabel(label, 8, TextPrimary);
        lbl.CustomMinimumSize = new Vector2(40, 0);
        row.AddChild(lbl);

        slider = new HSlider();
        slider.MinValue = min;
        slider.MaxValue = max;
        slider.Value = value;
        slider.Step = 1;
        slider.CustomMinimumSize = new Vector2(100, 12);
        slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(slider);

        valueLabel = CreateLabel($"{(int)value}%", 8, TextSecondary);
        valueLabel.CustomMinimumSize = new Vector2(25, 0);
        row.AddChild(valueLabel);

        // Live update label
        var vl = valueLabel;
        slider.ValueChanged += (double v) => vl.Text = $"{(int)v}%";

        return row;
    }

    private Button CreateStyledButton(string text, Color normalColor, Color hoverColor, Vector2 minSize)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = minSize;
        btn.AddThemeFontSizeOverride("font_size", 8);
        btn.AddThemeColorOverride("font_color", TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", Colors.White);

        var normal = new StyleBoxFlat();
        normal.BgColor = normalColor;
        normal.SetCornerRadiusAll(4);
        normal.ContentMarginLeft = 8;
        normal.ContentMarginRight = 8;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = normal.Duplicate() as StyleBoxFlat;
        hover.BgColor = hoverColor;
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = normal.Duplicate() as StyleBoxFlat;
        pressed.BgColor = normalColor.Darkened(0.3f);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        return btn;
    }
}
