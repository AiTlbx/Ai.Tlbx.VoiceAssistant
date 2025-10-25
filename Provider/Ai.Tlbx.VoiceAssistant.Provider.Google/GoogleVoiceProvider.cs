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
using Ai.Tlbx.VoiceAssistant.Provider.Google.Models;
using Ai.Tlbx.VoiceAssistant.Provider.Google.Protocol;

namespace Ai.Tlbx.VoiceAssistant.Provider.Google
{
    /// <summary>
    /// Google Gemini Live API provider implementation for real-time conversation via WebSocket.
    /// </summary>
    public sealed class GoogleVoiceProvider : IVoiceProvider
    {
        private const string LIVE_API_ENDPOINT = "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent";
        private const int CONNECTION_TIMEOUT_MS = 10000;
        private const int AUDIO_BUFFER_SIZE = 32384;
        private const string AUDIO_MIME_TYPE = "audio/pcm;rate=16000";
        private const int AUDIO_TX_LOG_INTERVAL = 100;
        private const int AUDIO_RX_LOG_INTERVAL = 50;
        private const int MESSAGE_LOG_TRUNCATE_LENGTH = 300;

        private readonly string _apiKey;
        private readonly Action<LogLevel, string> _logAction;
        private ClientWebSocket? _webSocket;
        private Task? _receiveTask;
        private CancellationTokenSource? _cts;
        private bool _isDisposed = false;
        private GoogleVoiceSettings? _settings;

        private bool _setupComplete = false;
        private readonly StringBuilder _currentTranscript = new();
        private readonly StringBuilder _currentUserTranscript = new();
        private readonly Dictionary<string, string> _pendingToolCalls = new();
        private int _audioTxCount = 0;
        private int _audioRxCount = 0;

        /// <summary>
        /// Gets a value indicating whether the provider is connected and ready.
        /// </summary>
        public bool IsConnected => _webSocket?.State == WebSocketState.Open && _setupComplete;

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
        /// Initializes a new instance of the <see cref="GoogleVoiceProvider"/> class.
        /// </summary>
        /// <param name="apiKey">The Google API key. If null, will try to get from environment variable GOOGLE_API_KEY.</param>
        /// <param name="logAction">Optional logging action.</param>
        public GoogleVoiceProvider(string? apiKey = null, Action<LogLevel, string>? logAction = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                ?? throw new InvalidOperationException("Google API key must be provided or set in GOOGLE_API_KEY environment variable");
            _logAction = logAction ?? ((level, message) => { /* no-op */ });
        }

        /// <summary>
        /// Connects to the Google Gemini Live API using the specified settings.
        /// </summary>
        /// <param name="settings">Google-specific voice settings.</param>
        /// <returns>A task representing the connection operation.</returns>
        public async Task ConnectAsync(IVoiceSettings settings)
        {
            if (settings is not GoogleVoiceSettings googleSettings)
            {
                throw new ArgumentException("Settings must be of type GoogleVoiceSettings for Google provider", nameof(settings));
            }

            _settings = googleSettings;
            _logAction(LogLevel.Info, $"Settings configured - Voice: {_settings.Voice}, Model: {_settings.Model}");

            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    throw new InvalidOperationException("Google API key is not set. Please set the GOOGLE_API_KEY environment variable.");
                }

                OnStatusChanged?.Invoke("Connecting to Google Gemini...");

                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource(CONNECTION_TIMEOUT_MS);

                var uri = new Uri($"{LIVE_API_ENDPOINT}?key={_apiKey}");
                _logAction(LogLevel.Info, $"Connecting to: {LIVE_API_ENDPOINT}");
                _logAction(LogLevel.Info, $"API Key present: {!string.IsNullOrEmpty(_apiKey)}, Length: {_apiKey?.Length ?? 0}");

                await _webSocket.ConnectAsync(uri, _cts.Token);

                OnStatusChanged?.Invoke("Connected to Google Gemini");
                _logAction(LogLevel.Info, $"WebSocket connected, State: {_webSocket.State}");

                _cts = new CancellationTokenSource();
                _logAction(LogLevel.Info, "Starting message receive loop");
                _receiveTask = ReceiveMessagesAsync(_cts.Token);

