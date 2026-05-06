using System;

namespace VentaExpress.Services
{
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
