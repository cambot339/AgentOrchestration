using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AgentOrchestration.Core;

const int discoveryPort = 42170;
const int commandPort = 42171;
const string discoveryRequest = "AGENT_ORCHESTRATION_DISCOVER";

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
var options = args.Skip(1).ToArray();

return command switch
{
    "listen" => await ListenAsync(options),
    "browse" => await BrowseAsync(),
    "dispatch" => await DispatchAsync(options),
    "help" or "--help" or "-h" => ShowHelp(),
    _ => UnknownCommand(command)
};

static int ShowHelp()
{
    Console.WriteLine("AgentOrchestration.Networking");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/AgentOrchestration.Networking -- listen [node-name] [log-path]");
    Console.WriteLine("  dotnet run --project src/AgentOrchestration.Networking -- browse");
    Console.WriteLine("  dotnet run --project src/AgentOrchestration.Networking -- dispatch <host> <command> [task-name] [log-path]");
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Use 'help' for usage.");
    return 1;
}

static async Task<int> ListenAsync(string[] options)
{
    var nodeName = options.ElementAtOrDefault(0) ?? Environment.MachineName;
    var logPath = options.ElementAtOrDefault(1) ?? Path.Combine("logs", "network-dispatch.jsonl");
    var announcement = CreateAnnouncement(nodeName);

    Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? ".");

    using var cancellationSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationSource.Cancel();
    };

    Console.WriteLine($"Listening as '{nodeName}'");
    Console.WriteLine($"Discovery: udp/{discoveryPort}");
    Console.WriteLine($"Commands : tcp/{commandPort}");
    Console.WriteLine($"Log file : {Path.GetFullPath(logPath)}");
    Console.WriteLine("Press Ctrl+C to stop.");

    await Task.WhenAll(
        RunDiscoveryResponderAsync(announcement, cancellationSource.Token),
        RunCommandListenerAsync(nodeName, logPath, cancellationSource.Token));

    return 0;
}

static async Task<int> BrowseAsync()
{
    using var client = new UdpClient();
    client.EnableBroadcast = true;
    client.Client.ReceiveTimeout = 500;

    var payload = Encoding.UTF8.GetBytes(discoveryRequest);

    try
    {
        await client.SendAsync(payload.AsMemory(), new IPEndPoint(IPAddress.Broadcast, discoveryPort));
        Console.WriteLine("Searching for agents on the local network...");
    }
    catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AccessDenied)
    {
        Console.WriteLine("Broadcast discovery is blocked in this environment; probing localhost instead.");
        await client.SendAsync(payload.AsMemory(), new IPEndPoint(IPAddress.Loopback, discoveryPort));
    }

    var results = new List<NodeAnnouncement>();
    var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(2);

    while (DateTimeOffset.UtcNow < timeoutAt)
    {
        try
        {
            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
            var response = await client.ReceiveAsync(receiveTimeout.Token);
            var announcement = JsonSerializer.Deserialize<NodeAnnouncement>(response.Buffer);
            if (announcement is not null && results.All(existing => existing.Name != announcement.Name))
            {
                results.Add(announcement with { Address = response.RemoteEndPoint.Address.ToString() });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (SocketException)
        {
        }
    }

    if (results.Count == 0)
    {
        Console.WriteLine("No agents responded. Start 'listen' on another machine and try again.");
        return 0;
    }

    foreach (var result in results.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine($"- {result.Name} | {result.OperatingSystem} | {result.Address}:{result.CommandPort} | tags: {string.Join(", ", result.Tags)}");
    }

    return 0;
}

static async Task<int> DispatchAsync(string[] options)
{
    var host = options.ElementAtOrDefault(0);
    var shellCommand = options.ElementAtOrDefault(1);

    if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(shellCommand))
    {
        Console.Error.WriteLine("Usage: dispatch <host> <command> [task-name] [log-path]");
        return 1;
    }

    var taskName = options.ElementAtOrDefault(2) ?? "network-dispatched-task";
    var logPath = options.ElementAtOrDefault(3) ?? Path.Combine("logs", "network-dispatch.jsonl");
    var envelope = new RemoteCommandEnvelope(taskName, shellCommand, DateTimeOffset.UtcNow);

    using var client = new TcpClient();
    await client.ConnectAsync(host, commandPort);
    await using var stream = client.GetStream();

    await JsonSerializer.SerializeAsync(stream, envelope);
    await stream.FlushAsync();
    client.Client.Shutdown(SocketShutdown.Send);

    using var reader = new StreamReader(stream, Encoding.UTF8);
    var response = await reader.ReadToEndAsync();
    Console.WriteLine(string.IsNullOrWhiteSpace(response) ? "No acknowledgement received." : response);

    await AppendLogEntryAsync(logPath, new
    {
        type = "dispatch-client",
        host,
        envelope.TaskName,
        envelope.Command,
        envelope.DispatchedAt
    });

    return 0;
}

static NodeAnnouncement CreateAnnouncement(string nodeName)
{
    var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    return new NodeAnnouncement(nodeName, os, "0.1-bootstrap", commandPort, string.Empty, ["networking", "agent-runtime"]);
}

static async Task RunDiscoveryResponderAsync(NodeAnnouncement announcement, CancellationToken cancellationToken)
{
    using var udpClient = new UdpClient(discoveryPort);

    while (!cancellationToken.IsCancellationRequested)
    {
        var result = await udpClient.ReceiveAsync(cancellationToken);
        var message = Encoding.UTF8.GetString(result.Buffer);
        if (!string.Equals(message, discoveryRequest, StringComparison.Ordinal))
        {
            continue;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(announcement);
        await udpClient.SendAsync(payload.AsMemory(), result.RemoteEndPoint, cancellationToken);
    }
}

static async Task RunCommandListenerAsync(string nodeName, string logPath, CancellationToken cancellationToken)
{
    var listener = new TcpListener(IPAddress.Any, commandPort);
    listener.Start();

    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
            _ = HandleCommandAsync(tcpClient, nodeName, logPath, cancellationToken);
        }
    }
    finally
    {
        listener.Stop();
    }
}

static async Task HandleCommandAsync(TcpClient tcpClient, string nodeName, string logPath, CancellationToken cancellationToken)
{
    using var _ = tcpClient;
    try
    {
        await using var stream = tcpClient.GetStream();
        var envelope = await JsonSerializer.DeserializeAsync<RemoteCommandEnvelope>(stream, cancellationToken: cancellationToken);
        if (envelope is null)
        {
            return;
        }

        await AppendLogEntryAsync(logPath, new
        {
            type = "dispatch-server",
            nodeName,
            envelope.TaskName,
            envelope.Command,
            envelope.DispatchedAt,
            receivedAt = DateTimeOffset.UtcNow
        });

        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync($"Queued '{envelope.TaskName}' on {nodeName}. Review the log for execution details.");
        await writer.FlushAsync();
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
    }
}

static async Task AppendLogEntryAsync(string logPath, object entry)
{
    Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? ".");
    var line = JsonSerializer.Serialize(entry);
    await File.AppendAllTextAsync(logPath, line + Environment.NewLine);
}

sealed record NodeAnnouncement(string Name, string OperatingSystem, string RuntimeVersion, int CommandPort, string Address, IReadOnlyList<string> Tags);
sealed record RemoteCommandEnvelope(string TaskName, string Command, DateTimeOffset DispatchedAt);
