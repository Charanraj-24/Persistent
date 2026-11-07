using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.Tests;

public class ReviewTimeRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(15, 15)]
    [InlineData(20, 20)]
    [InlineData(600, 20)]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    public void First_heartbeat_is_capped(int claimed, int expected)
    {
        Assert.Equal(expected, ReviewTimeRules.AcceptedIncrement(claimed, null, Now));
    }

    [Fact]
    public void Heartbeat_cannot_claim_more_than_elapsed_wall_clock_time()
    {
        Assert.Equal(7, ReviewTimeRules.AcceptedIncrement(15, Now.AddSeconds(-5), Now));
        Assert.Equal(15, ReviewTimeRules.AcceptedIncrement(15, Now.AddSeconds(-15), Now));
    }

    [Fact]
    public void Replayed_heartbeat_adds_almost_nothing()
    {
        Assert.True(ReviewTimeRules.AcceptedIncrement(20, Now, Now) <= 2);
    }
}

public class AssessmentScoringTests
{
    [Theory]
    [InlineData(0, "Minimal")]
    [InlineData(4, "Minimal")]
    [InlineData(5, "Mild")]
    [InlineData(10, "Moderate")]
    [InlineData(15, "Moderately severe")]
    [InlineData(20, "Severe")]
    [InlineData(27, "Severe")]
    public void Phq9_bands(int score, string expected) =>
        Assert.Equal(expected, AssessmentScoring.Severity(AssessmentInstrument.Phq9, score));

    [Theory]
    [InlineData(4, "Minimal")]
    [InlineData(9, "Mild")]
    [InlineData(14, "Moderate")]
    [InlineData(15, "Severe")]
    public void Gad7_bands(int score, string expected) =>
        Assert.Equal(expected, AssessmentScoring.Severity(AssessmentInstrument.Gad7, score));
}

