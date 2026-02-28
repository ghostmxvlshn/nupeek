namespace Nupeek.Core.Tests;

public class SymbolParserTests
{
    [Fact]
    public void ToTypeName_FromMethodSymbol_ReturnsTypeName()
    {
        // Arrange
        const string symbol = "Polly.Policy.Handle";

        // Act
        var result = SymbolParser.ToTypeName(symbol);

        // Assert
        Assert.Equal("Polly.Policy", result);
    }

    [Fact]
    public void ToTypeName_FromSingleToken_ReturnsOriginal()
    {
        // Arrange
        const string symbol = "ServiceBusSender";

        // Act
        var result = SymbolParser.ToTypeName(symbol);

        // Assert
        Assert.Equal("ServiceBusSender", result);
    }
}
