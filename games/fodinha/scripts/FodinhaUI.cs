using Godot;
using System;
using System.Linq;
using GameHub.Core.Visuals;
using GameHub.Core.Systems;

namespace GameHub.Games.Fodinha;

public partial class FodinhaUI : Control
{
    private FodinhaMatch _match;
    private TableStage _table;
    private Label _round, _status, _vira;
    private readonly Label[] _scores = new Label[4];
    private HBoxContainer _actions, _hand;
    private double _cooldown;
    private bool _collecting, _starting = true;
    private bool _cutReady, _dealing, _dealAnimationStarted;
    public FodinhaMatch Match => _match;

    public override async void _Ready()
    {
        Theme = ClubTheme.Create();
        AddChild(new ColorRect { Color = ClubTheme.Ink, MouseFilter = MouseFilterEnum.Ignore });
        ((Control)GetChild(0)).SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = new MarginContainer(); AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 20);
        var page = new VBoxContainer(); page.AddThemeConstantOverride("separation", 10); margin.AddChild(page);
        var header = new HBoxContainer(); page.AddChild(header);
        var title = ClubTheme.Label("Fodinha", 34); title.AddThemeFontOverride("font", ClubTheme.DisplayFont); header.AddChild(title);
        _round = ClubTheme.Label("", 15, ClubTheme.Gold); _round.SizeFlagsHorizontal = SizeFlags.ExpandFill; _round.HorizontalAlignment = HorizontalAlignment.Center; header.AddChild(_round);
        var rules = ClubTheme.Button("Como jogar"); rules.Pressed += ShowRules; header.AddChild(rules);
        var exit = ClubTheme.Button("Voltar ao clube"); exit.Pressed += () => GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn"); header.AddChild(exit);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 14); page.AddChild(body);
        var sidebar = new PanelContainer { CustomMinimumSize = new Vector2(222, 0) }; body.AddChild(sidebar);
        var scoreColumn = new VBoxContainer(); scoreColumn.AddThemeConstantOverride("separation", 9); sidebar.AddChild(scoreColumn);
        scoreColumn.AddChild(ClubTheme.Label("CADA UM POR SI", 13, ClubTheme.Gold));
        for (int seat = 0; seat < 4; seat++)
        {
            _scores[seat] = ClubTheme.Label("", 15);
            _scores[seat].AutowrapMode = TextServer.AutowrapMode.WordSmart;
            scoreColumn.AddChild(_scores[seat]);
        }
        scoreColumn.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        _vira = ClubTheme.Label("", 14, ClubTheme.Gold); scoreColumn.AddChild(_vira);
        var note = ClubTheme.Label("5 vidas · 3 IAs\nErrou por 2? Perde 2 vidas.\nAcertou? Mantém as vidas.", 13, ClubTheme.Muted);
        scoreColumn.AddChild(note);
        var stageFrame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipContents = true };
        stageFrame.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Border, 1)); body.AddChild(stageFrame);
        _table = new TableStage { Name = "FodinhaTable", SeatCount = 4, RivalIndex = 2 }; stageFrame.AddChild(_table);
        _status = ClubTheme.Label("", 16); _status.AutowrapMode = TextServer.AutowrapMode.WordSmart; page.AddChild(_status);
        _actions = new HBoxContainer { CustomMinimumSize = new Vector2(0, 44) }; _actions.AddThemeConstantOverride("separation", 8); page.AddChild(_actions);
        _hand = new HBoxContainer { CustomMinimumSize = new Vector2(0, 108), Alignment = BoxContainer.AlignmentMode.Center };
        _hand.AddThemeConstantOverride("separation", 10); page.AddChild(_hand);
        AudioManager.Instance?.PlayMusic("copper-steps");
        _match = new FodinhaMatch(); SyncFans(); Render();
        AccessibilityVisuals.AddGlobalFilter(this);
        await _table.PlayEntrance();
        if (!IsInsideTree()) return;
        _starting = false; PrepareDeck();
    }

    private string SeatName(int seat) => seat == 0 ? "Você" : CharacterCatalog.Names[_table.CharacterAt(seat)];
    private void SyncFans()
    {
        for (int seat = 0; seat < 4; seat++) _table.SetCardCount(seat, _match.Hand(seat).Count);
    }
    private static void Clear(Control container)
    {
        foreach (var child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }
    }
    private void Action(string text, Action action, bool primary = false)
    {
        var button = ClubTheme.Button(text, primary); button.Pressed += action; _actions.AddChild(button);
    }

    private void Render()
    {
        _round.Text = $"MÃO {_match.RoundIndex + 1} / 9  ·  {_match.CardsPerHand} CARTA(S)";
        string vira = _match.Vira == null ? "Vira fechada" : $"Vira: {_match.Vira} · Manilha: {new GameHub.Games.Truco.TrucoCardData { Rank = _match.Manilha }.GetRankString()}";
        _vira.Text = $"Distribui: {SeatName(_match.DealerSeat)}\nCorta: {SeatName(_match.CutterSeat)}\n{vira}";
        for (int seat = 0; seat < 4; seat++)
        {
            string bid = _match.Bids[seat] < 0 ? "—" : _match.Bids[seat].ToString();
            string loss = _match.State is FodinhaMatch.Phase.RoundResult or FodinhaMatch.Phase.Finished ? $"  ·  −{_match.Losses[seat]}" : "";
            _scores[seat].Text = $"{SeatName(seat)}  ·  {_match.Lives[seat]} vidas{loss}\nPalpite {bid}   /   Vitórias {_match.Wins[seat]}";
            _scores[seat].Modulate = _match.Lives[seat] == 0 ? new Color(1, 1, 1, .45f) : Colors.White;
        }
        Clear(_actions); Clear(_hand);
        bool playable = !_starting && _match.State == FodinhaMatch.Phase.Playing && _match.CurrentSeat == 0;
        for (int i = 0; i < (_dealing ? 0 : _match.Hand(0).Count); i++)
        {
            int index = i; var card = _match.Hand(0)[i];
            string suit = new[] { "diamonds", "spades", "hearts", "clubs" }[(int)card.Suit];
            var button = new Button { Name = $"HandCard{i}", Text = "", Disabled = !playable,
                CustomMinimumSize = new Vector2(74, 104), TooltipText = $"Jogar {card}", MouseDefaultCursorShape = CursorShape.PointingHand };
            var face = new TextureRect { Texture = GD.Load<Texture2D>($"res://assets/models/cards/{card.GetRankString()}-{suit}.png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore, TextureFilter = TextureFilterEnum.Linear };
            button.AddChild(face); face.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 5);
            button.Pressed += () => Play(0, index); _hand.AddChild(button);

            if (GameHub.Core.Systems.SettingsManager.Instance?.ReduceMotion != true)
            {
                button.Modulate = new Color(1, 1, 1, 0);
                button.Scale = new Vector2(0.82f, 0.82f);
                button.PivotOffset = button.CustomMinimumSize / 2f;
                var tween = CreateTween();
                tween.TweenInterval(i * 0.05f);
                tween.TweenProperty(button, "modulate:a", 1f, 0.18f);
                tween.Parallel().TweenProperty(button, "scale", Vector2.One, 0.22f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }
        if (_dealing) { _status.Text = $"{SeatName(_match.DealerSeat)} distribuindo as cartas…"; return; }
        switch (_match.State)
        {
            case FodinhaMatch.Phase.Cutting:
                _status.Text = !_cutReady ? $"{SeatName(_match.DealerSeat)} embaralhando…" :
                    _match.CutterSeat == 0 ? "Sua vez de cortar o baralho." : $"{SeatName(_match.CutterSeat)} vai cortar…";
                if (!_starting && _cutReady && _match.CutterSeat == 0) Action("Cortar o baralho", () => Cut(0), true);
                break;
            case FodinhaMatch.Phase.Bidding:
                _status.Text = _match.CurrentSeat == 0 ? "Quantas vazas você vai ganhar? Escolha seu palpite antes de jogar." : $"{SeatName(_match.CurrentSeat)} está dando seu palpite…";
                if (_match.CurrentSeat == 0 && !_starting)
                    for (int amount = 0; amount <= _match.CardsPerHand; amount++)
                    { int bid = amount; Action($"Palpite: {bid}", () => { if (_match.Bid(0, bid)) { _cooldown = .65; Render(); } }, true); }
                break;
            case FodinhaMatch.Phase.Playing:
                _status.Text = playable ? "Sua vez. Jogue uma carta para chegar ao seu palpite." : $"Vez de {SeatName(_match.CurrentSeat)}…";
                _actions.AddChild(ClubTheme.Label($"VAZA {_match.TricksCompleted + 1} / {_match.CardsPerHand}  ·  Qualquer naipe pode ser jogado.", 14, ClubTheme.Muted));
                break;
            case FodinhaMatch.Phase.TrickResult:
                _status.Text = $"{SeatName(_match.LastWinner)} ganhou a vaza e começa a próxima.";
                break;
            case FodinhaMatch.Phase.RoundResult:
                _status.Text = _match.Lives[0] == 0 ? "Você foi eliminado. Acompanhe os palpites restantes ou comece outra partida." : "Mão encerrada. Confira seu palpite, suas vitórias e as vidas perdidas.";
                Action("Próxima mão →", () => { if (_match.AdvanceRound()) PrepareDeck(); }, true);
                if (_match.Lives[0] == 0) Action("Jogar novamente", Restart);
                break;
            case FodinhaMatch.Phase.Finished:
                var winners = _match.Winners;
                _status.Text = winners.Length == 0 ? "Todos perderam suas vidas. A mesa terminou empatada." :
                    $"{(winners.Length > 1 ? "Vitória compartilhada" : "Vencedor")}: {string.Join(" e ", winners.Select(SeatName))}.";
                Action("Jogar novamente", Restart, true);
                break;
        }
    }

    private void Restart() => GetTree().ReloadCurrentScene();
    private void PrepareDeck()
    {
        _table.ClearPlayedCards(); SyncFans();
        _cutReady = false; _cooldown = .85;
        AudioManager.Instance?.PlaySound("shuffle"); _table.AnimateDeck(false); Render();
    }

    private void Cut(int seat)
    {
        if (!_cutReady || !_match.Cut(seat)) return;
        _dealing = true; _dealAnimationStarted = false; _cooldown = .45;
        AudioManager.Instance?.PlaySound("cut"); _table.AnimateDeck(true); Render();
    }
    private void Play(int seat, int cardIndex)
    {
        if (_starting || _match.State != FodinhaMatch.Phase.Playing || _match.CurrentSeat != seat) return;
        var card = _match.Hand(seat)[cardIndex];
        if (!_match.Play(seat, cardIndex)) return;
        _table.PlayCard(seat, card.ToString(), 0, SeatName(seat), card.Rank == _match.Manilha);
        SyncFans(); AudioManager.Instance?.PlaySound("play");
        _cooldown = _match.State == FodinhaMatch.Phase.TrickResult ? 1.7 : .8;
        if (_match.State == FodinhaMatch.Phase.TrickResult) { _table.React(_match.LastWinner); _table.ShowOutcome($"{SeatName(_match.LastWinner)} ganhou a vaza"); }
        Render();
    }

    public override void _Process(double delta)
    {
        if (_starting || _match == null || _collecting) return;
        _cooldown -= delta; if (_cooldown > 0) return;
        if (_dealing)
        {
            if (!_dealAnimationStarted)
            {
                _dealAnimationStarted = true;
                _table.AnimateDeal(_match.DealerSeat, Enumerable.Range(0,4).Select(s => _match.Hand(s).Count).ToArray());
                _table.PlayTableAction(_match.DealerSeat);
                AudioManager.Instance?.PlaySound("deal");
                _cooldown = .45 + _match.ActiveSeats.Length * _match.CardsPerHand * .045;
                return;
            }
            _dealing = false; SyncFans(); _cooldown = .65; Render(); return;
        }
        int seat = _match.CurrentSeat;
        if (_match.State == FodinhaMatch.Phase.Cutting)
        {
            if (!_cutReady) { _cutReady = true; _cooldown = .65; Render(); }
            else if (seat != 0) Cut(seat);
        }
        else if (_match.State == FodinhaMatch.Phase.Bidding && seat != 0)
        {
            _match.Bid(seat, FodinhaBot.Predict(_match.Hand(seat), _match.Vira, _match.ActiveSeats.Length));
            _table.PlayGesture(seat, "truco"); _cooldown = .65; Render();
        }
        else if (_match.State == FodinhaMatch.Phase.Playing && seat != 0)
            Play(seat, FodinhaBot.Choose(_match.Hand(seat), _match.Trick, _match.Manilha, _match.Bids[seat], _match.Wins[seat]));
        else if (_match.State == FodinhaMatch.Phase.TrickResult) CollectTrick();
    }

    private async void CollectTrick()
    {
        _collecting = true;
        await _table.CollectRoundCardsToDiscard();
        if (!IsInsideTree()) return;
        _match.AdvanceTrick(); _collecting = false; _cooldown = .6;
        if (_match.State == FodinhaMatch.Phase.Finished)
        {
            foreach (int winner in _match.Winners) _table.React(winner);
            if (_match.Winners.Contains(0)) { CharacterProgress.RecordWin(); AudioManager.Instance?.PlaySound("win"); }
        }
        Render();
    }

    private void ShowRules()
    {
        var dialog = new AcceptDialog { Title = "Fodinha · regras desta mesa", DialogText =
            "Você + 3 IAs, cada um por si. Todos começam com 5 vidas.\nMãos de 1, 2, 3, 4, 5, 4, 3, 2 e 1 cartas.\nAntes de jogar, cada participante prevê suas vitórias (vazas).\nPerde |palpite − vitórias| vidas. Acertar preserva suas vidas.\n\nBaralho de truco: 4 < 5 < 6 < 7 < Q < J < K < A < 2 < 3.\nA carta acima da vira é manilha: ♦ < ♠ < ♥ < ♣.\nNão precisa seguir naipe. Empate de força: vence quem jogou primeiro.\nO vencedor da vaza começa a próxima. A abertura gira a cada mão.\nOs palpites são livres, inclusive o último.\n\nZero vidas elimina. Vence quem sobreviver sozinho ou tiver mais\nvidas após a nona mão. Igualdade de vidas dá vitória compartilhada." };
        AddChild(dialog); dialog.Confirmed += dialog.QueueFree; dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered();
    }
}
