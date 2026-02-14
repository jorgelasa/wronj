using System.ComponentModel;
using WRONJ.Models;
using SD = System.Drawing;

namespace WRONJ.ViewModels
{
    public class JobInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private int jobNumber;
        private readonly int jobs;
        public JobInfo(int jobs)
        {
            this.jobs = jobs;
        }
        public int JobNumber
        {
            get
            {
                return jobNumber;
            }
            set
            {
                if (jobNumber != value)
                {
                    jobNumber = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JobColor"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Glyph"));
                }
            }
        }
        public Microsoft.Maui.Graphics.Color JobColor
        {
            get
            {
                if (jobNumber > jobs)
                    return Microsoft.Maui.Graphics.Colors.Transparent;
                return Microsoft.Maui.Graphics.Color.FromHsla(0.5, 0.5, 0.5 + (jobNumber % 16) / 48.0);
            }
        }
        public string Glyph
        {
            get
            {
                //Codes between \uf001 and \ufe7d
                char c = (char)(61441 + jobNumber % 3700);
                return c.ToString();
            }
        }
    }
    public class JobsInfo
    {
        private readonly Dictionary<int, JobInfo> jobsInfo = new Dictionary<int, JobInfo>();
        private readonly int jobs;
        public JobsInfo(int jobs)
        {
            this.jobs = jobs;
        }
        public JobInfo this[int jobNumber]
        {
            get
            {
                if (!jobsInfo.ContainsKey(jobNumber))
                {
                    jobsInfo.Add(jobNumber, new JobInfo(jobs));
                }
                return jobsInfo[jobNumber];
            }
        }
    }
    public class WRONJViewModel : BaseViewModel
    {
        public Models.WRONJModel Model { get; }
        public WRONJViewModel(Models.WRONJModel model)
        {
            Model = model ?? new Models.WRONJModel();
        }
        public int Jobs
        {
            get { return Model.Jobs; }
            set
            {
                if (SetProperty(Model, value))
                {
                    JobsInfo = new JobsInfo(value);
                    ChangeOutputData();
                }
            }
        }
        public int Workers
        {
            get { return Model.Workers; }
            set
            {
                if (SetProperty(Model, value))
                {
                    ChangeOutputData();
                }
            }
        }
        public int Machines
        {
            get { return Model.Machines; }
            set
            {
                if (SetProperty(Model, value))
                {
                    ChangeOutputData();
                }
            }
        }
        /// <summary>
        /// Convert model AssignmentTime to milliseconds
        /// </summary>
        public double AssignmentTime
        {
            get { return Model.AssignmentTime * 1000; }
            set
            {
                if (SetProperty(Model, value / 1000))
                {
                    ChangeOutputData();
                }
            }
        }
        /// <summary>
        /// Convert model AssignmentTimeVolatility to milliseconds
        /// </summary>
        public double AssignmentTimeVolatility
        {
            get { return Model.AssignmentTimeVolatility * 1000; }
            set
            {
                if (SetProperty(Model, value / 1000))
                {
                    ChangeOutputData();
                }
            }
        }
        public double JobTime
        {
            get { return Model.JobTime; }
            set
            {
                if (SetProperty(Model, value))
                {
                    ChangeOutputData();
                }
            }
        }
        public double JobTimeVolatility
        {
            get { return Model.JobTimeVolatility; }
            set
            {
                if (SetProperty(Model, value))
                {
                    ChangeOutputData();
                }
            }
        }

        public bool MachineAssignment
        {
            get { return Model.MachineAssignment; }
            set
            {
                if (SetProperty(Model, value))
                {
                    ChangeOutputData();
                }
            }
        }

        private double nextJobTime, nextAssignmentTime;
        public double NextJobTime
        {
            get { return nextJobTime; }
            set
            {
                SetProperty(ref nextJobTime, value);
            }
        }
        public double NextAssignmentTime
        {
            get { return nextAssignmentTime; }
            set
            {
                SetProperty(ref nextAssignmentTime, value);
            }
        }

        int freeWorkers;
        public int FreeWorkers
        {
            get { return freeWorkers; }
            set
            {
                if (SetProperty(ref freeWorkers, value)) OnPropertyChanged("FreeWorkersRate");
            }
        }
        public double FreeWorkersRate
        {
            get { return Workers > 0 ? (double)freeWorkers / Workers : 0; }
        }
        public void ChangeOutputData()
        {
            JobTimeLimit = Model.JobTimeLimit();
            WorkersLimit = (int)Math.Round(Model.WorkersLimit());
            IdealTotalTime = Model.TotalTime(true);
            ModelTotalTime = Model.TotalTime(false);
            ModelWorkerTime = Model.WorkerTime();
            IdealSimulationTotalTime = 0;
            SimulationTotalTime = 0;
            SimulationWorkerTime = "";
            SimulationMaxJobTime = 0;
            Seed = 1;
            VariableTimes = AssignmentTimeVolatility > 0 || JobTimeVolatility > 0;
        }
        private int nextJob = 1;
        public int NextJob
        {
            get { return nextJob; }
            set
            {
                SetProperty(ref nextJob, value);
            }
        }
        double modelWorkerTime;
        string simulationWorkerTime;
        double idealTotalTime, idealSimulationTotalTime;
        double modelTotalTime, simulationTotalTime;
        double jobTimeLimit, timeBetweenEndings, simulationMaxJobTime;

        int workersLimit;
        public double ModelWorkerTime
        {
            get { return modelWorkerTime; }
            set
            {
                SetProperty(ref modelWorkerTime, value);
            }
        }
        public string SimulationWorkerTime
        {
            get { return simulationWorkerTime; }
            set
            {
                SetProperty(ref simulationWorkerTime, value);
            }
        }
        public double IdealTotalTime
        {
            get { return idealTotalTime; }
            set
            {
                SetProperty(ref idealTotalTime, value);
            }
        }
        public double IdealSimulationTotalTime
        {
            get { return idealSimulationTotalTime; }
            set
            {
                SetProperty(ref idealSimulationTotalTime, value);
            }
        }
        public double ModelTotalTime
        {
            get { return modelTotalTime; }
            set
            {
                SetProperty(ref modelTotalTime, value);
            }
        }
        public double SimulationTotalTime
        {
            get { return simulationTotalTime; }
            set
            {
                SetProperty(ref simulationTotalTime, value);
            }
        }
        public double JobTimeLimit
        {
            get { return jobTimeLimit; }
            set
            {
                SetProperty(ref jobTimeLimit, value);
            }
        }
        public double TimeBetweenEndings
        {
            get { return timeBetweenEndings; }
            set
            {
                SetProperty(ref timeBetweenEndings, value);
            }
        }
        public double SimulationMaxJobTime
        {
            get { return simulationMaxJobTime; }
            set
            {
                SetProperty(ref simulationMaxJobTime, value);
            }
        }
        public int WorkersLimit
        {
            get { return workersLimit; }
            set
            {
                SetProperty(ref workersLimit, value);
            }
        }
        bool variableTimes, showExtraInfo;
        public bool VariableTimes
        {
            get { return variableTimes; }
            set
            {
                SetProperty(ref variableTimes, value);
            }
        }
        public bool ShowExtraInfo
        {
            get { return showExtraInfo; }
            set
            {
                SetProperty(ref showExtraInfo, value);
            }
        }
        public int Seed { get; set; }
        public async Task Calculate(CancellationToken cancelToken)
        {            
            var data = await Model.CalculateAsync(cancelToken, ++Seed);
            IdealSimulationTotalTime = data.idealTotalTime;
            SimulationTotalTime = data.realTotalTime;
            SimulationWorkerTime = Jobs > Model.EffectiveWorkers ? string.Format("{0:F4}",data.workerTime) : "Jobs > Total workers is required";
            SimulationMaxJobTime = data.maxJobTime;
        }
        public Microsoft.Maui.Graphics.Color WorkerColor(int worker)
        {
            return Microsoft.Maui.Graphics.Color.FromHsla(worker * 0.7 / Model.Workers, 0.8, 0.5);
        }
        JobsInfo jobsInfo;
        public JobsInfo JobsInfo
        {
            get
            {
                if (jobsInfo == null) jobsInfo = new JobsInfo(Jobs);
                return jobsInfo;
            }
            set
            {
                jobsInfo = value;
            }
        }
        const double fontSize = 14;

    }
}
