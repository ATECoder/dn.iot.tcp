namespace cc.isr.Iot.Tcp.Client;

/// <summary>   Additional information for connection changing events. </summary>
/// <remarks>   2023-08-10. </remarks>
public class ConnectionChangingEventArgs
{

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-10. </remarks>
    /// <param name="connected">    True if connected, false if not. </param>
    /// <param name="cancel">       True if cancel, false if not. </param>
    public ConnectionChangingEventArgs( bool connected, bool cancel)
    {
        this.Connected = connected;
        this.Cancel = cancel;
    }

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-10. </remarks>
    public ConnectionChangingEventArgs() : this(false, false)
    {
    }

    /// <summary>   Gets or sets a value indicating whether the session is connected. </summary>
    /// <value> True if connected, false if not. </value>
    public bool Connected { get; set; }

    /// <summary>   Gets or sets a value indicating whether the cancel. </summary>
    /// <value> True if cancel, false if not. </value>
    public bool Cancel { get; set; }

}
