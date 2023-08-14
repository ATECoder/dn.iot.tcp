using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using cc.isr.Iot.Tcp.Client;

using CommunityToolkit.Mvvm.ComponentModel;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A gpib lan controller. </summary>
/// <remarks>   2023-08-12. </remarks>
public partial class GpibLanController : ObservableObject, IDisposable
{

    #region " construction and cleanup "

    private const int _gpibLanPortNumber = 1234;

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="exceptionTracer">          The exception tracer. </param>
    /// <param name="readTermination">          (Optional) The read termination. </param>
    /// <param name="writeTermination">         (Optional) The write termination. </param>
    /// <param name="readAfterWriteDelayMs">    (Optional) The read after write delay in
    ///                                         milliseconds. </param>
    public GpibLanController( TcpSession tcpSession, IExceptionTracer exceptionTracer,
                             char readTermination = '\n', char writeTermination = '\n',
                             int readAfterWriteDelayMs = 5)
    {
        this.TcpSession = tcpSession;
        this.ReadTermination = readTermination;
        this.WriteTermination = writeTermination;
        this.ControllerMode = true;
        this.ExceptionTracer = exceptionTracer;
        this.ReadAfterWriteDelay = readAfterWriteDelayMs;

        // this needs to be turned on if using the Keithley 2700 with enabled read-after-write

        this.DisableReadAfterWriteOnWrite = false;

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

    #region " gpib lan controller implementation "

    /// <summary>   Gets or sets a value indicating whether this object is enabled. </summary>
    /// <remarks>
    /// The GPIB-Lan controller is enabled only if the Tcp client is connected to a GPIB-Lan
    /// controller such as the Prologix GPIB-Lan controller.
    /// </remarks>
    /// <value> True if enabled, false if not. </value>
    [ObservableProperty]
    private bool _enabled;

    /// <summary>   Gets or sets the read after write delay. </summary>
    /// <value> The read after write delay. </value>
    [ObservableProperty]
    private int _readAfterWriteDelay;

    #endregion

    #region " controller mode device i/o "

    /// <summary>
    /// Gets or sets a value indicating whether the read after write on write is disabled.
    /// </summary>
    /// <remarks>
    /// Read-After-Write should be disabled on non-query commands with instruments that throw Query
    /// Unterminated errors on such commands if Read-After-Write is on.
    /// </remarks>
    /// <value> True if disable read after write on write, false if not. </value>
    [ObservableProperty]
    private bool _disableReadAfterWriteOnWrite;

    /// <summary>
    /// Gets or sets a value indicating whether the read after write is enabled.
    /// </summary>
    /// <value> True if read after write enabled, false if not. </value>
    [ObservableProperty]
    private bool _readAfterWriteEnabled;

    /// <summary>   Gets or sets the read timeout. </summary>
    /// <value> The read timeout. </value>
    [ObservableProperty]
    private int _readTimeout;

    /// <summary>   Configure the Read-After-Write mode of the controller. </summary>
    /// <remarks>
    /// The controller can be configured to automatically address
    /// instruments to talk after sending them a command in order to read their response. The
    /// feature called, Read-After-Write, saves the user from having to issue read commands
    /// repeatedly. This command enabled or disabled the Read-After-Write feature.
    ///
    /// In addition, auto command also addresses the instrument at the currently specified
    /// address to TALK or LISTEN. ++auto 0 addresses the instrument to LISTEN and
    /// ++auto 1 addresses the instrument to TALK.
    /// If the command is issued without any arguments it returns the current state of the
    /// read-after-write feature.
    ///
    /// NOTE:
    /// Some instruments generate “Query Unterminated” or “-420” error if they are addressed
    /// to talk after sending a command that does not generate a response (often called non
    /// query commands). In effect the instrument is saying, I have been asked to talk but I have
    /// nothing to say. The error is often benign and may be ignored. Otherwise, use the
    /// ++read command to read the instrument response. For example:
    /// <code>
    /// ++auto 0 — Turn off read-after-write and address instrument to listen
    /// SET VOLT 1.0 — Non-query command
    /// *idn? — Query command
    /// ++read eoi — Read until EOI asserted by instrument
    /// "HP54201A" — Response from instrument
    /// </code>
    /// <param name="enable">   true to enable read-after-write. </param>
    /// </remarks>
    public void ReadAfterWriteEnabledSetter( bool enable )
    {
        if ( enable != this.ReadAfterWriteEnabled )
        {
            _ = this.SendToController( "++auto " + (enable ? "1" : "0") );
            this.ReadAfterWriteEnabled = enable;
        }
    }

    /// <summary>   Query the Read-After-Write enabled mode of the controller. </summary>
    public bool ReadAfterWriteEnabledGetter()
    {
        this.ReadAfterWriteEnabled = "1" == this.QueryController( "++auto" );
        return this.ReadAfterWriteEnabled;
    }

    /// <summary>   Sends a message to the device. </summary>
    /// <remarks>
    /// If using the controller at port 1234 and auto read-after-write is on, this method first sets
    /// the controller to auto off (++auto 0) to prevent it from setting the device to talk
    /// prematurely which might cause the device (e.g., the Keithley 2700 scanning multimeter) to
    /// issue error -420 Query Unterminated.
    /// </remarks>
    /// <param name="message">              The message. </param>
    /// <param name="appendTermination">    (Optional, True) True to append termination. </param>
    /// <returns>   [Long] The number of sent characters. </returns>
    public int SendToDevice( string message , bool appendTermination = true )
    {
        int result = 0;

        if ( this.TcpSession != null )
        {

            if ( appendTermination ) message += this.WriteTermination;

            // if auto read after write and write control of read after write is enabled,
            // turn on listen to prevent Query Unterminated error on the 2700.

            if ( this.ReadAfterWriteEnabled && this.DisableReadAfterWriteOnWrite ) this.ReadAfterWriteEnabledSetter( false );

            // send the message to the device.

            result = this.TcpSession.Write( message );

            // a read after write delay is necessary for proper operations.

            _ = Task.Delay( this.ReadAfterWriteDelay );

        }
        return result;

    }

    /// <summary>   Receives a message from the server until reaching the specified termination
    /// or reading the specified number of characters. </summary>
    /// <remarks>   If <see cref="ReadAfterWriteEnabled"/> is not enabled then
    /// this method uses the <c>++read</c> command to first read the data
    /// from the device to the controller. </remarks>
    /// <param name="a_maxLength">     (Optional, 32767) The maximum number of bytes to read. </param>
    /// <param name="a_trimEnd">       (Optional, true) true to return the string without the termination. </param>
    /// <returns>   The received message. </returns>
    public string ReceiveFromDevice( int a_maxLength = 0x7FFF, bool a_trimEnd = true )
    {

        string reply = string.Empty;

        if ( this.TcpSession != null )
        {
            if ( !this.ReadAfterWriteEnabled )
            {
                // if auto read after write is disabled, we can either turn on // read after write,
                // but it seems that getting the controller to read from the instrument is much faster.

                _ = this.ReadFromDeviceToController();
            }

            _ = this.TcpSession.Read( a_maxLength, ref reply, a_trimEnd );

        }

        return reply;
    }

    /// <summary>   Sends a message to the device and receives a reply. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="a_message">            The message. </param>
    /// <param name="appendTermination">    (true) (Optional) true to append the
    ///                                     <see cref="WriteTermination"/> to the message. </param>
    /// <param name="maxLength">            (Optional) (Optional, 32767) The maximum number of bytes
    ///                                     to read. </param>
    /// <param name="trimEnd">              (Optional) (Optional, true) true to return the string
    ///                                     without the termination. </param>
    /// <returns>   The received message. </returns>
    public string QueryDevice( string a_message, bool appendTermination = true, int maxLength = 0x7FFF, bool trimEnd = true )
    {
        return 0 < this.SendToDevice( a_message, appendTermination )
                ? this.ReceiveFromDevice( maxLength , trimEnd)
                : string.Empty;
    }


    #endregion

    #region " controller i/o "

    /// <summary>   Sends a message to the controller. </summary>
    /// <remarks>   This method does not alter the auto Read-After-Write condition. </remarks>
    /// <param name="message">              The message. </param>
    /// <param name="appendTermination">    [Optional] True to append the
    ///                                     <see cref="WriteTermination"/> to the message. </param>
    /// <returns>   [Long] The number of sent characters. </returns>
    public int SendToController( string message, bool appendTermination = true )
    { 
        if (appendTermination ) message += this.WriteTermination;

        int result = this.TcpSession!.Write( message );

        _ = Task.Delay( this.ReadAfterWriteDelay );

        return result;
    }

    /// <summary>   Receives a message from the controller. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="maxLength">    [32767] The maximum length of the. </param>
    /// <param name="trimEnd">      [true] true to trim the end termination. </param>
    /// <returns>   The received message. </returns>
    public string ReceiveFromController( int maxLength = 0x7FFF, bool trimEnd = true ) 
    {
        if ( this.TcpSession is null ) return string.Empty;

        string reply = string.Empty;
        int readCount = this.TcpSession.Read( maxLength, ref reply, trimEnd );

        // messages that come from the controller have a CR + LF termination. The Receive Raw method
        // may stop at the LF termination, in which case the CR termination needs to be trimmed.

        if ( trimEnd && ( 0 < readCount ) )
        {
            reply = reply.TrimEnd( '\r' );
            reply = reply.TrimEnd( '\n' );
        }

        return reply;   
    }

    /// <summary>   Sends a message to the controller and read back the reply. </summary>
    /// <param name="message">             The message. </param>
    /// <param name="appendTermination">   [Optional, true] true to append the
    ///                                      <see cref="WriteTermination"/> to the message. </param>
    /// <returns>   The received message. </returns>
    public string QueryController( string message, bool appendTermination = true )
    {
        return 0 < this.SendToController( message, appendTermination )
            ? this.ReceiveFromController()
            : string.Empty;
    }

    /// <summary>   Sends the <c>++read eoi</c> command to have the controller set the instrument to talk
    /// and get the data from the instrument into the controller until EOI character is received.
    /// </summary>
    /// <remarks>
    /// This command can be used to read data from an instrument until:
    /// <code>
    /// EOI is detected or timeout expires, or
    /// A specified character is read, or
    /// timeout expires,
    /// </code>
    /// Timeout is set using the read_tmo_ms command and applies to inter-character delay, i.e.,
    /// the delay since the last character was read. Timeout is not be confused with the total
    /// time for which data is read.
    /// SYNTAX: ++read [eoi|{char}] where {char}> is a decimal value less than 256
    /// Examples:
    /// <code>
    ///  ++read     - Read until timeout
    ///  ++read eoi - Read until EOI detected or timeout
    ///  ++read 10  - Read until LF (ASCII 10) is received or timeout
    /// </code>
    /// </remarks>
    /// <returns>   [int] The number of characters that were sent. </returns>
    public int ReadFromDeviceToController()
    {
        return this.SendToController( "++read eoi" );
    }

    #endregion

    #region " device mode functions "

    /// <summary>   Gets or sets a value indicating whether the controller mode. </summary>
    /// <value> True if controller mode, false if not. </value>
    [ObservableProperty]
    private bool _controllerMode;

    /// <summary>   Gets or sets the status byte. </summary>
    /// <value> The status byte. </value>
    [ObservableProperty]
    private int _statusByte;

    /// <summary>   Sets the device Status Byte to be returned on a serial poll. </summary>
    /// <remarks>
    /// This command works only if the controller is set as a device using the ++mode command.
    /// The status command is used to specify the device status byte to be returned when serial
    /// polled by a GPIB controller. If the RQS bit (bit #6) of this status byte is set ) the
    /// SRQ signal is asserted (low). After a serial poll, SRQ line is de-asserted and status byte is
    /// set to 0. Status byte is initialized to 0 on power up.
    /// SRQ is also de-asserted and status byte is cleared if DEVICE CLEAR (DCL) message,
    /// or SELECTED DEVICE CLEAR (SDC) message, is received from the GPIB controller.
    /// If the command is issued without any arguments it returns the currently specified status
    /// byte.
    /// SYNTAX: ++status [0-255]
    /// Example:
    /// ++status 72 Specify serial poll status byte as 72. Since bit #6 is set, this
    ///             command will assert SRQ.
    /// ++status Query current serial poll status byte.
    /// </remarks>
    /// <param name="a_value">   The status byte mask. </param>
    public void StatusByteSetter( int a_value )
    { 
        if ( !this.ControllerMode )
        {
            a_value = this.Delimit(a_value, 0, 255);
            if ( a_value != this.StatusByte )
                _ = this.SendToController( $"++status {a_value}" );
        }
        this.StatusByte = a_value;
    }

    /// <summary>   Gets the device Status Byte. </summary>
    public int StatusByteGetter()
    {
        if ( this.ControllerMode )
            this.StatusByte = 0;
        else
        {
            if ( int.TryParse( this.QueryController( "++status" ), out int value ) )
                this.StatusByte = value;
        }
        return this.StatusByte;
    }

    /// <summary>   Returns the delimited integer value. </summary>
    /// <param name="value">      [Integer] the value to delimit.</param>
    /// <param name="minValue">   [Integer] the minimum. </param>
    /// <param name="maxValue">   [Integer] the maximum. </param>
    public int Delimit( int value, int minValue, int maxValue )
    {
        return value < minValue
            ? minValue
            : value > maxValue
                ? maxValue
                : value;
    }

    #endregion

    #region " tcp client "

    /// <summary>   Gets or sets the TCP client session. </summary>
    /// <value> The TCP client session. </value>
    private TcpSession? TcpSession { get; set; }

    /// <summary>   Gets or sets the read termination. </summary>
    /// <value> The read termination. </value>
    [ObservableProperty]
    private char _readTermination;

    /// <summary>   Gets or sets the write termination. </summary>
    /// <value> The write termination. </value>
    /// <summary>   Gets or sets the read termination. </summary>
    /// <value> The read termination. </value>
    [ObservableProperty]
    private char _writeTermination;

    #endregion

    #region " GPIB methods "

    /// <summary>   Issues a Go To Local (GTL) (++loc). </summary>
    /// <remarks>   Valid if <see cref="Enabled"/>. </remarks>
    public void GoToLocal()
    {
        _ = this.SendToController( "++loc" );
    }

    /// <summary>   Query the GPIB address. </summary>
    /// <value>    An string including the primary and secondary address separated by a space. </value>
    public String GpibAddressGetter()
    {
        string reply = "-1";
        string p_receivedMessage = this.QueryController( "++addr" );
        if ( !string.IsNullOrEmpty( p_receivedMessage ) )
        {
            string[] p_addresses = p_receivedMessage.Split( new char[] { ' ' } );
            switch ( p_addresses.Length )
            {
                case 1:
                    reply  = p_addresses[0];
                    break;
                case 2:
                    reply =  p_addresses[ 1 ] + " " + p_addresses[ 2 ];
                    break;
                default:
                    break;
            }
        }
        return reply;
    }

    /// <summary>
    /// Configure the GPIB address of the GPIB Lan Controller. The meaning of the GPIB address
    /// depends on the operating mode of the controller.
    /// 
    /// In Controller mode, it refers to the GPIB address of the instrument being controlled. In
    /// DEVICE mode, it is the address of the GPIB peripheral that the controller is emulating.
    /// </summary>
    /// <remarks>
    /// An optional secondary address may also be specified.
    /// 
    /// Internally, the secondary address, which is offset by 96, must be separated from the primary
    /// address by a space character. Specifying secondary address has no effect in DEVICE mode.
    /// </remarks>
    /// <param name="a_primaryAddress">     Specifies the primary GPIB address between 0 and 30. </param>
    /// <param name="a_secondaryAddress">   [Optional, Integer] (Optional) Specifies the second GPIB
    ///                                     Address between 0 and 30. </param>
    public void GpibAddressSetter( int a_primaryAddress, int a_secondaryAddress = -1 )
    {
        if ( a_primaryAddress >= 0 && a_secondaryAddress < 0 )
        {
            a_primaryAddress = this.Delimit( a_primaryAddress, 0, 30 );
            _ = this.SendToController( $"++addr {a_primaryAddress}" );
        }
        else if ( a_primaryAddress >= 0 && a_secondaryAddress >= 0 )
        {
            a_primaryAddress = this.Delimit( a_primaryAddress, 0, 30 );
            a_secondaryAddress = this.Delimit( a_secondaryAddress, 0, 30 );
            _ = this.SendToController( $"++addr {a_primaryAddress} {a_secondaryAddress + 96}" );
        }
    }

    /// <summary>   Issues a local lockout (++llo). </summary>
    /// <remarks>   Valid if <see cref="Enabled"/>. </remarks>
    public void LocalLockout()
    {
        _ = this.SendToController( "++llo" );
    }

    /// <summary>
    /// Sets the device read timeout, in milliseconds, to be used in the controller read and spoll
    /// commands. Timeout may be set to any value between 1 &amp;&amp; 3000 milliseconds.
    /// </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="a_timeoutMs">  The timeout interval in milliseconds. </param>
    public void ReadTimeoutSetter( int a_timeoutMs )
    {
        a_timeoutMs = this.Delimit( a_timeoutMs, 1, 3000 );
    
        if ( a_timeoutMs != this.ReadTimeout )
            _ = this.SendToController( $"++read_tmo_ms {a_timeoutMs}" );

        this.ReadTimeout = a_timeoutMs;
    }

    /// <summary>   Gets the device read timeout, in milliseconds, that is used in the
    ///  controller read and spoll commands.
    /// </summary>
    public int ReadTimeoutGetter()
    {
        if ( int.TryParse( this.QueryController( "++read_tmo_ms" ), out int value ) )
            this.ReadTimeout = value;
        return this.ReadTimeout;
    }

    /// <summary>   Issues an SDC. </summary>
    /// <remarks>   Valid if <see cref="Enabled"/>. </remarks>
    public void SelectiveDeviceClear()
    {
        _ = this.SendToController( "++clr" );
    }

    /// <summary>
    /// Performs a serial poll of the instrument at the current or the specified address. If no
    /// address is specified ) this command serial polls the currently addressed instrument (as set
    /// by a previous ++addr command). This command uses the time-out value specified by the
    /// read_tmo_ms command.
    /// </summary>
    /// <remarks>
    /// Serial polling obtains specific information from a device. When you serial poll, the
    /// Controller sends a special command message—Serial Poll Enable (SPE)—to the device, directing
    /// it to return its serial poll status byte. The SPE message sets the IEEE 488.1 serial poll
    /// mode in the device, so when the device is addressed to talk, it returns a single 8-bit status
    /// byte. This serial poll status byte is different for each type of instrument; except for one
    /// bit, you must refer to the instrument user manual for information on the other bits. Bit 6
    /// (hex 40) of any serial poll status byte indicates whether a device requested service by
    /// asserting the SRQ line. The device uses the other seven bits of the status byte to specify
    /// why it needs attention.
    /// 
    /// After the Controller reads the status byte, it sends another command message, Serial Poll
    /// Disable (SPD), to the device. The SPD message terminates the serial poll mode, thus returning
    /// the device to its normal Talker/Listener state. Once a device requesting service is serial
    /// polled, it usually un-asserts the SRQ line.
    /// </remarks>
    /// <param name="a_primaryAddress">     [Optional, Integer] (Optional) Specifies the primary GPIB
    ///                                     address between 0 and 30. </param>
    /// <param name="a_secondaryAddress">   [Optional, Integer] (Optional) Specifies the second GPIB
    ///                                     Address between 0 and 30. </param>
    /// <returns>   The status byte. </returns>
    public int SerialPoll( int a_primaryAddress = -1, int a_secondaryAddress = -1 )
    { 
        string command = "++spoll";

        if  (a_primaryAddress >= 0 && a_secondaryAddress< 0 )
            command = $"{command} {a_primaryAddress}";

        else if  (a_primaryAddress >= 0 && a_secondaryAddress >= 0 )
            command = $"{command} {a_primaryAddress} {a_secondaryAddress + 96}";


        return int.TryParse( this.QueryController( command ), out int value )
            ? value
            : 0;
    }

    /// <summary>   Queries the GPIB SRQ signal status. </summary>
    /// <remarks>   This command returns the current state of the GPIB SRQ signal. The controller returns
    /// <c>1</c> if the SRQ signal is asserted (low) and <c>0</c> if the signal is not asserted (high). </remarks>
    /// <returns>   true if Server requested. </returns>
    public bool ServiceRequested()
    {
        return int.TryParse( this.QueryController( "++srq" ), out int value ) && 1 == value;
    }

    /// <summary>   Wait for the specified masked bits on the status byte or timeout. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="timeout">          time to wait for reply. </param>
    /// <param name="a_bitMask">        the bitmask to match for terminating the wait. </param>
    /// <param name="loopDelay">        (5) The loop delay in milliseconds. </param>
    /// <param name="doEventsAction">   (null) The do events action. </param>
    /// <returns>   [Integer] The last status byte read before ending the wait. </returns>
    public int AwaitStatus( TimeSpan timeout, int a_bitMask, int loopDelay = 5, Action? doEventsAction = null )
    {
        int p_statusByte;

        // read the status byte
        p_statusByte = this.SerialPoll();

        Stopwatch stopwatch = Stopwatch.StartNew();
    
        if ( timeout > TimeSpan.Zero )
        {
            bool completed = a_bitMask == (p_statusByte & a_bitMask);
            while ( stopwatch.Elapsed <= timeout && !completed )
            {
                if ( loopDelay > 0 )
                    _ = Stopwatch.StartNew().SyncLetElapse( TimeSpan.FromMilliseconds( loopDelay ));
                doEventsAction?.Invoke();
                p_statusByte = this.SerialPoll();
                completed = a_bitMask == (p_statusByte & a_bitMask);
            }
        }

        return p_statusByte;

    }

    #endregion

    #region " tcp session event handlers "

    /// <summary>   Gets or sets the exception tracer. </summary>
    /// <value> The exception tracer. </value>
    [ObservableProperty]
    private IExceptionTracer? _exceptionTracer;

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

            this.Enabled = this.TcpSession?.PortNumber == _gpibLanPortNumber;

            // the GPIB-Lan controller is enabled only if the Tcp client is
            // connected to a GPIB-Lan controller such as the Prologix GPIB-Lan controller.
             
            if ( !this.Enabled ) return;

		    // when connected, the read after write is turned off by default.
		    // this is done to prevent Query Unterminated errors on instruments such as the Keithley 2700.
		    // Where Read-After-Write is disabled, upon read, the controller is commanded to read from
		    // the instrument using the <c>++read</c> command.
		
		    if (eventArgs.Connected )
		    {

                // from experiments it seems this needs to be set first.
                _ = this.ReadAfterWriteEnabledGetter();

                // defaults to turning off auto read-after-write
                this.ReadAfterWriteEnabled = true;

                this.ReadAfterWriteEnabledSetter( false );

            }
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
            this.Enabled = _gpibLanPortNumber == ( ( TcpSession ) sender)?.PortNumber;

            // the GPIB-Lan controller is enabled only if the Tcp client is
            // connected to a GPIB-Lan controller such as the Prologix GPIB-Lan controller.
            if ( !this.Enabled ) return;

            if ( eventArgs.Connected )
			{
                // leave the instrument with auto read-after-write off to prevent
                // query unterminated errors.

                _ = this.ReadAfterWriteEnabledGetter();
                this.ReadAfterWriteEnabledSetter( false );

                // send the instrument back to local.

                this.GoToLocal();
            }
        }
        catch ( Exception ex )
        {
            this.ExceptionTracer?.Trace( ex );
        }

    }

    #endregion
}
