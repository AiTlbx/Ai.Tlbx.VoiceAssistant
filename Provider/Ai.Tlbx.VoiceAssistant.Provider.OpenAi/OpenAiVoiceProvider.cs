using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;

namespace Ai.Tlbx.VoiceAssistant.Provider.OpenAi
{
    /// <summary>
    /// OpenAI voice provider implementation for real-time conversation via WebSocket.
    /// </summary>
    public sealed class OpenAiVoiceProvider : IVoiceProvider
    {
        private const string REALTIME_WEBSOCKET_ENDPOINT = "wss://api.openai.com/v1/realtime";
        private const int CONNECTION_TIMEOUT_MS = 10000;
        private const int AUDIO_BUFFER_SIZE = 32384;

        private readonly string _apiKey;
        private readonly Action<LogLevel, string> _logAction;
        private ClientWebSocket? _webSocket;
        private Task? _receiveTask;
        private CancellationTokenSource? _cts;
        private bool _isDisposed = false;
        private OpenAiVoiceSettings? _settings;
        
        // State tracking
        private bool _hasActiveResponse = false;
        private readonly StringBuilder _currentAiMessage = new();
        private string _currentResponseId = string.Empty;
        private readonly StringBuilder _currentFunctionArgs = new();
        private string _currentFunctionName = string.Empty;
        private string _currentCallId = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the provider is connected and ready.
        /// </summary>
        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        
        /// <summary>
        /// Gets or sets the settings for this provider instance.
        /// </summary>
        public OpenAiVoiceSettings? Settings 
        { 
            get => _settings;
            set => _settings = value;
        }

        /// <summary>
        /// Callback invoked when a message is received from the AI provider.
        /// </summary>
        public Action<ChatMessage>? OnMessageReceived { get; set; }

        /// <summary>
        /// Callback invoked when audio data is received from the AI provider for playback.
        /// </summary>
        public Action<string>? OnAudioReceived { get; set; }

        /// <summary>
        /// Callback invoked when the provider status changes.
        /// </summary>
        public Action<string>? OnStatusChanged { get; set; }

        /// <summary>
        /// Callback invoked when an error occurs in the provider.
        /// </summary>
        public Action<string>? OnError { get; set; }
        
