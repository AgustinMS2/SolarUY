using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class SolarCalendarTests
{
    private static DateTime Local(int year, int month, int day, double hour = 0)
        => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified).AddHours(hour);

    // ---------- Duración del día ----------

    [Theory]
    [InlineData(172, 10.1)]   // 21 de junio: el día más corto
    [InlineData(355, 14.3)]   // 21 de diciembre: el día más largo
    public void DayLengthHours_AtTheSolstices_IsTheExtreme(int dayOfYear, double expected)
    {
        Assert.Equal(expected, SolarCalendar.DayLengthHours(dayOfYear), precision: 2);
    }

    [Fact]
    public void DayLengthHours_NeverLeavesTheSeasonalRange()
    {
        for (var day = 1; day <= 366; day++)
        {
            var length = SolarCalendar.DayLengthHours(day);
            Assert.InRange(length, 10.1 - 1e-9, 14.3 + 1e-9);
        }
    }

    // ---------- Amanecer y atardecer por fecha ----------

    [Fact]
    public void SunriseAndSunset_OnTheShortestDay_AreSevenFortyTwoAndFiveFortyEight()
    {
        var date = Local(2026, 6, 21);

        Assert.Equal(7.70, SolarCalendar.SunriseHour(date), precision: 2);   // 07:42
        Assert.Equal(17.80, SolarCalendar.SunsetHour(date), precision: 2);   // 17:48
    }

    [Fact]
    public void SunriseAndSunset_OnTheLongestDay_AreFiveThirtySixAndSevenFiftyFour()
    {
        var date = Local(2026, 12, 21);

        Assert.Equal(5.60, SolarCalendar.SunriseHour(date), precision: 2);   // 05:36
        Assert.Equal(19.90, SolarCalendar.SunsetHour(date), precision: 2);   // 19:54
    }

    [Fact]
    public void SunriseAndSunset_AtTheEndOfOctober_MatchTheSpecWindowOfSixToSevenThirty()
    {
        var date = Local(2026, 10, 30);

        Assert.InRange(SolarCalendar.SunriseHour(date), 5.98, 6.02);         // 06:00
        Assert.InRange(SolarCalendar.SunsetHour(date), 19.48, 19.52);        // 19:30
    }

    [Fact]
    public void SunriseAndSunset_AreSymmetricAroundSolarNoon()
    {
        var date = Local(2026, 3, 15);

        var morning = SolarCalendar.SolarNoonHour - SolarCalendar.SunriseHour(date);
        var evening = SolarCalendar.SunsetHour(date) - SolarCalendar.SolarNoonHour;

        Assert.Equal(morning, evening, precision: 9);
    }

    [Fact]
    public void SunriseAndSunset_IgnoreTheTimeOfDayOfTheGivenDate()
    {
        Assert.Equal(SolarCalendar.SunriseHour(Local(2026, 6, 21, 0)), SolarCalendar.SunriseHour(Local(2026, 6, 21, 15.5)));
    }

    [Fact]
    public void SunriseAndSunset_WorkOnDayThreeHundredSixtySixOfALeapYear()
    {
        var date = Local(2028, 12, 31);

        Assert.InRange(SolarCalendar.SunriseHour(date), 5.5, 5.8);
        Assert.InRange(SolarCalendar.SunsetHour(date), 19.7, 20.0);
    }

    // ---------- Curva (factor de generación) ----------

    [Fact]
    public void Factor_AtSolarNoon_IsOne()
    {
        Assert.Equal(1.0, SolarCalendar.Factor(Local(2026, 11, 15, 12.75)), precision: 6);
        Assert.Equal(1.0, SolarCalendar.Factor(Local(2026, 6, 21, 12.75)), precision: 6);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.0)]
    [InlineData(5.0)]
    [InlineData(20.5)]
    [InlineData(23.99)]
    public void Factor_ByNight_IsZero(double hour)
    {
        Assert.Equal(0.0, SolarCalendar.Factor(Local(2026, 11, 15, hour)));
    }

    [Fact]
    public void Factor_ExactlyAtSunriseAndSunset_IsZero()
    {
        var date = Local(2026, 11, 15);

        Assert.Equal(0.0, SolarCalendar.Factor(date.AddHours(SolarCalendar.SunriseHour(date))), precision: 9);
        Assert.Equal(0.0, SolarCalendar.Factor(date.AddHours(SolarCalendar.SunsetHour(date))), precision: 9);
    }

    [Fact]
    public void Factor_IsSymmetricAroundSolarNoon()
    {
        var morning = SolarCalendar.Factor(Local(2026, 11, 15, 12.75 - 3.0));
        var evening = SolarCalendar.Factor(Local(2026, 11, 15, 12.75 + 3.0));

        Assert.Equal(morning, evening, precision: 9);
        Assert.InRange(morning, 0.01, 0.99);
    }

    [Fact]
    public void Factor_GrowsFromSunriseToSolarNoon()
    {
        var previous = 0.0;
        for (var hour = 6.0; hour <= 12.75; hour += 0.25)
        {
            var factor = SolarCalendar.Factor(Local(2026, 11, 15, hour));
            Assert.True(factor >= previous, $"El factor bajó a las {hour}");
            previous = factor;
        }
    }

    [Fact]
    public void Factor_RespectsTheSeason_AtSevenInTheMorning()
    {
        // En pleno invierno el sol todavía no salió a las 07:00; en verano ya está alto.
        Assert.Equal(0.0, SolarCalendar.Factor(Local(2026, 6, 21, 7.0)));
        Assert.True(SolarCalendar.Factor(Local(2026, 12, 21, 7.0)) > 0.2);
    }

    [Fact]
    public void Factor_RespectsTheSeason_AtSixInTheEvening()
    {
        Assert.Equal(0.0, SolarCalendar.Factor(Local(2026, 6, 21, 18.0)));
        Assert.True(SolarCalendar.Factor(Local(2026, 12, 21, 18.0)) > 0.2);
    }

    [Fact]
    public void Factor_IgnoresTheDateKind()
    {
        var unspecified = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Unspecified);
        var utc = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

        Assert.Equal(SolarCalendar.Factor(unspecified), SolarCalendar.Factor(utc));
    }

    // ---------- Horas de sol ----------

    [Fact]
    public void IsDaylight_AtNoon_IsTrueAndAtNightIsFalse()
    {
        Assert.True(SolarCalendar.IsDaylight(Local(2026, 11, 15, 12.75)));
        Assert.False(SolarCalendar.IsDaylight(Local(2026, 11, 15, 3.0)));
        Assert.False(SolarCalendar.IsDaylight(Local(2026, 11, 15, 23.0)));
    }

    [Fact]
    public void IsDaylight_ExactlyAtSunrise_IsFalseAndOneMinuteLaterIsTrue()
    {
        var date = Local(2026, 11, 15);
        var sunrise = date.AddHours(SolarCalendar.SunriseHour(date));

        Assert.False(SolarCalendar.IsDaylight(sunrise));
        Assert.True(SolarCalendar.IsDaylight(sunrise.AddMinutes(1)));
    }

    [Fact]
    public void IsDaylight_DependsOnTheSeason()
    {
        Assert.False(SolarCalendar.IsDaylight(Local(2026, 6, 21, 7.0)));
        Assert.True(SolarCalendar.IsDaylight(Local(2026, 12, 21, 7.0)));
        Assert.False(SolarCalendar.IsDaylight(Local(2026, 6, 21, 18.5)));
        Assert.True(SolarCalendar.IsDaylight(Local(2026, 12, 21, 18.5)));
    }

    [Fact]
    public void IsDaylight_IsTrueExactlyWhereTheFactorIsPositive()
    {
        var date = Local(2026, 9, 10);
        for (var hour = 0.0; hour < 24.0; hour += 0.1)
        {
            var time = date.AddHours(hour);
            Assert.Equal(SolarCalendar.Factor(time) > 0.0, SolarCalendar.IsDaylight(time));
        }
    }
}
