using System.Collections.Generic;

namespace Ai.Tlbx.VoiceAssistant.Models
{
    /// <summary>
    /// Represents the schema for a voice tool's parameters.
    /// Built automatically by the ToolSchemaInferrer from a C# type.
    /// </summary>
    public class ToolSchema
    {
        /// <summary>
        /// The parameters of the tool, keyed by parameter name (snake_case).
        /// </summary>
        public Dictionary<string, ToolParameter> Parameters { get; set; } = new();

        /// <summary>
        /// List of required parameter names.
        /// </summary>
        public List<string> Required { get; set; } = new();

        /// <summary>
        /// When true, enables strict mode for OpenAI.
        /// Strict mode enforces exact schema adherence.
        /// Default is true for improved accuracy.
        /// </summary>
        public bool Strict { get; set; } = true;
    }

    /// <summary>
    /// Represents a single parameter in a tool schema.
    /// </summary>
    public class ToolParameter
    {
        /// <summary>
        /// The JSON Schema type of the parameter.
        /// </summary>
        public ToolParameterType Type { get; set; }

        /// <summary>
        /// When true, this parameter can be null.
        /// For OpenAI strict mode, this generates ["type", "null"].
        /// </summary>
        public bool Nullable { get; set; }

        /// <summary>
        /// Description of the parameter for the AI.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// For string types, the allowed enum values.
        /// </summary>
        public List<string>? Enum { get; set; }

        /// <summary>
        /// Default value for the parameter.
        /// </summary>
        public object? Default { get; set; }

        /// <summary>
        /// JSON Schema format hint (e.g., "date-time", "email", "uri").
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// For object types, the nested properties.
        /// </summary>
        public Dictionary<string, ToolParameter>? Properties { get; set; }

        /// <summary>
        /// For object types, list of required property names.
        /// </summary>
        public List<string>? RequiredProperties { get; set; }

        /// <summary>
        /// For array types, the schema of array items.
        /// </summary>
        public ToolParameter? Items { get; set; }

        /// <summary>
        /// For numeric types, the minimum allowed value.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// For numeric types, the maximum allowed value.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// For string types, the minimum length.
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// For string types, the maximum length.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// For string types, a regex pattern the value must match.
        /// </summary>
        public string? Pattern { get; set; }
    }

    /// <summary>
    /// JSON Schema types for tool parameters.
    /// </summary>
    public enum ToolParameterType
    {
        String,
        Integer,
        Number,
        Boolean,
        Object,
        Array
    }
}