        /// <summary>
        /// Callback invoked when interruption is detected and audio needs to be cleared.
        /// </summary>
        public Action? OnInterruptDetected { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAiVoiceProvider"/> class.
        /// </summary>
        /// <param name="apiKey">The OpenAI API key. If null, will try to get from environment variable OPENAI_API_KEY.</param>
        /// <param name="logAction">Optional logging action.</param>
        public OpenAiVoiceProvider(string? apiKey = null, Action<LogLevel, string>? logAction = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                ?? throw new InvalidOperationException("OpenAI API key must be provided or set in OPENAI_API_KEY environment variable");
            _logAction = logAction ?? ((level, message) => { /* no-op */ });
        }

        /// <summary>
        /// Connects to the OpenAI real-time API using the specified settings.
        /// </summary>
        /// <param name="settings">OpenAI-specific voice settings.</param>
        /// <returns>A task representing the connection operation.</returns>
        public async Task ConnectAsync(IVoiceSettings settings)
        {
            if (settings is not OpenAiVoiceSettings openAiSettings)
            {
                throw new ArgumentException("Settings must be of type OpenAiVoiceSettings for OpenAI provider", nameof(settings));
            }

            // If we have pre-configured settings with tools, merge them
            if (_settings != null && _settings.Tools.Any())
            {
                foreach (var tool in _settings.Tools.Where(t => !openAiSettings.Tools.Any(st => st.Name == t.Name)))
                {
                    openAiSettings.Tools.Add(tool);
                }
            }

            _settings = openAiSettings;
            _logAction(LogLevel.Info, $"Settings configured - Voice: {_settings.Voice}, Speed: {_settings.TalkingSpeed}, Model: {_settings.Model}");

            try
            {
                OnStatusChanged?.Invoke("Connecting to OpenAI...");
                _logAction(LogLevel.Info, "Starting connection to OpenAI real-time API");

                _webSocket = new ClientWebSocket();
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

                _cts = new CancellationTokenSource(CONNECTION_TIMEOUT_MS);

                var uri = new Uri($"{REALTIME_WEBSOCKET_ENDPOINT}?model={_settings.Model.ToApiString()}");
                await _webSocket.ConnectAsync(uri, _cts.Token);

                _logAction(LogLevel.Info, "WebSocket connection established");
                OnStatusChanged?.Invoke("Connected to OpenAI");

                // Start the message receiving task
                _cts = new CancellationTokenSource();
                _receiveTask = ReceiveMessagesAsync(_cts.Token);

                // Send session configuration
                await SendSessionConfigurationAsync();
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Failed to connect to OpenAI: {ex.Message}");
                OnError?.Invoke($"Connection failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Disconnects from the OpenAI real-time API.
        /// </summary>
        /// <returns>A task representing the disconnection operation.</returns>
        public async Task DisconnectAsync()
        {
            try
            {
                OnStatusChanged?.Invoke("Disconnecting...");

                _cts?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }

                if (_receiveTask != null)
                {
                    await _receiveTask;
                }

                OnStatusChanged?.Invoke("Disconnected");
                _logAction(LogLevel.Info, "Disconnected from OpenAI");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error during disconnection: {ex.Message}");
                OnError?.Invoke($"Disconnection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the settings for an existing connection and sends the configuration to OpenAI.
        /// </summary>
        /// <param name="settings">The new settings to apply.</param>
        /// <returns>A task representing the update operation.</returns>
        public async Task UpdateSettingsAsync(IVoiceSettings settings)
        {
            if (settings is not OpenAiVoiceSettings openAiSettings)
            {
                throw new ArgumentException("Settings must be of type OpenAiVoiceSettings for OpenAI provider", nameof(settings));
            }

            _settings = openAiSettings;
            _logAction(LogLevel.Info, $"Settings configured - Voice: {_settings.Voice}, Speed: {_settings.TalkingSpeed}, Model: {_settings.Model}");
            
            if (IsConnected)
            {
                _logAction(LogLevel.Info, "Updating session configuration for existing connection");
                await SendSessionConfigurationAsync();
            }
        }

        /// <summary>
        /// Processes audio data received from the microphone and sends it to OpenAI.
        /// </summary>
        /// <param name="base64Audio">Base64-encoded PCM 16-bit audio data.</param>
        /// <returns>A task representing the audio processing operation.</returns>
        public async Task ProcessAudioAsync(string base64Audio)
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                var audioMessage = new
                {
                    type = "input_audio_buffer.append",
                    audio = base64Audio
                };

                await SendMessageAsync(JsonSerializer.Serialize(audioMessage));
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error processing audio: {ex.Message}");
                OnError?.Invoke($"Audio processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends an interrupt signal to OpenAI to stop current response generation.
        /// </summary>
        /// <returns>A task representing the interrupt operation.</returns>
        public async Task SendInterruptAsync()
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                var interruptMessage = new
                {
                    type = "response.cancel"
                };

                await SendMessageAsync(JsonSerializer.Serialize(interruptMessage));
                _logAction(LogLevel.Info, "Interrupt signal sent to OpenAI");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error sending interrupt: {ex.Message}");
                OnError?.Invoke($"Interrupt error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Injects conversation history into the current session.
        /// </summary>
        /// <param name="messages">The conversation history to inject.</param>
        /// <returns>A task representing the injection operation.</returns>
        public async Task InjectConversationHistoryAsync(IEnumerable<ChatMessage> messages)
        {
            if (!IsConnected)
            {
                _logAction(LogLevel.Warn, "Cannot inject conversation history: not connected");
                return;
            }
            
            try
            {
                foreach (var message in messages)
                {
                    // Skip tool messages as they need special handling
                    if (message.Role == ChatMessage.ToolRole)
                        continue;
                    
                    var conversationItem = new
                    {
                        type = "conversation.item.create",
                        item = new
                        {
                            type = "message",
                            role = message.Role == ChatMessage.UserRole ? "user" : "assistant",
                            content = new[]
                            {
                                new
                                {
                                    type = "input_text",
                                    text = message.Content
                                }
                            }
                        }
                    };
                    
                    await SendMessageAsync(JsonSerializer.Serialize(conversationItem));
                    _logAction(LogLevel.Info, $"Injected {message.Role} message into conversation");
                    
                    // Small delay to avoid overwhelming the API
                    await Task.Delay(50);
                }
                
                _logAction(LogLevel.Info, $"Successfully injected {messages.Count()} messages into conversation history");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error injecting conversation history: {ex.Message}");
                throw;
            }
        }

        private async Task SendSessionConfigurationAsync()
        {
            if (_settings == null || !IsConnected)
                return;

            var voiceString = _settings.Voice.ToString().ToLowerInvariant();
            _logAction(LogLevel.Info, $"Configuring session with voice: {_settings.Voice} -> {voiceString}");
            
            var session = new Dictionary<string, object>
            {
                ["model"] = _settings.Model.ToApiString(),
                ["voice"] = voiceString,
                ["instructions"] = _settings.Instructions,
                ["temperature"] = _settings.Temperature,
                ["speed"] = _settings.TalkingSpeed,
                ["turn_detection"] = new
                {
                    type = _settings.TurnDetection.Type,
                    threshold = _settings.TurnDetection.Threshold,
                    prefix_padding_ms = _settings.TurnDetection.PrefixPaddingMs,
                    silence_duration_ms = _settings.TurnDetection.SilenceDurationMs
                },
                ["input_audio_transcription"] = new
                {
                    model = _settings.InputAudioTranscription.Model
                },
                ["output_audio_format"] = _settings.OutputAudioFormat,
                ["tools"] = _settings.Tools.Select(tool => new
                {
                    type = "function",
                    name = tool.Name,
                    description = tool.Description,
                    parameters = new { type = "object", properties = new { } }
                }).ToArray()
            };

            // Only add max_response_output_tokens if it has a value
            if (_settings.MaxTokens.HasValue)
            {
                session["max_response_output_tokens"] = _settings.MaxTokens.Value;
            }

            var sessionConfig = new
            {
                type = "session.update",
                session = session
            };

            var jsonMessage = JsonSerializer.Serialize(sessionConfig, new JsonSerializerOptions { WriteIndented = true });
            _logAction(LogLevel.Info, $"Sending session config to OpenAI:\n{jsonMessage}");
            await SendMessageAsync(jsonMessage);
            _logAction(LogLevel.Info, "Session configuration sent to OpenAI");
        }

        private async Task SendMessageAsync(string message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                return;

            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[AUDIO_BUFFER_SIZE];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var messageBuilder = new MemoryStream();
                    WebSocketReceiveResult result;
                    
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            messageBuilder.Write(buffer, 0, result.Count);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            OnStatusChanged?.Invoke("Connection closed by server");
                            return;
                        }
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(messageBuilder.ToArray());
                        await ProcessReceivedMessage(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error in message receive loop: {ex.Message}");
                OnError?.Invoke($"Message receive error: {ex.Message}");
            }
        }

        private async Task ProcessReceivedMessage(string message)
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                if (!root.TryGetProperty("type", out var typeElement))
                    return;

                var messageType = typeElement.GetString();

                switch (messageType)
                {
                    case "session.created":
                        _logAction(LogLevel.Info, "Session created by OpenAI");
                        break;
                    case "session.updated":
                        _logAction(LogLevel.Info, "Session configuration updated");
                        break;
                    case "response.created":
                        HandleResponseCreated(root);
                        break;
                    case "conversation.item.created":
                        // Expected message that we don't need to process
                        break;
                    case "response.audio.delta":
                        await HandleAudioResponse(root);
                        break;
                    case "response.audio_transcript.done":
                        HandleAudioTranscriptDone(root);
                        break;
                    case "response.done":
                        await HandleResponseDone(root);
                        break;
                    case "response.text.delta":
                        HandleTextDelta(root);
                        break;
                    case "response.text.done":
                        await HandleTextDone();
                        break;
                    case "response.function_call_arguments.delta":
                        HandleFunctionCallDelta(root);
                        break;
                    case "response.function_call_arguments.done":
                        await HandleFunctionCallDone(root);
                        break;
                    case "input_audio_buffer.speech_started":
                        _logAction(LogLevel.Info, "User started speaking - server detected interruption");
                        HandleInterruption();
                        break;
                    case "input_audio_buffer.speech_stopped":
                        _logAction(LogLevel.Info, "User stopped speaking");
                        break;
                    case "conversation.item.input_audio_transcription.completed":
                        HandleInputAudioTranscriptionCompleted(root);
                        break;
                    case "error":
                        HandleError(root);
                        break;
                    // Known message types we don't need to process
                    case "input_audio_buffer.committed":
                    case "response.output_item.added":
                    case "response.content_part.added":
                    case "response.content_part.done":
                    case "response.audio.done":
                    case "response.audio_transcript.delta":
                    case "response.output_item.done":
                    case "rate_limits.updated":
                    case "conversation.item.input_audio_transcription.delta":
                        // These are expected messages that we don't need special handling for
                        break;
                    default:
                        _logAction(LogLevel.Info, $"Unhandled message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error processing received message: {ex.Message}");
            }
        }


        private async Task HandleAudioResponse(JsonElement root)
        {
            // Handle audio response - forward to audio hardware via callback
            if (root.TryGetProperty("delta", out var delta))
            {
                var audioData = delta.GetString();
                if (!string.IsNullOrEmpty(audioData))
                {
                    _logAction(LogLevel.Info, $"Received audio response: {audioData.Length} characters");
                    OnAudioReceived?.Invoke(audioData);
                }
            }

            await Task.CompletedTask;
        }

        private void HandleFunctionCallDelta(JsonElement root)
        {
            if (root.TryGetProperty("delta", out var delta))
            {
                var argsDelta = delta.GetString() ?? "";
                _currentFunctionArgs.Append(argsDelta);
                
                if (root.TryGetProperty("name", out var nameElement))
                {
                    _currentFunctionName = nameElement.GetString() ?? "";
                }
                
                if (root.TryGetProperty("call_id", out var callIdElement))
                {
                    _currentCallId = callIdElement.GetString() ?? "";
                }
            }
        }
        
        private async Task HandleFunctionCallDone(JsonElement root)
        {
            if (root.TryGetProperty("name", out var nameElement) &&
                root.TryGetProperty("call_id", out var callIdElement))
            {
                var functionName = nameElement.GetString() ?? _currentFunctionName;
                var callId = callIdElement.GetString() ?? _currentCallId;
                var argumentsJson = _currentFunctionArgs.ToString();
                
                _logAction(LogLevel.Info, $"Function call complete: {functionName} (ID: {callId})");
                
                // Find and execute the tool
                var tool = _settings?.Tools.FirstOrDefault(t => t.Name == functionName);
                
                if (tool != null)
                {
                    try
                    {
                        _logAction(LogLevel.Info, $"Executing tool: {functionName}");
                        var result = await tool.ExecuteAsync(argumentsJson);
                        
                        // Send the tool result back to OpenAI
                        await SendToolResultAsync(callId, result);
                        
                        // Add a message to chat history
                        var toolMessage = ChatMessage.CreateAssistantMessage($"[Tool: {functionName}] {result}");
                        OnMessageReceived?.Invoke(toolMessage);
                    }
                    catch (Exception ex)
                    {
                        _logAction(LogLevel.Error, $"Error executing tool {functionName}: {ex.Message}");
                        await SendToolResultAsync(callId, $"Error: {ex.Message}");
                    }
                }
                else
                {
                    _logAction(LogLevel.Warn, $"Tool not found: {functionName}");
                    await SendToolResultAsync(callId, $"Tool not found: {functionName}");
                }
                
                // Clear the buffers
                _currentFunctionArgs.Clear();
                _currentFunctionName = "";
                _currentCallId = "";
            }
        }
        
        private async Task SendToolResultAsync(string callId, string result)
        {
            var toolResponse = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = result
                }
            };
            
            await SendMessageAsync(JsonSerializer.Serialize(toolResponse));
            _logAction(LogLevel.Info, $"Tool result sent for call {callId}");
            
            // Request a new response from the AI
            var responseCreate = new
            {
                type = "response.create"
            };
            
            await SendMessageAsync(JsonSerializer.Serialize(responseCreate));
            _logAction(LogLevel.Info, "Requested AI response after tool execution");
        }

        private void HandleError(JsonElement root)
        {
            if (root.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var messageElement))
            {
                var errorMessage = messageElement.GetString() ?? "Unknown error";
                
                // Don't report cancellation errors when there's no active response
                if (errorMessage.Contains("Cannot cancel response", StringComparison.OrdinalIgnoreCase) && !_hasActiveResponse)
                {
                    _logAction(LogLevel.Info, $"Ignoring cancellation error - no active response: {errorMessage}");
                    return;
                }
                
                _logAction(LogLevel.Error, $"OpenAI API error: {errorMessage}");
                OnError?.Invoke(errorMessage);
            }
        }

        private void HandleAudioTranscriptDone(JsonElement root)
        {
            if (root.TryGetProperty("transcript", out var transcript))
            {
                var text = transcript.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    _logAction(LogLevel.Info, $"Audio transcript: {text}");
                    
                    // Send the transcript as an assistant message
                    var message = ChatMessage.CreateAssistantMessage(text);
                    OnMessageReceived?.Invoke(message);
                }
            }
        }
        
        private void HandleInputAudioTranscriptionCompleted(JsonElement root)
        {
            if (root.TryGetProperty("transcript", out var transcript))
            {
                var text = transcript.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    _logAction(LogLevel.Info, $"User transcript: {text}");
                    
                    // Send the transcript as a user message
                    var message = ChatMessage.CreateUserMessage(text);
                    OnMessageReceived?.Invoke(message);
                }
            }
        }

        private void HandleTextDelta(JsonElement root)
        {
            if (root.TryGetProperty("delta", out var deltaElem) && 
                deltaElem.TryGetProperty("text", out var textElem))
            {
                string deltaText = textElem.GetString() ?? string.Empty;
                _currentAiMessage.Append(deltaText);
            }
        }
        
        private void HandleResponseCreated(JsonElement root)
        {
            if (root.TryGetProperty("response", out var response) && 
                response.TryGetProperty("id", out var idElement))
            {
                _currentResponseId = idElement.GetString() ?? "";
                _hasActiveResponse = true;
                _logAction(LogLevel.Info, $"New response started: {_currentResponseId}");
            }
        }
        
        private async Task HandleResponseDone(JsonElement root)
        {
            _hasActiveResponse = false;
            _logAction(LogLevel.Info, "Response completed");
            await Task.CompletedTask;
        }
        
        private async Task HandleTextDone()
        {
            if (_currentAiMessage.Length > 0)
            {
                string messageText = _currentAiMessage.ToString();
                _logAction(LogLevel.Info, $"AI Text Complete: {messageText}");
                
                var message = ChatMessage.CreateAssistantMessage(messageText);
                OnMessageReceived?.Invoke(message);
                
                _currentAiMessage.Clear();
            }
            await Task.CompletedTask;
        }
        
        private async void HandleInterruption()
        {
            _logAction(LogLevel.Info, "Speech detected - user interruption");
            
            // Always clear audio queue when speech is detected (like in the old code)
            OnInterruptDetected?.Invoke();
            
            // Only cancel if there's an active response
            if (_hasActiveResponse)
            {
                _logAction(LogLevel.Info, "Interrupting active AI response");
                await SendInterruptAsync();
                _hasActiveResponse = false;
            }
            else
            {
                _logAction(LogLevel.Info, "No active response to interrupt");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            try
            {
                await DisconnectAsync();
                
                _webSocket?.Dispose();
                _cts?.Dispose();
                
                _isDisposed = true;
                _logAction(LogLevel.Info, "OpenAI voice provider disposed");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error during disposal: {ex.Message}");
            }
        }
    }
}