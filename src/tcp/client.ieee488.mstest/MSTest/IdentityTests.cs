namespace cc.isr.Iot.Tcp.Client.Ieee488.MSTest;

[TestClass]
public class IdentityTests
{
    [DataTestMethod]
    [DataRow(  "192.168.0.252", 5025, DisplayName = "192.168.0.252:5025" )]
    [DataRow( "192.168.0.252", 1234, DisplayName = "192.168.0.252:1234" )]
    public void ShouldReadIdentity( string hostAddress, int portNumber )
    {

        IdentityViewModel viewModel = new IdentityViewModel();
        viewModel.HostAddress = hostAddress;
        viewModel.PortNumber = portNumber;

        if ( !viewModel.CanReadIdentity() )
            Assert.Inconclusive( $"View model {nameof(IdentityViewModel.CanReadIdentity)} is false indicating that the instrument was not found at {hostAddress}:{portNumber}." );

        if ( !NetExplorer.PingPort( hostAddress, portNumber ) )
            Assert.Inconclusive( $"instrument not found at {hostAddress}:{portNumber}." );

        // start with a single trial:

        viewModel.RepeatCount = 1;

        try
        {
            viewModel.ReadIdentity();
        } catch ( Exception ex )
        {
            Assert.Fail( ex.ToString() );
        }

    }
}
