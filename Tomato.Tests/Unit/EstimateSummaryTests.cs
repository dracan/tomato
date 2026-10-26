using FluentAssertions;
using Tomato.Models;

namespace Tomato.Tests.Unit;

public class EstimateSummaryTests
{
    [Fact]
    public void Describe_TookLonger_SaysHowFarOver()
    {
        EstimateSummary.Describe(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(18))
            .Should().Be("Estimated 15m, took 18m (3m over)");
    }

    [Fact]
    public void Describe_FinishedEarly_SaysHowFarUnder()
    {
        EstimateSummary.Describe(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(12))
            .Should().Be("Estimated 15m, took 12m (3m under)");
    }

    [Fact]
    public void Describe_WithinHalfAMinute_SaysOnTime()
    {
        EstimateSummary.Describe(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(25).Add(TimeSpan.FromSeconds(20)))
            .Should().Be("Estimated 25m, took 25m (on time)");
    }

    [Fact]
    public void Describe_RoundsToTheNearestMinute()
    {
        EstimateSummary.Describe(TimeSpan.FromMinutes(10), new TimeSpan(0, 7, 40))
            .Should().Be("Estimated 10m, took 8m (2m under)");
    }

    [Fact]
    public void Describe_AnHourOrMore_ShowsHoursAndMinutes()
    {
        EstimateSummary.Describe(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(95))
            .Should().Be("Estimated 1h 0m, took 1h 35m (35m over)");
    }
}
