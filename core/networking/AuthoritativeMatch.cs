using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace GameHub.Core.Networking;
public partial class AuthoritativeMatch : Node
{
    public static AuthoritativeMatch Instance {get;private set;}
    public bool Active {get;private set;}
    public MatchView LocalView {get;private set;}
    public string LocalHash {get;private set;}="";
    public string Notice {get;private set;}="";
    public int Rejected {get;private set;}
    public int Divergences {get;private set;}
    public int Acknowledged {get;private set;}
    private IAuthoritativeRules _rules;
    private long[] _peers;
    private string[] _names;
    private string _matchId;
    private long _sequence,_actionId;
    private double _wait,_heartbeat,_loadWait;
    private readonly HashSet<long> _loading=new();
    private readonly Dictionary<long,long> _lastAction=new();
    private readonly Dictionary<long,(ulong Window,int Count)> _rates=new();
    private readonly Dictionary<long,Dictionary<long,string>> _sent=new();
    [Signal] public delegate void ViewUpdatedEventHandler();
    public override void _EnterTree(){Instance=this;}
    public override void _Ready(){Multiplayer.ServerDisconnected+=()=>{Active=false;Notice="O host saiu. A partida foi interrompida.";EmitSignal(SignalName.ViewUpdated);};}
    private bool IsHost=>NetworkManager.Instance.Hosting&&Multiplayer.IsServer();
    private double Step=>OS.HasEnvironment("MULTIGAME_NET_QA")?.045:1.1;
    public void StartAuthority(string game,long[] peers,string[] names)
    {
        if(!IsHost||Active||peers.Length!=names.Length||peers.Count(p=>p>0)<2)return;
        _rules=game switch{"truco"=>new TrucoAuthority(peers.Length),"fodinha"=>new FodinhaAuthority(),"poker_classic"=>new HoldemAuthority(peers.Length),_=>throw new ArgumentException("Unsupported game")};
        _matchId=Guid.NewGuid().ToString("N");_peers=(long[])peers.Clone();_names=(string[])names.Clone();_sequence=0;_actionId=0;
        _loading.Clear();foreach(long id in peers.Where(p=>p>0))_loading.Add(id);_loadWait=15;_wait=Step;Active=true;
        Rpc(MethodName.StartClient,_matchId);StartLocalScene();Publish();
    }
    private void StartLocalScene(){if(!OS.HasEnvironment("MULTIGAME_NET_QA"))LobbyManager.Instance.EnterMatchScene();}
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartClient(string id){if(id.Length!=32)return;Reset();_matchId=id;Active=true;StartLocalScene();}
    public void Loaded(){if(!Active)return;if(IsHost)MarkLoaded(1);else RpcId(1,MethodName.LoadedIntent,_matchId);}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void LoadedIntent(string id){if(IsHost&&Active&&id==_matchId)MarkLoaded(Multiplayer.GetRemoteSenderId());}
    private void MarkLoaded(long id){if(_loading.Remove(id))Publish();}
    public void SendIntent(string action,int value=0)
    {
        if(!Active||LocalView==null)return;
        string json=MatchProtocol.Encode(new MatchIntent{MatchId=_matchId,Sequence=LocalView.Sequence,ActionId=++_actionId,Action=action,Value=value});
        if(IsHost)Handle(1,json);else RpcId(1,MethodName.SubmitIntent,json);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SubmitIntent(string json){if(IsHost)Handle(Multiplayer.GetRemoteSenderId(),json);}
    private bool Rate(long id)
    {
        ulong now=Time.GetTicksMsec();var r=_rates.GetValueOrDefault(id);if(now-r.Window>=1000)r=(now,0);
        r.Count++;_rates[id]=r;return r.Count<=20;
    }
    private void Handle(long peer,string json)
    {
        if(!Active||!IsHost||Array.IndexOf(_peers,peer)<0)return;
        if(!Rate(peer)){Rejected++;return;}
        if(json==null||json.Length>1024){Reject(peer,"Formato inválido.");return;}
        MatchIntent intent;
        try{intent=MatchProtocol.Decode<MatchIntent>(json);}catch(JsonException){Reject(peer,"Formato inválido.");return;}
        long previous=_lastAction.GetValueOrDefault(peer);
        if(intent==null||intent.MatchId!=_matchId||intent.ActionId<=previous||intent.ActionId>previous+100||intent.Action==null||intent.Action.Length>16)
        {Reject(peer,"Ação duplicada ou sessão inválida.");return;}
        _lastAction[peer]=intent.ActionId;
        if(intent.Sequence!=_sequence||_loading.Count>0||_wait>0){Reject(peer,"Aguarde a atualização da mesa.");return;}
        int seat=Array.IndexOf(_peers,peer);
        if(!_rules.Apply(seat,intent.Action,intent.Value)){Reject(peer,"Ação não permitida nesta fase ou turno.");return;}
        _wait=Step;Publish();
    }
    private void Reject(long peer,string message)
    {
        Rejected++;SendProjection(peer);GD.Print($"NET_REJECT peer={peer} seq={_sequence} reason={message}");
        if(peer==1){Notice=message;EmitSignal(SignalName.ViewUpdated);}else RpcId(peer,MethodName.Rejection,message);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Rejection(string message){Notice=message;EmitSignal(SignalName.ViewUpdated);}
    private void Publish(){_sequence++;foreach(long peer in _peers.Where(p=>p>0).ToArray())SendProjection(peer);}
    private void SendProjection(long peer)
    {
        int seat=Array.IndexOf(_peers,peer);if(seat<0)return;
        var view=_rules.Project(seat);view.Room=LobbyManager.Instance.CurrentLobby.SelectedScenarioId;view.MatchId=_matchId;view.Sequence=_sequence;view.Names=(string[])_names.Clone();view.Bots=_peers.Select(p=>p==0).ToArray();
        if(_loading.Count>0){view.Phase="Loading";view.Actions=Array.Empty<string>();}else if(_wait>0)view.Actions=Array.Empty<string>();
        string json=MatchProtocol.Encode(view),hash=MatchProtocol.Hash(json);
        if(!_sent.TryGetValue(peer,out var history))_sent[peer]=history=new();history[_sequence]=hash;
        foreach(long old in history.Keys.Where(s=>s<_sequence-64).ToArray())history.Remove(old);
        if(peer==1)AcceptProjection(json,hash);else RpcId(peer,MethodName.ReceiveProjection,json,hash);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveProjection(string json,string hash)
    {
        if(!Active||json.Length>32768||MatchProtocol.Hash(json)!=hash){Divergences++;Notice="Estado inconsistente. Aguardando sincronização.";return;}
        AcceptProjection(json,hash);if(LocalView!=null)RpcId(1,MethodName.Acknowledge,_matchId,LocalView.Sequence,LocalHash);
    }
    private void AcceptProjection(string json,string hash)
    {
        var view=MatchProtocol.Decode<MatchView>(json);
        if(view.Version!=1||view.MatchId!=_matchId||LocalView!=null&&view.Sequence<LocalView.Sequence)return;
        LocalView=view;LocalHash=hash;Notice="";EmitSignal(SignalName.ViewUpdated);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Acknowledge(string id,long sequence,string hash)
    {
        if(!IsHost||!Active||id!=_matchId)return;long peer=Multiplayer.GetRemoteSenderId();
        if(!_sent.TryGetValue(peer,out var history)||!history.TryGetValue(sequence,out string expected))return;
        if(hash!=expected){Divergences++;GD.Print($"NET_DESYNC peer={peer} seq={sequence}");SendProjection(peer);}else Acknowledged++;
    }
    public void PeerLeft(long peer)
    {
        if(!Active||!IsHost)return;int seat=Array.IndexOf(_peers,peer);if(seat<0)return;
        _peers[seat]=0;_names[seat]+=" · bot";_loading.Remove(peer);_lastAction.Remove(peer);_sent.Remove(peer);_rates.Remove(peer);_wait=Step;
        GD.Print($"NET_TAKEOVER seat={seat}");Publish();
    }
    public override void _Process(double delta)
    {
        if(!Active||!IsHost)return;
        if(_loading.Count>0)
        {
            _loadWait-=delta;if(_loadWait<=0)foreach(long peer in _loading.ToArray()){if(peer==1)MarkLoaded(1);else{PeerLeft(peer);Multiplayer.MultiplayerPeer.DisconnectPeer((int)peer);}}return;
        }
        if(_wait>0){_wait-=delta;if(_wait<=0)Publish();return;}
        if(_rules.Advance()){_wait=Step;Publish();return;}
        int turn=_rules.Turn;
        if(turn>=0&&turn<_peers.Length&&_peers[turn]==0)
        {
            var intent=_rules.BotIntent();if(_rules.Apply(turn,intent.Action,intent.Value)){_wait=Step;Publish();return;}
        }
        _heartbeat+=delta;if(_heartbeat>=2){_heartbeat=0;foreach(long peer in _peers.Where(p=>p>0))SendProjection(peer);}
    }
    public void Reset(){Active=false;LocalView=null;LocalHash="";_rules=null;_peers=null;_loading.Clear();_lastAction.Clear();_rates.Clear();_sent.Clear();_actionId=0;_sequence=0;_heartbeat=0;Notice="";}
}
