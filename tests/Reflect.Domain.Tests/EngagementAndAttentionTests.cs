using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Domain.Tests;

public class EngagementAndAttentionTests
{
    private static readonly DateOnly Today = new(2026, 10, 13);

    private static ActivitySession Session(int daysAgo, ReflectPlatform platform = ReflectPlatform.ReflectXR, int minutes = 20,
        SessionSetting setting = SessionSetting.Remote, int? before = null, int? after = null, string module = "xr-creative-calm")
    {
        var start = Today.AddDays(-daysAgo).StartOfEasternDayUtc().AddHours(14);
        return new ActivitySession
        {
            Platform = platform, Setting = setting, ModuleKey = module, StartedAt = start, EndedAt = start.AddMinutes(minutes),
            MoodBefore = before, MoodAfter = after
        };
    }

    [Fact]
    public void Summary_splits_platforms_and_averages_mood_change()
    {
        var sessions = new[]
        {
            Session(1, ReflectPlatform.ReflectXR, 30, before: 4, after: 6),
            Session(2, ReflectPlatform.ReflectMobile, 10, before: 5, after: 6, module: "mobile-create"),
            Session(2, ReflectPlatform.ReflectMobile, 10, module: "mobile-learn")
        };

        var summary = EngagementCalculator.Summarize(sessions);

        Assert.Equal(3, summary.Sessions);
        Assert.Equal(50, summary.TotalMinutes);
        Assert.Equal(2, summary.ActiveDays);
        Assert.Equal(60, summary.XrMinutesPercent);
        Assert.Equal(1.5m, summary.AverageMoodChange);
        Assert.Equal("Creative Calm", summary.Modules[0].ModuleName);
    }

    [Fact]
    public void Only_remote_sessions_are_data_days()
    {
        var sessions = new[] { Session(1), Session(1), Session(2, setting: SessionSetting.InClinic), Session(3) };

        Assert.Equal(2, EngagementCalculator.RemoteDataDays(sessions).Count);
    }

    [Fact]
    public void Weekly_buckets_start_on_monday_and_include_empty_weeks()
    {
        var weeks = EngagementCalculator.Weekly(new[] { Session(0, minutes: 15), Session(20, ReflectPlatform.ReflectMobile, 5) }, Today, 4);

        Assert.Equal(4, weeks.Count);
        Assert.All(weeks, w => Assert.Equal(DayOfWeek.Monday, w.WeekStart.DayOfWeek));
        Assert.Equal(15, weeks[^1].XrMinutes);
        Assert.Equal(5, weeks.Sum(w => w.MobileMinutes));
    }

    [Fact]
    public void Flags_inactivity_and_worsening_phq9()
    {
        var sessions = new[] { Session(16) };
        var assessments = new[]
        {
            new Assessment { Instrument = AssessmentInstrument.Phq9, Score = 9, CompletedAt = Today.AddDays(-30).StartOfEasternDayUtc() },
            new Assessment { Instrument = AssessmentInstrument.Phq9, Score = 15, CompletedAt = Today.AddDays(-2).StartOfEasternDayUtc() }
        };

        var items = AttentionRules.Evaluate(sessions, assessments, null, Today);

        Assert.Contains(items, i => i.Title == "No recent activity" && i.Severity == AttentionSeverity.Critical);
        Assert.Contains(items, i => i.Title == "PHQ-9 worsened");
        Assert.Equal(AttentionSeverity.Critical, items[0].Severity);
    }

    [Fact]
    public void Flags_mood_drop_after_three_sessions()
    {
        var sessions = new[] { Session(1, before: 6, after: 4), Session(2, before: 5, after: 4), Session(3, before: 7, after: 5) };

        var items = AttentionRules.Evaluate(sessions, Array.Empty<Assessment>(), null, Today);

        Assert.Contains(items, i => i.Category == "Mood");
    }

    [Fact]
    public void Flags_missing_interactive_contact_once_time_is_logged()
    {
        var rtm = new RtmBillingSummary { Readiness = RtmReadiness.OnTrack, TreatmentMinutes = 15, HasInteractiveCommunication = false };

        var items = AttentionRules.Evaluate(new[] { Session(1) }, Array.Empty<Assessment>(), rtm, Today);

        Assert.Contains(items, i => i.Title == "Interactive contact needed");
    }
}
