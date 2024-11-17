using System.Diagnostics;
using System.Net.Sockets;

namespace Client;

internal static class ClientProgram
{
    private static readonly Dictionary<int, long> _times = new Dictionary<int, long>();

    private static UdpClient _client;
    private static readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

    private static void Main()
    {
        _client = new UdpClient("127.0.0.1", 8080);

        new Thread(() => _ = HandleSend()).Start();
        new Thread(() => _ = HandleReceive()).Start();
    }

    private static async Task HandleSend()
    {
        while (true)
        {
            var epoch = GetNanoTime();
            var indexEncoded = BitConverter.GetBytes(_times.Count);
            _times[_times.Count] = epoch;

            await _client.SendAsync(indexEncoded);
            _autoResetEvent.WaitOne();
        }
    }

    private static async Task HandleReceive()
    {
        while (true)
        {
            var result = await _client.ReceiveAsync();
            var index = BitConverter.ToInt32(result.Buffer);

            var end = GetNanoTime();
            var delta = end - _times[index];

            Console.WriteLine($"Roundtrip {index} took: {(double)delta / 1_000_000}ms");
            _autoResetEvent.Set();
        }
    }

    private static long GetNanoTime()
    {
        var nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
}