using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MathNet.Numerics.Integration;

namespace WRONJ.Models
{
    public class WRONJModel
    {
        public delegate void FreeWorkerEventHandler(List<int> workers, double idealTotalTime, double realTotalTime);
        public delegate void AssignmentStartEventHandler(List<int> workers,double jobTime, double assignmentTime);
        public delegate void AssignmentEndEventHandler(List<int> workers, int worker, double modelTime, double workerTime);
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
        /// <summary>
        ///  Input average job time, in seconds
        /// </summary>
        public double JobTime { get; set; }
        public double JobTimeVolatility { get; set; }
        public int Workers { get; set; }
        public int Jobs { get; set; }
        MathNet.Numerics.Distributions.LogNormal Distribution(double mean, double volatility)
        {
            if (volatility <= 0)
                return null;
            return MathNet.Numerics.Distributions.LogNormal.WithMeanVariance(mean,volatility * volatility);
        }
        public double ModelWorkerTime(double assignmentTime,double jobTime)
        {
            return assignmentTime * (Workers - 1) > jobTime ? assignmentTime * Workers  : jobTime + assignmentTime;
        }
        public double IdeallTotalTime()
        {
            return Workers == 0 ? 0 : JobTime * (Jobs/Workers + (Jobs % Workers > 0 ? 1 : 0));
        }
        public async Task<(double modelTime, double workerTime, double idealTotalTime, double realTotalTime)> Calculate(CancellationToken cancelToken)
        {
            var data = await CalculateAsync(cancelToken);
            return (ModelWorkerTime(data.assignmentsTime, data.jobsTime), data.workerTime, data.idealTotalTime, data.realTotalTime);
        }
        Task<(  double assignmentsTime, 
                double jobsTime,
                double workerTime,
                double idealTotalTime,
                double realTotalTime) >  CalculateAsync(CancellationToken cancelToken)
        {
            double inputAssignmentTime = AssignmentTime, inputJobTime = JobTime, 
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            int workers = Workers, jobs = Jobs;
            if (workers == 0 || jobs == 0 || inputJobTime == 0)
                return Task<(double,double,double,double,double)>.FromResult((inputAssignmentTime,0.0,0.0,0.0, 0.0));
            return Task < (double, double, double, double, double) >.Run(() =>
            {
                double workerTime = 0, assignmentsTime=0, jobsTime=0;
                double time = 0;
                // Sorted sets to manage the ideal and real worker times 
                SortedSet<(double endTime, int worker)> workersTime = new SortedSet<(double, int)>();
                SortedSet<(double endTime, int worker)> workersIdealTime = new SortedSet<(double, int)>();
                var jobDist = Distribution(inputJobTime, jobTimeVolatility);
                var assignmentDist = Distribution(inputAssignmentTime,assignmentVolatility);
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
                        workersTime.Add((time + jobTime, workersIdealTime.Count));
                    }
                }
                return (assignmentsTime, jobsTime, workerTime, workersIdealTime.Last().endTime, workersTime.Last().endTime);
            });
        }
    public async void Simulate(CancellationToken cancelToken)
        {            
            //All times in seconds
            double inputAssignmentTime = AssignmentTime , inputJobTime = JobTime,
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            int workers = Workers, jobs=Jobs;
            if (workers == 0)
                return;
            double time = 0, idealTime=0;
            var jobDist = Distribution(inputJobTime, jobTimeVolatility);
            var assignmentDist = Distribution(inputAssignmentTime, assignmentVolatility);
            List<int> FWQ = Enumerable.Range(0, (int)workers).ToList();
            // Sorted set to manage the real worker times 
            SortedSet<(double endTime, int position)> workersTime = new SortedSet<(double, int)>();
            // Dictionary to manage the ideal and real worker last times: 
            // - The first item is worker position
            // - The second item is the last time when the worker ends the job 
            Dictionary<int, double> workersLastTime = new Dictionary<int, double>();
            Dictionary<int, double> workersIdealLastTime = new Dictionary<int, double>();
            double workerTime = 0, assignmentsTime = 0, jobsTime = 0;
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
                FreeWorker?.Invoke(FWQ,idealTime,time);
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
                double lastIdealTime;
                if (workersIdealLastTime.ContainsKey(assignedWorker))
                {
                    lastIdealTime = workersIdealLastTime[assignedWorker] + jobTime;
                    workersIdealLastTime[assignedWorker] = lastIdealTime;
                }
                else
                {
                    lastIdealTime = jobTime;
                    workersIdealLastTime.Add(assignedWorker, lastIdealTime);
                }
                if (lastIdealTime > idealTime)
                {
                    idealTime = lastIdealTime;
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
                //Assign to an active worker
                workersTime.Add((time + jobTime, assignedWorker));
                assignmentsTime = (j * assignmentsTime + assignmentTime) / (j + 1);
                jobsTime = (j * jobsTime + jobTime) / (j + 1);
                if (j >= workers)
                {
                    // We start to compute the workerTime only when the grid is full
                    workerTime = ((j- workers) * workerTime + workerLastTime) / (j + 1 - workers);
                }
                AssignmentEnd?.Invoke(FWQ, assignedWorker, ModelWorkerTime(assignmentsTime, jobsTime),workerTime);
                if (FWQ.Count == 0)
                {
                    await freeWorker(workersTime.First(), time);
                    time = workersTime.First().endTime;
                    workersTime.Remove(workersTime.First());
                }
            }
            // Releasing active workers
            foreach (var activeWorker in workersTime)
            {
                await freeWorker(activeWorker, time);
                time = activeWorker.endTime;
            }
            EndSimulation?.Invoke(idealTime,time);
        }
    }
}