using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   A debug tracer. </summary>
/// <remarks>   2023-08-12. </remarks>
internal class DebugTracer : IExceptionTracer
{
    /// <summary>   Traces the given exception. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="exception">    The exception. </param>
    public void Trace( Exception exception )
    {
        Debug.WriteLine(exception.ToString());
    }
}
