using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Protocol;
using Ai.Tlbx.VoiceAssistant.Reflection;
using Ai.Tlbx.VoiceAssistant.Translation;

namespace Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Translation
{
    /// <summary>
    /// Translates tool schemas to OpenAI Realtime API format.
    /// Supports strict mode with nullable types and additionalProperties enforcement.
    /// </summary>
    public class OpenAiToolTranslator : IToolSchemaTranslator
    {
        private readonly bool _useStrictMode;

        public OpenAiToolTranslator(bool useStrictMode = true)
        {
            _useStrictMode = useStrictMode;
        }

        public object TranslateToolDefinition(IVoiceTool tool, ToolSchema schema)
        {
            return new ToolDefinition
            {
                Type = "function",
                Name = tool.Name,
                Description = tool.Description,
                Parameters = BuildParametersObject(schema)
            };
        }

        public object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools)
        {
            return tools.Select(t => TranslateToolDefinition(t.Tool, t.Schema)).ToList();
        }

        public object FormatToolResponse(string result, string callId, string toolName)
        {
            return new ConversationItemCreateMessage
            {
                Item = new ConversationItem
                {
                    Type = "function_call_output",
                    CallId = callId,
                    Output = result
                }
            };
        }

        private ToolParameters BuildParametersObject(ToolSchema schema)
        {
            var properties = new Dictionary<string, ToolProperty>();
            var required = new List<string>();

            foreach (var (name, param) in schema.Parameters)
            {
                properties[name] = BuildPropertyObject(param);

                if (_useStrictMode)
                {
                    required.Add(name);
                }
                else if (schema.Required.Contains(name))
                {
                    required.Add(name);
                }
            }

            return new ToolParameters
            {
                Type = "object",
                Properties = properties,
                Required = required.Count > 0 ? required : null,
                AdditionalProperties = _useStrictMode ? false : null
            };
        }

        private ToolProperty BuildPropertyObject(ToolParameter param)
        {
            var prop = new ToolProperty();

            var typeName = TypeMapper.ToJsonSchemaType(param.Type);
            if (param.Nullable && _useStrictMode)
            {
                using var doc = JsonDocument.Parse($"[\"{typeName}\", \"null\"]");
                prop.Type = doc.RootElement.Clone();
            }
            else
            {
                using var doc = JsonDocument.Parse($"\"{typeName}\"");
                prop.Type = doc.RootElement.Clone();
            }

            if (!string.IsNullOrEmpty(param.Description))
            {
                prop.Description = param.Description;
            }

            if (param.Enum != null && param.Enum.Count > 0)
            {
                prop.Enum = param.Enum;
            }

            if (!string.IsNullOrEmpty(param.Format))
            {
                prop.Format = param.Format;
            }

            if (param.Default != null)
            {
                prop.Default = SerializeDefaultValue(param.Default);
            }

            if (param.Type == ToolParameterType.Array && param.Items != null)
            {
                prop.Items = BuildPropertyObject(param.Items);
            }

            if (param.Type == ToolParameterType.Object && param.Properties != null)
            {
                var nestedProps = new Dictionary<string, ToolProperty>();
                var nestedRequired = new List<string>();

                foreach (var (name, nestedParam) in param.Properties)
                {
                    nestedProps[name] = BuildPropertyObject(nestedParam);

                    if (_useStrictMode)
                    {
                        nestedRequired.Add(name);
                    }
                    else if (param.RequiredProperties?.Contains(name) == true)
                    {
                        nestedRequired.Add(name);
                    }
                }

                prop.Properties = nestedProps;
                prop.Required = nestedRequired.Count > 0 ? nestedRequired : null;
                prop.AdditionalProperties = _useStrictMode ? false : null;
            }

            if (param.Minimum.HasValue)
            {
                prop.Minimum = param.Minimum.Value;
            }

            if (param.Maximum.HasValue)
            {
                prop.Maximum = param.Maximum.Value;
            }

            if (param.MinLength.HasValue)
            {
                prop.MinLength = param.MinLength.Value;
            }

            if (param.MaxLength.HasValue)
            {
                prop.MaxLength = param.MaxLength.Value;
            }

            if (!string.IsNullOrEmpty(param.Pattern))
            {
                prop.Pattern = param.Pattern;
            }

            return prop;
        }

        private static JsonElement SerializeDefaultValue(object value)
        {
            var json = value switch
            {
                string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
                bool b => b ? "true" : "false",
                int i => i.ToString(),
                long l => l.ToString(),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => $"\"{value}\""
            };

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
    }
}
