using System.ComponentModel;

namespace cc.isr.Iot.Tcp.Session.MSTest;

/// <summary>   (Unit Test Class) a TCP session echo server asynchronous tests. </summary>
/// <remarks>   2023-05-31. </remarks>
[TestClass]
public class TcpSessionEchoServerAsyncTests
{

    /// <summary> Initializes the test class before running the first test. </summary>
    /// <param name="testContext"> Gets or sets the test context which provides information about
    /// and functionality for the current test run. </param>
    /// <remarks>Use ClassInitialize to run code before running the first test in the class</remarks>
    [ClassInitialize()]
    public static void InitializeTestClass( TestContext testContext )
    {
        try
        {
            string methodFullName =  $"{testContext.FullyQualifiedTestClassName}.{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name}";
            Console.WriteLine( methodFullName );
            _classTestContext = testContext;
            System.Diagnostics.Debug.WriteLine( $"{_classTestContext.FullyQualifiedTestClassName}.{System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Name} Tester" );
            _server = new TcpEchoServer( _ipv4Address!, _portNumber!.Value );
            _server.PropertyChanged += OnServerPropertyChanged;
            Console.WriteLine( $"{nameof( TcpEchoServer )} is {_server.Listening:'running';'limbo';'idle'}" );
            // start listening.
            _server.Start();
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Failed initializing the test class: {ex}" );

            // cleanup to meet strong guarantees

            try
            {
                CleanupTestClass();
            }
            finally
            {
            }
        }
    }

    /// <summary>
    /// Gets or sets the test context which provides information about and functionality for the
    /// current test run.
    /// </summary>
    /// <value> The test context. </value>
    public TestContext? TestContext { get; set; }

    private static TestContext? _classTestContext;

    /// <summary> Cleans up the test class after all tests in the class have run. </summary>
    /// <remarks> Use <see cref="CleanupTestClass"/> to run code after all tests in the class have run. </remarks>
    [ClassCleanup()]
    public static void CleanupTestClass()
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
            string response = string.Empty;
            _ = session.QueryLine( command, 1024 , ref response, trimEnd ); 
            Assert.AreEqual( identity, response, $"@count = {count - repeatCount}" );
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
