using System;
using System.Linq;
using System.Collections.Generic;
using GameHub.Games.Truco;

namespace GameHub.Core.Networking;

/// <summary>Server-only Truco rules. No timers, scene nodes or client RNG.</summary>
public sealed class TrucoAuthority : IAuthoritativeRules
{
    private readonly List<TrucoCardData>[] _hands;
    private List<TrucoCardData> _deck;
    private readonly List<PublicPlay> _public = new();
    private readonly List<(int Seat,TrucoCardData Card)> _trick = new();
    private readonly List<int> _results = new();
    private readonly int[] _scores = new int[2];
    private TrucoCardData _vira,_pena;
    private int _dealer,_round,_leader,_returnTurn,_caller=-1,_responder=-1,_pending,_stake=1,_winner=-1,_lastRaiser=-1;
    private int Seats=>_hands.Length;
    public string Phase { get; private set; }
    public int Turn { get; private set; }
    public TrucoAuthority(int seats)
    {
        if (seats is not (2 or 4 or 6)) throw new ArgumentOutOfRangeException(nameof(seats));
        _hands=Enumerable.Range(0,seats).Select(_=>new List<TrucoCardData>()).ToArray();Prepare();
    }
    private void Prepare()
    {
        foreach(var hand in _hands)hand.Clear();
        _deck=TrucoCardData.CreateDeck();MatchProtocol.Shuffle(_deck);
        _public.Clear();_trick.Clear();_results.Clear();_vira=null;_pena=null;_stake=1;_pending=0;_lastRaiser=-1;_caller=-1;_responder=-1;
        _leader=(_dealer+1)%Seats;Turn=(_dealer+Seats-1)%Seats;Phase="Shuffling";
    }
    private TrucoCardData Draw(){var c=_deck[0];_deck.RemoveAt(0);return c;}
    public bool Apply(int seat,string action,int value)
    {
        if (seat<0||seat>=Seats||seat!=Turn)return false;
        if(Phase=="Cutting"&&action=="cut")
        {
            int cut=System.Security.Cryptography.RandomNumberGenerator.GetInt32(4,_deck.Count-4);
            var top=_deck.Take(cut).ToArray();_deck.RemoveRange(0,cut);_deck.AddRange(top);
            if(Seats>2){_pena=Draw();Phase="PenaOffer";}else Deal();return true;
        }
        if(Phase=="PenaOffer"&&action=="offer")
        {
            Phase="PenaDecision";Turn=(_dealer+2)%Seats;return true;
        }
        if(Phase=="PenaDecision"&&action=="pena"&&value is 0 or 1)
        {
            if(value==1)_hands[seat].Add(_pena);else _vira=_pena;Deal();return true;
        }
        if(Phase=="Playing"&&action=="truco"&&_stake<12&&seat%2!=_lastRaiser)
        {
            _returnTurn=Turn;_caller=seat;_pending=NextStake(_stake);_responder=(seat+1)%Seats;Turn=_responder;Phase="TrucoResponse";return true;
        }
        if(Phase=="TrucoResponse")
        {
            if(action=="decline"){Award(_caller%2,_stake);return true;}
            if(action=="accept"||action=="raise"&&_pending<12)
            {
                _stake=_pending;_lastRaiser=_caller%2;
                if(action=="raise") { _caller=seat;_pending=NextStake(_stake);_responder=(seat+1)%Seats;Turn=_responder; }
                else { _pending=0;_responder=-1;Turn=_returnTurn;Phase="Playing"; }
                return true;
            }
            return false;
        }
        if(Phase!="Playing"||action!="play"||value<0||value>=_hands[seat].Count)return false;
        var card=_hands[seat][value];_hands[seat].RemoveAt(value);_trick.Add((seat,card));_public.Add(new PublicPlay{Seat=seat,Card=card.ToString()});
        if(_trick.Count<Seats)Turn=(seat+1)%Seats;else Resolve();
        return true;
    }
    private static int NextStake(int value)=>value==1?3:value+3;
    private void Deal()
    {
        for(int pass=0;pass<3;pass++)for(int offset=1;offset<=Seats;offset++){int seat=(_dealer+offset)%Seats;if(_hands[seat].Count<3)_hands[seat].Add(Draw());}
        _vira??=Draw();Turn=_leader;Phase="Dealing";
    }
    private void Resolve()
    {
        var manilha=TrucoCardData.GetManilhaRank(_vira.Rank);
        int best=_trick.Max(p=>p.Card.GetStrength(manilha));var highest=_trick.Where(p=>p.Card.GetStrength(manilha)==best).ToArray();
        int winner=highest.Select(p=>p.Seat%2).Distinct().Count()>1?2:highest[0].Seat%2;
        _results.Add(winner);if(winner!=2)_leader=highest[0].Seat;
        int decided=-1;
        if(_results.Count==2&&(_results[0]==2||winner==2||winner==_results[0]))decided=_results.FirstOrDefault(w=>w!=2, -1);
        if(_results.Count==3)
        {
            int a=_results.Count(w=>w==0),b=_results.Count(w=>w==1);
            decided=a>b?0:b>a?1:_results.FirstOrDefault(w=>w!=2,(_dealer+1)%2);
        }
        if(decided>=0)Award(decided,_stake);else Phase="TrickResult";
    }
    private void Award(int team,int points){_scores[team]+=points;_winner=team;Phase=_scores[team]>=12?"Finished":"RoundResult";}
    public bool Advance()
    {
        if(Phase=="Shuffling"){Phase="Cutting";return true;}
        if(Phase=="Dealing"){Phase="Playing";return true;}
        if(Phase=="TrickResult"){_public.Clear();_trick.Clear();Turn=_leader;Phase="Playing";return true;}
        if(Phase=="RoundResult"){_dealer=(_dealer+1)%Seats;_round++;Prepare();return true;}
        return false;
    }
    public MatchView Project(int seat)
    {
        string[] actions=seat!=Turn?Array.Empty<string>():Phase switch {
            "Cutting"=>new[]{"cut"},"PenaOffer"=>new[]{"offer"},"PenaDecision"=>new[]{"pena"},
            "TrucoResponse"=>_pending<12?new[]{"accept","decline","raise"}:new[]{"accept","decline"},
            "Playing"=>_stake<12&&seat%2!=_lastRaiser?new[]{"play","truco"}:new[]{"play"},_=>Array.Empty<string>()};
        return new MatchView{Game="truco",Phase=Phase,YourSeat=seat,Turn=Turn,Dealer=_dealer,Cutter=(_dealer+Seats-1)%Seats,Round=_round,Trick=_results.Count,
            Stakes=_stake,PendingStakes=_pending,Responder=_responder,Vira=_vira?.ToString()??"",Pena=Phase=="PenaDecision"&&seat==Turn?_pena.ToString():"",
            Hand=_hands[seat].Select(c=>c.ToString()).ToArray(),Counts=_hands.Select(h=>h.Count).ToArray(),Scores=(int[])_scores.Clone(),
            Winners=Phase is "RoundResult" or "Finished"?new[]{_winner}:Array.Empty<int>(),Plays=_public.ToArray(),Actions=actions};
    }
    public (string,int) BotIntent()=>Phase switch {"Cutting"=>("cut",0),"PenaOffer"=>("offer",0),"PenaDecision"=>("pena",(int)_pena.Rank>=(int)TrucoRank.Ace?1:0),"TrucoResponse"=>("accept",0),"Playing"=>("play",0),_=>("",0)};
}
