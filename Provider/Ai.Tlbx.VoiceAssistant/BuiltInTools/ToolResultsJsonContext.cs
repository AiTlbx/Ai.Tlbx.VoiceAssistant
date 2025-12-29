using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    [JsonSerializable(typeof(ToolSuccessResult<WeatherResult>))]
    [JsonSerializable(typeof(ToolSuccessResult<CalculatorResult>))]
    [JsonSerializable(typeof(ToolErrorResult))]
    [JsonSerializable(typeof(ToolValidationError))]
    [JsonSerializable(typeof(ToolTypeError))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(TimeTool.Args), TypeInfoPropertyName = "TimeToolArgs")]
    [JsonSerializable(typeof(WeatherTool.Args), TypeInfoPropertyName = "WeatherToolArgs")]
    [JsonSerializable(typeof(CalculatorTool.Args), TypeInfoPropertyName = "CalculatorToolArgs")]
    [JsonSerializable(typeof(TemperatureUnit))]
    [JsonSerializable(typeof(CalculatorOperation))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public partial class ToolResultsJsonContext : JsonSerializerContext
    {
    }
}
