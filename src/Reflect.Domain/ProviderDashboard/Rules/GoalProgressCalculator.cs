using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Rules;

public enum GoalTrack
{
    Achieved = 1,
    OnTrack = 2,
    Behind = 3,
    OnHold = 4
}

public sealed record GoalProgress(int Percent, int ExpectedPercent, GoalTrack Track);

public static class GoalProgressCalculator
{
    /// <summary>Behind means more than this many points under the time-based expected progress.</summary>
    public const int BehindTolerancePercent = 15;

    public static GoalProgress Calculate(TreatmentGoal goal, DateOnly today)
    {
        var percent = PercentComplete(goal.Direction, goal.BaselineValue, goal.TargetValue, goal.CurrentValue);
        var expected = ExpectedPercent(goal.StartDate, goal.TargetDate, today);

        GoalTrack track;
        if (goal.Status == GoalStatus.Achieved || percent >= 100) track = GoalTrack.Achieved;
        else if (goal.Status == GoalStatus.OnHold) track = GoalTrack.OnHold;
        else if (percent + BehindTolerancePercent < expected) track = GoalTrack.Behind;
        else track = GoalTrack.OnTrack;

        return new GoalProgress(percent, expected, track);
    }

    public static int PercentComplete(GoalDirection direction, decimal baseline, decimal target, decimal current)
    {
        var span = direction == GoalDirection.Decrease ? baseline - target : target - baseline;
        if (span <= 0) return MeetsTarget(direction, target, current) ? 100 : 0;
        var done = direction == GoalDirection.Decrease ? baseline - current : current - baseline;
        var percent = (int)Math.Round(done / span * 100m, MidpointRounding.AwayFromZero);
        return Math.Clamp(percent, 0, 100);
    }

    public static int ExpectedPercent(DateOnly start, DateOnly target, DateOnly today)
    {
        var total = target.DayNumber - start.DayNumber;
        if (total <= 0) return 100;
        var elapsed = Math.Clamp(today.DayNumber - start.DayNumber, 0, total);
        return (int)Math.Round(elapsed * 100.0 / total, MidpointRounding.AwayFromZero);
    }

    private static bool MeetsTarget(GoalDirection direction, decimal target, decimal current) =>
        direction == GoalDirection.Decrease ? current <= target : current >= target;
}
