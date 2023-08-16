using System;

using cc.isr.Iot.Tcp.Client.Ieee488;

using static System.Collections.Specialized.BitVector32;

namespace cc.isr.Iot.Tcp.Client.Scpi;

/// <summary>   A scpi system. </summary>
/// <remarks>   2023-08-12. </remarks>
public class ScpiSystem
{

    #region " Construction and cleanup "

    /// <summary>   Constructor. </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="session">  The session. </param>
    public ScpiSystem(ViSession session)
    {
        this.Session = session;
    }

    #endregion

    #region " ieee488 session "

    /// <summary>   Gets or sets the session. </summary>
    /// <value> The session. </value>
    public ViSession? Session {get; set; }

    #endregion

    #region " SCPI system subsystem implementation "

    /// <summary>   Gets or sets the 'beep' command. </summary>
    /// <value> The 'beep' command. </value>
    public string BeepCommand { get; set; } = "SYST:BEEP";

    /// <summary>   Issues a beep. </summary>
    public void Beep()
    {
        if ( !String.IsNullOrEmpty( this.BeepCommand ) )
            _ = (this.Session?.WriteLine( this.BeepCommand ));
    }

    /// <summary>   Gets or sets the 'error queue query' command. </summary>
    /// <value> The 'error queue query' command. </value>
    public string ErrorQueueQueryCommand { get; set; } = ":SYST:ERR?";

    /// <summary>   Gets the top error from the error queue. </summary>
    /// <value>   [string]. </value>
    public string ErrorDequeue()
    {
        return String.IsNullOrEmpty( this.ErrorQueueQueryCommand )
            ? string.Empty
            : this.Session?.QueryLine( ":SYST:ERR?" ) ?? string.Empty;
    }

    /// <summary>   Gets or sets the 'error queue clear' command. </summary>
    /// <value> The 'error queue clear' command. </value>
    public string ErrorQueueClearCommand { get; set; } = ":SYST:CLE";

    /// <summary>   Clears the error queue. </summary>
    public void ErrorQueueClear()
    {
        if ( !String.IsNullOrEmpty( this.ErrorQueueClearCommand ) )
            _ = (this.Session?.WriteLine( this.ErrorQueueClearCommand ));
    }

    /// <summary>   Gets or sets the 'front switch query' command. </summary>
    /// <value> The 'front switch query' command. </value>
    public string FrontSwitchQueryCommand { get; set; } = ":SYST:FRSW?";

    /// <summary>   Query INPUTS switch (0=rear, 1=front). </summary>
    /// <value>   [bool] true if the inputs are set to the front panel. </value>
    public bool FrontSwitch()
    {
        return String.IsNullOrEmpty( this.FrontSwitchQueryCommand )
            ? false
            : (this.Session?.QueryLine(this.FrontSwitchQueryCommand ) ?? string.Empty).StartsWith("1");
    }

    /// <summary>   Gets or sets the 'preset' command. </summary>
    /// <value> The 'preset' command. </value>
    public string PresetCommand { get; set; } = ":SYST:PRES";

    /// <summary>   Return to system preset defaults. </summary>
    public void Preset()
    {
        if ( !String.IsNullOrEmpty( this.PresetCommand ) )
            _ = (this.Session?.WriteLine( this.PresetCommand ));
    }

    #endregion
}
