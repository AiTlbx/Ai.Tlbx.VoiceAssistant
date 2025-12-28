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