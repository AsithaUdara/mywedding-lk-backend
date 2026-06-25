namespace MyWedding.Domain.Entities
{
    public class User
    {
        // This will be the Firebase UID
        public required string Id { get; set; }

        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}