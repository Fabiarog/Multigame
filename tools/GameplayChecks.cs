using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameHub.Core.Registry;
using GameHub.Core.Systems;
using GameHub.Games.Truco;
using GameHub.Core.Visuals;

/// <summary>Integration checks for complete bot teams and ownership of the solo Pena decision.</summary>
public partial class GameplayChecks : Node
{
    [Signal] public delegate void CompletedEventHandler(bool passed, string detail);
    private int _assertions;

    public void ConfigureTeams(int teamSize) => GameRegistry.TrucoTeamSize = teamSize;
    public void ReleaseManagedResources()
    {
        // Rapid automated scene changes leave managed Resource wrappers waiting
        // for their next GC; settle them before the native engine shuts down.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    }
    public void ConfigureBenchmark(bool ultra)
    {
        SettingsManager.Instance.ReduceMotion = true;
        GameRegistry.TrucoTeamSize = 3;
        var quality = new GameHub.Core.Graphics.RayTracingSettings();
        if (ultra) quality.EnableAll(GameHub.Core.Graphics.RayTracingSettings.RtQualityLevel.Ultra);
        GameHub.Core.Graphics.GraphicsQualityManager.Instance.ApplyNewSettings(quality);
        foreach(var table in GetTree().CurrentScene.FindChildren("*", "SubViewportContainer", true, false).OfType<TableStage>())
        {
            var environment = table.FindChildren("TableLighting", "WorldEnvironment", true, false).OfType<WorldEnvironment>().Single().Environment;
            if (environment.SsaoEnabled != ultra || environment.SsrEnabled != ultra)
                throw new InvalidOperationException("Graphics changes must reach the table's private viewport through the settings signal.");
            table.ClearPlayedCards();
            for(int i=0;i<18;i++) table.PlayCard(i%6,new[]{"A♠","K♥","Q♦","J♣","3♥","2♠"}[i%6]);
        }
    }

    public async void Run()
    {
        try
        {
            SettingsManager.Instance.ReduceMotion = true;
            for (int character = 0; character < CharacterCatalog.Ids.Length; character++)
            {
                var model = GD.Load<PackedScene>(CharacterCatalog.ModelPath(character)).Instantiate<Node3D>();
                var animator = model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().First();
                Assert(new[] { "entrance", "truco", "victory", "boss_intro", "flourish" }.All(clip => animator.GetAnimationList().Any(name => name == clip || name.EndsWith("/" + clip))), "Each Blender model exports all five animated clips");
                Assert(model.FindChildren("*", "MeshInstance3D", true, false).Count == 4, "Each GLB contains exactly one four-part character, without other open Blender scenes");
                var head = model.FindChildren("Head*", "Node3D", true, false).OfType<Node3D>().First(node => node is not MeshInstance3D);
                Assert(head.Position.Y > 1.3f, "Head retains its rest height without playing an animation");
                var skin = model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                    .SelectMany(mesh => Enumerable.Range(0, mesh.Mesh.GetSurfaceCount()).Select(surface => mesh.Mesh.SurfaceGetMaterial(surface)))
                    .OfType<StandardMaterial3D>().First(material => material.ResourceName.StartsWith(CharacterCatalog.Ids[character] + "_skin", StringComparison.Ordinal));
                var color = skin.AlbedoColor;
                Assert(Mathf.Max(color.R, Mathf.Max(color.G, color.B)) - Mathf.Min(color.R, Mathf.Min(color.G, color.B)) > .005f,
                    "Character skin retains its palette instead of Blender's default white material");
                model.Free();
            }
            var clock = GD.Load<PackedScene>("res://assets/models/club/club_clock.glb").Instantiate<Node3D>();
            Assert(clock.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Any(player => player.HasAnimation("idle")), "Clock exports its pendulum animation");
            clock.Free();
            Assert(CharacterCatalog.PlayableCount == 6 && CharacterCatalog.IsBoss(6) && CharacterCatalog.IsBoss(7), "Bosses are separated from playable characters");
            SettingsManager.Instance.CharacterId = "corvo";
            for (int win = 0; win < 3; win++) CharacterProgress.RecordWin();
            Assert(CharacterProgress.TrucoClip(2) == "flourish" && CharacterProgress.TrucoClip(3) == "truco", "Cosmetic mission unlocks only for the character used");
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
                var table = GetTree().CurrentScene.FindChildren("*", "SubViewportContainer", true, false).OfType<TableStage>().First();
                var environment = table.FindChildren("TableLighting", "WorldEnvironment", true, false).OfType<WorldEnvironment>().Single().Environment;
                Assert(!environment.SsaoEnabled && !environment.SsrEnabled, "Default light settings disable costly screen-space effects");
                var clockPlayer = table.FindChildren("ClubClock", "Node3D", true, false).First().FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Single();
                Assert(!clockPlayer.IsPlaying(), "Reduced motion keeps the pendulum still");
                Assert(Enumerable.Range(0,teamSize*2).All(seat=>table.VisibleHandCount(seat)==3), "Every seat displays its dealt three-card fan");
                int pileCount = 0;
                bool pileCorrect = true;
                bool duplicatePlay = false;
                game.DeckShuffled += () => { played.Clear(); pileCount = 0; };
                game.CardPlayed += (_, _, round) =>
                {
                    seats.Add(game.LastPlayedSeatIndex);
                    pileCorrect &= table.PlayedCardCount == ++pileCount;
                    if (!played.Add($"{round}:{game.LastPlayedSeatIndex}")) duplicatePlay = true;
                };
                await FinishHand(game);
                Assert(seats.Count == teamSize * 2, "Every ally and opponent participates in the hand");
                // Advance dealer twice: the nearest ally then becomes the local human.
                game.StartNewHand(); game.CutDeck(); game.GivePena();
                Assert(Enumerable.Range(0,teamSize*2).All(seat=>table.VisibleHandCount(seat)==3), "Card fans are refilled for the next hand");
                await FinishHand(game);
                game.StartNewHand();
                Assert(game.CutterSeatIndex == (game.DealerSeatIndex - 1 + teamSize * 2) % (teamSize * 2), "Cutter rotates counter-clockwise with dealer");
                Assert(game.CutterIsPlayer == (game.CutterSeatIndex == 0), "CutterIsPlayer reflects whether Cutter is local seat 0");
                game.CutDeck(); game.GivePena();
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
                Assert(pileCorrect, "Every played card, including weaker team cards, remains in the physical pile");
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
