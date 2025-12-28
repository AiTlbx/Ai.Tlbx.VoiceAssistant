using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Terminal.Gui;
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Hardware.Windows;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;
using Ai.Tlbx.VoiceAssistant.Provider.Google;
using Ai.Tlbx.VoiceAssistant.Provider.Google.Models;
using Ai.Tlbx.VoiceAssistant.Provider.XAi;
using Ai.Tlbx.VoiceAssistant.Provider.XAi.Models;
using Ai.Tlbx.VoiceAssistant.BuiltInTools;

namespace Ai.Tlbx.VoiceAssistant.Demo.Console;

public class Program
{
    private static VoiceAssistant? _assistant;
    private static WindowsAudioHardware? _audioHardware;
    private static TextView? _chatView;
    private static TextView? _logView;
    private static Button? _talkButton;
    private static Button? _micTestButton;
    private static Label? _statusLabel;
    private static ComboBox? _providerCombo;
    private static ComboBox? _voiceCombo;
    private static ComboBox? _micCombo;
    private static readonly StringBuilder _chatHistory = new();
    private static readonly StringBuilder _logHistory = new();
    private static readonly List<IVoiceTool> _tools = new();
    private static List<AudioDeviceInfo> _microphones = new();
    private static bool _isConnected = false;

    private static readonly string[] _providers = { "OpenAI", "Google", "xAI" };
    private static readonly string[][] _voices =
    {
        new[] { "Alloy", "Ash", "Coral", "Echo", "Sage", "Shimmer" },
        new[] { "Aoede", "Charon", "Fenrir", "Kore", "Puck" },
        new[] { "Ara", "Rex", "Sal", "Eve", "Leo" }
    };

