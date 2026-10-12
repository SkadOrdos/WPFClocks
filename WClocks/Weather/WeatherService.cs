using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WClocks
{
    public class WeatherArgs : EventArgs
    {
        public int TemperatureC;
    }


    internal class WeatherService
    {
        public event EventHandler<WeatherArgs> WeatherUpdate = delegate { };

        DispatcherTimer timer;
        long updateInterval = -1;
        public WeatherService(TimeSpan timeSpan)
        {
            updateInterval = (long)timeSpan.TotalMilliseconds;
            if ((long)timeSpan.TotalMilliseconds > 0)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(17);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.UtcNow - lastCheckTime).TotalMilliseconds >= updateInterval)
                UpdateWeatherStateAsync();
        }

        DateTime lastCheckTime = DateTime.MinValue;
        public Task UpdateWeatherStateAsync()
        {
            return Task.Factory.StartNew(() => UpdateWeatherState());
        }

        public void UpdateWeatherState()
        {
            try
            {
                lastCheckTime = DateTime.UtcNow;
                using (var watcher = new System.Device.Location.GeoCoordinateWatcher(System.Device.Location.GeoPositionAccuracy.Default))
                {
                    // Do not suppress prompt
                    watcher.TryStart(false, TimeSpan.FromMilliseconds(1000));
                    while (watcher.Permission == System.Device.Location.GeoPositionPermission.Unknown ||
                        DateTime.UtcNow - lastCheckTime <= TimeSpan.FromMilliseconds(1000))
                    {
                        Task.Delay(127).Wait(); // Need wait to grant permission
                    }                    

                    System.Device.Location.GeoCoordinate coord = watcher.Position.Location;
                    if (!coord.IsUnknown)
                        RaiseWeatherUpdate(coord.Latitude, coord.Longitude);
                }
            }
            catch (Exception ex)
            {
            }
        }


        private void RaiseWeatherUpdate(double lat, double lon)
        {
            try
            {
                // Request current weather from Open-Meteo API
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,rain,wind_speed_10m&forecast_days=1";

                using (var client = new System.Net.Http.HttpClient())
                {
                    // Synchronously wait for the result to keep signature unchanged
                    string json = client.GetStringAsync(url).GetAwaiter().GetResult();

                    //var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var weather = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherOpenMeteoDTO>(json);

                    // If we have a valid weather object, invoke the event with the current temperature
                    if (weather?.current != null)
                    {
                        int tempC = (int)Math.Round(weather.current.temperature_2m);
                        WeatherUpdate.Invoke(this, new WeatherArgs() { TemperatureC = tempC });
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
