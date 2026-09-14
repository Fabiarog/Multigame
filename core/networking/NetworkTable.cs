using Godot;
using System;
using System.Linq;
using GameHub.Core.Visuals;
namespace GameHub.Core.Networking;

/// <summary>Renders only the host's recipient-specific projection. Never runs local game rules.</summary>
public partial class NetworkTable : Control
{
    private TableStage _stage;
    private Label _status, _score, _public, _notice;
    private HBoxContainer _hand, _actions;
    private string _room = "", _phase = "";
    private int _castSeats, _round = -1, _trick = -1, _shown;
    public override void _Ready()
    {
        Theme = ClubTheme.Create();
        var background = new ColorRect { Color = new Color("101d19"), MouseFilter = MouseFilterEnum.Ignore }; AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var layout = new VBoxContainer(); AddChild(layout); layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _status = new Label(); layout.AddChild(_status);
        _score = new Label(); layout.AddChild(_score);
        _stage = new TableStage { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        layout.AddChild(_stage);
        _public = new Label { HorizontalAlignment = HorizontalAlignment.Center }; layout.AddChild(_public);
        _hand = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; layout.AddChild(_hand);
        _actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; layout.AddChild(_actions);
        _notice = new Label { HorizontalAlignment = HorizontalAlignment.Center }; layout.AddChild(_notice);
        var leave = new Button { Text = "Sair da mesa" }; layout.AddChild(leave);
        leave.Pressed += () => { LobbyManager.Instance.LeaveLobby(); GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn"); };
        AuthoritativeMatch.Instance.ViewUpdated += Render;
        Render(); AuthoritativeMatch.Instance.Loaded();
    }
    public override void _ExitTree() { if (AuthoritativeMatch.Instance != null) AuthoritativeMatch.Instance.ViewUpdated -= Render; }
    private static void Clear(Node node) { foreach (Node child in node.GetChildren()) { node.RemoveChild(child); child.QueueFree(); } }
    private void Button(HBoxContainer row, string title, string action, int value = 0, bool enabled = true)
    {
        var button = new Button { Text = title, Disabled = !enabled, CustomMinimumSize = new Vector2(86, 48) };
        row.AddChild(button);
        button.Pressed += () => { foreach (var child in _actions.GetChildren().Concat(_hand.GetChildren())) if (child is Button b) b.Disabled = true; AuthoritativeMatch.Instance.SendIntent(action, value); };
    }
    private void Render()
    {
        var match = AuthoritativeMatch.Instance; var v = match.LocalView;
        _notice.Text = match.Notice;
        Clear(_hand); Clear(_actions);
        if (v == null) { _status.Text = "Carregando mesa…"; return; }
        int Map(int seat) => (seat - v.YourSeat + v.Names.Length) % v.Names.Length;
        string Name(int seat) => seat >= 0 && seat < v.Names.Length ? v.Names[seat] : "—";
        if (_castSeats != v.Names.Length) { _castSeats = v.Names.Length; _stage.SetCast(2, _castSeats); }
        if (_room != v.Room) { _room = v.Room; _stage.SetRoomTheme(_room); }
        if (_phase != v.Phase)
        {
            if (v.Phase == "Shuffling") _stage.AnimateDeck(false);
            if (v.Phase == "Cutting") { _stage.AnimateDeckPass(Map(v.Cutter)); _stage.AnimateDeck(true); }
            if (v.Phase == "TrucoResponse") _stage.ShowReactionBubble(Map(v.Turn), $"Truco · {v.PendingStakes}");
            _phase = v.Phase;
        }
        for (int i = 0; i < v.Counts.Length; i++) _stage.SetCardCount(Map(i), v.Counts[i]);
        if (_round != v.Round || _trick != v.Trick || v.Plays.Length < _shown)
        { _stage.ClearPlayedCards(); _shown = 0; _round = v.Round; _trick = v.Trick; }
        for (; _shown < v.Plays.Length; _shown++) { var play = v.Plays[_shown]; _stage.PlayCard(Map(play.Seat), play.Card, ownerName: Name(play.Seat)); }
        string phase = v.Phase switch { "Loading" => "Aguardando jogadores", "Shuffling" => "Embaralhando", "Cutting" => "Corte", "PenaOffer" => "Entrega da pena", "PenaDecision" => "Decisão da pena", "Dealing" => "Distribuindo", "Playing" => "Jogada", "Betting" => "Apostas", "Bidding" => "Palpites", "TrucoResponse" => "Resposta ao Truco", "TrickResult" => "Resultado da vaza", "StreetResult" => "Abrindo cartas", "RoundResult" => "Resultado da mão", "Finished" => "Partida encerrada", _ => v.Phase };
        _status.Text = $"{(v.Game == "poker_classic" ? "Texas Hold’em · fichas fictícias" : v.Game == "truco" ? "Truco" : "Fodinha")} · {phase} · Vez: {Name(v.Turn)} · Distribui: {Name(v.Dealer)}";
        _score.Text = v.Game == "truco" ? string.Join("   ", v.Scores.Select((s, i) => $"Equipe {i + 1}: {s}")) : string.Join("   ", v.Scores.Select((s, i) => $"{Name(i)}: {s}{(v.Game == "fodinha" ? " vidas" : " fichas")}"));
        _public.Text = v.Game == "poker_classic" ? $"Mesa: {string.Join("  ", v.Board)} · Pote: {v.Pot}" : $"Vira: {v.Vira} · Valor: {v.Stakes}";
        if (v.Bids.Length > 0) _public.Text += "\n" + string.Join(" · ", v.Bids.Select((b, i) => $"{Name(i)}: palpite {b}, vitórias {v.Wins[i]}"));
        if (v.Winners.Length > 0) _public.Text += "\nVencedor(es): " + string.Join(", ", v.Winners.Select(i => v.Game == "truco" ? $"Equipe {i + 1}" : Name(i)));
        bool Can(string action) => match.Active && v.Actions.Contains(action);
        for (int i = 0; i < v.Hand.Length; i++) Button(_hand, v.Hand[i], "play", i, Can("play"));
        if (Can("bid")) for (int i = 0; i <= v.Stakes; i++) Button(_actions, $"Palpite {i}", "bid", i);
        if (Can("pena")) { Button(_actions, $"Ficar com {v.Pena}", "pena", 1); Button(_actions, "Descartar pena", "pena", 0); }
        foreach (string action in new[] { "cut", "offer", "truco", "accept", "decline", "fold", "check", "call" })
            if (Can(action)) Button(_actions, action switch { "cut" => "Cortar baralho", "offer" => "Entregar pena", "truco" => "Truco!", "accept" => "Aceitar", "decline" => "Correr", "fold" => "Desistir", "check" => "Passar", _ => $"Pagar {v.ToCall}" }, action);
        if (Can("raise"))
        {
            if (v.Game != "poker_classic") Button(_actions, "Aumentar!", "raise");
            else
            {
                var amount = new SpinBox { MinValue = Math.Min(v.MinRaiseTo, v.MaxRaiseTo), MaxValue = v.MaxRaiseTo, Step = 1, Value = Math.Min(v.MinRaiseTo, v.MaxRaiseTo) }; _actions.AddChild(amount);
                var raise = new Button { Text = "Aumentar para", CustomMinimumSize = new Vector2(120, 48) }; _actions.AddChild(raise);
                raise.Pressed += () => { raise.Disabled = true; match.SendIntent("raise", (int)amount.Value); };
            }
        }
    }
}
