using System;
using System.Collections.Generic;
using System.Text;
using cc.isr.Iot.Tcp.Client.Ieee488;

namespace cc.isr.Iot.Tcp.Client.Scpi;

/// <summary>   A scpi route subsystem for handling multiplexer cards. </summary>
/// <remarks>   2023-08-17. </remarks>
public partial class ScpiRoute
{
    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <param name="ieee488VI">   A reference to the IEEE488 VI. </param>
    public ScpiRoute( Ieee488VI ieee488VI )
    {
        this.Ieee488VI = ieee488VI;
        this.InstrumentFamilyCards = new();
        this.InstalledCards = new();
    }
    #endregion

    #region " IEEE488 VI session "

    /// <summary>   Gets or sets the session. </summary>
    /// <value> The session. </value>
    public Ieee488VI Ieee488VI { get; set; }

    #endregion

    #region " Multiplexer cards "

    /// <summary>   Gets or sets the dictionary of cards that are available for this 
    ///             instrument family as defined by the <see cref="Ieee488VI"/>
    ///             keyed by the card name. </summary>
    /// <value> A dictionary of cards. </value>
    public Dictionary<string, MultiplexerCard> InstrumentFamilyCards { get; set; }

    /// <summary>   Populates the 7700 cards family dictionary. </summary>
    /// <remarks>   2023-08-17. </remarks>
    public void Populate7700Cards()
    {
        this.InstrumentFamilyCards.Clear();
        string name = "7700"; int capacity =20;
        this.InstrumentFamilyCards.Add(name, new MultiplexerCard( name, capacity ) );
        name = "7702"; capacity = 20; this.InstrumentFamilyCards.Add( name, new MultiplexerCard( name, capacity ) );
        name = "7708"; capacity = 40; this.InstrumentFamilyCards.Add( name, new MultiplexerCard( name, capacity ) );
        name = "7710"; capacity = 20; this.InstrumentFamilyCards.Add( name, new MultiplexerCard( name, capacity ) );
    }

    /// <summary>   Gets or sets the installed cards for this instrument instance
    ///             keyed by the card slot number. </summary>
    /// <value> The installed cards. </value>
    public Dictionary<int, MultiplexerCard> InstalledCards { get; set; }

    /// <summary>   Populates the cards using the <see cref="Ieee488.Syntax.OptionsQueryCommand"/> reply. </summary>
    /// <remarks>   2023-08-17. </remarks>
    public void PopulateCards( )
    {
        this.InstalledCards.Clear();
        Queue<string> cardsQueue = new( this.Ieee488VI.Options.Split( ',' ) );
        int slotNumber = 0;
        while ( cardsQueue.Count > 0 )
        {
            slotNumber += 1;
            string name = cardsQueue.Dequeue();
            if (this.InstrumentFamilyCards.ContainsKey (name ))
            {
                MultiplexerCard multiplexerCard = this.InstrumentFamilyCards[name].Clone();
                multiplexerCard.SlotNumber = slotNumber;
                this.InstalledCards.Add( slotNumber, this.InstrumentFamilyCards[name].Clone() );
            }
        }
    }

    /// <summary>   Sets and applies the full capacity scan lists for resistance measurements on all cards. </summary>
    public void DefineResistanceScanLists()
    {
        foreach ( var item in this.InstalledCards )
        {
            _ = item.Value.BuildDefaultScanList( "RES" );
            _ = this.Ieee488VI.WriteLine( item.Value.ScanList! );
        }
    }

    /// <summary>   Returns the route command for measuring a specific channel. </summary>
    /// <param name="channelNumber">   The channel number. </param>
    /// <returns>  The channel list to close. </returns>
    public string BuildChannelList( int channelNumber )
    {

        string p_routeCommand = string.Empty;

        int cumulativeCapacity = 0;
        foreach ( var item in this.InstalledCards )
        {
            cumulativeCapacity += item.Value.Capacity;
            if ( channelNumber <= cumulativeCapacity )
                p_routeCommand = item.Value.RouteCommand;
        }
        return p_routeCommand;

    }

    /// <summary>   Returns the scan list for a specific channel number. </summary>
    /// <param name="channelNumber">   The channel number. </param>
    /// <returns>  The scan list. </returns>
    public string BuildScanList( int channelNumber )
    {
        string scanCardChannel = string.Empty;

        int previousCumulativeCapacity = 0;
        int cumulativeCapacity = 0;
        foreach ( var item in this.InstalledCards )
        {
            cumulativeCapacity += item.Value.Capacity;
            if ( channelNumber <= cumulativeCapacity )
            {
                string scanChannel = $"0{channelNumber - previousCumulativeCapacity}";
                scanCardChannel = $"{item.Value.SlotNumber} {scanChannel.Right(2)}";
            }
            previousCumulativeCapacity = cumulativeCapacity;
        }

        return $"(@{scanCardChannel})";
    }

    #endregion
}
