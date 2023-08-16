namespace cc.isr.Iot.Tcp.Client.Scpi;

/// <summary>   Additional information for channel reading events. </summary>
/// <remarks>   2023-08-15. </remarks>
public class ChannelReadingEventArgs: EventArgs
{

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-15. </remarks>
    public ChannelReadingEventArgs()
    {
    }

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="channelNo">    The channel number. </param>
    /// <param name="reading">      The reading. </param>
    public ChannelReadingEventArgs( int channelNo, string reading )
    {
        this.Initialize(channelNo, reading);
    }

    /// <summary>   Initializes and returns the event arguments. </summary>
    /// <param name="channelNo">   [integer] The channel number. </param>
    /// <param name="reading">     [double] The reading. </param>
    /// <value>   [<see cref="ChannelReadingEventArgs"/>]. </value>
    public ChannelReadingEventArgs Initialize( int channelNo, string reading )
    {
        this.ChannelNumber = channelNo;
        this.Reading = reading;
        return this;
    }

    /// <summary>   Gets the channel number. </summary>
    /// <value>   [int]. </value>
    public int ChannelNumber { get; set; }

    /// <summary>   Gets the reading. </summary>
    /// <value>   [string]. </value>
    public string? Reading { get; set; }
}
