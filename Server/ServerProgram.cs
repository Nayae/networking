﻿using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

                ThreadPool.QueueUserWorkItem(
                    callBack: HandlePacket,
                    state: new ConnectionPacket(connection, data),
                    preferLocal: true
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
        Task.Run(async () =>
        {
            await packet.connection.Socket.SendAsync(packet.data);
        }).Wait();
    }
}