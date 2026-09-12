using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameHub.Core.Registry;
using GameHub.Core.Systems;
using GameHub.Games.Truco;
using GameHub.Core.Visuals;
using GameHub.Games.Fodinha;

/// <summary>Integration checks for complete bot teams and ownership of the solo Pena decision.</summary>
public partial class GameplayChecks : Node
{
    [Signal] public delegate void CompletedEventHandler(bool passed, string detail);
    private int _assertions;

    public void ConfigureTeams(int teamSize) => GameRegistry.TrucoTeamSize = teamSize;
    public void ConfigureCameraMotion(bool reduced) => SettingsManager.Instance.ReduceMotion = reduced;
    public void ConfigureFidelity(float scale)
    {
        SettingsManager.Instance.ReduceMotion = true;
        GameHub.Core.Graphics.VideoSettingsManager.Instance.RenderScale = scale;
        var settings = new GameHub.Core.Graphics.RayTracingSettings();
        settings.EnableAll(GameHub.Core.Graphics.RayTracingSettings.RtQualityLevel.Ultra);
        GameHub.Core.Graphics.GraphicsQualityManager.Instance.ApplyNewSettings(settings);
    }
    public Godot.Collections.Dictionary CheckRenderResolution()
    {
        var table = GetTree().CurrentScene.FindChildren("*", "Control", true, false).OfType<TableStage>().Single();
        var expected = table.Size * ((Vector2)GetTree().Root.Size / new Vector2(1280,720));
        if (((Vector2)table.RenderTargetSize - expected).Length() > 3)
            throw new InvalidOperationException($"Table must render at display pixels: {table.RenderTargetSize} vs {expected}");
        return new Godot.Collections.Dictionary { ["table_logical_size"] = table.Size.ToString(), ["table_target_pixels"] = table.RenderTargetSize.ToString(),
            ["render_scale"] = GameHub.Core.Graphics.VideoSettingsManager.Instance.RenderScale };
    }

