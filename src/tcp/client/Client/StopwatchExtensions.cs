using System.Diagnostics;

namespace cc.isr.Iot.Tcp.Client;

/// <summary> Includes extensions for <see cref="Stopwatch">Stop Watch</see>. </summary>
/// <remarks> (c) 2015 Integrated Scientific Resources, Inc. All rights reserved. <para>
/// Licensed under The MIT License.</para><para>
/// David, 2015-03-19, 2.0.5556 </para></remarks>
public static class StopwatchExtensionMethods
{

    /// <summary>   Static constructor. </summary>
    /// <remarks>   David, 2021-02-10. </remarks>
    static StopwatchExtensionMethods()
    {
        StopwatchExtensionMethods.SystemClockResolution = StopwatchExtensionMethods.EstimateSystemClockResolution( StopwatchExtensionMethods.SystemClockResolution );
    }

    #region " constants "

    /// <summary> Gets or sets the microsecond per tick. </summary>
    /// <value> The microsecond per tick. </value>
    public static double MicrosecondPerTick { get; private set; } = 1000000.0d / Stopwatch.Frequency;

    /// <summary> Gets or sets the millisecond per tick. </summary>
    /// <value> The millisecond per tick. </value>
    public static double MillisecondPerTick { get; private set; } = 1000.0d / Stopwatch.Frequency;

    #endregion

    #region " resolutions "

    /// <summary>   (Immutable) the system clock rate. </summary>
    public const int SystemClockRate = 64;

    /// <summary> The system clock resolution. </summary>
    /// <value> The system clock resolution. </value>
    public static TimeSpan SystemClockResolution { get; private set; } = TimeSpan.FromMilliseconds( 1000 / StopwatchExtensionMethods.SystemClockRate );

    /// <summary>   The thread clock resolution. </summary>
    /// <remarks>
    /// This might have changed from previous test results with the upgrade to Windows 20H2. Test
    /// results consistently show the thread sleep resolution at 15.6 ms.
    /// https://StackOverflow.com/questions/7614936/can-i-improve-the-resolution-of-thread-sleep The
    /// Thread.Sleep cannot be expected to provide reliable timing. It is notorious for behaving
    /// differently on different hardware Thread.Sleep(1) could sleep for 15.6 ms.
    /// https://social.msdn.Microsoft.com/Forums/vstudio/en-US/facc2b57-9a27-4049-bb32-ef093fbf4c29/threadsleep1-sleeps-for-156-ms?forum=clr.
    /// </remarks>
    /// <value> The thread clock resolution. </value>
    public static TimeSpan ThreadClockResolution { get; private set; } = TimeSpan.FromMilliseconds( 15.6001 );

    /// <summary> The high resolution clock resolution. </summary>
    /// <value> The high resolution clock resolution. </value>
    public static TimeSpan HighResolutionClockResolution { get; private set; } = TimeSpan.FromSeconds( 1d / Stopwatch.Frequency );

    /// <summary>   Estimate system clock resolution. </summary>
    /// <remarks>   David, 2021-12-16. </remarks>
    /// <param name="guess">        The guess. </param>
    /// <param name="trialCount">   (Optional) Number of trials. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan EstimateSystemClockResolution( TimeSpan guess, int trialCount = 3 )
    {
        TimeSpan resolution = guess;
        for ( int i = 0; i < trialCount; i++ )
        {
            Stopwatch sw = Stopwatch.StartNew();
            Thread.Sleep( 1 );
            sw.Stop();
            if ( resolution < sw.Elapsed )
                resolution = sw.Elapsed;
        }
        return resolution;
    }

    /// <summary>   Estimate system clock resolution. </summary>
    /// <remarks>   David, 2021-12-16. </remarks>
    /// <param name="trialCount">   (Optional) Number of trials. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan EstimateSystemClockResolution( int trialCount = 3 )
    {
        return EstimateSystemClockResolution( TimeSpan.MaxValue, trialCount );
    }

    #endregion

    #region " calculations "

    /// <summary> Elapsed microseconds. </summary>
    /// <remarks> David, 2020-09-15. </remarks>
    /// <param name="stopwatch"> The stop watch. </param>
    /// <returns> A Long. </returns>
    public static double ElapsedMicroseconds( this Stopwatch stopwatch )
    {
        return stopwatch.ElapsedTicks * MicrosecondPerTick;
    }

