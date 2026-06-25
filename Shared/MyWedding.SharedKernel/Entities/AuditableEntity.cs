using System;

namespace MyWedding.SharedKernel.Entities;

/// <summary>
/// Base class for entities that require audit tracking.
/// Provides standardized CreatedAt and UpdatedAt properties.
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
