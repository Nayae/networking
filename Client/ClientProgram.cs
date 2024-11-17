using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Client;

internal static class ClientProgram
{
    private static readonly Dictionary<int, long> _times = new Dictionary<int, long>();

    private static async Task Main()
    {
        var client = new TcpClient();
        // await client.ConnectAsync(IPAddress.Parse("134.209.202.27"), 8080);
        await client.ConnectAsync(IPAddress.Loopback, 8080);

        _ = HandleSend(client);
        _ = HandleReceive(client);

        Console.ReadKey();
    }

    private static async Task HandleSend(TcpClient client)
    {
        while (true)
        {
            var epoch = nanoTime();
            var indexEncoded = BitConverter.GetBytes(_times.Count);
            _times[_times.Count] = epoch;

            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.SendPacketsElements =
            [
                new SendPacketsElement(indexEncoded)
            ];

            client.Client.SendPacketsAsync(socketAsyncEventArgs);
            await Task.Delay(10);
        }
    }

    private static async Task HandleReceive(TcpClient client)
    {
        var receiveBuffer = new byte[1024];

        while (true)
        {
            await client.Client.ReceiveAsync(receiveBuffer);
            var end = nanoTime();

            var index = BitConverter.ToInt32(receiveBuffer);
            var delta = end - _times[index];

            Console.WriteLine($"Roundtrip {index} took: {(double)delta / 1_000_000}ms");
        }
    }

    private static long nanoTime()
    {
        var nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
}