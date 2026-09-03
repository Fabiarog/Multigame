using Godot;
using System.Collections.Generic;
using GameHub.Core.AI;

namespace GameHub.Core.AI;

/// <summary>
/// Manages a tutorial match with bots.
/// Steps the player through gameplay mechanics with highlighted UI hints.
/// </summary>
public partial class TutorialController : Node
{
    public enum TutorialStep
    {
        Welcome,
        ExplainHand,
        ExplainPlayingCards,
        ExplainScoring,
        ExplainMultipliers,
        ExplainRoguelikeChoices,
        ExplainShop,
        FreePlay,
        Complete
    }

    [Signal]
    public delegate void TutorialStepChangedEventHandler(int step, string title, string description);

    [Signal]
    public delegate void TutorialCompleteEventHandler();

    public TutorialStep CurrentStep { get; private set; } = TutorialStep.Welcome;

    // Tutorial hint data (localization keys in a real build)
    private static readonly Dictionary<TutorialStep, (string Title, string Desc)> STEP_DATA = new()
    {
        { TutorialStep.Welcome, (
            "Bem-vindo ao Poker Roguelike!",
            "Neste tutorial, você vai aprender a jogar contra bots. Clique para continuar."
        )},
        { TutorialStep.ExplainHand, (
            "Sua Mão",
            "Estas são as cartas na sua mão. Você pode selecionar até 5 cartas para formar uma combinação de poker."
        )},
        { TutorialStep.ExplainPlayingCards, (
            "Jogando Cartas",
            "Selecione as cartas que deseja jogar e clique em 'Jogar Mão'. A combinação será avaliada automaticamente."
        )},
        { TutorialStep.ExplainScoring, (
            "Pontuação",
            "Cada combinação vale uma pontuação base. Par = 10, Dois Pares = 20, Trinca = 30, Sequência = 40, Flush = 50, Full House = 60, Quadra = 80, Straight Flush = 100, Royal Flush = 150."
        )},
        { TutorialStep.ExplainMultipliers, (
            "Multiplicadores",
            "Modificadores e relíquias podem multiplicar sua pontuação. Fique atento aos multiplicadores ativos!"
        )},
        { TutorialStep.ExplainRoguelikeChoices, (
            "Escolhas Roguelike",
            "Após cada rodada, você poderá escolher entre modificadores, habilidades e relíquias. Cada escolha afeta sua estratégia."
        )},
        { TutorialStep.ExplainShop, (
            "Loja",
            "Na loja entre rodadas, você pode comprar novas cartas, remover cartas ruins, ou adquirir itens especiais."
        )},
        { TutorialStep.FreePlay, (
            "Jogo Livre",
            "Agora jogue livremente contra os bots! Tente alcançar a pontuação alvo."
        )},
        { TutorialStep.Complete, (
            "Tutorial Completo!",
            "Você está pronto para jogar online contra seus amigos. Boa sorte!"
        )},
    };

    public override void _Ready()
    {
        GD.Print("[Tutorial] Tutorial controller initialized.");
        ShowCurrentStep();
    }

    /// <summary>
    /// Advance to the next tutorial step.
    /// </summary>
    public void AdvanceStep()
    {
        if (CurrentStep == TutorialStep.Complete) return;

        CurrentStep = (TutorialStep)((int)CurrentStep + 1);
        ShowCurrentStep();

        if (CurrentStep == TutorialStep.Complete)
        {
            EmitSignal(SignalName.TutorialComplete);
            GD.Print("[Tutorial] Tutorial complete!");
        }
    }

    /// <summary>
    /// Go back one step.
    /// </summary>
    public void PreviousStep()
    {
        if (CurrentStep == TutorialStep.Welcome) return;
        CurrentStep = (TutorialStep)((int)CurrentStep - 1);
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (STEP_DATA.TryGetValue(CurrentStep, out var data))
        {
            EmitSignal(SignalName.TutorialStepChanged, (int)CurrentStep, data.Title, data.Desc);
            GD.Print($"[Tutorial] Step {(int)CurrentStep}: {data.Title}");
        }
    }

    /// <summary>
    /// Spawn tutorial bots (Easy difficulty) into the match.
    /// </summary>
    public List<BotController> SpawnTutorialBots(Node parent, int count = 3)
    {
        var bots = new List<BotController>();
        string[] botNames = { "Carlos", "Maria", "João", "Ana", "Pedro" };

        for (int i = 0; i < count && i < botNames.Length; i++)
        {
            // In a real implementation, we'd instance PokerBotBrain nodes.
            // For now, log the creation.
            GD.Print($"[Tutorial] Spawned bot: {botNames[i]} (Easy)");
        }

        return bots;
    }
}
