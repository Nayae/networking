using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client;

internal static class ClientProgram
{
    private static async Task Main()
    {
        var buffer = new byte[1024];

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, 8080);

        while (true)
        {
            const string message = "The quick brown fox jumps over the lazy dog.";
            var encoded = Encoding.Default.GetBytes(message);
            Array.Copy(encoded, 0, buffer, 0, encoded.Length);

            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.SendPacketsElements =
            [
                new SendPacketsElement(buffer, 0, encoded.Length)
            ];

            client.Client.SendPacketsAsync(socketAsyncEventArgs);

            await Task.Delay(10);
        }
    }
}