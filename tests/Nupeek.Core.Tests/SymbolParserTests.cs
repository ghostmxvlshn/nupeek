using Nupeek.Core;

namespace Nupeek.Core.Tests;

public class SymbolParserTests
{
    [Fact]
    public void ToTypeName_FromMethodSymbol_ReturnsTypeName()
    {
        var result = SymbolParser.ToTypeName("Polly.Policy.Handle");
        Assert.Equal("Polly.Policy", result);
    }

    [Fact]
    public void ToTypeName_FromSingleToken_ReturnsOriginal()
    {
        var result = SymbolParser.ToTypeName("ServiceBusSender");
        Assert.Equal("ServiceBusSender", result);
    }
}
