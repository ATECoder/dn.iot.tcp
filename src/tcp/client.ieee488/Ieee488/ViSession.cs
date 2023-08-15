using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Mvvm.ComponentModel;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A VI session. </summary>
/// <remarks>   2023-08-12. </remarks>
public partial class ViSession : ObservableObject, IConnectable
{

    #region " construction and cleanup "

    /// <summary>   (Immutable) the gpib LAN port number. </summary>
    private const int _gpibLanPortNumber = 1234;

    /// <summary>   (Immutable) the read after write delay default. </summary>
    private const int _readAfterWriteDelayDefault = 5;

    /// <summary>   (Immutable) the session read timeout default. </summary>
    [SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>" )]
    private const int _sessionReadTimeoutDefault = 3000;

    /// <summary>   (Immutable) the socket receive timeout default. </summary>
    [SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>" )]
    private const int _socketReceiveTimeoutDefault = 500;

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="exceptionTracer">          The exception tracer. </param>
    /// <param name="readTermination">           The read termination. </param>
    /// <param name="writeTermination">          The write termination. </param>
    /// <param name="readAfterWriteDelayMs">     The read after write delay in
    ///                                         milliseconds. </param>
    public ViSession( TcpSession tcpSession, IExceptionTracer exceptionTracer,
                      char readTermination = '\n', char writeTermination = '\n',
                      int readAfterWriteDelayMs = _readAfterWriteDelayDefault )
    {
        this.TcpSession = tcpSession;
        this.ReadTermination = readTermination;
        this.WriteTermination = writeTermination;
        this.ExceptionTracer = exceptionTracer;
        this.ReadAfterWriteDelay = readAfterWriteDelayMs;
        this.Init(tcpSession, exceptionTracer);
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="portNumber">    The port number. </param>
    public ViSession( string ipv4Address, int portNumber = 5025 ) : this( new TcpSession( ipv4Address, portNumber ),
            new DebugExceptionTracer() )
    { }

    /// <summary>   Initializes this object. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="tcpSession">       The TCP client session. </param>
    /// <param name="exceptionTracer">  The exception tracer. </param>
    [MemberNotNull( nameof( GpibLan ) )]
    private void Init( TcpSession tcpSession, IExceptionTracer exceptionTracer )
    {
        this.GpibLan = new GpibLanController( tcpSession, exceptionTracer );
        if ( this.TcpSession is not null )
        {
            this.TcpSession.ConnectionChanged += this.TcpSession_ConnectionChanged;
            this.TcpSession.ConnectionChanging += this.TcpSession_ConnectionChanging;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose( true );
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.
    /// </summary>
    /// <param name="disposing">    True to release both managed and unmanaged resources; false to
    ///                             release only unmanaged resources. </param>
    private void Dispose( bool disposing )
    {
        if ( disposing )
        {

            if ( this.TcpSession is not null )
            {
                if ( this.TcpSession.Connected )
                {
                    this.TcpSession.Disconnect();
                }
                this.TcpSession.ConnectionChanged -= this.TcpSession_ConnectionChanged;
                this.TcpSession.ConnectionChanging -= this.TcpSession_ConnectionChanging;
            }

            this.GpibLan?.Dispose();
            this.GpibLan = null;

            this.TcpSession?.Dispose();
            this.TcpSession = null;

            this.ExceptionTracer = null;
        }
    }

    #endregion

    #region " tcp client "

    /// <summary>   Gets or sets the TCP client session. </summary>
    /// <value> The TCP client session. </value>
    private TcpSession? TcpSession { get; set; }

    /// <summary>   Gets the socket address. </summary>
    /// <value> The socket address. </value>
    public string? SocketAddress => this.TcpSession?.SocketAddress;

    /// <summary>   Gets or sets the read termination. </summary>
    /// <value> The read termination. </value>
    [ObservableProperty]
    private char _readTermination;

    /// <summary>   Gets or sets the write termination. </summary>
    /// <value> The write termination. </value>
    [ObservableProperty]
    private char _writeTermination;

    /// <summary>   Gets or sets the read after write delay. </summary>
    /// <value> The read after write delay. </value>
    [ObservableProperty]
    private int _readAfterWriteDelay;

    /// <summary>   Gets or sets the receive timeout. </summary>
    /// <remarks> Default value = 0 ms.</remarks>
    /// <value> The receive timeout. </value>
    public TimeSpan? ReceiveTimeout
    {
        get => this.TcpSession?.ReceiveTimeout;
        set {
            if ( value is not null && this.TcpSession is not null )
            {
                _ = this.SetProperty( this.ReceiveTimeout, value,
                                     this.TcpSession, ( model, value ) => model.ReceiveTimeout = value!.Value );
            }
        }
    }

    /// <summary>   Gets the Session Read Timeout. </summary>
    [ObservableProperty]
    private int _sessionReadTimeout;

    #endregion

    #region " gpib lan controller "

    /// <summary>   Gets or sets the gpib LAN. </summary>
    /// <value> The gpib LAN. </value>
    [ObservableProperty]
    private GpibLanController? _gpibLan;

    /// <summary>   Returns true if the TCP Connection uses the Prologix
    /// GPIB-Lan interface device. </summary>
    /// <value>   [Boolean] True if using the Prologix GPIB-Lan interface device. </value>
    [ObservableProperty]
    private bool _usingGpibLan;

    #endregion

    #region " device i/o "

    /// <summary>   Sends a message. </summary>
    /// <remarks>
    /// if (using the Prologix device at port 1234, this method first sets the Prologix to auto off
    /// (++auto 0) to prevent it from setting the device to talk prematurely which might cause the
    /// device (e.g., the Keithley 2700 scanning multimeter) to issue error -420 Query Unterminated.
    /// </remarks>
    /// <param name="message">              The message to send to the instrument. </param>
    /// <param name="appendTermination">    (Optional) (true) True to append termination to the
    ///                                     message. </param>
    /// <returns>   [int] The number of sent characters. </returns>
    public int WriteLine( string message , bool appendTermination = true )
    {
        int reply = 0;
        if ( this.UsingGpibLan )
            reply = this.GpibLan?.SendToDevice( message, appendTermination ) ?? 0;
        else if (this.TcpSession is not null )
        {
            if ( appendTermination ) message += this.WriteTermination;
            reply = this.TcpSession.Write( message );
            _ = TimeSpan.FromMilliseconds(this.ReadAfterWriteDelay).SyncWait();
        }
        return reply;
    }

    /// <summary>
    /// Receives a message from the server until reaching the specified termination, reading the
    /// specified number of characters, or timeout.
    /// </summary>
    /// <remarks>
    /// if (using a GPIB-Lan device, such as the Prologix GPIB-Lan interface, this method first uses
    /// the device <c>++read</c> to read the instrument.
    /// </remarks>
    /// <param name="timeout">        time to wait for reply in milliseconds. </param>
    /// <param name="maxLength">      [Optional, int, 32767]  The maximum number of bytes
    ///                                 to read. </param>
    /// <param name="trimEnd">        (true)  true to return the
    ///                                 string without the termination. </param>
    /// <param name="doEventsAction">   The do events action. </param>
    /// <returns>   The reading. </returns>
    public string AwaitReading( int timeout, int maxLength = 0x7FFF, bool trimEnd = true, Action? doEventsAction = null ) 
    {
        string reading = string.Empty;
        try
        {
            Stopwatch stopper = Stopwatch.StartNew();

            // wait for data or timeout
            while ( this.TcpSession is not null && (string.IsNullOrEmpty( reading ) || (stopper.ElapsedMilliseconds < timeout)) )
            {
                doEventsAction?.Invoke();

                // take a reading

                if ( this.UsingGpibLan )
                    reading = this.GpibLan?.ReceiveFromDevice( maxLength, trimEnd ) ?? string.Empty;
                else
                    _ = this.TcpSession.Read( maxLength, ref reading, trimEnd );
            }
        }
	    catch (Exception ex )
	    {
		    this.ExceptionTracer?.Trace(ex);
	    }
	    finally
	    {
        }
        return reading;
    }

    /// <summary>
    /// Receives a message from the server until reaching the specified termination, reading the
    /// specified number of characters, or <see cref="SessionReadTimeout">timeout</see>.
    /// </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <exception cref="TimeoutException"> Thrown when a Timeout error condition occurs. </exception>
    /// <param name="maxLength">      [Optional, int, 32767]  The maximum number of bytes
    ///                                 to read. </param>
    /// <param name="trimEnd">        (true)  true to return the
    ///                                 string without the termination. </param>
    /// <param name="doEventsAction">    The do events action. </param>
    /// <returns>   The received message. </returns>
    public string Read( int maxLength = 0x7FFF, bool trimEnd = true, Action? doEventsAction = null )
    {
        string reading = string.Empty;
        if ( this.SessionReadTimeout > 0 )

            reading = this.AwaitReading( this.SessionReadTimeout, maxLength, trimEnd, doEventsAction );

        else if ( this.TcpSession is not null )
        {
            if ( this.UsingGpibLan )

                reading = this.GpibLan?.ReceiveFromDevice( maxLength, trimEnd ) ?? string.Empty;

            else
                _ = this.TcpSession.Read( maxLength, ref reading, trimEnd);
        }

        // report an error on failure to read.
        return string.IsNullOrEmpty( reading )
            ? throw new TimeoutException( $"Data not received reading the instrument at {this.SocketAddress} with a timeout of {this.SessionReadTimeout}ms." )
            : reading;
    }

    /// <summary>   Sends a message and receives a reply. </summary>
    /// <param name="message">             The message to send to the instrument. </param>
    /// <param name="appendTermination">   (true) true to append termination to
    ///                                      the message. </param>
    /// <param name="maxLength">           [Optional, 32767] The maximum number of bytes to read. </param>
    /// <param name="trimEnd">             (true) true to return the string without the termination. </param>
    /// <returns>   The received string. </returns>
    public string QueryLine( string message, bool appendTermination = true, int maxLength = 0x7FFF, bool trimEnd = true )
    {
        return 0 < this.WriteLine( message, appendTermination )
            ? this.Read( maxLength, trimEnd )
            : string.Empty;
    }

#endregion

#region " connectable implementation "

    /// <summary>   Gets a reference to the connectable <see cref="TcpSession"/> object . </summary>
    /// <value>   [<see cref="IConnectable"/>]. </value>
    public IConnectable? Connectable => this.TcpSession;

    /// <summary>   Gets or sets a value indicating whether the connected. </summary>
    /// <value> True if connected, false if not. </value>
    public bool Connected => this.Connectable?.Connected ?? false;

    /// <summary>   Gets or sets a value indicating whether we can connect. </summary>
    /// <value> True if we can connect, false if not. </value>
    public bool CanConnect => this.Connectable?.CanConnect ?? false;

    /// <summary>   Gets or sets a value indicating whether we can disconnect. </summary>
    /// <value> True if we can disconnect, false if not. </value>
    public bool CanDisconnect => this.Connectable?.CanDisconnect ?? false;

    /// <summary>   Event queue for all listeners interested in ConnectionChanged events. </summary>
    public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

    /// <summary>   Raises the connection changed event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="e">    Event information to send to registered event handlers. </param>
    protected void OnConnectionChanged( ConnectionChangedEventArgs e )
    {
        var handler = this.ConnectionChanged;
        handler?.Invoke( this, e );
    }

    /// <summary>   Event queue for all listeners interested in ConnectionChanging events. </summary>
    public event EventHandler<ConnectionChangingEventArgs>? ConnectionChanging;

    /// <summary>   Raises the connection changing event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="e">    Event information to send to registered event handlers. </param>
    protected void OnConnectionChanging( ConnectionChangingEventArgs e )
    {
        var handler = this.ConnectionChanging;
        handler?.Invoke( this, e );
    }

    /// <summary>   Opens the connection. </summary>
    /// <remarks>   2023-08-12. </remarks>
    public void Connect()
    {
        ConnectionChangingEventArgs e = new();
        this.OnConnectionChanging( e );
        if ( !e.Cancel )
        {
            this.Connectable?.Connect();

            ConnectionChangedEventArgs args = new( this.Connected );
            this.OnConnectionChanged( args );
        }
    }

    /// <summary>   Closes the connection. </summary>
    /// <remarks>   2023-08-12. </remarks>
    public void Disconnect()
    {
        ConnectionChangingEventArgs e = new( this.Connected, false );
        this.OnConnectionChanging( e );
        if ( !e.Cancel )
        {
            this.Connectable?.Disconnect();
            ConnectionChangedEventArgs args = new( this.Connected );
            this.OnConnectionChanged( args );
        }
    }

    #endregion

    #region " tcp session event handlers "

    private IExceptionTracer? _exceptionTracer;
    /// <summary>   Gets or sets the exception tracer. </summary>
    /// <value> The exception tracer. </value>
    private IExceptionTracer? ExceptionTracer
    {
        get => this._exceptionTracer;
        set => _ = this.SetProperty( ref this._exceptionTracer, value );
    }

    /// <summary>   Handles the <see cref="TcpSession.ConnectionChanged"/> event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">  Reference to the <see cref="ConnectionChangedEventArgs"/> event
    ///                             arguments. </param>
    private void TcpSession_ConnectionChanged( object sender, ConnectionChangedEventArgs eventArgs )
    {

        if ( sender is null || eventArgs is null ) return;

        try
        {
        }
        catch ( Exception ex )
        {
            this.ExceptionTracer?.Trace( ex );
        }
    }

    /// <summary>   Handles the <see cref="TcpSession.ConnectionChanging"/> event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">  Reference to the <see cref="ConnectionChangingEventArgs"/> event
    ///                             arguments. </param>
    private void TcpSession_ConnectionChanging( object sender, ConnectionChangingEventArgs eventArgs )
    {
        if ( sender is null || eventArgs is null ) return;
        try
        {
            // enable the GPIB-Lan controller if the Tcp Session connects to the 
            // GPIB-Lan controller port
            if ( this.GpibLan is not null )
            {
                this.GpibLan.Enabled = _gpibLanPortNumber == (( TcpSession ) sender)?.PortNumber;
                this.UsingGpibLan = this.GpibLan.Enabled;
            }
        }
        catch ( Exception ex )
        {
            this.ExceptionTracer?.Trace( ex );
        }

    }

    #endregion

}
