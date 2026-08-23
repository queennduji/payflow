using FluentAssertions;
using Microsoft.Extensions.Options;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Infrastructure;

namespace Payflow.Authorization.UnitTests;

public class SimulatedCardNetworkClientTests
{
    private static SimulatedCardNetworkClient CreateClient(double faultRate) =>
        new(Options.Create(new ChaosOptions { CardNetworkFaultRate = faultRate }));

    [Fact]
    public async Task With_zero_fault_rate_it_always_delegates_to_the_decision_engine()
    {
        var client = CreateClient(faultRate: 0.0);

        var result = await client.AuthorizeAsync(50m, "USD", "tok_visa", CancellationToken.None);

        result.Approved.Should().BeTrue();
    }

    [Fact]
    public async Task With_zero_fault_rate_the_blocked_test_token_still_declines()
    {
        var client = CreateClient(faultRate: 0.0);

        var result = await client.AuthorizeAsync(50m, "USD", "tok_declined", CancellationToken.None);

        result.Approved.Should().BeFalse();
        result.DeclineReason.Should().Be("insufficient_funds");
    }

    [Fact]
    public async Task With_a_guaranteed_fault_rate_it_throws_instead_of_deciding()
    {
        var client = CreateClient(faultRate: 1.0);

        var act = () => client.AuthorizeAsync(50m, "USD", "tok_visa", CancellationToken.None);

        await act.Should().ThrowAsync<CardNetworkUnavailableException>();
    }
}
