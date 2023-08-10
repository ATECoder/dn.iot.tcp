using System.Net.Sockets;

namespace cc.isr.Iot.Tcp.Session.MSTest;

[TestClass]
[TestCategory( "dmm2700P" )]
public class Dmm2700PrologixTests
{

    const int _prologixPortNo = 1234;

    const int _prologixWaitInterval = 5;

    /// <summary>   Assert identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="repeatCount">  Number of repeats. </param>
    private static void AssertIdentityShouldQuery( string ipv4Address, int portNumber, int repeatCount )
    {

        string command = string.Empty;

        using TcpSession session = new ( ipv4Address, portNumber );
        session.Connect();

        Assert.IsTrue( session.Connected, " the session should be connected." );

        try
        {

            if ( portNumber == _prologixPortNo )
            {

                /* set auto read after write
                   Prologix GPIB-ETHERNET controller can be configured to automatically address
                   instruments to talk after sending them a command in order to read their response. The
                   feature called, Read-After-Write, saves the user from having to issue read commands
                   repeatedly. */

                command = "++auto 1";

                /* send the command, which may cause Query Unterminated because we are setting the device to talk
                   where there is nothing to talk. */

                session.WriteLine( command );

                // wait for the command to process.

                Thread.Sleep( _prologixWaitInterval );

                // disable front panel operation of the currently addressed instrument.

                session.WriteLine( "++llo" );

                Thread.Sleep( _prologixWaitInterval );

                /* clear errors if any so as to leave the instrument without errors.
                   here we add *OPC? to prevent the query unterminated error. */

                session.WriteLine( "*CLS; *OPC?" );
                Thread.Sleep( _prologixWaitInterval );
                string reply = string.Empty;
                int readCount = session.Read( 1024, ref reply, true );

                // note the the GPIB-Lan device appends CR and LF character and we are trimming only the LF.
                Assert.AreEqual( 2, readCount, "Operation completed reply of a single character is expected" );

                reply.TrimEnd( '\r' );
                Assert.AreEqual( "1", reply, "Operation completed reply is expected" );

            }

            string identity = string.Empty;
            command = "*IDN?";
            bool trimEnd = true;

            _ = session.QueryLine( command, 1024, ref identity, trimEnd );
            Assert.IsTrue( identity.Contains( "2700" ), "Identity should contain '2700'" );

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
        catch ( Exception )
        {

            throw;
        }
        finally
        {
            if ( session.Connected )
            {

                /* clear errors if any so as to leave the instrument without errors.
                   here we add *OPC? to prevent the query unterminated error. */
                session.WriteLine( "*CLS; *OPC?" );
                Thread.Sleep( _prologixWaitInterval );
                string reply = string.Empty;
                _ = session.Read( 1024, ref reply, true );

                if ( portNumber == _prologixPortNo )
                {

                    // enable front panel operation of the currently addressed instrument.

                    _ = session.WriteLine( "++loc" );

                    Thread.Sleep( _prologixWaitInterval );

                    //  send the command to set the interface to listen with read after write set to false.

                    _ = session.WriteLine( "++auto 0" );

                    Thread.Sleep( _prologixWaitInterval );

                }

                session.Disconnect();

            }

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
