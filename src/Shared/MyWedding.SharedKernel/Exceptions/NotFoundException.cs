namespace MyWedding.SharedKernel.Exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string message) 
            : base("Not Found", message, 404) { }

        public NotFoundException(string name, object key) 
            : base("Not Found", $"Entity \"{name}\" ({key}) was not found.", 404) { }
    }
}
