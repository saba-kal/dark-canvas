using System;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Holds information for calling a callback function once a thread completes its job.
    /// </summary>
    public class ThreadInfo
    {
        public readonly Action<object> Callback;
        public readonly object Parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            Callback = callback;
            Parameter = parameter;
        }
    }
}