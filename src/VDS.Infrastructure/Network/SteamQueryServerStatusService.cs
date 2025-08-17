using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VDS.Contracts.Server;
using VDS.Core.Features.Server;
using System.Net.Sockets;
using System.Text;

namespace VDS.Infrastructure.Network;

/// <summary>
/// Implementation of server status service using Steam query protocol
/// Based on the Python valheim-status implementation
/// </summary>
public class SteamQueryServerStatusService : IServerStatusService
{
    private readonly IOptions<ServerConfiguration> _config;
    private readonly ILogger<SteamQueryServerStatusService> _logger;
    private ServerStatus _currentStatus = ServerStatus.Unknown;
    private readonly object _statusLock = new();

    public SteamQueryServerStatusService(
        IOptions<ServerConfiguration> config,
        ILogger<SteamQueryServerStatusService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<ServerStatusInfo?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var config = _config.Value;
        var queryHost = "localhost";
        var queryPort = config.ServerQueryPort;

        try
        {
            _logger.LogDebug("Querying server status at {Host}:{Port}", queryHost, queryPort);

            // Simple implementation - in a full implementation you'd want to use 
            // a proper A2S (Source/Steam query protocol) library
            using var client = new UdpClient();
            client.Connect(queryHost, queryPort);

            // Send A2S_INFO query
            var infoQuery = CreateA2SInfoQuery();
            await client.SendAsync(infoQuery, cancellationToken);

            // Set receive timeout
            client.Client.ReceiveTimeout = 5000;

            var response = await client.ReceiveAsync();
            var serverInfo = ParseA2SInfoResponse(response.Buffer);

            // Get player information
            var playersQuery = CreateA2SPlayerQuery();
            await client.SendAsync(playersQuery, cancellationToken);
            
            var playersResponse = await client.ReceiveAsync();
            var players = ParseA2SPlayerResponse(playersResponse.Buffer);

            return new ServerStatusInfo
            {
                Status = _currentStatus,
                ServerName = serverInfo.ServerName,
                ServerType = "Dedicated",
                Platform = "Linux",
                PlayerCount = players.Count,
                PasswordProtected = !string.IsNullOrEmpty(config.ServerPassword),
                VacEnabled = false, // Valheim doesn't use VAC
                Port = config.ServerPort,
                SteamId = 0, // Would need to be extracted from server response
                Keywords = "",
                GameId = 892970, // Valheim's Steam app ID
                Players = players,
                LastUpdate = DateTimeOffset.UtcNow,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query server status");
            
            return new ServerStatusInfo
            {
                Status = ServerStatus.Error,
                PlayerCount = 0,
                Players = new List<PlayerInfo>(),
                LastUpdate = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task UpdateStatusAsync(ServerStatus status, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_statusLock)
            {
                _currentStatus = status;
                _logger.LogInformation("Server status updated to: {Status}", status);
            }
        }, cancellationToken);
    }

    public async Task<bool> IsServerListeningAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _config.Value;
            using var client = new TcpClient();
            
            // Try to connect to the server port
            var connectTask = client.ConnectAsync("localhost", config.ServerPort);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == connectTask && client.Connected)
            {
                _logger.LogDebug("Server is listening on port {Port}", config.ServerPort);
                return true;
            }
            
            _logger.LogDebug("Server is not listening on port {Port}", config.ServerPort);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if server is listening");
            return false;
        }
    }

    private static byte[] CreateA2SInfoQuery()
    {
        // A2S_INFO query packet
        // Format: 0xFFFFFFFF + "TSource Engine Query" + 0x00
        var query = new List<byte>();
        query.AddRange(BitConverter.GetBytes(0xFFFFFFFF));
        query.Add(0x54); // 'T'
        query.AddRange(Encoding.ASCII.GetBytes("Source Engine Query"));
        query.Add(0x00);
        return query.ToArray();
    }

    private static byte[] CreateA2SPlayerQuery()
    {
        // A2S_PLAYER query packet  
        // Format: 0xFFFFFFFF + 0x55 + challenge (4 bytes)
        var query = new List<byte>();
        query.AddRange(BitConverter.GetBytes(0xFFFFFFFF));
        query.Add(0x55); // 'U'
        query.AddRange(BitConverter.GetBytes(0xFFFFFFFF)); // Challenge (simplified)
        return query.ToArray();
    }

    private ServerInfo ParseA2SInfoResponse(byte[] response)
    {
        // Simplified parsing - a full implementation would properly parse the A2S_INFO response
        try
        {
            if (response.Length < 10)
                return new ServerInfo { ServerName = "Unknown" };

            // Skip header (5 bytes: 0xFFFFFFFF + response type)
            var offset = 5;
            
            // Read server name (null-terminated string)
            var serverName = ReadNullTerminatedString(response, ref offset);
            
            return new ServerInfo
            {
                ServerName = serverName
            };
        }
        catch (Exception)
        {
            return new ServerInfo { ServerName = "Unknown" };
        }
    }

    private List<PlayerInfo> ParseA2SPlayerResponse(byte[] response)
    {
        // Simplified parsing - a full implementation would properly parse the A2S_PLAYER response
        var players = new List<PlayerInfo>();
        
        try
        {
            if (response.Length < 6)
                return players;

            // Skip header (5 bytes) and player count (1 byte)
            var offset = 5;
            var playerCount = response[offset++];

            for (int i = 0; i < playerCount && offset < response.Length; i++)
            {
                // Each player has: index (1 byte) + name (string) + score (4 bytes) + duration (4 bytes)
                if (offset >= response.Length) break;
                
                offset++; // Skip index
                var name = ReadNullTerminatedString(response, ref offset);
                
                if (offset + 8 > response.Length) break;
                
                var score = BitConverter.ToInt32(response, offset);
                offset += 4;
                
                var duration = BitConverter.ToSingle(response, offset);
                offset += 4;

                players.Add(new PlayerInfo
                {
                    Name = name,
                    Score = score,
                    Duration = TimeSpan.FromSeconds(duration)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse player response");
        }

        return players;
    }

    private static string ReadNullTerminatedString(byte[] data, ref int offset)
    {
        var start = offset;
        while (offset < data.Length && data[offset] != 0)
        {
            offset++;
        }
        
        var length = offset - start;
        offset++; // Skip null terminator
        
        return length > 0 ? Encoding.UTF8.GetString(data, start, length) : string.Empty;
    }

    private record ServerInfo
    {
        public string ServerName { get; init; } = string.Empty;
    }
}