using System.ComponentModel;

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


        int idleWorkers;
        public int IdleWorkers
        {
            get { return idleWorkers; }
            set
            {
                if (SetProperty(ref idleWorkers, value))
                {
                    IdleWorkersRate = Model.TotalWorkers > 0 ? (double)idleWorkers / Model.TotalWorkers : 0;
                }
                OnPropertyChanged("IdleWorkersRate");
            }
        }
        double idleWorkersRate;
        public double IdleWorkersRate
        {
            get { return idleWorkersRate; }
            set
            {
                SetProperty(ref idleWorkersRate, value);
            }
        }
        public void ChangeOutputData()
        {
            JobTimeLimit = Model.JobTimeLimit();
            WorkersMachinesLimit = (int)Math.Round(Model.WorkersMachinesLimit());
            IdealTotalTime = Model.TotalTime(true);
            ModelTotalTime = Model.TotalTime(false);
            ModelWorkerTime = Model.WorkerTime();
            IdealCalculatedTotalTime = 0;
            CalculatedTotalTime = 0;
			CalculatedWorkerTime = 0;
			CalculatedMaxJobTime = 0;
            Seed = 1;
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
        double modelWorkerTime, simulationWorkerTime, calculatedWorkerTime;
        double idealTotalTime, idealCalculatedTotalTime;
        double modelTotalTime, simulationTotalTime, calculatedTotalTime;
        double jobTimeLimit, simulationMaxJobTime, calculatedMaxJobTime;

        int workersLimit;
        public double ModelWorkerTime
        {
            get { return modelWorkerTime; }
            set
            {
                SetProperty(ref modelWorkerTime, value);
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
        public double IdealCalculatedTotalTime
        {
            get { return idealCalculatedTotalTime; }
            set
            {
                SetProperty(ref idealCalculatedTotalTime, value);
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
        public double CalculatedTotalTime
        {
            get { return calculatedTotalTime; }
            set
            {
                SetProperty(ref calculatedTotalTime, value);
            }
        }
        public double SimulationWorkerTime
        {
            get { return simulationWorkerTime; }
            set
            {
                SetProperty(ref simulationWorkerTime, value);
            }
        }
        public double CalculatedWorkerTime
        {
            get { return calculatedWorkerTime; }
            set
            {
                SetProperty(ref calculatedWorkerTime, value);
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
        public double SimulationMaxJobTime
        {
            get { return simulationMaxJobTime; }
            set
            {
                SetProperty(ref simulationMaxJobTime, value);
            }
        }
        public double CalculatedMaxJobTime
        {
            get { return calculatedMaxJobTime; }
            set
            {
                SetProperty(ref calculatedMaxJobTime, value);
            }
        }
        public int WorkersMachinesLimit
        {
            get { return workersLimit; }
            set
            {
                SetProperty(ref workersLimit, value);
            }
        }
        bool showExtraInfo;
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
            IdealCalculatedTotalTime = data.idealTotalTime;
            CalculatedTotalTime = data.realTotalTime;
            CalculatedWorkerTime = data.workerTime;
            CalculatedMaxJobTime = data.maxJobTime;
        }
        public Microsoft.Maui.Graphics.Color WorkerColor(int worker, bool assigning = false)
        {
            int totalColors = Model.UseMachines() ? Model.Machines : Model.Workers;
            int indexColor = Model.UseMachines() ? worker / Model.Workers : worker;
            return Microsoft.Maui.Graphics.Color.FromHsla(indexColor * 0.7 / totalColors, 0.8, 0.5, assigning ? 0.1  : 1);
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