                await SendSetupMessageAsync();
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Failed to connect to Google Gemini: {ex.Message}");
                OnError?.Invoke($"Connection failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Disconnects from the Google Gemini Live API.
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

                _setupComplete = false;
                OnStatusChanged?.Invoke("Disconnected");
                _logAction(LogLevel.Info, "Disconnected from Google Gemini");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error during disconnection: {ex.Message}");
                OnError?.Invoke($"Disconnection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the settings for an existing connection and sends the configuration to Google.
        /// </summary>
        /// <param name="settings">The new settings to apply.</param>
        /// <returns>A task representing the update operation.</returns>
        public async Task UpdateSettingsAsync(IVoiceSettings settings)
        {
            if (settings is not GoogleVoiceSettings googleSettings)
            {
                throw new ArgumentException("Settings must be of type GoogleVoiceSettings for Google provider", nameof(settings));
            }

            _settings = googleSettings;
            _logAction(LogLevel.Info, $"Settings updated - Voice: {_settings.Voice}, Model: {_settings.Model}");

            if (IsConnected)
            {
                _logAction(LogLevel.Warn, "Updating settings requires reconnection for Google provider");
                await DisconnectAsync();
                await ConnectAsync(settings);
            }
        }

        /// <summary>
        /// Processes audio data received from the microphone and sends it to Google.
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
                if (string.IsNullOrEmpty(base64Audio))
                {
                    _logAction(LogLevel.Warn, "[AUDIO-TX] Received null or empty audio data, skipping");
                    return;
                }

                var message = new RealtimeInputMessage
                {
                    RealtimeInput = new RealtimeInput
                    {
                        MediaChunks = new List<MediaChunk>
                        {
                            new MediaChunk
                            {
                                MimeType = AUDIO_MIME_TYPE,
                                Data = base64Audio
                            }
                        }
                    }
                };

                await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error processing audio: {ex.Message}");
                OnError?.Invoke($"Audio processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends an interrupt signal to Google to stop current response generation.
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
                _logAction(LogLevel.Info, "Interrupt handled automatically by Google VAD");
                await Task.CompletedTask;
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
                var turns = new List<Turn>();

                foreach (var message in messages)
                {
                    if (message.Role == ChatMessage.ToolRole)
                        continue;

                    var turn = new Turn
                    {
                        Role = message.Role == ChatMessage.UserRole ? "user" : "model",
                        Parts = new List<Part>
                        {
                            new Part { Text = message.Content }
                        }
                    };

                    turns.Add(turn);
                }

                if (turns.Count > 0)
                {
                    var message = new ClientContentMessage
                    {
                        ClientContent = new ClientContent
                        {
                            Turns = turns,
                            TurnComplete = true
                        }
                    };

                    await SendMessageAsync(message);
                    _logAction(LogLevel.Info, $"Successfully injected {turns.Count} messages into conversation history");
                }
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error injecting conversation history: {ex.Message}");
                throw;
            }
        }

        private async Task SendSetupMessageAsync()
        {
            if (_settings == null || !(_webSocket?.State == WebSocketState.Open))
                return;

            var voiceName = _settings.Voice.ToString();

            var setup = new SetupMessage
            {
                Setup = new Setup
                {
                    Model = _settings.Model.ToApiString(),
                    GenerationConfig = new GenerationConfig
                    {
                        ResponseModalities = new List<string> { _settings.ResponseModality },
                        SpeechConfig = new SpeechConfig
                        {
                            VoiceConfig = new VoiceConfig
                            {
                                PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                                {
                                    VoiceName = voiceName
                                }
                            },
                            LanguageCode = _settings.LanguageCode
                        },
                        Temperature = _settings.Temperature,
                        TopP = _settings.TopP,
                        TopK = _settings.TopK,
                        MaxOutputTokens = _settings.MaxTokens
                    },
                    SystemInstruction = new SystemInstruction
                    {
                        Parts = new List<Part>
                        {
                            new Part { Text = _settings.Instructions }
                        }
                    },
                    Tools = _settings.Tools.Count > 0 ? ConvertToolsToGoogleFormat() : null,
                    InputAudioTranscription = _settings.TranscriptionConfig.EnableInputTranscription ? new { } : null,
                    OutputAudioTranscription = _settings.TranscriptionConfig.EnableOutputTranscription ? new { } : null,
                    RealtimeInputConfig = new RealtimeInputConfig
                    {
                        AutomaticActivityDetection = _settings.VoiceActivityDetection.AutomaticDetection ? new Protocol.AutomaticActivityDetection
                        {
                            StartOfSpeechSensitivity = _settings.VoiceActivityDetection.StartOfSpeechSensitivity.ToApiString(true),
                            EndOfSpeechSensitivity = _settings.VoiceActivityDetection.EndOfSpeechSensitivity.ToApiString(false),
                            PrefixPaddingMs = _settings.VoiceActivityDetection.PrefixPaddingMs,
                            SilenceDurationMs = _settings.VoiceActivityDetection.SilenceDurationMs,
                            Disabled = false
                        } : new Protocol.AutomaticActivityDetection { Disabled = true },
                        ActivityHandling = _settings.VoiceActivityDetection.ActivityHandling.ToApiString()
                    }
                }
            };

            _logAction(LogLevel.Info, $"Configuring session - Transcription (In/Out): {_settings.TranscriptionConfig.EnableInputTranscription}/{_settings.TranscriptionConfig.EnableOutputTranscription}, VAD: {_settings.VoiceActivityDetection.StartOfSpeechSensitivity}/{_settings.VoiceActivityDetection.EndOfSpeechSensitivity}, Silence: {_settings.VoiceActivityDetection.SilenceDurationMs}ms");

            await SendMessageAsync(setup);
            _logAction(LogLevel.Info, "Setup message sent, awaiting confirmation");
        }

        private List<Tool> ConvertToolsToGoogleFormat()
        {
            if (_settings?.Tools == null || _settings.Tools.Count == 0)
                return new List<Tool>();

            var functionDeclarations = new List<FunctionDeclaration>();

            foreach (var tool in _settings.Tools)
            {
                var declaration = new FunctionDeclaration
                {
                    Name = tool.Name,
                    Description = tool.Description
                };

                if (tool is IVoiceToolWithSchema schemaTool)
                {
                    var schema = schemaTool.GetParameterSchema();
                    declaration.Parameters = ConvertSchemaToGoogleFormat(schema);
                }
                else
                {
                    declaration.Parameters = new { type = "object", properties = new { } };
                }

                functionDeclarations.Add(declaration);
            }

            return new List<Tool>
            {
                new Tool { FunctionDeclarations = functionDeclarations }
            };
        }

        private object ConvertSchemaToGoogleFormat(ToolParameterSchema schema)
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = schema.Type
            };

