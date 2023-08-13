using System.Diagnostics;

namespace cc.isr.Iot.Tcp.Client;

/// <summary> Includes extensions for <see cref="TimeSpan"/> calculations. </summary>
/// <remarks> Requires: DispatcherExtensions; Reference to Windows Base DLL. <para>
/// (c) 2018 Integrated Scientific Resources, Inc. All rights reserved.</para><para>
/// Licensed under The MIT License.</para> </remarks>
public static class TimeSpanExtensionMethods
{

    #region " equals "

    /// <summary>
    /// A TimeSpan extension method that checks if the two timespan values are equal within
    /// <paramref name="epsilon"/>.
    /// </summary>
    /// <remarks>   David, 2020-11-25. </remarks>
    /// <param name="leftHand">     The leftHand to act on. </param>
    /// <param name="rightHand">    The right hand. </param>
    /// <param name="epsilon">      The epsilon. </param>
    /// <returns>   True if it succeeds, false if it fails. </returns>
    public static bool Approximates( this TimeSpan leftHand, TimeSpan rightHand, TimeSpan epsilon )
    {
        return Math.Abs( leftHand.Subtract( rightHand ).Ticks ) <= epsilon.Ticks;
    }

    #endregion

    #region " exact times "

    /// <summary> Gets or sets the microseconds per tick. </summary>
    /// <value> The microseconds per tick. </value>
    public static double MicrosecondsPerTick { get; private set; } = 1000000.0d / Stopwatch.Frequency;

    /// <summary> Gets or sets the millisecond per tick. </summary>
    /// <value> The millisecond per tick. </value>
    public static double MillisecondsPerTick { get; private set; } = 1000.0d / Stopwatch.Frequency;

    /// <summary> Gets or sets the seconds per tick. </summary>
    /// <value> The seconds per tick. </value>
    public static double SecondsPerTick { get; private set; } = 1.0d / TimeSpan.TicksPerSecond;

    /// <summary> Gets or sets the ticks per microseconds. </summary>
    /// <value> The ticks per microseconds. </value>
    public static double TicksPerMicroseconds { get; private set; } = 0.001d * TimeSpan.TicksPerMillisecond;

    /// <summary> Converts seconds to time span with tick timespan accuracy. </summary>
    /// <remarks>
    /// <code>
    /// Dim actualTimespan As TimeSpan = TimeSpan.Zero.FromSecondsPrecise(42.042)
    /// </code>
    /// </remarks>
    /// <param name="seconds"> The number of seconds. </param>
    /// <returns> A TimeSpan. </returns>
    public static TimeSpan FromSeconds( this double seconds )
    {
        return TimeSpan.FromTicks( ( long ) (TimeSpan.TicksPerSecond * seconds) );
    }

    /// <summary> Converts a timespan to the seconds with tick timespan accuracy. </summary>
    /// <remarks> David, 2020-09-15. </remarks>
    /// <param name="timespan"> The timespan. </param>
    /// <returns> Timespan as a Double. </returns>
    public static double ToSeconds( this TimeSpan timespan )
    {
        return timespan.Ticks * SecondsPerTick;
    }

    /// <summary> Converts milliseconds to time span with tick timespan accuracy. </summary>
    /// <remarks>
    /// <code>
    /// Dim actualTimespan As TimeSpan = TimeSpan.Zero.FromMillisecondsPrecise(42.042)
    /// </code>
    /// </remarks>
    /// <param name="milliseconds"> The number of milliseconds. </param>
    /// <returns> A TimeSpan. </returns>
    public static TimeSpan FromMilliseconds( this double milliseconds )
    {
        return TimeSpan.FromTicks( ( long ) (TimeSpan.TicksPerMillisecond * milliseconds) );
    }

    /// <summary> Converts a timespan to an exact milliseconds with tick timespan accuracy. </summary>
    /// <remarks> David, 2020-09-15. </remarks>
    /// <param name="timespan"> The timespan. </param>
    /// <returns> Timespan as a Double. </returns>
    public static double ToMilliseconds( this TimeSpan timespan )
    {
        return timespan.Ticks * MillisecondsPerTick;
    }

