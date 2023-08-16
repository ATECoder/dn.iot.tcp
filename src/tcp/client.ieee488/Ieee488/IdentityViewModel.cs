using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A ViewModel for the identity query. </summary>
/// <remarks>   2023-08-14. </remarks>
public partial class IdentityViewModel: ObservableObject
{

    #region " observable properties "

    /// <summary>   Number of repeats. </summary>
    [ObservableProperty]
    private int _repeatCount;

    /// <summary>   The host address. </summary>
    [ObservableProperty]
    private string? _hostAddress;

    /// <summary>   The port number. </summary>
    [ObservableProperty]
    private int _portNumber;

    /// <summary>   The receive timeout. </summary>
    [ObservableProperty]
    private int _receiveTimeout;

    /// <summary>   Message describing the error. </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>   The socket address. </summary>
    [ObservableProperty]
    private string? _socketAddress;

    /// <summary>   True if connected. </summary>
    [ObservableProperty]
    private bool _connected;

    /// <summary>   Message describing the sent. </summary>
    [ObservableProperty]
    private string? _sentMessage;

    /// <summary>   Length of the received message. </summary>
    [ObservableProperty]
    private string? _receivedMessageLength;

    /// <summary>   Message describing the received. </summary>
    [ObservableProperty]
    private string? _receivedMessage;

    /// <summary>   The status byte. </summary>
    [ObservableProperty]
    private string? _statusByte;

    [ObservableProperty]
    private string? _averageElapsedTime;

    /// <summary>   The elapsed time. </summary>
    [ObservableProperty]
    private string? _elapsedTime;

    /// <summary>   The elapsed time format. </summary>
    [ObservableProperty]
    private string? _elapsedTimeFormat;

    /// <summary>   The identity. </summary>
    [ObservableProperty]
    private string? _identity;

    [ObservableProperty]
    private bool _useViSession;

    #endregion

    #region " event handlers "

    /// <summary>   Event handler. Called by ViSession for event handler exception events. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="sender">   Source of the event. </param>
    /// <param name="e">        Thread exception event information. </param>
    private void ViSession_EventHandlerException( object sender, ThreadExceptionEventArgs e )
    {
        if ( e is not null && e.Exception is not null )
            this.ErrorMessage = e.Exception.ToString();
    }

    #endregion

    #region " commands "

    /// <summary>   Determine if we can read identity. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <returns>   True if we can read identity, false if not. </returns>
    public bool CanReadIdentity()
    {
        return NetExplorer.PingPort( this.HostAddress ?? string.Empty, this.PortNumber );
    }

    /// <summary>   Reads the identity. </summary>
    /// <remarks>   2023-08-14. </remarks>
    [RelayCommand( CanExecute = nameof( CanReadIdentity ) )]
    public void ReadIdentity()
    {

        Ieee488Session? session = null;
        try
        {
            this.SocketAddress = string.Empty;
            this.Connected = false;
            this.SentMessage = string.Empty;
            this.ReceivedMessageLength = string.Empty;
            this.ReceivedMessage = string.Empty;
            this.AverageElapsedTime = string.Empty;
            this.ElapsedTime = string.Empty;
            this.ErrorMessage = string.Empty;

            Stopwatch stopper = new();

            if ( string.IsNullOrEmpty( this.HostAddress ) )
                throw new InvalidOperationException( $"Empty host address" );


            session = new( this.HostAddress!, this.PortNumber );
            session.EventHandlerException+= this.ViSession_EventHandlerException;

            // open the connection

            session.Connect();

            // report the connection state
            this.Connected = session.Connected;
            this.SocketAddress = session!.ViSession!.SocketAddress;


            double totalMilliseconds = 0;
            if ( this.RepeatCount > 0 && session.Connected )
            {

                this.SentMessage = "*IDN?";

                int loopCount = 0;
                while ( loopCount < this.RepeatCount )
                {
                    loopCount++;
                    stopper.Restart();

                    this.Identity = this.UseViSession
                                        ? session.ViSession.QueryLine( this.SentMessage )
                                        : session.QueryLine( this.SentMessage );
                    totalMilliseconds += stopper.ElapsedMilliseconds;

                    this.ReceivedMessage = this.Identity;
                    this.ReceivedMessageLength = (this.Identity?.Length ?? 0).ToString();
                }

                this.AverageElapsedTime = String.Format( this.ElapsedTimeFormat , totalMilliseconds / loopCount ) + " ms";
                this.ElapsedTime = String.Format( this.ElapsedTimeFormat, totalMilliseconds ) + " ms";
            }
            else this.ReceivedMessage = this.RepeatCount <= 0
                ? "testing connect and disconnect; disconnecting..."
                : "connection failed without reporting an exception";

        }
        catch ( Exception ex )
        {
            this.ErrorMessage = $"Reading identity failed using Tcp session. {ex}";
        }
        finally
        {
            if ( session?.Connected ?? false )
                session.Disconnect();
            session = null;
        }
    }

    #endregion

}
