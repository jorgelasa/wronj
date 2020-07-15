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
        public delegate void FWQ(List<int> workers);
        public delegate void FWQAndTime(List<int> workers,double time);
        public delegate void FWQSAndWorker(List<int> workers, int worker);
        public event FWQAndTime AssignmentStart;
        public event FWQSAndWorker AssignmentEnd;
        public event FWQ FreeWorker;
        /// <summary>
        /// Task assignment time, in milliseconds
        /// </summary>
        public double AssignmentTime { get; set; }
        public double AssignmentTimeVolatility { get; set; }
        /// <summary>
        ///  Average task time, in seconds
        /// </summary>
        public double TaskTime { get; set; }
        public double TaskTimeVolatility { get; set; }
        public int Workers { get; set; }
        public double ModelTime
        {
            get
            {
                return AssignmentTime * (Workers - 1) / 1000 > TaskTime ? AssignmentTime * Workers / 1000 : TaskTime + AssignmentTime / 1000;
            }
        }
        public double RealTaskTime { get; private set; }
        public int TaskNumber { get; set; }
        public double TotalTime
        {
            get
            {
                return TaskNumber * TaskTime / Workers;
            }
        }
        public async Task CalculateUntilConvergence()
        {
            var data = await CalculateUntilConvergenceAsync();
            RealTaskTime = data.Item1;
            TaskNumber = data.Item2;
        }
        Task<Tuple<double, int>> CalculateUntilConvergenceAsync()
        {
            double assignmentTime = AssignmentTime / 1000, taskTime = TaskTime, modelTime = ModelTime, 
                assignmentVolatility = AssignmentTimeVolatility, taskTimeVolatility = TaskTimeVolatility;
            int workers = Workers, topTasks = TaskNumber;
            return Task<Tuple<double, int>>.Run(() =>
            {
                //Average of real task times
                double realTaskTime = 0;
                double lastTaskTime;
                double time = 0;
                int tasks = 0;
                //The first item is the time when the worker ends the task. 
                //The second is the time when the task was assigned
                SortedSet<Tuple<double, double>> workersTime = new SortedSet<Tuple<double, double>>();
                const int topTasksWithinRange = 1000;
                double convergenceDiff = 0.00001;
                int lastTasksWithinRange = 0;
                MathNet.Numerics.Distributions.Normal taskDist = new MathNet.Numerics.Distributions.Normal(taskTime, taskTimeVolatility);
                MathNet.Numerics.Distributions.Normal assignmentDist = new MathNet.Numerics.Distributions.Normal(taskTime, assignmentVolatility);
                while (topTasks == 0 && lastTasksWithinRange < topTasksWithinRange
                || tasks < topTasks)
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
                    double rndTaskTime = taskTime;
                    if (taskTimeVolatility > 0)
                        rndTaskTime = taskDist.Sample();
                    double workerEndTime = time + rndTaskTime;
                    workersTime.Add(Tuple.Create(workerEndTime, lastWorkerEndTime));
                    lastTaskTime = realTaskTime;
                    realTaskTime = (tasks * realTaskTime + (workerEndTime - lastWorkerEndTime)) / ++tasks;
                    //if (Math.Abs(realTaskTime - lastTaskTime) < convergenceDiff)
                    if (Math.Abs(realTaskTime - modelTime) < convergenceDiff)
                        lastTasksWithinRange++;
                    else
                        lastTasksWithinRange = 0;
                }
                return Tuple.Create(realTaskTime, tasks);
            });
        }
        public async void Simulate(CancellationToken cancelToken)
        {
            //All times in milliseconds
            double q = AssignmentTime , taskTime = TaskTime*1000, modelTime = ModelTime*1000, volatility = 0;
            int workers = Workers, tasks = TaskNumber;
            double time = 0;
            int ms = 0;
            MathNet.Numerics.Distributions.Normal normal = new MathNet.Numerics.Distributions.Normal(taskTime, volatility);
            List<int> FWQ= Enumerable.Range(0, workers-1).ToList();
            SortedSet<Tuple<double, int>> workersTime = new SortedSet<Tuple<double, int>>();
            for (int i=1; i<=tasks; i++)
            {
                if (cancelToken.IsCancellationRequested)
                    return;
                double rndTaskTime = taskTime;
                if (volatility > 0)
                    rndTaskTime = normal.Sample();
                // Getting a new task from pending queue
                AssignmentStart?.Invoke(FWQ, rndTaskTime);
                double freeWorkerTime = time;
                // Free all workers that end while assigning the new task
                while (workersTime.Count > 0 &&  workersTime.First().Item1 < time + q)
                {
                    freeWorkerTime = workersTime.First().Item1;
                    FWQ.Add(workersTime.First().Item2);
                    workersTime.Remove(workersTime.First());
                    FreeWorker?.Invoke(FWQ);
                }
                time += q;
                ms = (int) (time - freeWorkerTime);
                await Task.Delay(ms > 0 ? ms : 1);
                int assignedWorker = FWQ[0];
                FWQ.RemoveAt(0);
                //Assign to an active worker
                AssignmentEnd?.Invoke(FWQ, assignedWorker);
                workersTime.Add(Tuple.Create(time + rndTaskTime, assignedWorker));
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