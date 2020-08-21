using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MathNet.Numerics.Integration;
using Xamarin.Forms;
using System.Reflection;
using MathNet.Numerics.Financial;

namespace WRONJ.Models
{
    public class WRONJModel : ICloneable
    {
        public delegate void AssignmentStartEventHandler(List<int> workers,double jobTime, double assignmentTime);
        public delegate void AssignmentEndEventHandler(List<int> workers, int worker, double workerTime);
        public delegate void FreeWorkerEventHandler(List<int> workers, double timeBetweenEndings);
        public delegate void EndSimulationEventHandler(double idealTotalTime, double realTotalTime);
        public event AssignmentStartEventHandler AssignmentStart;
        public event AssignmentEndEventHandler AssignmentEnd;
        public event FreeWorkerEventHandler FreeWorker;
        public event EndSimulationEventHandler EndSimulation;
        /// <summary>
        /// Input average job assignment time, in seconds
        /// </summary>
        public double AssignmentTime { get; set; }
        public double AssignmentTimeVolatility { get; set; }
        public bool RandomAssignmentTimeVolatility { get; set; }
        /// <summary>
        ///  Input average job time, in seconds
        /// </summary>
        public double JobTime { get; set; }
        public double JobTimeVolatility { get; set; }
        public bool RandomJobTimeVolatility { get; set; }
        public int Workers { get; set; }
        public int Jobs { get; set; }
        MathNet.Numerics.Distributions.LogNormal Distribution(double mean, double volatility, int seed=0)
        {
            if (volatility <= 0)
                return null;
            var dist= MathNet.Numerics.Distributions.LogNormal.WithMeanVariance(mean,volatility * volatility);
            if (seed != 0 )
                dist.RandomSource= new MathNet.Numerics.Random.SystemRandomSource(seed);
            return dist;
        }
        public static double WorkerTime(double assignmentTime, double jobTime, int workers)
        {
            return assignmentTime * (workers - 1) > jobTime ? assignmentTime * workers : jobTime + assignmentTime;
        }
        public static double JobTimeLimit(double assignmentTime,int workers)
        {
            if (workers == 0)
                return 0;
            return assignmentTime * (workers -1);
        }
        public static double WorkersLimit(double assignmentTime, double jobTime)
        {
            if (assignmentTime == 0 || jobTime == 0)
                return 0;
            return jobTime / assignmentTime + 1;
        }
        public static double TotalTime(double jobTime, int workers, int jobs,double assignmentTime=0)
        {
            if (workers == 0 || jobs == 0)
                return 0;
            if (assignmentTime==0)
                return jobTime * (jobs/workers + (jobs % workers > 0 ? 1 : 0));
            if (jobs <= workers)
                return assignmentTime * jobs + jobTime;
            if (jobs % workers == 0)
            {
                return WorkerTime(assignmentTime, jobTime, workers) * (jobs / workers - 1) +
                    assignmentTime * workers + jobTime;
            }
            return WorkerTime(assignmentTime, jobTime, workers) * (jobs / workers) +
                    assignmentTime * (jobs % workers) + jobTime;

        }
        public Task<(double workerTime,
                double idealTotalTime,
                double realTotalTime) >  CalculateAsync(CancellationToken cancelToken)
        {
            double inputAssignmentTime = AssignmentTime, inputJobTime = JobTime, 
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            bool randomATVol = RandomAssignmentTimeVolatility, randomJTVol = RandomJobTimeVolatility;
            int workers = Workers, jobs = Jobs;
            if (workers == 0 || jobs == 0 || inputJobTime == 0)
                return Task<(double,double,double)>.FromResult((0.0,0.0, 0.0));
            return Task < (double, double, double) >.Run(() =>
            {
                double assignmentsTime=0, jobsTime=0;
                double time = 0;
                double workerTime = jobs > workers ? 0 : WorkerTime(inputAssignmentTime, inputJobTime, workers);
                // Sorted sets to manage the ideal and real worker times 
                SortedSet<(double endTime, int worker)> workersTime = new SortedSet<(double, int)>();
                SortedSet<(double endTime, int worker)> workersIdealTime = new SortedSet<(double, int)>();
                var jobDist = Distribution(inputJobTime, jobTimeVolatility, randomJTVol? 0: 1);
                var assignmentDist = Distribution(inputAssignmentTime,assignmentVolatility, randomATVol ? 0 : 2);
                for (int j=0; j < jobs;j++)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;
                    double jobTime = (jobDist == null ? inputJobTime : jobDist.Sample());
                    jobsTime = (j * jobsTime + jobTime) / (j + 1);
                    if (workersIdealTime.Count == workers)
                    {
                        var firstWorker = workersIdealTime.First();
                        workersIdealTime.Remove(firstWorker);
                        // In the ideal grid, the assignment time is 0: the worker time 
                        // (= difference between the ending time of a job and the the ending time of the next one)
                        // always be equal to the job time
                        workersIdealTime.Add((firstWorker.endTime + jobTime, firstWorker.worker));
                    }
                    else
                    {
                        workersIdealTime.Add((jobTime, workersIdealTime.Count));
                    }
                    double assignmentTime = (assignmentDist == null ? inputAssignmentTime : assignmentDist.Sample());
                    assignmentsTime = (j * assignmentsTime + assignmentTime) / (j + 1);
                    if (workersTime.Count == workers)
                    {
                        var firstWorker = workersTime.First();
                        if (firstWorker.endTime > time)
                        {
                            time = firstWorker.endTime;
                        }
                        time += assignmentTime;
                        workersTime.Remove(firstWorker);
                        workersTime.Add((time + jobTime, firstWorker.worker));
                        // We start to compute the workerTime only when the grid is full
                        workerTime = ((j - workers) * workerTime + time + jobTime - firstWorker.endTime) / (j + 1 - workers);
                    }
                    else
                    {
                        time += assignmentTime;
                        workersTime.Add((time + jobTime, workersTime.Count));
                    }
                }
                return (workerTime, workersIdealTime.Last().endTime, workersTime.Last().endTime);
            });
        }
    public async void Simulate(CancellationToken cancelToken)
        {            
            //All times in seconds
            double inputAssignmentTime = AssignmentTime , inputJobTime = JobTime,
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            bool randomATVol = RandomAssignmentTimeVolatility, randomJTVol = RandomJobTimeVolatility;
            int workers = Workers, jobs=Jobs;
            if (workers == 0)
                return;
            double time = 0, idealTime=0;
            var jobDist = Distribution(inputJobTime, jobTimeVolatility, randomJTVol ? 0 : 1);
            var assignmentDist = Distribution(inputAssignmentTime, randomATVol ? 0 : 2);
            List<int> FWQ = Enumerable.Range(0, (int)workers).ToList();
            // Sorted set to manage the real worker times 
            SortedSet<(double endTime, int position)> workersTime = new SortedSet<(double, int)>();
            // Dictionary to manage the ideal and real worker last times: 
            // - The first item is worker position
            // - The second item is the last time when the worker ends the job 
            Dictionary<int, double> workersLastTime = new Dictionary<int, double>();
            Dictionary<int, double> workersIdealLastTime = new Dictionary<int, double>();
            double workerTime = jobs > workers ? 0 : WorkerTime(inputAssignmentTime, inputJobTime, workers);
            double assignmentsTime = 0, jobsTime = 0;
            double timeBetweenEndings = 0, timeLastEnding=0;
            int endedCount = 0;
            Func<(double endTime, int worker), double, Task<bool>> freeWorker = async (activeWorker, timeBefore) =>
            {
                int ms = (int)((activeWorker.endTime - timeBefore) * 1000);
                bool waited = false;                
                if (ms > 0)
                {
                    await Task.Delay(ms);
                    waited = true;
                }
                FWQ.Add(activeWorker.worker);
                if (timeLastEnding > 0)
                {
                    timeBetweenEndings = (endedCount * timeBetweenEndings + activeWorker.endTime - timeLastEnding) / ++endedCount;
                }
                timeLastEnding = activeWorker.endTime;
                FreeWorker?.Invoke(FWQ, timeBetweenEndings);
                return waited;
            };
            // Assigning all jobs
            for (int j=0; j < jobs;j++)
            {
                if (cancelToken.IsCancellationRequested)
                    break;
                int assignedWorker = FWQ[0];
                double jobTime = (jobDist == null ? inputJobTime : jobDist.Sample());
                // In the ideal grid, the assignment time is 0: the worker time 
                // (= difference between the ending time of a job and the the ending time of the next one)
                // always be equal to the job time
                if (workersIdealLastTime.ContainsKey(assignedWorker))
                {
                    workersIdealLastTime[assignedWorker] = workersIdealLastTime[assignedWorker] + jobTime;
                }
                else
                {
                    workersIdealLastTime.Add(assignedWorker, jobTime);
                }
                if (workersIdealLastTime[assignedWorker] > idealTime)
                {
                    idealTime = workersIdealLastTime[assignedWorker];
                }
                double assignmentTime = (assignmentDist == null ? inputAssignmentTime : assignmentDist.Sample());
                // Getting a new job from pending queue
                AssignmentStart?.Invoke(FWQ, jobTime, assignmentTime);
                // Free all workers that end while assigning the new job
                double freeWorkerTime = time;
                bool waited = false;
                while (workersTime.Count > 0 && workersTime.First().endTime < time + assignmentTime)
                {
                    waited = await freeWorker(workersTime.First(), freeWorkerTime);
                    freeWorkerTime = workersTime.First().endTime;
                    workersTime.Remove(workersTime.First());
                }
                time += assignmentTime;
                int ms = (int)((time - freeWorkerTime)*1000);
                if (ms > 0 || !waited)
                {
                    // If ms == 0 but there hasn't been any previous call to await, just make one to ensure
                    // the GUI is refreshed
                    await Task.Delay(ms > 0 ? ms : 1);
                }
                FWQ.RemoveAt(0);
                double workerLastTime = workersLastTime.ContainsKey(assignedWorker) ?
                                    time + jobTime - workersLastTime[assignedWorker] :
                                    jobTime;                
                //Assign to an active worker
                if (workersLastTime.ContainsKey(assignedWorker))
                {
                    workersLastTime[assignedWorker] = time + jobTime;
                }
                else
                {
                    workersLastTime.Add(assignedWorker, time + jobTime);
                }
                workersTime.Add((time + jobTime, assignedWorker));
                assignmentsTime = (j * assignmentsTime + assignmentTime) / (j + 1);
                jobsTime = (j * jobsTime + jobTime) / (j + 1);
                // We start to compute the worker times only when the grid is full
                if (j >= workers)
                {
                    workerTime = ((j- workers) * workerTime + workerLastTime) / (j + 1 - workers);
                }
                AssignmentEnd?.Invoke(FWQ, assignedWorker, workerTime);
                if (FWQ.Count == 0)
                {
                    await freeWorker(workersTime.First(), time);
                    time = workersTime.First().endTime;
                    workersTime.Remove(workersTime.First());
                }
            }
            // Releasing remaining active workers
            foreach (var activeWorker in workersTime)
            {
                time = activeWorker.endTime;
                await freeWorker(activeWorker, time);
            }
            EndSimulation?.Invoke(idealTime, time);
        }
        private PropertyInfo[] BasicProperties()
        {
            return GetType().GetProperties();
        }
        public void Save()
        {
            IDictionary<string, object> properties = Application.Current.Properties;
            foreach (var property in BasicProperties())
            {
                properties[property.Name] = property.GetValue(this);
            }
        }
        public void Load()
        {
            IDictionary<string, object> properties = Application.Current.Properties;
            foreach (var property in BasicProperties())
            {
                if (properties.ContainsKey(property.Name))
                {
                    property.SetValue(this, properties[property.Name]);
                }
            }
        }
        public object Clone()
        {
            WRONJModel clone = new WRONJModel();
            foreach (var property in BasicProperties())
            {
                property.SetValue(clone, property.GetValue(this));
            }
            return clone;
        }
    }
}