    /// <summary> Elapsed milliseconds. </summary>
    /// <remarks> David, 2020-09-15. </remarks>
    /// <param name="stopwatch"> The stop watch. </param>
    /// <returns> A Double. </returns>
    public static double ElapsedMilliseconds( this Stopwatch stopwatch )
    {
        return stopwatch.ElapsedTicks * MillisecondPerTick;
    }

    /// <summary>
    /// Query if <see cref="Stopwatch"/> <see cref="Stopwatch.Elapsed"/> time exceeds
    /// <paramref name="timeoutTimespan"/>.
    /// </summary>
    /// <remarks>   David, 2020-09-15. </remarks>
    /// <param name="stopwatch">        The stop watch. </param>
    /// <param name="timeoutTimespan">  The value. </param>
    /// <returns>   <c>true</c> if expired; otherwise <c>false</c> </returns>
    public static bool IsExpired( this Stopwatch stopwatch, TimeSpan timeoutTimespan )
    {
        return stopwatch is object && timeoutTimespan > TimeSpan.Zero && stopwatch.Elapsed > timeoutTimespan;
    }

    #endregion

    #region " async let elapse "

    /// <summary>   Waits while the stop watch is running until its elapsed time expires. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stop watch. </param>
    /// <param name="elapsedTime">  The elapsed time. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan LetElapse( this Stopwatch stopwatch, TimeSpan elapsedTime )
    {
        return stopwatch.AsyncLetElapse( elapsedTime );
    }

    /// <summary>   A Stopwatch extension method that asynchronous let elapse. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stopwatch to act on. </param>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan AsyncLetElapse( this Stopwatch stopwatch, TimeSpan timeout )
    {
        System.Threading.Tasks.Task<TimeSpan> t = StartLetElapse( stopwatch, timeout );
        t.Wait();
        return t.Result;
    }

    /// <summary>   A Stopwatch extension method that starts let elapse. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stopwatch to act on. </param>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <returns>   A TimeSpan </returns>
    public static Task<TimeSpan> StartLetElapse( this Stopwatch stopwatch, TimeSpan timeout )
    {
        return Task<TimeSpan>.Factory.StartNew( () => { return SyncLetElapse( stopwatch, timeout ); } );
    }

    /// <summary>   A Stopwatch extension method that awaits for the specified duration. </summary>
    /// <remarks>   David, 2021-03-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="duration">         The duration. </param>
    /// <param name="loopDelay">        The internal loop delay between
    ///                                 <paramref name="doEventsAction"/>. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>   elapsed time. </returns>
    public static TimeSpan AsyncLetElapse( this Stopwatch stopwatch, TimeSpan duration, TimeSpan loopDelay, Action doEventsAction )
    {
        System.Threading.Tasks.Task<TimeSpan> t = StartLetElapse( stopwatch, duration, loopDelay, doEventsAction );
        t.Wait();
        return t.Result;
    }

    /// <summary>   A Stopwatch extension method that starts let elapse task. </summary>
    /// <remarks>   David, 2021-03-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="duration">         The duration. </param>
    /// <param name="loopDelay">        The internal loop delay between
    ///                                 <paramref name="doEventsAction"/>. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>   A TimeSpan. </returns>
    public static Task<TimeSpan> StartLetElapse( this Stopwatch stopwatch, TimeSpan duration, TimeSpan loopDelay, Action doEventsAction )
    {
        return Task<TimeSpan>.Factory.StartNew( () => { return SyncLetElapse( stopwatch, duration, loopDelay, doEventsAction ); } );
    }

    #endregion

    #region " async waits until w/ do events "

