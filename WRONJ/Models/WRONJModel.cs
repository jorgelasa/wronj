using System.Reflection;
using System.Text.Json;

/// Alias for the type used to manage the workers end times in the machines simulation,
/// along with an assignation id to allow repeated times in the sorted set
using WorkersMachine = System.Collections.Generic.SortedSet<(double time, int id)>;

namespace WRONJ.Models
{
    public class WRONJModel : ICloneable
    {
        public delegate void AssignmentStartEventHandler(List<int> workers, double jobTime, double assignmentTime);
        public delegate void AssignmentEndEventHandler(List<int> workers, int worker, string workerTime);
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
        public int EffectiveWorkers => UseMachines() ? Workers * Machines : Workers;
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
            return EffectiveAssignmentTime * (EffectiveWorkers - 1) > JobTime ? EffectiveAssignmentTime * EffectiveWorkers : JobTime + AssignmentTime;
        }
        public double JobTimeLimit()
        {
            if (EffectiveWorkers == 0)
                return 0;

            return EffectiveAssignmentTime * (EffectiveWorkers - 1);
        }
        public double WorkersLimit()
        {
            if (JobTime == 0 || JobTime == 0)
                return 0;
            return JobTime / JobTime + 1;
        }
        
        public double TotalTime(bool idealTime)
        {
            if (EffectiveWorkers == 0 || Jobs == 0)
                return 0;

            double assignmentTime = idealTime ? 0 : AssignmentTime;
            double effectiveAssignmentTime = idealTime ? 0 : EffectiveAssignmentTime;
            
            if (Jobs <= EffectiveWorkers || effectiveAssignmentTime > 0 && JobTime <= JobTimeLimit())
                return effectiveAssignmentTime * Jobs + JobTime;

            return (JobTime + assignmentTime) * (Jobs / EffectiveWorkers + (Jobs % AssignationUnits > 0 ? 1 : 0)) +
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
                double assignmentsTime = 0;
                // This will be the time of the last assignation
                double time = 0;
                double lastTime = 0;
                double maxJobTime = 0;
                var jobDist = Distribution(inputJobTime, jobTimeVolatility, seed);
                var assignmentDist = Distribution(inputAssignmentTime, assignmentVolatility, seed + 1);
                // The worker time will be calculated with the average value of the difference between end times for each worker,
                // so we can only begin the calculation once all the workers have been filled
                double workerTime = 0;
                Queue<double> jobsQue = new ();
                // Sorted set to manage the ideal worker times, where the end time of the last job assigned to the worker is stored,
                // so we can always assign the next job to the worker that will be free first in the ideal grid
                WorkersMachine workersIdealTime = new ();
                // Sorted set to manage the real worker times, where the end time of the last job assigned to the worker is stored,
                // so we can always assign the next job to the worker that will be free first in the simluation grid
                WorkersMachine workersTime = new ();
                for (int j = 0; j < Jobs; j++)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;
                    double jobTime = (jobDist == null ? inputJobTime : jobDist.Sample());
                    if (jobDist != null && jobTime > maxJobTime)
                    {
                        maxJobTime = jobTime;
                    }   
                    if (workersIdealTime.Count == EffectiveWorkers)
                    {
                        var firstWorker = workersIdealTime.First();
                        workersIdealTime.Remove(firstWorker);
                        // In the ideal grid, the assignment time is 0: the worker time 
                        // (= difference between the ending time of a job and the the ending time of the next one)
                        // always be equal to the job time
                        workersIdealTime.Add((firstWorker.time + jobTime, firstWorker.id));
                    }
                    else
                    {
                        workersIdealTime.Add((jobTime, workersIdealTime.Count));
                    }
                    if (UseMachines())
                    {
                        jobsQue.Enqueue(jobTime);
                    }
                    else 
                    {
                        double assignmentTime = (assignmentDist == null ? inputAssignmentTime : assignmentDist.Sample());
                        assignmentsTime = (j * assignmentsTime + assignmentTime) / (j + 1);
                        if (j >= EffectiveWorkers)
                        {
                            var firstWorker = workersTime.First();
                            if (firstWorker.time > time)
                            {
                                time = firstWorker.time;
                            }
                            time += assignmentTime;
                            workerTime = ((j - EffectiveWorkers) * workerTime + time + jobTime - firstWorker.time) / (j + 1 - EffectiveWorkers);
                            workersTime.Remove(firstWorker);
                            workersTime.Add((time + jobTime, firstWorker.id));
                        }
                        else
                        {
                            time += assignmentTime;
                            workersTime.Add((time + jobTime, workersTime.Count));
                        }
                        lastTime = workersTime.Last().time;
                    }
                }
                // If we are using machines, we need to simulate the process of assigning jobs to machines and workers,
                // where each machine can have a queue of jobs assigned to it, and each worker in the machine will take the jobs in order.
                // We will use a sorted set for each machine to manage the end times of the workers in that machine,
                // so we can always assign the next job to the worker that will be free first in that machine.
                List<WorkersMachine> machinesWorkersTimes = Enumerable.Range(0, Machines).Select(_ => new WorkersMachine()).ToList();

