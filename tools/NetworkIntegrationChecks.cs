using Godot;
using System;
using System.Linq;
using GameHub.Core.Networking;
public partial class NetworkIntegrationChecks : Node
{
    private bool _host, _ready, _started, _loaded, _attacked, _visual, _captured;
    private string _game;
    private double _elapsed, _tick, _done;
    private long _sent = -1;
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs(); _visual = args.Contains("visual"); _host = args.Contains("host");
        _game = args.First(a => a is "truco" or "fodinha" or "poker_classic");
        int port = int.Parse(args.Last());
        if (_host) { if (!LobbyManager.Instance.HostAt(_game, _game == "fodinha" ? 4 : 2, port)) throw new Exception("Host failed"); }
        else LobbyManager.Instance.JoinLobby($"127.0.0.1:{port}");
        GD.Print($"NET_QA_START pid={OS.GetProcessId()} role={(_host ? "host" : "client")} game={_game}");
    }
    private async void Capture() { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw); GetViewport().GetTexture().GetImage().SavePng($"C:/workspace/multigame/docs/network-qa/{_game}-{(_host ? "host" : "client")}.png"); }
    public override void _Process(double delta)
    {
        _elapsed += delta; _tick += delta;
        if (_elapsed > 120) { GD.Print("NET_QA_FAIL timeout"); GetTree().Quit(1); return; }
        if (_tick < .07) return; _tick = 0;
        var lobby = LobbyManager.Instance; var match = AuthoritativeMatch.Instance;
        if (!match.Active)
        {
            if (!_ready && lobby.CurrentLobby.PlayerSlots.Count >= 2) { _ready = true; lobby.ToggleReady(); }
            if (_host && !_started && lobby.CurrentLobby.AreAllPlayersReady()) { _started = true; lobby.TryStartMatch(); }
            return;
        }
        if (!_loaded) { _loaded = true; if (_visual) GetTree().ChangeSceneToFile("res://core/networking/NetworkTable.tscn"); else match.Loaded(); }
        var v = match.LocalView; if (v == null) return;
        if (v.Hand.Length > (_game == "poker_classic" ? 2 : _game == "truco" ? 3 : 5)) throw new Exception("Private hand oversized");
        if (_visual && !_captured && v.Hand.Length > 0 && v.Phase != "Loading") { _captured = true; Capture(); }
        if (!_host && !_attacked && v.Phase != "Loading")
        { _attacked = true; match.RpcId(1, "SubmitIntent", "{bad"); match.RpcId(1,"SubmitIntent", "{\"Score\":999,\"Winner\":1}"); }
        if (v.Phase == "Finished" || (_game == "poker_classic" && v.Phase == "RoundResult"))
        {
            _done += .07;
            if (_done > (_host ? 1.0 : .07))
            {
                if (_host && (match.Acknowledged == 0 || match.Rejected < 2)) throw new Exception("Missing peer ACK or malicious rejection");
                GD.Print($"NET_QA_PASS pid={OS.GetProcessId()} game={_game} match={v.MatchId} seat={v.YourSeat} sequence={v.Sequence} scores={string.Join(',',v.Scores)} ack={match.Acknowledged} rejected={match.Rejected} divergences={match.Divergences}");
                GetTree().Quit(0);
            }
            return;
        }
        if (_sent == v.Sequence || v.Actions.Length == 0) return;
        _sent = v.Sequence;
        string action = v.Actions.Contains("play") ? "play" : v.Actions.Contains("call") ? "call" : v.Actions.Contains("check") ? "check" : v.Actions.First();
        int value = 0;
        if (_game == "poker_classic" && v.Actions.Contains("raise")) { action = "raise"; value = v.MaxRaiseTo; }
        if (action == "pena") value = 1;
        match.SendIntent(action, value);
    }
}
