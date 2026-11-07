using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Assistant;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.Tests;

public class PseudonymizerTests
{
    private static Pseudonymizer Make()
    {
        var p = new Pseudonymizer();
        p.Register("[C01]", "Grace", "Okafor");
        p.Register("[C02]", "Henry", "Walsh");
        return p;
    }

    [Fact]
    public void Full_and_reversed_names_become_tokens()
    {
        var p = Make();
        Assert.Equal("Is [C01] doing better than [C02]?", p.Apply("Is Grace Okafor doing better than Walsh, Henry?"));
    }

    [Fact]
    public void Single_names_are_replaced_but_ordinary_words_are_not()
    {
        var p = Make();
        Assert.Equal("How is [C01]? She showed grace under stress.", p.Apply("How is Grace? She showed grace under stress."));
    }

    [Fact]
    public void Restore_puts_real_names_back_including_bare_tokens()
    {
        var p = Make();
        Assert.Equal("Grace Okafor improved; Henry Walsh did not.", p.Restore("[C01] improved; C02 did not."));
    }
}

public class ClinicalContextBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 16, 0, 0, TimeSpan.Zero);

    private static ClientContextData Data()
    {
        var client = new ProviderClient
        {
            FirstName = "Avery", LastName = "Collins", Mrn = "RX-100200", DateOfBirth = new DateOnly(1992, 8, 29),
            PayerName = "Medicare Part B", PrimaryDiagnosisCode = "F33.1", PrimaryDiagnosisLabel = "Major depressive disorder",
            TreatmentGoals = "Rebuild daily routine.", RtmConsentDate = new DateOnly(2026, 6, 11), RtmStartDate = new DateOnly(2026, 6, 12),
            ClientUserId = Guid.NewGuid()
        };
        var s = new AppSession { ClientUserId = client.ClientUserId, Platform = ReflectPlatform.ReflectMobile, ModuleKey = "m-pour-paint", StartedAt = Now.AddDays(-1), EndedAt = Now.AddDays(-1).AddMinutes(12), Completed = true };
        var a = new AssessmentResult { ClientUserId = client.ClientUserId, Instrument = AssessmentInstrument.Phq9, TotalScore = 19, SafetyItemScore = 1, CompletedAt = Now.AddDays(-2) };
        return new ClientContextData
        {
            Client = client, Token = "[CLIENT]", Sessions = new[] { s }, Assessments = new[] { a },
            Rtm = RtmBillingCalculator.Evaluate(new RtmEvaluationInput { Today = EasternTime.ToDate(Now), DevicePeriod = new DateRange(new DateOnly(2026, 10, 10), new DateOnly(2026, 11, 8)), HasConsent = true, SetupBilled = true })
        };
    }

    private static string Build() => ClinicalContextBuilder.ForClient(Data(), Now, EasternTime.ToDate, t => t.Hour);

    [Fact]
    public void Context_contains_no_direct_identifiers()
    {
        var text = Build();
        Assert.False(text.Contains("Avery"));
        Assert.False(text.Contains("Collins"));
        Assert.False(text.Contains("RX-100200"));
        Assert.False(text.Contains("1992"));
        Assert.False(text.Contains("Medicare"));
        Assert.False(System.Text.RegularExpressions.Regex.IsMatch(text, @"\d{1,2}/\d{1,2}/\d{4}"));
    }

    [Fact]
    public void Context_uses_age_band_relative_days_and_flags_item_9()
    {
        var text = Build();
        Assert.True(text.Contains("Age band: 30-39"));
        Assert.True(text.Contains("yesterday"));
        Assert.True(text.Contains("ITEM 9"));
        Assert.True(text.Contains("Pour Paint (Mobile, Creative Calm), 12 min"));
        Assert.False(text.Contains("(Mobile) (Mobile)"));
    }
}

public class RtmNoteDrafterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Draft_summarizes_data_without_identifiers_and_flags_safety()
    {
        var client = new ProviderClient { FirstName = "Avery", LastName = "Collins", Mrn = "RX-1", ClientUserId = Guid.NewGuid(), DateOfBirth = new DateOnly(1990, 1, 1), RtmStartDate = new DateOnly(2026, 9, 1), RtmConsentDate = new DateOnly(2026, 9, 1) };
        var s = new AppSession { ClientUserId = client.ClientUserId, Platform = ReflectPlatform.ReflectXR, ModuleKey = "xr-flow", StartedAt = Now.AddDays(-2), EndedAt = Now.AddDays(-2).AddMinutes(20), Completed = true };
        var a1 = new AssessmentResult { ClientUserId = client.ClientUserId, Instrument = AssessmentInstrument.Phq9, TotalScore = 12, CompletedAt = Now.AddDays(-40) };
        var a2 = new AssessmentResult { ClientUserId = client.ClientUserId, Instrument = AssessmentInstrument.Phq9, TotalScore = 18, SafetyItemScore = 1, CompletedAt = Now.AddDays(-1) };
        var rtm = RtmBillingCalculator.Evaluate(new RtmEvaluationInput { Today = EasternTime.ToDate(Now), DevicePeriod = new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 30)), DataDays = new[] { new DateOnly(2026, 10, 28) }, HasConsent = true, SetupBilled = true });
        var note = RtmNoteDrafter.Draft(new ClientContextData { Client = client, Sessions = new[] { s }, Assessments = new[] { a1, a2 }, Rtm = rtm }, Now, EasternTime.ToDate);

        Assert.True(note.Contains("1 session (20 min) on 1 day"));
        Assert.True(note.Contains("Flow"));
        Assert.True(note.Contains("Latest PHQ-9 18 (moderately severe), +6 since baseline."));
        Assert.True(note.Contains("item 9 endorsed"));
        Assert.False(note.Contains("Avery"));
        Assert.False(note.Contains("RX-1"));
    }
}