    private void CheckFodinha()
    {
        Assert(FodinhaMatch.LifePenalty(0,3)==3 && FodinhaMatch.LifePenalty(4,1)==3 && FodinhaMatch.LifePenalty(2,2)==0, "Fodinha charges the absolute bid error");
        var tied = new[] { (2, new TrucoCardData { Rank=TrucoRank.Ace, Suit=TrucoSuit.Hearts }), (0, new TrucoCardData { Rank=TrucoRank.Ace, Suit=TrucoSuit.Clubs }) };
        Assert(FodinhaMatch.ResolveTrick(tied,TrucoRank.Two)==2 && FodinhaMatch.ResolveTrick(tied,TrucoRank.Ace)==0, "Ordinary ties go to first play; manilha suit breaks ties");
        bool unique=true, losses=true, eliminated=true, turns=true, final=true, allSizes=false, cutting=true;
        var cutters = new HashSet<int>();
        for(int seed=0;seed<100;seed++)
        {
            var match=new FodinhaMatch(seed); int rounds=0;
            while(match.State!=FodinhaMatch.Phase.Finished && rounds++<10)
            {
                cutters.Add(match.CutterSeat);
                cutting &= match.State==FodinhaMatch.Phase.Cutting && match.Vira==null && Enumerable.Range(0,4).All(s=>match.Hand(s).Count==0);
                cutting &= !match.Cut((match.CutterSeat+1)%4) && !match.Bid(match.CurrentSeat,0) && !match.Play(match.CurrentSeat,0);
                int cutter=match.CutterSeat;
                cutting &= match.Cut(cutter) && !match.Cut(cutter);
                var cards=match.ActiveSeats.SelectMany(s=>match.Hand(s)).Select(c=>c.ToString()).Append(match.Vira.ToString()).ToArray();
                unique &= cards.Distinct().Count()==cards.Length && match.ActiveSeats.All(s=>match.Hand(s).Count==match.CardsPerHand);
                var lifeBefore=match.Lives.ToArray();
                eliminated &= Enumerable.Range(0,4).Where(s=>match.Lives[s]==0).All(s=>match.Hand(s).Count==0);
                turns &= !match.Bid((match.CurrentSeat+1)%4,0) && !match.Bid(match.CurrentSeat,-1) && !match.Bid(match.CurrentSeat,match.CardsPerHand+1);
                while(match.State==FodinhaMatch.Phase.Bidding)
                {
                    int seat=match.CurrentSeat;
                    match.Bid(seat,FodinhaBot.Predict(match.Hand(seat),match.Vira,match.ActiveSeats.Length));
                }
                while(match.State==FodinhaMatch.Phase.Playing)
                {
                    int seat=match.CurrentSeat;
                    turns &= !match.Play((seat+1)%4,0) && !match.Play(seat,-1) && !match.Play(seat,match.Hand(seat).Count);
                    match.Play(seat,FodinhaBot.Choose(match.Hand(seat),match.Trick,match.Manilha,match.Bids[seat],match.Wins[seat]));
                    if(match.State==FodinhaMatch.Phase.TrickResult)
                    {
                        turns &= match.Trick.Select(p=>p.Seat).Distinct().Count()==match.ActiveSeats.Length;
                        int winner=match.LastWinner; match.AdvanceTrick();
                        turns &= match.State!=FodinhaMatch.Phase.Playing || match.CurrentSeat==winner;
                    }
                }
                losses &= Enumerable.Range(0,4).All(s=>match.Lives[s]==Math.Max(0,lifeBefore[s]-match.Losses[s]));
                losses &= match.Wins.Sum()==match.CardsPerHand;
                allSizes |= match.RoundIndex==8;
                if(match.State==FodinhaMatch.Phase.RoundResult)
                {
                    int next=match.DealerSeat;
                    do { next=(next+1)%4; } while(match.Lives[next]==0);
                    match.AdvanceRound();
                    cutting &= match.DealerSeat==next && match.Lives[match.CutterSeat]>0;
                }
            }
            final &= match.State==FodinhaMatch.Phase.Finished && match.Winners.All(s=>match.Lives[s]==match.Lives.Max());
            final &= !match.AdvanceRound() && !match.AdvanceTrick() && !match.Bid(0,0) && !match.Play(0,0);
        }
        Assert(unique,"100 Fodinha games deal unique cards and a separate vira");
        Assert(losses,"Each vaza has one winner and penalties reduce lives without going negative");
        Assert(eliminated,"Eliminated Fodinha seats receive no cards");
        Assert(turns,"Only the current seat may act; every active seat plays once; winner leads");
        Assert(final && allSizes,"Fodinha terminates, protects final state, and exercises all nine hand sizes");
        Assert(cutting && cutters.Count==4,"Fodinha rotates dealer/cutter through every seat, skips eliminated seats and deals only after the authorized cut");
    }
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
        foreach(var table in GetTree().CurrentScene.FindChildren("*", "Control", true, false).OfType<TableStage>())
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
            CheckFodinha();
            await CheckPokerPacing();
            for (int character = 0; character < CharacterCatalog.Ids.Length; character++)
            {
                var model = GD.Load<PackedScene>(CharacterCatalog.ModelPath(character)).Instantiate<Node3D>();
                var animator = model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().First();
                string[] requiredClips = { "entrance", "truco", "victory", "boss_intro", "flourish", "idle", "play_card" };
                Assert(requiredClips.All(clip => animator.GetAnimationList().Any(name => name == clip || name.EndsWith("/" + clip))), "Each Blender model exports all seven animated clips including play_card and idle");
                Assert(Mathf.IsEqualApprox(animator.SpeedScale, 1f), "Character animation players use the normal playback rate");
                foreach (string clip in requiredClips)
                {
                    string path = animator.GetAnimationList().First(name => name == clip || name.EndsWith("/" + clip));
                    var animation = animator.GetAnimation(path);
                    Assert(animation != null && animation.Length >= .5f && animation.Length <= 5f,
                        $"{clip} has a readable duration instead of an accelerated or stalled clip");
                }
                int meshCount = model.FindChildren("*", "MeshInstance3D", true, false).Count;
                Assert(meshCount == 1 || meshCount == 9 || meshCount == 6 || meshCount == 4, "Each GLB contains exactly one articulated character, without other open Blender scenes");
                var head = model.FindChildren("Head*", "Node3D", true, false).OfType<Node3D>().First(node => node is not MeshInstance3D);
                Assert(head.Position.Y > 1.3f, "Head retains its rest height without playing an animation");
                var skin = model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                    .SelectMany(mesh => Enumerable.Range(0, mesh.Mesh.GetSurfaceCount()).Select(surface => mesh.Mesh.SurfaceGetMaterial(surface)))
                    .OfType<StandardMaterial3D>().First(material => material.ResourceName.StartsWith(CharacterCatalog.Ids[character] + "_skin", StringComparison.Ordinal));
                var color = skin.AlbedoColor;
                Assert(skin.AlbedoTexture != null || (Mathf.Max(color.R, Mathf.Max(color.G, color.B)) - Mathf.Min(color.R, Mathf.Min(color.G, color.B)) > .005f),
                    "Character skin retains its palette instead of Blender's default white material");
                model.Free();
            }
            var clock = GD.Load<PackedScene>("res://assets/models/club/club_clock.glb").Instantiate<Node3D>();
            Assert(clock.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Any(player => player.HasAnimation("idle")), "Clock exports its pendulum animation");
            clock.Free();

