using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POGOLib.Official.Util
{
    internal class LocationUtil
    {

        public static float OffsetLatitudeLongitude(double lat, double ran)
        {
            const int round = 6378137;
            var dl = ran / (round * Math.Cos(Math.PI * lat / 180));

            return (float)(lat + dl * 180 / Math.PI);
        }

    }
}
