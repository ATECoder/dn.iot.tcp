namespace cc.isr.Iot.Tcp.Session.MSTest;

[TestClass]
[TestCategory( "dmm2700P" )]
public class Dmm2700PrologixTests
{

    /// <summary>   Assert identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="repeatCount">  Number of repeats. </param>
    private static void AssertIdentityShouldQuery( string ipv4Address, int portNumber, int repeatCount )
    {
        using TcpSession session = new ( ipv4Address, portNumber );
        string identity = string.Empty;
        string command = "*IDN?";
        bool trimEnd = true;
        session.Connect( true, command, ref identity, trimEnd );
        Assert.IsTrue( identity.Contains( "2700" ) );
        session.SendTimeout = TimeSpan.FromMilliseconds( 1000 );
        int count = repeatCount;
        while ( repeatCount > 0 )
        {
            repeatCount--;
            string response = string.Empty;
            _ = session.QueryLine( command, 1024, ref response, trimEnd ); 
            Assert.AreEqual( identity, response, $"@count = {count - repeatCount}" );
        }
    }

    /// <summary>   (Unit Test Method) identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    [TestMethod]
    public void IdentityShouldQuery()
    {
        string ipv4Address = "192.168.0.252";
        int portNumber = 1234;
        int count = 42;
        Dmm2700PrologixTests.AssertIdentityShouldQuery(ipv4Address, portNumber, count );
    }
}
