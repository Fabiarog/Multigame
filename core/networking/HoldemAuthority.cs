using System;
using System.Linq;
using System.Collections.Generic;

namespace GameHub.Core.Networking;

/// <summary>No-limit Hold'em, fictitious chips. Authority owns all hole cards and deck.</summary>
public sealed class HoldemAuthority : IAuthoritativeRules
{
    private readonly int[] _stack,_bet,_paid;
    private readonly bool[] _folded,_acted;
    private readonly List<int>[] _hands;
    private List<int> _deck=new(),_board=new();
    private readonly HashSet<int> _needs=new();
    private int _dealer=-1,_round=-1,_currentBet,_raiseSize=20;
    private int[] _winners=Array.Empty<int>();
    private bool _showdown;
    public string Phase {get;private set;}
    public int Turn {get;private set;}
    private int Seats=>_stack.Length;
    public HoldemAuthority(int seats,int chips=1000)
    {
        if(seats<2||seats>6||chips<20)throw new ArgumentOutOfRangeException();
        _stack=Enumerable.Repeat(chips,seats).ToArray();_bet=new int[seats];_paid=new int[seats];_folded=new bool[seats];_acted=new bool[seats];
        _hands=Enumerable.Range(0,seats).Select(_=>new List<int>()).ToArray();Prepare();
    }
    private int Next(int seat,Func<int,bool> predicate){for(int i=1;i<=Seats;i++){int s=(seat+i+Seats)%Seats;if(predicate(s))return s;}return -1;}
    private bool CanAct(int s)=>!_folded[s]&&_stack[s]>0;
    private int Draw(){int c=_deck[^1];_deck.RemoveAt(_deck.Count-1);return c;}
    private void Pay(int seat,int amount){amount=Math.Min(amount,_stack[seat]);_stack[seat]-=amount;_paid[seat]+=amount;_bet[seat]+=amount;}
    private void Prepare()
    {
        if(_stack.Count(s=>s>0)<2){Phase="Finished";_winners=Enumerable.Range(0,Seats).Where(s=>_stack[s]>0).ToArray();return;}
        _round++;_dealer=Next(_dealer,s=>_stack[s]>0);_board.Clear();_winners=Array.Empty<int>();_showdown=false;
        Array.Clear(_bet);Array.Clear(_paid);Array.Clear(_acted);_raiseSize=20;_currentBet=20;
        _deck=Enumerable.Range(0,52).ToList();MatchProtocol.Shuffle(_deck);
        for(int s=0;s<Seats;s++){_hands[s].Clear();_folded[s]=_stack[s]==0;}
        for(int i=0;i<2;i++)for(int offset=1;offset<=Seats;offset++){int s=(_dealer+offset)%Seats;if(!_folded[s])_hands[s].Add(Draw());}
        int small=_stack.Count(s=>s>0)==2?_dealer:Next(_dealer,s=>!_folded[s]);int big=Next(small,s=>!_folded[s]);
        Pay(small,10);Pay(big,20);_needs.Clear();for(int s=0;s<Seats;s++)if(CanAct(s))_needs.Add(s);
        Turn=Next(big,s=>_needs.Contains(s));Phase="Dealing";
    }
    public bool Apply(int seat,string action,int value)
    {
        if(Phase!="Betting"||seat!=Turn||seat<0||seat>=Seats||!CanAct(seat))return false;
        int due=Math.Max(0,_currentBet-_bet[seat]);
        if(action=="fold")_folded[seat]=true;
        else if(action=="check"&&due==0){}
        else if(action=="call"&&due>0)Pay(seat,due);
        else if(action=="raise"&&!_acted[seat]&&value>_currentBet&&value<=_bet[seat]+_stack[seat])
        {
            int delta=value-_currentBet;
            if(delta<_raiseSize&&value!=_bet[seat]+_stack[seat])return false;
            if(delta>=_raiseSize){_raiseSize=delta;Array.Clear(_acted);}
            Pay(seat,value-_bet[seat]);_currentBet=value;
        }
        else return false;
        _acted[seat]=true;_needs.Remove(seat);
        for(int s=0;s<Seats;s++)if(CanAct(s)&&s!=seat&&(_bet[s]<_currentBet||!_acted[s]))_needs.Add(s);
        _needs.RemoveWhere(s=>!CanAct(s));
        if(_folded.Count(f=>!f)==1){Settle();return true;}
        FinishBettingOrChoose(seat);return true;
    }
    private void FinishBettingOrChoose(int previous)
    {
        var can=Enumerable.Range(0,Seats).Where(CanAct).ToArray();
        if(can.Length<=1&&can.All(s=>_bet[s]>=_currentBet))_needs.Clear();
        if(_needs.Count==0){if(_board.Count==5)Settle();else Phase="StreetResult";}
        else Turn=Next(previous,s=>_needs.Contains(s));
    }
    public bool Advance()
    {
        if(Phase=="Dealing"){Phase="Betting";if(Turn<0)Phase="StreetResult";else FinishBettingOrChoose((Turn+Seats-1)%Seats);return true;}
        if(Phase=="StreetResult")
        {
            Draw();int cards=_board.Count==0?3:1;for(int i=0;i<cards;i++)_board.Add(Draw());
            Array.Clear(_bet);Array.Clear(_acted);_currentBet=0;_raiseSize=20;_needs.Clear();
            for(int s=0;s<Seats;s++)if(CanAct(s))_needs.Add(s);
            Phase="Betting";FinishBettingOrChoose(_dealer);return true;
        }
        if(Phase=="RoundResult"){Prepare();return true;}
        return false;
    }
    private void Settle()
    {
        var live=Enumerable.Range(0,Seats).Where(s=>!_folded[s]).ToArray();_showdown=live.Length>1;
        var won=new HashSet<int>();int previous=0;
        foreach(int level in _paid.Where(p=>p>0).Distinct().Order())
        {
            var contributors=Enumerable.Range(0,Seats).Where(s=>_paid[s]>=level).ToArray();int pot=(level-previous)*contributors.Length;previous=level;
            var eligible=live.Where(s=>_paid[s]>=level).ToArray();
            if(eligible.Length==0){foreach(int s in contributors)_stack[s]+=pot/contributors.Length;continue;}
            long best=eligible.Max(s=>_showdown?Evaluate(_hands[s].Concat(_board).ToArray()):0);
            var winners=eligible.Where(s=>(!_showdown?0:Evaluate(_hands[s].Concat(_board).ToArray()))==best).OrderBy(s=>(s-_dealer+Seats-1)%Seats).ToArray();
            for(int i=0;i<winners.Length;i++){_stack[winners[i]]+=pot/winners.Length+(i<pot%winners.Length?1:0);won.Add(winners[i]);}
        }
        _winners=won.Order().ToArray();Array.Clear(_paid);Array.Clear(_bet);Phase="RoundResult";
    }
    public static string Card(int c)=> ((c%13+2) switch{14=>"A",13=>"K",12=>"Q",11=>"J",var rank=>rank.ToString()})+new[]{"♦","♠","♥","♣"}[c/13];
    public static long Evaluate(int[] cards)
    {
        if(cards.Length<5||cards.Length>7||cards.Distinct().Count()!=cards.Length)throw new ArgumentException("Expected distinct 5–7 cards");
        long best=0;
        for(int a=0;a<cards.Length-4;a++)for(int b=a+1;b<cards.Length-3;b++)for(int c=b+1;c<cards.Length-2;c++)for(int d=c+1;d<cards.Length-1;d++)for(int e=d+1;e<cards.Length;e++)
            best=Math.Max(best,Five(new[]{cards[a],cards[b],cards[c],cards[d],cards[e]}));
        return best;
    }
    private static long Five(int[] cards)
    {
        var ranks=cards.Select(c=>c%13+2).OrderDescending().ToArray();var groups=ranks.GroupBy(r=>r).OrderByDescending(g=>g.Count()).ThenByDescending(g=>g.Key).ToArray();
        bool flush=cards.All(c=>c/13==cards[0]/13);int straight=ranks.Distinct().Count()==5&&ranks[0]-ranks[4]==4?ranks[0]:ranks.SequenceEqual(new[]{14,5,4,3,2})?5:0;
        int category;int[] kickers;
        if(flush&&straight>0){category=8;kickers=new[]{straight};}
        else if(groups[0].Count()==4){category=7;kickers=groups.Select(g=>g.Key).ToArray();}
        else if(groups[0].Count()==3&&groups[1].Count()==2){category=6;kickers=groups.Select(g=>g.Key).ToArray();}
        else if(flush){category=5;kickers=ranks;}
        else if(straight>0){category=4;kickers=new[]{straight};}
        else {category=groups[0].Count()==3?3:groups.Count(g=>g.Count()==2)==2?2:groups[0].Count()==2?1:0;kickers=groups.Select(g=>g.Key).ToArray();}
        long value=category;for(int i=0;i<5;i++)value=value*15+(i<kickers.Length?kickers[i]:0);return value;
    }
    public MatchView Project(int seat)
    {
        var actions=new List<string>();int due=Math.Max(0,_currentBet-_bet[seat]);
        if(Phase=="Betting"&&seat==Turn&&CanAct(seat)){actions.Add("fold");actions.Add(due==0?"check":"call");if(!_acted[seat]&&_stack[seat]>due)actions.Add("raise");}
        return new MatchView{Game="poker_classic",Phase=Phase,YourSeat=seat,Turn=Turn,Dealer=_dealer,Cutter=-1,Round=_round,Trick=_board.Count,Stakes=20,
            Hand=_hands[seat].Select(Card).ToArray(),Counts=_hands.Select(h=>h.Count).ToArray(),Scores=(int[])_stack.Clone(),Folded=(bool[])_folded.Clone(),Board=_board.Select(Card).ToArray(),
            Pot=_paid.Sum(),ToCall=Math.Min(due,_stack[seat]),MinRaiseTo=_currentBet+_raiseSize,MaxRaiseTo=_bet[seat]+_stack[seat],Actions=actions.ToArray(),Winners=_winners,
            Plays=_showdown?Enumerable.Range(0,Seats).Where(s=>!_folded[s]).SelectMany(s=>_hands[s].Select(c=>new PublicPlay{Seat=s,Card=Card(c)})).ToArray():Array.Empty<PublicPlay>()};
    }
    public (string,int) BotIntent(){int due=Math.Max(0,_currentBet-_bet[Turn]);return (due==0?"check":"call",0);}
}
