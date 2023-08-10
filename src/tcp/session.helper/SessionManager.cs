using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using cc.isr.Iot.Tcp.Client;

namespace cc.isr.Iot.Tcp.Session.Helper;

public enum InstrumentId
{
    None, K2450 = 2450, K2600 = 2600, K2700P = 2700, K6510 = 6510, K7510 = 7510
}

/// <summary>   Manager for sessions. </summary>
/// <remarks>   2023-05-31. </remarks>
public static class SessionManager
{

    const int _prologixPortNo = 1234;

    const int _prologixWaitInterval = 5;

    private static readonly Dictionary<string, (int ReadAfterWriteDelay, int InterQqueryDelay, string IPAddress, int PortNumber)> _instrumentInfo;

    /// <summary>   Gets the cancellation token source. </summary>
    /// <value> The cancellation token source. </value>
    public static CancellationTokenSource CancellationTokenSource { get; private set; }

    /// <summary>   Static constructor. </summary>
    /// <remarks>   2023-05-31. </remarks>
    static SessionManager()
    {
        QueryInfo = string.Empty;
        _instrumentInfo = new() {
        { "Echo", (0, 0, "127.0.0.1", 13000) },
        { InstrumentId.K2450.ToString() , (0, 0, "192.168.0.152", 5025) },
        { InstrumentId.K2600.ToString(), (0, 0, "192.168.0.50", 5025) },
        { InstrumentId.K2700P.ToString(), (0, 0, "192.168.0.252", 1234) },
        { InstrumentId.K6510.ToString(), (0, 0, "192.168.0.154", 5025) },
        { InstrumentId.K7510.ToString(), (0, 0, "192.168.0.144", 5025) }
        };
        CancellationTokenSource = new ();
    }

    /// <summary>   Gets or sets information describing the query. </summary>
    /// <value> Information describing the query. </value>
    public static string QueryInfo { get; set; }

    /// <summary>   Queries the identity. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="instrumentId">         Identifier for the instrument. </param>
    /// <param name="connectionTimeout">    The connection timeout. </param>
    /// <returns>   The identity. </returns>
    public static string QueryIdentity( InstrumentId instrumentId, TimeSpan connectionTimeout, bool useAsync )
    {

        string command = "*IDN?";

        string instrument = instrumentId == InstrumentId.None ? "Echo" : instrumentId.ToString();
        TimeSpan readAfterWriteDelay = TimeSpan.FromMilliseconds( _instrumentInfo[instrument].ReadAfterWriteDelay );
        int interQueryDelayMs = _instrumentInfo[instrument].InterQqueryDelay;
        string ipAddress = _instrumentInfo[instrument].IPAddress;
        int portNumber = _instrumentInfo[instrument].PortNumber;
        if ( !Paping( ipAddress , portNumber, (int)connectionTimeout.TotalMilliseconds) )
        {
            QueryInfo = $"Attempt to ping {instrument} at {ipAddress}:{portNumber} aborted after {connectionTimeout.TotalMilliseconds:0}ms";
            return QueryInfo + "\n";
        }

        QueryInfo = $"{instrument} Delays: Read: {readAfterWriteDelay.TotalMilliseconds:0}ms; Write: {interQueryDelayMs}ms";

        using TcpSession session = new( ipAddress, portNumber );
        session.Connect();

        if ( !session.Connected )
        {
            throw new InvalidOperationException( $"Connection failed at {ipAddress}:{portNumber}" );
        }

        try
        {

            if ( portNumber == _prologixPortNo )
            {

                /* set auto read after write
                   Prologix GPIB-ETHERNET controller can be configured to automatically address
                   instruments to talk after sending them a command in order to read their response. The
                   feature called, Read-After-Write, saves the user from having to issue read commands
                   repeatedly. */

                command = "++auto 1";

                /* send the command, which may cause Query Unterminated because we are setting the device to talk
                   where there is nothing to talk. */

                session.WriteLine( command );

                // wait for the command to process.

                Thread.Sleep( _prologixWaitInterval );

                // disable front panel operation of the currently addressed instrument.

                session.WriteLine( "++llo" );

                Thread.Sleep( _prologixWaitInterval );

                /* clear errors if any so as to leave the instrument without errors.
                   here we add *OPC? to prevent the query unterminated error. */

                session.WriteLine( "*CLS; *OPC?" );
                Thread.Sleep( _prologixWaitInterval );
                string reply = string.Empty;
                int readCount = session.Read( 1024, ref reply, true );

                // note the the GPIB-Lan device appends CR and LF character and we are trimming only the LF.

                if ( 2 != readCount )
                {
                    throw new InvalidOperationException( "Operation completed reply of a single character is expected" );
                }

                reply.TrimEnd( '\r' );

                if ( !string.Equals( reply, "1"))
                {
                    throw new InvalidOperationException( "Operation completed reply is expected" );
                }

            }

            System.Text.StringBuilder builder = new();

            string identity = string.Empty;

            command = "*IDN?";

            if ( useAsync  )
            {

                string response = QueryDeviceAsync( session, command, 256, readAfterWriteDelay, true );
                _ = builder.Append( $"b: {response}\n" );

                if ( interQueryDelayMs > 0 ) System.Threading.Thread.Sleep( interQueryDelayMs );
                response = QueryDeviceAsync( session, command, 256, readAfterWriteDelay, true );
                _ = builder.Append( $"c: {response}\n" );

                if ( interQueryDelayMs > 0 ) System.Threading.Thread.Sleep( interQueryDelayMs );
                response = QueryDeviceAsync( session, command, 256, readAfterWriteDelay, true );
                _ = builder.Append( $"d: {response}\n" );
            }
            else
            {

                string response = QueryDevice( session, command, 256, true );
                _ = builder.Append( $"b: {response}\n" );

                if ( interQueryDelayMs > 0 ) System.Threading.Thread.Sleep( interQueryDelayMs );
                response = QueryDevice( session, command, 256, true );
                _ = builder.Append( $"c: {response}\n" );

                if ( interQueryDelayMs > 0 ) System.Threading.Thread.Sleep( interQueryDelayMs );
                response = QueryDevice( session, command, 256, true );
                _ = builder.Append( $"d: {response}\n" );
            }

            return builder.ToString();

        }
        catch ( Exception )
        {

            throw;
        }
        finally
        {
            if ( session.Connected )
            {

                /* clear errors if any so as to leave the instrument without errors.
                   here we add *OPC? to prevent the query unterminated error. */
                session.WriteLine( "*CLS; *OPC?" );
                Thread.Sleep( _prologixWaitInterval );
                string reply = string.Empty;
                _ = session.Read( 1024, ref reply, true );

                if ( portNumber == _prologixPortNo )
                {

                    // enable front panel operation of the currently addressed instrument.

                    _ = session.WriteLine( "++loc" );

                    Thread.Sleep( _prologixWaitInterval );

                    //  send the command to set the interface to listen with read after write set to false.

                    _ = session.WriteLine( "++auto 0" );

                    Thread.Sleep( _prologixWaitInterval );

                }

                session.Disconnect();

            }

        }

    }

