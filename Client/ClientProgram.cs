using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Client;

internal static class ClientProgram
{
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
        var sendBuffer = new byte[1024];

        while (true)
        {
            var epoch = nanoTime();
            var encoded = BitConverter.GetBytes(epoch);
            Array.Copy(encoded, 0, sendBuffer, 0, encoded.Length);

            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.SendPacketsElements =
            [
                new SendPacketsElement(sendBuffer, 0, encoded.Length)
            ];

            client.Client.SendPacketsAsync(socketAsyncEventArgs);
            await Task.Delay(1000);
        }
    }

    private static async Task HandleReceive(TcpClient client)
    {
        var receiveBuffer = new byte[1024];

        while (true)
        {
            await client.Client.ReceiveAsync(receiveBuffer);
            var end = nanoTime();
            var start = BitConverter.ToInt64(receiveBuffer, 0);

            Console.WriteLine($"Roundtrip took: {(double)(end - start) / 1_000_000}ms");
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