using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using isr.Iot.Tcp.Client;

namespace isr.Iot.Tcp.Session.Helper;

public enum InstrumentId
{
    None, K2450 = 2450, K2600 = 2600, K6510 = 6510, K7510 = 7510
}

public static class SessionManager
{
    private static readonly Dictionary<string, (int ReadAfterWriteDelay, int InterQqueryDelay, string IPAddress)> _instrumentInfo;

    static SessionManager()
    {
        _instrumentInfo = new() {
        { "Echo", (0, 0, "127.0.0.1") },
        { InstrumentId.K2450.ToString() , (0, 0, "192.168.0.152") },
        { InstrumentId.K2600.ToString(), (0, 0, "192.168.0.50") },
        { InstrumentId.K6510.ToString(), (0, 0, "192.168.0.154") },
        { InstrumentId.K7510.ToString(), (0, 0, "192.168.0.144") }
        };
    }

    /// <summary>   Gets or sets information describing the query. </summary>
    /// <value> Information describing the query. </value>
    public static string QueryInfo { get; set; }

    /// <summary>   Queries the identity. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="instrumentId">         Identifier for the instrument. </param>
    /// <param name="connectionTimeout">    The connection timeout. </param>
    /// <returns>   The identity. </returns>
    public static string QueryIdentity( InstrumentId instrumentId, TimeSpan connectionTimeout )
    {

        string command = "*IDN?";
        int portNumber = instrumentId == InstrumentId.None ? 13000 : 5025;

        string instrument = instrumentId == InstrumentId.None ? "Echo" : instrumentId.ToString();
        TimeSpan readAfterWriteDelay = TimeSpan.FromMilliseconds( _instrumentInfo[instrument].ReadAfterWriteDelay );
        int interQueryDelayMs = _instrumentInfo[instrument].InterQqueryDelay;
        string ipAddress = _instrumentInfo[instrument].IPAddress;
        if ( !Paping( ipAddress , portNumber, (int)connectionTimeout.TotalMilliseconds) )
        {
            QueryInfo = $"Attempt to connect to {instrument} at {ipAddress}:{portNumber} aborted after {connectionTimeout.TotalMilliseconds:0}ms";
            return string.Empty;
        }

        QueryInfo = $"{instrument} Delays: Read: {readAfterWriteDelay.TotalMilliseconds:0}ms; Write: {interQueryDelayMs}ms";

        System.Text.StringBuilder builder = new();
        using var session = new TcpSession( ipAddress, portNumber );

        string identity = string.Empty;
        session.Connect( true, command, ref identity, true );
        _ = builder.Append( $"a: {identity}\n" );

        string response = QueryDevice( session, command, 256, true );
        _ = builder.Append( $"b: {response}\n" );

        if ( interQueryDelayMs > 0 ) System.Threading.Thread.Sleep( interQueryDelayMs );
        response = QueryDevice( session, command, 256, true );
        _ = builder.Append( $"c: {response}\n" );

        if ( interQueryDelayMs > 0 ) System.Threading.Thread.Sleep( interQueryDelayMs );
        response = QueryDevice( session, command, 256, true );
        _ = builder.Append( $"d: {response}\n" );

        return builder.ToString();
    }

    /// <summary>   Queries identity asynchronously. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="instrumentId">         Identifier for the instrument. </param>
    /// <param name="connectionTimeout">    The connection timeout. </param>
    /// <returns>   The identity asynchronous. </returns>
    public static string QueryIdentityAsync( InstrumentId instrumentId, TimeSpan connectionTimeout )
    {

        string command = "*IDN?";
        int portNumber = instrumentId == InstrumentId.None ? 13000 : 5025;
        string instrument = instrumentId == InstrumentId.None ? "Echo" : instrumentId.ToString();
        TimeSpan readAfterWriteDelay = TimeSpan.FromMilliseconds( _instrumentInfo[instrument].ReadAfterWriteDelay );
        int interqueryDelayMs = _instrumentInfo[instrument].InterQqueryDelay;
        string ipAddress = _instrumentInfo[instrument].IPAddress;

        if ( !Paping( ipAddress, portNumber, ( int ) connectionTimeout.TotalMilliseconds ) )
        {
            QueryInfo = $"Attempt to connect to {instrument} at {ipAddress}:{portNumber} aborted after {connectionTimeout.TotalMilliseconds:0}ms";
            return string.Empty;
        }


        QueryInfo = $"Async {instrument} Delays: Read: {readAfterWriteDelay.TotalMilliseconds:0}ms; Write: {interqueryDelayMs}ms";

        System.Text.StringBuilder builder = new();
        using var session = new TcpSession( ipAddress, portNumber );

        string identity = string.Empty;
        session.Connect( true, command, ref identity, true );
        _ = builder.Append( $"a: {identity}\n" );

        string response = QueryDeviceAsync( session, command, 256, readAfterWriteDelay, true );
        _ = builder.Append( $"b: {response}\n" );

        if ( interqueryDelayMs > 0 ) System.Threading.Thread.Sleep( interqueryDelayMs );
        response = QueryDeviceAsync( session, command, 256, readAfterWriteDelay, true );
        _ = builder.Append( $"c: {response}\n" );

        if ( interqueryDelayMs > 0 ) System.Threading.Thread.Sleep( interqueryDelayMs );
        response = QueryDeviceAsync( session, command, 256, readAfterWriteDelay, true );
        _ = builder.Append( $"d: {response}\n" );

        return builder.ToString();
    }

    /// <summary>   Queries a device. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="session">      The session. </param>
    /// <param name="command">      The command. </param>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="trimEnd">      True to trim end. </param>
    /// <returns>   The device. </returns>
    private static string QueryDevice( TcpSession session, string command, int byteCount, bool trimEnd )
    {
        try
        {
            string reply = string.Empty;
            var response = session.QueryLine( command, byteCount, ref reply, trimEnd );
            return reply;
        }
        catch ( ApplicationException ex )
        {
            Console.WriteLine( ex.ToString() );
        }
        return "Exception occurred";
    }

    private static string QueryDeviceAsync( TcpSession session, string command, int byteCount, TimeSpan readDelay, bool trimEnd )
    {
        try
        {
            var task = session.QueryLineAsync( command, byteCount, readDelay, trimEnd, session.CancellationToken );
            task.Wait( session.CancellationToken );
            return task.Result;
        }
        catch ( ApplicationException ex )
        {
            Console.WriteLine( ex.ToString() );
        }
        return "Exception occurred";
    }

    /// <summary>   Pings port. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="ipv4Address">          The IPv4 address. </param>
    /// <param name="portNumber">           (Optional) The port number. </param>
    /// <param name="timeoutMilliseconds">  (Optional) The timeout in milliseconds. </param>
    /// <returns>   True if it succeeds, false if it fails. </returns>
    public static bool Paping( string ipv4Address, int portNumber = 5025, int timeoutMilliseconds = 10 )
    {
        try
        {
            using Socket socket = new ( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
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
            using Ping pinger = new ();
            PingReply reply = pinger.Send( nameOrAddress );
            pingable = reply.Status == IPStatus.Success;
        }
        catch ( PingException )
        {
            // Discard PingExceptions and return false;
        }
        return pingable;
    }
}
