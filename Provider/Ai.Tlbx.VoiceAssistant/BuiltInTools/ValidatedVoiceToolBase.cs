using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Base class for voice tools with built-in parameter validation and type-safe execution.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments expected by this tool.</typeparam>
    public abstract class ValidatedVoiceToolBase<TArgs> : IVoiceToolWithSchema where TArgs : class
    {
        /// <summary>
        /// Gets the name of the tool.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description of what this tool does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the parameter schema for this tool.
        /// </summary>
        /// <returns>The parameter schema defining the expected input structure.</returns>
        public abstract ToolParameterSchema GetParameterSchema();

        /// <summary>
        /// Executes the tool with the provided JSON arguments.
        /// </summary>
        /// <param name="argumentsJson">The arguments in JSON format.</param>
        /// <returns>The result as a JSON string.</returns>
        public async Task<string> ExecuteAsync(string argumentsJson)
        {
            try
            {
                // Parse arguments
                TArgs arguments;
                if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
                {
                    // Handle empty arguments case
                    arguments = Activator.CreateInstance<TArgs>();
                }
                else
                {
                    arguments = JsonSerializer.Deserialize<TArgs>(argumentsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                if (arguments == null)
                {
                    return JsonSerializer.Serialize(new { error = "Failed to parse arguments" });
                }

                // Execute with type-safe arguments
                return await ExecuteValidatedAsync(arguments);
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(new { error = $"Invalid arguments format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = $"Tool execution failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Executes the tool with validated, type-safe arguments.
        /// </summary>
        /// <param name="arguments">The parsed and validated arguments.</param>
        /// <returns>The result as a JSON string.</returns>
        protected abstract Task<string> ExecuteValidatedAsync(TArgs arguments);

        /// <summary>
        /// Helper method to create a successful result with data.
        /// </summary>
        /// <param name="data">The data to return.</param>
        /// <returns>JSON-serialized result.</returns>
        protected string CreateSuccessResult(object data)
        {
            return JsonSerializer.Serialize(new { success = true, data });
        }

        /// <summary>
        /// Helper method to create an error result.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>JSON-serialized error result.</returns>
        protected string CreateErrorResult(string error)
        {
            return JsonSerializer.Serialize(new { success = false, error });
        }
    }
}