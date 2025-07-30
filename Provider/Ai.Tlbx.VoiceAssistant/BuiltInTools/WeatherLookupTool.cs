using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Weather lookup tool that demonstrates parameter schema features with place and time inputs.
    /// Returns a simulated weather report with realistic variations.
    /// </summary>
    public class WeatherLookupTool : ValidatedVoiceToolBase<WeatherLookupTool.WeatherLookupArgs>
    {
        /// <summary>
        /// Arguments for the weather lookup tool.
        /// </summary>
        public class WeatherLookupArgs
        {
            /// <summary>
            /// The location to get weather for.
            /// </summary>
            public string Place { get; set; } = string.Empty;

            /// <summary>
            /// The date/time for the weather forecast.
            /// </summary>
            public string Time { get; set; } = "now";

            /// <summary>
            /// The temperature unit.
            /// </summary>
            public string Unit { get; set; } = "celsius";

            /// <summary>
            /// Whether to include detailed information.
            /// </summary>
            public bool Detailed { get; set; } = false;
        }

        private readonly Random _random = new();
        private readonly string[] _conditions = 
        {
            "clear", "partly cloudy", "mostly cloudy", "overcast", 
            "light rain", "moderate rain", "heavy rain", "thunderstorms",
            "light snow", "moderate snow", "foggy", "windy"
        };

        private readonly string[] _windDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        /// <inheritdoc/>
        public override string Name => "weather_lookup";

        /// <inheritdoc/>
        public override string Description => "Look up weather for a specific place and time with detailed forecast information";

        /// <inheritdoc/>
        public override ToolParameterSchema GetParameterSchema() => new()
        {
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["place"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "The location to get weather for (city, address, or coordinates)",
                    MinLength = 1
                },
                ["time"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "When to get weather for: 'now', 'today', 'tomorrow', or specific date/time (e.g., '2024-12-25 15:00')",
                    Default = "now"
                },
                ["unit"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "Temperature unit preference",
                    Enum = new List<string> { "celsius", "fahrenheit", "kelvin" },
                    Default = "celsius"
                },
                ["detailed"] = new ParameterProperty
                {
                    Type = "boolean",
                    Description = "Include detailed forecast information",
                    Default = false
                }
            },
            Required = new List<string> { "place" }
        };

        /// <inheritdoc/>
        protected override Task<string> ExecuteValidatedAsync(WeatherLookupArgs args)
        {
            // Parse the time
            var requestedTime = ParseTime(args.Time);
            var isNow = args.Time.ToLowerInvariant() == "now";
            var isFuture = requestedTime > DateTime.Now;
            
            // Generate base temperature based on time of day and season
            var baseTemp = GenerateBaseTemperature(requestedTime);
            
            // Add some variance
            var temperature = baseTemp + _random.Next(-5, 6);
            
            // Select weather condition with some logic
            var condition = SelectWeatherCondition(temperature, requestedTime);
            
            // Convert temperature to requested unit
            var (convertedTemp, unitSymbol) = ConvertTemperature(temperature, args.Unit);
            
            // Generate other weather data
            var humidity = GenerateHumidity(condition);
            var windSpeed = _random.Next(5, 30);
            var windDirection = _windDirections[_random.Next(_windDirections.Length)];
            var pressure = _random.Next(990, 1030);
            var visibility = condition.Contains("fog") ? _random.Next(1, 5) : _random.Next(10, 20);
            
            // Build response
            var response = new Dictionary<string, object>
            {
                ["place"] = args.Place,
                ["time"] = requestedTime.ToString("yyyy-MM-dd HH:mm"),
                ["is_forecast"] = isFuture,
                ["temperature"] = convertedTemp,
                ["temperature_unit"] = unitSymbol,
                ["condition"] = condition,
                ["description"] = GenerateDescription(args.Place, requestedTime, convertedTemp, unitSymbol, condition),
                ["humidity_percent"] = humidity,
                ["wind_speed_kmh"] = windSpeed,
                ["wind_direction"] = windDirection
            };

            if (args.Detailed)
            {
                response["feels_like"] = convertedTemp + (windSpeed > 20 ? -2 : 0);
                response["pressure_hpa"] = pressure;
                response["visibility_km"] = visibility;
                response["uv_index"] = condition == "clear" && requestedTime.Hour > 6 && requestedTime.Hour < 18 ? _random.Next(3, 10) : 0;
                response["precipitation_chance_percent"] = condition.Contains("rain") || condition.Contains("snow") ? _random.Next(60, 95) : _random.Next(0, 20);
                response["sunrise"] = "06:45";
                response["sunset"] = "18:30";
                
                // Add hourly forecast if detailed
                if (isFuture)
                {
                    var hourly = new List<object>();
                    for (int i = 0; i < 6; i++)
                    {
                        var hour = requestedTime.AddHours(i);
                        var hourTemp = baseTemp + _random.Next(-3, 4);
                        var (hourConvertedTemp, _) = ConvertTemperature(hourTemp, args.Unit);
                        hourly.Add(new
                        {
                            time = hour.ToString("HH:mm"),
                            temperature = hourConvertedTemp,
                            condition = _conditions[_random.Next(_conditions.Length)]
                        });
                    }
                    response["hourly_forecast"] = hourly;
                }
            }

            return Task.FromResult(CreateSuccessResult(response));
        }

        private DateTime ParseTime(string time)
        {
            var now = DateTime.Now;
            return time.ToLowerInvariant() switch
            {
                "now" => now,
                "today" => now.Date.AddHours(12),
                "tomorrow" => now.Date.AddDays(1).AddHours(12),
                _ => DateTime.TryParse(time, out var parsed) ? parsed : now
            };
        }

        private int GenerateBaseTemperature(DateTime time)
        {
            // Base temperature by month (Northern Hemisphere)
            var monthTemp = time.Month switch
            {
                12 or 1 or 2 => 5,   // Winter
                3 or 4 or 5 => 15,   // Spring
                6 or 7 or 8 => 25,   // Summer
                9 or 10 or 11 => 15, // Fall
                _ => 20
            };

            // Adjust by time of day
            var hourAdjustment = time.Hour switch
            {
                >= 0 and < 6 => -5,   // Night
                >= 6 and < 12 => 0,   // Morning
                >= 12 and < 18 => 5,  // Afternoon
                _ => -2               // Evening
            };

            return monthTemp + hourAdjustment + _random.Next(-3, 4);
        }

        private string SelectWeatherCondition(int temperature, DateTime time)
        {
            // Snow only when cold
            if (temperature < 2 && _random.Next(100) < 30)
                return _conditions[_random.Next(8, 10)]; // Snow conditions

            // Rain more likely in spring/fall
            if ((time.Month >= 3 && time.Month <= 5) || (time.Month >= 9 && time.Month <= 11))
            {
                if (_random.Next(100) < 40)
                    return _conditions[_random.Next(4, 8)]; // Rain conditions
            }

            // Otherwise random from non-precipitation conditions
            return _conditions[_random.Next(0, 4)];
        }

        private int GenerateHumidity(string condition)
        {
            if (condition.Contains("rain") || condition.Contains("snow"))
                return _random.Next(70, 95);
            if (condition.Contains("fog"))
                return _random.Next(80, 100);
            if (condition == "clear")
                return _random.Next(30, 60);
            return _random.Next(40, 70);
        }

        private (double temperature, string unit) ConvertTemperature(double celsius, string targetUnit)
        {
            return targetUnit.ToLowerInvariant() switch
            {
                "fahrenheit" => (Math.Round(celsius * 9.0 / 5.0 + 32, 1), "°F"),
                "kelvin" => (Math.Round(celsius + 273.15, 1), "K"),
                _ => (Math.Round(celsius, 1), "°C")
            };
        }

        private string GenerateDescription(string place, DateTime time, double temperature, string unit, string condition)
        {
            var timeDesc = time.Date == DateTime.Today ? "Currently" : 
                          time.Date == DateTime.Today.AddDays(1) ? "Tomorrow" : 
                          $"On {time:MMMM d}";
            
            var tempDesc = temperature switch
            {
                < 0 => "freezing",
                < 10 => "cold",
                < 20 => "mild",
                < 30 => "warm",
                _ => "hot"
            };

            return $"{timeDesc}, the weather in {place} is {temperature}{unit} and {condition}. " +
                   $"It's {tempDesc} with {GetConditionDescription(condition)}.";
        }

        private string GetConditionDescription(string condition)
        {
            return condition switch
            {
                "clear" => "clear skies and good visibility",
                "partly cloudy" => "some clouds but mostly sunny",
                "mostly cloudy" => "significant cloud cover",
                "overcast" => "complete cloud coverage",
                "light rain" => "gentle precipitation",
                "moderate rain" => "steady rainfall",
                "heavy rain" => "intense precipitation",
                "thunderstorms" => "lightning and thunder activity",
                "light snow" => "gentle snowfall",
                "moderate snow" => "steady snow accumulation",
                "foggy" => "reduced visibility due to fog",
                "windy" => "strong wind conditions",
                _ => "variable conditions"
            };
        }
    }
}