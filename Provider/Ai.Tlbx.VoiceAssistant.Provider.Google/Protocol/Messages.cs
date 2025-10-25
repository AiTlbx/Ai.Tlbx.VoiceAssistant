using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Protocol
{
    // Client-to-Server Messages

    /// <summary>
    /// Initial setup message sent to configure the session.
    /// </summary>
    public class SetupMessage
    {
        [JsonPropertyName("setup")]
        public Setup Setup { get; set; } = new();
    }

    public class Setup
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("generationConfig")]
        public GenerationConfig? GenerationConfig { get; set; }

        [JsonPropertyName("systemInstruction")]
        public SystemInstruction? SystemInstruction { get; set; }

        [JsonPropertyName("tools")]
        public List<Tool>? Tools { get; set; }

        [JsonPropertyName("inputAudioTranscription")]
        public object? InputAudioTranscription { get; set; }

        [JsonPropertyName("outputAudioTranscription")]
        public object? OutputAudioTranscription { get; set; }

        [JsonPropertyName("realtimeInputConfig")]
        public RealtimeInputConfig? RealtimeInputConfig { get; set; }
    }

    public class GenerationConfig
    {
        [JsonPropertyName("responseModalities")]
        public List<string>? ResponseModalities { get; set; }

        [JsonPropertyName("speechConfig")]
        public SpeechConfig? SpeechConfig { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("topP")]
        public double? TopP { get; set; }

        [JsonPropertyName("topK")]
        public int? TopK { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }
    }

    public class SpeechConfig
    {
        [JsonPropertyName("voiceConfig")]
        public VoiceConfig? VoiceConfig { get; set; }

        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }
    }

    public class VoiceConfig
    {
        [JsonPropertyName("prebuiltVoiceConfig")]
        public PrebuiltVoiceConfig? PrebuiltVoiceConfig { get; set; }
    }

    public class PrebuiltVoiceConfig
    {
        [JsonPropertyName("voiceName")]
        public string VoiceName { get; set; } = string.Empty;
    }

    public class SystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        public InlineData? InlineData { get; set; }
    }

    public class InlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    public class Tool
    {
        [JsonPropertyName("functionDeclarations")]
        public List<FunctionDeclaration>? FunctionDeclarations { get; set; }
    }

    public class FunctionDeclaration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public object? Parameters { get; set; }
    }

    /// <summary>
    /// Message for streaming realtime audio input.
    /// </summary>
    public class RealtimeInputMessage
    {
        [JsonPropertyName("realtimeInput")]
        public RealtimeInput RealtimeInput { get; set; } = new();
    }

    public class RealtimeInput
    {
        [JsonPropertyName("mediaChunks")]
        public List<MediaChunk>? MediaChunks { get; set; }
    }

    public class MediaChunk
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Message for sending text content or conversation history.
    /// </summary>
    public class ClientContentMessage
    {
        [JsonPropertyName("clientContent")]
        public ClientContent ClientContent { get; set; } = new();
    }

    public class ClientContent
    {
        [JsonPropertyName("turns")]
        public List<Turn> Turns { get; set; } = new();

        [JsonPropertyName("turnComplete")]
        public bool TurnComplete { get; set; }
    }

    public class Turn
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    /// <summary>
    /// Message for responding to tool/function calls.
    /// </summary>
    public class ToolResponseMessage
    {
        [JsonPropertyName("toolResponse")]
        public ToolResponse ToolResponse { get; set; } = new();
    }

    public class ToolResponse
    {
        [JsonPropertyName("functionResponses")]
        public List<FunctionResponse> FunctionResponses { get; set; } = new();
    }

    public class FunctionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public FunctionResponseData Response { get; set; } = new();
    }

    public class FunctionResponseData
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;
    }

    // Server-to-Client Messages

    /// <summary>
    /// Server message envelope.
    /// </summary>
    public class ServerMessage
    {
        [JsonPropertyName("setupComplete")]
        public object? SetupComplete { get; set; }

        [JsonPropertyName("serverContent")]
        public ServerContent? ServerContent { get; set; }

        [JsonPropertyName("toolCall")]
        public ToolCall? ToolCall { get; set; }

        [JsonPropertyName("toolCallCancellation")]
        public ToolCallCancellation? ToolCallCancellation { get; set; }
    }

    public class ServerContent
    {
        [JsonPropertyName("modelTurn")]
        public ModelTurn? ModelTurn { get; set; }

        [JsonPropertyName("turnComplete")]
        public bool TurnComplete { get; set; }

        [JsonPropertyName("interrupted")]
        public bool Interrupted { get; set; }
    }

    public class ModelTurn
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    public class ToolCall
    {
        [JsonPropertyName("functionCalls")]
        public List<FunctionCall>? FunctionCalls { get; set; }
    }

    public class FunctionCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public object? Args { get; set; }
    }

    public class ToolCallCancellation
    {
        [JsonPropertyName("ids")]
        public List<string>? Ids { get; set; }
    }

    /// <summary>
    /// Configuration for realtime input processing including voice activity detection.
    /// </summary>
    public class RealtimeInputConfig
    {
        [JsonPropertyName("automaticActivityDetection")]
        public AutomaticActivityDetection? AutomaticActivityDetection { get; set; }

        [JsonPropertyName("activityHandling")]
        public string? ActivityHandling { get; set; }
    }

    /// <summary>
    /// Configuration for automatic voice activity detection.
    /// </summary>
    public class AutomaticActivityDetection
    {
        [JsonPropertyName("startOfSpeechSensitivity")]
        public string? StartOfSpeechSensitivity { get; set; }

        [JsonPropertyName("endOfSpeechSensitivity")]
        public string? EndOfSpeechSensitivity { get; set; }

        [JsonPropertyName("prefixPaddingMs")]
        public int? PrefixPaddingMs { get; set; }

        [JsonPropertyName("silenceDurationMs")]
        public int? SilenceDurationMs { get; set; }

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }
    }
}
