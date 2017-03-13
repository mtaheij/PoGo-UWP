using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POGOLib.Official.LoginProviders
{
    public class PtcLoginException : Exception
    {
        public PtcLoginException(string message) : base(message)
        {
        }
    }
}
