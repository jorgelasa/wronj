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
        public bool RandomAssignmentTimeVolatility
        {
            get { return Model.RandomAssignmentTimeVolatility; }
            set
            {
                SetProperty(Model, value);
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
        public bool RandomJobTimeVolatility
        {
            get { return Model.RandomJobTimeVolatility; }
            set
            {
                SetProperty(Model, value);
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
            JobTimeLimit = WRONJModel.JobTimeLimit(Model.AssignmentTime, Model.Workers);
            WorkersLimit = (int)Math.Round(WRONJModel.WorkersLimit(Model.AssignmentTime, Model.JobTime));
            IdealTotalTime = WRONJModel.TotalTime(Model.JobTime, Model.Workers, Model.Jobs);
            ModelTotalTime = WRONJModel.TotalTime(Model.JobTime, Model.Workers, Model.Jobs, Model.AssignmentTime);
            ModelWorkerTime = WRONJModel.WorkerTime(Model.AssignmentTime, Model.JobTime, Model.Workers);
            IdealTotalTimeVol = 0;
            ModelTotalTimeVol = 0;
            ModelWorkerTimeVol = 0;
            VariableTimes = AssignmentTimeVolatility > 0 || JobTimeVolatility > 0;
            EnableCharts = Workers > 1 && AssignmentTime > 0 && JobTime > 0;
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
        double modelWorkerTime, modelWorkerTimeVol;
        double idealTotalTime, idealTotalTimeVol;
        double modelTotalTime, modelTotalTimeVol;
        double jobTimeLimit, timeBetweenEndings;
        int workersLimit;
        public double ModelWorkerTime
        {
            get { return modelWorkerTime; }
            set
            {
                SetProperty(ref modelWorkerTime, value);
            }
        }
        public double ModelWorkerTimeVol
        {
            get { return modelWorkerTimeVol; }
            set
            {
                SetProperty(ref modelWorkerTimeVol, value);
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
        public double IdealTotalTimeVol
        {
            get { return idealTotalTimeVol; }
            set
            {
                SetProperty(ref idealTotalTimeVol, value);
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
        public double ModelTotalTimeVol
        {
            get { return modelTotalTimeVol; }
            set
            {
                SetProperty(ref modelTotalTimeVol, value);
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
        public int WorkersLimit
        {
            get { return workersLimit; }
            set
            {
                SetProperty(ref workersLimit, value);
            }
        }
        bool variableTimes, enableCharts, showExtraInfo;
        public bool VariableTimes
        {
            get { return variableTimes; }
            set
            {
                SetProperty(ref variableTimes, value);
            }
        }
        public bool EnableCharts
        {
            get { return enableCharts; }
            set
            {
                SetProperty(ref enableCharts, value);
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

        public async Task Calculate(CancellationToken cancelToken)
        {
            var data = await Model.CalculateAsync(cancelToken);
            //ModelWorkerTime = data.modelTime;
            ModelWorkerTimeVol = data.workerTime;
            IdealTotalTimeVol = data.idealTotalTime;
            ModelTotalTimeVol = data.realTotalTime;
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
