using MathNet.Numerics.Financial;
using System.Reflection;
using System.Text.Json;

/// Alias for the type used to manage the workers end times,
/// along with an assignation id to allow repeated times in the sorted set
using WorkersTimes = System.Collections.Generic.SortedSet<(double time, int id)>;

namespace WRONJ.Models
{
    public class WRONJModel : ICloneable
    {
        public delegate void AssignmentStartEventHandler(List<int> freeWorkers, List<int> assignedWorkers, double maxJobTime, double assignmentTime);
        public delegate void AssignmentEndEventHandler(List<int> freeWorkers, List<int> assignedWorkers, double workerTime);
        public delegate void FreeWorkerEventHandler(List<int> freeWorkers);
        public delegate void EndSimulationEventHandler(double idealTotalTime, double realTotalTime);
        public event AssignmentStartEventHandler AssignmentStart;
        public event AssignmentEndEventHandler AssignmentEnd;
        public event FreeWorkerEventHandler AddFreeWorker;
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
        public int Jobs { get; set; }
        public int Workers { get; set; }
        public bool MachineAssignment { get; set; }
        public int Machines { get; set; }
        public bool UseMachines()
        {
            return MachineAssignment && Machines > 0;
        }
        public int AssignationUnits => UseMachines() ? Machines : Workers;
        public int TotalWorkers => UseMachines() ? Workers * Machines : Workers;
        public double EffectiveAssignmentTime => UseMachines() ? AssignmentTime / Workers : AssignmentTime;
        MathNet.Numerics.Distributions.LogNormal Distribution(double mean, double volatility, int seed = 0)
        {
            if (volatility <= 0)
                return null;
            var dist = MathNet.Numerics.Distributions.LogNormal.WithMeanVariance(mean, volatility * volatility);
            if (seed != 0)
                dist.RandomSource = new MathNet.Numerics.Random.SystemRandomSource(seed);
            return dist;
        }
        public double WorkerTime()
        {
            return EffectiveAssignmentTime * (TotalWorkers - 1) > JobTime ? EffectiveAssignmentTime * TotalWorkers : JobTime + AssignmentTime;
        }
        public double JobTimeLimit()
        {
            if (TotalWorkers == 0)
                return 0;

            return EffectiveAssignmentTime * (TotalWorkers - 1);
        }
        public double WorkersLimit()
        {
            if (JobTime == 0 || JobTime == 0)
                return 0;
            return JobTime / JobTime + 1;
        }

