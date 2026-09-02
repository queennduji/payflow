namespace Payflow.IntegrationTests;

/// <summary>
/// Shares one <see cref="PayflowInfrastructureFixture"/> (the containers) across every test class
/// in this assembly – they're expensive to start, cheap to share, and nothing about them needs to
/// vary between test classes (only the service-level config built on top of them does).
/// </summary>
[CollectionDefinition(Name)]
public sealed class PayflowCollection : ICollectionFixture<PayflowInfrastructureFixture>
{
    public const string Name = "Payflow infrastructure";
}
