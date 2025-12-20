using System.Collections.Generic;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Reflection;
using Ai.Tlbx.VoiceAssistant.Translation;

namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Translation
{
    /// <summary>
    /// Translates tool schemas to Google Gemini Live API format.
    /// Uses functionDeclarations wrapper structure.
    /// </summary>
    public class GoogleToolTranslator : IToolSchemaTranslator
    {
        public object TranslateToolDefinition(IVoiceTool tool, ToolSchema schema)
        {
            var parameters = BuildParametersObject(schema);

            return new Dictionary<string, object>
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = parameters
            };
        }

        public object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools)
        {
            var functionDeclarations = tools
                .Select(t => TranslateToolDefinition(t.Tool, t.Schema))
                .ToList();

            return new List<object>
            {
                new Dictionary<string, object>
                {
                    ["functionDeclarations"] = functionDeclarations
                }
            };
        }

        public object FormatToolResponse(string result, string callId, string toolName)
        {
            return new Dictionary<string, object>
            {
                ["toolResponse"] = new Dictionary<string, object>
                {
                    ["functionResponses"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["id"] = callId,
                            ["name"] = toolName,
                            ["response"] = new Dictionary<string, object>
                            {
                                ["result"] = result
                            }
                        }
                    }
                }
            };
        }

        private Dictionary<string, object> BuildParametersObject(ToolSchema schema)
        {
            var properties = new Dictionary<string, object>();

            foreach (var (name, param) in schema.Parameters)
            {
                properties[name] = BuildPropertyObject(param);
            }

            var result = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties
            };

            if (schema.Required.Count > 0)
            {
                result["required"] = schema.Required.ToList();
            }

            return result;
        }

        private object BuildPropertyObject(ToolParameter param)
        {
            var prop = new Dictionary<string, object>
            {
                ["type"] = TypeMapper.ToJsonSchemaType(param.Type)
            };

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

            if (param.Type == ToolParameterType.Array && param.Items != null)
            {
                prop["items"] = BuildPropertyObject(param.Items);
            }

            if (param.Type == ToolParameterType.Object && param.Properties != null)
            {
                var nestedProps = new Dictionary<string, object>();

                foreach (var (name, nestedParam) in param.Properties)
                {
                    nestedProps[name] = BuildPropertyObject(nestedParam);
                }

                prop["properties"] = nestedProps;

                if (param.RequiredProperties?.Count > 0)
                {
                    prop["required"] = param.RequiredProperties.ToList();
                }
            }

            return prop;
        }
    }
}
