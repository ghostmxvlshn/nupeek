namespace Nupeek.Core.Tests;

public class OutputPathBuilderTests
{
    [Fact]
    public void BuildTypeOutputPath_BuildsExpectedLayout()
    {
        // Arrange
        const string root = "deps-src";
        const string packageId = "Azure.Messaging.ServiceBus";
        const string version = "7.20.1";
        const string tfm = "netstandard2.0";
        const string typeName = "Azure.Messaging.ServiceBus.ServiceBusSender";

        // Act
        var path = OutputPathBuilder.BuildTypeOutputPath(root, packageId, version, tfm, typeName);

        // Assert
        Assert.Contains(Path.Combine("packages", "azure.messaging.servicebus", "7.20.1", "netstandard2.0"), path, StringComparison.Ordinal);
        Assert.EndsWith("Azure_Messaging_ServiceBus_ServiceBusSender.decompiled.cs", path, StringComparison.Ordinal);
    }
}
