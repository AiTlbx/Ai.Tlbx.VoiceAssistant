using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    public enum TemperatureUnit
    {
        Celsius,
        Fahrenheit
    }

    [Description("Get the current weather for a specific location")]
    public class WeatherTool : VoiceToolBase<WeatherTool.Args>
    {
        public record Args(
            [property: Description("The city and state/country, e.g., 'San Francisco, CA' or 'London, UK'")] string Location,
            [property: Description("The temperature unit to use")] TemperatureUnit Unit = TemperatureUnit.Celsius
        );

        public override string Name => "get_weather";

        public override Task<string> ExecuteAsync(Args args)
        {
            var random = new Random();
            var temperature = random.Next(10, 30);
            var conditions = new[] { "sunny", "cloudy", "rainy", "partly cloudy" };
            var condition = conditions[random.Next(conditions.Length)];

            var unitSymbol = args.Unit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
            if (args.Unit == TemperatureUnit.Fahrenheit)
            {
                temperature = (int)(temperature * 9.0 / 5.0 + 32);
            }

            var result = new ToolSuccessResult<WeatherResult>(new WeatherResult
            {
                Location = args.Location,
                Temperature = temperature,
                Unit = unitSymbol,
                Condition = condition,
                Humidity = random.Next(40, 80),
                WindSpeed = random.Next(5, 25),
                Description = $"Currently {temperature}{unitSymbol} and {condition} in {args.Location}"
            });

            return Task.FromResult(JsonSerializer.Serialize(result, ToolResultsJsonContext.Default.ToolSuccessResultWeatherResult));
        }
    }
}
