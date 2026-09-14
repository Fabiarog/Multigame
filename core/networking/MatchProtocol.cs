using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace GameHub.Core.Networking;

public sealed class MatchIntent
{
    public string MatchId { get; set; } = "";
    public long Sequence { get; set; }
    public long ActionId { get; set; }
    public string Action { get; set; } = "";
    public int Value { get; set; }
}
public sealed class PublicPlay { public int Seat { get; set; } public string Card { get; set; } = ""; }
public sealed class MatchView
{
    public int Version { get; set; } = 1;
    public string MatchId { get; set; } = "";
    public long Sequence { get; set; }
    public string Game { get; set; } = "";
    public string Phase { get; set; } = "";
    public int YourSeat { get; set; }
    public int Turn { get; set; }
    public int Dealer { get; set; }
    public int Cutter { get; set; }
    public int Round { get; set; }
    public int Trick { get; set; }
    public int Stakes { get; set; }
    public int PendingStakes { get; set; }
    public int Responder { get; set; } = -1;
    public string Vira { get; set; } = "";
    public string Pena { get; set; } = "";
    public string[] Names { get; set; } = Array.Empty<string>();
    public bool[] Bots { get; set; } = Array.Empty<bool>();
    public string[] Hand { get; set; } = Array.Empty<string>();
    public int[] Counts { get; set; } = Array.Empty<int>();
    public int[] Scores { get; set; } = Array.Empty<int>();
    public int[] Bids { get; set; } = Array.Empty<int>();
    public int[] Wins { get; set; } = Array.Empty<int>();
    public int[] Winners { get; set; } = Array.Empty<int>();
    public PublicPlay[] Plays { get; set; } = Array.Empty<PublicPlay>();
    public string[] Board { get; set; } = Array.Empty<string>();
    public bool[] Folded { get; set; } = Array.Empty<bool>();
    public int Pot { get; set; }
    public int ToCall { get; set; }
    public int MinRaiseTo { get; set; }
    public int MaxRaiseTo { get; set; }
    public string[] Actions { get; set; } = Array.Empty<string>();
}
public interface IAuthoritativeRules
{
    string Phase { get; }
    int Turn { get; }
    bool Apply(int seat, string action, int value);
    bool Advance();
    MatchView Project(int seat);
    (string Action, int Value) BotIntent();
}
public static class MatchProtocol
{
    public static readonly JsonSerializerOptions Json = new() { MaxDepth=12, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
    public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Json);
    public static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value, Json);
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static void Shuffle<T>(System.Collections.Generic.IList<T> deck)
    {
        for (int i=deck.Count-1;i>0;i--) { int j=RandomNumberGenerator.GetInt32(i+1); (deck[i],deck[j])=(deck[j],deck[i]); }
    }
}
