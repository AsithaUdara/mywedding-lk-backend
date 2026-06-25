using System.Collections.Generic;
using System.Linq;
using MyWedding.Domain.Entities;
using MyWedding.Domain.Helpers;

namespace MyWedding.Vendors.Application.Helpers
{
    public static class VendorMediaHelper
    {
        public static IEnumerable<VendorService> ActiveServices(Vendor vendor) =>
            vendor.Services.Where(s => s.IsActive);

        public static string? ResolvePrimaryImageUrl(Vendor vendor, IEnumerable<VendorService>? activeServices = null)
        {
            var services = activeServices?.ToList() ?? ActiveServices(vendor).ToList();
            var cheapest = services.OrderBy(s => s.BasePrice).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(cheapest?.PrimaryImageUrl))
            {
                return cheapest.PrimaryImageUrl;
            }

            if (!string.IsNullOrWhiteSpace(vendor.CoverImageUrl))
            {
                return vendor.CoverImageUrl;
            }

            return services
                .Select(s => s.PrimaryImageUrl)
                .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
        }

        public static IReadOnlyList<string> ResolveGalleryUrls(Vendor vendor, IEnumerable<VendorService>? activeServices = null)
        {
            var urls = new List<string>();
            var services = activeServices?.ToList() ?? ActiveServices(vendor).ToList();

            foreach (var service in services)
            {
                if (!string.IsNullOrWhiteSpace(service.PrimaryImageUrl))
                {
                    urls.Add(service.PrimaryImageUrl!);
                }

                urls.AddRange(GalleryUrlHelper.Parse(service.GalleryUrlsJson));
            }

            if (!string.IsNullOrWhiteSpace(vendor.CoverImageUrl))
            {
                urls.Insert(0, vendor.CoverImageUrl);
            }

            urls.AddRange(GalleryUrlHelper.Parse(vendor.GalleryUrlsJson));

            return urls.Distinct().ToList();
        }
    }
}
