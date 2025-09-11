using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common;

namespace Service
{
    class Database
    {
        readonly static Dictionary<long, WeatherSample> collectionOfWeatherData;

        static Database()
        {
            collectionOfWeatherData = new Dictionary<long, WeatherSample>();
        }

        public static Dictionary<long, WeatherSample> CollectionOfWeatherData { get { return collectionOfWeatherData; } }

    }
}
