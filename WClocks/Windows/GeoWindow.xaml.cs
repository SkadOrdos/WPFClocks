using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WClocks
{
    public class NamedCoorditate : GeoCoordinate
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public NamedCoorditate(double latitude, double longitude)
            : base(latitude, longitude)
        {
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class CoordinateArgs : EventArgs
    {
        public GeoCoordinate Coordinate;
        public CoordinateArgs(GeoCoordinate coordinate)
        {
            Coordinate = coordinate;
        }
    }


    public partial class GeoWindow : Window
    {
        private readonly ObservableCollection<NamedCoorditate> _cities = new ObservableCollection<NamedCoorditate>();

        private GeoCoordinate _coordinates;

        public GeoWindow(GeoCoordinate coordinates)
        {
            InitializeComponent();

            this._coordinates = coordinates;
            CityComboBox.ItemsSource = _cities;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchCitiesAsync();
        }

        private async void CityComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SearchCitiesAsync();
            }
        }

        private async Task SearchCitiesAsync()
        {
            string query = CityComboBox.Text.Trim();
            if (query.Length < 2)
                return;

            try
            {
                _cities.Clear();
                string url = "https://nominatim.openstreetmap.org/search" +
                    $"?city={Uri.EscapeDataString(query)}&format=geojson&accept-language=en";

                using (var client = new System.Net.Http.HttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("User-Agent", "Proxy"); // User agent is required by Nominatim API
                    // Synchronously wait for the result to keep signature unchanged
                    using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var result = ParseCities(json);

                        foreach (var city in result)
                        {
                            _cities.Add(city);
                        }
                    }
                }

                if (_cities.Count == 1)
                {
                    CityComboBox.SelectedIndex = 0;
                    UpdateCoordinates(_cities[0]);
                }
                else if (_cities.Count > 1)
                {
                    CityComboBox.IsDropDownOpen = true;
                }
            }
            catch (HttpRequestException ex)
            {
            }
            catch (JsonException ex)
            {
            }
        }

        public static List<NamedCoorditate> ParseCities(string responseJson)
        {
            JObject json = JObject.Parse(responseJson);
            JArray features = json["features"] as JArray;

            if (features == null)
                return new List<NamedCoorditate>();

            return features.OfType<JObject>().Select(feature =>
                {
                    JToken properties = feature["properties"];
                    JToken coordinates = feature["geometry"]?["coordinates"];

                    JArray coords = coordinates as JArray;
                    if (properties == null || coords == null || coords.Count < 2)
                        return null;

                    // GeoJSON order: longitude, latitude
                    return new NamedCoorditate((double)coords[1], (double)coords[0])
                    {
                        Name = (string)properties["name"] ?? string.Empty,
                        DisplayName = (string)properties["display_name"] ?? string.Empty,
                    };
                })
                .Where(city => city != null).OrderBy(city => city.DisplayName).ToList();
        }

        private void CityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CityComboBox.SelectedItem is GeoCoordinate city)
            {
                UpdateCoordinates(city);
            }
        }

        private void UpdateCoordinates(GeoCoordinate city)
        {
            CoordinatesText.Text = $"Latitude: {city.Latitude:F6}; Longitude: {city.Longitude:F6}";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (CityComboBox.SelectedItem is GeoCoordinate city)
            {
                this._coordinates = new GeoCoordinate(Math.Round(city.Latitude, 6), Math.Round(city.Longitude, 6));
            }
            else
            {
                this._coordinates = new GeoCoordinate(Double.NaN, Double.NaN);
            }

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public CoordinateArgs GetCoordinatesArgs()
        {
            if (this.DialogResult == true)
                return new CoordinateArgs(this._coordinates);
            return new CoordinateArgs(new GeoCoordinate(Double.NaN, Double.NaN));
        }
    }
}