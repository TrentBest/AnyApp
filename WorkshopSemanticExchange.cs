using TheSingularityWorkshop.FSM_UserIO;
using TheSingularityWorkshop.GrammarAi;
using TheSingularityWorkshop.ProtocolAi;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Host-owned semantic vocabulary and structural grammar for the AnyApp Experience boundary.
/// </summary>
/// <remarks>
/// ProtocolAI owns the deterministic vocabulary, GrammarAI owns the structural relationship,
/// and FSM_UserIO carries the resulting application intent. None of the foundation packages
/// depends on this host or on each other to make this exchange possible.
/// </remarks>
public static class WorkshopSemanticExchange
{
    public const ulong IntentProtocolId = 4200;
    public const ulong OpenForgeSymbolId = 4201;
    public const ulong ExploreExperiencesSymbolId = 4202;

    public const ulong ExperienceIntentGrammarId = 4300;
    public const ulong ExperienceIntentStartSymbolId = 4301;
    public const ulong OpenForgeRuleId = 4302;
    public const ulong ExploreExperiencesRuleId = 4303;

    public static ProtocolDefinition IntentProtocol { get; } =
        new ProtocolBuilder(IntentProtocolId, "WorkshopExperienceIntents")
            .Define(OpenForgeSymbolId, "openForge", "open.forge")
            .Define(ExploreExperiencesSymbolId, "exploreExperiences", "explore.experiences")
            .Build();

    public static GrammarDefinition IntentGrammar { get; } =
        new GrammarBuilder(
                ExperienceIntentGrammarId,
                "WorkshopExperienceIntent",
                ExperienceIntentStartSymbolId)
            .Rule(
                OpenForgeRuleId,
                ExperienceIntentStartSymbolId,
                GrammarSymbol.Terminal(
                    new GrammarProtocolReference(IntentProtocolId, OpenForgeSymbolId)))
            .Rule(
                ExploreExperiencesRuleId,
                ExperienceIntentStartSymbolId,
                GrammarSymbol.Terminal(
                    new GrammarProtocolReference(IntentProtocolId, ExploreExperiencesSymbolId)))
            .Build();

    public static SemanticIntent OpenForgeIntent { get; } =
        new("open.forge", IntentProtocolId);

    public static SemanticIntent ExploreExperiencesIntent { get; } =
        new("explore.experiences", IntentProtocolId);

    /// <summary>
    /// Resolves an FSM_UserIO intent into the ProtocolAI symbol it names.
    /// </summary>
    public static ProtocolReference Resolve(SemanticIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (intent.ProtocolId != IntentProtocolId)
            throw new ArgumentException(
                $"Intent belongs to protocol '{intent.ProtocolId}', not '{IntentProtocolId}'.",
                nameof(intent));

        return IntentProtocol.ReferenceByValue(intent.Name);
    }

    /// <summary>
    /// Determines whether the intent is structurally admitted by the host grammar.
    /// </summary>
    public static bool IsAllowed(SemanticIntent intent)
    {
        var reference = Resolve(intent);

        return IntentGrammar.Rules
            .SelectMany(rule => rule.RightHandSide)
            .Where(symbol => symbol.IsProtocolSymbol)
            .Select(symbol => symbol.ProtocolReference!.Value)
            .Any(candidate =>
                candidate.ProtocolId == reference.ProtocolId &&
                candidate.SymbolId == reference.SymbolId);
    }
}
