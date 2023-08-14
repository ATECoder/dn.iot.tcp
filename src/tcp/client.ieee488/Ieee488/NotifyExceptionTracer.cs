namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A notify exception tracer. </summary>
/// <remarks>   2023-08-14. </remarks>
public class NotifyExceptionTracer : IExceptionTracer
{

    /// <summary>   Event queue for all listeners interested in TraceException events. </summary>
    public event EventHandler<ThreadExceptionEventArgs>? TraceException;

    /// <summary>   Traces the given exception. </summary>
    /// <remarks>   2023-08-14. </remarks>
    /// <param name="exception">    The exception. </param>
    public void Trace( Exception exception )
    {
        var handler = TraceException;
        handler?.Invoke( this, new ThreadExceptionEventArgs( exception );
    }
}
