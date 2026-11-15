using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class HistoryGapPlannerTests
{
    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static readonly DateTime Now = Utc(2026, 11, 15, 18, 0, 30);
    private static readonly DateTime LongAgo = Utc(2024, 1, 1);

    // ---------- Minutos ----------

    [Fact]
    public void MissingMinutes_WithoutHistory_StartsThirtyDaysBackAndEndsAtTheLastCompleteMinute()
    {
        var range = HistoryGapPlanner.MissingMinutes(null, LongAgo, Now, 30);

        Assert.NotNull(range);
        Assert.Equal(Utc(2026, 10, 16, 18, 0), range!.Value.FromUtc);
        Assert.Equal(Utc(2026, 11, 15, 18, 0), range.Value.ToUtc);
    }

    [Fact]
    public void MissingMinutes_WhenHistoryIsUpToDate_ReturnsNothing()
    {
        // El minuto 18:00 todavía está en curso: el último completo es el 17:59.
        Assert.Null(HistoryGapPlanner.MissingMinutes(Utc(2026, 11, 15, 17, 59), LongAgo, Now, 30));
    }

    [Fact]
    public void MissingMinutes_AfterARestart_CoversTheGapOnly()
    {
        var range = HistoryGapPlanner.MissingMinutes(Utc(2026, 11, 15, 17, 40), LongAgo, Now, 30);

        Assert.NotNull(range);
        Assert.Equal(Utc(2026, 11, 15, 17, 41), range!.Value.FromUtc);
        Assert.Equal(Utc(2026, 11, 15, 18, 0), range.Value.ToUtc);
    }

    [Fact]
    public void MissingMinutes_ForANewInverter_StartsAtTheFirstMinuteAfterItsRegistration()
    {
        var range = HistoryGapPlanner.MissingMinutes(null, Utc(2026, 11, 15, 17, 30, 20), Now, 30);

        Assert.Equal(Utc(2026, 11, 15, 17, 31), range!.Value.FromUtc);
    }

    [Fact]
    public void MissingMinutes_ForAnInverterRegisteredOnAMinuteBoundary_IncludesThatMinute()
    {
        var range = HistoryGapPlanner.MissingMinutes(null, Utc(2026, 11, 15, 17, 30), Now, 30);

        Assert.Equal(Utc(2026, 11, 15, 17, 30), range!.Value.FromUtc);
    }

    [Fact]
    public void MissingMinutes_WhenTheLastSavedMinuteIsOlderThanTheWindow_StartsAtTheWindow()
    {
        var range = HistoryGapPlanner.MissingMinutes(Utc(2026, 8, 1), LongAgo, Now, 30);

        Assert.Equal(Utc(2026, 10, 16, 18, 0), range!.Value.FromUtc);
    }

    [Fact]
    public void MissingMinutes_ForAnInverterRegisteredInTheFuture_ReturnsNothing()
    {
        Assert.Null(HistoryGapPlanner.MissingMinutes(null, Utc(2026, 11, 16), Now, 30));
    }

    [Fact]
    public void MissingMinutes_IsNotAffectedByTheKindOfTheDates()
    {
        var unspecified = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified);

        Assert.Equal(
            HistoryGapPlanner.MissingMinutes(null, LongAgo, Now, 30),
            HistoryGapPlanner.MissingMinutes(null, DateTime.SpecifyKind(LongAgo, DateTimeKind.Unspecified), unspecified, 30));
    }

    // ---------- Horas ----------

    [Fact]
    public void HourlyWindowStart_IsJanuaryFirstOfThePreviousLocalYear()
    {
        // 00:00 locales del 1 de enero de 2025 = 03:00 UTC.
        Assert.Equal(Utc(2025, 1, 1, 3), HistoryGapPlanner.HourlyWindowStartUtc(Now));
    }

    [Fact]
    public void HourlyWindowStart_FollowsTheLocalYearAroundNewYear()
    {
        // 02:00 UTC del 1/1/2027 todavía es el 31/12/2026 en Uruguay: el año local es 2026.
        Assert.Equal(Utc(2025, 1, 1, 3), HistoryGapPlanner.HourlyWindowStartUtc(Utc(2027, 1, 1, 2)));
        Assert.Equal(Utc(2026, 1, 1, 3), HistoryGapPlanner.HourlyWindowStartUtc(Utc(2027, 1, 1, 3)));
    }

    [Fact]
    public void MissingHours_WithoutHistory_StartsAtThePreviousYearAndEndsAtTheLastCompleteHour()
    {
        var range = HistoryGapPlanner.MissingHours(null, LongAgo, Now);

        Assert.NotNull(range);
        Assert.Equal(Utc(2025, 1, 1, 3), range!.Value.FromUtc);
        Assert.Equal(Utc(2026, 11, 15, 18), range.Value.ToUtc);
    }

    [Fact]
    public void MissingHours_WhenHistoryIsUpToDate_ReturnsNothing()
    {
        Assert.Null(HistoryGapPlanner.MissingHours(Utc(2026, 11, 15, 17), LongAgo, Now));
    }

    [Fact]
    public void MissingHours_AfterARestart_CoversTheGapOnly()
    {
        var range = HistoryGapPlanner.MissingHours(Utc(2026, 11, 15, 10), LongAgo, Now);

        Assert.Equal(Utc(2026, 11, 15, 11), range!.Value.FromUtc);
        Assert.Equal(Utc(2026, 11, 15, 18), range.Value.ToUtc);
    }

    [Fact]
    public void MissingHours_ForNewEquipment_StartsAtTheFirstWholeHourAfterItsRegistration()
    {
        var range = HistoryGapPlanner.MissingHours(null, Utc(2026, 11, 10, 10, 20), Now);

        Assert.Equal(Utc(2026, 11, 10, 11), range!.Value.FromUtc);
    }

    [Fact]
    public void MissingHours_ForEquipmentRegisteredOnAnHourBoundary_IncludesThatHour()
    {
        var range = HistoryGapPlanner.MissingHours(null, Utc(2026, 11, 10, 10), Now);

        Assert.Equal(Utc(2026, 11, 10, 10), range!.Value.FromUtc);
    }

    [Fact]
    public void MissingHours_ForEquipmentRegisteredInTheCurrentHour_ReturnsNothing()
    {
        Assert.Null(HistoryGapPlanner.MissingHours(null, Utc(2026, 11, 15, 17, 30), Now));
    }
}