    /// <summary>   Queries identity asynchronously. </summary>
    /// <remarks>   2022-11-19. </remarks>
    /// <param name="instrumentId">         Identifier for the instrument. </param>
    /// <param name="connectionTimeout">    The connection timeout. </param>
    /// <returns>   The identity asynchronous. </returns>
    public static string QueryIdentityAsync( InstrumentId instrumentId, TimeSpan connectionTimeout )
    {

        string command = "*IDN?";
        string instrument = instrumentId == InstrumentId.None ? "Echo" : instrumentId.ToString();
        TimeSpan readAfterWriteDelay = TimeSpan.FromMilliseconds( _instrumentInfo[instrument].ReadAfterWriteDelay );
        int interqueryDelayMs = _instrumentInfo[instrument].InterQqueryDelay;
        string ipAddress = _instrumentInfo[instrument].IPAddress;
        int portNumber = _instrumentInfo[instrument].PortNumber;

        if ( !Paping( ipAddress, portNumber, ( int ) connectionTimeout.TotalMilliseconds ) )
        {
            QueryInfo = $"Attempt to connect to {instrument} at {ipAddress}:{portNumber} aborted after {connectionTimeout.TotalMilliseconds:0}ms";
            return QueryInfo;
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

    /// <summary>   Queries device asynchronous. </summary>
    /// <remarks>   2023-05-31. </remarks>
    /// <param name="session">      The session. </param>
    /// <param name="command">      The command. </param>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="readDelay">    The read delay. </param>
    /// <param name="trimEnd">      True to trim end. </param>
    /// <returns>   The device asynchronous. </returns>
    private static string QueryDeviceAsync( TcpSession session, string command, int byteCount, TimeSpan readDelay, bool trimEnd )
    {
        Task<string>? task= null;
        try
        {
            task = session.QueryLineAsync( command, byteCount, readDelay, trimEnd, CancellationTokenSource );
            task.Wait( CancellationTokenSource.Token );
            return task.Result;
        }
        catch ( OperationCanceledException e )
        {
            if ( task is not null )
                Console.WriteLine( "{0}: The wait has been canceled. Task status: {1:G}",
                                  e.GetType().Name, task?.Status );
            Thread.Sleep( 100 );
            Console.WriteLine( "After sleeping, the task status:  {0:G}", task?.Status );
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
