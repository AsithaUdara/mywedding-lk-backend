namespace MyWedding.SharedKernel.Interfaces;

public record InquiryQuotePdfModel(
    string QuoteReference,
    string VendorName,
    string PlannerDisplayName,
    string PlannerBusinessName,
    decimal Amount,
    string Currency,
    string RecipientEmail,
    string? EventName,
    string QuoteBody);

public interface IQuotePdfGenerator
{
    byte[] Generate(InquiryQuotePdfModel model);
}
