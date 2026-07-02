namespace MyWedding.Domain.ReadModels;

public class AdminVendorSummary
{
    public int Total { get; set; }
    public int Verified { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public int LiveListings { get; set; }
    public int Categories { get; set; }
}
