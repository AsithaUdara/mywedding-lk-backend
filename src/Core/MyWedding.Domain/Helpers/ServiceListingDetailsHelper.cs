using System.Text.Json;
using MyWedding.Domain.Models;

namespace MyWedding.Domain.Helpers
{
    public static class ServiceListingDetailsHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static ServiceListingDetails Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ServiceListingDetails();
            }

            try
            {
                return JsonSerializer.Deserialize<ServiceListingDetails>(json, JsonOptions)
                    ?? new ServiceListingDetails();
            }
            catch
            {
                return new ServiceListingDetails();
            }
        }

        public static string? Serialize(ServiceListingDetails? details)
        {
            if (details is null)
            {
                return null;
            }

            var hasContent =
                details.IncludedItems.Count > 0 ||
                details.Highlights.Count > 0 ||
                !string.IsNullOrWhiteSpace(details.DurationLabel) ||
                !string.IsNullOrWhiteSpace(details.CapacityNote) ||
                !string.IsNullOrWhiteSpace(details.CancellationPolicy);

            if (!hasContent)
            {
                return null;
            }

            return JsonSerializer.Serialize(details, JsonOptions);
        }
    }
}