        public double TotalTime(bool idealTime)
        {
            if (TotalWorkers == 0 || Jobs == 0)
                return 0;

            double assignmentTime = idealTime ? 0 : AssignmentTime;
            double effectiveAssignmentTime = idealTime ? 0 : EffectiveAssignmentTime;

            if (Jobs <= TotalWorkers || effectiveAssignmentTime > 0 && JobTime <= JobTimeLimit())
                return effectiveAssignmentTime * Jobs + JobTime;

            return (JobTime + assignmentTime) * (Jobs / TotalWorkers + (Jobs % AssignationUnits > 0 ? 1 : 0)) +
                assignmentTime * (Jobs % AssignationUnits > 0 ? Jobs % AssignationUnits - 1 : AssignationUnits - 1);

        }
        public Task<(double idealTotalTime, double realTotalTime, double maxJobTime, double workerTime)> CalculateAsync(CancellationToken cancelToken, int seed)
        {
            double inputAssignmentTime = AssignmentTime, inputJobTime = JobTime,
                assignmentVolatility = AssignmentTimeVolatility, jobTimeVolatility = JobTimeVolatility;
            // If Machines > 0, Workers is the number of workers by machine, and the total number of workers will be workers * machines
            if (Workers == 0 || Jobs == 0 || inputJobTime == 0)
                return Task<(double, double, double, double)>.FromResult((0.0, 0.0, 0.0, 0.0));
            return Task<(double, double, double, double)>.Run(() =>
            {
                // This will be the time of the last assignation
                double time = 0;
                double lastTime = 0;
                double maxJobTime = 0;
                var jobDist = Distribution(inputJobTime, jobTimeVolatility, seed);
                var assignmentDist = Distribution(inputAssignmentTime, assignmentVolatility, seed + 1);
                // The worker time will be calculated with the average value of the difference between end times for each worker,
                // so we can only begin the calculation once all the workers have been filled
                double workerTime = Jobs > TotalWorkers ? 0 : WorkerTime();
                // Sorted set to manage the ideal worker times, where the end time of the last job assigned to the worker is stored,
                // so we can always assign the next job to the worker that will be free first in the ideal grid
                WorkersTimes workersIdealTime = new();
                // Sorted set to manage the real worker times, where the end time of the last job assigned to the worker is stored,
                // so we can always assign the next job to the worker that will be free first in the simluation grid
                WorkersTimes workersTime = new();
                // We need to simulate the process of assigning jobs to machines and workers,
                // where each machine can have a queue of jobs assigned to it, and each worker in the machine will take the jobs in order.
                // We will use a sorted set for each machine to manage the end times of the workers in that machine,
                // so we can always assign the next job to the worker that will be free first in that machine.
                int machines = UseMachines() ? Machines : Workers;
                int workersByMachine = UseMachines() ? Workers : 1;
                List<WorkersTimes> machinesWorkersTimes = Enumerable.Range(0, machines).Select(_ => new WorkersTimes()).ToList();
                int finishedJobs = 0;
                while (finishedJobs < Jobs)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;
                    double nextTime = double.MaxValue;
                    WorkersTimes nextMachine = null;
                    foreach (var machine in machinesWorkersTimes)
                    {
                        if (machine.Count == 0)
                        {
                            nextMachine = machine;
                            nextTime = time;
                            break;
                        }
                        else if (machine.First().time < nextTime)
                        {
                            nextMachine = machine;
                            nextTime = machine.First().time;
                        }
                    }
                    if (nextTime > time)
                    {
                        time = nextTime;
                    }
                    double assignmentTime = (assignmentDist == null ? inputAssignmentTime : assignmentDist.Sample());
                    List<double> finishedWorkersEndTimes = new();
                    if (finishedJobs >= TotalWorkers)
                    {
                        finishedWorkersEndTimes.AddRange(nextMachine.Where(w => w.time <= time).Select(w => w.time));
                    }
                    // Remove all the finished workers
                    nextMachine?.RemoveWhere(w => w.time <= time);
                    time += assignmentTime;
                    int jobsToAssign = Math.Min(workersByMachine - nextMachine.Count, Jobs - finishedJobs);
                    List<double> jobTimes = Enumerable.Range(0, jobsToAssign).Select(_ => jobDist == null ? JobTime : jobDist.Sample()).ToList();
                    for (int i = 0; i < jobsToAssign; i++)
                    {
                        double jobTime = jobTimes[i];
                        if (jobTime > maxJobTime)
                        {
                            maxJobTime = jobTime;
                        }
                        // In the ideal grid, the assignment time is 0, so we don't have to take
                        // account of the different machines, only the total number or workers.
                        // The worker time (= difference between the ending time of a job and the the ending time of the next one)
                        // in this case always be equal to the job time
                        if (workersIdealTime.Count == TotalWorkers)
                        {
                            var firstWorker = workersIdealTime.First();
                            workersIdealTime.Remove(firstWorker);
                            workersIdealTime.Add((firstWorker.time + jobTime, firstWorker.id));
                        }
                        else
                        {
                            workersIdealTime.Add((jobTime, workersIdealTime.Count));
                        }

                        double nextWorkerTime = time + jobTime;
                        if (finishedWorkersEndTimes.Count > i)
                        {
                            workerTime = ((finishedJobs + i - TotalWorkers) * workerTime + nextWorkerTime - finishedWorkersEndTimes[i]) /
                                (finishedJobs + i + 1 - TotalWorkers);

                        }
                        if (nextWorkerTime > lastTime)
                        {
                            lastTime = nextWorkerTime;
                        }
                        nextMachine.Add((nextWorkerTime, finishedJobs));
                    }
                    finishedJobs += jobsToAssign;
                }
                return (workersIdealTime.Last().time, lastTime, maxJobTime, workerTime);
            });
        }
        public async Task<double> Simulate(CancellationToken cancelToken, int seed)
        {
            //All times in seconds
            if (TotalWorkers == 0)
                return 0;
            double time = 0, idealTime = 0;
            var jobDist = Distribution(JobTime, JobTimeVolatility, seed);
            var assignmentDist = Distribution(AssignmentTime, AssignmentTimeVolatility, seed + 1);
            List<int> FWQ = Enumerable.Range(0, (int)TotalWorkers).ToList();
            // Time and worker index of all the running workers
            WorkersTimes activeWorkersTime = new();
            // Dictionary to manage the ideal and real worker last times: 
            // - The first item is worker position
            // - The second item is the last time when the worker ends the job 
            Dictionary<int, double> workersLastTimes = new Dictionary<int, double>();
            Dictionary<int, double> workersIdealLastTimes = new Dictionary<int, double>();

            // The worker time will be calculated with the average value of the difference between end times for each worker,
            // so we can only begin the calculation once all the workers have been filled
            double workerTime = Jobs > TotalWorkers ? 0 : WorkerTime();
            // Release a worker that has finished its job
            async Task<bool> ReleaseWorker((double endTime, int worker) activeWorker, double timeBefore)
            {
                int ms = (int)((activeWorker.endTime - timeBefore) * 1000);
                bool waited = false;
                if (ms > 0)
                {
                    await Task.Delay(ms);
                    waited = true;
                }
                FWQ.Add(activeWorker.worker);
                AddFreeWorker?.Invoke(FWQ);
                return waited;
            }
            // When using machines, return the index of the machine (starting with 1)
            // corresponding to the worker; otherwise, return 0
            int Machine(int worker)
            {
                if (!UseMachines())
                {
                    return 0;
                }
                return 1 + worker / Workers;
            }
            // Assigning all jobs
            int finishedJobs = 0;
            while (finishedJobs < Jobs)
            {
                if (cancelToken.IsCancellationRequested)
                    break;
                List<int> assignedWorkers = new();
                int firstAssignedWorker = FWQ[0];
                int machine = Machine(FWQ[0]);
                if (machine == 0)
                {
                    assignedWorkers.Add(firstAssignedWorker);
                }
                else
                {
                    if (workersLastTimes.Count < TotalWorkers)
                    {
                        // We are still filling the grid in the first pass
                        assignedWorkers.AddRange(FWQ.Where(w => w >= firstAssignedWorker && w < Jobs && w < firstAssignedWorker + Workers));
                    }
                    else
                    {
                        int j = finishedJobs;
                        assignedWorkers.AddRange(workersLastTimes.Where(w => Machine(w.Key) == machine && w.Value <= time && j++ < Jobs).Select(w => w.Key));
                    }
                }
                List<double> jobTimes = assignedWorkers.Select(_ => jobDist == null ? JobTime : jobDist.Sample()).ToList();
                // In the ideal grid, the assignment time is 0: the worker time 
                // (= difference between the ending time of a job and the the ending time of the next one)
                // always be equal to the job time
                for (int i = 0; i < assignedWorkers.Count; i++)
                {
                    if (workersIdealLastTimes.ContainsKey(assignedWorkers[i]))
                    {
                        workersIdealLastTimes[assignedWorkers[i]] = workersIdealLastTimes[assignedWorkers[i]] + jobTimes[i];
                    }
                    else
                    {
                        workersIdealLastTimes.Add(assignedWorkers[i], jobTimes[i]);
                    }
                    if (workersIdealLastTimes[assignedWorkers[i]] > idealTime)
                    {
                        idealTime = workersIdealLastTimes[assignedWorkers[i]];
                    }
                }
                double assignmentTime = (assignmentDist == null ? AssignmentTime : assignmentDist.Sample());
                AssignmentStart?.Invoke(FWQ, assignedWorkers, jobTimes.Max(), assignmentTime);

                #region Free all workers that end while assigning the new job
                double freeWorkerTime = time;
                bool waited = false;
                while (activeWorkersTime.Count > 0 && activeWorkersTime.First().time < time + assignmentTime)
                {
                    waited = await ReleaseWorker(activeWorkersTime.First(), freeWorkerTime);
                    freeWorkerTime = activeWorkersTime.First().time;
                    activeWorkersTime.Remove(activeWorkersTime.First());
                }
                time += assignmentTime;
                int ms = (int)((time - freeWorkerTime) * 1000);
                if (ms > 0 || !waited)
                {
                    // If ms == 0 but there hasn't been any previous call to await, just make one to ensure
                    // the GUI is refreshed
                    await Task.Delay(ms > 0 ? ms : 1);
                }
                #endregion


                double finishedWorkersLastTimes = 0;
                for (int i = 0; i < assignedWorkers.Count; i++)
                {
                    double workerLastTime = workersLastTimes.ContainsKey(assignedWorkers[i]) ?
                                    time + jobTimes[i] - workersLastTimes[assignedWorkers[i]] :
                                    jobTimes[i];

                    //Assign to an active worker
                    if (workersLastTimes.ContainsKey(assignedWorkers[i]))
                    {
                        finishedWorkersLastTimes += time + jobTimes[i] - workersLastTimes[assignedWorkers[i]];
                        workersLastTimes[assignedWorkers[i]] = time + jobTimes[i];
                    }
                    else
                    {
                        workersLastTimes.Add(assignedWorkers[i], time + jobTimes[i]);
                    }
                    activeWorkersTime.Add((time + jobTimes[i], assignedWorkers[i]));
                }
                // We start to compute the worker times only when the grid is full
                if (finishedJobs >= TotalWorkers)
                {
                    workerTime = ((finishedJobs - TotalWorkers) * workerTime + finishedWorkersLastTimes) / (finishedJobs + assignedWorkers.Count - TotalWorkers);
                }

                FWQ.RemoveAll(w => assignedWorkers.Contains(w));
                AssignmentEnd?.Invoke(FWQ, assignedWorkers, workerTime);
                if (FWQ.Count == 0)
                {
                    await ReleaseWorker(activeWorkersTime.First(), time);
                    time = activeWorkersTime.First().time;
                    activeWorkersTime.Remove(activeWorkersTime.First());
                }
                else
                {
                    await Task.Delay(1);
                }
                finishedJobs += assignedWorkers.Count;
            }
            // Releasing remaining active workers
            foreach (var activeWorker in activeWorkersTime)
            {
                time = activeWorker.time;
                await ReleaseWorker(activeWorker, time);
            }
            return time;
        }
        private PropertyInfo[] BasicProperties()
        {
            // Return only public instance properties that can be read and written
            return GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .ToArray();
        }

