using System;
using System.Transactions;
using WRONJ.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.ComponentModel;

namespace WRONJ.ViewModels
{
    public class WRONJViewModel : BaseViewModel
    {
        public Models.WRONJModel Model{ get;}
        public WRONJViewModel(Models.WRONJModel model)
        {
            Model = model??new Models.WRONJModel();
        }
        public double AssignmentTime
        {
            get { return Model.AssignmentTime; }
            set {
                double v=Model.AssignmentTime;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTime = v;
                    OnPropertyChanged("ModelTime");
                }
            }                
        }
        public double AssignmentTimeVolatility
        {
            get { return Model.AssignmentTimeVolatility; }
            set
            {
                double v = Model.AssignmentTimeVolatility;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTimeVolatility = v;
                    OnPropertyChanged("ModelTime");
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
                    OnPropertyChanged("ModelTime");
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
                    OnPropertyChanged("ModelTime");
                }
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
                    OnPropertyChanged("ModelTime");
                }
            }
        }
        private int lastJob = 1;
        public int LastJob
        {
            get { return lastJob; }
            set {
                SetProperty(ref lastJob, value);
            }
        }

        public double ModelTime
        {
            get { return Model.ModelTime; }
        }
        public double RealJobTime
        {
            get { return Model.RealJobTime; }
        }        
        public int JobNumber
        {
            get { return Model.JobNumber; }
            set
            {
                int v = Model.JobNumber;
                if (SetProperty(ref v, value))
                {
                    Model.JobNumber = v;
                }
            }
        }
        public async Task CalculateDataUntilConvergence()
        {
            await Model.Calculate();
            OnPropertyChanged("RealJobTime");
            OnPropertyChanged("JobNumber");
        }
        public Color WorkerColor(int worker) {
            return Color.FromHsla(worker * 0.7 / Model.Workers, 0.8, 0.5);
        }
        public class JobInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
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
                    return Color.FromHsla((jobNumber % 16)/16.0 , 0.8, 0.5);
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
