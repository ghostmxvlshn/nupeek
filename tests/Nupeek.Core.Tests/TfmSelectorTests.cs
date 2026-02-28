using Nupeek.Core;

namespace Nupeek.Core.Tests;

public class TfmSelectorTests
{
    [Fact]
    public void SelectBest_PrefersNet10()
    {
        var result = TfmSelector.SelectBest(["netstandard2.0", "net8.0", "net10.0"]);
        Assert.Equal("net10.0", result);
    }

    [Fact]
    public void SelectBest_FallsBackAlphabetical_WhenNoPriorityMatch()
    {
        var result = TfmSelector.SelectBest(["foo", "bar"]);
        Assert.Equal("bar", result);
    }
}
