using System;
using System.Collections.Generic;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.Reflection
{
    /// <summary>
    /// Maps C# types to JSON Schema types.
    /// </summary>
    public static class TypeMapper
    {
        private static readonly Dictionary<Type, ToolParameterType> _typeMap = new()
        {
            { typeof(string), ToolParameterType.String },
            { typeof(char), ToolParameterType.String },
            { typeof(int), ToolParameterType.Integer },
            { typeof(long), ToolParameterType.Integer },
            { typeof(short), ToolParameterType.Integer },
            { typeof(byte), ToolParameterType.Integer },
            { typeof(uint), ToolParameterType.Integer },
            { typeof(ulong), ToolParameterType.Integer },
            { typeof(ushort), ToolParameterType.Integer },
            { typeof(sbyte), ToolParameterType.Integer },
            { typeof(float), ToolParameterType.Number },
            { typeof(double), ToolParameterType.Number },
            { typeof(decimal), ToolParameterType.Number },
            { typeof(bool), ToolParameterType.Boolean },
            { typeof(DateTime), ToolParameterType.String },
            { typeof(DateTimeOffset), ToolParameterType.String },
            { typeof(DateOnly), ToolParameterType.String },
            { typeof(TimeOnly), ToolParameterType.String },
            { typeof(TimeSpan), ToolParameterType.String },
            { typeof(Guid), ToolParameterType.String },
            { typeof(Uri), ToolParameterType.String },
        };

        private static readonly Dictionary<Type, string> _formatMap = new()
        {
            { typeof(DateTime), "date-time" },
            { typeof(DateTimeOffset), "date-time" },
            { typeof(DateOnly), "date" },
            { typeof(TimeOnly), "time" },
            { typeof(TimeSpan), "duration" },
            { typeof(Guid), "uuid" },
            { typeof(Uri), "uri" },
        };

        /// <summary>
        /// Maps a C# type to a JSON Schema type.
        /// </summary>
        /// <param name="type">The C# type to map.</param>
        /// <returns>The corresponding JSON Schema type.</returns>
        public static ToolParameterType MapType(Type type)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
            }

            // Direct mapping
            if (_typeMap.TryGetValue(type, out var schemaType))
            {
                return schemaType;
            }

            // Enums are strings
            if (type.IsEnum)
            {
                return ToolParameterType.String;
            }

            // Arrays
            if (type.IsArray)
            {
                return ToolParameterType.Array;
            }

            // Generic collections
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>))
                {
                    return ToolParameterType.Array;
                }

                if (genericDef == typeof(Dictionary<,>) ||
                    genericDef == typeof(IDictionary<,>))
                {
                    return ToolParameterType.Object;
                }
            }

            // Default to object for complex types
            return ToolParameterType.Object;
        }

        /// <summary>
        /// Gets the JSON Schema format hint for a C# type.
        /// </summary>
        /// <param name="type">The C# type.</param>
        /// <returns>The format hint, or null if none applies.</returns>
        public static string? GetFormat(Type type)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
            }

            if (_formatMap.TryGetValue(type, out var format))
            {
                return format;
            }

            return null;
        }

        /// <summary>
        /// Converts a ToolParameterType to its JSON Schema string representation.
        /// </summary>
        /// <param name="type">The tool parameter type.</param>
        /// <returns>The JSON Schema type string (e.g., "string", "integer").</returns>
        public static string ToJsonSchemaType(ToolParameterType type)
        {
            return type switch
            {
                ToolParameterType.String => "string",
                ToolParameterType.Integer => "integer",
                ToolParameterType.Number => "number",
                ToolParameterType.Boolean => "boolean",
                ToolParameterType.Object => "object",
                ToolParameterType.Array => "array",
                _ => "string"
            };
        }
    }
}
