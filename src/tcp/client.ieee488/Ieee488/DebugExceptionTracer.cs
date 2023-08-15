using System.Diagnostics;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   An exception tracer that users the debugger write line to report the exception. </summary>
/// <remarks>   2023-08-12. </remarks>
internal class DebugExceptionTracer : IExceptionTracer
{
    /// <summary>   Traces the given exception. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="exception">    The exception. </param>
    public void Trace( Exception exception )
    {
        Debug.WriteLine(exception.ToString());
    }
}
