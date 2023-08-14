using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace cc.isr.Iot.Tcp.Client;

/// <summary>   A net explorer. </summary>
/// <remarks>   2023-08-14. </remarks>
public static class NetExplorer
{

    /// <summary>   Pings the host at the specified port. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="ipv4Address">          The host IPv4 address. </param>
    /// <param name="portNumber">           The port number. </param>
    /// <returns>   True if it succeeds, false if it fails. </returns>
    public static bool PingPort( string ipv4Address, int portNumber )
    {
        try
        {
            using ( var client = new TcpClient( ipv4Address, portNumber ) )
            {
                return true;
            }
        }
        catch ( SocketException )
        {
            return false;
        }
    }


    /// <summary>   Ping host. </summary>
         /// <remarks>   2022-11-04. </remarks>
         /// <param name="nameOrAddress">    The name or address. </param>
         /// <returns>   True if it succeeds, false if it fails. </returns>
    public static bool PingHost( string nameOrAddress )
    {
        bool pingable = false;
        try
        {
            using Ping ping = new();
            PingReply reply = ping.Send( nameOrAddress );
            pingable = reply.Status == IPStatus.Success;
        }
        catch ( PingException )
        {
            // Discard PingExceptions and return false;
        }
        return pingable;
    }

}
