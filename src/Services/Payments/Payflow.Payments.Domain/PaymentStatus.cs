namespace Payflow.Payments.Domain;

public enum PaymentStatus
{
    Pending,
    Authorized,
    Declined,
    Captured,
    Failed
}
