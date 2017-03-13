using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POGOLib.Official.Net.Authentication.Exceptions
{
    public class WrongCredentialsException : Exception
    {
        public WrongCredentialsException(string message) : base(message)
        {

        }
    }
}
