using MLoop.Models;
using System.Collections.Concurrent;

namespace MLoop.Services
{
    public class MLTrainPool
    {
        private readonly MLTrainThread[] threads;
        private readonly BlockingCollection<TrainRequest> queue;

        public MLTrainPool(int threadCount)
        {
            this.threads = new MLTrainThread[threadCount];
            this.queue = [];

            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new MLTrainThread(queue);
                var thread = new Thread(threads[i].Run)
                {
                    IsBackground = true // 백그라운드 스레드로 설정
                };
                thread.Start();
            }
        }

        internal TrainResponse Train(TrainRequest request)
        {
            if (queue.Any(p => p.Key == request.Key))
            {
                // 이미 대기열에 있음
                return new TrainResponse(request.Key, TrainStatus.Pending);
            }

            queue.Add(request);
            return new TrainResponse(request.Key, TrainStatus.Pending);
        }

        public IEnumerable<string> GetWorkingModels()
        {
            for (int i = 0; i < threads.Length; i++)
            {
                yield return $"{i + 1}: {threads[i].WorkingModelKey ?? "Idle"}";
            }
        }
    }
}