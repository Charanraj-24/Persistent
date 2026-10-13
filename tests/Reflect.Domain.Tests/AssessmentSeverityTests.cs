using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Domain.Tests;

public class AssessmentSeverityTests
{
    [Theory]
    [InlineData(0, "Minimal")]
    [InlineData(4, "Minimal")]
    [InlineData(5, "Mild")]
    [InlineData(10, "Moderate")]
    [InlineData(15, "Moderately severe")]
    [InlineData(20, "Severe")]
    [InlineData(27, "Severe")]
    public void Phq9_bands(int score, string label)
    {
        Assert.Equal(label, AssessmentSeverity.Classify(AssessmentInstrument.Phq9, score).Label);
    }

    [Theory]
    [InlineData(4, "Minimal")]
    [InlineData(9, "Mild")]
    [InlineData(14, "Moderate")]
    [InlineData(15, "Severe")]
    public void Gad7_bands(int score, string label)
    {
        Assert.Equal(label, AssessmentSeverity.Classify(AssessmentInstrument.Gad7, score).Label);
    }
}
