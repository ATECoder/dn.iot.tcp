namespace cc.isr.Iot.Tcp.Client.Ieee488.MSTest;

/// <summary>   (Unit Test Class) a vi tests. </summary>
/// <remarks>   2023-08-15. </remarks>
[TestClass]
public class ViTests
{
    /// <summary>   Should read identity. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="hostAddress">  The host address. </param>
    /// <param name="portNumber">   The port number. </param>
    /// <param name="model">        The model. </param>
    [DataTestMethod]
    [DataRow(  "192.168.0.144", 5025, "7510", DisplayName = "7510: 192.168.0.144:5025" )]
    [DataRow( "192.168.0.252", 1234, "2700", DisplayName = "2700: 192.168.0.252:1234" )]
    public void ShouldReadIdentity( string hostAddress, int portNumber, string model )
    {

        ViViewModel viewModel = new ( ) {
            HostAddress = hostAddress,
            PortNumber = portNumber,
            ElapsedTimeFormat = "0.0"
        };

        if ( !viewModel.CanToggleConnection() )
            Assert.Inconclusive( $"View model {nameof(ViViewModel)}.{nameof(ViViewModel.CanToggleConnection )} is false indicating that no listener was found at {hostAddress}:{portNumber}." );

        if ( !NetExplorer.PingPort( hostAddress, portNumber ) )
            Assert.Inconclusive( $"instrument not found at {hostAddress}:{portNumber}." );

        Assert.IsFalse( viewModel.Connected, $"View model {nameof( ViViewModel )}.{nameof( ViViewModel.Connected )} should be false." );

        viewModel.ToggleConnection();

        Assert.IsTrue( viewModel.Connected, $"View model {nameof( ViViewModel )}.{nameof( ViViewModel.Connected )} should be true." );

        // start with connect/disconnect test:
        viewModel.QueryMessage = Syntax.IdentityQueryCommand;
        viewModel.QueryCommand.Execute(null);
        string? identity = viewModel.ReceivedMessage;

        Assert.IsTrue( string.IsNullOrEmpty( viewModel.LastErrorMessage ),
                $"Identity should read without exception. However, an exception was reported: {viewModel.LastErrorMessage}." );

        Assert.IsFalse( string.IsNullOrEmpty( identity ),
                $"Identity should have a value." );

        Assert.IsTrue( identity.Contains( model),
                $"Identity '{identity}' should contains the '{model}' model name." );

    }
}
