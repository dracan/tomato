using FluentAssertions;
using Tomato.Converters;

namespace Tomato.Tests.Unit;

public class TimerDisplayTests
{
    [Theory]
    [InlineData(25, 0, "25:00")]
    [InlineData(0, 5, "00:05")]
    [InlineData(0, 0, "00:00")]
    public void Format_TimeLeft_ShowsMinutesAndSeconds(int minutes, int seconds, string expected)
    {
        TimerDisplay.Format(new TimeSpan(0, minutes, seconds)).Should().Be(expected);
    }

    [Fact]
    public void Format_AnHourOrMore_ShowsHours()
    {
        TimerDisplay.Format(new TimeSpan(1, 5, 30)).Should().Be("1:05:30");
    }

    [Fact]
    public void Format_InOvertime_ShowsMinusSign()
    {
        TimerDisplay.Format(new TimeSpan(0, -4, -12)).Should().Be("-04:12");
    }

    [Fact]
    public void Format_InOvertimeOverAnHour_ShowsMinusSignAndHours()
    {
        TimerDisplay.Format(new TimeSpan(-1, -2, -3)).Should().Be("-1:02:03");
    }

    [Fact]
    public void Format_LessThanASecondIntoOvertime_ShowsZero()
    {
        TimerDisplay.Format(TimeSpan.FromMilliseconds(-400)).Should().Be("00:00");
    }
}
