// File: src/Core/MyWedding.Domain/Enums/PermissionLevel.cs
namespace MyWedding.Domain.Enums
{
    public enum PermissionLevel
    {
        Owner,      // Can do everything, including deleting the event
        Editor,     // Can manage tasks, vendors, budget, and invite others
        Viewer      // Can only view the event details, cannot make changes
    }
}
