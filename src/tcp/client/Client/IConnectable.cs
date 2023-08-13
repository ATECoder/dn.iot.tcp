namespace cc.isr.Iot.Tcp.Client;

/// <summary>   Interface for connectable. </summary>
/// <remarks>   2023-08-10. </remarks>
public interface IConnectable: IDisposable
{
    /// <summary>   Gets or sets a value indicating whether the connected. </summary>
    /// <value> True if connected, false if not. </value>
    public bool Connected { get; }

    /// <summary>   Gets or sets a value indicating whether we can connect. </summary>
    /// <value> True if we can connect, false if not. </value>
    public bool CanConnect { get; }

    /// <summary>   Opens the connection. </summary>
    public void Connect();

    /// <summary>   Gets or sets a value indicating whether we can disconnect. </summary>
    /// <value> True if we can disconnect, false if not. </value>
    public bool CanDisconnect { get; }

    /// <summary>   Closes the connection. </summary>
    public void Disconnect();

    /// <summary>   Event queue for all listeners interested in ConnectionChanged events. </summary>
    public event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

    /// <summary>   Event queue for all listeners interested in ConnectionChanging events. </summary>
    public event EventHandler<ConnectionChangingEventArgs> ConnectionChanging;

}