    /// <summary> Converts microseconds to time span with tick timespan accuracy. </summary>
    /// <remarks>
    /// <code>
    /// Dim actualTimespan As TimeSpan = TimeSpan.Zero.FromMicroseconds(42.2)
    /// </code>
    /// </remarks>
    /// <param name="microseconds"> The value. </param>
    /// <returns> A TimeSpan. </returns>
    public static TimeSpan FromMicroseconds( this double microseconds )
    {
        return TimeSpan.FromTicks( ( long ) (TicksPerMicroseconds * microseconds) );
    }

    /// <summary> Converts a timespan to an exact microseconds with tick timespan accuracy. </summary>
    /// <remarks> David, 2020-09-15. </remarks>
    /// <param name="timespan"> The timespan. </param>
    /// <returns> Timespan as a Double. </returns>
    public static double ToMicroseconds( this TimeSpan timespan )
    {
        return timespan.Ticks * MicrosecondsPerTick;
    }

    /// <summary>
    /// A TimeSpan extension method that adds the microseconds to 'microseconds'.
    /// </summary>
    /// <remarks>   David, 2020-12-07. </remarks>
    /// <param name="self">         The self to act on. </param>
    /// <param name="microseconds"> The value. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan AddMicroseconds( this TimeSpan self, double microseconds )
    {
        return self.Add( TimeSpan.FromTicks( ( long ) (microseconds * TicksPerMicroseconds) ) );
    }

    #endregion

    #region " delay: encapsulation of asynchronous wait  "

    /// <summary>
    /// Delays operations by the given delay time selecting the delay clock which resolution exceeds
    /// 0.2 times the delay time. T.
    /// </summary>
    /// <remarks> David, 2020-09-15. </remarks>
    /// <param name="delayMilliseconds"> The delay in milliseconds. </param>
    public static void AsyncDelay( double delayMilliseconds )
    {
        TimeSpanExtensionMethods.AsyncDelay( TimeSpanExtensionMethods.FromMilliseconds( delayMilliseconds ) );
    }

    /// <summary>   Delays operations by the given delay time on another thread. </summary>
    /// <remarks>   David, 2020-09-15. </remarks>
    /// <param name="duration"> The duration. </param>
    public static void AsyncDelay( this TimeSpan duration )
    {
        TimeSpanExtensionMethods.AsyncWait( duration );
    }

    #endregion

    #region " async waits w/ do events "

    /// <summary>
    /// A TimeSpan extension method that Lets Windows process all the messages currently in the
    /// message queue during a wait by invoking the wait on a task.
    /// </summary>
    /// <remarks>
    /// David, 2020-11-05. <para>
    /// DoEventsWait(1ms)  waits: 00:00:00.0010066s </para><para>
    /// DoEventsWait(2ms)  waits: 00:00:00.0020038s </para><para>
    /// DoEventsWait(5ms)  waits: 00:00:00.0050051s </para><para>
    /// DoEventsWait(10ms) waits: 00:00:00.0100138s </para><para>
    /// DoEventsWait(20ms) waits: 00:00:00.0200103s </para><para>
    /// DoEventsWait(50ms) waits: 00:00:00.0500064s </para><para>
    /// DoEventsWait(100ms) waits: 00:00:00.1000037s </para>
    /// </remarks>
    /// <param name="duration">         The duration. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan AsyncWait( this TimeSpan duration, Action doEventsAction )
    {
        return Stopwatch.StartNew().AsyncLetElapse( duration, TimeSpan.Zero, doEventsAction );
    }

    /// <summary>
    /// Lets Windows process all the messages currently in the message queue during a wait after each
    /// sleep interval.
    /// </summary>
    /// <remarks>   David, 2020-11-20. </remarks>
    /// <param name="duration">         The duration. </param>
    /// <param name="sleepInterval">    The sleep interval between <paramref name="doEventsAction"/>. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan AsyncWait( this TimeSpan duration, TimeSpan sleepInterval, Action doEventsAction )
    {
        return Stopwatch.StartNew().AsyncLetElapse( duration, sleepInterval, doEventsAction );
    }

