using System.Diagnostics;

namespace cc.isr.Iot.Tcp.Client.MSTest;

[TestClass]
[TestCategory( "dmm7510" )]

public class Dmm7510AsyncTests
{

    /// <summary>   Writes a line. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="session">  The session. </param>
    /// <param name="command">  The command. </param>
    /// <returns>   An int. </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage( "CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>" )]
    private static int WriteLine( TcpSession session, string command, CancellationToken token )
    {
        var task = session.WriteLineAsync( command, token );
        task.Wait( token );
        return task.Result;
    }

    /// <summary>   Queries a line. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="session">      The session. </param>
    /// <param name="command">      The command. </param>
    /// <param name="readDelay">    The read delay. </param>
    /// <param name="trimEnd">      True to trim end. </param>
    /// <param name="tokenSource">  The cancellation token source. </param>
    /// <returns>   The line. </returns>
    private static string QueryLine( TcpSession session, string command, TimeSpan readDelay, bool trimEnd, CancellationTokenSource tokenSource )
    {
        var task = session.QueryLineAsync( command, 1024, readDelay, trimEnd, tokenSource );
        task.Wait();
        return task.Result;
    }

    /// <summary>   Assert identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="readDelay">    The read delay. </param>
    /// <param name="repeatCount">  Number of repeats. </param>
    private static void AssertIdentityShouldQuery( string ipv4Address, TimeSpan readDelay, int repeatCount )
    {
        using CancellationTokenSource cancellationTokenSource = new ();    
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
            string response = QueryLine( session, command, readDelay, trimEnd, cancellationTokenSource ); 
            Assert.AreEqual( identity, response, $"@count = {count - repeatCount}" );
        }
    }

    /// <summary>   (Unit Test Method) identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    [TestMethod]
    public void IdentityShouldQuery()
    {
        string ipv4Address = "192.168.0.144";
        int count = 42;
        TimeSpan readDelay = TimeSpan.FromMilliseconds( 0 );
        AssertIdentityShouldQuery(ipv4Address, readDelay, count );
    }

    /// <summary>   (Unit Test Method) session TCP client should begin connect. </summary>
    /// <remarks>   2022-11-16. </remarks>
    [TestMethod]
    public void SessionTcpClientShouldBeginConnect()
    {
        string ipv4Address = "192.168.0.144";
        TimeSpan timeout = TimeSpan.FromMilliseconds( 100 );
        using TcpSession session = new( ipv4Address );
        Stopwatch sw = Stopwatch.StartNew();
        IAsyncResult asyncResult = session.BeginConnect();
        bool success = session.AwaitConnect( asyncResult, TimeSpan.FromMilliseconds( 100 ) );
        Assert.IsTrue( success , $"connected to {ipv4Address} failed after {timeout.TotalMilliseconds:0}ms");
        sw.Stop();
        Console.WriteLine(
            $"Connected in {sw.ElapsedTicks}/{TimeSpan.TicksPerMillisecond} {((double)sw.ElapsedTicks / TimeSpan.TicksPerMillisecond):0.0}ms" );
    }
}
