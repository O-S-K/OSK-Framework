using UnityEngine.Profiling;

namespace OSK
{
    [System.Serializable]
    public class PerforInfo
    {
        public string label;
        public long startTime;
        
        public PerforInfo(string label, long startTime)
        { 
            this.label = label;
            this.startTime = startTime;
        }
        
        
        public void StartTest(string label)
        {
            Profiler.BeginSample(label);
            startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public void StopTest()
        {
            long endTime = System.Diagnostics.Stopwatch.GetTimestamp();
            Profiler.EndSample();
            double elapsedTime = (endTime - startTime) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            OSKLogger.Log($"{startTime} took {elapsedTime:F2} ms");
        }
    } 
}