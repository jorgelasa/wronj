using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Threading;
using MathNet.Numerics.Financial;
using WRONJ.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Annotations;

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
        public PlotModel TimesChart
        {
            get {
                double fontSize = 16;
                var chart = new PlotModel { Title = $"WT = f (JT), with {Workers} workers and AT={AssignmentTime:F2} ms" };
                double medianPoint = Model.JobTimeLimit();
                var idealSeries = new FunctionSeries( x => x,0,2*medianPoint, medianPoint/100,"Ideal Grid");
                idealSeries.FontSize = fontSize;
                var realSeries = new FunctionSeries(x => WRONJModel.ModelWorkerTime(Model.AssignmentTime,x,Workers), 
                                        0, 2 * medianPoint, medianPoint / 100,"WRONJ Grid");
                realSeries.FontSize = fontSize;
                realSeries.FontWeight = FontWeights.Bold;
                chart.Series.Add(idealSeries);
                chart.Series.Add(realSeries);
                var annotation = new LineAnnotation();
                annotation.Color = OxyColors.Red;
                annotation.MinimumY = 0;
                annotation.MaximumY = WRONJModel.ModelWorkerTime(Model.AssignmentTime, 2 * medianPoint, Workers);
                annotation.FontSize = fontSize;
                annotation.FontWeight = FontWeights.Bold;
                annotation.X = medianPoint;
                annotation.LineStyle = LineStyle.Solid;
                annotation.Type = LineAnnotationType.Vertical;
                annotation.Text = $"Job Time Limit: {medianPoint:F2} (= ( Workers - 1) * AT)";
                annotation.TextOrientation = AnnotationTextOrientation.Horizontal;
                chart.Annotations.Add(annotation);
                return chart;
            }
        }
        public PlotModel WorkersChart
        {
            get
            {
                double fontSize = 16;
                var chart = new PlotModel { Title = $"TotalTime = f (Workers), with {Jobs} jobs, JT={JobTime:F2} secs and AT={AssignmentTime:F2} ms" };
                double medianPoint = Model.WorkersLimit(Model.AssignmentTime,JobTime);
                double x0 = (int)(medianPoint > 0 ? medianPoint / 2 : 1);
                double x1 = (int)(medianPoint > 0 ? 2 * medianPoint : Jobs);
                double dx = x1 - x0 > 100 ? (int)((x1 - x0) / 100) : 1;
                var idealSeries = new FunctionSeries(x => Jobs*JobTime/x, x0, x1, dx, "Ideal Grid");
                idealSeries.FontSize = fontSize;
                var realSeries = new FunctionSeries(x => Jobs * WRONJModel.ModelWorkerTime(Model.AssignmentTime, JobTime, (int)x) / x,
                    x0, x1, dx, "WRONJ Grid");
                realSeries.FontSize = fontSize;
                realSeries.FontWeight = FontWeights.Bold;
                chart.Series.Add(idealSeries);
                chart.Series.Add(realSeries);
                var annotation = new LineAnnotation();
                annotation.Color = OxyColors.Red;
                annotation.MinimumY = 0;
                annotation.MaximumY = Jobs * WRONJModel.ModelWorkerTime(Model.AssignmentTime, JobTime, (int)x0) / x0;
                annotation.FontSize = fontSize;
                annotation.FontWeight = FontWeights.Bold;
                annotation.X = medianPoint;
                annotation.LineStyle = LineStyle.Solid;
                annotation.Type = LineAnnotationType.Vertical;
                annotation.Text = $"Workers Limit: {medianPoint:F0} (= JT / AT + 1)";
                annotation.TextOrientation = AnnotationTextOrientation.Horizontal;
                chart.Annotations.Add(annotation);
                return chart;
            }
        }
    }
}
