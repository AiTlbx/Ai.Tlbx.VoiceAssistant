using Ai.Tlbx.VoiceAssistant.Demo.Web.Components;
using Ai.Tlbx.VoiceAssistant.Extensions;
using Ai.Tlbx.VoiceAssistant.Hardware.Web;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Extensions;
using Ai.Tlbx.VoiceAssistant.BuiltInTools;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ai.Tlbx.VoiceAssistant.Demo.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();


        // Configure VoiceAssistant with fluent DI pattern
        builder.Services.AddVoiceAssistant()
            .WithHardware<WebAudioAccess>()
            // Add all built-in tools (includes both basic and advanced tools)
            .AddBuiltInTools(includeAdvanced: true)
            .WithOpenAi()
            .WithLogging((level, message) => Debug.WriteLine($"[{level}] {message}"));
        
        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB, adjust as needed
            options.StreamBufferCapacity = 100; // Buffer for streaming
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}