    /// <summary>   A TimeSpan extension method that starts a wait task. </summary>
    /// <remarks>   David, 2021-01-30. </remarks>
    /// <param name="duration"> The duration. </param>
    /// <param name="yield">    (Optional) True to yield between spin waits. </param>
    /// <returns>   A Task. </returns>
    public static Task StartWaitTask( this TimeSpan duration, bool yield = false )
    {
        return Task.Factory.StartNew( () => SyncWait( duration, yield ) );
    }

    /// <summary>
    /// A TimeSpan extension method that asynchronously waits for a specific duration that is
    /// accurate up to the high resolution clock resolution.
    /// </summary>
    /// <remarks>   David, 2021-01-30. </remarks>
    /// <param name="duration"> The duration. </param>
    /// <param name="yield">    (Optional) True to yield between spin waits. </param>
    public static void AsyncWait( this TimeSpan duration, bool yield = false )
    {
        StartWaitTask( duration, yield ).Wait();
    }

    /// <summary>
    /// A TimeSpan extension method that starts wait task returning the elapsed time.
    /// </summary>
    /// <remarks>   David, 2021-01-30. </remarks>
    /// <param name="duration"> The duration. </param>
    /// <param name="yield">    (Optional) True to yield between spin waits. </param>
    /// <returns>   A TimeSpan Task. </returns>
    public static Task<TimeSpan> StartWaitElpasedTask( this TimeSpan duration, bool yield = false )
    {
        return Task<TimeSpan>.Factory.StartNew( () => { return SyncWait( duration, yield ); } );
    }

    /// <summary>
    /// A TimeSpan extension method that asynchronously waits for a specific duration that is
    /// accurate up to the high resolution clock resolution and returning the elapsed time.
    /// </summary>
    /// <remarks>   David, 2021-01-30. </remarks>
    /// <param name="duration"> The duration. </param>
    /// <param name="yield">    (Optional) True to yield between spin waits. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan AsyncWaitElapsed( this TimeSpan duration, bool yield = false )
    {
        System.Threading.Tasks.Task<TimeSpan> t = StartWaitElpasedTask( duration, yield );
        t.Wait();
        return t.Result;
    }

    #endregion

    #region " async waits until w/ do events "

