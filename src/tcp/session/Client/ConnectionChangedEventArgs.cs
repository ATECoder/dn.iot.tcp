namespace cc.isr.Iot.Tcp.Client;

/// <summary> Additional information for connection changed events. </summary>
/// <remarks> 2023-08-10. </remarks>
public class ConnectionChangedEventArgs : System.EventArgs
{

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-10. </remarks>
    /// <param name="connected">    True if connected, false if not. </param>
    public ConnectionChangedEventArgs(bool connected )
    {
        this.Connected = connected;
    }

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-10. </remarks>
    public ConnectionChangedEventArgs(): this(false)
    {
    }

    /// <summary>   Gets or sets a value indicating whether the session is connected. </summary>
    /// <value> True if connected, false if not. </value>
    public bool Connected { get; set; }

}
