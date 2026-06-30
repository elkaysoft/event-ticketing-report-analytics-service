using System;
using System.Collections.Generic;
using System.Text;

namespace ETS.Domain.Errors
{
    public record Error
    {
        public static readonly Error None = new(string.Empty, string.Empty);
        public static readonly Error NullValue = new("99", "The value was not provided");


        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; set; }
        public string Message { get; set; }

        public static Error Create(string code, string message) => new(code, message);

    }
}
