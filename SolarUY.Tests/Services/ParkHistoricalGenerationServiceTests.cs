using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class ParkHistoricalGenerationServiceTests
{
    private readonly FakeSolarParkRepository _parkRepo = new();
    private readonly FakeParkGenerationRepository _genRepo = new();
    private readonly SolarPark _testPark;

    public ParkHistoricalGenerationServiceTests()
    {
        _testPark = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Solar Salto Grande",
            NormalizedName = "PARQUE SOLAR SALTO GRANDE",
            MaxCapacityKw = 500.0,
            Status = SolarParkStatus.Active
        };
        _parkRepo.Add(_testPark);
    }

    [Fact]
    public void GetDailyReport_ParkNotFound_ReturnsNotFound()
    {
        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetDailyReport(Guid.NewGuid(), new DateOnly(2026, 11, 15));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public void GetDailyReport_WithoutData_ReturnsHasDataFalseAnd24ZeroPoints()
    {
        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetDailyReport(_testPark.Id, new DateOnly(2026, 11, 15));

        Assert.True(result.Success);
        var report = result.Value!;
        Assert.False(report.HasData);
        Assert.Equal(0.0, report.TotalEnergy);
        Assert.Equal(0.0, report.PeakPowerKw);
        Assert.Equal(0.0, report.EquivalentSunHours);
        Assert.Equal(24, report.Points.Count);
        Assert.All(report.Points, p => Assert.Equal(0.0, p.Energy));
    }

    [Fact]
    public void GetDailyReport_WithData_Aggregates24Hours_CalculatesHspAndPeak()
    {
        var date = new DateOnly(2026, 11, 15);
        // Seed 2 hours of generation: 10:00 (100 kWh, peak 120 kW) and 11:00 (150 kWh, peak 180 kW)
        _genRepo.AddRange(new[]
        {
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = date,
                HourLocal = 10,
                TimestampUtc = new DateTime(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc),
                EnergyGeneratedKwh = 100.0,
                PeakPowerKw = 120.0
            },
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = date,
                HourLocal = 11,
                TimestampUtc = new DateTime(2026, 11, 15, 14, 0, 0, DateTimeKind.Utc),
                EnergyGeneratedKwh = 150.0,
                PeakPowerKw = 180.0
            }
        });

        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetDailyReport(_testPark.Id, date);

        Assert.True(result.Success);
        var report = result.Value!;
        Assert.True(report.HasData);
        Assert.Equal(250.0, report.TotalEnergy);
        Assert.Equal(180.0, report.PeakPowerKw);
        Assert.Equal("11:00", report.PeakPowerTimeLocal);
        // HSP = 250 kWh / 500 kW = 0.5 h
        Assert.Equal(0.5, report.EquivalentSunHours);
        Assert.Equal("kWh", report.Unit);
        Assert.Equal(24, report.Points.Count);
        Assert.Equal(100.0, report.Points[10].Energy);
        Assert.Equal(150.0, report.Points[11].Energy);
        Assert.Equal(0.0, report.Points[0].Energy);
    }

    [Fact]
    public void GetDailyReport_WithPreviousPeriod_CalculatesComparisonPercentage()
    {
        var date = new DateOnly(2026, 11, 15);
        var previousWeekDate = date.AddDays(-7);

        // Previous week: 200 kWh
        _genRepo.AddRange(new[]
        {
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = previousWeekDate,
                HourLocal = 12,
                TimestampUtc = new DateTime(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc),
                EnergyGeneratedKwh = 200.0,
                PeakPowerKw = 250.0
            }
        });

        // Current week: 250 kWh (+25%)
        _genRepo.AddRange(new[]
        {
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = date,
                HourLocal = 12,
                TimestampUtc = new DateTime(2026, 11, 15, 15, 0, 0, DateTimeKind.Utc),
                EnergyGeneratedKwh = 250.0,
                PeakPowerKw = 300.0
            }
        });

        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetDailyReport(_testPark.Id, date);

        Assert.True(result.Success);
        var report = result.Value!;
        Assert.NotNull(report.PreviousPeriodComparisonPercent);
        Assert.Equal(25.0, report.PreviousPeriodComparisonPercent.Value, 1);
    }

    [Fact]
    public void GetMonthlyReport_AggregatesDaysInMonth_CalculatesPeakAndHsp()
    {
        // November 2026 has 30 days
        _genRepo.AddRange(new[]
        {
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = new DateOnly(2026, 11, 1),
                HourLocal = 12,
                EnergyGeneratedKwh = 500.0,
                PeakPowerKw = 400.0
            },
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = new DateOnly(2026, 11, 2),
                HourLocal = 13,
                EnergyGeneratedKwh = 700.0,
                PeakPowerKw = 450.0
            }
        });

        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetMonthlyReport(_testPark.Id, 2026, 11);

        Assert.True(result.Success);
        var report = result.Value!;
        Assert.True(report.HasData);
        Assert.Equal(30, report.Points.Count);
        Assert.Equal(1200.0, report.TotalEnergy);
        Assert.Equal(450.0, report.PeakPowerKw);
        // HSP = 1200 / 500 = 2.4 h
        Assert.Equal(2.4, report.EquivalentSunHours);
        Assert.Equal("kWh", report.Unit);
        Assert.Equal(500.0, report.Points[0].Energy);
        Assert.Equal(700.0, report.Points[1].Energy);
    }

    [Fact]
    public void GetMonthlyReport_LeapYearFebruary_Generates29Days()
    {
        // February 2028 is a leap year (29 days)
        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetMonthlyReport(_testPark.Id, 2028, 2);

        Assert.True(result.Success);
        var report = result.Value!;
        Assert.Equal(29, report.Points.Count);
        Assert.Equal("01", report.Points[0].Label);
        Assert.Equal("29", report.Points[28].Label);
    }

    [Fact]
    public void GetYearlyReport_Aggregates12MonthsInMwh()
    {
        // Seed 1,000,000 kWh in January 2026 (1,000 MWh) and 2,000,000 kWh in February 2026 (2,000 MWh)
        _genRepo.AddRange(new[]
        {
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = new DateOnly(2026, 1, 15),
                HourLocal = 12,
                EnergyGeneratedKwh = 1000.0, // 1 MWh
                PeakPowerKw = 420.0
            },
            new ParkHourlyGeneration
            {
                Id = Guid.NewGuid(),
                SolarParkId = _testPark.Id,
                DateLocal = new DateOnly(2026, 2, 10),
                HourLocal = 12,
                EnergyGeneratedKwh = 2000.0, // 2 MWh
                PeakPowerKw = 480.0
            }
        });

        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetYearlyReport(_testPark.Id, 2026);

        Assert.True(result.Success);
        var report = result.Value!;
        Assert.True(report.HasData);
        Assert.Equal(12, report.Points.Count);
        Assert.Equal("MWh", report.Unit);
        Assert.Equal(3.0, report.TotalEnergy, 2); // 3000 kWh = 3 MWh
        Assert.Equal(480.0, report.PeakPowerKw);
        Assert.Equal(1.0, report.Points[0].Energy, 2);
        Assert.Equal(2.0, report.Points[1].Energy, 2);
        Assert.Equal(0.0, report.Points[2].Energy);
    }

    [Fact]
    public void GetMonthlyReport_InvalidMonth_ReturnsValidation()
    {
        var service = new ParkHistoricalGenerationService(_parkRepo, _genRepo);
        var result = service.GetMonthlyReport(_testPark.Id, 2026, 13);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
    }
}

