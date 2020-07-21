using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Xamarin.Forms;
using MathNet.Numerics;
using System.Threading;
using MathNet.Numerics.Financial;

namespace WRONJ.Models
{
    public class WRONJModel
    {
        /// <summary>
        /// Delegate used with a Free Worker Queue
        /// </summary>
        /// <param name="workers"></param>
        public delegate void FreeWorkerEventHandler(List<int> workers);
        public delegate void AssignmentStartEventHandler(List<int> workers,double jobTime, double assignmentTime);
        public delegate void AssignmentEndEventHandler(List<int> workers, int worker, double jobTime);
        public event AssignmentStartEventHandler AssignmentStart;
        public event AssignmentEndEventHandler AssignmentEnd;
        public event FreeWorkerEventHandler FreeWorker;
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
        public uint Workers { get; set; }
        /// <summary>
        /// Output worker time, in seconds
        /// </summary>
        public uint JobNumber { get; set; }
        public double TotalTime()
        {
            return JobNumber * JobTime / Workers;
        }
        MathNet.Numerics.Distributions.LogNormal Distribution(double mean, double volatility)
        {
            if (volatility <= 0)
                return null;
            // According to https://en.wikipedia.org/wiki/Log-normal_distribution, we can calculate
            // mu and sigma from actual mean and volatility this way
            double sigma = Math.Sqrt(Math.Log(1 + Math.Pow(volatility/mean, 2)));
            double mu = Math.Log(mean) - sigma * sigma / 2;
            return MathNet.Numerics.Distributions.LogNormal.WithMeanVariance(mean,volatility * volatility);
        }
        public double ModelTime(double assignmentTime,double jobTime)
        {
            return assignmentTime * (Workers - 1) > jobTime ? assignmentTime * Workers  : jobTime + assignmentTime;
        }
        /// <summary>
        /// Returns Tuple(modelTime, workerTime)
        /// </summary>
        public async Task<Tuple<double, double>> Calculate()
        {
            var data = await CalculateAsync();
            return Tuple.Create(ModelTime(data.Item1, data.Item2), data.Item3);
        }
        /// <summary>
        /// Returns Tuple(assignmentsTime, jobsTime, workerTime)
        /// </summary>
        Task<Tuple<double, double, double>> CalculateAsync()
        {
            double inputAssignmentTime = AssignmentTime, inputJobTime = JobTime, 
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            uint workers = Workers, jobs = JobNumber;
            return Task<Tuple<double, double, double>>.Run(() =>
            {
                //Average of real job times
                double workerTime = 0, assignmentsTime=0, jobsTime=0;
                double lastJobTime;
                double time = 0;
                //The first item is the time when the worker ends the job. 
                //The second is the time when the job was assigned
                SortedSet<Tuple<double, double>> workersTime = new SortedSet<Tuple<double, double>>();
                var jobDist = Distribution(inputJobTime, jobTimeVolatility);
                var assignmentDist = Distribution(inputAssignmentTime,assignmentVolatility);
                for (int j=0; j < jobs;j++)
                {
                    double lastWorkerEndTime = time;
                    if (workersTime.Count == workers)
                    {
                        var firstWorker = workersTime.First();
                        lastWorkerEndTime = firstWorker.Item1;
                        if (lastWorkerEndTime > time)
                            time = lastWorkerEndTime;
                        workersTime.Remove(firstWorker);
                    }
                    double assignmentTime = (assignmentDist == null ? inputAssignmentTime : assignmentDist.Sample());
                    time += assignmentTime;
                    double jobTime = (jobDist == null ? inputJobTime : jobDist.Sample());
                    double workerEndTime = time + jobTime;
                    workersTime.Add(Tuple.Create(workerEndTime, lastWorkerEndTime));
                    lastJobTime = workerTime;
                    assignmentsTime = (j * assignmentsTime + assignmentTime) / (j + 1);
                    jobsTime = (j * jobsTime + jobTime) / (j + 1);
                    workerTime = (j * workerTime + (workerEndTime - lastWorkerEndTime)) / (j + 1);
                }
                return Tuple.Create(assignmentsTime, jobsTime, workerTime);
            });
        }
        public async void Simulate(CancellationToken cancelToken)
        {
            //All times in seconds
            double inputAssignmentTime = AssignmentTime , inputJobTime = JobTime,
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            uint workers = Workers, jobs=JobNumber;
            double time = 0;
            int ms = 0;
            var jobDist = Distribution(inputJobTime, jobTimeVolatility);
            var assignmentDist = Distribution(inputAssignmentTime, assignmentVolatility);
            List<int> FWQ = Enumerable.Range(0, (int)workers).ToList();
            SortedSet<Tuple<double, int>> workersTime = new SortedSet<Tuple<double, int>>();
            for (int i=0; jobs<=0 || i < jobs;i++)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                double jobTime = (jobDist == null ? inputJobTime : jobDist.Sample());
                double assignmentTime = (assignmentDist == null ? inputAssignmentTime : assignmentDist.Sample());
                // Getting a new job from pending queue
                AssignmentStart?.Invoke(FWQ, jobTime, assignmentTime);
                double freeWorkerTime = time;
                // Free all workers that end while assigning the new job
                while (workersTime.Count > 0 && workersTime.First().Item1 < time + assignmentTime)
                {
                    freeWorkerTime = workersTime.First().Item1;
                    FWQ.Add(workersTime.First().Item2);
                    workersTime.Remove(workersTime.First());
                    FreeWorker?.Invoke(FWQ);
                }
                time += assignmentTime;
                ms = (int)((time - freeWorkerTime)*1000);
                await Task.Delay(ms > 0 ? ms : 1);
                int assignedWorker = FWQ[0];
                FWQ.RemoveAt(0);
                //Assign to an active worker
                AssignmentEnd?.Invoke(FWQ, assignedWorker, jobTime);
                workersTime.Add(Tuple.Create(time + jobTime, assignedWorker));
                if (FWQ.Count == 0)
                {
                    double timeBefore = time;
                    time = workersTime.First().Item1;
                    FWQ.Add(workersTime.First().Item2);
                    workersTime.Remove(workersTime.First());
                    ms = (int)((time - timeBefore)*1000);
                    await Task.Delay(ms > 0 ? ms : 1);
                    FreeWorker?.Invoke(FWQ);
                }
            }
        }
    }
}