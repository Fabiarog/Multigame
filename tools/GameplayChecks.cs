using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameHub.Core.Registry;
using GameHub.Core.Systems;
using GameHub.Games.Truco;

/// <summary>Integration checks for complete bot teams and ownership of the solo Pena decision.</summary>
public partial class GameplayChecks : Node
{
    [Signal] public delegate void CompletedEventHandler(bool passed, string detail);
    private int _assertions;

    public void ConfigureTeams(int teamSize) => GameRegistry.TrucoTeamSize = teamSize;

    public async void Run()
    {
        try
        {
            SettingsManager.Instance.ReduceMotion = true;
            foreach (int teamSize in new[] { 2, 3 })
            {
                GameRegistry.TrucoTeamSize = teamSize;
                GetTree().ChangeSceneToFile("res://games/truco/scenes/TrucoGame.tscn");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var game = GetTree().CurrentScene.GetNode<TrucoGameManager>("GameManager");
                Assert(game.CurrentPhase == TrucoGameManager.TrucoPhase.Cutting, "Hand starts at cut");
                game.CutDeck();
                Assert(game.CurrentPhase == TrucoGameManager.TrucoPhase.PenaDecision, "Team hand offers Pena");
                Assert(game.PenaRecipientSeatIndex == 2 && game.PenaDecisionIsBot, "Nearest ally is an AI in solo");
                game.GivePena();
                Assert(game.CurrentPhase != TrucoGameManager.TrucoPhase.PenaDecision, "AI resolves its own Pena without a human keep/return action");
                Assert(game.TeamOneHands.Concat(game.TeamTwoHands).All(hand => hand.Count == 3), "Every seat receives exactly three cards");
                var dealt = game.TeamOneHands.Concat(game.TeamTwoHands).SelectMany(hand => hand).Select(card => card.ToString()).ToList();
                Assert(dealt.Count == teamSize * 6 && dealt.Distinct().Count() == dealt.Count, "All dealt cards are unique");
                Assert(!dealt.Contains(game.ViraCard.ToString()), "Vira is outside all hands");
                var seats = new HashSet<int>();
                var played = new HashSet<string>();
                bool duplicatePlay = false;
                game.DeckShuffled += () => played.Clear();
                game.CardPlayed += (_, _, round) =>
                {
                    seats.Add(game.LastPlayedSeatIndex);
                    if (!played.Add($"{round}:{game.LastPlayedSeatIndex}")) duplicatePlay = true;
                };
                await FinishHand(game);
                Assert(seats.Count == teamSize * 2, "Every ally and opponent participates in the hand");
                // Advance dealer twice: the nearest ally then becomes the local human.
                game.StartNewHand(); game.CutDeck(); game.GivePena();
                await FinishHand(game);
                game.StartNewHand(); game.CutDeck(); game.GivePena();
                Assert(game.PenaRecipientSeatIndex == (4 % (teamSize * 2)), "Pena follows the rotating dealer");
                if (teamSize == 2)
                {
                    Assert(game.PenaDecisionIsLocal && game.CurrentPhase == TrucoGameManager.TrucoPhase.PenaDecision, "Human recipient waits for a real choice");
                    string pena = game.PenaCard.ToString();
                    game.ResolvePena(true);
                    Assert(game.PenaWasKept && game.PlayerHand.Count == 3 && game.PlayerHand.Any(card => card.ToString() == pena), "Keeping the Pena grants it plus exactly two other cards");
                }
                // Reach a settled phase before freeing async game callbacks.
                if (game.CurrentPhase == TrucoGameManager.TrucoPhase.PenaDecision) game.ResolvePena(false);
                await FinishHand(game);
                Assert(!duplicatePlay, "A seat plays only once per tombo");
            }
            EmitSignal(SignalName.Completed, true, $"{_assertions} assertions passed: solo 2v2, 3v3, full seat turns, deck uniqueness, AI/human Pena ownership.");
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            EmitSignal(SignalName.Completed, false, ex.Message);
        }
    }

    private async Task FinishHand(TrucoGameManager game)
    {
        ulong deadline = Time.GetTicksMsec() + 18000;
        while (game.CurrentPhase != TrucoGameManager.TrucoPhase.HandEnd && game.CurrentPhase != TrucoGameManager.TrucoPhase.GameOver)
        {
            if (Time.GetTicksMsec() > deadline) throw new Exception($"Team hand stalled in {game.CurrentPhase}, seat {game.ActiveSeatIndex}");
            if (game.CurrentPhase == TrucoGameManager.TrucoPhase.PlayerTurn) game.PlayerPlayCard(0);
            if (game.CurrentPhase == TrucoGameManager.TrucoPhase.TrucoRequested) game.RespondToTruco(true, false);
            await ToSignal(GetTree().CreateTimer(.035), SceneTreeTimer.SignalName.Timeout);
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _assertions++;
    }
}
