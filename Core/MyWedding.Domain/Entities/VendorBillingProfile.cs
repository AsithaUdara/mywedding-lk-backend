namespace MyWedding.Domain.Entities;

/// <summary>
/// Masked payment method metadata only — never store full card numbers (PCI).
/// </summary>
public class VendorBillingProfile
{
    public required string VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public string? CardholderName { get; set; }
    public string? CardBrand { get; set; }
    public string? Last4 { get; set; }
    public byte? ExpiryMonth { get; set; }
    public short? ExpiryYear { get; set; }
    public string? PayHerePaymentMethod { get; set; }

    public DateTime UpdatedAt { get; set; }
}
