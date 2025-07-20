namespace Ai.Tlbx.VoiceAssistant.Models
{
    /// <summary>
    /// Represents a provider-agnostic chat message in voice assistant conversations.
    /// Supports standard message types and tool-related messages.
    /// </summary>
    public class ChatMessage
    {
        // Constants for Roles
        public const string UserRole = "user";
        public const string AssistantRole = "assistant";
        public const string ToolRole = "tool";

        // Core Properties
        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Content { get; private set; }
        
        /// <summary>
        /// The role of the message sender (user, assistant, tool).
        /// </summary>
        public string Role { get; private set; }
        
        /// <summary>
        /// Timestamp when the message was created.
        /// </summary>
        public DateTime Timestamp { get; }

        // Tool-related Properties (nullable)
        /// <summary>
        /// Unique identifier for tool calls, used to match calls with their results.
        /// </summary>
        public string? ToolCallId { get; private set; }
        
        /// <summary>
        /// Name of the tool being called or that produced the result.
        /// </summary>
        public string? ToolName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatMessage"/> class.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="role">The message role.</param>
        /// <param name="toolCallId">Optional tool call identifier.</param>
        /// <param name="toolName">Optional tool name.</param>
        public ChatMessage(string content, string role, string? toolCallId = null, string? toolName = null)
        {
            Content = content;
            Role = role;
            ToolCallId = toolCallId;
            ToolName = toolName;
            Timestamp = DateTime.UtcNow;
        }

        // Factory Methods
        /// <summary>
        /// Creates a user message.
        /// </summary>
        public static ChatMessage CreateUserMessage(string content)
            => new(content, UserRole);

        /// <summary>
        /// Creates an assistant message.
        /// </summary>
        public static ChatMessage CreateAssistantMessage(string content)
            => new(content, AssistantRole);

        /// <summary>
        /// Creates a tool result message.
        /// </summary>
        public static ChatMessage CreateToolMessage(string toolName, string result, string? toolCallId = null)
            => new(result, ToolRole, toolCallId, toolName);
    }
}