namespace isr.Iot.Tcp.Session.MSTest;

[TestClass]
[TestCategory( "dmm7510" )]
public class Dmm7510Tests
{

    /// <summary>   Assert identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="repeatCount">  Number of repeats. </param>
    private void AssertIdentityShouldQuery( string ipv4Address, int repeatCount )
    {
        using TcpSession session = new ( ipv4Address );
        string identity = string.Empty;
        string command = "*IDN?";
        bool trimEnd = true;
        session.Connect( true, command, ref identity, trimEnd );
        session.SendTimeout = TimeSpan.FromMilliseconds( 1000 );
        int count = repeatCount;
        while ( repeatCount > 0 )
        {
            repeatCount--;
            string respnonse = string.Empty;
            _ = session.QueryLine( command, 1024, ref respnonse, trimEnd ); 
            Assert.AreEqual( identity, respnonse, $"@count = {count - repeatCount}" );
        }
    }

    /// <summary>   (Unit Test Method) identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    [TestMethod]
    public void IdentityShouldQuery()
    {
        string ipv4Address = "192.168.0.144";
        int count = 42;
        this.AssertIdentityShouldQuery(ipv4Address, count );
    }
}
