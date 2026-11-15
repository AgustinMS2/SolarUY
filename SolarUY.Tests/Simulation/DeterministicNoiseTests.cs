using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class DeterministicNoiseTests
{
    private static readonly Guid SeedA = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");
    private static readonly Guid SeedB = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");
    private static readonly DateTime Start = new(2026, 11, 15, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Smooth_WithTheSameInputs_AlwaysGivesTheSameValue()
    {
        var instant = Start.AddSeconds(137.4);

        Assert.Equal(DeterministicNoise.Smooth(SeedA, instant, 60), DeterministicNoise.Smooth(SeedA, instant, 60));
        Assert.Equal(DeterministicNoise.Smooth(SeedA, instant, 60, channel: 3), DeterministicNoise.Smooth(SeedA, instant, 60, channel: 3));
    }

    [Fact]
    public void Smooth_IsAlwaysBetweenMinusOneAndOne()
    {
        for (var i = 0; i < 5000; i++)
        {
            var value = DeterministicNoise.Smooth(SeedA, Start.AddSeconds(i * 7.3), 60);
            Assert.InRange(value, -1.0, 1.0);
        }
    }

    [Fact]
    public void Smooth_ChangesGraduallyBetweenCloseInstants()
    {
        // Con puntos de control cada 60 s la pendiente máxima es 1,5 × 2 / 60 = 0,05 por segundo.
        for (var i = 0; i < 2000; i++)
        {
            var instant = Start.AddSeconds(i * 3.1);
            var step = Math.Abs(DeterministicNoise.Smooth(SeedA, instant.AddSeconds(1), 60) - DeterministicNoise.Smooth(SeedA, instant, 60));
            Assert.True(step <= 0.06, $"Salto de {step} en {instant:O}");
        }
    }

    [Fact]
    public void Smooth_AtAKnotEqualsTheKnotValue()
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var knotInstant = epoch.AddSeconds(60 * 12345);

        Assert.Equal(DeterministicNoise.KnotValue(SeedA, 12345), DeterministicNoise.Smooth(SeedA, knotInstant, 60), precision: 9);
    }

    [Fact]
    public void Smooth_DifferentSeedsGiveDifferentSequences()
    {
        var different = 0;
        for (var i = 0; i < 100; i++)
        {
            var instant = Start.AddMinutes(i);
            if (Math.Abs(DeterministicNoise.Smooth(SeedA, instant, 60) - DeterministicNoise.Smooth(SeedB, instant, 60)) > 1e-6)
            {
                different++;
            }
        }

        Assert.True(different > 90, $"Solo {different} de 100 instantes difieren");
    }

    [Fact]
    public void Smooth_DifferentChannelsAreIndependentSequences()
    {
        var different = 0;
        for (var i = 0; i < 100; i++)
        {
            var instant = Start.AddMinutes(i);
            if (Math.Abs(DeterministicNoise.Smooth(SeedA, instant, 60, channel: 1) - DeterministicNoise.Smooth(SeedA, instant, 60, channel: 2)) > 1e-6)
            {
                different++;
            }
        }

        Assert.True(different > 90);
    }

    [Fact]
    public void Smooth_HasNoSystematicBias()
    {
        double sum = 0;
        const int knots = 3000;
        for (var i = 0; i < knots; i++)
        {
            sum += DeterministicNoise.KnotValue(SeedA, i);
        }

        Assert.InRange(sum / knots, -0.08, 0.08);
    }

    [Fact]
    public void Smooth_IgnoresTheDateKind()
    {
        var utc = new DateTime(2026, 11, 15, 15, 0, 0, DateTimeKind.Utc);
        var unspecified = new DateTime(2026, 11, 15, 15, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(DeterministicNoise.Smooth(SeedA, utc, 60), DeterministicNoise.Smooth(SeedA, unspecified, 60));
    }

    [Fact]
    public void Unit_IsInsideZeroAndOneAndRoughlyUniform()
    {
        var below = 0;
        const int samples = 5000;
        for (var i = 0; i < samples; i++)
        {
            var value = DeterministicNoise.Unit(SeedA, i);
            Assert.InRange(value, 0.0, 0.9999999999);
            if (value < 0.5)
            {
                below++;
            }
        }

        Assert.InRange(below / (double)samples, 0.45, 0.55);
    }

    [Fact]
    public void Unit_IsDeterministicAndDependsOnSeedIndexAndChannel()
    {
        Assert.Equal(DeterministicNoise.Unit(SeedA, 42), DeterministicNoise.Unit(SeedA, 42));
        Assert.NotEqual(DeterministicNoise.Unit(SeedA, 42), DeterministicNoise.Unit(SeedB, 42));
        Assert.NotEqual(DeterministicNoise.Unit(SeedA, 42), DeterministicNoise.Unit(SeedA, 43));
        Assert.NotEqual(DeterministicNoise.Unit(SeedA, 42, channel: 0), DeterministicNoise.Unit(SeedA, 42, channel: 1));
    }
}
