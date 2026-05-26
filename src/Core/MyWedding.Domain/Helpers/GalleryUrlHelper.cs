using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MyWedding.Domain.Helpers
{
    public static class GalleryUrlHelper
    {
        public static IReadOnlyList<string> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json)?
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        public static string? Serialize(IEnumerable<string>? urls)
        {
            var list = urls?
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct()
                .ToList();

            if (list is null || list.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(list);
        }
    }
}
