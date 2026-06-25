using System;

namespace MyWedding.SharedKernel.Exceptions
{
    public abstract class BaseException : Exception
    {
        public string Title { get; }
        public int StatusCode { get; }

        protected BaseException(string title, string message, int statusCode) : base(message)
        {
            Title = title;
            StatusCode = statusCode;
        }
    }
}
