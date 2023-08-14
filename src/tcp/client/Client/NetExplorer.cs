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
    /// <param name="portNumber">           (Optional) (5025) The port number. </param>
    /// <param name="timeoutMilliseconds">  (Optional) The timeout in milliseconds. </param>
    /// <returns>   True if it succeeds, false if it fails. </returns>
    public static bool PingPort( string ipv4Address, int portNumber = 5025, int timeoutMilliseconds = 10 )
    {
        try
        {
            using Socket socket = new( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
            socket.Blocking = true;
            IAsyncResult result = socket.BeginConnect( ipv4Address, portNumber, null, null );
            bool success = result.AsyncWaitHandle.WaitOne( timeoutMilliseconds, true );
            if ( socket.Connected )
            {
                socket.EndConnect( result );
                socket.Shutdown( SocketShutdown.Both );
                socket.Close();
                // this is required for the server to recover after the socket is closed.
                System.Threading.Thread.Sleep( 1 );
                return true;
            }
            else
            {
                socket.Close();
                return false;
            }
        }
        catch
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
