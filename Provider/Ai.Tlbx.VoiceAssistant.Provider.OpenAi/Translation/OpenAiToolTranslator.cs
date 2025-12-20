using System.Collections.Generic;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
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
            var parameters = BuildParametersObject(schema);

            return new Dictionary<string, object>
            {
                ["type"] = "function",
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = parameters
            };
        }

        public object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools)
        {
            return tools.Select(t => TranslateToolDefinition(t.Tool, t.Schema)).ToList();
        }

        public object FormatToolResponse(string result, string callId, string toolName)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "conversation.item.create",
                ["item"] = new Dictionary<string, object>
                {
                    ["type"] = "function_call_output",
                    ["call_id"] = callId,
                    ["output"] = result
                }
            };
        }

        private Dictionary<string, object> BuildParametersObject(ToolSchema schema)
        {
            var properties = new Dictionary<string, object>();
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

            var result = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties
            };

            if (required.Count > 0)
            {
                result["required"] = required;
            }

            if (_useStrictMode)
            {
                result["additionalProperties"] = false;
            }

            return result;
        }

        private object BuildPropertyObject(ToolParameter param)
        {
            var prop = new Dictionary<string, object>();

            if (param.Nullable && _useStrictMode)
            {
                prop["type"] = new[] { TypeMapper.ToJsonSchemaType(param.Type), "null" };
            }
            else
            {
                prop["type"] = TypeMapper.ToJsonSchemaType(param.Type);
            }

            if (!string.IsNullOrEmpty(param.Description))
            {
                prop["description"] = param.Description;
            }

            if (param.Enum != null && param.Enum.Count > 0)
            {
                prop["enum"] = param.Enum;
            }

            if (!string.IsNullOrEmpty(param.Format))
            {
                prop["format"] = param.Format;
            }

            if (param.Default != null)
            {
                prop["default"] = param.Default;
            }

            if (param.Type == ToolParameterType.Array && param.Items != null)
            {
                prop["items"] = BuildPropertyObject(param.Items);
            }

            if (param.Type == ToolParameterType.Object && param.Properties != null)
            {
                var nestedProps = new Dictionary<string, object>();
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

                prop["properties"] = nestedProps;

                if (nestedRequired.Count > 0)
                {
                    prop["required"] = nestedRequired;
                }

                if (_useStrictMode)
                {
                    prop["additionalProperties"] = false;
                }
            }

            if (param.Minimum.HasValue)
            {
                prop["minimum"] = param.Minimum.Value;
            }

            if (param.Maximum.HasValue)
            {
                prop["maximum"] = param.Maximum.Value;
            }

            if (param.MinLength.HasValue)
            {
                prop["minLength"] = param.MinLength.Value;
            }

            if (param.MaxLength.HasValue)
            {
                prop["maxLength"] = param.MaxLength.Value;
            }

            if (!string.IsNullOrEmpty(param.Pattern))
            {
                prop["pattern"] = param.Pattern;
            }

            return prop;
        }
    }
}
