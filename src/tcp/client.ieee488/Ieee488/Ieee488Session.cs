using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Net.NetworkInformation;

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
        this._identity = string.Empty;
        this.ViSession = new ViSession( tcpSession, exceptionTracer, readTermination, writeTermination, readAfterWriteDelayMs );
        this.ReadTermination = readTermination;
        this.WriteTermination = writeTermination;
        this.ExceptionTracer = exceptionTracer;
        this.ReadAfterWriteDelay = readAfterWriteDelayMs;

        if ( this.ViSession is not null )
        {
            this.ViSession.ConnectionChanged += this.ViSession_ConnectionChanged;
            this.ViSession.ConnectionChanging += this.ViSession_ConnectionChanging;
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
            if ( this.ViSession is not null )
            {
                if ( this.ViSession.Connected ) { this.ViSession.Disconnect(); }
                this.ViSession.ConnectionChanged -= this.ViSession_ConnectionChanged;
                this.ViSession.ConnectionChanging -= this.ViSession_ConnectionChanging;
            }
            this.ViSession?.Dispose();

            this.ExceptionTracer = null;
        }
    }

    #endregion

    #region " VI Session "

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

    #region " i/o "

    /// <summary>   Query if 'status' is service request. </summary>
    /// <remarks>   2023-08-13. </remarks>
    /// <param name="status">           The status. </param>
    /// <param name="serviceRequests">  The service requests. </param>
    /// <returns>   True if service request, false if not. </returns>
    public bool IsServiceRequest( int status, ServiceRequests serviceRequests)
    {
        return ( int ) serviceRequests == (status & ( int ) serviceRequests);
    }

    /// <summary>   Sends a message. </summary>
    /// <param name="message">             The message to send to the instrument. </param>
    /// <param name="queryEAV">            (true) true to check the Error Available status bit
    ///                                      after sending the message if (<see cref="ViSession.UsingGpibLan"/> or VXI. </param>
    /// <param name="a_appendTermination">   (true) true to append termination to
    ///                                      the message. </param>
    /// <returns>   [Long] The number of sent characters. </returns>
    public int WriteLine( string message, bool queryEAV = true, bool a_appendTermination = true )
    {
        int reply = 0;
        if ( this.ViSession is not null )
        {

            reply = this.ViSession.WriteLine( message, a_appendTermination );

            if ( this.ViSession.GpibLan is not null && this.ViSession.UsingGpibLan && queryEAV)
    	    {

                // read the status byte and check for errors.

                int status = this.ViSession.GpibLan.SerialPoll();

                // check if we have an error.

                if ( this.IsServiceRequest( status, ServiceRequests.ErrorAvailable ) ) 
	        	{
                    // raise the write error adding some information

                    throw new InvalidOperationException(
                        $"SRQ=0x{status:x2}. Error Available (0x{ServiceRequests.ErrorAvailable:x2}) after sending {message} to the instrument at {this.ViSession.SocketAddress}." );
                }

            }
        }

        return reply;
    }

    /// <summary>
    /// Receives a message from the server until reaching the specified termination or reading the
    /// specified number of characters.
    /// </summary>
    /// <remarks>   2023-08-13. </remarks>
    /// <exception cref="TimeoutException"> Thrown when a Timeout error condition occurs. </exception>
    /// <param name="a_awaitMAV">       (true) (Optional) true to wait for the
    ///                                 Message Available status bit before reading if (<see cref="ViSession.UsingGpibLan"/>
    ///                                 or VXI. </param>
    /// <param name="a_maxLength">      (32767) (Optional) The maximum number of
    ///                                 bytes to read. </param>
    /// <param name="a_trimEnd">        (true) (Optional) true to return the
    ///                                 string without the termination. </param>
    /// <param name="doEventsAction">   (Optional) The do events action. </param>
    /// <returns>   The received message. </returns>
    public string Read( bool a_awaitMAV = true, int a_maxLength = 0x7FFF, bool a_trimEnd = true, Action? doEventsAction = null )
    {
        int p_statusByte = 0;
        string reply = string.Empty;
        if ( this.ViSession is not null )
        {

            if ( this.ViSession.GpibLan is not null && this.ViSession.UsingGpibLan && a_awaitMAV )
            {
                // wait for the message available bits.

                p_statusByte = this.ViSession.GpibLan.AwaitStatus( TimeSpan.FromMilliseconds( this.ViSession.SessionReadTimeout ),
                                                                   ( int ) ServiceRequests.MessageAvailable,
                                                                   doEventsAction:doEventsAction );
            }

            // either way, try reading the instrument

            reply = this.ViSession.SessionReadTimeout > 0
                    ? this.ViSession.AwaitReading( this.ViSession.SessionReadTimeout, a_maxLength, a_trimEnd )
                    : this.ViSession.Read( a_maxLength, a_trimEnd );

            // report an error on failure to read.
            if ( string.IsNullOrEmpty( reply ) )
            {

                // check if (message available.

                string errorMessage = this.IsServiceRequest( p_statusByte , ServiceRequests.MessageAvailable)
                    ? "No Message Available"
                    : "Data not received after message available";

                throw new TimeoutException(
                    $"SRQ=0x{p_statusByte:x2}. {errorMessage} (0x{ServiceRequests.MessageAvailable:x2}) reading the instrument at {this.ViSession.SocketAddress} with a timeout of {this.ViSession.SessionReadTimeout}ms." );
            }
        }
        return reply;
    }


    /// <summary>   Sends a message and receives a reply. </summary>
    /// <param name="a_message">             The message to send to the instrument. </param>
    /// <param name="a_queryEAV">            (true) true to check the Error Available status bit
    ///                                      after sending the message if (<see cref="ViSession.UsingGpibLan"/> or VXI. </param>
    /// <param name="a_awaitMAV">            (true) true to wait for the Message Available
    ///                                      status bit before reading if (<see cref="ViSession.UsingGpibLan"/> or VXI. </param>
    /// <param name="a_appendTermination">   (true) true to append termination to
    ///                                      the message. </param>
    /// <param name="a_maxLength">           (32767) The maximum number of bytes to read. </param>
    /// <param name="a_trimEnd">             (true) true to return the string without
    ///                                      the termination. </param>
    /// <returns>   The received message. </returns>
    public string QueryLine( string a_message, bool a_queryEAV = true, bool a_awaitMAV = true,
                             bool a_appendTermination = true, int a_maxLength = 0x7FFF, bool a_trimEnd = true )
    {
        return 0 < this.WriteLine( a_message, a_queryEAV, a_appendTermination )
            ? this.Read( a_awaitMAV, a_maxLength, a_trimEnd )
            : string.Empty;
    }

    #endregion

    #region " ieee488 commands "

    /// <summary>   Clears Status (CLS) command. </summary>
    /// <param name="a_awaitOpc">   [Optional, Boolean, true] true to wait for operation completion
    ///                             after issuing the <c>*CLS</c> command by querying <c>*CLS; *OPC?</c></param>
    public void ClearExecutionState( bool a_awaitOpc = true )
    {
        string p_command = Syntax.ClearExecutionStateCommand;
        if ( a_awaitOpc )
        {
            p_command = $"{p_command};{Syntax.OperationCompletedQueryCommand}";
            _ = this.QueryLine( p_command, false, true );
        }
        else
        {
            _ = this.WriteLine( p_command, false );
        }
    }

    private string _identity;
    /// <summary>   Returns the identity. </summary>
    /// <value>   [String]. </value>
    public string Identity
    {
        get
        {
            if ( String.IsNullOrEmpty( this._identity ) && (this.ViSession?.Connected ?? false) )
                this._identity = this.QueryIdentity();
            return this._identity;
        }
    }

    /// <summary>   Returns the instrument identity using the *IDN? query command. </summary>
    /// <returns>   [String]. </returns>
    public string QueryIdentity()
    { 
        this._identity = this.QueryLine( Syntax.IdentityQueryCommand);
        return this._identity;
    }

    /// <summary>   Operation Complete (*OPC) command. </summary>
    /// <returns>   [Long] The number of characters that were sent. </returns>
    public int OperationComplete()
    {
        return this.WriteLine( Syntax.OperationCompleteCommand );
    }

    /// <summary>   Returns '1' if (operation was completed; otherwise 0. </summary>
    /// <returns>   [String] 1 if (completed; otherwise 0. </returns>
    public string QueryOperationCompleted() 
    {
        return this.QueryLine( Syntax.OperationCompletedQueryCommand );
    }

    /// <summary>   Returns option (instrument specific). </summary>
    /// <returns>   [String]. </returns>
    public string QueryOptions()
    {   
        return this.QueryLine(Syntax.OptionsQueryCommand);
    }

    // <summary>   Returns the Service Request status byte using the *STB? query command. </summary>
    /// <returns>   The service request event register status value. </returns>
    public int QueryServiceRequestStatus()
    {
        string p_eventStatus = this.QueryLine( Syntax.ServiceRequestQueryCommand );
        return int.TryParse( p_eventStatus, out int value )
            ? value
            : 0;
    }

    /// <summary>   Reads the status byte. </summary>
    /// <param name="a_canQuery">   (false) true to send <c>*STB?</c>
    ///                             if (not <see cref="ViSession.UsingGpibLan"/>. </param>
    /// <returns>   The status byte. </returns>
    public int ReadStatusByte( bool a_canQuery = false )
    {
        // querying the service request status could cause
        // a query unterminated error.

        return this.ViSession?.UsingGpibLan ?? false
            ? this.ViSession?.GpibLan?.SerialPoll() ?? 0 
            : a_canQuery
                ? this.QueryServiceRequestStatus()
                : 0;
    }


    #endregion

    #region " connectable implementation "

    /// <summary>   Gets or sets the TCP client session. </summary>
    /// <value> The TCP client session. </value>
    public ViSession? ViSession { get; }

    /// <summary>   Gets a reference to the connectable <see cref="ViSession"/> object . </summary>
    /// <value>   [<see cref="IConnectable"/>]. </value>
    public IConnectable? Connectable => this.ViSession;

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
    private void ViSession_ConnectionChanged( object sender, ConnectionChangedEventArgs eventArgs )
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
    private void ViSession_ConnectionChanging( object sender, ConnectionChangingEventArgs eventArgs )
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
