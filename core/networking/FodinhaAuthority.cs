using System.Linq;
using GameHub.Games.Fodinha;

namespace GameHub.Core.Networking;

public sealed class FodinhaAuthority : IAuthoritativeRules
{
    private readonly FodinhaMatch _match = new(System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue));
    public string Phase => _match.State.ToString();
    public int Turn => _match.CurrentSeat;
    public bool Apply(int seat,string action,int value) => action switch {
        "cut" => _match.Cut(seat), "bid" => _match.Bid(seat,value), "play" => _match.Play(seat,value), _=>false };
    public bool Advance() => _match.State==FodinhaMatch.Phase.TrickResult ? _match.AdvanceTrick() : _match.AdvanceRound();
    public MatchView Project(int seat) => new() {
        Game="fodinha",Phase=Phase,YourSeat=seat,Turn=Turn,Dealer=_match.DealerSeat,Cutter=_match.CutterSeat,
        Round=_match.RoundIndex,Trick=_match.TricksCompleted,Vira=_match.Vira?.ToString()??"",
        Hand=_match.Hand(seat).Select(c=>c.ToString()).ToArray(),Counts=Enumerable.Range(0,4).Select(s=>_match.Hand(s).Count).ToArray(),
        Scores=_match.Lives.ToArray(),Bids=_match.Bids.ToArray(),Wins=_match.Wins.ToArray(),Winners=_match.Winners,
        Plays=_match.Trick.Select(p=>new PublicPlay{Seat=p.Seat,Card=p.Card.ToString()}).ToArray(),
        Stakes=_match.CardsPerHand,Actions=seat!=Turn ? new string[0] : Phase switch { "Cutting"=>new[]{"cut"},"Bidding"=>new[]{"bid"},"Playing"=>new[]{"play"},_=>new string[0] }
    };
    public (string,int) BotIntent() => Phase switch {
        "Cutting" => ("cut",0),
        "Bidding" => ("bid",FodinhaBot.Predict(_match.Hand(Turn),_match.Vira,_match.ActiveSeats.Length)),
        "Playing" => ("play",FodinhaBot.Choose(_match.Hand(Turn),_match.Trick,_match.Manilha,_match.Bids[Turn],_match.Wins[Turn])),
        _ => ("",0)
    };
}
