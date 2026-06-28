namespace MyWedding.Domain.ReadModels;

public class PayoutDueSummary
{
    public int Count { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalVendorNet { get; set; }
}
