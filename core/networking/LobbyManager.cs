using Godot;
using System;
using System.Linq;
using GameHub.Core.Systems;
namespace GameHub.Core.Networking;
public partial class LobbyManager : Node
{
    public static LobbyManager Instance {get;private set;}
    public LobbyState CurrentLobby {get;private set;}=new();
    public bool IsHost=>NetworkManager.Instance?.Active==true&&NetworkManager.Instance.Hosting&&Multiplayer.IsServer();
    public string Status {get;private set;}="";
    [Signal] public delegate void LobbyUpdatedEventHandler();
    [Signal] public delegate void MatchStartingEventHandler(string gameScenePath);
    public override void _EnterTree(){Instance=this;}
    public override void _Ready()
    {
        Multiplayer.PeerConnected+=OnPeerConnected;Multiplayer.PeerDisconnected+=OnPeerDisconnected;
        Multiplayer.ConnectedToServer+=()=>RpcId(1,MethodName.Hello,SettingsManager.Instance?.PlayerNickname??"Jogador");
        NetworkManager.Instance.NetworkError+=message=>{Status=message;EmitSignal(SignalName.LobbyUpdated);};
    }
    public void HostLobby(string gameId,int maxPlayers,LobbyState.TurnMode turnMode,int targetScore,string scenarioId)=>HostAt(gameId,maxPlayers,7070);
    public bool HostAt(string gameId,int seats,int port)
    {
        if(gameId is not ("truco" or "fodinha" or "poker_classic")){Status="O pôquer roguelike é solo. Escolha Texas Hold’em para multiplayer.";EmitSignal(SignalName.LobbyUpdated);return false;}
        if(gameId=="fodinha")seats=4;
        if(gameId=="truco"&&seats is not (2 or 4 or 6))return false;
        if(!NetworkManager.Instance.HostGame(seats,port)){Status=NetworkManager.Instance.LastError;EmitSignal(SignalName.LobbyUpdated);return false;}
        CurrentLobby=new LobbyState{SelectedGameId=gameId,MaxPlayers=seats,TargetScore=gameId=="truco"?12:gameId=="fodinha"?5:1000,SelectedTurnMode=LobbyState.TurnMode.Sequential,SelectedScenarioId="classic_club"};
        CurrentLobby.AddPlayer(1,CleanName(SettingsManager.Instance?.PlayerNickname??"Host"));CurrentLobby.PlayerSlots[1].Team=1;
        Status="Sala criada. Aguarde os participantes e marque Pronto.";
        LanDiscovery.Instance?.StartBroadcasting(CurrentLobby.PlayerSlots[1].PlayerName,gameId,1,seats);Sync();return true;
    }
    public void JoinLobby(string address)
    {
        int port=7070;string host=address.Trim();int colon=host.LastIndexOf(':');
        if(colon>0&&host.IndexOf(':')==colon){if(!int.TryParse(host[(colon+1)..],out port)){Status="Porta inválida.";EmitSignal(SignalName.LobbyUpdated);return;}host=host[..colon];}
        CurrentLobby=new LobbyState();Status="Conectando...";NetworkManager.Instance.JoinGame(host,port);EmitSignal(SignalName.LobbyUpdated);
    }
    private static string CleanName(string name)=>new string((name??"Jogador").Where(c=>!char.IsControl(c)).Take(24).ToArray());
    private void OnPeerConnected(long id)
    {
        if(!IsHost)return;
        if(AuthoritativeMatch.Instance.Active||CurrentLobby.PlayerSlots.Count>=CurrentLobby.MaxPlayers){Multiplayer.MultiplayerPeer.DisconnectPeer((int)id);return;}
        CurrentLobby.AddPlayer(id,$"Jogador {CurrentLobby.PlayerSlots.Count+1}");BalanceTeams();Sync();
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Hello(string name)
    {
        if(!IsHost||AuthoritativeMatch.Instance.Active||name==null||name.Length>96)return;
        long id=Multiplayer.GetRemoteSenderId();if(!CurrentLobby.PlayerSlots.TryGetValue(id,out var player))return;
        player.PlayerName=CleanName(name);Sync();
    }
    public void ToggleReady(){if(IsHost)SetReadyFor(1);else if(NetworkManager.Instance.Active)RpcId(1,MethodName.ToggleReadyIntent);}
    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ToggleReadyIntent(){if(IsHost)SetReadyFor(Multiplayer.GetRemoteSenderId());}
    private void SetReadyFor(long id){if(AuthoritativeMatch.Instance.Active||!CurrentLobby.PlayerSlots.TryGetValue(id,out var slot))return;slot.IsReady=!slot.IsReady;Sync();}
    public void SetTeamAssignment(LobbyState.TeamAssignmentMode mode){if(!IsHost||AuthoritativeMatch.Instance.Active)return;CurrentLobby.TeamAssignment=mode;if(mode==LobbyState.TeamAssignmentMode.Random)BalanceTeams();Sync();}
    public void SetPlayerTeam(long id,int team){if(!IsHost||AuthoritativeMatch.Instance.Active||team is not (1 or 2))return;if(CurrentLobby.PlayerSlots.TryGetValue(id,out var slot))slot.Team=team;Sync();}
    private void BalanceTeams(){int index=0;foreach(var slot in CurrentLobby.PlayerSlots.Values.OrderBy(s=>s.PeerId==1?long.MinValue:s.PeerId))slot.Team=index++%2+1;}
    private void Sync()
    {
        if(!IsHost)return;
        string json=MatchProtocol.Encode(new LobbyWire{Game=CurrentLobby.SelectedGameId,Seats=CurrentLobby.MaxPlayers,TeamMode=(int)CurrentLobby.TeamAssignment,
            Players=CurrentLobby.PlayerSlots.Values.OrderBy(s=>s.PeerId).Select(s=>new LobbyPeer{Id=s.PeerId,Name=s.PlayerName,Ready=s.IsReady,Team=s.Team}).ToArray()});
        Rpc(MethodName.ReceiveLobby,json);EmitSignal(SignalName.LobbyUpdated);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode=MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveLobby(string json)
    {
        if(json.Length>8192)return;var data=MatchProtocol.Decode<LobbyWire>(json);
        CurrentLobby=new LobbyState{SelectedGameId=data.Game,MaxPlayers=data.Seats,TeamAssignment=(LobbyState.TeamAssignmentMode)data.TeamMode};
        foreach(var player in data.Players){CurrentLobby.AddPlayer(player.Id,player.Name);var slot=CurrentLobby.PlayerSlots[player.Id];slot.Team=player.Team;slot.IsReady=player.Ready;}
        Status="Conectado. Marque Pronto para iniciar.";EmitSignal(SignalName.LobbyUpdated);
    }
    public void TryStartMatch()
    {
        if(!IsHost||AuthoritativeMatch.Instance.Active)return;
        if(!CurrentLobby.AreAllPlayersReady()){Status="São necessários pelo menos dois jogadores, todos prontos.";EmitSignal(SignalName.LobbyUpdated);return;}
        var players=CurrentLobby.PlayerSlots.Values.OrderBy(s=>s.PeerId==1?long.MinValue:s.PeerId).ToList();
        if(CurrentLobby.SelectedGameId=="truco"&&players.GroupBy(s=>s.Team).Any(g=>g.Key is not (1 or 2)||g.Count()>CurrentLobby.MaxPlayers/2))
        {Status="Equilibre as duas equipes antes de iniciar.";EmitSignal(SignalName.LobbyUpdated);return;}
        long[] peers=new long[CurrentLobby.MaxPlayers];string[] names=new string[peers.Length];
        for(int s=0;s<peers.Length;s++)
        {
            var player=players.FirstOrDefault(p=>CurrentLobby.SelectedGameId!="truco"||p.Team==s%2+1);
            if(player!=null){peers[s]=player.PeerId;names[s]=player.PlayerName;players.Remove(player);}else names[s]=$"Bot {s+1}";
        }
        LanDiscovery.Instance?.StopBroadcasting();AuthoritativeMatch.Instance.StartAuthority(CurrentLobby.SelectedGameId,peers,names);
    }
    public void EnterMatchScene(){EmitSignal(SignalName.MatchStarting,"res://core/networking/NetworkTable.tscn");}
    private void OnPeerDisconnected(long id){if(!IsHost)return;AuthoritativeMatch.Instance.PeerLeft(id);CurrentLobby.RemovePlayer(id);Sync();}
    public void LeaveLobby(){AuthoritativeMatch.Instance?.Reset();LanDiscovery.Instance?.StopBroadcasting();LanDiscovery.Instance?.StopListening();NetworkManager.Instance.Close();CurrentLobby=new LobbyState();Status="Desconectado.";EmitSignal(SignalName.LobbyUpdated);}
}
public sealed class LobbyWire {public string Game{get;set;} public int Seats{get;set;} public int TeamMode{get;set;} public LobbyPeer[] Players{get;set;}}
public sealed class LobbyPeer {public long Id{get;set;} public string Name{get;set;} public bool Ready{get;set;} public int Team{get;set;}}
