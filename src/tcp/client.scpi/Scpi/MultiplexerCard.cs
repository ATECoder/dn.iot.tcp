using System;
using System.Collections.Generic;
using System.Text;

namespace cc.isr.Iot.Tcp.Client.Scpi;

/// <summary>   A multiplexer card. </summary>
/// <remarks>   2023-08-17. </remarks>
public class MultiplexerCard
{
    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <param name="name">         The name. </param>
    /// <param name="capacity">     The capacity. </param>
    public MultiplexerCard(string name, int capacity)
    {
        this.Name = name;
        this.Capacity = capacity;
    }

    /// <summary>   Makes a deep copy of this object. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <returns>   A copy of this object. </returns>
    public MultiplexerCard Clone( )
    {
        return new MultiplexerCard( this.Name, this.Capacity );
    }

    /// <summary>   Gets the name of the card, e.g., 7704. </summary>
    /// <value> The name. </value>
    public string Name { get;  }

    /// <summary>   Gets the capacity. </summary>
    /// <value> The capacity. </value>
    public int Capacity { get; }

    /// <summary>   Gets or sets the slot number. </summary>
    /// <value> The slot number. </value>
    public int SlotNumber { get; set; }

    /// <summary>   Builds default scan list. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <param name="measurementCode">   The measurement code. </param>
    /// <returns>   A <see cref="string" />. </returns>
    public string BuildDefaultScanList( string measurementCode )
    {
        int firstChannelNumber = this.SlotNumber * 100 + 1;
        int lastChannelNumber = firstChannelNumber + this.Capacity - 1;
        this.ScanList = $":FUNC '{measurementCode}',(@{firstChannelNumber}:{lastChannelNumber})";
        return this.ScanList;
    }

    /// <summary>   Gets or sets the scan list associated with this card. </summary>
    /// <value> Ths scan list associated with this card. </value>
    public string? ScanList { get; set; }

    /// <summary>   Gets or sets the 'route' command. </summary>
    /// <value> The 'route' command. </value>
    public string RouteCommand =>
        20 == this.Capacity
            ? $":ROUT:MULT:CLOS (@{this.SlotNumber}44,{this.SlotNumber}45)"
            : 40 == this.Capacity
                ? $":ROUT:MULT:CLOS (@{this.SlotNumber}44,{this.SlotNumber}45)"
                : string.Empty;

}
