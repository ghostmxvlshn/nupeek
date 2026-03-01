namespace Nupeek.Core.Tests;

public class TypeNameNormalizerTests
{
    [Theory]
    [InlineData("Polly.Retry.RetryStrategyOptions<T>", "Polly.Retry.RetryStrategyOptions`1")]
    [InlineData("Polly.Retry.RetryStrategyOptions<>", "Polly.Retry.RetryStrategyOptions`1")]
    [InlineData("My.Type<A, B>", "My.Type`2")]
    [InlineData("My.Type`1", "My.Type`1")]
    [InlineData("Polly.Policy", "Polly.Policy")]
    public void Normalize_ConvertsGenericFriendlyNames(string input, string expected)
    {
        // Act
        var result = TypeNameNormalizer.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
