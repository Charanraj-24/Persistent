using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Domain.ProviderDashboard.Clinical;

public static class AssessmentScoring
{
    /// <summary>Change in total score treated as clinically meaningful (PHQ-9 and GAD-7 both use 5 points).</summary>
    public const int MeaningfulChange = 5;

    public static int MaxScore(AssessmentInstrument instrument) => instrument == AssessmentInstrument.Phq9 ? 27 : 21;

    public static string Name(AssessmentInstrument instrument) => instrument == AssessmentInstrument.Phq9 ? "PHQ-9" : "GAD-7";

    public static string Severity(AssessmentInstrument instrument, int score)
    {
        if (instrument == AssessmentInstrument.Phq9)
        {
            return score switch
            {
                <= 4 => "Minimal",
                <= 9 => "Mild",
                <= 14 => "Moderate",
                <= 19 => "Moderately severe",
                _ => "Severe"
            };
        }

        return score switch
        {
            <= 4 => "Minimal",
            <= 9 => "Mild",
            <= 14 => "Moderate",
            _ => "Severe"
        };
    }

    /// <summary>Bootstrap color name for a severity badge (always paired with a text label).</summary>
    public static string SeverityColor(AssessmentInstrument instrument, int score)
    {
        var severity = Severity(instrument, score);
        return severity switch
        {
            "Minimal" => "success",
            "Mild" => "info",
            "Moderate" => "warning",
            _ => "danger"
        };
    }
}
