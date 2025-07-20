using System;
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
                await _hardwareAccess.StopRecordingAudio();
                IsRecording = false;
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
        /// Tests the microphone by recording a brief sample and checking for audio data.
        /// </summary>
        /// <returns>A task representing the microphone test operation.</returns>
        public async Task<bool> TestMicrophoneAsync()
        {
            try
            {
                _isMicrophoneTesting = true;
                ReportStatus("Testing microphone...");
                
                // Initialize audio hardware first
                await _hardwareAccess.InitAudio();
                
                // Start a brief recording session to test the microphone
                bool testRecordingStarted = await _hardwareAccess.StartRecordingAudio(OnTestAudioDataReceived);
                
                if (!testRecordingStarted)
                {
                    ReportStatus("Microphone test failed: Could not start recording");
                    return false;
                }
                
                // Let it record for a few seconds
                await Task.Delay(3000);
                
                // Stop the test recording
                await _hardwareAccess.StopRecordingAudio();
                
                ReportStatus("Microphone test completed");
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
                    _logAction(LogLevel.Info, "Audio queue cleared due to speech detection");
                }
                catch (Exception ex)
                {
                    _logAction(LogLevel.Error, $"Error clearing audio queue on interruption: {ex.Message}");
                }
            };
        }

        private async void OnAudioDataReceived(object sender, MicrophoneAudioReceivedEventArgs e)
        {
            try
            {
                if (_provider.IsConnected && !_isMicrophoneTesting)
                {
                    await _provider.ProcessAudioAsync(e.Base64EncodedPcm16Audio);
                }
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _logAction(LogLevel.Error, $"Error processing audio data: {ex.Message}");
            }
        }

        private void OnTestAudioDataReceived(object sender, MicrophoneAudioReceivedEventArgs e)
        {
            // During microphone testing, just log that we received audio data
            _logAction(LogLevel.Info, $"Test audio data received: {e.Base64EncodedPcm16Audio.Length} chars");
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