    public static void Main(string[] args)
    {
        Debug.WriteLine("[TUI] Starting application...");

        Application.Init();
        Debug.WriteLine("[TUI] Application.Init() completed");

        _tools.Add(new TimeTool());
        _tools.Add(new WeatherTool());
        _tools.Add(new CalculatorTool());

        var mainWindow = new Window
        {
            Title = "AI Voice Assistant [Enter=Start/Stop] [Ctrl+Q=Quit]",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Left panel - Settings (compact)
        var settingsFrame = new FrameView
        {
            Title = "Settings",
            X = 0,
            Y = 0,
            Width = 32,
            Height = 9
        };

        // Provider
        var providerLabel = new Label { Text = "Provider:", X = 1, Y = 0 };
        _providerCombo = new ComboBox
        {
            X = 1,
            Y = 1,
            Width = 28,
            Height = 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        _providerCombo.SetSource(new ObservableCollection<string>(_providers));
        _providerCombo.SelectedItem = 0; // OpenAI
        _providerCombo.SelectedItemChanged += (_, _) => UpdateVoiceOptions();
        _providerCombo.HasFocusChanged += (_, e) => Debug.WriteLine($"[TUI] Provider combo focus: {e.NewValue}");

        // Voice
        var voiceLabel = new Label { Text = "Voice:", X = 1, Y = 2 };
        _voiceCombo = new ComboBox
        {
            X = 1,
            Y = 3,
            Width = 28,
            Height = 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        _voiceCombo.SetSource(new ObservableCollection<string>(_voices[0]));
        _voiceCombo.SelectedItem = 5; // Shimmer
        _voiceCombo.HasFocusChanged += (_, e) => Debug.WriteLine($"[TUI] Voice combo focus: {e.NewValue}");

        // Microphone
        var micLabel = new Label { Text = "Microphone:", X = 1, Y = 4 };
        _micCombo = new ComboBox
        {
            X = 1,
            Y = 5,
            Width = 28,
            Height = 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        _micCombo.SetSource(new ObservableCollection<string> { "(Loading...)" });
        _micCombo.SelectedItemChanged += OnMicrophoneChanged;
        _micCombo.HasFocusChanged += (_, e) => Debug.WriteLine($"[TUI] Mic combo focus: {e.NewValue}");

        settingsFrame.Add(providerLabel, _providerCombo, voiceLabel, _voiceCombo, micLabel, _micCombo);

        // API Keys status
        var keysFrame = new FrameView
        {
            Title = "API Keys",
            X = 0,
            Y = Pos.Bottom(settingsFrame),
            Width = 32,
            Height = 5
        };

        var openAiKey = new Label { Text = GetKeyStatus("OPENAI_API_KEY"), X = 1, Y = 0 };
        var googleKey = new Label { Text = GetKeyStatus("GOOGLE_API_KEY"), X = 1, Y = 1 };
        var xaiKey = new Label { Text = GetKeyStatus("XAI_API_KEY"), X = 1, Y = 2 };
        keysFrame.Add(openAiKey, googleKey, xaiKey);

        // Controls
        var controlsFrame = new FrameView
        {
            Title = "Controls",
            X = 0,
            Y = Pos.Bottom(keysFrame),
            Width = 32,
            Height = 5
        };

        _talkButton = new Button
        {
            Text = "Start [Enter]",
            X = 1,
            Y = 0
        };
        _talkButton.Accepting += (_, _) => OnTalkButtonClicked();

        _micTestButton = new Button
        {
            Text = "Mic Test [F5]",
            X = 15,
            Y = 0
        };
        _micTestButton.Accepting += (_, _) => OnMicTestClicked();

        _statusLabel = new Label
        {
            Text = "Ready",
            X = 1,
            Y = 2,
            Width = 28
        };

        controlsFrame.Add(_talkButton, _micTestButton, _statusLabel);

        // Chat panel (right side, top)
        var chatFrame = new FrameView
        {
            Title = "Conversation",
            X = 32,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(70)
        };

        _chatView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        chatFrame.Add(_chatView);

        // Log panel (right side, bottom)
        var logFrame = new FrameView
        {
            Title = "Logs",
            X = 32,
            Y = Pos.Bottom(chatFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _logView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        logFrame.Add(_logView);

        mainWindow.Add(settingsFrame, keysFrame, controlsFrame, chatFrame, logFrame);

        // Keyboard shortcuts - Enter to start/stop, F5 for mic test, Ctrl+Q to quit
        mainWindow.KeyDown += (_, e) =>
        {
            Debug.WriteLine($"[TUI] Key pressed: {e.KeyCode}");
            if (e.KeyCode == KeyCode.Enter)
            {
                OnTalkButtonClicked();
                e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.F5)
            {
                OnMicTestClicked();
                e.Handled = true;
            }
            else if (e.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
            {
                Application.RequestStop();
                e.Handled = true;
            }
            else if (e.KeyCode == (KeyCode.C | KeyCode.CtrlMask))
            {
                ClearChat();
                e.Handled = true;
            }
        };

        // Load microphones async after UI is ready
        Application.Invoke(async () => await LoadMicrophonesAsync());

        Debug.WriteLine("[TUI] Starting Application.Run(mainWindow)...");
        Application.Run(mainWindow);

        mainWindow.Dispose();
        Application.Shutdown();

        CleanupAsync().GetAwaiter().GetResult();
        Debug.WriteLine("[TUI] Application exited");
    }

    private static async Task LoadMicrophonesAsync()
    {
        try
        {
            Debug.WriteLine("[TUI] Loading microphones...");
            var tempHardware = new WindowsAudioHardware();
            _microphones = await tempHardware.GetAvailableMicrophones();

            var micNames = _microphones.Select(m => m.IsDefault ? $"* {m.Name}" : m.Name).ToList();
            if (micNames.Count == 0)
            {
                micNames.Add("(No microphones found)");
            }

            Application.Invoke(() =>
            {
                _micCombo?.SetSource(new ObservableCollection<string>(micNames));
                var defaultIndex = _microphones.FindIndex(m => m.IsDefault);
                if (_micCombo != null && defaultIndex >= 0)
                {
                    _micCombo.SelectedItem = defaultIndex;
                }
            });

            Debug.WriteLine($"[TUI] Loaded {_microphones.Count} microphones");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TUI] Failed to load microphones: {ex.Message}");
        }
    }

    private static async void OnMicrophoneChanged(object? sender, ListViewItemEventArgs e)
    {
        if (_audioHardware == null || _micCombo == null) return;
        if (_micCombo.SelectedItem < 0 || _micCombo.SelectedItem >= _microphones.Count) return;

        var mic = _microphones[_micCombo.SelectedItem];
        Debug.WriteLine($"[TUI] Switching to microphone: {mic.Name}");
        await _audioHardware.SetMicrophoneDevice(mic.Id);
    }

    private static async Task CleanupAsync()
    {
        if (_assistant != null)
        {
            _assistant.OnMessageAdded = null;
            _assistant.OnConnectionStatusChanged = null;
            await _assistant.DisposeAsync();
            _assistant = null;
        }
    }

    private static void UpdateVoiceOptions()
    {
        if (_providerCombo == null || _voiceCombo == null) return;

        var providerIndex = _providerCombo.SelectedItem;
        if (providerIndex >= 0 && providerIndex < _voices.Length)
        {
            _voiceCombo.SetSource(new ObservableCollection<string>(_voices[providerIndex]));
            _voiceCombo.SelectedItem = 0;
        }
    }

    private static string GetKeyStatus(string envVar)
    {
        var key = Environment.GetEnvironmentVariable(envVar);
        var status = string.IsNullOrEmpty(key) ? "X" : "OK";
        var shortName = envVar.Replace("_API_KEY", "");
        return $"[{status}] {shortName}";
    }

    private static async void OnTalkButtonClicked()
    {
        Debug.WriteLine($"[TUI] Talk button clicked, isConnected={_isConnected}");
        if (_isConnected)
        {
            await StopAsync();
        }
        else
        {
            await StartAsync();
        }
    }

    private static async void OnMicTestClicked()
    {
        // Don't allow mic test while connected to a provider
        if (_isConnected)
        {
            UpdateStatus("Stop session first");
            return;
        }

        Debug.WriteLine("[TUI] Starting mic test (beep -> record -> beep -> playback)");

        Application.Invoke(() =>
        {
            _micTestButton!.Text = "Testing...";
            _micTestButton.Enabled = false;
        });

        try
        {
            // Create audio hardware
            _audioHardware = new WindowsAudioHardware();
            _audioHardware.SetLogAction(Log);

            // Set selected microphone
            if (_micCombo != null && _micCombo.SelectedItem >= 0 && _micCombo.SelectedItem < _microphones.Count)
            {
                var mic = _microphones[_micCombo.SelectedItem];
                await _audioHardware.SetMicrophoneDevice(mic.Id);
            }

            // Create a temporary VoiceAssistant just for mic testing (no provider needed for test)
            // We need a dummy provider, so let's use the hardware test directly
            var testAssistant = new VoiceAssistant(_audioHardware, null!, Log);

            var success = await testAssistant.TestMicrophoneAsync();

            Application.Invoke(() =>
            {
                if (success)
                {
                    UpdateStatus("Mic test passed!");
                    AppendChat("[System]: Microphone test completed successfully.");
                }
                else
                {
                    UpdateStatus("Mic test failed");
                    AppendChat("[System]: Microphone test failed - check logs.");
                }
            });

            await testAssistant.DisposeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TUI] Mic test error: {ex.Message}");
            Application.Invoke(() =>
            {
                UpdateStatus($"Mic test error: {ex.Message}");
            });
        }
        finally
        {
            _audioHardware = null;
            Application.Invoke(() =>
            {
                _micTestButton!.Text = "Mic Test [F5]";
                _micTestButton.Enabled = true;
            });
        }
    }

    private static async Task StartAsync()
    {
        if (_providerCombo == null || _voiceCombo == null || _talkButton == null || _statusLabel == null)
            return;

        try
        {
            Debug.WriteLine("[TUI] StartAsync beginning...");
            UpdateStatus("Initializing...");

            _audioHardware = new WindowsAudioHardware();
            _audioHardware.SetLogAction(Log);

            // Set selected microphone
            if (_micCombo != null && _micCombo.SelectedItem >= 0 && _micCombo.SelectedItem < _microphones.Count)
            {
                var mic = _microphones[_micCombo.SelectedItem];
                await _audioHardware.SetMicrophoneDevice(mic.Id);
                Debug.WriteLine($"[TUI] Using microphone: {mic.Name}");
            }

            var (provider, settings) = CreateProviderAndSettings(
                _providerCombo.SelectedItem,
                _voiceCombo.SelectedItem);

            if (provider == null || settings == null)
            {
                UpdateStatus("Missing API key!");
                return;
            }

            Debug.WriteLine($"[TUI] Creating VoiceAssistant with {provider.GetType().Name}");
            _assistant = new VoiceAssistant(_audioHardware, provider, Log);

            _assistant.OnMessageAdded = message =>
            {
                Debug.WriteLine($"[TUI] Message: {message.Role}");
                Application.Invoke(() =>
                {
                    var prefix = message.Role == ChatMessage.UserRole ? "You" : "AI";
                    AppendChat($"[{prefix}]: {message.Content}");
                });
            };

            _assistant.OnConnectionStatusChanged = status =>
            {
                Debug.WriteLine($"[TUI] Status: {status}");
                Application.Invoke(() => UpdateStatus(status));
            };

            await _assistant.StartAsync(settings);

            _isConnected = true;
            Application.Invoke(() =>
            {
                _talkButton.Text = "Stop [Enter]";
                UpdateStatus("Listening...");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TUI] StartAsync error: {ex}");
            Log(LogLevel.Error, $"Failed to start: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}");
        }
    }

    private static async Task StopAsync()
    {
        if (_assistant == null || _talkButton == null) return;

        try
        {
            Debug.WriteLine("[TUI] StopAsync...");
            await _assistant.StopAsync();
            _assistant.OnMessageAdded = null;
            _assistant.OnConnectionStatusChanged = null;
            await _assistant.DisposeAsync();
            _assistant = null;
            _audioHardware = null;

            _isConnected = false;
            Application.Invoke(() =>
            {
                _talkButton.Text = "Start [Enter]";
                UpdateStatus("Stopped");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TUI] StopAsync error: {ex}");
            Log(LogLevel.Error, $"Failed to stop: {ex.Message}");
        }
    }

    private static (IVoiceProvider?, IVoiceSettings?) CreateProviderAndSettings(int providerIndex, int voiceIndex)
    {
        return providerIndex switch
        {
            0 => CreateOpenAi(voiceIndex),
            1 => CreateGoogle(voiceIndex),
            2 => CreateXai(voiceIndex),
            _ => (null, null)
        };
    }

    private static (IVoiceProvider?, IVoiceSettings?) CreateOpenAi(int voiceIndex)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Log(LogLevel.Error, "OPENAI_API_KEY not set");
            return (null, null);
        }

        var voices = new[] { AssistantVoice.Alloy, AssistantVoice.Ash, AssistantVoice.Coral,
                             AssistantVoice.Echo, AssistantVoice.Sage, AssistantVoice.Shimmer };

        var provider = new OpenAiVoiceProvider(apiKey, Log);
        var settings = new OpenAiVoiceSettings
        {
            Voice = voices[Math.Clamp(voiceIndex, 0, voices.Length - 1)],
            Instructions = "You are a helpful assistant. Keep responses concise.",
            Tools = _tools
        };

        return (provider, settings);
    }

    private static (IVoiceProvider?, IVoiceSettings?) CreateGoogle(int voiceIndex)
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Log(LogLevel.Error, "GOOGLE_API_KEY not set");
            return (null, null);
        }

        var voices = new[] { GoogleVoice.Aoede, GoogleVoice.Charon, GoogleVoice.Fenrir,
                             GoogleVoice.Kore, GoogleVoice.Puck };

        var provider = new GoogleVoiceProvider(apiKey, Log);
        var settings = new GoogleVoiceSettings
        {
            Voice = voices[Math.Clamp(voiceIndex, 0, voices.Length - 1)],
            Instructions = "You are a helpful assistant. Keep responses concise.",
            Tools = _tools
        };

        return (provider, settings);
    }

    private static (IVoiceProvider?, IVoiceSettings?) CreateXai(int voiceIndex)
    {
        var apiKey = Environment.GetEnvironmentVariable("XAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Log(LogLevel.Error, "XAI_API_KEY not set");
            return (null, null);
        }

        var voices = new[] { XaiVoice.Ara, XaiVoice.Rex, XaiVoice.Sal, XaiVoice.Eve, XaiVoice.Leo };

        var provider = new XaiVoiceProvider(apiKey, Log);
        var settings = new XaiVoiceSettings
        {
            Voice = voices[Math.Clamp(voiceIndex, 0, voices.Length - 1)],
            Instructions = "You are a helpful assistant. Keep responses concise.",
            Tools = _tools
        };

        return (provider, settings);
    }

    private static void Log(LogLevel level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelStr = level switch
        {
            LogLevel.Error => "ERR",
            LogLevel.Warn => "WRN",
            _ => "INF"
        };

        var logLine = $"[{timestamp}] [{levelStr}] {message}";
        Debug.WriteLine($"[VA] {logLine}");

        Application.Invoke(() =>
        {
            _logHistory.AppendLine(logLine);
            if (_logView != null)
            {
                _logView.Text = _logHistory.ToString();
                _logView.MoveEnd();
            }
        });
    }

    private static void AppendChat(string message)
    {
        _chatHistory.AppendLine(message);
        _chatHistory.AppendLine();

        if (_chatView != null)
        {
            _chatView.Text = _chatHistory.ToString();
            _chatView.MoveEnd();
        }
    }

    private static void UpdateStatus(string status)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = status;
        }
    }

    private static void ClearChat()
    {
        _chatHistory.Clear();
        if (_chatView != null)
        {
            _chatView.Text = "";
        }
    }
}