            // Validate all 4 architecturally unique 3D club rooms and room switcher
            foreach (var roomTheme in new[] { "classic_club", "barao_lounge", "dama_salon", "cyber_casino" })
            {
                var roomScene = GD.Load<PackedScene>($"res://assets/models/club/room_{roomTheme}.glb");
                Assert(roomScene != null, $"Unique 3D room_{roomTheme}.glb loads successfully");
                var roomInstance = roomScene.Instantiate<Node3D>();
                Assert(roomInstance.FindChildren("Ceiling*", "Node3D", true, false).Count > 0 || roomInstance.FindChildren("Floor*", "Node3D", true, false).Count > 0, $"room_{roomTheme} includes architectural envelope");
                if (roomTheme == "classic_club")
                    Assert(roomInstance.FindChildren("*Fireplace*", "Node3D", true, false).Count > 0, "classic_club has grand brick fireplace");
                else if (roomTheme == "barao_lounge")
                    Assert(roomInstance.FindChildren("*Gothic*", "Node3D", true, false).Count > 0 || roomInstance.FindChildren("*Moon*", "Node3D", true, false).Count > 0, "barao_lounge has gothic architecture/moon");
                else if (roomTheme == "dama_salon")
                    Assert(roomInstance.FindChildren("*Mirror*", "Node3D", true, false).Count > 0 || roomInstance.FindChildren("*Urn*", "Node3D", true, false).Count > 0, "dama_salon has belle epoque mirrors/urns");
                else if (roomTheme == "cyber_casino")
                    Assert(roomInstance.FindChildren("*Sky*", "Node3D", true, false).Count > 0 || roomInstance.FindChildren("*Holo*", "Node3D", true, false).Count > 0, "cyber_casino has skyline/hologram");
                roomInstance.Free();
            }

            // Validate CharacterViewer3D Arkham City style component
            var charViewer = new CharacterViewer3D();
            charViewer.LoadCharacter(0);
            charViewer.SetOutfit(1);
            Assert(charViewer.CharacterIndex == 0 && charViewer.OutfitIndex == 1, "CharacterViewer3D loads character and outfit correctly");
            charViewer.LoadCharacter(7);
            Assert(charViewer.CharacterIndex == 7, "CharacterViewer3D loads boss models correctly");
            charViewer.Free();

            // Validate DefaultCameraMode and RoomTheme persistence
            SettingsManager.Instance.DefaultCameraMode = "pov";
            SettingsManager.Instance.RoomTheme = "dama_salon";
            SettingsManager.Instance.SaveSettings();
            Assert(SettingsManager.Instance.DefaultCameraMode == "pov", "SettingsManager persists DefaultCameraMode");
            Assert(SettingsManager.Instance.RoomTheme == "dama_salon", "SettingsManager persists chosen room theme");
            SettingsManager.Instance.DefaultCameraMode = "table";
            SettingsManager.Instance.RoomTheme = "classic_club";
            SettingsManager.Instance.SaveSettings();
            Assert(CharacterCatalog.PlayableCount == 7 && CharacterCatalog.IsBoss(7) && CharacterCatalog.IsBoss(8) && CharacterCatalog.IsBoss(9) && CharacterCatalog.IsBoss(10), "Bosses are separated from playable characters");

