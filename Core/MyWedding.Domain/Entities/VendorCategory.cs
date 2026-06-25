using System;
using System.Collections.Generic;

namespace MyWedding.Domain.Entities
{
    public class VendorCategory
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
