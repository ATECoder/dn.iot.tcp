using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace cc.isr.Iot.Tcp.Client;

/// <summary>   A TCP session. </summary>
/// <remarks>   2022-11-14. </remarks>
public partial class TcpSession : ObservableObject, IDisposable
{

    #region " construction and cleanup "

    /// <summary>   Constructor. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="ipv4Address">  IPv4 Address in string format. </param>
    /// <param name="portNumber">   (Optional) The port number. </param>
    public TcpSession( string ipv4Address, int portNumber = 5025 )
    {
        this._portNumber = portNumber;
        this._iPv4Address = ipv4Address;
        this._readTermination = "\n";
        this._writeTermination = "\n";
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.
    /// </summary>
    /// <remarks>   2022-11-14. </remarks>
    public void Dispose()
    {
        this.Dispose( true );
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.
    /// </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="disposing">    True to release both managed and unmanaged resources; false to
    ///                             release only unmanaged resources. </param>
    private void Dispose( bool disposing )
    {
        if ( disposing )
        {
            this._tcpClient?.Dispose();
        }
    }

    #endregion

    #region " tcp client and stream "

    private TcpClient? _tcpClient;
    private NetworkStream? _netStream;

    /// <summary>   Gets or sets the port number. </summary>
    /// <value> The port number. </value>
    [ObservableProperty]
    private int _portNumber;

    /// <summary>   Gets or sets the IPv4 address. </summary>
    /// <value> The IP address. </value>
    [ObservableProperty]
    private string _iPv4Address; 

    /// <summary>   Gets or sets the receive timeout. </summary>
    /// <remarks> Default value = 0 ms.</remarks>
    /// <value> The receive timeout. </value>
    public TimeSpan? ReceiveTimeout
    {
        get => this._tcpClient is not null ? TimeSpan.FromMilliseconds( this._tcpClient.ReceiveTimeout ) : null;
        set {
            if ( value is not null && this._tcpClient is not null )
            {
                _= this.SetProperty( this.ReceiveTimeout, value,
                                     this._tcpClient, ( model, value ) => model.ReceiveTimeout = value!.Value.Milliseconds );
            }
        }
    }

    /// <summary>   Gets or sets the send timeout. </summary>
    /// <remarks> Default value = 0 ms.</remarks>
    /// <value> The send timeout. </value>
    public TimeSpan? SendTimeout
    {
        get => this._tcpClient is not null ? TimeSpan.FromMilliseconds( this._tcpClient.SendTimeout ) : null ;
        set {
            if ( value is not null && this._tcpClient is not null )
            {
                _ = this.SetProperty( this.SendTimeout, value,
                                  this._tcpClient, ( model, value ) => model.SendTimeout = value!.Value.Milliseconds );
            }
        }
    }

    /// <summary>   Gets or sets the size of the receive buffer. </summary>
    /// <remarks> Default value = 65536 </remarks>
    /// <value> The size of the receive buffer. </value>
    public int? ReceiveBufferSize
    {
        get => this._tcpClient?.ReceiveBufferSize;
        set {
            if ( value is not null && this._tcpClient is not null )
            {
                _ = this.SetProperty( this.ReceiveBufferSize, value,
                                      this._tcpClient, ( model, value ) => model.ReceiveBufferSize = value!.Value );
            }
        }
    }

    /// <summary>   Opens a new session. </summary>
    /// <remarks>   2022-11-14. </remarks>
    [RelayCommand( CanExecute = nameof(this.CanConnect )) ]
    public void Connect()
    {
        this._tcpClient = new TcpClient( this.IPv4Address, this.PortNumber );

        // notice that the Tcp Client is connected to the end point at this point
        // even though the .Connect command was not issued.

        this._netStream = this._tcpClient.GetStream();
    }

    /// <summary>   Determines if we can connect. </summary>
    /// <remarks>   2022-11-18. </remarks>
    /// <returns>   True if we can connect, false if not. </returns>
    public bool CanConnect => this._tcpClient is null || this._netStream is null
                              || this._tcpClient.Client is null || ! this.Connected;

    /// <summary>   Gets the connected status of the session after the last I/O. </summary>
    /// <remarks>
    /// 2023-07-13. <para>
    /// The Connected property gets the connection state of the Client socket as of the last I/O
    /// operation. When it returns false, the Client socket was either never connected, or is no
    /// longer connected. Because the Connected property only reflects the state of the connection as
    /// of the most recent operation, you should attempt to send or receive a message to determine
    /// the current state. After the message send fails, this property no longer returns true. Note
    /// that this behavior is by design. </para>
    /// </remarks>
    /// <returns>   True if it connected; otherwise, false. </returns>
    public bool Connected => this._tcpClient?.Connected ?? false;

    /// <summary>   Asynchronously start a connection process. </summary>
    /// <remarks>   2022-11-18. </remarks>
    /// <returns>   An IAsyncResult. </returns>
    [RelayCommand( CanExecute = nameof( CanConnect ) )]
    public async Task< IAsyncResult > AsyncBeginConnect( )
    {
        this._tcpClient = new TcpClient();
        return await Task<IAsyncResult>.Run( () => this.BeginConnect() );
    }

    /// <summary>   Begins a connection process. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <returns>   An IAsyncResult. </returns>
    public IAsyncResult BeginConnect()
    {
        this._tcpClient = new TcpClient( );
        return  this._tcpClient.BeginConnect( this.IPv4Address, this.PortNumber, this.OnConnected, null );
    }

    /// <summary>   Await connect. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="asyncResult">  The result of the asynchronous operation. </param>
    /// <param name="timeout">      The timeout. </param>
    /// <returns>   True if it succeeds, false if it fails. </returns>
    public bool AwaitConnect( IAsyncResult asyncResult, TimeSpan timeout )
    {
        var success = asyncResult.AsyncWaitHandle.WaitOne( timeout );
        if ( !success )
        {
            this._tcpClient?.Close();
            this._tcpClient?.EndConnect( asyncResult );
        }
        return success;
    }

    /// <summary>   Asynchronous callback, called on completion of on connected. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="asyncResult">  The result of the asynchronous operation. </param>
    private void OnConnected( IAsyncResult asyncResult  )
    {
        if ( asyncResult.IsCompleted )
        {
            this._netStream = this._tcpClient!.GetStream();
        }
    }

    /// <summary>
    /// Opens a new session and reads the instrument identity to the debug console.
    /// </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="echoIdentity"> True to read the identity from the instrument. </param>
    /// <param name="queryMessage"> Message describing the query. </param>
    public void Connect( bool echoIdentity, string queryMessage )
    {
        this.Connect();
        if ( echoIdentity )
        {
            string identity = "";
            _ = this.QueryLine( queryMessage, 128, ref identity, false);
            System.Diagnostics.Debug.WriteLine( identity );
        }
    }

    /// <summary>
    /// Opens a new session and returns the instrument identity.
    /// </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="echoIdentity"> True to read the identity from the instrument. </param>
    /// <param name="queryMessage"> Message describing the query. </param>
    /// <param name="identity">     [in,out] The identity text. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    public void Connect( bool echoIdentity, string queryMessage, ref string identity, bool trimEnd )
    {
        this.Connect();
        if ( echoIdentity )
        {
            _ = this.QueryLine( queryMessage, 128, ref identity, trimEnd );
        }
    }

    /// <summary>   Disconnects this object. </summary>
    /// <remarks>   2022-11-14. </remarks>
    [RelayCommand( CanExecute = nameof( CanDisconnect ) )]
    public void Disconnect()
    {
        this._netStream?.Close();
        this._tcpClient?.Close();
    }

    /// <summary>   Determine if we can disconnect. </summary>
    /// <remarks>   2022-11-18. </remarks>
    /// <returns>   True if we can disconnect, false if not. </returns>
    public bool CanDisconnect()
    {
        return this._tcpClient is not null && this._netStream is not null && this._tcpClient.Client is not null;
    }

    /// <summary>   Flushes the TCP Stream. </summary>
    /// <remarks>   2022-11-14. </remarks>
    [RelayCommand( CanExecute = nameof( CanDisconnect ) )]
    public void Flush()
    {
        this._netStream?.Flush();
    }

    #endregion

    #region " i/o "

    /// <summary>   Gets or sets the read termination. </summary>
    /// <value> The read termination. </value>
    [ObservableProperty]
    private string _readTermination;

    /// <summary>   Gets or sets the write termination. </summary>
    /// <value> The write termination. </value>
    [ObservableProperty]
    private string _writeTermination;

    /// <summary>   Builds a reply. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="buffer">           The buffer. </param>
    /// <param name="receivedCount">    Number of received. </param>
    /// <param name="trimEnd">          True to trim the <see cref="_readTermination"/>. </param>
    /// <returns>   A string. </returns>
    private string BuildReply( byte[] buffer, int receivedCount, bool trimEnd )
    {
        int replyLength = receivedCount - (trimEnd ? this.ReadTermination.Length : 0);
        return replyLength > 0
            ? Encoding.ASCII.GetString( buffer, 0, replyLength )
            : string.Empty;
    }

    #endregion

    #region " synchronous i/o "

    /// <summary>
    /// Get a value indicating if data was received from the network and is available to be read.
    /// </summary>
    /// <remarks>   2022-11-15. </remarks>
    /// <returns>   True if data is available; otherwise, false . </returns>
    public bool QueryDataAvailable()
    {
        return this._netStream is not null && this._netStream.DataAvailable;
    }

    /// <summary>   Sends a message. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="message">  The message. </param>
    /// <returns>   The number of sent characters. </returns>
    public int Write( string message )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;
        byte[] buffer = Encoding.ASCII.GetBytes( message );
        this._netStream?.Write( buffer, 0, buffer.Length );
        return buffer.Length;
    }

    /// <summary>   Sends a message with <see cref="WriteTermination"/>. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="message">  The message. </param>
    /// <returns>   The number of sent characters. </returns>
    public int WriteLine( string message )
    {
        return string.IsNullOrEmpty( message ) ? 0 : this.Write( $"{message}{this.WriteTermination}" );
    }

    /// <summary>   Reads the reply as a string. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="reply">        [in,out] The reply. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <returns>   The number of received characters. </returns>
    public int Read( int byteCount, ref string reply, bool trimEnd )
    {
        byte[] buffer = new byte[byteCount];
        int receivedCount = 0;
        if ( this._netStream is not null )
        {
            receivedCount = this._netStream.Read( buffer, 0, byteCount );
            reply = this.BuildReply( buffer, receivedCount, trimEnd );
        }
        return receivedCount;
    }

    /// <summary>   Sends a query message and reads the reply as a string. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="message">      The message. </param>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="reply">        [in,out] The reply. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <returns>   the number of received characters. </returns>
    public int Query( string message, int byteCount, ref string reply, bool trimEnd )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;
        _ = this.Write( message );
        return this.Read( byteCount, ref reply, trimEnd );
    }