    /// <summary>
    /// Lets Windows process all the messages currently in the message queue during a wait for a
    /// timeout or a completion of the action as signaled by the predicate polling every loop delay interval.
    /// </summary>
    /// <remarks>   David, 2021-03-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="timeout">          The timeout to act on. </param>
    /// <param name="loopDelay">        The internal loop delay between
    ///                                 <paramref name="doEventsAction"/> and
    ///                                 <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">        The predicate. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) AsyncLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan loopDelay,
                                                                          Func<bool> predicate, Action doEventsAction )
    {
        System.Threading.Tasks.Task<(bool, TimeSpan)> t = StartLetElapseUntil( stopwatch, timeout, loopDelay, predicate, doEventsAction );
        t.Wait();
        return t.Result;
    }

    /// <summary>
    /// Lets Windows process all the messages currently in the message queue during a wait for a
    /// timeout or a completion of the action as signaled by the predicate polling every loop delay interval.
    /// </summary>
    /// <remarks>   David, 2021-03-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="timeout">          The timeout to act on. </param>
    /// <param name="loopDelay">        The internal loop delay between
    ///                                 <paramref name="doEventsAction"/> and
    ///                                 <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">        The predicate. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static Task<(bool Completed, TimeSpan Elapsed)> StartLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan loopDelay,
                                                                                Func<bool> predicate, Action doEventsAction )
    {
        return Task<(bool, TimeSpan)>.Factory.StartNew( () => { return SyncLetElapseUntil( stopwatch, timeout, loopDelay, predicate, doEventsAction ); } );
    }

    /// <summary>   A Stopwatch extension method that starts let elapse until. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stopwatch to act on. </param>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <param name="pollInterval"> The time between <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">    A function to query completion. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static Task<(bool Completed, TimeSpan Elapsed)> StartLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan pollInterval, Func<bool> predicate )
    {
        return Task<(bool, TimeSpan)>.Factory.StartNew( () => { return SyncLetElapseUntil( stopwatch, timeout, pollInterval, predicate ); } );
    }

    /// <summary>   A Stopwatch extension method that asynchronous let elapse until. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stopwatch to act on. </param>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <param name="pollInterval"> The time between <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">        A function to query completion. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) AsyncLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan pollInterval, Func<bool> predicate )
    {
        System.Threading.Tasks.Task<(bool, TimeSpan)> t = StartLetElapseUntil( stopwatch, timeout, pollInterval, predicate );
        t.Wait();
        return t.Result;
    }

    #endregion

    #region " sync waits "

    /// <summary>
    /// A stopwatch extension method that synchronously waits for a specific time delay that is
    /// accurate up to the high resolution clock resolution or one of the system clock cycle in case
    /// of duration exceeds <paramref name="clockCycles"/> clock cycles (e.g., up to 15.4 error for
    /// 15.4 ms clock cycle with a duration of over 154 ms.)
    /// </summary>
    /// <remarks>   David, 2021-01-30. </remarks>
    /// <param name="stopWatch">    The stop watch. </param>
    /// <param name="duration">     The duration. </param>
    /// <param name="yield">        (Optional) True to yield between <see cref="Thread.Sleep(int)"/>
    ///                             and <see cref="Thread.SpinWait(int)"/>. </param>
    /// <param name="clockCycles">  (Optional) The clock cycles. </param>
    /// <returns>   The actual wait time. </returns>
    public static TimeSpan SyncLetElapse( this Stopwatch stopWatch, TimeSpan duration, bool yield = false, int clockCycles = 10 )
    {
        var yieldCount = 100;
        var counter = yieldCount;
        int systemClockCycles = ( int ) Math.Floor( ( double ) duration.Ticks / StopwatchExtensionMethods.SystemClockResolution.Ticks );
        if ( systemClockCycles >= clockCycles )
        {
            if ( yield )
            {
                while ( duration.Subtract( stopWatch.Elapsed ) < StopwatchExtensionMethods.SystemClockResolution )
                {
                    var ms = ( int ) Math.Floor( StopwatchExtensionMethods.SystemClockResolution.TotalMilliseconds );
                    Thread.Sleep( ms );
                    _ = Thread.Yield();
                }
            }
            else
            {
                Thread.Sleep( duration );
                _ = Thread.Yield();
            }
        }
        while ( stopWatch.Elapsed < duration )
        {
            if ( counter >= yieldCount )
            {
                Thread.SpinWait( 1 );
                if ( yield )
                {
                    _ = Thread.Yield();
                }
                counter = 0;
            }
            counter += 1;
        }
        return stopWatch.Elapsed;
    }

    /// <summary>   A Stopwatch extension method that synchronizes the let elapse. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stopwatch to act on. </param>
    /// <param name="duration">     The duration. </param>
    /// <returns>   A TimeSpan. </returns>
    public static TimeSpan SyncLetElapse( this Stopwatch stopwatch, TimeSpan duration )
    {
        while ( stopwatch.Elapsed <= duration )
        {
        }
        return stopwatch.Elapsed;
    }

    /// <summary>   A Stopwatch extension method that waits until the predicate signals that it completed its actions or timeout. </summary>
    /// <remarks>   David, 2021-03-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="timeout">          The timeout to act on. </param>
    /// <param name="loopDelay">        The internal loop delay between
    ///                                 <paramref name="doEventsAction"/>. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>   The elapsed time. </returns>
    public static TimeSpan SyncLetElapse( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan loopDelay, Action doEventsAction )
    {
        while ( stopwatch.Elapsed <= timeout )
        {
            if ( loopDelay > TimeSpan.Zero ) _ = Stopwatch.StartNew().SyncLetElapse( loopDelay );
            doEventsAction?.Invoke();
        }
        return stopwatch.Elapsed;
    }
    #endregion

    #region " sync wait until "

    /// <summary>   A timespan extension method that waits until the predicate signals that it completed its actions or timeout. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <param name="loopDelay">    The time between <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">    The predicate. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) SyncWaitUntil( this TimeSpan timeout, TimeSpan loopDelay, Func<bool> predicate )
    {
        return Stopwatch.StartNew().SyncLetElapseUntil( timeout, loopDelay, predicate );
    }

    /// <summary>   A Stopwatch extension method that waits until the predicate signals that it completed its actions or timeout. </summary>
    /// <remarks>   David, 2021-02-16. </remarks>
    /// <param name="stopwatch">    The stopwatch to act on. </param>
    /// <param name="timeout">      The timeout to act on. </param>
    /// <param name="pollInterval"> The time between <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">    The predicate. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) SyncLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan pollInterval,
                                                                         Func<bool> predicate )
    {
        var completed = predicate.Invoke();
        while ( stopwatch.Elapsed <= timeout && !completed )
        {
            if ( pollInterval > TimeSpan.Zero )
                _ = Stopwatch.StartNew().SyncLetElapse( pollInterval );
            completed = predicate.Invoke();
        }
        return (completed, stopwatch.Elapsed);
    }


    /// <summary>
    /// A Stopwatch extension method that await for the specified elapsed time or until the predicate
    /// signals that it completed its actions.
    /// </summary>
    /// <remarks>   David, 2021-03-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="timeout">          The timeout to act on. </param>
    /// <param name="loopDelay">        The delay between <paramref name="doEventsAction"/> and
    ///                                 <paramref name="predicate"/> actions. </param>
    /// <param name="predicate">        The predicate. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) SyncLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout, TimeSpan loopDelay,
                                                                         Func<bool> predicate, Action doEventsAction )
    {
        var completed = predicate.Invoke();
        while ( stopwatch.Elapsed <= timeout && !completed )
        {
            if ( loopDelay > TimeSpan.Zero )
                _ = Stopwatch.StartNew().SyncLetElapse( loopDelay );
            doEventsAction?.Invoke();
            completed = predicate.Invoke();
        }
        return (completed, stopwatch.Elapsed);
    }

    /// <summary>
    /// A Stopwatch extension method that await for the specified elapsed time or until the predicate
    /// signals that it completed its actions.
    /// </summary>
    /// <remarks>   David, 2021-12-16. </remarks>
    /// <param name="stopwatch">        The stopwatch to act on. </param>
    /// <param name="timeout">          The timeout to act on. </param>
    /// <param name="onsetDelay">       The onset delay; before the first call to. </param>
    /// <param name="loopDelay">        The delay between <paramref name="doEventsAction"/> and. </param>
    /// <param name="predicate">        The predicate. </param>
    /// <param name="doEventsAction">   The do event action. </param>
    /// <returns>
    /// A Tuple: (true if Completed; otherwise, false if timed out before completion, elapsed time).
    /// </returns>
    public static (bool Completed, TimeSpan Elapsed) SyncLetElapseUntil( this Stopwatch stopwatch, TimeSpan timeout,
                                                                         TimeSpan onsetDelay, TimeSpan loopDelay,
                                                                         Func<bool> predicate, Action doEventsAction )
    {
        onsetDelay = Stopwatch.StartNew().SyncLetElapse( onsetDelay );
        var completed = predicate.Invoke();
        while ( stopwatch.Elapsed <= timeout && !completed )
        {
            if ( loopDelay > TimeSpan.Zero )
                _ = Stopwatch.StartNew().SyncLetElapse( loopDelay );
            doEventsAction?.Invoke();
            completed = predicate.Invoke();
        }
        return (completed, stopwatch.Elapsed.Add( onsetDelay ));
    }

    #endregion
}