                // This will be the siumulation process when machines > 0, where we have a queue of jobs
                // that we can assign in batches to the different machines
                int job = 0;
                while (jobsQue.Count > 0)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;
                    double nextTime = double.MaxValue;
                    WorkersMachine nextMachine = null;
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
                    if (job >= EffectiveWorkers)
                    {
                        finishedWorkersEndTimes.AddRange(nextMachine.Where(w => w.time <= time).Select(w => w.time));
                    }
                    // Remove all the finished workers
                    nextMachine?.RemoveWhere(w => w.time <= time);
                    time += assignmentTime;
                    int jobsToAssign = Math.Min(Workers - nextMachine.Count, jobsQue.Count);
                    for (int i = 0; i < jobsToAssign; i++)
                    {
                        double nextWorkerTime = time + jobsQue.Dequeue();
                        if (finishedWorkersEndTimes.Count > i)
                        {
                            workerTime = ((job + i - EffectiveWorkers) * workerTime + nextWorkerTime - finishedWorkersEndTimes[i]) / 
                                (job + i + 1 - EffectiveWorkers);

                        }
                        if (nextWorkerTime > lastTime)
                        {
                            lastTime = nextWorkerTime;
                        }
                        nextMachine.Add((nextWorkerTime, job));
                    }
                    job += jobsToAssign;

                }
                return (workersIdealTime.Last().time, lastTime, maxJobTime, workerTime);
            });
        }
        public async void Simulate(CancellationToken cancelToken, int seed)
        {
            //All times in seconds
            double inputAssignmentTime = AssignmentTime,
                assignmentVolatility = AssignmentTimeVolatility;
            int workers = Workers;
            if (workers == 0)
                return;
            double time = 0, idealTime = 0;
            var jobDist = Distribution(JobTime, JobTimeVolatility, seed);
            var assignmentDist = Distribution(inputAssignmentTime, assignmentVolatility, seed + 1);
            List<int> FWQ = Enumerable.Range(0, (int)workers).ToList();
            // Sorted set to manage the real worker times 
            SortedSet<(double endTime, int position)> workersTime = new SortedSet<(double, int)>();
            // Dictionary to manage the ideal and real worker last times: 
            // - The first item is worker position
            // - The second item is the last time when the worker ends the job 
            Dictionary<int, double> workersLastTime = new Dictionary<int, double>();
            Dictionary<int, double> workersIdealLastTime = new Dictionary<int, double>();
            double workerTime = Jobs > workers ? 0 : WorkerTime();
            double assignmentsTime = 0, jobsTime = 0;
            double timeBetweenEndings = 0, timeLastEnding = 0;
            int endedCount = 0;
            async Task<bool> freeWorker((double endTime, int worker) activeWorker, double timeBefore)
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
            }
            ;
            // Assigning all jobs
            for (int j = 0; j < Jobs; j++)
            {
                if (cancelToken.IsCancellationRequested)
                    break;
                int assignedWorker = FWQ[0];
                double jobTime = (jobDist == null ? JobTime : jobDist.Sample());
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
                int ms = (int)((time - freeWorkerTime) * 1000);
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
                    workerTime = ((j - workers) * workerTime + workerLastTime) / (j + 1 - workers);
                }

                AssignmentEnd?.Invoke(FWQ, assignedWorker, workerTime > 0 ? string.Format("{0:F4}", workerTime): "");
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