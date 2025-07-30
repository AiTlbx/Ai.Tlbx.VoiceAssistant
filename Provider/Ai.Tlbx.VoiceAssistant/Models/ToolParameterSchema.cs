using System.Collections.Generic;

namespace Ai.Tlbx.VoiceAssistant.Models
{
    /// <summary>
    /// Represents the parameter schema for a voice tool, compatible with OpenAI's function calling format.
    /// </summary>
    public class ToolParameterSchema
    {
        /// <summary>
        /// The JSON Schema type. Default is "object".
        /// </summary>
        public string Type { get; set; } = "object";

        /// <summary>
        /// The properties of the object schema, mapped by property name.
        /// </summary>
        public Dictionary<string, ParameterProperty> Properties { get; set; } = new();

        /// <summary>
        /// List of required property names.
        /// </summary>
        public List<string> Required { get; set; } = new();

        /// <summary>
        /// Whether additional properties are allowed. Default is false.
        /// </summary>
        public bool AdditionalProperties { get; set; } = false;
    }

    /// <summary>
    /// Represents a property in a tool parameter schema.
    /// </summary>
    public class ParameterProperty
    {
        /// <summary>
        /// The JSON Schema type (e.g., "string", "number", "boolean", "object", "array").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the property to help the AI understand its purpose.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// For string types, an enumeration of allowed values.
        /// </summary>
        public List<string> Enum { get; set; }

        /// <summary>
        /// Default value for the property.
        /// </summary>
        public object Default { get; set; }

        /// <summary>
        /// For object types, nested properties.
        /// </summary>
        public Dictionary<string, ParameterProperty> Properties { get; set; }

        /// <summary>
        /// For array types, the schema of array items.
        /// </summary>
        public ParameterProperty Items { get; set; }

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
        public string Pattern { get; set; }
    }
}