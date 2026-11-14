using System;
using Temp;
using Xunit;

namespace Temp.Tests;

public sealed class MovingAverageTests
{
    [Fact]
    public void CalculatesAverageAndEvictsOldestValue()
    {
        var average = new MovingAverage(3);

        Assert.Equal(10, average.Add(10));
        Assert.Equal(15, average.Add(20));
        Assert.Equal(20, average.Add(30));
        Assert.Equal(30, average.Add(40));
        Assert.Equal(3, average.Count);
    }

    [Fact]
    public void RejectsNonFiniteValues()
    {
        var average = new MovingAverage();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => average.Add(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => average.Add(float.PositiveInfinity));
    }
}
