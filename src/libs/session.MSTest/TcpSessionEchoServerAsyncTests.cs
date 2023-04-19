using System.ComponentModel;

namespace cc.isr.Iot.Tcp.Session.MSTest;

[TestClass]
public class TcpSessionEchoServerAsyncTests
{

    [ClassInitialize]
    public static void InitializeFixture( TestContext context )
    {
        try
        {
            System.Diagnostics.Debug.WriteLine( $"{context.FullyQualifiedTestClassName}.{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name} Tester" );
            _classTestContext = context;
            System.Diagnostics.Debug.WriteLine( $"{_classTestContext.FullyQualifiedTestClassName}.{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name} Tester" );
            _server = new TcpEchoServer( _ipv4Address!, _portNumber!.Value );
            _server.PropertyChanged += OnServerPropertyChanged;
            Console.WriteLine( $"{nameof( TcpEchoServer )} is {_server.Listening:'running';'limbo';'idle'}" );
            // start listening.
            _server.Start();
        }
        catch ( Exception ex )
        {
            Console.WriteLine( ex.ToString() );
            CleanupFixture();
        }
    }

    public TestContext? TestContext { get; set; }

    private static TestContext? _classTestContext;

    [ClassCleanup]
    public static void CleanupFixture()
    {
        if ( _server is not null )
        {
            if ( _server.Listening)
            {
                // stop listening.
                _server.Stop();
            }
            _server = null;
        }
    }

    private static readonly string? _ipv4Address = "127.0.0.1";
    private static readonly int? _portNumber = 13000;

    private static TcpEchoServer? _server;

    private static void OnServerPropertyChanged( object? sender, PropertyChangedEventArgs args )
    {
        switch ( args.PropertyName )
        {
            case nameof( TcpEchoServer.Message ):
                Console.WriteLine( _server?.Message );
                break;
            case nameof( TcpEchoServer.Port ):
                Console.WriteLine( $"{args.PropertyName} set to {_server?.Port}" );
                break;
            case nameof( TcpEchoServer.IPv4Address ):
                Console.WriteLine( $"{args.PropertyName} set to {_server?.IPv4Address}" );
                break;
            case nameof( TcpEchoServer.Listening ):
                Console.WriteLine( $"{args.PropertyName} set to {_server?.Listening}" );
                break;
        }
    }

    /// <summary>   Assert identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    /// <param name="ipv4Address">  The IPv4 address. </param>
    /// <param name="repeatCount">  Number of repeats. </param>
    private static void AssertIdentityShouldQuery( string ipv4Address, int? portNumber, int repeatCount )
    {
        using TcpSession session = new ( ipv4Address, portNumber.GetValueOrDefault(13000) );
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
            _ = session.QueryLine( command, 1024 , ref respnonse, trimEnd ); 
            Assert.AreEqual( identity, respnonse, $"@count = {count - repeatCount}" );
        }
    }

    /// <summary>   (Unit Test Method) identity should query. </summary>
    /// <remarks>   2022-11-16. </remarks>
    [TestMethod]
    public void IdentityShouldQuery()
    {
        int count = 42;
        AssertIdentityShouldQuery(_ipv4Address!, _portNumber, count );
    }
}
