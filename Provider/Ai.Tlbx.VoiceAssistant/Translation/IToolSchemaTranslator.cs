using System.Collections.Generic;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.Translation
{
    /// <summary>
    /// Translates tool definitions and responses to provider-specific formats.
    /// Each provider implements this interface to handle their unique JSON structures.
    /// </summary>
    public interface IToolSchemaTranslator
    {
        /// <summary>
        /// Translates a tool definition to the provider-specific JSON format.
        /// </summary>
        /// <param name="tool">The tool to translate.</param>
        /// <param name="schema">The inferred schema for the tool's parameters.</param>
        /// <returns>The provider-specific tool definition object (to be serialized as JSON).</returns>
        object TranslateToolDefinition(IVoiceTool tool, ToolSchema schema);

        /// <summary>
        /// Translates multiple tools to the provider-specific format.
        /// Some providers (like Google) wrap tools in additional structure.
        /// </summary>
        /// <param name="tools">The tools and their schemas.</param>
        /// <returns>The provider-specific tools array or wrapper object.</returns>
        object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools);

        /// <summary>
        /// Formats a tool execution result for sending back to the provider.
        /// </summary>
        /// <param name="result">The JSON result from tool execution.</param>
        /// <param name="callId">The call ID from the provider's tool call.</param>
        /// <param name="toolName">The name of the tool that was executed.</param>
        /// <returns>The provider-specific response object (to be serialized as JSON).</returns>
        object FormatToolResponse(string result, string callId, string toolName);
    }
}
