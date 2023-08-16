using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.Mvvm.ComponentModel;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A Virtual Instrument supporting the IEEE488.2 instrument protocol. </summary>
/// <remarks>   2023-08-12. </remarks>
public class Ieee488VI : ObservableObject, IConnectable
{

    #region " construction and cleanup "

    [SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>" )]
    private const int _gpibLanPortNumber = 1234;

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="readTermination">          (Optional) (The read termination. </param>
    /// <param name="writeTermination">         (Optional) (The write termination. </param>
    /// <param name="readAfterWriteDelayMs">    (Optional) (The read after write delay in
    ///                                         milliseconds. </param>
    public Ieee488VI( TcpSession tcpSession, 
                      char readTermination = '\n', char writeTermination = '\n',
                      int readAfterWriteDelayMs = 5 )
    {
        this._identity = string.Empty;
        this.Initialize(tcpSession, readTermination, writeTermination, readAfterWriteDelayMs );
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="ipv4Address">      The IPv4 address. </param>
    /// <param name="portNumber">       The port number. </param>
    public Ieee488VI( string ipv4Address, int portNumber ) : this( new TcpSession( ipv4Address, portNumber ) )
    { }

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-15. </remarks>
    public Ieee488VI()
    {
        this._identity = string.Empty;
    }

    /// <summary>   Initializes this object. </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="readTermination">          (Optional) (The read termination. </param>
    /// <param name="writeTermination">         (Optional) (The write termination. </param>
    /// <param name="readAfterWriteDelayMs">    (Optional) (The read after write delay in
    ///                                         milliseconds. </param>
    [MemberNotNull( nameof( Identity ) )]
    public virtual void Initialize( TcpSession tcpSession,
                            char readTermination = '\n', char writeTermination = '\n',
                            int readAfterWriteDelayMs = 5 )
    {
        this.Identity = string.Empty;
        this.ViSession = new ViSession( tcpSession, readTermination, writeTermination, readAfterWriteDelayMs );
        this.ReadTermination = readTermination;
        this.WriteTermination = writeTermination;
        this.ReadAfterWriteDelay = readAfterWriteDelayMs;

        if ( this.ViSession is not null )
        {
            this.ViSession.ConnectionChanged += this.ViSession_ConnectionChanged;
            this.ViSession.ConnectionChanging += this.ViSession_ConnectionChanging;
            this.ViSession.EventHandlerException += this.ViSession_EventHandlerException;
        }
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="portNumber">    The port number. </param>
    public virtual void Initialize( string ipv4Address, int portNumber = 5025 )
    {
        this.Initialize( new TcpSession( ipv4Address, portNumber ) );
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
    protected virtual void Dispose( bool disposing )
    {
        if ( disposing )
        {
            if ( this.ViSession is not null )
            {
                if ( this.ViSession.Connected ) { this.ViSession.Disconnect(); }
                this.ViSession.ConnectionChanged -= this.ViSession_ConnectionChanged;
                this.ViSession.ConnectionChanging -= this.ViSession_ConnectionChanging;
                this.ViSession.EventHandlerException -= this.ViSession_EventHandlerException;
            }
            this.ViSession?.Dispose();
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
    /// <remarks>   2023-08-14. </remarks>
    /// <exception cref="InvalidOperationException">    Thrown when the requested operation is
    ///                                                 invalid. </exception>
    /// <param name="message">              The message to send to the instrument. </param>
    /// <param name="queryEAV">             (Optional) (true) true to check the Error Available
    ///                                     status bit after sending the message if (<see cref="ViSession.UsingGpibLan"/>
    ///                                     or VXI. </param>
    /// <param name="appendTermination">    (Optional) (true) true to append termination to the
    ///                                     message. </param>
    /// <returns>   [Long] The number of sent characters. </returns>
    public int WriteLine( string message, bool queryEAV = true, bool appendTermination = true )
    {
        int reply = 0;
        if ( this.ViSession is not null )
        {

            reply = this.ViSession.WriteLine( message, appendTermination );

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
    /// <param name="awaitMAV">         (Optional) (true)  true to wait for the Message Available
    ///                                 status bit before reading if (<see cref="ViSession.UsingGpibLan"/>
    ///                                 or VXI. </param>
    /// <param name="maxLength">        (Optional) (32767)  The maximum number of bytes to read. </param>
    /// <param name="trimEnd">          (Optional) (true)  true to return the string without the
    ///                                 termination. </param>
    /// <param name="doEventsAction">   (Optional) (The do events action. </param>
    /// <returns>   The received message. </returns>
    public string Read( bool awaitMAV = true, int maxLength = 0x7FFF, bool trimEnd = true, Action? doEventsAction = null )
    {
        int statusByte = 0;
        string reply = string.Empty;
        if ( this.ViSession is not null )
        {

            if ( this.ViSession.GpibLan is not null && this.ViSession.UsingGpibLan && awaitMAV )
            {
                // wait for the message available bits.

                statusByte = this.ViSession.GpibLan.AwaitStatus( TimeSpan.FromMilliseconds( this.ViSession.SessionReadTimeout ),
                                                                   ( int ) ServiceRequests.MessageAvailable,
                                                                   doEventsAction:doEventsAction );
            }

            // either way, try reading the instrument

            reply = this.ViSession.SessionReadTimeout > 0
                    ? this.ViSession.AwaitReading( this.ViSession.SessionReadTimeout, maxLength, trimEnd )
                    : this.ViSession.Read( maxLength, trimEnd );

            // report an error on failure to read.
            if ( string.IsNullOrEmpty( reply ) )
            {

                // check if (message available.

                string errorMessage = this.IsServiceRequest( statusByte , ServiceRequests.MessageAvailable)
                    ? "No Message Available"
                    : "Data not received after message available";

                throw new TimeoutException(
                    $"SRQ=0x{statusByte:x2}. {errorMessage} (0x{ServiceRequests.MessageAvailable:x2}) reading the instrument at {this.ViSession.SocketAddress} with a timeout of {this.ViSession.SessionReadTimeout}ms." );
            }
        }
        return reply;
    }

    /// <summary>   Sends a message and receives a reply. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="message">              The message to send to the instrument. </param>
    /// <param name="queryEAV">             (Optional) (true) true to check the Error Available
    ///                                     status bit after sending the message if (<see cref="ViSession.UsingGpibLan"/>
    ///                                     or VXI. </param>
    /// <param name="awaitMAV">             (Optional) (true) true to wait for the Message Available
    ///                                     status bit before reading if (<see cref="ViSession.UsingGpibLan"/>
    ///                                     or VXI. </param>
    /// <param name="appendTermination">    (Optional) (true) true to append termination to the
    ///                                     message. </param>
    /// <param name="maxLength">            (Optional) (32767) The maximum number of bytes to read. </param>
    /// <param name="trimEnd">              (Optional) (true) true to return the string without the
    ///                                     termination. </param>
    /// <returns>   The received message. </returns>
    public string QueryLine( string message, bool queryEAV = true, bool awaitMAV = true,
                             bool appendTermination = true, int maxLength = 0x7FFF, bool trimEnd = true )
    {
        return 0 < this.WriteLine( message, queryEAV, appendTermination )
            ? this.Read( awaitMAV, maxLength, trimEnd )
            : string.Empty;
    }

    #endregion

    #region " ieee488 commands "

    /// <summary>   Clears Status (CLS) command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="awaitOpc"> (Optional) (true) true to wait for operation completion after issuing
    ///                         the <c>*CLS</c> command by querying <c>*CLS; *OPC?</c> </param>
    public void ClearExecutionState( bool awaitOpc = true )
    {
        string command = Syntax.ClearExecutionStateCommand;
        if ( awaitOpc )
        {
            command = $"{command};{Syntax.OperationCompletedQueryCommand}";
            _ = this.QueryLine( command, false, true );
        }
        else
        {
            _ = this.WriteLine( command, false );
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
        private set
        {
            this._identity = value;
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
        string eventStatus = this.QueryLine( Syntax.ServiceRequestQueryCommand );
        return int.TryParse( eventStatus, out int value )
            ? value
            : 0;
    }

    /// <summary>   Reads the status byte. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="canQuery"> (Optional) (false) true to send <c>*STB?</c>
    ///                         if (not <see cref="ViSession.UsingGpibLan"/>. </param>
    /// <returns>   The status byte. </returns>
    public int ReadStatusByte( bool canQuery = false )
    {
        // querying the service request status could cause
        // a query unterminated error.

        return this.ViSession?.UsingGpibLan ?? false
            ? this.ViSession?.GpibLan?.SerialPoll() ?? 0 
            : canQuery
                ? this.QueryServiceRequestStatus()
                : 0;
    }

    /// <summary>   Checks if service is requested. </summary>
    /// <param name="canQuery">   (false) true to send <c>*STB?</c>
    /// if (not <see cref="ViSession.UsingGpibLan"/>. </param>
    /// <returns>   The status byte. </returns>
    public bool ServiceRequested( bool canQuery = false )
    {
        return this.ViSession?.UsingGpibLan ?? false
            ? this.ViSession?.GpibLan?.ServiceRequested() ?? false
            : canQuery && this.IsServiceRequest( this.QueryServiceRequestStatus(), ServiceRequests.RequestingService );
    }

    /// <summary>   Issues a wait (*WAI) command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="awaitOpc"> (Optional) (true) true to wait for operation completion after issuing
    ///                         the <c>*WAI</c> command by querying <c>*WAI; *OPC?</c> </param>
    public void Wait( bool awaitOpc = true )
    {
        string command = Syntax.WaitCommand;
        if ( awaitOpc )
        {
            command = $"{command};{Syntax.OperationCompletedQueryCommand}";
            _ = this.QueryLine( command, false, true );
        }
        else
            _ = this.WriteLine( command );
    }

    /// <summary>   Enables standard events using the (*ESE {0}) command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="bitMask">  Defines the bits corresponding to the standard events to enable. </param>
    /// <param name="awaitOpc"> (Optional) (true) true to wait for operation completion after issuing
    ///                         the <c>*ESE #</c> command by querying <c>*ESE #; *OPC?</c> </param>
    public void EnableStandardEvents( int bitMask, bool awaitOpc = true )
    {
        string command = String.Format( Syntax.StandardEventEnableCommand, bitMask );
        if ( awaitOpc )
        {
            command = $"{command};{Syntax.OperationCompletedQueryCommand}";
            _ = this.QueryLine( command, false, true );
        }
        else
            _ = this.WriteLine( command );
    }

    /// <summary>   Returns the standard events enable byte using the *ESE? query command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   The standard event register enable status value. </returns>
    public int QueryStandardEventsEnable()
    {
        string enabledBits = this.QueryLine( Syntax.StandardEventEnableQueryCommand );
        return int.TryParse( enabledBits, out int value ) ? value : 0;
    }

    /// <summary>   Returns the standard events status byte using the *ESR? query command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   The standard event register status value. </returns>
    public int QueryStandardEventsStatus()
    {
        string statusBits = this.QueryLine( Syntax.StandardEventStatusQueryCommand );
        return int.TryParse( statusBits, out int value ) ? value : 0;
    }

    /// <summary>   Enables Service Request using the (*SRE {0}) command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="bitMask">  Defines the bits corresponding to the service request events to
    ///                         enable. </param>
    /// <param name="awaitOpc"> (Optional) (true) true to wait for operation completion after issuing
    ///                         the <c>*SRE #</c> command by querying <c>*SRE #; *OPC?</c> </param>
    public void EnableServiceRequest( int bitMask, bool awaitOpc = true )
    {
        string command = String.Format( Syntax.ServiceRequestEnableCommand, bitMask );
        if ( awaitOpc )
        {
            command = $"{command};{Syntax.OperationCompletedQueryCommand}";
            _ = this.QueryLine( command, false, true );
        }
        else
            _ = this.WriteLine( command );
    }

    /// <summary>   Returns the Service Request enable byte using the *SRE? query command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   The service request event register enable status value. </returns>
    public int QueryServiceRequestEnable()
    {
        string enabledBits = this.QueryLine( Syntax.ServiceRequestEnableQueryCommand );
        return int.TryParse( enabledBits, out int value ) ? value : 0;
    }

    /// <summary>   Reset to known state (*RST) command. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="awaitOpc"> (Optional) (true) true to wait for operation completion after issuing
    ///                         the <c>*SRE</c> command by querying <c>*SRE; *OPC?</c> </param>
    public void ResetKnownState( bool awaitOpc = true )
    {
        string command = Syntax.ResetKnownStateCommand;
        if ( awaitOpc )
        {
            command = $"{command};{Syntax.OperationCompletedQueryCommand}";
            _ = this.QueryLine( command, false, true );
        }
        else
            _ = this.WriteLine( command, false );
    }

    #endregion

    #region " connectable implementation "

    /// <summary>   Gets or sets the TCP client session. </summary>
    /// <value> The TCP client session. </value>
    public ViSession? ViSession { get; private set; }

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

    /// <summary>   Event queue for all listeners interested in EventHandlerException events. </summary>
    public event EventHandler<ThreadExceptionEventArgs>? EventHandlerException;

    /// <summary>   Event queue for all listeners interested in ConnectionChanged events. </summary>
    public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

    /// <summary>   Raises the connection changed event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="e">    Event information to send to registered event handlers. </param>
    protected void OnConnectionChanged( ConnectionChangedEventArgs e )
    {

        var handler = this.ConnectionChanged;
        try
        {
            handler?.Invoke( this, e );
        }
        catch ( System.OutOfMemoryException ) { throw; }
        catch ( System.DllNotFoundException ) { throw; }
        catch ( System.StackOverflowException ) { throw; }
        catch ( System.InvalidCastException ) { throw; }
        catch ( Exception ex )
        {
            // https://stackoverflow.com/questions/3114543/should-event-handlers-in-c-sharp-ever-raise-exceptions
            // other exceptions are to be callers for tracing or further handling.

            ex.Data.Add( $"Method {ex.Data.Count}", $"in {handler?.Method.Name}" );
            var eventHandlerException = this.EventHandlerException;
            eventHandlerException?.Invoke( this, new ThreadExceptionEventArgs( ex ) );
        }
    }

    /// <summary>   Event queue for all listeners interested in ConnectionChanging events. </summary>
    public event EventHandler<ConnectionChangingEventArgs>? ConnectionChanging;

    /// <summary>   Raises the connection changing event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="e">    Event information to send to registered event handlers. </param>
    protected void OnConnectionChanging( ConnectionChangingEventArgs e )
    {
        var handler = this.ConnectionChanging;
        try
        {
            handler?.Invoke( this, e );
        }
        catch ( System.OutOfMemoryException ) { throw; }
        catch ( System.DllNotFoundException ) { throw; }
        catch ( System.StackOverflowException ) { throw; }
        catch ( System.InvalidCastException ) { throw; }
        catch ( Exception ex )
        {
            // https://stackoverflow.com/questions/3114543/should-event-handlers-in-c-sharp-ever-raise-exceptions
            // other exceptions are to be callers for tracing or further handling.

            ex.Data.Add( $"Method {ex.Data.Count}", $"in {handler?.Method.Name}" );
            var eventHandlerException = this.EventHandlerException;
            eventHandlerException?.Invoke( this, new ThreadExceptionEventArgs( ex ) );
        }
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

    #region " vi session event handlers "

    /// <summary>   Handles the <see cref="TcpSession.ConnectionChanged"/> event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">  Reference to the <see cref="ConnectionChangedEventArgs"/> event
    ///                             arguments. </param>
    private void ViSession_ConnectionChanged( object sender, ConnectionChangedEventArgs eventArgs )
    {
        if ( sender is null || eventArgs is null ) return;
    }

    /// <summary>   Handles the <see cref="TcpSession.ConnectionChanging"/> event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">  Reference to the <see cref="ConnectionChangingEventArgs"/> event
    ///                             arguments. </param>
    private void ViSession_ConnectionChanging( object sender, ConnectionChangingEventArgs eventArgs )
    {
        if ( sender is null || eventArgs is null ) return;
    }

    /// <summary>
    /// Event handler. Called by TcpSession for event handler exception events.
    /// </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">    Thread exception event information. </param>
    private void ViSession_EventHandlerException( object sender, ThreadExceptionEventArgs eventArgs )
    {
        var handler = this.EventHandlerException;
        handler?.Invoke( this, eventArgs );
    }


    #endregion

    #region " tcp session event handlers "

    #endregion

}