    /// <summary>   Sends a query message with termination and reads the reply as a string. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="message">      The message. </param>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="reply">        [in,out] The reply. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <returns>   The number of received characters. </returns>
    public int QueryLine( string message, int byteCount, ref string reply, bool trimEnd )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;
        _ = this.WriteLine( message );
        return this.Read( byteCount, ref reply, trimEnd );
    }

    /// <summary>   Read single-precision values. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="offset">   The offset into the received bytes. </param>
    /// <param name="count">    Number of single precision values. </param>
    /// <param name="values">   [in,out] the single precision values. </param>
    /// <returns>   The number of received bytes. </returns>
    public int Read( int offset, int count, ref float[] values )
    {
        byte[] buffer = new byte[count * 4 + offset + 1];
        int receivedCount = 0;
        if ( this._netStream is not null )
        {
            receivedCount = this._netStream.Read( buffer, 0, buffer.Length );

            // Need to convert to the byte array into single

            Buffer.BlockCopy( buffer, offset, values, 0, values.Length * 4 );
        }
        return receivedCount;
    }

    /// <summary>   Sends a query message and reads the reply as a single-precision values. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="message">  The message. </param>
    /// <param name="offset">   The offset into the received bytes. </param>
    /// <param name="count">    Number of single precision values. </param>
    /// <param name="values">   [in,out] the single precision values. </param>
    /// <returns>   The number of received bytes. </returns>
    public int Query( string message, int offset, int count, ref float[] values )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;
        _ = this.Write( message );
        return this.Read( offset, count, ref values );
    }

    /// <summary>   Sends a query message with termination and reads the reply as a single-precision values. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="message">  The message. </param>
    /// <param name="offset">   The offset into the received bytes. </param>
    /// <param name="count">    Number of single precision values. </param>
    /// <param name="values">   [in,out] the single precision values. </param>
    /// <returns>   The number of received bytes. </returns>
    public int QueryLine( string message, int offset, int count, ref float[] values )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;
        _ = this.WriteLine( message );
        return this.Read( offset, count, ref values );
    }

    #endregion

    #region " asynchronous i/o "

    /// <summary>   Query if data was received from the network and is available to be read. </summary>
    /// <remarks>   2022-11-04. </remarks>
    /// <param name="timeout">  The timeout. </param>
    /// <returns>   True if data is available; otherwise, false . </returns>
    public async Task<bool> StartQueryDataAvailable( TimeSpan timeout, CancellationToken token )
    {
        return await Task.Run( () => this.QueryDataAvailable( timeout ), token );
    }

    /// <summary>
    /// Query if data was received from the network and is available to be read during the <paramref name="timeout"/>
    /// period.
    /// </summary>
    /// <remarks>   2022-11-04. The cancellation token was used here because
    /// the task calling this method often fails to complete. </remarks>
    /// <param name="timeout">  The timeout. </param>
    /// <param name="token">    The cancellation token. </param>
    /// <returns>   True if data is available; otherwise, false . </returns>
    public bool QueryDataAvailable( TimeSpan timeout)
    {
        if ( timeout == TimeSpan.Zero || this.QueryDataAvailable() ) return true;
        DateTime endTime = DateTime.Now.Add( timeout );
        bool done;
        do
        {
            System.Threading.Thread.Sleep( 1 );
            done = DateTime.Now > endTime || this.QueryDataAvailable();
        } while ( !done );
        return this.QueryDataAvailable();
    }

    /// <summary>   Read asynchronously until no characters are available in the stream. </summary>
    /// <remarks>   2022-11-15. </remarks>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <param name="ct">           A token that allows processing to be canceled. </param>
    /// <returns>   A reply. </returns>
    public async Task<string> ReadWhileAvailableAsync( int byteCount, bool trimEnd, CancellationToken ct )
    {
        StringBuilder sb = new();
        int replyLength = 0;
        if ( this._netStream is not null )
        {
            while ( this._netStream.DataAvailable )
            {
                var buffer = new byte[byteCount];
                int receivedCount = await this._netStream.ReadAsync( buffer, 0, byteCount, ct );
                if ( receivedCount > 0 ) _ = sb.Append( Encoding.ASCII.GetString( buffer, 0, receivedCount ) );
            }
            replyLength = sb.Length - (trimEnd ? this.ReadTermination.Length : 0);
        }
        return replyLength > 0
            ? sb.ToString( 0, replyLength )
            : String.Empty;
    }

    /// <summary>
    /// Read asynchronously data that was already received from the network and is available to be
    /// read.
    /// </summary>
    /// <remarks>   2022-11-15. </remarks>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <param name="ct">           A token that allows processing to be canceled. </param>
    /// <returns>   A reply. </returns>
    public async Task<string> ReadAsync( int byteCount, bool trimEnd, CancellationToken ct )
    {
        var buffer = new byte[byteCount];
        if ( this._netStream is not null )
        {
            int receivedCount = await this._netStream.ReadAsync( buffer, 0, byteCount, ct );
            return this.BuildReply( buffer, receivedCount, trimEnd );
        }
        return string.Empty;
    }

    /// <summary>   Sends a message asynchronously reading any existing data into the orphan . </summary>
    /// <remarks>   2022-11-15. </remarks>
    /// <param name="message">  The message. </param>
    /// <param name="ct">       A token that allows processing to be canceled. </param>
    /// <returns>   The number of sent characters. </returns>
    public async Task<int> WriteAsync( string message, CancellationToken ct )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;

        if ( this._netStream is not null )
        {
            // read any data already in the stream.

            this.Leftover = this._netStream.DataAvailable
                ? await this.ReadWhileAvailableAsync( 2048, false, ct )
                : string.Empty;

            byte[] buffer = Encoding.ASCII.GetBytes( message );
            await this._netStream.WriteAsync( buffer, 0, buffer.Length, ct );
            return buffer.Length;
        }
        return 0;
    }

    /// <summary>   Sends a message with termination asynchronously reading any existing data into the orphan . </summary>
    /// <remarks>   2022-11-15. </remarks>
    /// <param name="message">  The message. </param>
    /// <param name="ct">       A token that allows processing to be canceled. </param>
    /// <returns>   The number of sent characters. </returns>
    public async Task<int> WriteLineAsync( string message, CancellationToken ct )
    {
        return string.IsNullOrEmpty( message ) ? 0 : await this.WriteAsync( $"{message}{this.WriteTermination}" , ct );
    }

    /// <summary>   Sends a query message with termination and reads the reply as a string. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <exception cref="TimeoutException"> Thrown when a Timeout error condition occurs. </exception>
    /// <param name="message">      The message. </param>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="readDelay">    The read delay. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <param name="tokenSource">  A token that allows processing to be canceled. </param>
    /// <returns>   A reply. </returns>
    public async Task<string> QueryAsync( string message, int byteCount, TimeSpan readDelay, bool trimEnd, CancellationTokenSource tokenSource )
    {
        if ( string.IsNullOrEmpty( message ) ) return string.Empty;

        var sendTask = this.WriteAsync( message, tokenSource.Token ).ConfigureAwait( false );
        _ = await sendTask;

        // wait for available data.
        // a read delay of 1ms is required for Maui, WPF and windows forms applications.

        readDelay = TimeSpan.FromMilliseconds( Math.Max( 1, readDelay.TotalMilliseconds ) );
        var dataAvailableTask = Task.Run( () => this.QueryDataAvailable( readDelay ), tokenSource.Token );

        // two checks on data available are needed for some reason. 
        _ = dataAvailableTask.Wait( readDelay );

        var completed = dataAvailableTask.Wait( readDelay );
        if ( !completed ) tokenSource.Cancel();
        var hasData = this.QueryDataAvailable();

        // this delay is required for the MAUI application: not tested for WPF or Windows Forms.
        // if the above delay is not used.
        // System.Threading.Thread.Sleep( 1 );

        // we ignore the delay task result in order to simplify the code as this
        // would return no data if the stream has no available data.

        return await this.ReadAsync( byteCount, trimEnd, tokenSource.Token );
    }

    /// <summary>   Queries line asynchronous. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="message">      The message. </param>
    /// <param name="byteCount">    Number of bytes. </param>
    /// <param name="readDelay">    The read delay. </param>
    /// <param name="trimEnd">      True to trim the <see cref="_readTermination"/>. </param>
    /// <param name="tokenSource">  A token that allows processing to be canceled. </param>
    /// <returns>   A reply. </returns>
    public async Task<string> QueryLineAsync( string message, int byteCount, TimeSpan readDelay, bool trimEnd, CancellationTokenSource tokenSource )
    {
        return string.IsNullOrEmpty( message )
            ? string.Empty
            : await this.QueryAsync( $"{message}{this.WriteTermination}", byteCount, readDelay, trimEnd, tokenSource );
    }

    /// <summary>   Gets the last leftover response. </summary>
    /// <value> Any leftover message in the stream. </value>
    public string? Leftover { get; private set; }

    #endregion

    #region " listeners "

    /// <summary>   Enumerates the listeners in this collection. </summary>
    /// <remarks>   2023-08-10. 
    /// This does finds neither Prologix or LXI instruments. </remarks>
    /// <param name="portToCheck">  The port to check. </param>
    /// <returns>
    /// An enumerator that allows foreach to be used to process the listeners in this collection.
    /// </returns>
    public static IEnumerable<IPEndPoint> EnumerateListeners( int portToCheck )
    {

        IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] activeListeners = ipProperties.GetActiveTcpListeners();
        activeListeners = ipProperties.GetActiveUdpListeners();
        List<IPEndPoint> portListeners = new();
        foreach ( var listener in activeListeners )
        {
            if ( listener.Port == portToCheck )
            {
                portListeners.Add( listener );
                Console.WriteLine( $"Server is listening on port {listener.Port}" );
                Console.WriteLine( $"Local address: {listener.Address}" );
                Console.WriteLine( $"State: {listener.AddressFamily}" );
                Console.WriteLine();
            }
        }
        return portListeners;

    }

    #endregion

}

