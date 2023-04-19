// See https://aka.ms/new-console-template for more information
using cc.isr.Iot.Tcp.Session.Helper;

InstrumentId instrumentId = InstrumentId.K7510;
Console.WriteLine( $"Talking to {instrumentId}. Press any key to stop..." );
Random rnd = new Random( DateTime.Now.Second );

while ( true )
{
    if ( rnd.NextDouble() > 0.5 )
        Console.Write( SessionManager.QueryIdentity( instrumentId, TimeSpan.FromMilliseconds( 10 ) ) );
    else
        Console.WriteLine( "async:" );
        Console.Write(  SessionManager.QueryIdentityAsync( instrumentId, TimeSpan.FromMilliseconds( 10 ) ) );
    Thread.Sleep( 100 );
    if ( Console.KeyAvailable ) break;
}

