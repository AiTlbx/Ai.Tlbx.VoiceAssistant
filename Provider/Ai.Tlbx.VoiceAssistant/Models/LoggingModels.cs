namespace Ai.Tlbx.VoiceAssistant.Models
{
    /// <summary>
    /// Simple 3-level logging enumeration for voice assistant components.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Error level - for critical errors that require attention.
        /// </summary>
        Error = 0,

        /// <summary>
        /// Warning level - for warnings that may indicate issues.
        /// </summary>
        Warn = 1,

        /// <summary>
        /// Information level - for general information and debugging.
        /// </summary>
        Info = 2
    }

    /// <summary>
    /// Categories for different areas of voice assistant functionality.
    /// </summary>
    public enum StatusCategory
    {
        Connection,
        Recording,
        Processing,
        Configuration,
        Error,
        Tool
    }

    /// <summary>
    /// Debug categories for different voice assistant subsystems.
    /// </summary>
    public enum LogCategory
    {
        General,
        Provider,
        Audio,
        Session,
        Tooling,
        Microphone,
        Cleanup,
        Initialization,
        MessageProcessing,
        MicTest,
        VoiceAssistant
    }

    /// <summary>
    /// Detailed status codes for voice assistant operations.
    /// </summary>
    public enum StatusCode
    {
        // Connection related
        Connecting,
        Connected,
        Disconnected,
        ConnectionFailed,
        Reconnecting,
        
        // Recording related
        RecordingStarted,
        RecordingStopped,
        RecordingFailed,
        MicrophoneDetected,
        MicrophoneChanged,
        MicrophoneTestStarted,
        MicrophoneTestCompleted,
        MicrophoneTestFailed,
        Stopping,
        Stopped,
        Interrupting,
        Interrupted,
        
        // Processing related
        ProcessingStarted,
        ProcessingCompleted,
        ProcessingFailed,
        MessageReceived,
        MessageSent,
        
        // Configuration related
        Initialized,
        ConfigurationUpdated,
        VoiceChanged,
        SettingsApplied,
        
        // Error related
        GeneralError,
        AudioError,
        NetworkError,
        Error,
        
        // Tool related
        ToolCallReceived,
        ToolResultSent
    }

    /// <summary>
    /// Diagnostic levels for voice assistant components.
    /// </summary>
    public enum DiagnosticLevel
    {
        None = 0,
        Basic = 1,
        Detailed = 2,
        Verbose = 3
    }
}