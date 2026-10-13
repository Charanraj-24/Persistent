using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Domain.Tests;

public class GoalProgressCalculatorTests
{
    [Theory]
    [InlineData(GoalDirection.Decrease, 18, 8, 13, 50)]
    [InlineData(GoalDirection.Decrease, 18, 8, 20, 0)]
    [InlineData(GoalDirection.Decrease, 18, 8, 5, 100)]
    [InlineData(GoalDirection.Increase, 1, 4, 3, 67)]
    [InlineData(GoalDirection.Increase, 4, 4, 4, 100)]
    public void Percent_complete_respects_direction(GoalDirection direction, int baseline, int target, int current, int expected)
    {
        Assert.Equal(expected, GoalProgressCalculator.PercentComplete(direction, baseline, target, current));
    }

    [Fact]
    public void Goal_well_behind_the_calendar_is_behind()
    {
        var goal = new TreatmentGoal
        {
            Direction = GoalDirection.Decrease, BaselineValue = 20, TargetValue = 10, CurrentValue = 19,
            StartDate = new DateOnly(2026, 7, 1), TargetDate = new DateOnly(2026, 11, 1)
        };

        var progress = GoalProgressCalculator.Calculate(goal, new DateOnly(2026, 10, 1));

        Assert.Equal(GoalTrack.Behind, progress.Track);
        Assert.Equal(10, progress.Percent);
    }

    [Fact]
    public void Goal_that_meets_target_is_achieved()
    {
        var goal = new TreatmentGoal
        {
            Direction = GoalDirection.Increase, BaselineValue = 1, TargetValue = 3, CurrentValue = 3,
            StartDate = new DateOnly(2026, 7, 1), TargetDate = new DateOnly(2026, 12, 1)
        };

        Assert.Equal(GoalTrack.Achieved, GoalProgressCalculator.Calculate(goal, new DateOnly(2026, 8, 1)).Track);
    }
}
