namespace Nupeek.Core.Tests;

public class TfmSelectorTests
{
    [Fact]
    public void SelectBest_PrefersNet10()
    {
        // Arrange
        var tfms = new[] { "netstandard2.0", "net8.0", "net10.0" };

        // Act
        var result = TfmSelector.SelectBest(tfms);

        // Assert
        Assert.Equal("net10.0", result);
    }

    [Fact]
    public void SelectBest_FallsBackAlphabetical_WhenNoPriorityMatch()
    {
        // Arrange
        var tfms = new[] { "foo", "bar" };

        // Act
        var result = TfmSelector.SelectBest(tfms);

        // Assert
        Assert.Equal("bar", result);
    }
}
