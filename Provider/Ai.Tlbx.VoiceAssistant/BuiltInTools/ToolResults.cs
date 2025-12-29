using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    public class ToolSuccessResult<TData>
    {
        [JsonPropertyName("success")]
        public bool Success { get; } = true;

        [JsonPropertyName("data")]
        public TData? Data { get; set; }

        public ToolSuccessResult() { }

        public ToolSuccessResult(TData? data) => Data = data;
    }

    public class WeatherResult
    {
        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public int Temperature { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonPropertyName("condition")]
        public string Condition { get; set; } = string.Empty;

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("wind_speed")]
        public int WindSpeed { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class CalculatorResult
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("a")]
        public double A { get; set; }

        [JsonPropertyName("b")]
        public double B { get; set; }

        [JsonPropertyName("result")]
        public double Result { get; set; }

        [JsonPropertyName("expression")]
        public string Expression { get; set; } = string.Empty;
    }

    public class ToolErrorResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; } = false;

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        public ToolErrorResult() { }

        public ToolErrorResult(string error) => Error = error;
    }

    public class ToolValidationError
    {
        [JsonPropertyName("success")]
        public bool Success { get; } = false;

        [JsonPropertyName("error")]
        public string Error { get; } = "missing_required_parameters";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("missing_parameters")]
        public List<string> MissingParameters { get; set; } = new();

        [JsonPropertyName("provided_parameters")]
        public List<string> ProvidedParameters { get; set; } = new();
    }

    public class ToolTypeError
    {
        [JsonPropertyName("success")]
        public bool Success { get; } = false;

        [JsonPropertyName("error")]
        public string Error { get; } = "invalid_parameter_type";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("expected_type")]
        public string ExpectedType { get; set; } = string.Empty;
    }
}
