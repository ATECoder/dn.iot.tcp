using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using cc.isr.Iot.Tcp.Client.Ieee488;

namespace cc.isr.Iot.Tcp.Client.Scpi;

/// <summary>   A 2700 vi. </summary>
/// <remarks>   2023-08-15. </remarks>
public class K2700VI : Ieee488VI
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
    public K2700VI( TcpSession tcpSession,
                    char readTermination = '\n', char writeTermination = '\n',
                    int readAfterWriteDelayMs = 5 ) : base( tcpSession, readTermination, writeTermination, readAfterWriteDelayMs )
    {
        this.ScpiSystem = new ScpiSystem( this );
        this.ScpiRoute = new ScpiRoute( this );
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="ipv4Address">      The IPv4 address. </param>
    /// <param name="portNumber">       The port number. </param>
    public K2700VI( string ipv4Address, int portNumber ) : this( new TcpSession( ipv4Address, portNumber ) )
    { }

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-15. </remarks>
    public K2700VI() : this( "192.168.0.252", 1234 )
    {
    }

    /// <summary>   Initializes this object. </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="readTermination">          (Optional) (The read termination. </param>
    /// <param name="writeTermination">         (Optional) (The write termination. </param>
    /// <param name="readAfterWriteDelayMs">    (Optional) (The read after write delay in
    ///                                         milliseconds. </param>
    public override void  Initialize( TcpSession tcpSession, char readTermination = '\n', char writeTermination = '\n',
                                      int readAfterWriteDelayMs = 5 ) 
    {
        base.Initialize( tcpSession, readTermination, writeTermination, readAfterWriteDelayMs );
        this.ScpiSystem = new ScpiSystem( this );
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="portNumber">    The port number. </param>
    public override void Initialize( string ipv4Address, int portNumber = 5025 )
    {
        this.Initialize( new TcpSession( ipv4Address, portNumber ) );
    }


    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.
    /// </summary>
    /// <param name="disposing">    True to release both managed and unmanaged resources; false to
    ///                             release only unmanaged resources. </param>
    protected override void Dispose( bool disposing )
    {
        try
        {
            if ( disposing )
            {
            }
        }
        finally
        {
            base.Dispose( disposing );
        }
    }

    #endregion

    #region " channel readings "

    /// <summary>   The event that is raised upon completion of a reading. </summary>
    public event EventHandler<ChannelReadingEventArgs>? ReadingCompleted;

    /// <summary>   Raises the channel reading event. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <param name="e">    Event information to send to registered event handlers. </param>
    private void OnReadingCompleted( ChannelReadingEventArgs e )
    {
        var handler = ReadingCompleted;
        handler?.Invoke( this, e );
    }

    #endregion

    #region " subsystems "

    /// <summary>   Gets or sets the scpi system. </summary>
    /// <value> The scpi system. </value>
    public ScpiSystem ScpiSystem { get; private set; }

    /// <summary>   Gets or sets the scpi route. </summary>
    /// <value> The scpi route. </value>
    public ScpiRoute ScpiRoute{ get; private set; }

    #endregion

    #region " 2700 properties "

    /// <summary>   Gets a value indicating whether this instruments is measuring using its front inputs. </summary>
    /// <value> True if front inputs, false if not. </value>
    public bool IsFrontInputs => this.ScpiSystem.FrontSwitch();

    #endregion

    #region " Resistance measurement configurations "

    /// <summary>   Sets the instrument to continuous auto range resistance measurement. </summary>
    public void ContinuousResistanceAutoRange()
    {
        // set resistance defaults
        _ = this.WriteLine( ":RES:RANG:AUTO ON" );
        _ = this.WriteLine( ":RES:NPLC 1" );

        // set reading format
        _ = this.WriteLine( ":FORM:ELEM READ" );

        // turn on continuous mode
        _ = this.WriteLine( ":FUNC 'FRES'" );
        _ = this.WriteLine( ":FRES:RANG:AUTO ON" );
        _ = this.WriteLine( ":FRES:NPLC 1" );
        _ = this.WriteLine( ":INIT:CONT On" );
    }

    /// <summary>   Configures single resistance reading. </summary>
    public void ConfigureSingleResistanceReading()
    {
        // set the device to measure 4 wire resistance
        // _ = this.WriteLine("*RST");
        _ = this.WriteLine( ":TRIG:SOUR IMM" );
        _ = this.WriteLine( ":INIT:CONT OFF" );
        _ = this.WriteLine( ":SAMP:COUN 1" );
        _ = this.WriteLine( ":TRIG:COUN 1" );
        _ = this.WriteLine( ":FUNC 'RES'" );

        // set reading format
        _ = this.WriteLine( ":FORM:ELEM READ" );

    }

    /// <summary>   Performs a single read from the front panel. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <param name="resistanceNo">   (Optional) (0) The resistance number. </param>
    /// <returns>   [Double] the measured resistance. </returns>
    public Double ReadFrontResistance( int resistanceNo = 0 )
    {
        _ = this.WriteLine( ":INIT" );
        _ = this.WriteLine( ":READ?" );

        string reading = this.Read();

        this.OnReadingCompleted( new ChannelReadingEventArgs(  resistanceNo, reading));

        return Convert.ToDouble(reading);    
    }

    #endregion

}
