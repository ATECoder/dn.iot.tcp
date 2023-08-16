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
        this.ScpiSystem = new ScpiSystem( base.ViSession! );
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="ipv4Address">      The IPv4 address. </param>
    /// <param name="portNumber">       The port number. </param>
    public K2700VI( string ipv4Address, int portNumber ) : this( new TcpSession( ipv4Address, portNumber ) )
    { }

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-15. </remarks>
    public K2700VI()
    {
    }

    /// <summary>   Initializes this object. </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="tcpSession">               The TCP client session. </param>
    /// <param name="readTermination">          (Optional) (The read termination. </param>
    /// <param name="writeTermination">         (Optional) (The write termination. </param>
    /// <param name="readAfterWriteDelayMs">    (Optional) (The read after write delay in
    ///                                         milliseconds. </param>
    public override void  Initialize( TcpSession tcpSession,
                            char readTermination = '\n', char writeTermination = '\n',
                            int readAfterWriteDelayMs = 5 ) 
    {
        base.Initialize( tcpSession, readTermination, writeTermination, readAfterWriteDelayMs );
        this.ScpiSystem = new ScpiSystem( base.ViSession! );
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

    #endregion

    #region " scpi system "

    /// <summary>   Gets or sets the scpi system. </summary>
    /// <value> The scpi system. </value>
    public ScpiSystem? ScpiSystem { get; private set; }

    #endregion

}
