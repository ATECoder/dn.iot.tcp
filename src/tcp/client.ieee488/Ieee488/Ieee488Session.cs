using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

using CommunityToolkit.Mvvm.ComponentModel;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   An ieee 488 session. </summary>
/// <remarks>   2023-08-12. </remarks>
public class Ieee488Session : ObservableObject, IConnectable
{

    #region " construction and cleanup "

    [SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>" )]
    private const int _gpibLanPortNumber = 1234;

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="exceptionTracer">          The exception tracer. </param>
    /// <param name="readTermination">          (Optional) The read termination. </param>
    /// <param name="writeTermination">         (Optional) The write termination. </param>
    /// <param name="readAfterWriteDelayMs">    (Optional) The read after write delay in
    ///                                         milliseconds. </param>
    public Ieee488Session( TcpSession tcpSession, IExceptionTracer exceptionTracer,
                             char readTermination = '\n', char writeTermination = '\n',
                             int readAfterWriteDelayMs = 5 )
    {
        this.TcpSession = tcpSession;
        this.ReadTermination = readTermination;
        this.WriteTermination = writeTermination;
        this.ExceptionTracer = exceptionTracer;
        this.ReadAfterWriteDelay = readAfterWriteDelayMs;

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
                this.TcpSession.ConnectionChanged -= this.TcpSession_ConnectionChanged;
                this.TcpSession.ConnectionChanging -= this.TcpSession_ConnectionChanging;
            }
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

    private char _readTermination;
    /// <summary>   Gets or sets the read termination. </summary>
    /// <value> The read termination. </value>
    public char ReadTermination
    {
        get => this._readTermination;
        set => _ = this.SetProperty( ref this._readTermination, value );
    }

    private char _writeTermination;
    /// <summary>   Gets or sets the write termination. </summary>
    /// <value> The write termination. </value>
    public char WriteTermination
    {
        get => this._writeTermination;
        set => _ = this.SetProperty( ref this._writeTermination, value );
    }

    private int _readAfterWriteDelay;
    /// <summary>   Gets or sets the read after write delay. </summary>
    /// <value> The read after write delay. </value>
    public int ReadAfterWriteDelay
    {
        get => this._readAfterWriteDelay;
        set => _ = this.SetProperty( ref this._readAfterWriteDelay, value );
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
        throw new NotImplementedException();
    }

    /// <summary>   Closes the connection. </summary>
    /// <remarks>   2023-08-12. </remarks>
    public void Disconnect()
    {
        throw new NotImplementedException();
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
        }
        catch ( Exception ex )
        {
            this.ExceptionTracer?.Trace( ex );
        }

    }

    #endregion

}
