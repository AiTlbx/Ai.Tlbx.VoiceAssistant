using System;
using System.Linq;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Managers;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant
{
    /// <summary>
    /// Main orchestrator for voice assistant functionality across different AI providers.
    /// Manages the interaction between hardware, AI providers, and UI components.
    /// </summary>
    public sealed class VoiceAssistant : IAsyncDisposable
    {
        private readonly IAudioHardwareAccess _hardwareAccess;
        private readonly IVoiceProvider _provider;
        private readonly ChatHistoryManager _chatHistory;
        private readonly Action<LogLevel, string> _logAction;
        
        // State management
        private bool _isInitialized = false;
        private bool _isConnecting = false;
        private bool _isDisposed = false;
        private bool _isMicrophoneTesting = false;
        private string? _lastErrorMessage = null;
        private string _connectionStatus = string.Empty;
        
        // Static cache for generated beeps (lazy-initialized)
        private static readonly Dictionary<(int frequency, int duration, int sampleRate), string> _beepCache = new();

#if DEBUG
        // 🎤💀 EXPERIMENTAL: Sample skip percentage for bandwidth testing (0 = no skip, 20 = skip 20% of samples)
        // WARNING: This violates Nyquist theorem and will introduce aliasing. For science only!
        private const int SAMPLE_SKIP_PERCENTAGE = 0; // Set to 5, 10, 20 etc to test
#endif
        
        // UI Callbacks - Direct actions for simple 1:1 communication
        /// <summary>
        /// Callback that fires when the connection status changes.
        /// </summary>
        public Action<string>? OnConnectionStatusChanged { get; set; }
        
        /// <summary>
        /// Callback that fires when a new message is added to the chat history.
        /// </summary>
        public Action<ChatMessage>? OnMessageAdded { get; set; }
        
        /// <summary>
        /// Callback that fires when the list of microphone devices changes.
        /// </summary>
        public Action<List<AudioDeviceInfo>>? OnMicrophoneDevicesChanged { get; set; }

        // Public properties
        /// <summary>
        /// Gets a value indicating whether the voice assistant is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Gets a value indicating whether audio recording is active.
        /// </summary>
        public bool IsRecording { get; private set; }
        
        /// <summary>
        /// Gets a value indicating whether the voice assistant is currently connecting.
        /// </summary>
        public bool IsConnecting => _isConnecting;
        
        /// <summary>
        /// Gets a value indicating whether microphone testing is active.
        /// </summary>
        public bool IsMicrophoneTesting => _isMicrophoneTesting;
        
        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        public string? LastErrorMessage => _lastErrorMessage;
        
        /// <summary>
        /// Gets the current connection status.
        /// </summary>
        public string ConnectionStatus => _connectionStatus;
        
        /// <summary>
        /// Gets the chat history as a read-only list.
        /// </summary>
        public IReadOnlyList<ChatMessage> ChatHistory => _chatHistory.GetMessages();

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceAssistant"/> class.
        /// </summary>
        /// <param name="hardwareAccess">The audio hardware access implementation.</param>
        /// <param name="provider">The AI voice provider implementation.</param>
        /// <param name="logAction">Optional logging action for compatibility.</param>
        public VoiceAssistant(
            IAudioHardwareAccess hardwareAccess, 
            IVoiceProvider provider,
            Action<LogLevel, string>? logAction = null)
        {
            _hardwareAccess = hardwareAccess ?? throw new ArgumentNullException(nameof(hardwareAccess));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logAction = logAction ?? ((level, message) => { /* no-op */ });
            _chatHistory = new ChatHistoryManager();
            
            // Set up hardware logging
            _hardwareAccess.SetLogAction(_logAction);
            
            // Wire up provider callbacks
            WireUpProviderCallbacks();
        }

        /// <summary>
        /// Starts the voice assistant with the specified settings.
        /// </summary>
        /// <param name="settings">Provider-specific settings for the voice assistant.</param>
        /// <returns>A task representing the start operation.</returns>
        public async Task StartAsync(IVoiceSettings settings)
        {
            await StartAsync(settings, CancellationToken.None);
        }

        /// <summary>
        /// Starts the voice assistant with the specified settings.
        /// </summary>
        /// <param name="settings">Provider-specific settings for the voice assistant.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the start operation.</returns>
        public async Task StartAsync(IVoiceSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                _lastErrorMessage = null;
                
                if (!_isInitialized || !_provider.IsConnected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _isConnecting = true;
                    ReportStatus("Connecting to AI provider...");
                    await _provider.ConnectAsync(settings);
                    _isInitialized = true;
                    _isConnecting = false;
                    
                    // Inject conversation history if available
                    var history = _chatHistory.GetMessages();
                    if (history.Any())
                    {
                        // Only inject messages up to pairs (user + assistant responses)
                        // This prevents the AI from continuing the last assistant message
                        var messagesToInject = new List<ChatMessage>();
                        for (int i = 0; i < history.Count; i++)
                        {
                            messagesToInject.Add(history[i]);
                            // Stop if we've added a complete pair (user + assistant)
                            if (i > 0 && i % 2 == 1)
                            {
                                // We've added a user and assistant message pair
                            }
                        }
                        
                        // If the last message is from the assistant, exclude it
                        // This prevents the AI from continuing where it left off
                        if (messagesToInject.Count > 0 && 
                            messagesToInject[messagesToInject.Count - 1].Role == ChatMessage.AssistantRole)
                        {
                            messagesToInject.RemoveAt(messagesToInject.Count - 1);
                        }
                        
                        if (messagesToInject.Any())
                        {
                            _logAction(LogLevel.Info, $"Injecting {messagesToInject.Count} messages from conversation history (excluded last assistant message if any)");
                            await _provider.InjectConversationHistoryAsync(messagesToInject);
                        }
                    }
                }
                else
                {
                    // Provider is already connected, just update the settings
                    _logAction(LogLevel.Info, "Provider already connected, updating settings");
                    await _provider.UpdateSettingsAsync(settings);
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // Initialize hardware
                await _hardwareAccess.InitAudio();
                
                // Start recording with audio callback handler
                bool recordingStarted = await _hardwareAccess.StartRecordingAudio(OnAudioDataReceived);
                
                if (!recordingStarted)
                {
                    throw new InvalidOperationException("Failed to start audio recording");
                }
                
                IsRecording = true;
                
                ReportStatus("Voice assistant started and recording");
                _logAction(LogLevel.Info, "Voice assistant started successfully");
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _isConnecting = false;
                ReportStatus($"Error: {ex.Message}");
                _logAction(LogLevel.Error, $"Failed to start voice assistant: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops the voice assistant and disconnects from the AI provider.
        /// </summary>
        /// <returns>A task representing the stop operation.</returns>
        public async Task StopAsync()
        {
            try
            {
                // First stop recording to prevent new audio
                await _hardwareAccess.StopRecordingAudio();
                IsRecording = false;
                
                // Clear any queued audio immediately
                await _hardwareAccess.ClearAudioQueue();
                
                // Then disconnect from provider
                await _provider.DisconnectAsync();
                _isInitialized = false;
                
                ReportStatus("Voice assistant stopped");
                _logAction(LogLevel.Info, "Voice assistant stopped");
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                ReportStatus($"Error stopping: {ex.Message}");
                _logAction(LogLevel.Error, $"Error stopping voice assistant: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends an interrupt signal to stop the current AI response.
        /// </summary>
        /// <returns>A task representing the interrupt operation.</returns>
        public async Task InterruptAsync()
        {
            try
            {
                // Send interrupt to provider
                await _provider.SendInterruptAsync();
                
                // Clear any pending audio to stop playback immediately
                await _hardwareAccess.ClearAudioQueue();
                
                _logAction(LogLevel.Info, "Interrupt signal sent to AI provider and audio queue cleared");
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _logAction(LogLevel.Error, $"Error sending interrupt: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests the microphone by recording a brief sample and playing it back.
        /// </summary>
        /// <returns>A task representing the microphone test operation.</returns>
        public async Task<bool> TestMicrophoneAsync()
        {
            List<string> recordedAudioChunks = new List<string>();
            
            try
            {
                _isMicrophoneTesting = true;
                ReportStatus("Testing speakers...");
                
                // Initialize audio hardware first
                await _hardwareAccess.InitAudio();
                
                // Play initial beep to test speakers (440 Hz for 200ms)
                _logAction(LogLevel.Info, "Playing initial beep to test speakers");
                _hardwareAccess.PlayAudio(GenerateBeep(440, 200), 24000);
                await Task.Delay(300); // Wait a bit after beep
                
                ReportStatus("Testing microphone - recording...");
                
                // Start recording with a callback that collects audio chunks
                bool testRecordingStarted = await _hardwareAccess.StartRecordingAudio((sender, e) =>
                {
                    recordedAudioChunks.Add(e.Base64EncodedPcm16Audio);
                    _logAction(LogLevel.Info, $"Test audio chunk received: {e.Base64EncodedPcm16Audio.Length} chars");
                });
                
                if (!testRecordingStarted)
                {
                    ReportStatus("Microphone test failed: Could not start recording");
                    return false;
                }
                
                // Record for 5 seconds
                await Task.Delay(5000);
                
                // Stop recording
                await _hardwareAccess.StopRecordingAudio();
                
                // Play end-of-recording beep (880 Hz for 200ms - higher pitch)
                _logAction(LogLevel.Info, "Playing end-of-recording beep");
                _hardwareAccess.PlayAudio(GenerateBeep(880, 200), 24000);
                await Task.Delay(300); // Wait a bit after beep
                
                if (recordedAudioChunks.Count == 0)
                {
                    ReportStatus("Microphone test failed: No audio data received");
                    return false;
                }
                
                ReportStatus("Playing back recorded audio...");
                _logAction(LogLevel.Info, $"Playing back {recordedAudioChunks.Count} audio chunks");

#if DEBUG
#pragma warning disable CS0162 // Unreachable code detected (SAMPLE_SKIP_PERCENTAGE is intentionally 0)
                if (SAMPLE_SKIP_PERCENTAGE > 0)
                {
                    _logAction(LogLevel.Warn, $"🎤💀 MICROPHONE TEST: Sample skip filter ({SAMPLE_SKIP_PERCENTAGE}%) will be applied to playback - listen for artifacts!");
                }
#pragma warning restore CS0162
#endif

                // Play back all recorded chunks
                foreach (var audioChunk in recordedAudioChunks)
                {
#if DEBUG
                    var audioToPlay = ApplySampleSkipFilter(audioChunk);
#else
                    var audioToPlay = audioChunk;
#endif
                    _hardwareAccess.PlayAudio(audioToPlay, 24000);
                }
                
                // Play final success beep (660 Hz for 300ms)
                await Task.Delay(200); // Small pause before final beep
                _logAction(LogLevel.Info, "Playing success beep");
                _hardwareAccess.PlayAudio(GenerateBeep(660, 300), 24000);
                
                ReportStatus("Microphone test completed successfully");
                _logAction(LogLevel.Info, "Microphone test completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                ReportStatus($"Microphone test failed: {ex.Message}");
                _logAction(LogLevel.Error, $"Microphone test failed: {ex.Message}");
                return false;
            }
            finally
            {
                _isMicrophoneTesting = false;
            }
        }

        /// <summary>
        /// Generates a simple sine wave beep as base64 encoded PCM audio.
        /// </summary>
        /// <param name="frequency">The frequency of the beep in Hz.</param>
        /// <param name="durationMs">The duration of the beep in milliseconds.</param>
        /// <param name="sampleRate">The sample rate in Hz (default 24000).</param>
        /// <returns>Base64 encoded PCM16 audio data.</returns>
        private string GenerateBeep(int frequency = 440, int durationMs = 200, int sampleRate = 24000)
        {
            // Check cache first
            var cacheKey = (frequency, durationMs, sampleRate);
            if (_beepCache.TryGetValue(cacheKey, out var cachedBeep))
            {
                return cachedBeep;
            }
            
            // Ensure frequency is below Nyquist frequency to prevent aliasing
            if (frequency > sampleRate / 2)
            {
                _logAction(LogLevel.Warn, $"Beep frequency {frequency}Hz exceeds Nyquist limit for {sampleRate}Hz sample rate");
                frequency = sampleRate / 2 - 100; // Set to just below Nyquist
            }
            
            int numSamples = (sampleRate * durationMs) / 1000;
            byte[] pcmData = new byte[numSamples * 2]; // 16-bit PCM = 2 bytes per sample
            
            // Fade in/out duration (5ms each)
            int fadeSamples = (sampleRate * 5) / 1000;
            fadeSamples = Math.Min(fadeSamples, numSamples / 4); // Don't fade more than 25% of the signal
            
            for (int i = 0; i < numSamples; i++)
            {
                double angle = 2.0 * Math.PI * frequency * i / sampleRate;
                double amplitude = 16000; // Base amplitude
                
                // Apply fade-in envelope
                if (i < fadeSamples)
                {
                    amplitude *= (double)i / fadeSamples;
                }
                // Apply fade-out envelope
                else if (i >= numSamples - fadeSamples)
                {
                    amplitude *= (double)(numSamples - i) / fadeSamples;
                }
                
                short sample = (short)(Math.Sin(angle) * amplitude);
                
                // Convert to little-endian bytes
                pcmData[i * 2] = (byte)(sample & 0xFF);
                pcmData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            
            var beepData = Convert.ToBase64String(pcmData);
            _beepCache[cacheKey] = beepData; // Cache for future use
            return beepData;
        }

        /// <summary>
        /// Gets the list of available microphone devices.
        /// </summary>
        /// <returns>A list of available audio devices.</returns>
        public async Task<List<AudioDeviceInfo>> GetAvailableMicrophonesAsync()
        {
            try
            {
                return await _hardwareAccess.GetAvailableMicrophones();
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _logAction(LogLevel.Error, $"Error getting available microphones: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clears the chat history.
        /// </summary>
        public void ClearChatHistory()
        {
            _chatHistory.ClearHistory();
            _logAction(LogLevel.Info, "Chat history cleared");
        }

        private void WireUpProviderCallbacks()
        {
            _provider.OnMessageReceived = (message) =>
            {
                _chatHistory.AddMessage(message);
                OnMessageAdded?.Invoke(message);
                _logAction(LogLevel.Info, $"Message received from provider: {message.Role} - {message.Content?.Length ?? 0} chars");
            };
            
            _provider.OnAudioReceived = (base64Audio) =>
            {
                try
                {
                    // Forward audio to hardware for playback at 24kHz (OpenAI default)
                    _hardwareAccess.PlayAudio(base64Audio, 24000);
                }
                catch (Exception ex)
                {
                    _logAction(LogLevel.Error, $"Error playing audio: {ex.Message}");
                }
            };
            
            _provider.OnStatusChanged = async (status) =>
            {
                ReportStatus(status);
                
                // Handle interruption status from provider
                if (status.Contains("interrupted", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await _hardwareAccess.ClearAudioQueue();
                        _logAction(LogLevel.Info, "Audio queue cleared due to interruption");
                    }
                    catch (Exception ex)
                    {
                        _logAction(LogLevel.Error, $"Error clearing audio queue: {ex.Message}");
                    }
                }
            };
            
            _provider.OnError = (error) =>
            {
                _lastErrorMessage = error;
                ReportStatus($"Provider error: {error}");
                _logAction(LogLevel.Error, $"Provider error: {error}");
            };
            
            // Wire up interruption detection to clear audio immediately
            _provider.OnInterruptDetected = async () =>
            {
                try
                {
                    await _hardwareAccess.ClearAudioQueue();
                    // Audio queue cleared for interruption - normal operation
                }
                catch (Exception ex)
                {
                    _logAction(LogLevel.Error, $"Error clearing audio queue on interruption: {ex.Message}");
                }
            };
        }

#if DEBUG
        /// <summary>
        /// 🎤💀 EXPERIMENTAL: Skips samples from base64 PCM16 audio for bandwidth testing.
        /// WARNING: This creates aliasing artifacts and violates audio engineering best practices!
        /// </summary>
#pragma warning disable CS0162 // Unreachable code detected (SAMPLE_SKIP_PERCENTAGE is intentionally 0)
        private string ApplySampleSkipFilter(string base64Audio)
        {
            if (SAMPLE_SKIP_PERCENTAGE <= 0 || SAMPLE_SKIP_PERCENTAGE >= 100)
                return base64Audio; // No filtering

            // Decode base64 to raw PCM16 bytes
            var audioBytes = Convert.FromBase64String(base64Audio);

            // Convert bytes to 16-bit samples (little-endian)
            var sampleCount = audioBytes.Length / 2;
            var samples = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (short)(audioBytes[i * 2] | (audioBytes[i * 2 + 1] << 8));
            }

            // FIXED PATTERN: Skip every Nth sample
            // Calculate skip interval: skip every Nth sample
            // NOTE: Integer division! Only accurate when 100 % SAMPLE_SKIP_PERCENTAGE == 0
            // Example: 20% → 100/20 = 5 → skip every 5th = 20% (exact)
            //          30% → 100/30 = 3 → skip every 3rd = 33.3% (not 30%!)
            var skipPercentage = SAMPLE_SKIP_PERCENTAGE; // Copy to variable to avoid compile-time constant division
            var skipInterval = 100 / skipPercentage;

            // Filter samples: keep all except every Nth
            var filteredSamples = samples
                .Where((sample, index) => (index + 1) % skipInterval != 0)
                .ToArray();

            var skippedCount = samples.Length - filteredSamples.Length;

            // Log the first time this runs
            if (_audioReceivedCount == 1)
            {
                var originalSize = audioBytes.Length;
                var newSize = filteredSamples.Length * 2;
                var actualSkipPercent = (1.0 - (double)filteredSamples.Length / samples.Length) * 100;
                _logAction(LogLevel.Warn, $"🎤💀 SAMPLE SKIP FILTER ACTIVE (FIXED PATTERN): {SAMPLE_SKIP_PERCENTAGE}% target, {actualSkipPercent:F1}% actual skip, {originalSize} → {newSize} bytes (interval: every {skipInterval}th sample)");
            }

            // Convert filtered samples back to bytes
            var filteredBytes = new byte[filteredSamples.Length * 2];
            for (int i = 0; i < filteredSamples.Length; i++)
            {
                filteredBytes[i * 2] = (byte)(filteredSamples[i] & 0xFF);
                filteredBytes[i * 2 + 1] = (byte)(filteredSamples[i] >> 8);
            }

            // Encode back to base64
            return Convert.ToBase64String(filteredBytes);
        }
#pragma warning restore CS0162
#endif

        private int _audioReceivedCount = 0;

        private async void OnAudioDataReceived(object sender, MicrophoneAudioReceivedEventArgs e)
        {
            try
            {
                _audioReceivedCount++;
                if (_audioReceivedCount % 50 == 1)
                {
                    _logAction(LogLevel.Info, $"[VA-AUDIO] OnAudioDataReceived called {_audioReceivedCount} times, IsConnected: {_provider.IsConnected}, IsTesting: {_isMicrophoneTesting}, AudioLength: {e.Base64EncodedPcm16Audio?.Length ?? 0}");
                }

                if (_provider.IsConnected && !_isMicrophoneTesting)
                {
                    if (_audioReceivedCount % 50 == 1)
                    {
                        _logAction(LogLevel.Info, $"[VA-AUDIO] Calling ProcessAudioAsync on provider");
                    }

#if DEBUG
#pragma warning disable CS0162 // Unreachable code detected (SAMPLE_SKIP_PERCENTAGE is intentionally 0)
                    var audioToSend = ApplySampleSkipFilter(e.Base64EncodedPcm16Audio ?? "");
#pragma warning restore CS0162
#else
                    var audioToSend = e.Base64EncodedPcm16Audio ?? "";
#endif
                    await _provider.ProcessAudioAsync(audioToSend);
                }
                else
                {
                    if (_audioReceivedCount % 50 == 1)
                    {
                        _logAction(LogLevel.Warn, $"[VA-AUDIO] Skipping audio - IsConnected: {_provider.IsConnected}, IsTesting: {_isMicrophoneTesting}");
                    }
                }
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _logAction(LogLevel.Error, $"Error processing audio data: {ex.Message}");
                _logAction(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
            }
        }


        private void ReportStatus(string status)
        {
            _connectionStatus = status;
            OnConnectionStatusChanged?.Invoke(status);
            _logAction(LogLevel.Info, $"Status: {status}");
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
                if (_isInitialized)
                {
                    await StopAsync();
                }
                
                await _provider.DisposeAsync();
                await _hardwareAccess.DisposeAsync();
                
                _isDisposed = true;
                _logAction(LogLevel.Info, "Voice assistant disposed");
            }
            catch (Exception ex)
            {
                _logAction(LogLevel.Error, $"Error during disposal: {ex.Message}");
            }
        }
    }
}