using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Base class for voice tools with automatic schema inference and type-safe execution.
    /// Users extend this class and implement ExecuteAsync with their Args type.
    /// </summary>
    /// <typeparam name="TArgs">The type representing the tool's parameters (typically a record).</typeparam>
    public abstract class VoiceToolBase<TArgs> : IVoiceTool<TArgs>, IVoiceTool where TArgs : notnull
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: true) }
        };

        private string? _name;
        private string? _description;

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
        /// Deserializes JSON to TArgs and calls the typed ExecuteAsync.
        /// </summary>
        /// <param name="argumentsJson">JSON string containing tool arguments.</param>
        /// <returns>The result as a JSON string.</returns>
        async Task<string> IVoiceTool.ExecuteAsync(string argumentsJson)
        {
            try
            {
                TArgs args;
                if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
                {
                    args = CreateDefaultArgs();
                }
                else
                {
                    args = JsonSerializer.Deserialize<TArgs>(argumentsJson, _jsonOptions)!;
                }

                if (args == null)
                {
                    return CreateErrorResult("Failed to parse arguments");
                }

                return await ExecuteAsync(args);
            }
            catch (JsonException ex)
            {
                return CreateErrorResult($"Invalid arguments format: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResult($"Tool execution failed: {ex.Message}");
            }
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
        /// Creates a successful result with data.
        /// </summary>
        protected string CreateSuccessResult(object data)
        {
            return JsonSerializer.Serialize(new { success = true, data }, _jsonOptions);
        }

        /// <summary>
        /// Creates an error result.
        /// </summary>
        protected string CreateErrorResult(string error)
        {
            return JsonSerializer.Serialize(new { success = false, error }, _jsonOptions);
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
