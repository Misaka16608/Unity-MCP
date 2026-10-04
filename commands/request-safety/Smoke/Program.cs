/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try { await Run(args); return 0; }
        catch (Exception ex)
        {
            // A failed diagnostic is a logged nonzero exit, never an unhandled CLR
            // exception that opens Windows Error Reporting on the user's desktop.
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
    private static async Task Run(string[] args)
    {
        using var plugin = new McpPluginBuilder(new com.IvanMurzak.McpPlugin.Common.Version { Api = Consts.ApiVersion, Plugin = Consts.PluginVersion })
            .WithConfig(config => { config.Host = args[0].TrimEnd('/'); config.GenerateSkillFiles = false; config.KeepConnected = false; config.MaxConsecutiveConnectionFailures = 1; })
            .WithTools<DeliverySmokeTool>().Build(new Reflector());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        if (!await plugin.Connect(deadline.Token)) throw new InvalidOperationException("Paired plugin failed its real SignalR connection and version handshake.");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
        using var initialize = await http.PostAsync(args[0], Json("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"paired-delivery-smoke\",\"version\":\"1\"}}}"));
        initialize.EnsureSuccessStatusCode();
        var session = string.Join("", initialize.Headers.GetValues("Mcp-Session-Id"));
        http.DefaultRequestHeaders.Add("Mcp-Session-Id", session);
        using var initialized = await http.PostAsync(args[0], Json("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}"));
        initialized.EnsureSuccessStatusCode();
        const string call = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"delivery-smoke\",\"arguments\":{}}}";
        using var first = await http.PostAsync(args[0], Json(call));
        first.EnsureSuccessStatusCode();
        var result = await first.Content.ReadAsStringAsync();
        using var duplicate = await http.PostAsync(args[0], Json(call));
        duplicate.EnsureSuccessStatusCode();
        if (DeliverySmokeTool.Writes != 1 || result.Contains("\"isError\":true"))
            throw new InvalidOperationException("Packaged host failed real HTTP/SignalR execution or deduplication: " + result);
        Console.WriteLine("PASS: packaged native Windows host, paired API " + Consts.ApiVersion + ", real plugin SignalR registration, MCP initialize and same-ID tool execution exactly once.");
    }
    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
}

[AiToolType]
public sealed class DeliverySmokeTool
{
    public static int Writes;
    [AiTool("delivery-smoke")]
    public static int Write() => Interlocked.Increment(ref Writes);
}
