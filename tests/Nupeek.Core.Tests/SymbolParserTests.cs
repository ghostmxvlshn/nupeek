namespace Nupeek.Core.Tests;

public class SymbolParserTests
{
    [Fact]
    public void ToTypeName_PreservesQualifiedSymbol()
    {
        // Arrange
        const string symbol = "Polly.Policy.Handle";

        // Act
        var result = SymbolParser.ToTypeName(symbol);

        // Assert
        Assert.Equal("Polly.Policy.Handle", result);
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

    [Fact]
    public void ExtractMemberName_FromQualifiedSymbol_ReturnsLastToken()
    {
        // Arrange
        const string symbol = "Polly.Policy.Handle";

        // Act
        var result = SymbolParser.ExtractMemberName(symbol);

        // Assert
        Assert.Equal("Handle", result);
    }

    [Fact]
    public void ExtractMemberName_StripsMethodArguments()
    {
        // Arrange
        const string symbol = "Polly.Policy.Handle(System.Exception)";

        // Act
        var result = SymbolParser.ExtractMemberName(symbol);

        // Assert
        Assert.Equal("Handle", result);
    }
}
