namespace MyWedding.SharedKernel.Exceptions;

public class PaymentRequiredException : BaseException
{
    public PaymentRequiredException()
        : base("Payment Required", "Your planner subscription has expired. Renew to continue using CRM features.", 402)
    {
    }

    public PaymentRequiredException(string message)
        : base("Payment Required", message, 402)
    {
    }
}
