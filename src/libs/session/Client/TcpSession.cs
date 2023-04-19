using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace cc.isr.Iot.Tcp.Client;

/// <summary>   A TCP session. </summary>
/// <remarks>   2022-11-14. </remarks>
public partial class TcpSession : ObservableObject, IDisposable
{

    /// <summary>   Constructor. </summary>
    /// <remarks>   2022-11-14. </remarks>
    /// <param name="ipv4Address">  IPv4 Address in string format. </param>
    /// <param name="portNumber">   (Optional) The port number. </param>
    public TcpSession( string ipv4Address, int portNumber = 5025 )
    {
        this.PortNumber = portNumber;
        this.IPv4Address = ipv4Address;
        this.ReadTermination = "\n";
        this.WriteTermination = "\n";
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
            this._tcpClient = null;
            this._netStream = null;
        }
    }

    #region " TCP Client and Stream "

    private TcpClient _tcpClient = null;
    private NetworkStream _netStream = null;

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
    public TimeSpan ReceiveTimeout
    {
        get => TimeSpan.FromMilliseconds( this._tcpClient.ReceiveTimeout );
        set => this.SetProperty( this._tcpClient.ReceiveTimeout , value.Milliseconds, this._tcpClient,
                               (model, value ) => model.ReceiveTimeout = value ) ;
    }

    /// <summary>   Gets or sets the send timeout. </summary>
    /// <remarks> Default value = 0 ms.</remarks>
    /// <value> The send timeout. </value>
    public TimeSpan SendTimeout
    {
        get => TimeSpan.FromMilliseconds( this._tcpClient.SendTimeout );
        set => this.SetProperty( this._tcpClient.SendTimeout, value.Milliseconds,
                                 this._tcpClient, ( model, value ) => model.SendTimeout = value );
    }

    /// <summary>   Gets or sets the size of the receive buffer. </summary>
    /// <remarks> Default value = 65536 </remarks>
    /// <value> The size of the receive buffer. </value>
    public int ReceiveBufferSize
    {
        get => this._tcpClient.ReceiveBufferSize;
        set => this.SetProperty( this._tcpClient.ReceiveBufferSize, value,
                                 this._tcpClient, ( model, value ) => model.ReceiveBufferSize = value);
    }

    /// <summary>   Opens a new session. </summary>
    /// <remarks>   2022-11-14. </remarks>
    [RelayCommand( CanExecute = nameof(this.CanConnect )) ]
    public void Connect()
    {
        this._tcpClient = new TcpClient( this.IPv4Address, this.PortNumber );
        // notices that the Tcp Client is connected to the end point at this point
        // even though the .Connect command was not issued.
        this._netStream = this._tcpClient.GetStream();
    }

    /// <summary>   Determine if we can connect. </summary>
    /// <remarks>   2022-11-18. </remarks>
    /// <returns>   True if we can connect, false if not. </returns>
    public bool CanConnect()
    {
        return this._tcpClient is null || this._netStream is null || this._tcpClient.Client is null;
    }

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
            this._tcpClient.Close();
            this._tcpClient.EndConnect( asyncResult );
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
            this._netStream = this._tcpClient.GetStream();
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
        this._netStream.Close();
        this._tcpClient.Close();
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
        this._netStream.Flush();
    }

    #endregion

    #region " I/O "

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
        int replyLength = receivedCount - (trimEnd ? this._readTermination.Length : 0);
        return replyLength > 0
            ? Encoding.ASCII.GetString( buffer, 0, replyLength )
            : string.Empty;
    }

    #endregion

    #region " SYNCHRONOUS I/O "

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
        this._netStream.Write( buffer, 0, buffer.Length );
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
        int receivedCount = this._netStream.Read( buffer, 0, byteCount );
        reply = this.BuildReply(buffer, receivedCount, trimEnd);    
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
        int receivedCount = this._netStream.Read( buffer, 0, buffer.Length );
        // Need to convert to the byte array into single
        Buffer.BlockCopy( buffer, offset, values, 0, values.Length * 4 );
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

    #region " ASYNCHRONOUS I/O "

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
        while ( this._netStream.DataAvailable )
        {
            var buffer = new byte[byteCount];
            int receivedCount = await this._netStream.ReadAsync( buffer, 0, byteCount, ct );
            if ( receivedCount > 0 ) _ = sb.Append( Encoding.ASCII.GetString( buffer, 0, receivedCount ) );
        }
        int replyLength = sb.Length - ( trimEnd ? this._readTermination.Length : 0 );
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
        int receivedCount = await this._netStream.ReadAsync( buffer, 0, byteCount, ct );
        return  this.BuildReply( buffer, receivedCount, trimEnd );
    }

    /// <summary>   Sends a message asynchronously reading any existing data into the orphan . </summary>
    /// <remarks>   2022-11-15. </remarks>
    /// <param name="message">  The message. </param>
    /// <param name="ct">       A token that allows processing to be canceled. </param>
    /// <returns>   The number of sent characters. </returns>
    public async Task<int> WriteAsync( string message, CancellationToken ct )
    {
        if ( string.IsNullOrEmpty( message ) ) return 0;

        // read any data already in the stream.
        this.Orphan = this._netStream.DataAvailable
            ? await this.ReadWhileAvailableAsync( 2048, false, ct )
            : string.Empty;

        byte[] buffer = Encoding.ASCII.GetBytes( message );
        await this._netStream.WriteAsync( buffer, 0, buffer.Length, ct );
        return buffer.Length;
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

        Task<int> sendTask = this.WriteAsync( message, tokenSource.Token );
        sendTask.Wait();
        if ( !sendTask.IsCompleted )
        {
            tokenSource.Cancel();
            throw new TimeoutException( $"{nameof( WriteAsync )} timed out" );
        }

        // wait for available data.
        // a read delay of 1ms is required for Maui, WPF and windows forms applications.
        readDelay = TimeSpan.FromMilliseconds( Math.Max( 1, readDelay.TotalMilliseconds ) );
        var dataAvailabelTask = Task.Run( () => this.QueryDataAvailable( readDelay ), tokenSource.Token );
        dataAvailabelTask.Wait( readDelay );
        var completed = dataAvailabelTask.Wait( readDelay );
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
    public string Orphan { get; private set; }

#endregion

}
