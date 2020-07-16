using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Xamarin.Forms;
using MathNet.Numerics;
using System.Threading;

namespace WRONG.Models
{
    public class WRONGModel
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
        /// Job assignment time, in milliseconds
        /// </summary>
        public double AssignmentTime { get; set; }
        public double AssignmentTimeVolatility { get; set; }
        /// <summary>
        ///  Average job time, in seconds
        /// </summary>
        public double JobTime { get; set; }
        public double JobTimeVolatility { get; set; }
        public int Workers { get; set; }
        public double ModelTime
        {
            get
            {
                return AssignmentTime * (Workers - 1) / 1000 > JobTime ? AssignmentTime * Workers / 1000 : JobTime + AssignmentTime / 1000;
            }
        }
        public double RealJobTime { get; private set; }
        public int JobNumber { get; set; }
        public double TotalTime
        {
            get
            {
                return JobNumber * JobTime / Workers;
            }
        }
        public async Task CalculateUntilConvergence()
        {
            var data = await CalculateUntilConvergenceAsync();
            RealJobTime = data.Item1;
            JobNumber = data.Item2;
        }
        Task<Tuple<double, int>> CalculateUntilConvergenceAsync()
        {
            double assignmentTime = AssignmentTime / 1000, jobTime = JobTime, modelTime = ModelTime, 
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            int workers = Workers, topJobs = JobNumber;
            return Task<Tuple<double, int>>.Run(() =>
            {
                //Average of real job times
                double realJobTime = 0;
                double lastJobTime;
                double time = 0;
                int jobs = 0;
                //The first item is the time when the worker ends the job. 
                //The second is the time when the job was assigned
                SortedSet<Tuple<double, double>> workersTime = new SortedSet<Tuple<double, double>>();
                const int topJobsWithinRange = 1000;
                double convergenceDiff = 0.00001;
                int lastJobsWithinRange = 0;
                MathNet.Numerics.Distributions.Normal jobDist = new MathNet.Numerics.Distributions.Normal(jobTime, jobTimeVolatility);
                MathNet.Numerics.Distributions.Normal assignmentDist = new MathNet.Numerics.Distributions.Normal(jobTime, assignmentVolatility);
                while (topJobs == 0 && lastJobsWithinRange < topJobsWithinRange
                || jobs < topJobs)
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
                    time += assignmentTime;
                    double rndJobTime = jobTime;
                    if (jobTimeVolatility > 0)
                        rndJobTime = jobDist.Sample();
                    double workerEndTime = time + rndJobTime;
                    workersTime.Add(Tuple.Create(workerEndTime, lastWorkerEndTime));
                    lastJobTime = realJobTime;
                    realJobTime = (jobs * realJobTime + (workerEndTime - lastWorkerEndTime)) / ++jobs;
                    //if (Math.Abs(realJobTime - lastJobTime) < convergenceDiff)
                    if (Math.Abs(realJobTime - modelTime) < convergenceDiff)
                        lastJobsWithinRange++;
                    else
                        lastJobsWithinRange = 0;
                }
                return Tuple.Create(realJobTime, jobs);
            });
        }
        public async void Simulate(CancellationToken cancelToken)
        {
            //All times in milliseconds
            double q = AssignmentTime , jobTime = JobTime*1000, modelTime = ModelTime*1000,
                assignmentVolatility = AssignmentTimeVolatility * 1000, jobTimeVolatility = JobTimeVolatility * 1000;
            int workers = Workers;
            double time = 0;
            int ms = 0;
            MathNet.Numerics.Distributions.Normal jobDist = new MathNet.Numerics.Distributions.Normal(jobTime, jobTimeVolatility);
            List<int> FWQ = Enumerable.Range(0, workers).ToList();
            SortedSet<Tuple<double, int>> workersTime = new SortedSet<Tuple<double, int>>();
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                double rndJobTime = jobTime;
                if (jobTimeVolatility > 0)
                    rndJobTime = jobDist.Sample();
                // Getting a new job from pending queue
                AssignmentStart?.Invoke(FWQ, rndJobTime, q);
                double freeWorkerTime = time;
                // Free all workers that end while assigning the new job
                while (workersTime.Count > 0 && workersTime.First().Item1 < time + q)
                {
                    freeWorkerTime = workersTime.First().Item1;
                    FWQ.Add(workersTime.First().Item2);
                    workersTime.Remove(workersTime.First());
                    FreeWorker?.Invoke(FWQ);
                }
                time += q;
                ms = (int)(time - freeWorkerTime);
                await Task.Delay(ms > 0 ? ms : 1);
                int assignedWorker = FWQ[0];
                FWQ.RemoveAt(0);
                //Assign to an active worker
                AssignmentEnd?.Invoke(FWQ, assignedWorker, rndJobTime);
                workersTime.Add(Tuple.Create(time + rndJobTime, assignedWorker));
                if (FWQ.Count == 0)
                {
                    double timeBefore = time;
                    time = workersTime.First().Item1;
                    FWQ.Add(workersTime.First().Item2);
                    workersTime.Remove(workersTime.First());
                    ms = (int)(time - timeBefore);
                    await Task.Delay(ms > 0 ? ms : 1);
                    FreeWorker?.Invoke(FWQ);
                }
            }
        }
    }
}