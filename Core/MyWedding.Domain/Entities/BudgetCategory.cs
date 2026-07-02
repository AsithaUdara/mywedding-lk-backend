// File: src/Core/MyWedding.Domain/Entities/BudgetCategory.cs

using System;

namespace MyWedding.Domain.Entities
{
    public class BudgetCategory
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
