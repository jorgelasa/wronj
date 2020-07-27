using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Threading;
using MathNet.Numerics.Financial;
using WRONJ.Models;

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
                if (jobNumber > jobs)
                    return Color.Transparent;
                return Color.FromHsla(0.5,0.5,0.5 +(jobNumber % 16) / 48.0);
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
            get { return Model.AssignmentTime*1000; }
            set {
                if (SetProperty(Model, value/1000))
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
        public void ChangeOutputData()
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
        JobsInfo jobsInfo;
        public JobsInfo JobsInfo {
            get 
            {
                if (jobsInfo==null) jobsInfo = new JobsInfo(Jobs);
                return jobsInfo;
            }
            set
            {
                jobsInfo = value;
            }
        } 
    }
}