    /// <summary>
    /// Lets Windows process all the messages currently in the message queue during a wait for a
    /// timeout or a completion of the action as signaled by the predicate polling every loop delay interval.
    /// </summary>
    /// <remarks>   David, 2020-11-16. </remarks>
    /// <param name="timeout">          The maximum wait time. </param>
    /// <param name="loopDelay">        The internal loop delay between
    ///                                 <paramref name="doEventsAction"/>. </param>
    /// <param name="predicate">        A predicate function returning true upon affirmation of a
    ///                                 condition. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) AsyncWaitUntil( this TimeSpan timeout, TimeSpan loopDelay,
                                                                     Func<bool> predicate, Action doEventsAction )
    {
        return Stopwatch.StartNew().AsyncLetElapseUntil( timeout, loopDelay, predicate, doEventsAction );
    }

    /// <summary>   A TimeSpan extension method that starts wait until the predicate indicates that it completed its tasks. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <param name="pollInterval"> The time between <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">    The predicate. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static Task<(bool Completed, TimeSpan Elapsed)> StartWaitUntil( this TimeSpan timeout, TimeSpan pollInterval, Func<bool> predicate )
    {
        return Task<(bool, TimeSpan)>.Factory.StartNew( () => { return Stopwatch.StartNew().SyncLetElapseUntil( timeout, pollInterval, predicate ); } );
    }

    /// <summary>
    /// A TimeSpan extension method that starts wait until the predicate indicates that it completed
    /// its tasks.
    /// </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <param name="pollInterval"> The time between <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">    A function to query completion. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) AsyncWaitUntil( this TimeSpan timeout, TimeSpan pollInterval, Func<bool> predicate )
    {
        System.Threading.Tasks.Task<(bool, TimeSpan)> t = StartWaitUntil( timeout, pollInterval, predicate );
        t.Wait();
        return t.Result;
    }

    /// <summary>
    /// Starts a task waiting for a the predicate or timeout. The task complete after a timeout or if
    /// the predicate signals the completion of the action.
    /// </summary>
    /// <remarks>
    /// The task timeout is included in the task function. Otherwise, upon Wait(timeout), the task
    /// deadlocks attempting to get the task result. For more information see
    /// https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html. That document is short
    /// on examples for how to resolve this issue.
    /// </remarks>
    /// <param name="timeout">          The timeout. </param>
    /// <param name="onsetDelay">       The onset delay; before the first call to
    ///                                 <paramref name="predicate"/> </param>
    /// <param name="pollInterval">     Specifies time between serial polls. </param>
    /// <param name="predicate">        A function to query completion. </param>
    /// <param name="doEventsAction">   The do events action. </param>
    /// <returns>
    /// A Threading.Tasks.Task(Of ( bool, TimeSpan ) ) (true if Completed, elapsed time).
    /// </returns>
    public static Task<(bool, TimeSpan)> StartAwaitingUntilTask( this TimeSpan timeout, TimeSpan onsetDelay, TimeSpan pollInterval,
                                                                 Func<bool> predicate, Action doEventsAction )
    {
        return Task.Factory.StartNew<(bool, TimeSpan)>( () => {
            return Stopwatch.StartNew().SyncLetElapseUntil( timeout, onsetDelay, pollInterval, predicate, doEventsAction );
        } );
    }

    /// <summary>
    /// A TimeSpan extension method that await until a timeout or if the predicate signals the
    /// completion of the action.
    /// </summary>
    /// <remarks>   David, 2021-04-01. </remarks>
    /// <param name="timeout">          The timeout. </param>
    /// <param name="onsetDelay">       The time to wait before starting to query. </param>
    /// <param name="pollInterval">     Specifies time between serial polls. </param>
    /// <param name="predicate">        A function to query completion. </param>
    /// <param name="doEventsAction">   The do events action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) AsyncAwaitUntil( this TimeSpan timeout, TimeSpan onsetDelay, TimeSpan pollInterval,
                                                                      Func<bool> predicate, Action doEventsAction )
    {
        // emulate the reply for disconnected operations.
        var cts = new System.Threading.CancellationTokenSource();
        var t = StartAwaitingUntilTask( timeout, onsetDelay, pollInterval, predicate, doEventsAction );
        t.Wait();
        return t.Result;
    }

    #endregion

    #region " sync wait "

    /// <summary>
    /// A TimeSpan extension method that synchronously waits for a specific time delay that is
    /// accurate up to the high resolution clock resolution.
    /// </summary>
    /// <remarks>   David, 2021-11-13. </remarks>
    /// <param name="duration"> The duration. </param>
    /// <param name="yield">    (Optional) True to yield between <see cref="Thread.Sleep(int)"/>
    ///                         and <see cref="Thread.SpinWait(int)"/>. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan SyncWait( this TimeSpan duration, bool yield = false )
    {
        return Stopwatch.StartNew().SyncLetElapse( duration, yield );
    }

    /// <summary>   A TimeSpan extension method that await until the predicate returns a completion affirmation or timeout. </summary>
    /// <remarks>   David, 2021-04-01. </remarks>
    /// <param name="timeout">          The timeout. </param>
    /// <param name="onsetDelay">       The onset delay; before the first call to. </param>
    /// <param name="loopDelay">        The delay between <paramref name="doEventsAction"/> and
    ///                                 <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">        The predicate. </param>
    /// <param name="doEventsAction">   The do events action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) SyncWaitUntil( this TimeSpan timeout, TimeSpan onsetDelay, TimeSpan loopDelay,
                                                                    Func<bool> predicate, Action doEventsAction )
    {
        return Stopwatch.StartNew().SyncLetElapseUntil( timeout, onsetDelay, loopDelay, predicate, doEventsAction );
    }

    #endregion

}
