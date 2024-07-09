using System.Security.Principal;

namespace cc.isr.Iot.Tcp.Client.Ieee488.MSTest;

/// <summary>   (Unit Test Class) an identity tests. </summary>
/// <remarks>   2023-08-14. </remarks>
[TestClass]
public class IdentityTests
{
    #region " construction and cleanup "

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

        IdentityViewModel viewModel = new() {
            HostAddress = hostAddress,
            PortNumber = portNumber,
            ElapsedTimeFormat = "0.0"
        };

        if ( !viewModel.CanReadIdentity() )
            Assert.Inconclusive( $"View model {nameof(IdentityViewModel.CanReadIdentity)} is false indicating that no listeners were found at {hostAddress}:{portNumber}." );

        if ( !NetExplorer.PingPort( hostAddress, portNumber ) )
            Assert.Inconclusive( $"instrument not found at {hostAddress}:{portNumber}." );

        // start with connect/disconnect test:

        viewModel.RepeatCount = 0;

        viewModel.ReadIdentity();

        Assert.IsTrue( string.IsNullOrEmpty( viewModel.ErrorMessage ),
                $"Identity should read without exception. However, an exception was reported: {viewModel.ErrorMessage}." );

        viewModel.RepeatCount = 1;

        viewModel.ReadIdentity();

        Assert.IsTrue( string.IsNullOrEmpty( viewModel.ErrorMessage ),
                $"Identity should read without exception. However, an exception was reported: {viewModel.ErrorMessage}." );

        string? identity = viewModel.Identity;

        Assert.IsFalse( string.IsNullOrEmpty( identity ),
                $"Identity should have a value." );

        Assert.IsTrue( identity.Contains( model),
                $"Identity '{identity}' should contains the '{model}' model name." );

    }
}
