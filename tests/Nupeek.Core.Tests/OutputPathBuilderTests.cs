namespace Nupeek.Core.Tests;

public class OutputPathBuilderTests
{
    [Fact]
    public void BuildTypeOutputPath_BuildsExpectedLayout()
    {
        var path = OutputPathBuilder.BuildTypeOutputPath(
            root: "deps-src",
            packageId: "Azure.Messaging.ServiceBus",
            version: "7.20.1",
            tfm: "netstandard2.0",
            fullTypeName: "Azure.Messaging.ServiceBus.ServiceBusSender");

        Assert.Contains(Path.Combine("packages", "azure.messaging.servicebus", "7.20.1", "netstandard2.0"), path, StringComparison.Ordinal);
        Assert.EndsWith("Azure_Messaging_ServiceBus_ServiceBusSender.decompiled.cs", path, StringComparison.Ordinal);
    }
}
