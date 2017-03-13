using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POGOLib.Official.Util.Hash
{
    public class HashVersionMismatchException : Exception
    {
        public HashVersionMismatchException(string message) : base(message)
        {
        }
    }
}
