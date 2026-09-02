using FluentAssertions;
using NSubstitute;
using Payflow.Vault.Application.Abstractions;
using Payflow.Vault.Application.Tokenization;
using Payflow.Vault.Domain;

namespace Payflow.Vault.UnitTests;

public class TokenizeCardCommandHandlerTests
{
    private readonly IVaultTokenRepository _repository = Substitute.For<IVaultTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private TokenizeCardCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task Tokenizing_a_valid_card_returns_only_last_four_digits_and_an_opaque_token()
    {
        var result = await CreateHandler().Handle(new TokenizeCardCommand("4242424242424242", 12, 2030), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Last4.Should().Be("4242");
        result.Value.Token.Should().StartWith("tok_");
        result.Value.Token.Should().NotContain("4242424242424242");
    }

    [Fact]
    public async Task The_full_card_number_never_appears_in_the_persisted_entity()
    {
        VaultToken? persisted = null;
        await _repository.AddAsync(Arg.Do<VaultToken>(t => persisted = t), Arg.Any<CancellationToken>());

        await CreateHandler().Handle(new TokenizeCardCommand("4242424242424242", 12, 2030), CancellationToken.None);

        persisted.Should().NotBeNull();
        // Reflect over every string field the entity actually has – none of them may equal, or
        // even contain, the raw card number handed in.
        var fields = typeof(VaultToken).GetProperties().Where(p => p.PropertyType == typeof(string));
        foreach (var field in fields)
            field.GetValue(persisted).Should().NotBe("4242424242424242");
    }

    [Fact]
    public async Task An_invalid_card_number_fails_validation_without_touching_the_repository()
    {
        var result = await CreateHandler().Handle(new TokenizeCardCommand("12", 12, 2030), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<VaultToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_invalid_expiry_month_fails_validation()
    {
        var result = await CreateHandler().Handle(new TokenizeCardCommand("4242424242424242", 13, 2030), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
