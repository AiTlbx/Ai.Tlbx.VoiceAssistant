using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Example weather tool demonstrating parameter schema support.
    /// </summary>
    public class WeatherTool : ValidatedVoiceToolBase<WeatherTool.WeatherArgs>
    {
        /// <summary>
        /// Arguments for the weather tool.
        /// </summary>
        public class WeatherArgs
        {
            /// <summary>
            /// The location to get weather for.
            /// </summary>
            public string Location { get; set; } = string.Empty;

            /// <summary>
            /// The temperature unit.
            /// </summary>
            public string Unit { get; set; } = "celsius";
        }

        /// <inheritdoc/>
        public override string Name => "get_weather";

        /// <inheritdoc/>
        public override string Description => "Get the current weather for a specific location";

        /// <inheritdoc/>
        public override ToolParameterSchema GetParameterSchema() => new()
        {
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["location"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "The city and state/country, e.g., 'San Francisco, CA' or 'London, UK'",
                    MinLength = 1
                },
                ["unit"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "The temperature unit to use",
                    Enum = new List<string> { "celsius", "fahrenheit" },
                    Default = "celsius"
                }
            },
            Required = new List<string> { "location" }
        };

        /// <inheritdoc/>
        protected override Task<string> ExecuteValidatedAsync(WeatherArgs args)
        {
            // This is a mock implementation
            // In a real implementation, you would call a weather API
            
            var random = new Random();
            var temperature = random.Next(10, 30);
            var conditions = new[] { "sunny", "cloudy", "rainy", "partly cloudy" };
            var condition = conditions[random.Next(conditions.Length)];

            var unitSymbol = args.Unit == "fahrenheit" ? "°F" : "°C";
            if (args.Unit == "fahrenheit")
            {
                temperature = (int)(temperature * 9.0 / 5.0 + 32);
            }

            var result = new
            {
                location = args.Location,
                temperature = temperature,
                unit = unitSymbol,
                condition = condition,
                humidity = random.Next(40, 80),
                wind_speed = random.Next(5, 25),
                description = $"Currently {temperature}{unitSymbol} and {condition} in {args.Location}"
            };

            return Task.FromResult(CreateSuccessResult(result));
        }
    }
}