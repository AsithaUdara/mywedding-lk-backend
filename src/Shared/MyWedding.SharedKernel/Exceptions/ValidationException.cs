using System.Collections.Generic;

namespace MyWedding.SharedKernel.Exceptions
{
    public class ValidationException : BaseException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException() 
            : base("Validation Error", "One or more validation failures have occurred.", 400)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IDictionary<string, string[]> errors) 
            : base("Validation Error", "One or more validation failures have occurred.", 400)
        {
            Errors = errors;
        }
    }
}
