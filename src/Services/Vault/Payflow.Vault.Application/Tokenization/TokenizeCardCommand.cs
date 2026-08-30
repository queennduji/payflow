using MediatR;
using Payflow.Shared.Kernel;

namespace Payflow.Vault.Application.Tokenization;

public sealed record TokenizeCardCommand(string CardNumber, int ExpiryMonth, int ExpiryYear) : IRequest<Result<TokenizeCardResult>>;

/// <summary>The only thing a caller ever gets back — no field here could carry a full card number.</summary>
public sealed record TokenizeCardResult(string Token, string Last4, int ExpiryMonth, int ExpiryYear);
