// See https://aka.ms/new-console-template for more information
using System.Runtime.ConstrainedExecution;

using cc.isr.Iot.Tcp.Session.Helper;

InstrumentId instrumentId = InstrumentId.K2700P;

if ( args is not null && args.Any() && string.Equals( "--instrument", args[0] ) )
{
    string instrument = args[1];
    if ( !string.IsNullOrEmpty( instrument ) )
    {
        foreach ( InstrumentId id in Enum.GetValues( typeof( InstrumentId ) ) )
        {
            if ( string.Equals( instrument, id.ToString() ) )
            {
                instrumentId = id;
                break;
            }
        }
    }
}
Console.WriteLine( $"Talking to {instrumentId}. Press any key to stop..." );
Random rnd = new( DateTime.Now.Second );

while ( !Console.KeyAvailable )
{
    if ( rnd.NextDouble() > 0.5 )
        Console.Write( SessionManager.QueryIdentity( instrumentId, TimeSpan.FromMilliseconds( 100 ), false ) );
    else
    {
        Console.WriteLine( "async:" );
        Console.Write( SessionManager.QueryIdentity( instrumentId, TimeSpan.FromMilliseconds( 100 ), true ) );
    }
    Thread.Sleep( 100 );
}

