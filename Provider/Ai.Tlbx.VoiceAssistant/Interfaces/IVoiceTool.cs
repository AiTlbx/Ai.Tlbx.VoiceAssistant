using System;
using System.Threading.Tasks;

namespace Ai.Tlbx.VoiceAssistant.Interfaces
{
    /// <summary>
    /// Generic interface for voice assistant tools with strongly-typed arguments.
    /// Users implement this interface with their Args record/class.
    /// Schema is automatically inferred from TArgs via reflection.
    /// </summary>
    /// <typeparam name="TArgs">The type representing the tool's parameters (typically a record).</typeparam>
    public interface IVoiceTool<TArgs> where TArgs : notnull
    {
        /// <summary>
        /// Executes the tool with strongly-typed arguments.
        /// </summary>
        /// <param name="args">The deserialized arguments.</param>
        /// <returns>A task that resolves to the tool execution result as JSON string.</returns>
        Task<string> ExecuteAsync(TArgs args);
    }

    /// <summary>
    /// Non-generic base interface for tool registration and provider integration.
    /// Tools are registered using this interface; the generic version provides type safety.
    /// </summary>
    public interface IVoiceTool
    {
        /// <summary>
        /// Gets the name of the tool as it should be presented to the AI provider.
        /// By default, derived from class name converted to snake_case.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a description of what the tool does for the AI provider.
        /// Derived from [Description] attribute on the class.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the Type of the TArgs used by this tool.
        /// Used by the schema inferrer to build the parameter schema.
        /// </summary>
        Type ArgsType { get; }

        /// <summary>
        /// Executes the tool with JSON arguments.
        /// Deserializes the JSON to TArgs and calls the typed ExecuteAsync.
        /// </summary>
        /// <param name="argumentsJson">JSON string containing tool arguments.</param>
        /// <returns>A task that resolves to the tool execution result as JSON string.</returns>
        Task<string> ExecuteAsync(string argumentsJson);
    }
}
