using System.Net;
using System.Net.Sockets;

namespace Server;

public class Connection
{
    public required TcpClient Client { get; init; }
    public required Socket Socket { get; init; }

    public int TotalPacketCount { get; set; }
    public long TotalPacketSize { get; set; }

    public override int GetHashCode()
    {
        return Client.Client.RemoteEndPoint!.GetHashCode();
    }
}

public readonly struct ConnectionPacket(Connection connection, byte[] data)
{
    public readonly Connection connection = connection;
    public readonly byte[] data = data;
}

internal static class ServerProgram
{
    private static readonly HashSet<Connection> _connections = [];

    private static async Task Main()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                Console.Clear();
                Console.WriteLine("-- Connection information --");
                Console.WriteLine($"Active connections: {_connections.Count}");
                Console.WriteLine($"Total packet count: {_connections.Sum(c => c.TotalPacketCount)}");
                Console.WriteLine($"Total packet size: {_connections.Sum(c => c.TotalPacketSize)}");
                Console.WriteLine($"Pending work item count: {ThreadPool.PendingWorkItemCount}");
                Console.WriteLine($"Thread count: {ThreadPool.ThreadCount}");
                Console.WriteLine($"Completed work item count: {ThreadPool.CompletedWorkItemCount}");
            }
        });

        var listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            var connection = new Connection
            {
                Client = client,
                Socket = client.Client
            };

            _connections.Add(connection);
            _ = HandleClientConnection(connection);
        }
    }

    private static async Task HandleClientConnection(Connection connection)
    {
        try
        {
            var buffer = new byte[1024];

            while (true)
            {
                var length = await connection.Socket.ReceiveAsync(buffer);
                connection.TotalPacketCount += 1;
                connection.TotalPacketSize += length;

                var data = buffer[..length];

                _ = Task.Run(async () =>
                {
                    await connection.Socket.SendAsync(data);
                });

                ThreadPool.QueueUserWorkItem(
                    callBack: HandlePacket,
                    state: new ConnectionPacket(connection, data),
                    preferLocal: false
                );
            }
        }
        catch (SocketException)
        {
            _connections.Remove(connection);
        }
    }

    private static void HandlePacket(ConnectionPacket packet)
    {
        packet.connection.Socket.Send(packet.data);
    }
}