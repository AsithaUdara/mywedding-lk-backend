// File: src/Core/MyWedding.Application/Common/Exceptions/NotFoundException.cs
using System;

namespace MyWedding.Application.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
