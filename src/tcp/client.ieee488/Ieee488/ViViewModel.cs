using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A ViewModel for the generic IEEE488 Virtual Instrument. </summary>
/// <remarks>   2023-08-14. </remarks>
public partial class ViViewModel : ObservableObject, IDisposable
{

    #region " construction and cleanup "

    /// <summary>   Default constructor. </summary>
    /// <remarks>   2023-08-14. </remarks>
    public ViViewModel()
    {
        this._availableCommands = new();
        this.PopulateCommands();
        this.Stopwatch = new Stopwatch();
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

            if ( this.Session is not null )
            {
                if ( this.Connected )
                {
                    this.Session.Disconnect();
                }
                this.Session.ConnectionChanged -= this.Session_ConnectionChanged;
                this.Session.ConnectionChanging -= this.Session_ConnectionChanging;
                this.Session.EventHandlerException -= this.Session_EventHandlerException;
            }

            this.Session?.Dispose();
            this.Session = null;
        }
    }


    #endregion

    #region " ieee 488 session "

    /// <summary>   Gets or sets the session. </summary>
    /// <value> The session. </value>
    public Ieee488Session? Session { get; set; }

    #endregion

    #region " observable properties "

    /// <summary>   The session read timeout. </summary>
    [ObservableProperty]
    private int _sessionReadTimeout;

    /// <summary>   The host address. </summary>
    [ObservableProperty]
    private string? _hostAddress;

    /// <summary>   The port number. </summary>
    [ObservableProperty]
    private int _portNumber;

    /// <summary>   The socket address. </summary>
    [ObservableProperty]
    private string? _socketAddress;

    /// <summary>   True if connected. </summary>
    [ObservableProperty]
    private bool _connected;

    /// <summary>   The query message command which to send to the instrument. </summary>
    [ObservableProperty]
    private string? _queryMessage;

    /// <summary>   The Message that was sent to the instrument. </summary>
    [ObservableProperty]
    private string? _sentMessage;

    /// <summary>   The Message that was received from the instrument. </summary>
    [ObservableProperty]
    private string? _receivedMessage;

    /// <summary>   The elapsed time. </summary>
    [ObservableProperty]
    private int _elapsedTime;

    /// <summary>   The elapsed time label. </summary>
    [ObservableProperty]
    private string? _elapsedTimeLabel;

    /// <summary>   The elapsed time format. </summary>
    [ObservableProperty]
    private string? _elapsedTimeFormat;

    /// <summary>   Message describing the last error. </summary>
    [ObservableProperty]
    private string? _lastErrorMessage;

    /// <summary>   The serial poll status byte. </summary>
    [ObservableProperty]
    private int _serialPollByte;

    /// <summary>   The status register byte. </summary>
    [ObservableProperty]
    private int _statusByte;

    /// <summary>   The standard register byte. </summary>
    [ObservableProperty]
    private int _standardByte;

    /// <summary>   True if requesting service. </summary>
    [ObservableProperty]
    private bool _requestingService;

    /// <summary>   The gpib address. </summary>
    [ObservableProperty]
    private int _gpibAddress;

    /// <summary>   The gpib LAN read timeout. </summary>
    [ObservableProperty]
    private int _gpibLanReadTimeout;

    /// <summary>   True to enable, false to disable the read after write. </summary>
    [ObservableProperty]
    private bool _readAfterWriteEnabled;

    /// <summary>   True to automatically read the status byte. </summary>
    [ObservableProperty]
    private bool _autoStatusRead;

    /// <summary>   The available commands. </summary>
    [ObservableProperty]
    private List<string> _availableCommands;

    /// <summary>   Populates the commands. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [MemberNotNull(nameof(AvailableCommands))]
    private void PopulateCommands()
    {
        this.AvailableCommands = new() {
            Syntax.ClearExecutionStateCommand,
            $"{Syntax.ClearExecutionStateCommand};{Syntax.OperationCompletedQueryCommand}",
            Syntax.IdentityQueryCommand,
            Syntax.OperationCompleteCommand,
            Syntax.OperationCompletedQueryCommand,
            Syntax.OptionsQueryCommand,
            Syntax.ResetKnownStateCommand,
            $"{Syntax.ResetKnownStateCommand};{Syntax.OperationCompletedQueryCommand}",
            $"{String.Format( Syntax.StandardEventEnableCommand, 0x7F )};{Syntax.OperationCompletedQueryCommand}",
            Syntax.StandardEventEnableQueryCommand,
            Syntax.StandardEventStatusQueryCommand,
            $"{String.Format( Syntax.ServiceRequestEnableCommand, 0x7F )};{Syntax.OperationCompletedQueryCommand}",
            $"{String.Format( Syntax.StandardServiceEnableCommand, 0x7F, 0x7F )};{Syntax.OperationCompletedQueryCommand}",
            $"{String.Format( Syntax.StandardServiceEnableCommand, 0x7F, 0x7F )};{Syntax.OperationCompletedQueryCommand}",
            Syntax.ServiceRequestEnableQueryCommand,
            Syntax.ServiceRequestQueryCommand,
            Syntax.WaitCommand,
            $"{Syntax.WaitCommand};{Syntax.OperationCompletedQueryCommand}"
        };
    }

    #endregion

    #region " session relay commands "

    /// <summary>   Determine if we can toggle connection. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   True if we can toggle connection, false if not. </returns>
    public bool CanToggleConnection()
    {
        return this.Connected || !string.IsNullOrEmpty( this.HostAddress ) && NetExplorer.PingPort( this.HostAddress!, this.PortNumber );
    }

    /// <summary>   Toggle connection. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof(CanToggleConnection))]
    public void ToggleConnection()
    {
        this.RestartStopWatch();

        if ( this.Connected )
        {
            if (this.Session is not null )
            {
                this.Session.Disconnect();
                this.Connected = this.Session.Connected;
            }

            if ( this.Session?.ViSession?.GpibLan is not null  )
                this.Session.ViSession.GpibLan.PropertyChanged -= this.HandleGpibLanPropertyChange;

        }
        else
        {
            if ( string.IsNullOrEmpty( this.HostAddress ) )
                throw new InvalidOperationException( $"Empty host address" );

            this.Session = new( this.HostAddress!, this.PortNumber );

            this.Session.Connect();
            this.Connected = this.Session.Connected;

            if ( this.Session?.ViSession?.GpibLan is not null )
                this.Session.ViSession.GpibLan.PropertyChanged += this.HandleGpibLanPropertyChange;

            if ( this.Connected )
                this.Session!.ViSession!.SessionReadTimeout = this.SessionReadTimeout;

        }

        this.ReadStopWatch();
    }

    /// <summary>   Resets the instrument known state. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand(CanExecute = nameof(Connected))]
    public void ResetKnownState()
    {
        this.RestartStopWatch();
        this.Session?.ResetKnownState();
        this.ReadStopWatch();
    }

    /// <summary>   Clears the instrument execution state. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( Connected ) )]
    public void ClearExecutionState()
    {
        this.RestartStopWatch();
        this.Session?.ClearExecutionState();
        this.ReadStopWatch();
    }

    /// <summary>   Determine if we can send a query or a command to the instrument. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   True if we can query, false if not. </returns>
    public bool CanWrite()
    {
        return this.Connected && !string.IsNullOrEmpty( this.QueryMessage );
    }

    /// <summary>   Queries the instrument. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( CanWrite ) )]
    public void Query()
    {
        this.RestartStopWatch();
        this.ReceivedMessage = this.Session?.QueryLine( this.QueryMessage! );
        this.SentMessage = this.QueryMessage!;
        this.ReadStopWatch();
    }

    /// <summary>   Writes a message tot he instrument. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( CanWrite ) )]
    public void Write()
    {
        this.RestartStopWatch();
        _ = this.Session?.WriteLine( this.QueryMessage! );

        this.SentMessage = this.QueryMessage!;

        if ( this.UsingGpibLan() && this.AutoStatusRead )
            this.SerialPollByte = this.Session!.ViSession!.GpibLan!.SerialPoll();

        bool isQuery = this.SentMessage.EndsWith( "?" );
        if ( !isQuery && this.AutoStatusRead )
	    {
            this.StatusByte = this.Session!.QueryServiceRequestStatus();
            this.StandardByte = this.Session!.QueryStandardEventsStatus();
        }

        this.ReadStopWatch();
    }

    /// <summary>   Reads a message from the instrument. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( Connected ) )]
    public void Read()
    {
        this.RestartStopWatch();
        this.ReceivedMessage = this.Session?.Read();
        this.ReadStopWatch();
    }

    /// <summary>   Reads status byte. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( Connected ) )]
    public void ReadStatusByte()
    {
        this.RestartStopWatch();
        this.StatusByte = this.Session!.QueryServiceRequestStatus() ;
        this.ReadStopWatch();
    }

    /// <summary>   Reads standard event status. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( Connected ) )]
    public void ReadStandardEventStatus()
    {
        this.RestartStopWatch();
        this.StatusByte = this.Session!.QueryStandardEventsStatus();
        this.ReadStopWatch();
    }

    #endregion

    #region " gpib lan relay commands "

    /// <summary>   Determines if we can using gpib LAN, which controls the GPIB relay commands. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   True if it succeeds, false if it fails. </returns>
    public bool UsingGpibLan()
    {
        return this.Connected && this.Session!.ViSession!.UsingGpibLan;
    }

    /// <summary>   Selective device clear. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) ) ]
    public void SelectiveDeviceClear()
    {
        this.RestartStopWatch();
        this.Session!.ViSession!.GpibLan!.SelectiveDeviceClear();
        this.ReadStopWatch();
    }

    /// <summary>   Go to local. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void GoToLocal()
    {
        this.RestartStopWatch();
        this.Session!.ViSession!.GpibLan!.GoToLocal();
        this.ReadStopWatch();
    }

    /// <summary>   Local lockout. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void LocalLockout()
    {
        this.RestartStopWatch();
        this.Session!.ViSession!.GpibLan!.GoToLocal();
        this.ReadStopWatch();
    }

    /// <summary>   Sets the Read after write option. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void ReadAfterWriteSet()
    {
        this.RestartStopWatch();
        this.Session!.ViSession!.GpibLan!.ReadAfterWriteEnabledSetter( this.ReadAfterWriteEnabled );
        this.ReadStopWatch();

        // if ( Read-After-Write is enabled, make sure to set the GPIB-Lan class to
        // turn it off on write.
        this.Session.ViSession.GpibLan.DisableReadAfterWriteOnWrite = this.ReadAfterWriteEnabled;
    }

    /// <summary>   Reads after write get. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void ReadAfterWriteGet()
    {
        this.RestartStopWatch();
        this.ReadAfterWriteEnabled = this.Session!.ViSession!.GpibLan!.ReadAfterWriteEnabledGetter();
        this.ReadStopWatch();

        // if ( Read-After-Write is enabled, make sure to set the GPIB-Lan class to
        // turn it off on write.
        this.Session.ViSession.GpibLan.DisableReadAfterWriteOnWrite = this.ReadAfterWriteEnabled;
    }

    /// <summary>   Serial poll. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void SerialPoll()
    {
        this.RestartStopWatch();
        this.SerialPollByte = this.Session!.ViSession!.GpibLan!.SerialPoll();
        this.ReadStopWatch();
    }

    /// <summary>   Queries requesting service. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void QueryRequestingService()
    {
        this.RestartStopWatch();
        this.RequestingService= this.Session!.ViSession!.GpibLan!.ServiceRequested();
        this.ReadStopWatch();
    }

    /// <summary>   Gpib address setter. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void GpibAddressSetter()
    {
        this.RestartStopWatch();
        this.Session!.ViSession!.GpibLan!.GpibAddressSetter( this.GpibAddress );
        this.ReadStopWatch();
    }

    /// <summary>   Gpib address getter. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void GpibAddressGetter()
    {
        this.RestartStopWatch();
        this.GpibAddress = this.Session!.ViSession!.GpibLan!.GpibAddressesGetter().Primary;
        this.ReadStopWatch();
    }


    /// <summary>   Read timeout setter. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void ReadTimeoutSetter()
    {
        this.RestartStopWatch();
        this.Session!.ViSession!.GpibLan!.ReadTimeoutSetter( this.GpibLanReadTimeout );
        this.ReadStopWatch();
    }

    /// <summary>   Read timeout getter. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( UsingGpibLan ) )]
    public void ReadTimeoutGetter()
    {
        this.RestartStopWatch();
        this.GpibLanReadTimeout = this.Session!.ViSession!.GpibLan!.ReadTimeoutGetter();
        this.ReadStopWatch();
    }

    #endregion

    #region " tcp session event handlers "

    /// <summary>   Handles the <see cref="Ieee488Session.ConnectionChanged"/> event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">  Reference to the <see cref="ConnectionChangedEventArgs"/> event
    ///                             arguments. </param>
    private void Session_ConnectionChanged( object sender, ConnectionChangedEventArgs eventArgs )
    {

        if ( sender is null || eventArgs is null ) return;

        this.SocketAddress = eventArgs.Connected
                                ? this.Session!.ViSession!.SocketAddress
                                : string.Empty;

        if ( this.UsingGpibLan() )
            this.ReadAfterWriteEnabled = this.Session!.ViSession!.GpibLan!.ReadAfterWriteEnabled;
    }

    /// <summary>   Handles the <see cref="Ieee488Session.ConnectionChanging"/> event. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="sender">       Source of the event. </param>
    /// <param name="eventArgs">  Reference to the <see cref="ConnectionChangingEventArgs"/> event
    ///                             arguments. </param>
    private void Session_ConnectionChanging( object sender, ConnectionChangingEventArgs eventArgs )
    {
        if ( sender is null || eventArgs is null ) return;
    }

    /// <summary>   Event handler. Called by Session for event handler exception events. </summary>
    /// <remarks>   2023-08-15. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Thread exception event information. </param>
    private void Session_EventHandlerException( object sender, ThreadExceptionEventArgs e )
    {
        if ( e is not null && e.Exception is not null )
        {
            this.LastErrorMessage = e.Exception.ToString();
            if ( e.Exception.Data?.Count > 0 )
            {
                System.Text.StringBuilder builder = new();
                foreach ( System.Collections.DictionaryEntry keyValuePair in e.Exception.Data )
                {
                    _ = builder.AppendLine( $"Data: {keyValuePair.Key}: {keyValuePair.Value}" );
                }
                this.LastErrorMessage += e.Exception.Data;
            }
        }

    }

    #endregion

    #region " stopwatch "

    /// <summary>   Gets or sets the stopwatch. </summary>
    /// <value> The stopwatch. </value>
    private Stopwatch Stopwatch { get; set; }

    /// <summary>   Restarts the stop watch and clears the elapsed time cell. </summary>
    public void RestartStopWatch()
    {
        this.ElapsedTime = 0;
        this.ElapsedTimeLabel = String.Empty;
        this.Stopwatch.Restart();
    }

    /// <summary>   Reads the stop watch and updates the elapsed time cell. </summary>
    public void ReadStopWatch()
    {
        this.ElapsedTimeLabel = String.Format( this.ElapsedTimeFormat, this.Stopwatch.ElapsedMilliseconds );
    }

    #endregion

    #region " event handlers "

    private void HandleGpibLanPropertyChange( object sender, PropertyChangedEventArgs e )
    {
        if ( sender is null || e is null ) return;

        if ( e.PropertyName == nameof(GpibLanController.ReadAfterWriteEnabled) )
        {
            this.ReadAfterWriteEnabled = this.Session?.ViSession?.GpibLan?.ReadAfterWriteEnabled ?? false;
        }
    }

    #endregion

}
