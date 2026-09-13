using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Systems;

/// <summary>Original looping soundtracks, multi-format stream loading (MP3/WAV/OGG), crossfades, ducking and polyphonic SFX pool.</summary>
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    public static readonly string[] TrackIds =
    {
        "menu",
        "midnight-club",
        "velvet-table",
        "last-manilha",
        "copper-steps",
        "midnight-baron",
        "barao",
        "dama",
        "madrid",
        "mexico"
    };

    public static readonly string[] TrackNames =
    {
        "Tema do Clube (Menu)",
        "Depois da meia-noite (Classic Club)",
        "Mesa de veludo (Poker Roguelike)",
        "Última manilha (Tensão Truco)",
        "Passos de cobre (Fodinha)",
        "O barão da noite (Confronto)",
        "Barão da Meia-Noite (Boss)",
        "Dama de Copas (Boss)",
        "Salón de Madrid (Espanha)",
        "La Mesa de los Recuerdos (México)"
    };

    private readonly List<AudioStreamPlayer> _effects = new();
    private readonly Dictionary<string, AudioStream> _cache = new();
    private AudioStreamPlayer _music, _outgoing;
    private Tween _fade;
    private Tween _duckTween;
    private string _context = "menu", _playing = "";
    private int _nextEffect;
    private float _duckOffsetDb = 0f;

    public string CurrentTrack => _playing;

    public override void _EnterTree()
    {
        if (Instance == null) Instance = this;
        else QueueFree();
    }

    public override void _Ready()
    {
        _music = new AudioStreamPlayer { Name = "MusicPlayerA", Bus = "Master" };
        _outgoing = new AudioStreamPlayer { Name = "MusicPlayerB", Bus = "Master" };
        AddChild(_music);
        AddChild(_outgoing);

        for (int i = 0; i < 8; i++)
        {
            var player = new AudioStreamPlayer { Name = $"SfxPlayer_{i}", Bus = "Master" };
            AddChild(player);
            _effects.Add(player);
        }
    }

    private static float GetTrackVolumeOffset(string trackId) => trackId switch
    {
        "menu"   => -3.5f,
        "barao"  => -3.5f,
        "dama"   => -3.5f,
        "madrid" => -3.5f,
        "mexico" => -3.5f,
        _        => 0f
    };

    private AudioStream LoadMusicStream(string trackId)
    {
        if (_cache.TryGetValue(trackId, out var cached) && cached != null)
            return cached;

        string path = trackId switch
        {
            "menu"           => "res://assets/Musics/Musica Tema Menu.mp3",
            "barao"          => "res://assets/Musics/Barao da meia noite.mp3",
            "dama"           => "res://assets/Musics/Dama de copas.mp3",
            "madrid"         => "res://assets/Musics/Madrid.mp3",
            "mexico"         => "res://assets/Musics/Mexico.mp3",
            "midnight-club"  => "res://assets/audio/midnight-club.wav",
            "velvet-table"   => "res://assets/audio/velvet-table.wav",
            "last-manilha"   => "res://assets/audio/last-manilha.wav",
            "copper-steps"   => "res://assets/audio/copper-steps.wav",
            "midnight-baron" => "res://assets/audio/midnight-baron.wav",
            _                => $"res://assets/audio/{trackId}.wav"
        };

        if (!ResourceLoader.Exists(path))
        {
            path = "res://assets/audio/midnight-club.wav";
            if (!ResourceLoader.Exists(path)) return null;
        }

        var rawStream = GD.Load<AudioStream>(path);
        if (rawStream == null) return null;

        AudioStream processed = null;
        if (rawStream is AudioStreamWav wav)
        {
            var dup = (AudioStreamWav)wav.Duplicate();
            dup.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            dup.LoopBegin = 0;
            dup.LoopEnd = dup.Data.Length / (dup.Stereo ? 4 : 2);
            processed = dup;
        }
        else if (rawStream is AudioStreamMP3 mp3)
        {
            var dup = (AudioStreamMP3)mp3.Duplicate();
            dup.Loop = true;
            processed = dup;
        }
        else if (rawStream is AudioStreamOggVorbis ogg)
        {
            var dup = (AudioStreamOggVorbis)ogg.Duplicate();
            dup.Loop = true;
            processed = dup;
        }
        else
        {
            processed = rawStream;
        }

        _cache[trackId] = processed;
        return processed;
    }

    public void PlayMusic(string trackName)
    {
        _context = System.Array.IndexOf(TrackIds, trackName) >= 0 ? trackName : "midnight-club";
        int selected = SettingsManager.Instance?.MusicTrack ?? 0;
        string track = selected > 0 && selected <= TrackIds.Length ? TrackIds[selected - 1] : _context;

        if (_music == null) return;

        // Smooth continuation if same track is already active
        if (track == _playing && _music.Playing)
        {
            ApplyVolumes();
            return;
        }

        var stream = LoadMusicStream(track);
        if (stream == null) return;

        _fade?.Kill();
        (_music, _outgoing) = (_outgoing, _music);
        _music.Stream = stream;
        _music.VolumeDb = -60f;
        _music.Play();
        _playing = track;

        _fade = CreateTween().SetParallel(true);
        _fade.TweenProperty(_music, "volume_db", MusicDb(), 0.8f);
        _fade.TweenProperty(_outgoing, "volume_db", -60f, 0.8f);
        _fade.Chain().TweenCallback(Callable.From(_outgoing.Stop));
    }

    public void PlayMenuMusic() => PlayMusic("menu");

    public void PlayRoomMusic(string themeId)
    {
        string track = themeId switch
        {
            "classic_club" => "midnight-club",
            "madrid"       => "madrid",
            "madrid_salon" => "madrid",
            "mexico"           => "mexico",
            "mexico_recuerdos" => "mexico",
            "barao_lounge" => "barao",
            "dama_salon"   => "dama",
            "cyber_casino" => "velvet-table",
            _              => "midnight-club"
        };
        PlayMusic(track);
    }

    public void PlayBossMusic(int bossIndex)
    {
        string track = bossIndex switch
        {
            7 => "barao",
            8 => "dama",
            9 => "last-manilha",
            10 => "midnight-baron",
            _ => "midnight-baron"
        };
        PlayMusic(track);
    }

    public void PlayBossMusic(string characterId)
    {
        string track = characterId switch
        {
            "barao"    => "barao",
            "dama"     => "dama",
            "morgana"  => "last-manilha",
            "carnical" => "midnight-baron",
            _          => "midnight-baron"
        };
        PlayMusic(track);
    }

    public void SetDucking(bool active, float duckDb = -7f, float duration = 0.35f)
    {
        _duckTween?.Kill();
        _duckTween = CreateTween();
        float target = active ? duckDb : 0f;
        _duckTween.TweenProperty(this, nameof(_duckOffsetDb), target, duration);
        _duckTween.TweenCallback(Callable.From(ApplyVolumes));
    }

    public void ApplyVolumes()
    {
        if (_music != null && _music.Playing)
            _music.VolumeDb = MusicDb();

        float effects = (SettingsManager.Instance?.MasterVolume ?? 1) * (SettingsManager.Instance?.SfxVolume ?? 1);
        foreach (var player in _effects)
            if (IsInstanceValid(player)) player.VolumeDb = Db(effects);
    }

    public override void _ExitTree()
    {
        StopAll();
        _cache.Clear();
        _effects.Clear();
        if (Instance == this) Instance = null;
    }

    public void StopAll()
    {
        _fade?.Kill();
        _duckTween?.Kill();
        ReleasePlayer(_music);
        ReleasePlayer(_outgoing);
        foreach (var player in _effects) ReleasePlayer(player);
        _playing = "";
    }

    private static void ReleasePlayer(AudioStreamPlayer player)
    {
        if (!IsInstanceValid(player)) return;
        player.Stop();
        player.Stream = null;
    }

    public void RefreshTrack() => PlayMusic(_context);

    private float MusicDb()
    {
        float baseVol = (SettingsManager.Instance?.MasterVolume ?? 1) * (SettingsManager.Instance?.MusicVolume ?? .8f) * .55f;
        float offset = GetTrackVolumeOffset(_playing);
        return Db(baseVol) + offset + _duckOffsetDb;
    }

    private static float Db(float value) => value <= .001f ? -80f : Mathf.LinearToDb(value);

    public void PlaySound(string soundName)
    {
        if (_effects.Count == 0) return;
        string path = $"res://assets/audio/{soundName}.wav";
        if (!_cache.TryGetValue(soundName, out var stream) || stream == null)
        {
            if (!ResourceLoader.Exists(path)) return;
            stream = GD.Load<AudioStream>(path);
            _cache[soundName] = stream;
        }
        var player = _effects[_nextEffect++ % _effects.Count];
        player.Stream = stream;
        player.VolumeDb = Db((SettingsManager.Instance?.MasterVolume ?? 1) * (SettingsManager.Instance?.SfxVolume ?? 1));
        player.Play();
    }
}