        public void Save()
        {
            foreach (var property in BasicProperties())
            {
                string key = $"WRONJModel.{property.Name}";
                object? value = property.GetValue(this);
                Type t = property.PropertyType;

                try
                {
                    if (value == null)
                    {
                        // remove stored key if value null
                        if (Preferences.ContainsKey(key))
                            Preferences.Remove(key);
                        continue;
                    }

                    if (t == typeof(int))
                        Preferences.Set(key, (int)value);
                    else if (t == typeof(double))
                        Preferences.Set(key, (double)value);
                    else if (t == typeof(bool))
                        Preferences.Set(key, (bool)value);
                    else if (t == typeof(string))
                        Preferences.Set(key, (string)value);
                    else if (t.IsEnum)
                        Preferences.Set(key, value.ToString());
                    else
                    {
                        // fallback: serialize complex objects as JSON
                        string json = JsonSerializer.Serialize(value);
                        Preferences.Set(key, json);
                    }
                }
                catch
                {
                    // ignore individual property save errors
                }
            }
        }

        public void Load()
        {
            foreach (var property in BasicProperties())
            {
                string key = $"WRONJModel.{property.Name}";
                Type t = property.PropertyType;

                try
                {
                    if (!Preferences.ContainsKey(key))
                        continue;

                    if (t == typeof(int))
                    {
                        int v = Preferences.Get(key, default(int));
                        property.SetValue(this, v);
                    }
                    else if (t == typeof(double))
                    {
                        double v = Preferences.Get(key, default(double));
                        property.SetValue(this, v);
                    }
                    else if (t == typeof(bool))
                    {
                        bool v = Preferences.Get(key, default(bool));
                        property.SetValue(this, v);
                    }
                    else if (t == typeof(string))
                    {
                        string? v = Preferences.Get(key, default(string));
                        property.SetValue(this, v ?? string.Empty);
                    }
                    else if (t.IsEnum)
                    {
                        string? s = Preferences.Get(key, default(string));
                        if (!string.IsNullOrEmpty(s))
                        {
                            object? enumVal = Enum.Parse(t, s);
                            property.SetValue(this, enumVal);
                        }
                    }
                    else
                    {
                        // attempt to read JSON and deserialize
                        string? json = Preferences.Get(key, default(string));
                        if (!string.IsNullOrEmpty(json))
                        {
                            object? obj = JsonSerializer.Deserialize(json, t);
                            if (obj != null)
                                property.SetValue(this, obj);
                        }
                    }
                }
                catch
                {
                    // ignore individual property load errors
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