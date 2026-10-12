using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WClocks
{
    internal class WeatherOpenMeteoDTO
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double generationtime_ms { get; set; }
        public int utc_offset_seconds { get; set; }
        public string timezone { get; set; }
        public string timezone_abbreviation { get; set; }
        public double elevation { get; set; }
        public OpenMeteoCurrentUnits current_units { get; set; }
        public OpenMeteoCurrent current { get; set; }
    }

    internal class OpenMeteoCurrentUnits
    {
        public string time { get; set; }
        public string interval { get; set; }
        public string temperature_2m { get; set; }
        public string relative_humidity_2m { get; set; }
        public string rain { get; set; }
        public string wind_speed_10m { get; set; }
    }

    internal class OpenMeteoCurrent
    {
        public string time { get; set; }
        public int interval { get; set; }
        public double temperature_2m { get; set; }
        public int relative_humidity_2m { get; set; }
        public double rain { get; set; }
        public double wind_speed_10m { get; set; }
    }
}
