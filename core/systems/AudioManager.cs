using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Systems;

/// <summary>Original looping soundtracks, crossfades and a small polyphonic card SFX pool.</summary>
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }
    public static readonly string[] TrackIds = { "midnight-club", "velvet-table", "last-manilha", "copper-steps", "midnight-baron" };
    public static readonly string[] TrackNames = { "Depois da meia-noite", "Mesa de veludo", "Última manilha", "Passos de cobre", "O barão da noite" };
    private readonly List<AudioStreamPlayer> _effects = new();
    private readonly Dictionary<string, AudioStream> _cache = new();
    private AudioStreamPlayer _music, _outgoing;
    private Tween _fade;
    private string _context = "midnight-club", _playing = "";
    private int _nextEffect;

    public override void _EnterTree() { if (Instance == null) Instance = this; else QueueFree(); }
    public override void _Ready()
    {
        _music = new AudioStreamPlayer(); _outgoing = new AudioStreamPlayer();
        AddChild(_music); AddChild(_outgoing);
        for (int i = 0; i < 8; i++) { var player = new AudioStreamPlayer(); AddChild(player); _effects.Add(player); }
    }

    public void PlayMusic(string trackName)
    {
        _context = System.Array.IndexOf(TrackIds, trackName) >= 0 ? trackName : "midnight-club";
        int selected = SettingsManager.Instance?.MusicTrack ?? 0;
        string track = selected > 0 && selected <= TrackIds.Length ? TrackIds[selected - 1] : _context;
        if (_music == null || track == _playing) { ApplyVolumes(); return; }
        _fade?.Kill();
        (_music, _outgoing) = (_outgoing, _music);
        var stream = (AudioStreamWav)GD.Load<AudioStreamWav>($"res://assets/audio/{track}.wav").Duplicate();
        stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        stream.LoopBegin = 0;
        stream.LoopEnd = stream.Data.Length / (stream.Stereo ? 4 : 2);
        _music.Stream = stream; _music.VolumeDb = -60; _music.Play();
        _playing = track;
        _fade = CreateTween().SetParallel(true);
        _fade.TweenProperty(_music, "volume_db", MusicDb(), .8);
        _fade.TweenProperty(_outgoing, "volume_db", -60f, .8);
        _fade.Chain().TweenCallback(Callable.From(_outgoing.Stop));
    }

    public void ApplyVolumes()
    {
        if (_music == null) return;
        _fade?.Kill();
        _outgoing.Stop();
        _music.VolumeDb = MusicDb();
        float effects = (SettingsManager.Instance?.MasterVolume ?? 1) * (SettingsManager.Instance?.SfxVolume ?? 1);
        foreach (var player in _effects) player.VolumeDb = Db(effects);
    }

    public override void _ExitTree()
    {
        _fade?.Kill();
        _cache.Clear();
        _effects.Clear();
        if (Instance == this) Instance = null;
    }
    public void RefreshTrack() => PlayMusic(_context);
    private float MusicDb() => Db((SettingsManager.Instance?.MasterVolume ?? 1) * (SettingsManager.Instance?.MusicVolume ?? .8f) * .55f);
    private static float Db(float value) => value <= .001f ? -80 : Mathf.LinearToDb(value);

    public void PlaySound(string soundName)
    {
        if (_effects.Count == 0) return;
        var path = $"res://assets/audio/{soundName}.wav";
        if (!_cache.TryGetValue(soundName, out var stream))
        {
            if (!ResourceLoader.Exists(path)) return;
            stream = GD.Load<AudioStream>(path); _cache[soundName] = stream;
        }
        var player = _effects[_nextEffect++ % _effects.Count];
        player.Stream = stream;
        player.VolumeDb = Db((SettingsManager.Instance?.MasterVolume ?? 1) * (SettingsManager.Instance?.SfxVolume ?? 1));
        player.Play();
    }
}
