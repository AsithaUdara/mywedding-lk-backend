namespace MyWedding.SharedKernel.Exceptions
{
    public class ForbiddenAccessException : BaseException
    {
        public ForbiddenAccessException() 
            : base("Forbidden", "You do not have permission to access this resource.", 403) { }

        public ForbiddenAccessException(string message) 
            : base("Forbidden", message, 403) { }
    }
}
