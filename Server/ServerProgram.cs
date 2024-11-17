using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Server;

internal static class ServerProgram
{
    private static UdpClient _client;
    private static readonly BlockingCollection<UdpReceiveResult> _queue = new();

    private static async Task Main()
    {
        _client = new UdpClient(8080);

        for (var i = 0; i < 4; i++)
        {
            var thread = new Thread(DequeuePacketsAsync);
            thread.Start();
        }

        while (true)
        {
            var result = await _client.ReceiveAsync();
            _queue.Add(result);
        }
    }

    private static void DequeuePacketsAsync()
    {
        foreach (var result in _queue.GetConsumingEnumerable())
        {
            _client.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
        }
    }
}