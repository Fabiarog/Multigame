using Godot;
using System.Collections.Generic;
namespace GameHub.Core.Networking;
public class PlayerInfo { public long Id {get;set;} public string Name {get;set;} public bool IsReady {get;set;} public int Score {get;set;} }
public partial class NetworkManager : Node
{
    public static NetworkManager Instance {get;private set;}
    public Dictionary<long,PlayerInfo> ConnectedPlayers {get;}=new();
    public PlayerInfo LocalPlayer {get;}=new(){Name="Jogador"};
    public bool Active {get;private set;}
    public bool Hosting {get;private set;}
    public string LastError {get;private set;}="";
    [Signal] public delegate void PlayerConnectedEventHandler(long peerId);
    [Signal] public delegate void PlayerDisconnectedEventHandler(long peerId);
    [Signal] public delegate void NetworkErrorEventHandler(string message);
    public override void _EnterTree(){Instance=this;}
    public override void _Ready()
    {
        Multiplayer.ConnectedToServer+=()=>{LocalPlayer.Id=Multiplayer.GetUniqueId();};
        Multiplayer.ConnectionFailed+=()=>{Active=false;Fail("Não foi possível conectar ao host.");};
        Multiplayer.ServerDisconnected+=()=>{Active=false;Hosting=false;Fail("O host encerrou a conexão. A partida foi interrompida.");};
        Multiplayer.PeerConnected+=id=>EmitSignal(SignalName.PlayerConnected,id);
        Multiplayer.PeerDisconnected+=id=>{ConnectedPlayers.Remove(id);EmitSignal(SignalName.PlayerDisconnected,id);};
    }
    private bool Fail(string message){LastError=message;EmitSignal(SignalName.NetworkError,message);return false;}
    public bool HostGame(int maxPlayers,int port=7070)
    {
        if(Active)return Fail("Já existe uma sessão. Saia dela antes de criar outra.");
        if(maxPlayers<2||maxPlayers>6||port<1||port>65535)return Fail("Configuração de rede inválida.");
        var peer=new ENetMultiplayerPeer();var error=peer.CreateServer(port,maxPlayers-1);
        if(error!=Error.Ok){peer.Dispose();return Fail($"Não foi possível abrir a porta UDP {port}: {error}.");}
        Multiplayer.MultiplayerPeer=peer;Active=true;Hosting=true;LastError="";LocalPlayer.Id=1;ConnectedPlayers[1]=LocalPlayer;return true;
    }
    public bool JoinGame(string address,int port=7070)
    {
        if(Active)return Fail("Já existe uma conexão. Saia dela antes de conectar novamente.");
        if(string.IsNullOrWhiteSpace(address)||address.Length>253||port<1||port>65535)return Fail("Endereço ou porta inválidos.");
        var peer=new ENetMultiplayerPeer();var error=peer.CreateClient(address.Trim(),port);
        if(error!=Error.Ok){peer.Dispose();return Fail($"Falha ao conectar: {error}.");}
        Multiplayer.MultiplayerPeer=peer;Active=true;Hosting=false;LastError="";return true;
    }
    public void Close(){Active=false;Hosting=false;ConnectedPlayers.Clear();LocalPlayer.Id=0;Multiplayer.MultiplayerPeer?.Close();Multiplayer.MultiplayerPeer=new OfflineMultiplayerPeer();}
}
