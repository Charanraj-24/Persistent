using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Rules;

public sealed record SeverityBand(string Label, int Level);

/// <summary>Standard published severity bands for PHQ-9 (0-27) and GAD-7 (0-21).</summary>
public static class AssessmentSeverity
{
    public static int MaxScore(AssessmentInstrument instrument) => instrument == AssessmentInstrument.Phq9 ? 27 : 21;

    public static string Name(AssessmentInstrument instrument) => instrument == AssessmentInstrument.Phq9 ? "PHQ-9" : "GAD-7";

    /// <summary>Change considered clinically meaningful: 5 points for PHQ-9, 4 points for GAD-7.</summary>
    public static int MeaningfulChange(AssessmentInstrument instrument) => instrument == AssessmentInstrument.Phq9 ? 5 : 4;

    public static SeverityBand Classify(AssessmentInstrument instrument, int score)
    {
        if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
        if (instrument == AssessmentInstrument.Phq9)
        {
            return score switch
            {
                <= 4 => new SeverityBand("Minimal", 0),
                <= 9 => new SeverityBand("Mild", 1),
                <= 14 => new SeverityBand("Moderate", 2),
                <= 19 => new SeverityBand("Moderately severe", 3),
                _ => new SeverityBand("Severe", 4)
            };
        }
        return score switch
        {
            <= 4 => new SeverityBand("Minimal", 0),
            <= 9 => new SeverityBand("Mild", 1),
            <= 14 => new SeverityBand("Moderate", 2),
            _ => new SeverityBand("Severe", 4)
        };
    }
}
