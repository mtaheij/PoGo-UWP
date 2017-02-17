using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGoAPI.Helpers
{
    public class LehmerRng
    {
        private const int a = 16807;
        private const int m = 2147483647;
        private const int q = 127773;
        private const int r = 2836;
        private int seed;
        public LehmerRng(int seed = 1)
        {
            if (seed <= 0 || seed == int.MaxValue)
                throw new Exception("Bad seed");
            this.seed = seed;
        }

        public int Next()
        {
            int hi = seed / q;
            int lo = seed % q;
            int t = (a * lo) - (r * hi);
            if (t < 0)
                t += m;
            seed = t % unchecked((int)0x80000000);
            return seed;
        }
    }
}
