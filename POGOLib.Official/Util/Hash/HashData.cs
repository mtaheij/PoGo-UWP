using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POGOLib.Official.Util.Hash
{
    public class HashData
    {

        public uint LocationAuthHash { get; set; }

        public uint LocationHash { get; set; }

        public ulong[] RequestHashes { get; set; }

    }
}
