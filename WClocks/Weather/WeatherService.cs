using System;
using System.Device.Location;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WClocks
{
    public class WeatherArgs : EventArgs
    {
        public double Latitude;
        public double Longitude;
        public int TemperatureC;
        public int HumidityRh;
        public double WindKmh;
        public DateTime UtcTime;

        public WeatherArgs() { }
    }


    internal class WeatherService
    {
        public event EventHandler<WeatherArgs> WeatherUpdate = delegate { };

        public GeoCoordinate Coordinate { get; set; } = new GeoCoordinate(Double.NaN, Double.NaN);

        DispatcherTimer _timer;
        long _updateInterval = -1;
        public WeatherService(TimeSpan updateInterval)
        {
            _updateInterval = (long)updateInterval.TotalMilliseconds;
            if ((long)updateInterval.TotalMilliseconds > 0)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(1700);
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
        }

        public WeatherService(GeoCoordinate geoCoordinate, TimeSpan updateInterval)
            : this(updateInterval)
        {
            Coordinate = geoCoordinate;
        }

        public void Start()
        {
            lastCheckTime = DateTime.MinValue; // Reset time to update timer method
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            lastCheckTime = DateTime.MaxValue; // Disable timer method condition
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.UtcNow - lastCheckTime).TotalMilliseconds >= _updateInterval)
                UpdateWeatherStateAsync();
        }

        DateTime lastCheckTime = DateTime.MinValue;
        public Task UpdateWeatherStateAsync()
        {
            return Task.Factory.StartNew(() => UpdateWeatherState());
        }

        public void UpdateWeatherState(GeoCoordinate newCoordinate)
        {
            lastCheckTime = DateTime.UtcNow;
            this.Coordinate = newCoordinate;
            RaiseWeatherUpdate(newCoordinate.Latitude, newCoordinate.Longitude);
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

                    System.Device.Location.GeoCoordinate location = watcher.Position.Location;
                    if (!location.IsUnknown) Coordinate = new GeoCoordinate(location.Latitude, location.Longitude);

                    // Get weather by saved location
                    if (!this.Coordinate.IsUnknown)
                    {
                        RaiseWeatherUpdate(Coordinate.Latitude, Coordinate.Longitude);
                    }
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
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,rain,precipitation,wind_speed_10m&forecast_days=1";

                using (var client = new System.Net.Http.HttpClient())
                {
                    // Synchronously wait for the result to keep signature unchanged
                    string json = client.GetStringAsync(url).GetAwaiter().GetResult();

                    //var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var weather = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherOpenMeteoDTO>(json);

                    // If we have a valid weather object, invoke the event with the current temperature
                    if (weather?.current != null)
                    {
                        var currWeather = weather.current;
                        int tempC = (int)Math.Round(currWeather.temperature_2m);
                        WeatherUpdate.Invoke(this, new WeatherArgs()
                        {
                            Latitude = lat,
                            Longitude = lon,
                            TemperatureC = tempC,
                            HumidityRh = currWeather.relative_humidity_2m,
                            WindKmh = currWeather.wind_speed_10m,
                            UtcTime = DateTime.Parse(currWeather.time)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
