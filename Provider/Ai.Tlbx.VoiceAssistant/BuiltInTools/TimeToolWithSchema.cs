using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Enhanced time tool with parameter schema support for timezone conversion.
    /// </summary>
    public class TimeToolWithSchema : ValidatedVoiceToolBase<TimeToolWithSchema.TimeArgs>
    {
        /// <summary>
        /// Arguments for the time tool.
        /// </summary>
        public class TimeArgs
        {
            /// <summary>
            /// The timezone to convert to (optional).
            /// </summary>
            public string TimeZone { get; set; }

            /// <summary>
            /// The format for the time output.
            /// </summary>
            public string Format { get; set; } = "full";
        }

        /// <inheritdoc/>
        public override string Name => "get_current_time_advanced";

        /// <inheritdoc/>
        public override string Description => "Gets the current date and time with optional timezone conversion and formatting";

        /// <inheritdoc/>
        public override ToolParameterSchema GetParameterSchema() => new()
        {
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["timeZone"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "The timezone to convert to (e.g., 'UTC', 'EST', 'PST', 'Europe/London'). If not specified, uses server timezone (Berlin time)."
                },
                ["format"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "The format for the output",
                    Enum = new List<string> { "full", "date", "time", "iso8601", "unix" },
                    Default = "full"
                }
            },
            Required = new List<string>() // All parameters are optional
        };

        /// <inheritdoc/>
        protected override Task<string> ExecuteValidatedAsync(TimeArgs args)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                TimeZoneInfo targetTimeZone = TimeZoneInfo.Local;

                // Handle timezone conversion
                if (!string.IsNullOrEmpty(args.TimeZone))
                {
                    try
                    {
                        targetTimeZone = GetTimeZoneInfo(args.TimeZone);
                        currentTime = TimeZoneInfo.ConvertTime(currentTime, targetTimeZone);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        return Task.FromResult(CreateErrorResult($"Unknown timezone: {args.TimeZone}"));
                    }
                }

                string formattedTime = args.Format?.ToLowerInvariant() switch
                {
                    "date" => currentTime.ToString("yyyy-MM-dd"),
                    "time" => currentTime.ToString("HH:mm:ss"),
                    "iso8601" => currentTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    "unix" => ((DateTimeOffset)currentTime).ToUnixTimeSeconds().ToString(),
                    _ => currentTime.ToString("yyyy-MM-dd HH:mm:ss") // full format
                };

                var result = new
                {
                    datetime = formattedTime,
                    timezone = targetTimeZone.DisplayName,
                    timezone_id = targetTimeZone.Id,
                    utc_offset = targetTimeZone.GetUtcOffset(currentTime).ToString(),
                    is_daylight_saving = targetTimeZone.IsDaylightSavingTime(currentTime)
                };

                return Task.FromResult(CreateSuccessResult(result));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateErrorResult($"Time conversion error: {ex.Message}"));
            }
        }

        private TimeZoneInfo GetTimeZoneInfo(string timeZone)
        {
            // Common timezone aliases
            return timeZone.ToUpperInvariant() switch
            {
                "UTC" => TimeZoneInfo.Utc,
                "EST" => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
                "PST" => TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"),
                "CST" => TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"),
                "MST" => TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time"),
                _ => TimeZoneInfo.FindSystemTimeZoneById(timeZone)
            };
        }
    }
}