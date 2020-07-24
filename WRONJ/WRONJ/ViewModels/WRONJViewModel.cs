using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Threading;
using MathNet.Numerics.Financial;

namespace WRONJ.ViewModels
{
    public class JobInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private MD5 md5Hasher = MD5.Create();
        private int jobNumber,jobs;
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Jobs"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JobColor"));
                }
            }
        }
        public Color JobColor
        {
            get
            {
                var hashed = md5Hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(jobNumber.ToString()));
                var iValue = BitConverter.ToInt32(hashed, 0);
                if (jobNumber > jobs)
                    return Color.Transparent;
                return Color.FromHsla((iValue % 1000) / 1000.0, 0.5, 0.5);
            }
        }
    }
    public class JobsInfo
    {
        private Dictionary<int, JobInfo> jobsInfo = new Dictionary<int, JobInfo>();
        private int jobs;
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
        public Models.WRONJModel Model{ get;}
        public WRONJViewModel(Models.WRONJModel model)
        {
            Model = model??new Models.WRONJModel();
        }
        /// <summary>
        /// Convert model AssignmentTime to milliseconds
        /// </summary>
        public double AssignmentTime
        {
            get { return Model.AssignmentTime*1000; }
            set {
                double v=Model.AssignmentTime * 1000;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTime = v / 1000;
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
                double v = Model.AssignmentTimeVolatility * 1000;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTimeVolatility = v / 1000;
                    ChangeOutputData();
                }
            }
        }
        public double JobTime
        {
            get { return Model.JobTime; }
            set
            {
                double v = Model.JobTime;
                if (SetProperty(ref v, value))
                {
                    Model.JobTime = v;
                    ChangeOutputData();
                }
            }
        }
        public double JobTimeVolatility
        {
            get { return Model.JobTimeVolatility; }
            set
            {
                double v = Model.JobTimeVolatility;
                if (SetProperty(ref v, value))
                {
                    Model.JobTimeVolatility = v;
                    ChangeOutputData();
                }
            }
        }
        private double lastJobTime, lastAssignmentTime;
        public double LastJobTime
        {
            get { return lastJobTime; }
            set
            {
                SetProperty(ref lastJobTime, value);
            }
        }
        public double LastAssignmentTime
        {
            get { return lastAssignmentTime; }
            set
            {
                SetProperty(ref lastAssignmentTime, value);
            }
        }

        public int Workers
        {
            get { return Model.Workers; }
            set
            {
                int v = Model.Workers;
                if (SetProperty(ref v, value))
                {
                    Model.Workers = v;
                    ChangeOutputData();
                }
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
            get { return Workers > 0? (double)freeWorkers / Workers : 0; }
        }
        private void ChangeOutputData()
        {
            IdealTotalTime = Model.IdeallTotalTime();
            ModelWorkerTime = Model.ModelWorkerTime(Model.AssignmentTime, Model.JobTime);
            RealTotalTime = 0;
            RealWorkerTime = 0;
        }
        private int lastJob = 1;
        public int LastJob
        {
            get { return lastJob; }
            set {
                SetProperty(ref lastJob, value);
            }
        }
        public double modelWorkerTime, realWorkerTime,idealTotalTime,realTotalTime;
        public double ModelWorkerTime
        {
            get { return modelWorkerTime; }
            set
            {
                SetProperty(ref modelWorkerTime, value);
            }
        }
        public double RealWorkerTime
        {
            get { return realWorkerTime; }
            set
            {
                SetProperty(ref realWorkerTime, value);
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
        public double RealTotalTime
        {
            get { return realTotalTime; }
            set
            {
                SetProperty(ref realTotalTime, value);
            }
        }
        public int Jobs
        {
            get { return Model.Jobs; }
            set
            {
                int v = Model.Jobs;
                if (SetProperty(ref v, value))
                {
                    Model.Jobs = v;
                    this.JobsInfo = new JobsInfo(v);
                    ChangeOutputData();
                }
            }
        }
        public async Task Calculate(CancellationToken cancelToken)
        {
            var data =await Model.CalculateAsync(cancelToken);
            ModelWorkerTime = data.modelTime;
            RealWorkerTime = data.workerTime;
            IdealTotalTime = data.idealTotalTime;
            RealTotalTime = data.realTotalTime;
        }
        public Color WorkerColor(int worker) {
            return Color.FromHsla(worker * 0.7 / Model.Workers, 0.8, 0.5);
        }
        public JobsInfo JobsInfo { get; set; }
    }
}