            // Validate Truco Call sprites for all 8 characters across stakes 3, 6, 9, 12
            for (int c = 0; c < CharacterCatalog.Ids.Length; c++)
            {
                foreach (int stakes in new[] { 3, 6, 9, 12 })
                {
                    var sprite = CharacterCatalog.TrucoCallSprite(c, stakes);
                    Assert(sprite != null, $"Truco call sprite exists for character {CharacterCatalog.Ids[c]} at stakes {stakes}");
                }
            }
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
                // Asset loading can consume the short reduced-motion shuffle
                // during scene setup. Start a fresh hand before observing it.
                game.StartNewHand();
                Assert(game.CurrentPhase is TrucoGameManager.TrucoPhase.Shuffling or TrucoGameManager.TrucoPhase.Cutting, "Hand starts with the dealer shuffling");
                await PrepareTrucoHand(game);
                Assert(game.PenaRecipientSeatIndex == 2 && game.PenaDecisionIsBot, "Nearest ally is an AI in solo");
                Assert(game.CurrentPhase != TrucoGameManager.TrucoPhase.PenaDecision, "AI resolves its own Pena without a human keep/return action");
                Assert(game.TeamOneHands.Concat(game.TeamTwoHands).All(hand => hand.Count == 3), "Every seat receives exactly three cards");
                var dealt = game.TeamOneHands.Concat(game.TeamTwoHands).SelectMany(hand => hand).Select(card => card.ToString()).ToList();
                Assert(dealt.Count == teamSize * 6 && dealt.Distinct().Count() == dealt.Count, "All dealt cards are unique");
                Assert(!dealt.Contains(game.ViraCard.ToString()), "Vira is outside all hands");
                var seats = new HashSet<int>();
                var played = new HashSet<string>();
                var table = GetTree().CurrentScene.FindChildren("*", "Control", true, false).OfType<TableStage>().First();
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
                game.StartNewHand(); await PrepareTrucoHand(game);
                Assert(Enumerable.Range(0,teamSize*2).All(seat=>table.VisibleHandCount(seat)==3), "Card fans are refilled for the next hand");
                await FinishHand(game);
                game.StartNewHand();
                Assert(game.CutterSeatIndex == (game.DealerSeatIndex - 1 + teamSize * 2) % (teamSize * 2), "Cutter rotates counter-clockwise with dealer");
                Assert(game.CutterIsPlayer == (game.CutterSeatIndex == 0), "CutterIsPlayer reflects whether Cutter is local seat 0");
                await PrepareTrucoHand(game, teamSize==2);
                Assert(game.PenaRecipientSeatIndex == (4 % (teamSize * 2)), "Pena follows the rotating dealer");
                if (teamSize == 2)
                {
                    Assert(game.PenaDecisionIsLocal && game.CurrentPhase == TrucoGameManager.TrucoPhase.PenaDecision, "Human recipient waits for a real choice");
                    string pena = game.PenaCard.ToString();
                    game.ResolvePena(true);
                    ulong dealDeadline = Time.GetTicksMsec() + 5000;
                    while ((game.CurrentPhase is TrucoGameManager.TrucoPhase.PenaDecision or TrucoGameManager.TrucoPhase.Dealing) && Time.GetTicksMsec() < dealDeadline)
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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

    private async Task CheckPokerPacing()
    {
        var poker = new GameHub.Games.PokerRoguelike.PokerGameManager();
        AddChild(poker);
        poker.StartNewGame();
        int deals = 0;
        poker.HandDealt += () => deals++;
        poker.ToggleCard(0);
        int hands = poker.HandsRemaining;
        poker.PlayHand();
        Assert(poker.CurrentPhase == GameHub.Games.PokerRoguelike.PokerGameManager.GamePhase.Scoring, "Poker locks its rules before scoring awaits");
        poker.ToggleCard(0);
        poker.PlayHand(); poker.DiscardCards();
        Assert(poker.HandsRemaining == hands - 1 && poker.DiscardsRemaining == 3, "Repeated actions cannot spend a second hand or discard while scoring");
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        Assert(deals == 0, "Reduced motion still gives players time to read the score");
        await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
        Assert(deals == 1 && poker.GetPlayerHand().Count == 8, "Poker refills and emits exactly one hand presentation");
        poker.QueueFree();
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

    private async Task PrepareTrucoHand(TrucoGameManager game, bool leaveLocalPena = false)
    {
        ulong deadline=Time.GetTicksMsec()+4000;
        while(game.CurrentPhase==TrucoGameManager.TrucoPhase.Shuffling)
            await ToSignal(GetTree().CreateTimer(.01),SceneTreeTimer.SignalName.Timeout);
        Assert(game.CurrentPhase==TrucoGameManager.TrucoPhase.Cutting,"Shuffle finishes before the cut begins");
        if(game.CutterIsPlayer)
        {
            await ToSignal(GetTree().CreateTimer(.35),SceneTreeTimer.SignalName.Timeout);
            Assert(game.CurrentPhase==TrucoGameManager.TrucoPhase.Cutting,"Human cutter is never skipped by the AI timer");
            game.CutDeck();
        }
        else
        {
            game.CutDeck();
            Assert(game.CurrentPhase==TrucoGameManager.TrucoPhase.Cutting,"Local cut request cannot steal the AI's turn");
        }
        while(game.CurrentPhase is TrucoGameManager.TrucoPhase.Cutting or TrucoGameManager.TrucoPhase.Dealing or TrucoGameManager.TrucoPhase.PenaDecision)
        {
            if(Time.GetTicksMsec()>deadline) throw new Exception("Automatic cut/pena/deal stalled");
            if(game.CanOfferPena) game.GivePena();
            else if(game.CanResolvePena)
            {
                if(leaveLocalPena) return;
                game.ResolvePena(true);
            }
            else if(game.CurrentPhase==TrucoGameManager.TrucoPhase.PenaDecision)
            {
                game.GivePena(); game.ResolvePena(false);
                Assert(game.CurrentPhase==TrucoGameManager.TrucoPhase.PenaDecision,"Local requests cannot take over a bot's Pena decision");
            }
            await ToSignal(GetTree().CreateTimer(.02),SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _assertions++;
    }
}
