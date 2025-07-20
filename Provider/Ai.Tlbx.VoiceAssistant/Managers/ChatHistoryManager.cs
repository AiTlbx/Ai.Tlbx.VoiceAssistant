using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.Managers
{
    /// <summary>
    /// Manages the chat history for voice assistant conversations.
    /// Provides thread-safe access to message history across providers.
    /// </summary>
    public sealed class ChatHistoryManager
    {
        private readonly List<ChatMessage> _messages = new();
        private readonly object _lock = new();

        /// <summary>
        /// Gets the current chat messages as a read-only list.
        /// </summary>
        /// <returns>A read-only list of chat messages.</returns>
        public IReadOnlyList<ChatMessage> GetMessages()
        {
            lock (_lock)
            {
                return _messages.AsReadOnly();
            }
        }

        /// <summary>
        /// Adds a new message to the chat history.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public void AddMessage(ChatMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            lock (_lock)
            {
                _messages.Add(message);
            }
        }

        /// <summary>
        /// Clears all messages from the chat history.
        /// </summary>
        public void ClearHistory()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }

        /// <summary>
        /// Gets the number of messages in the chat history.
        /// </summary>
        public int MessageCount
        {
            get
            {
                lock (_lock)
                {
                    return _messages.Count;
                }
            }
        }
    }
}