            if (schema.Properties != null && schema.Properties.Count > 0)
            {
                result["properties"] = schema.Properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ConvertPropertyToGoogleFormat(kvp.Value)
                );
            }

            if (schema.Required != null && schema.Required.Count > 0)
            {
                result["required"] = schema.Required;
            }

            return result;
        }

        private object ConvertPropertyToGoogleFormat(ParameterProperty property)
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = property.Type
            };

            if (!string.IsNullOrEmpty(property.Description))
                result["description"] = property.Description;

            if (property.Enum != null && property.Enum.Count > 0)
                result["enum"] = property.Enum;

            if (property.Default != null)
                result["default"] = property.Default;

            if (property.Properties != null && property.Properties.Count > 0)
            {
                result["properties"] = property.Properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ConvertPropertyToGoogleFormat(kvp.Value)
                );
            }

            if (property.Items != null)
                result["items"] = ConvertPropertyToGoogleFormat(property.Items);

            if (property.Minimum.HasValue)
                result["minimum"] = property.Minimum.Value;

            if (property.Maximum.HasValue)
                result["maximum"] = property.Maximum.Value;

            if (property.MinLength.HasValue)
                result["minLength"] = property.MinLength.Value;

            if (property.MaxLength.HasValue)
                result["maxLength"] = property.MaxLength.Value;

            if (!string.IsNullOrEmpty(property.Pattern))
                result["pattern"] = property.Pattern;

            return result;
        }

        private async Task SendMessageAsync<T>(T message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                return;

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

            var messageType = typeof(T).Name;

            if (messageType == "RealtimeInputMessage")
            {
                _audioTxCount++;
                if (_audioTxCount % AUDIO_TX_LOG_INTERVAL == 1)
                {
                    _logAction(LogLevel.Info, $"[AUDIO-TX] Sent {_audioTxCount} audio chunks to Google");
                }
            }
            else
            {
                var logMessage = json.Length > MESSAGE_LOG_TRUNCATE_LENGTH
                    ? json.Substring(0, MESSAGE_LOG_TRUNCATE_LENGTH) + "..."
                    : json;
                _logAction(LogLevel.Info, $"[MSG-TX] {messageType}: {logMessage}");
            }

            var buffer = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[AUDIO_BUFFER_SIZE];

            try
            {
                _logAction(LogLevel.Info, "[RX-LOOP] Receive loop started");

                while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var messageBuilder = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                        {
                            messageBuilder.Write(buffer, 0, result.Count);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logAction(LogLevel.Info, "[RX-LOOP] Close message received from server");
                            OnStatusChanged?.Invoke("Connection closed by server");
                            return;
                        }
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                    {
                        var message = Encoding.UTF8.GetString(messageBuilder.ToArray());
                        await ProcessReceivedMessage(message);
                    }
                }

                _logAction(LogLevel.Info, $"[RX-LOOP] Exited loop - WebSocket State: {_webSocket?.State}, Cancelled: {cancellationToken.IsCancellationRequested}");
            }
            catch (OperationCanceledException)
            {
                _logAction(LogLevel.Info, "[RX-LOOP] Receive loop cancelled");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"[RX-LOOP] Error in message receive loop: {ex.Message}");
                _logAction(LogLevel.Error, $"[RX-LOOP] Stack trace: {ex.StackTrace}");
                OnError?.Invoke($"Message receive error: {ex.Message}");
            }
        }

        private async Task ProcessReceivedMessage(string message)
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                if (root.TryGetProperty("setupComplete", out _))
                {
                    _setupComplete = true;
                    _logAction(LogLevel.Info, "Setup complete - ready for interaction");
                    OnStatusChanged?.Invoke("Ready");
                    return;
                }

                if (root.TryGetProperty("serverContent", out var serverContent))
                {
                    await HandleServerContent(serverContent);
                    return;
                }

                if (root.TryGetProperty("toolCall", out var toolCall))
                {
                    await HandleToolCall(toolCall);
                    return;
                }

                if (root.TryGetProperty("toolCallCancellation", out var cancellation))
                {
                    HandleToolCallCancellation(cancellation);
                    return;
                }

                _logAction(LogLevel.Info, $"Received unhandled message type");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error processing received message: {ex.Message}");
            }
        }

        private async Task HandleServerContent(JsonElement serverContent)
        {
            // Google sends incremental transcription fragments as user speaks.
            // Accumulate fragments until AI starts responding (modelTurn), then flush complete message.
            if (serverContent.TryGetProperty("inputTranscription", out var inputTranscription))
            {
                if (inputTranscription.TryGetProperty("text", out var inputText))
                {
                    var textValue = inputText.GetString();
                    if (!string.IsNullOrEmpty(textValue))
                    {
                        _currentUserTranscript.Append(textValue);
                    }
                }
            }

            if (serverContent.TryGetProperty("outputTranscription", out var outputTranscription))
            {
                if (outputTranscription.TryGetProperty("text", out var outputText))
                {
                    var textValue = outputText.GetString();
                    if (!string.IsNullOrEmpty(textValue))
                    {
                        _currentTranscript.Append(textValue);
                    }
                }
            }

            if (serverContent.TryGetProperty("interrupted", out var interrupted) && interrupted.GetBoolean())
            {
                _logAction(LogLevel.Info, "Response interrupted by user");
                OnInterruptDetected?.Invoke();
                _currentTranscript.Clear();
                _currentUserTranscript.Clear();
                return;
            }

            if (serverContent.TryGetProperty("modelTurn", out var modelTurn))
            {
                // Flush accumulated user transcript when AI begins responding
                if (_currentUserTranscript.Length > 0)
                {
                    var userMessage = _currentUserTranscript.ToString();
                    _logAction(LogLevel.Info, $"[USER-COMPLETE] {userMessage}");
                    OnMessageReceived?.Invoke(ChatMessage.CreateUserMessage(userMessage));
                    _currentUserTranscript.Clear();
                }

                if (modelTurn.TryGetProperty("parts", out var parts))
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var text))
                        {
                            var textValue = text.GetString();
                            if (!string.IsNullOrEmpty(textValue))
                            {
                                _currentTranscript.Append(textValue);
                            }
                        }

                        if (part.TryGetProperty("inlineData", out var inlineData))
                        {
                            if (inlineData.TryGetProperty("mimeType", out var mimeType) &&
                                mimeType.GetString()?.Contains("audio") == true &&
                                inlineData.TryGetProperty("data", out var data))
                            {
                                var audioData = data.GetString();
                                if (!string.IsNullOrEmpty(audioData))
                                {
                                    _audioRxCount++;
                                    if (_audioRxCount % AUDIO_RX_LOG_INTERVAL == 1)
                                    {
                                        _logAction(LogLevel.Info, $"[AUDIO-RX] Received {_audioRxCount} audio chunks from Google");
                                    }
                                    OnAudioReceived?.Invoke(audioData);
                                }
                            }
                        }
                    }
                }
            }

            if (serverContent.TryGetProperty("turnComplete", out var turnComplete) && turnComplete.GetBoolean())
            {
                if (_currentTranscript.Length > 0)
                {
                    var transcriptText = _currentTranscript.ToString();
                    _logAction(LogLevel.Info, $"[AI] {transcriptText}");
                    OnMessageReceived?.Invoke(ChatMessage.CreateAssistantMessage(transcriptText));
                    _currentTranscript.Clear();
                }
            }

            await Task.CompletedTask;
        }

        private async Task HandleToolCall(JsonElement toolCall)
        {
            if (!toolCall.TryGetProperty("functionCalls", out var functionCalls))
                return;

            var responses = new List<FunctionResponse>();

            foreach (var functionCall in functionCalls.EnumerateArray())
            {
                if (!functionCall.TryGetProperty("id", out var idElement) ||
                    !functionCall.TryGetProperty("name", out var nameElement))
                    continue;

                var id = idElement.GetString() ?? "";
                var name = nameElement.GetString() ?? "";

                _logAction(LogLevel.Info, $"Tool call received: {name} (ID: {id})");

                var tool = _settings?.Tools.FirstOrDefault(t => t.Name == name);

                if (tool != null)
                {
                    try
                    {
                        var argsJson = functionCall.TryGetProperty("args", out var args)
                            ? JsonSerializer.Serialize(args)
                            : "{}";

                        _logAction(LogLevel.Info, $"Executing tool: {name}");
                        var result = await tool.ExecuteAsync(argsJson);

                        responses.Add(new FunctionResponse
                        {
                            Id = id,
                            Name = name,
                            Response = new FunctionResponseData { Result = result }
                        });

                        OnMessageReceived?.Invoke(ChatMessage.CreateAssistantMessage($"Calling tool: {name}"));
                        OnMessageReceived?.Invoke(ChatMessage.CreateToolMessage(name, result, id));
                    }
                    catch (Exception ex)
                    {
                        _logAction(LogLevel.Error, $"Error executing tool {name}: {ex.Message}");
                        responses.Add(new FunctionResponse
                        {
                            Id = id,
                            Name = name,
                            Response = new FunctionResponseData { Result = $"Error: {ex.Message}" }
                        });
                    }
                }
                else
                {
                    _logAction(LogLevel.Warn, $"Tool not found: {name}");
                    responses.Add(new FunctionResponse
                    {
                        Id = id,
                        Name = name,
                        Response = new FunctionResponseData { Result = $"Tool not found: {name}" }
                    });
                }
            }

            if (responses.Count > 0)
            {
                var toolResponse = new ToolResponseMessage
                {
                    ToolResponse = new ToolResponse
                    {
                        FunctionResponses = responses
                    }
                };

                await SendMessageAsync(toolResponse);
                _logAction(LogLevel.Info, $"Sent {responses.Count} tool response(s)");
            }
        }

        private void HandleToolCallCancellation(JsonElement cancellation)
        {
            if (cancellation.TryGetProperty("ids", out var ids))
            {
                var cancelledIds = new List<string>();
                foreach (var id in ids.EnumerateArray())
                {
                    var idValue = id.GetString();
                    if (!string.IsNullOrEmpty(idValue))
                    {
                        cancelledIds.Add(idValue);
                        _pendingToolCalls.Remove(idValue);
                    }
                }

                if (cancelledIds.Count > 0)
                {
                    _logAction(LogLevel.Info, $"Tool calls cancelled: {string.Join(", ", cancelledIds)}");
                }
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
                _logAction(LogLevel.Info, "Google voice provider disposed");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error during disposal: {ex.Message}");
            }
        }
    }
}
