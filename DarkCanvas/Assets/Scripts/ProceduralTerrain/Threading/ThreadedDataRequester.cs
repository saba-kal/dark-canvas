using DarkCanvas.Common;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DarkCanvas.ProceduralTerrain
{
    /// <summary>
    /// Class for calling functions in separate threads.
    /// </summary>
    public class ThreadedDataRequester : Singleton<ThreadedDataRequester>
    {
        private ConcurrentQueue<ThreadInfo> _dataQueue =
            new ConcurrentQueue<ThreadInfo>();

        private void Update()
        {
            if (_dataQueue.Count > 0)
            {
                for (var i = 0; i < _dataQueue.Count; i++)
                {
                    if (_dataQueue.TryDequeue(out var mapThreadInfo))
                    {
                        mapThreadInfo.Callback(mapThreadInfo.Parameter);
                    }
                }
            }
        }

        /// <summary>
        /// Makes an asynchronous request for new data.
        /// </summary>
        /// <param name="generateData">Function to run on a separate thread.</param>
        /// <param name="callback">Callback function for when data is returned.</param>
        public static void RequestData(Func<object> generateData, Action<object> callback)
        {
            ThreadStart threadStart = delegate
            {
                Instance.DataThread(generateData, callback);
            };

            new Thread(threadStart).Start();
        }

        private void DataThread(Func<object> generateData, Action<object> callback)
        {
            var data = generateData();
            _dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }
}