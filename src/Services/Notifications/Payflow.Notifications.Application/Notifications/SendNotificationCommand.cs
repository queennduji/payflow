using MediatR;
using Payflow.Shared.Kernel;

namespace Payflow.Notifications.Application.Notifications;

public sealed record SendNotificationCommand(Guid PaymentId, string MerchantId, string Status) : IRequest<Result<NotificationResult>>;

public sealed record NotificationResult(Guid PaymentId, string MerchantId, string Status, DateTimeOffset SentAt);
