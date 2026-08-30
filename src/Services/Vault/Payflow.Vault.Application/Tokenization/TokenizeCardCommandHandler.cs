using MediatR;
using Payflow.Shared.Kernel;
using Payflow.Vault.Application.Abstractions;
using Payflow.Vault.Domain;

namespace Payflow.Vault.Application.Tokenization;

public sealed class TokenizeCardCommandHandler(IVaultTokenRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<TokenizeCardCommand, Result<TokenizeCardResult>>
{
    public async Task<Result<TokenizeCardResult>> Handle(TokenizeCardCommand request, CancellationToken cancellationToken)
    {
        VaultToken token;
        try
        {
            token = VaultToken.Issue(request.CardNumber, request.ExpiryMonth, request.ExpiryYear);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<TokenizeCardResult>(Error.Validation("Vault.InvalidCard", ex.Message));
        }

        await repository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new TokenizeCardResult(token.Token, token.Last4, token.ExpiryMonth, token.ExpiryYear));
    }
}
