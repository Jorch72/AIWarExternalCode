using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Arcen.AIW2.External
{
    public class Window_DebugInfo : WindowControllerAbstractBase
    {
        public class tText : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer buffer )
            {
                if ( Engine_Universal.IsProfilerEnabled )
                    DoSingleCase_ProfilerEnabled( buffer );
                if ( Engine_Universal.DebugTextToShowImmediately.Length > 0 )
                    DoSingleCase_DebugTextToShowImmediately( buffer );
                if ( Engine_Universal.TracePerformance )
                    DoSingleCase_TracePerformance( buffer );
            }

            private void DoSingleCase_TracePerformance( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Performance:" );
                TimeSpan spanToCheck = TimeSpan.FromSeconds( 30 );
                DateTime checkEverythingAfter = DateTime.Now - spanToCheck;
                for ( PerformanceSegment segment = PerformanceSegment.None + 1; segment < PerformanceSegment.Length; segment++ )
                {
                    ArcenStopwatch stopwatch = Engine_Universal.PerformanceStopwatches[segment];
                    double worst = 0;
                    double total = 0;
                    int samplesRead = 0;
                    for ( int i = stopwatch.SampleHistory.Count - 1; i >= 0; i-- )
                    {
                        ArcenStopwatch.Sample sample = stopwatch.SampleHistory[i];
                        if ( sample.Timestamp < checkEverythingAfter )
                            break;
                        worst = Math.Max( sample.Elapsed.TotalMilliseconds, worst );
                        total += sample.Elapsed.TotalMilliseconds;
                        samplesRead++;
                    }
                    if ( samplesRead > 0 )
                    {
                        double average = total / samplesRead;
                        buffer.Add( "\n" ).Add( segment.ToString() ).Add( " Avg:" ).Add( average.ToString( "0.##" ) ).Add( " Worst:" ).Add( worst.ToString( "0.##" ) );
                    }
                }
            }

            private void DoSingleCase_DebugTextToShowImmediately( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "DebugOutput:" );
                buffer.Add( Engine_Universal.DebugTextToShowImmediately );
            }

            private void DoSingleCase_ProfilerEnabled( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "Profiler On! " );
            }
        }
    }
}