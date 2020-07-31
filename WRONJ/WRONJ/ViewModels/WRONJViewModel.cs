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
using MathNet.Numerics.Integration;
using OxyPlot.Axes;

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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JobColor"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Glyph"));
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
            JobTimeLimit = WRONJModel.JobTimeLimit(Model.AssignmentTime, Model.Workers);
            WorkersLimit = (int)Math.Round(WRONJModel.WorkersLimit(Model.AssignmentTime, Model.JobTime));
            IdealTotalTime = WRONJModel.TotalTime(Model.JobTime,Model.Workers,Model.Jobs);
            ModelTotalTime = WRONJModel.TotalTime(Model.JobTime, Model.Workers, Model.Jobs,Model.AssignmentTime);
            ModelWorkerTime = WRONJModel.WorkerTime(Model.AssignmentTime, Model.JobTime, Model.Workers);
            IdealTotalTimeVol = 0;
            ModelTotalTimeVol = 0;
            ModelWorkerTimeVol = 0;
            VariableTimes = AssignmentTimeVolatility > 0 || JobTimeVolatility > 0;
        }
        private int lastJob = 1;
        public int LastJob
        {
            get { return lastJob; }
            set {
                SetProperty(ref lastJob, value);
            }
        }
        double modelWorkerTime, modelWorkerTimeVol;
        double idealTotalTime, idealTotalTimeVol;
        double modelTotalTime, modelTotalTimeVol;
        double jobTimeLimit;
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
        public int WorkersLimit
        {
            get { return workersLimit; }
            set
            {
                SetProperty(ref workersLimit, value);
            }
        }
        bool variableTimes;
        public bool VariableTimes
        {
            get { return variableTimes; }
            set
            {
                SetProperty(ref variableTimes, value);
            }
        }

        public async Task Calculate(CancellationToken cancelToken)
        {
            var data =await Model.CalculateAsync(cancelToken);
            //ModelWorkerTime = data.modelTime;
            ModelWorkerTimeVol = data.workerTime;
            IdealTotalTimeVol = data.idealTotalTime;
            ModelTotalTimeVol = data.realTotalTime;
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
        private LineAnnotation verticalAnnotation(string text, double x, double y, OxyColor color, 
            double margin = 10,
            HorizontalAlignment alignment= HorizontalAlignment.Left)
        {
            var annotation = new LineAnnotation();
            annotation.X = x;
            annotation.MinimumY = 0;
            annotation.MaximumY = y;
            annotation.FontSize = 12;
            annotation.Color = color;
            annotation.Text = text;
            annotation.FontWeight = FontWeights.Bold;
            annotation.LineStyle = LineStyle.Solid;
            annotation.Type = LineAnnotationType.Vertical;
            annotation.TextOrientation = AnnotationTextOrientation.Horizontal;
            annotation.TextPadding = 5;
            annotation.TextMargin = margin;
            annotation.TextHorizontalAlignment = alignment;
            return annotation;
        }
        private LineAnnotation horizontalAnnotation(string text, double x0, double x1, double y, OxyColor color,
            LineStyle style = LineStyle.Solid)
        {
            var annotation = new LineAnnotation();
            annotation.Y = y;
            annotation.MinimumX = x0;
            annotation.MaximumX = x1;
            annotation.FontSize = 14;
            annotation.Color = color;
            annotation.StrokeThickness = 3;
            annotation.Text = text;
            annotation.FontWeight = FontWeights.Bold;
            annotation.LineStyle = style;
            annotation.Type = LineAnnotationType.Horizontal;
            annotation.TextOrientation = AnnotationTextOrientation.Horizontal;
            annotation.TextPadding = 5;
            //annotation.TextMargin = margin;
            annotation.TextVerticalAlignment = VerticalAlignment.Bottom;
            //annotation.TextHorizontalAlignment = HorizontalAlignment.Left; 
            return annotation;
        }
        public PlotModel TimesChart
        {
            get {
                const double fontSize = 14;
                var chart = new PlotModel { Title = $"WT = f (JT), with {Workers} workers and AT={AssignmentTime:F2} ms" };
                double limit = WRONJModel.JobTimeLimit(Model.AssignmentTime, Model.Workers);
                var idealSeries = new FunctionSeries( x => x,0,2*limit, limit/100,"Ideal Grid");
                idealSeries.FontSize = fontSize;
                var realSeries = new FunctionSeries(x => WRONJModel.WorkerTime(Model.AssignmentTime,x,Workers), 
                                        0, 2 * limit, limit / 100,"WRONJ Grid");
                realSeries.FontSize = fontSize;
                realSeries.FontWeight = FontWeights.Bold;
                chart.Series.Add(idealSeries);
                chart.Series.Add(realSeries);
                double maxY = WRONJModel.WorkerTime(Model.AssignmentTime, 2 * limit, Workers);
                chart.Annotations.Add(verticalAnnotation($"Limit: {limit:F2}",
                    limit, maxY, OxyColors.Blue, 100,
                    limit <= JobTime ? HorizontalAlignment.Right : HorizontalAlignment.Left));
                chart.Annotations.Add(verticalAnnotation($"Job Time: {JobTime:F2}",
                    JobTime, maxY, limit <= JobTime ? OxyColors.Green : OxyColors.Red, 100,
                    limit <= JobTime ? HorizontalAlignment.Left : HorizontalAlignment.Right));
                return chart;
            }
        }
        public PlotModel WorkersChart
        {
            get
            {
                double fontSize = 12;
                var chart = new PlotModel { Title = $"TotalTime = f (Workers), with {Jobs} jobs, JT={JobTime:F2} secs and AT={AssignmentTime:F2} ms" };
                double limit = WRONJModel.WorkersLimit(Model.AssignmentTime,JobTime);
                double x0 = (int)(limit > 0 ? limit / 2 : 1);
                double x1 = (int)(limit > 0 ? 2 * limit : Jobs);
                double dx = x1 - x0 > 100 ? (int)((x1 - x0) / 100) : 1;
                var idealSeries = new FunctionSeries(
                    x => WRONJModel.TotalTime(
                            Model.JobTime,
                            (int)Math.Round(x),
                            Model.Jobs)
                    , x0, x1, dx, "Ideal Grid");
                idealSeries.FontSize = fontSize;
                var realSeries = new FunctionSeries(
                    x => WRONJModel.TotalTime(
                            WRONJModel.WorkerTime(Model.AssignmentTime, JobTime, (int)Math.Round(x)),
                            (int)Math.Round(x),
                            Model.Jobs)
                    ,x0, x1, dx, "WRONJ Grid");
                realSeries.FontSize = fontSize;
                realSeries.FontWeight = FontWeights.Bold;
                chart.Series.Add(idealSeries);
                chart.Series.Add(realSeries);
                double maxY= WRONJModel.TotalTime(
                            WRONJModel.WorkerTime(Model.AssignmentTime, JobTime, (int)Math.Round(x0)),
                            (int)Math.Round(x0),
                            Model.Jobs);
                chart.Annotations.Add(verticalAnnotation($"Limit: {limit:F0}",
                    limit, maxY, OxyColors.Blue,100,
                    limit <= Workers ? HorizontalAlignment.Right : HorizontalAlignment.Left));
                chart.Annotations.Add(verticalAnnotation($"Workers: {Workers}",
                    Workers, maxY, limit >= Workers ? OxyColors.Green : OxyColors.Red, 100,
                    limit <= Workers ? HorizontalAlignment.Left : HorizontalAlignment.Right));
                return chart;
            }
        }
        public PlotModel WorkerTimeChart
        {
            get
            {
                double fontSize = 12;
                var chart = new PlotModel { Title = $"WRONJ Worker Time" };
                var yAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Workers",
                    Minimum = 0,
                    Maximum = Workers + 1.5
                };
                yAxis.MinorStep = 1;
                chart.Axes.Add(yAxis);
                double assignmentTime = Model.AssignmentTime;
                double maxX = JobTime > WRONJModel.JobTimeLimit(assignmentTime, Workers) ?
                            2 * (JobTime + assignmentTime) :
                            JobTime + assignmentTime + WRONJModel.WorkerTime(assignmentTime, JobTime, Workers);
                var xAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Time",
                    Minimum = 0,
                    Maximum = maxX
                };
                chart.Axes.Add(xAxis);
                double jobStart = 0;
                for (int w = 1; w <= Workers; w++)
                {
                    chart.Annotations.Add(horizontalAnnotation("",
                        0, jobStart+=assignmentTime, w, OxyColors.Red));
                    chart.Annotations.Add(horizontalAnnotation("",
                        jobStart, jobStart + JobTime, w, OxyColors.Green));
                }
                chart.Annotations.Add(horizontalAnnotation("Job Queue",
                    0, maxX, Workers + 1, OxyColors.Red, LineStyle.Dot));

                chart.Annotations.Add(new ArrowAnnotation
                {
                    StartPoint = new DataPoint(assignmentTime + JobTime, 1),
                    EndPoint = new DataPoint(assignmentTime + JobTime, Workers + 1)
                });
                return chart;
            }
        }
    }
}
