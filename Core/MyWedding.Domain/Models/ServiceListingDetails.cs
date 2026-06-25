namespace MyWedding.Domain.Models
{
    public class ServiceListingDetails
    {
        public List<string> IncludedItems { get; set; } = [];
        public List<string> Highlights { get; set; } = [];
        public string? DurationLabel { get; set; }
        public string? CapacityNote { get; set; }
        public string? CancellationPolicy { get; set; }
    }
}
