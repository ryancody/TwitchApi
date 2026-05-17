using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TwitchApi.Models;

public class SocketWrapper : IDisposable
{
    public event Action<WebSocketMessage> MessageReceived;
    public event Action<WebSocketState> WebSocketStateChanged;
    public ConcurrentQueue<WebSocketMessage> Messages = [];
    public WebSocketState State => webSocket.State;

    private ClientWebSocket webSocket;
    private WebSocketState cachedWebsocketState = WebSocketState.None;
    private ILogger logger;
    private Task? receivingTask;
    private CancellationTokenSource? cts;

    public SocketWrapper(ILogger logger)
    {
        this.logger = logger;
        webSocket = new ClientWebSocket();
        cts = new CancellationTokenSource();
    }

    public void Dispose()
    {
        webSocket.Dispose();
    }

    public async Task StartReceiving(Uri uri)
    {
        await webSocket.ConnectAsync(uri, cts.Token);
        receivingTask = Receive();
    }

    public async Task StopReceiving()
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        cts.Cancel();

        if (receivingTask is not null)
            await receivingTask;
    }

    private async Task Receive()
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();

        while (!cts.IsCancellationRequested)
        {
            if (webSocket.State != cachedWebsocketState)
            {
                cachedWebsocketState = webSocket.State;
                WebSocketStateChanged?.Invoke(cachedWebsocketState);
            }

            if (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(buffer, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Don't care about task cancellation exceptions
                    return;
                }
                catch
                {
                    throw;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    var text = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrEmpty(text))
                    {
                        logger.LogInformation("Received: " + text);

                        var message = JsonSerializer.Deserialize<WebSocketMessage>(text);

                        if (message is not null)
                        {
                            Messages.Enqueue(message);
                            MessageReceived?.Invoke(message);
                        }
                    }
                }
            }
        }
    }
}
