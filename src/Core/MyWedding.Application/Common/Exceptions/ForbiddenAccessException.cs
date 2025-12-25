// File: src/Core/MyWedding.Application/Common/Exceptions/ForbiddenAccessException.cs
using System;

namespace MyWedding.Application.Common.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message) : base(message)
        {
        }
    }
}
