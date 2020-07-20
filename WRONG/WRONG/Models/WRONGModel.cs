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
        public double RealJobTime { get; private set; }
        public int JobNumber { get; set; }
        public double TotalTime
        {
            get
            {
                return JobNumber * JobTime / Workers;
            }
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
        public double ModelTime
        {
            get
            {
                if (JobTimeVolatility==0)
                    return AssignmentTime * (Workers - 1) / 1000 > JobTime ? AssignmentTime * Workers / 1000 : JobTime + AssignmentTime / 1000;
                var jobDist = Distribution(JobTime, JobTimeVolatility);
                double limitTime = AssignmentTime * (Workers - 1) / 1000;
                double limitCDF = jobDist.CumulativeDistribution(limitTime);
                //Using partial expectation of a lognormal for t > limitTime: https://en.wikipedia.org/wiki/Log-normal_distribution#Partial_expectation
                return limitCDF * (AssignmentTime * Workers / 1000) +
                    (1 - limitCDF) * AssignmentTime / 1000 +
                    JobTime * MathNet.Numerics.Distributions.Normal.CDF(0,1,(jobDist.Mu + jobDist.Sigma * jobDist.Sigma - Math.Log(limitTime)) / jobDist.Sigma);
            }
        }
        public async Task Calculate()
        {
            var data = await CalculateAsync();
            RealJobTime = data.Item1;
            JobNumber = data.Item2;
        }
        Task<Tuple<double, int>> CalculateAsync()
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
                var jobDist = Distribution(jobTime, jobTimeVolatility);
                var assignmentDist = Distribution(assignmentTime,assignmentVolatility);
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
                    time += (assignmentDist == null? assignmentTime : assignmentDist.Sample());
                    double workerEndTime = time + (jobDist == null ? jobTime : jobDist.Sample());
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
            //All times in seconds
            double assignmentTime = AssignmentTime/1000 , jobTime = JobTime, modelTime = ModelTime,
                assignmentVolatility = AssignmentTimeVolatility/1000, jobTimeVolatility = JobTimeVolatility;
            int workers = Workers, jobs=JobNumber;
            double time = 0;
            int ms = 0;
            var jobDist = Distribution(jobTime, jobTimeVolatility);
            var assignmentDist = Distribution(assignmentTime, assignmentVolatility);
            List<int> FWQ = Enumerable.Range(0, workers).ToList();
            SortedSet<Tuple<double, int>> workersTime = new SortedSet<Tuple<double, int>>();
            for (int i=0; jobs<=0 || i < jobs;i++)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                double rndJobTime = (jobDist == null ? jobTime : jobDist.Sample());
                double rndAssignmentTime = (assignmentDist == null ? assignmentTime : assignmentDist.Sample());
                // Getting a new job from pending queue
                AssignmentStart?.Invoke(FWQ, rndJobTime, rndAssignmentTime);
                double freeWorkerTime = time;
                // Free all workers that end while assigning the new job
                while (workersTime.Count > 0 && workersTime.First().Item1 < time + rndAssignmentTime)
                {
                    freeWorkerTime = workersTime.First().Item1;
                    FWQ.Add(workersTime.First().Item2);
                    workersTime.Remove(workersTime.First());
                    FreeWorker?.Invoke(FWQ);
                }
                time += rndAssignmentTime;
                ms = (int)((time - freeWorkerTime)*1000);
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
                    ms = (int)((time - timeBefore)*1000);
                    await Task.Delay(ms > 0 ? ms : 1);
                    FreeWorker?.Invoke(FWQ);
                }
            }
        }
    }
}