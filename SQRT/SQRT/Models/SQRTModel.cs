using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Xamarin.Forms;
using MathNet.Numerics;

namespace SQRT.Models
{
    public class SQRTModel
    {
        /// <summary>
        /// Delegate used with a Free Slot Queue
        /// </summary>
        /// <param name="slots"></param>
        public delegate void FSQ(List<int> slots);
        public delegate void FSQAndTime(List<int> slots,double time);
        public delegate void FSQSAndSlot(List<int> slots, int slot);
        public event FSQAndTime AssignmentStart;
        public event FSQSAndSlot AssignmentEnd;
        public event FSQ FreeSlot;
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
        public int Slots { get; set; }
        public double ModelTime
        {
            get
            {
                return AssignmentTime * (Slots - 1) / 1000 > TaskTime ? AssignmentTime * Slots / 1000 : TaskTime + AssignmentTime / 1000;
            }
        }
        public double RealTaskTime { get; private set; }
        public int TaskNumber { get; set; }
        public double TotalTime
        {
            get
            {
                return TaskNumber * TaskTime / Slots;
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
            int slots = Slots, topTasks = TaskNumber;
            return Task<Tuple<double, int>>.Run(() =>
            {
                //Average of real task times
                double realTaskTime = 0;
                double lastTaskTime;
                double time = 0;
                int tasks = 0;
                //The first item is the time when the slot ends the task. 
                //The second is the time when the task was assigned
                SortedSet<Tuple<double, double>> slotsTime = new SortedSet<Tuple<double, double>>();
                const int topTasksWithinRange = 1000;
                double convergenceDiff = 0.00001;
                int lastTasksWithinRange = 0;
                MathNet.Numerics.Distributions.Normal taskDist = new MathNet.Numerics.Distributions.Normal(taskTime, taskTimeVolatility);
                MathNet.Numerics.Distributions.Normal assignmentDist = new MathNet.Numerics.Distributions.Normal(taskTime, assignmentVolatility);
                while (topTasks == 0 && lastTasksWithinRange < topTasksWithinRange
                || tasks < topTasks)
                {
                    double lastSlotEndTime = time;
                    if (slotsTime.Count == slots)
                    {
                        var firstSlot = slotsTime.First();
                        lastSlotEndTime = firstSlot.Item1;
                        if (lastSlotEndTime > time)
                            time = lastSlotEndTime;
                        slotsTime.Remove(firstSlot);
                    }
                    time += assignmentTime;
                    double rndTaskTime = taskTime;
                    if (taskTimeVolatility > 0)
                        rndTaskTime = taskDist.Sample();
                    double slotEndTime = time + rndTaskTime;
                    slotsTime.Add(Tuple.Create(slotEndTime, lastSlotEndTime));
                    lastTaskTime = realTaskTime;
                    realTaskTime = (tasks * realTaskTime + (slotEndTime - lastSlotEndTime)) / ++tasks;
                    //if (Math.Abs(realTaskTime - lastTaskTime) < convergenceDiff)
                    if (Math.Abs(realTaskTime - modelTime) < convergenceDiff)
                        lastTasksWithinRange++;
                    else
                        lastTasksWithinRange = 0;
                }
                return Tuple.Create(realTaskTime, tasks);
            });
        }
        public async void Simulate()
        {
            //All times in milliseconds
            double q = AssignmentTime , taskTime = TaskTime*1000, modelTime = ModelTime*1000, volatility = 0;
            int slots = Slots, tasks = TaskNumber;
            double time = 0;
            int ms = 0;
            MathNet.Numerics.Distributions.Normal normal = new MathNet.Numerics.Distributions.Normal(taskTime, volatility);
            List<int> FSQ= Enumerable.Range(0, slots-1).ToList();
            SortedSet<Tuple<double, int>> slotsTime = new SortedSet<Tuple<double, int>>();
            for (int i=1; i<=tasks; i++)
            {
                double rndTaskTime = taskTime;
                if (volatility > 0)
                    rndTaskTime = normal.Sample();
                // Getting a new task from pending queue
                AssignmentStart?.Invoke(FSQ, rndTaskTime);
                double freeSlotTime = time;
                // Free all slots that end while assigning the new task
                while (slotsTime.Count > 0 &&  slotsTime.First().Item1 < time + q)
                {
                    freeSlotTime = slotsTime.First().Item1;
                    FSQ.Add(slotsTime.First().Item2);
                    slotsTime.Remove(slotsTime.First());
                    FreeSlot?.Invoke(FSQ);
                }
                time += q;
                ms = (int) (time - freeSlotTime);
                await Task.Delay(ms > 0 ? ms : 1);
                int assignedSlot = FSQ[0];
                FSQ.RemoveAt(0);
                //Assign to an active slot
                AssignmentEnd?.Invoke(FSQ, assignedSlot);
                slotsTime.Add(Tuple.Create(time + rndTaskTime, assignedSlot));
                if (FSQ.Count == 0)
                {
                    double timeBefore = time;
                    time = slotsTime.First().Item1;
                    FSQ.Add(slotsTime.First().Item2);
                    slotsTime.Remove(slotsTime.First());
                    ms = (int)(time - timeBefore);
                    await Task.Delay(ms > 0 ? ms : 1);
                    FreeSlot?.Invoke(FSQ);
                }
            }
        }
    }
}