public class EngagementAnalyzerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid User = Guid.NewGuid();

    private static AppSession Session(int daysAgo, int minutes = 20, ReflectPlatform platform = ReflectPlatform.ReflectXR, string module = "xr-flow") => new()
    {
        ClientUserId = User, Platform = platform, ModuleKey = module,
        StartedAt = Now.AddDays(-daysAgo).AddHours(-1), EndedAt = Now.AddDays(-daysAgo).AddHours(-1).AddMinutes(minutes), Completed = true
    };

    private static AppSession Tutorial(int daysAgo) => Session(daysAgo, 10, ReflectPlatform.ReflectXR, ModuleCatalog.TutorialKey);

    /// <summary>A session starting at a given Eastern local hour, daysAgo days before Now.</summary>
    private static AppSession At(int daysAgo, int localHour, ReflectPlatform platform = ReflectPlatform.ReflectMobile)
    {
        var start = EasternTime.AtLocal(EasternTime.ToDate(Now).AddDays(-daysAgo), new TimeOnly(localHour, 15));
        return new AppSession { ClientUserId = User, Platform = platform, ModuleKey = "m-pour-paint", StartedAt = start, EndedAt = start.AddMinutes(12), Completed = true };
    }

    private static AssessmentResult Phq(int daysAgo, int score, int item9 = 0) => new()
    {
        ClientUserId = User, Instrument = AssessmentInstrument.Phq9, TotalScore = score, SafetyItemScore = item9, CompletedAt = Now.AddDays(-daysAgo)
    };

    private static EngagementSummary Summary(params AppSession[] sessions) =>
        EngagementAnalyzer.Summarize(sessions, Array.Empty<MoodCheckIn>(), Now, EasternTime.ToDate);

    [Fact]
    public void Summary_splits_platforms_and_counts_active_days()
    {
        var s = Summary(Session(1, 30), Session(1, 10, ReflectPlatform.ReflectMobile, "m-learn-self"), Session(3, 20));
        Assert.Equal(3, s.Sessions);
        Assert.Equal(2, s.ActiveDays);
        Assert.Equal(50, Math.Round(s.XrMinutes));
        Assert.Equal(10, Math.Round(s.MobileMinutes));
        Assert.Equal("xr-flow", s.TopModuleKey);
    }

    [Fact]
    public void Safety_item_creates_a_critical_flag_listed_first()
    {
        var flags = EngagementAnalyzer.Flags(Summary(Session(10)), new[] { Phq(30, 10), Phq(1, 16, item9: 1) }, Now);
        Assert.Equal(FlagSeverity.Critical, flags[0].Severity);
        Assert.Contains(flags, f => f.Title == "PHQ-9 worsening");
        Assert.Contains(flags, f => f.Title == "Inactive");
    }

    [Fact]
    public void Five_point_improvement_is_flagged_as_progress()
    {
        var flags = EngagementAnalyzer.Flags(Summary(Session(1), Tutorial(40)), new[] { Phq(60, 18), Phq(1, 9) }, Now);
        var f = Assert.Single(flags);
        Assert.Equal(FlagSeverity.Positive, f.Severity);
    }

    [Fact]
    public void Halving_of_sessions_is_flagged_as_dropping_engagement()
    {
        var sessions = new[] { Session(16), Session(18), Session(20), Session(22), Session(24), Session(2) };
        var flags = EngagementAnalyzer.Flags(Summary(sessions), Array.Empty<AssessmentResult>(), Now);
        Assert.Contains(flags, f => f.Title == "Engagement dropping");
    }

    [Fact]
    public void Steady_engagement_and_stable_scores_raise_no_flags()
    {
        var sessions = Enumerable.Range(1, 20).Select(d => Session(d)).Append(Tutorial(30)).ToArray();
        Assert.Empty(EngagementAnalyzer.Flags(Summary(sessions), new[] { Phq(30, 10), Phq(1, 11) }, Now));
    }

    [Fact]
    public void Talking_points_suggest_asking_about_barriers_when_inactive()
    {
        var points = TalkingPointsBuilder.Build(new[] { Session(12) }, Array.Empty<MoodCheckIn>(), Array.Empty<AssessmentResult>(), Now);
        Assert.Contains(points, p => p.Contains("barriers"));
    }

    [Fact]
    public void Talking_points_lead_with_safety_when_item_9_is_endorsed()
    {
        var points = TalkingPointsBuilder.Build(new[] { Session(1) }, Array.Empty<MoodCheckIn>(), new[] { Phq(2, 18, item9: 1) }, Now);
        Assert.True(points[0].StartsWith("Start with safety"));
    }
    [Fact]
    public void Xr_use_without_the_training_tutorial_is_noted()
    {
        var flags = EngagementAnalyzer.Flags(Summary(Session(1), Session(3)), Array.Empty<AssessmentResult>(), Now);
        Assert.Contains(flags, f => f.Title == "Training Tutorial not completed" && f.Severity == FlagSeverity.Info);
        var withTutorial = EngagementAnalyzer.Flags(Summary(Session(1), Tutorial(5)), Array.Empty<AssessmentResult>(), Now);
        Assert.False(withTutorial.Any(f => f.Title == "Training Tutorial not completed"));
        var mobileOnly = EngagementAnalyzer.Flags(Summary(Session(1, 10, ReflectPlatform.ReflectMobile, "m-pour-paint")), Array.Empty<AssessmentResult>(), Now);
        Assert.False(mobileOnly.Any(f => f.Title == "Training Tutorial not completed"));
    }

    [Fact]
    public void Two_late_night_sessions_in_14_days_raise_a_flag_and_a_talking_point()
    {
        var sessions = new[] { At(2, 3), At(5, 1), At(6, 21) };
        var summary = Summary(sessions);
        Assert.Equal(2, summary.LateNightSessions14Days);
        Assert.Contains(EngagementAnalyzer.Flags(summary, Array.Empty<AssessmentResult>(), Now), f => f.Title == "Late-night use");
        Assert.Contains(TalkingPointsBuilder.Build(sessions, Array.Empty<MoodCheckIn>(), Array.Empty<AssessmentResult>(), Now, 6), p => p.Contains("after midnight on 2 nights"));
    }

    [Fact]
    public void One_late_night_session_or_evening_use_is_not_flagged()
    {
        var flags = EngagementAnalyzer.Flags(Summary(At(2, 3), At(3, 22), At(4, 23)), Array.Empty<AssessmentResult>(), Now);
        Assert.False(flags.Any(f => f.Title == "Late-night use"));
    }

    [Fact]
    public void Catalog_matches_pti_official_module_counts()
    {
        Assert.Equal(14, ModuleCatalog.All.Count(m => m.Platform == ReflectPlatform.ReflectMobile));
        Assert.Equal(17, ModuleCatalog.All.Count(m => m.Platform == ReflectPlatform.ReflectXR));
        Assert.Equal(new[] { "Creative Calm", "Create", "Learn", "Reflect" }, ModuleCatalog.All.Where(m => m.Platform == ReflectPlatform.ReflectMobile).Select(m => m.Group).Distinct());
        Assert.Equal(new[] { "Creative Calm", "Explore Emotions", "Explore Self", "Explore Journey", "Getting Started" }, ModuleCatalog.All.Where(m => m.Platform == ReflectPlatform.ReflectXR).Select(m => m.Group).Distinct());
        Assert.True(ModuleCatalog.Get(ModuleCatalog.TutorialKey).IsTutorial);
    }
}
