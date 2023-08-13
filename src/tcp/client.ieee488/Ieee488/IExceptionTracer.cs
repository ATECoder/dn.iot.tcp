
namespace cc.isr.Iot.Tcp.Client.Ieee488;

/// <summary>   An exception tracer interface. </summary>
/// <remarks>   2023-08-12. </remarks>
public interface IExceptionTracer
{
    /// <summary>   Traces the given exception. </summary>
    /// <remarks>   2023-08-12. </remarks>
    /// <param name="exception">    The exception. </param>
    public void Trace( Exception exception );

}
