namespace Ai.Tlbx.VoiceAssistant.Models
{
    /// <summary>
    /// Event arguments for microphone audio data received events.
    /// </summary>
    public class MicrophoneAudioReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the base64-encoded PCM 16-bit audio data.
        /// </summary>
        public string Base64EncodedPcm16Audio { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrophoneAudioReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="base64EncodedPcm16Audio">The base64-encoded PCM 16-bit audio data.</param>
        public MicrophoneAudioReceivedEventArgs(string base64EncodedPcm16Audio)
        {
            Base64EncodedPcm16Audio = base64EncodedPcm16Audio;
        }
    }

    /// <summary>
    /// Delegate for handling microphone audio data received events.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments containing audio data.</param>
    public delegate void MicrophoneAudioReceivedEventHandler(object sender, MicrophoneAudioReceivedEventArgs e);
}