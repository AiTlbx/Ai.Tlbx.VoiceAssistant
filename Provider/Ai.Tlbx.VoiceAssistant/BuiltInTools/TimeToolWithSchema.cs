using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    public enum TimeFormat
    {
        Full,
        Date,
        Time,
        Iso8601,
        Unix
    }

    [Description("Gets the current date and time with optional timezone conversion and formatting")]
    public class TimeToolWithSchema : VoiceToolBase<TimeToolWithSchema.Args>
    {
        public record Args(
            [property: Description("The timezone to convert to (e.g., 'UTC', 'EST', 'PST', 'Europe/London'). If not specified, uses server timezone (Berlin time).")] string? TimeZone = null,
            [property: Description("The format for the output")] TimeFormat Format = TimeFormat.Full
        );

        public override string Name => "get_current_time_advanced";

        public override Task<string> ExecuteAsync(Args args)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                TimeZoneInfo targetTimeZone = TimeZoneInfo.Local;

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

                string formattedTime = args.Format switch
                {
                    TimeFormat.Date => currentTime.ToString("yyyy-MM-dd"),
                    TimeFormat.Time => currentTime.ToString("HH:mm:ss"),
                    TimeFormat.Iso8601 => currentTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    TimeFormat.Unix => ((DateTimeOffset)currentTime).ToUnixTimeSeconds().ToString(),
                    _ => currentTime.ToString("yyyy-MM-dd HH:mm:ss")
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

        private static TimeZoneInfo GetTimeZoneInfo(string timeZone)
        {
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
