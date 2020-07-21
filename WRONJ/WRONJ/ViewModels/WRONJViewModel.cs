using System;
using System.Transactions;
using WRONJ.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.ComponentModel;
using System.Security.Cryptography;

namespace WRONJ.ViewModels
{
    public class WRONJViewModel : BaseViewModel
    {
        public Models.WRONJModel Model{ get;}
        public WRONJViewModel(Models.WRONJModel model)
        {
            Model = model??new Models.WRONJModel();
        }
        /// <summary>
        /// Convert model to milliseconds
        /// </summary>
        public double AssignmentTime
        {
            get { return Model.AssignmentTime*1000; }
            set {
                double v=Model.AssignmentTime * 1000;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTime = v / 1000;
                    ChangeInputData();
                }
            }                
        }
        /// <summary>
        /// Convert model to milliseconds
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
                    ChangeInputData();
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
                    ChangeInputData();
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
                    ChangeInputData();
                }
            }
        }
        public uint Workers
        {
            get { return Model.Workers; }
            set
            {
                uint v = Model.Workers;
                if (SetProperty(ref v, value))
                {
                    Model.Workers = v;
                    ChangeInputData();
                }
            }
        }
        private bool showOutputData;
        public bool ShowOutputData
        {
            get { return showOutputData; }
            set
            {
                SetProperty(ref showOutputData, value);
            }
        }
        private void ChangeInputData()
        {
            ShowOutputData = false;
            ModelTime = Model.ModelTime(Model.AssignmentTime, Model.JobTime);
        }
        private int lastJob = 1;
        public int LastJob
        {
            get { return lastJob; }
            set {
                SetProperty(ref lastJob, value);
            }
        }
        public double modelTime, workerTime;
        public double ModelTime
        {
            get { return modelTime; }
            set
            {
                SetProperty(ref modelTime, value);
            }
        }
        public double WorkerTime
        {
            get { return workerTime; }
            set
            {
                SetProperty(ref workerTime, value);
            }
        }
        public uint JobNumber
        {
            get { return Model.JobNumber; }
            set
            {
                uint v = Model.JobNumber;
                if (SetProperty(ref v, value))
                {
                    Model.JobNumber = v;
                }
            }
        }
        public async Task CalculateDataUntilConvergence()
        {
            var data=await Model.Calculate();
            ModelTime = data.Item1;
            WorkerTime = data.Item2;
            ShowOutputData = true;
        }
        public Color WorkerColor(int worker) {
            return Color.FromHsla(worker * 0.7 / Model.Workers, 0.8, 0.5);
        }
        public class JobInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private MD5 md5Hasher = MD5.Create();
            private int jobNumber;
            public int JobNumber {
                get {
                    return jobNumber;
                }
                set
                {
                    if (jobNumber!= value)
                    {
                        jobNumber = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JobNumber"));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JobColor"));
                    }
                } 
            }
            public Color JobColor
            {
                get {
                    var hashed = md5Hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(jobNumber.ToString()));
                    var iValue=BitConverter.ToUInt32(hashed, 0);
                    return Color.FromHsla((iValue % 1000)/ 1000.0, 0.5, 0.5);
                }
            }
        }
        public class JobsInfo
        {
            private Dictionary<int, JobInfo> jobs = new Dictionary<int, JobInfo>();
            public JobInfo this[int jobNumber]
            {
                get
                {
                    if (!jobs.ContainsKey(jobNumber))
                    {
                        jobs.Add(jobNumber, new JobInfo());
                    }
                    return jobs[jobNumber];
                }
            }
        }
        public JobsInfo Jobs { get; set; } = new JobsInfo();

    }
}
