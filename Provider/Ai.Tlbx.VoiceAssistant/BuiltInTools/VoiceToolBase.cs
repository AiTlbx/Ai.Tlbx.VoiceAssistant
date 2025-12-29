using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Base class for voice tools with automatic schema inference and type-safe execution.
    /// Users extend this class and implement ExecuteAsync with their Args type.
    /// For AOT compatibility, call SetJsonTypeInfo with a source-generated JsonTypeInfo.
    /// </summary>
    /// <typeparam name="TArgs">The type representing the tool's parameters (typically a record).</typeparam>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection fallback is intentional for non-AOT scenarios. AOT users should call SetJsonTypeInfo.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection fallback is intentional for non-AOT scenarios. AOT users should call SetJsonTypeInfo.")]
    public abstract class VoiceToolBase<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>
        : IVoiceTool<TArgs>, IVoiceTool where TArgs : notnull
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: true) }
        };

        private string? _name;
        private string? _description;
        private JsonTypeInfo<TArgs>? _jsonTypeInfo;

        /// <summary>
        /// Sets the JsonTypeInfo for AOT-compatible deserialization.
        /// Call this method with a source-generated context for AOT scenarios.
        /// </summary>
        public void SetJsonTypeInfo(JsonTypeInfo<TArgs> typeInfo) => _jsonTypeInfo = typeInfo;

        /// <summary>
        /// Gets the name of the tool. Defaults to class name converted to snake_case.
        /// Override to provide a custom name.
        /// </summary>
        public virtual string Name => _name ??= ToSnakeCase(GetType().Name);

        /// <summary>
        /// Gets the description of the tool. Derived from [Description] attribute on the class.
        /// Override to provide a custom description.
        /// </summary>
        public virtual string Description => _description ??= GetDescriptionFromAttribute();

        /// <summary>
        /// Gets the Type of TArgs for schema inference.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type ArgsType => typeof(TArgs);

        /// <summary>
        /// Executes the tool with strongly-typed arguments.
        /// Implement this method with your tool logic.
        /// </summary>
        /// <param name="args">The deserialized arguments.</param>
        /// <returns>The result as a JSON string.</returns>
        public abstract Task<string> ExecuteAsync(TArgs args);

        /// <summary>
        /// Executes the tool with JSON arguments.
        /// Validates required parameters first, then deserializes and executes.
        /// </summary>
        /// <param name="argumentsJson">JSON string containing tool arguments.</param>
        /// <returns>The result as a JSON string.</returns>
        async Task<string> IVoiceTool.ExecuteAsync(string argumentsJson)
        {
            try
            {
                // Parse JSON to check provided parameters
                var providedParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(argumentsJson) && argumentsJson != "{}")
                {
                    using var doc = JsonDocument.Parse(argumentsJson);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        // Only count as provided if not null
                        if (prop.Value.ValueKind != JsonValueKind.Null)
                        {
                            providedParams.Add(prop.Name);
                        }
                    }
                }

                // Check for missing required parameters
                var validationError = ValidateRequiredParameters(providedParams, argumentsJson);
                if (validationError != null)
                {
                    return validationError;
                }

                // Deserialize and execute
                TArgs args;
                if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
                {
                    args = CreateDefaultArgs();
                }
                else
                {
                    args = _jsonTypeInfo != null
                        ? JsonSerializer.Deserialize(argumentsJson, _jsonTypeInfo)!
                        : JsonSerializer.Deserialize<TArgs>(argumentsJson, _jsonOptions)!;
                }

                if (args == null)
                {
                    return CreateErrorResult("Failed to parse arguments");
                }

                return await ExecuteAsync(args);
            }
            catch (JsonException ex)
            {
                return CreateSelfHealingError(ex, argumentsJson);
            }
            catch (Exception ex)
            {
                return CreateErrorResult($"Tool execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that all required parameters are provided.
        /// Returns null if valid, or an error message if parameters are missing.
        /// </summary>
        private string? ValidateRequiredParameters(HashSet<string> providedParams, string argumentsJson)
        {
            var requiredParams = GetRequiredParameters();
            var missingParams = new List<string>();
            var providedList = new List<string>();

            foreach (var (paramName, paramType, description) in requiredParams)
            {
                var snakeName = ToSnakeCase(paramName);
                if (providedParams.Contains(snakeName) || providedParams.Contains(paramName))
                {
                    providedList.Add($"{snakeName} ({GetFriendlyTypeName(paramType)})");
                }
                else
                {
                    missingParams.Add($"{snakeName} ({GetFriendlyTypeName(paramType)}): {description}");
                }
            }

            if (missingParams.Count > 0)
            {
                var message = $"Cannot execute tool '{Name}' - missing required parameters.\n\n" +
                              $"MISSING (please ask the user for these):\n- {string.Join("\n- ", missingParams)}\n\n" +
                              (providedList.Count > 0
                                  ? $"Already provided:\n- {string.Join("\n- ", providedList)}"
                                  : "No parameters provided yet.");

                var error = new ToolValidationError
                {
                    Message = message,
                    MissingParameters = missingParams.Select(p => p.Split(' ')[0]).ToList(),
                    ProvidedParameters = providedList.Select(p => p.Split(' ')[0]).ToList()
                };

                return JsonSerializer.Serialize(error, ToolResultsJsonContext.Default.ToolValidationError);
            }

            return null;
        }

        /// <summary>
        /// Gets all required parameters with their types and descriptions.
        /// </summary>
        private List<(string Name, Type Type, string Description)> GetRequiredParameters()
        {
            var result = new List<(string, Type, string)>();

            // Get constructor parameters (for records)
            var constructor = typeof(TArgs).GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor != null)
            {
                foreach (var param in constructor.GetParameters())
                {
                    // Required if: no default value AND not nullable
                    if (!param.HasDefaultValue && !IsNullableParameter(param))
                    {
                        var desc = param.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description";
                        result.Add((param.Name ?? "unknown", param.ParameterType, desc));
                    }
                }
            }

            return result;
        }

        private static bool IsNullableParameter(ParameterInfo param)
        {
            // Check for Nullable<T>
            if (Nullable.GetUnderlyingType(param.ParameterType) != null)
                return true;

            // Check for nullable reference type
            var nullabilityContext = new NullabilityInfoContext();
            var nullabilityInfo = nullabilityContext.Create(param);
            return nullabilityInfo.WriteState == NullabilityState.Nullable;
        }

        /// <summary>
        /// Creates a self-healing error message for JSON deserialization failures.
        /// </summary>
        private string CreateSelfHealingError(JsonException ex, string argumentsJson)
        {
            var path = ex.Path ?? "unknown";
            var paramName = path.TrimStart('$', '.');

            // Try to find the expected type
            var expectedType = "correct type";
            var constructor = typeof(TArgs).GetConstructors().FirstOrDefault();
            if (constructor != null)
            {
                var param = constructor.GetParameters()
                    .FirstOrDefault(p => ToSnakeCase(p.Name ?? "") == paramName || p.Name == paramName);
                if (param != null)
                {
                    expectedType = GetFriendlyTypeName(param.ParameterType);
                }
            }

            var message = $"Parameter '{paramName}' has wrong type. Expected: {expectedType}. " +
                          $"If the user hasn't provided a value, send null or ask the user for a valid {expectedType}.";

            var error = new ToolTypeError
            {
                Message = message,
                Parameter = paramName,
                ExpectedType = expectedType
            };

            return JsonSerializer.Serialize(error, ToolResultsJsonContext.Default.ToolTypeError);
        }

        private static string GetFriendlyTypeName(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                return $"{GetFriendlyTypeName(underlying)} or null";
            }

            if (type.IsEnum)
            {
                var values = string.Join(", ", Enum.GetNames(type).Take(4));
                if (Enum.GetNames(type).Length > 4) values += ", ...";
                return $"one of: {values}";
            }

            return type.Name switch
            {
                "String" => "text",
                "Int32" => "integer number",
                "Int64" => "integer number",
                "Double" => "decimal number",
                "Single" => "decimal number",
                "Decimal" => "decimal number",
                "Boolean" => "true or false",
                "DateTime" => "date/time",
                _ => type.Name.ToLowerInvariant()
            };
        }

        /// <summary>
        /// Creates a default instance of TArgs when no arguments are provided.
        /// Override if TArgs requires special initialization.
        /// </summary>
        protected virtual TArgs CreateDefaultArgs()
        {
            return Activator.CreateInstance<TArgs>();
        }


        /// <summary>
        /// Creates an error result.
        /// </summary>
        protected string CreateErrorResult(string error)
        {
            return JsonSerializer.Serialize(new ToolErrorResult(error), ToolResultsJsonContext.Default.ToolErrorResult);
        }

        private string GetDescriptionFromAttribute()
        {
            var attr = GetType()
                .GetCustomAttributes(typeof(DescriptionAttribute), true)
                .FirstOrDefault() as DescriptionAttribute;

            return attr?.Description ?? $"Tool: {Name}";
        }

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Remove "Tool" suffix if present
            if (name.EndsWith("Tool", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];

            // Insert underscore before uppercase letters and convert to lowercase
            var result = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2");
            return result.ToLowerInvariant();
        }
    }
}
