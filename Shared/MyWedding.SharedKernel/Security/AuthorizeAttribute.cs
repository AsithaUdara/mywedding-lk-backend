using System;

namespace MyWedding.SharedKernel.Security
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute
    {
        public string Policy { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
    }
}
