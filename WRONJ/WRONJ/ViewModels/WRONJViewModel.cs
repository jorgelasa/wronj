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
            EnableCharts = Workers > 0 && AssignmentTime > 0 && JobTime > 0;
        }
        private int nextJob = 1;
        public int NextJob
        {
            get { return nextJob; }
            set {
                SetProperty(ref nextJob, value);
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
        bool variableTimes, enableCharts;
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
        const double fontSize = 14;
        LineAnnotation verticalAnnotation(string text, double x, double y, OxyColor color, 
            double margin = 10,
            HorizontalAlignment alignment= HorizontalAlignment.Left)
        {
            var annotation = new LineAnnotation();
            annotation.Type = LineAnnotationType.Vertical;
            annotation.X = x;
            annotation.MinimumY = 0;
            annotation.MaximumY = y;
            annotation.Color = color;
            annotation.Text = text;
            annotation.LineStyle = LineStyle.Solid;
            annotation.TextOrientation = AnnotationTextOrientation.Horizontal;
            annotation.TextPadding = 5;
            annotation.TextMargin = margin;
            annotation.TextHorizontalAlignment = alignment;
            return annotation;
        }
        LineAnnotation horizontalLine(double x0, double x1, double y, OxyColor color,
            LineStyle style = LineStyle.Solid, double thickness=3)
        {
            var annotation = new LineAnnotation();
            annotation.Type = LineAnnotationType.Horizontal;
            annotation.Y = y;
            annotation.MinimumX = x0;
            annotation.MaximumX = x1;
            annotation.Color = color;
            annotation.StrokeThickness = thickness;
            annotation.LineStyle = style;
            return annotation;
        }
        public PlotModel TimesChart
        {
            get {
                var chart = new PlotModel { Title = "Worker Time" };
                double limit = WRONJModel.JobTimeLimit(Model.AssignmentTime, Model.Workers);
                var idealSeries = new FunctionSeries( x => x,0,2*limit, limit/100,"Ideal Grid");
                idealSeries.FontSize = fontSize;
                var realSeries = new FunctionSeries(x => WRONJModel.WorkerTime(Model.AssignmentTime,x,Workers), 
                                        0, 2 * limit, limit / 100,"WRONJ Grid");
                realSeries.FontSize = fontSize;
                realSeries.FontWeight = FontWeights.Bold;
                chart.Series.Add(idealSeries);
                chart.Series.Add(realSeries);
                chart.Axes.Add(
                    new LinearAxis{
                        Title = $"Job Time (Workers={Workers};AT={AssignmentTime:F2} ms)",
                        Position = AxisPosition.Bottom,
                        TitleFontSize = fontSize,
                        TitleFontWeight = FontWeights.Bold
                    }
                );
                chart.Axes.Add(
                    new LinearAxis {
                        Title = "Worker Time",
                        Position = AxisPosition.Left,
                        TitleFontSize = fontSize,
                        TitleFontWeight = FontWeights.Bold
                    }
                );
                double maxY = WRONJModel.WorkerTime(Model.AssignmentTime, 2 * limit, Workers);
                chart.Annotations.Add(
                    new TextAnnotation
                    {
                        Text = "WT = Workers * AT",
                        TextPosition = new DataPoint(limit/2, 0.93*WRONJModel.WorkerTime(Model.AssignmentTime, 0, Workers))
                    }
                );
                chart.Annotations.Add(
                    new TextAnnotation
                    {
                        Text = "WT = JT + AT",
                        TextPosition = new DataPoint( 1.25* limit , 0.93 * WRONJModel.WorkerTime(Model.AssignmentTime, 1.5 * limit, Workers))
                    }
                );
                chart.Annotations.Add(verticalAnnotation($"Limit=AT*(Workers -1)={limit:F2}",
                    limit, maxY, OxyColors.Blue, 100,
                    limit <= JobTime ? HorizontalAlignment.Right : HorizontalAlignment.Left));
                chart.Annotations.Add(verticalAnnotation($"Job Time={JobTime:F2}",
                    JobTime, maxY, limit <= JobTime ? OxyColors.Green : OxyColors.Red, 100,
                    limit <= JobTime ? HorizontalAlignment.Left : HorizontalAlignment.Right));
                return chart;
            }
        }
        public PlotModel WorkersChart
        {
            get
            {
                var tChart = TimesChart;
                var chart = new PlotModel { Title = "Total Time" };
                double limit = WRONJModel.WorkersLimit(Model.AssignmentTime,JobTime);
                double x0 = (int)(limit > 0 ? limit / 2 : 1);
                double x1 = (int)(limit > 0 ? Math.Max(2 * limit,Workers +1): Jobs);
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
                chart.Axes.Add(
                    new LinearAxis {
                        Title = $"Workers (Jobs={Jobs};JT={JobTime:F2};AT={AssignmentTime:F2} ms)",
                        Position = AxisPosition.Bottom,
                        TitleFontSize = fontSize,
                        TitleFontWeight = FontWeights.Bold
                    }
                );
                chart.Axes.Add(
                    new LinearAxis {
                        Title = "Total Time",
                        Position = AxisPosition.Left,
                        TitleFontSize = fontSize,
                        TitleFontWeight = FontWeights.Bold
                    }
                );
                double maxY = WRONJModel.TotalTime(
                            WRONJModel.WorkerTime(Model.AssignmentTime, JobTime, (int)Math.Round(x0)),
                            (int)Math.Round(x0),
                            Model.Jobs);
                chart.Annotations.Add(verticalAnnotation($"Limit=JT/AT + 1={limit:F0}",
                    limit, maxY, OxyColors.Blue,100,
                    limit <= Workers ? HorizontalAlignment.Right : HorizontalAlignment.Left));
                chart.Annotations.Add(verticalAnnotation($"Workers={Workers}",
                    Workers, maxY, limit >= Workers ? OxyColors.Green : OxyColors.Red, 100,
                    limit <= Workers ? HorizontalAlignment.Left : HorizontalAlignment.Right));
                return chart;
            }
        }
        public PlotModel WorkerTimeChart
        {
            get
            {
                var chart = new PlotModel { Title = $"WRONJ Worker Time" };
                const int waves = 3;
                int wStep = Workers <= 256 ? 1 : Workers / 256 + 1;
                var yAxis = new LinearAxis
                {
                    Title = "Workers (Green: processing job; Red: waiting in the queue)",
                    Position = AxisPosition.Left,
                    Minimum = 0,
                    Maximum = Workers + 2,
                    MinorStep = wStep,
                    TitleFontSize = fontSize,
                    TitleFontWeight = FontWeights.Bold
                };
                chart.Axes.Add(yAxis);
                double assignmentTime = Model.AssignmentTime;
                double limit = WRONJModel.JobTimeLimit(assignmentTime, Workers);
                double workerTime = WRONJModel.WorkerTime(assignmentTime, JobTime, Workers);
                double maxX = waves * workerTime + assignmentTime;
                var xAxis = new LinearAxis
                {
                    Title = $"Time (Workers={Workers};JT={JobTime:F2};AT={AssignmentTime:F2} ms)",
                    Position = AxisPosition.Bottom,
                    Minimum = 0,
                    Maximum = maxX,
                    TitleFontSize = fontSize,
                    TitleFontWeight = FontWeights.Bold
                };
                chart.Axes.Add(xAxis);
                //Jobs queue
                chart.Annotations.Add(horizontalLine(0, maxX, Workers + 1, OxyColors.Red, LineStyle.Dot,5));
                chart.Annotations.Add(
                    new TextAnnotation
                    {
                        Text = "Job Queue",
                        TextPosition = new DataPoint(maxX / 2, Workers + 1.5)
                    }
                );
                //Time diagram
                for (int wave = 0; wave < waves; wave++)
                {
                    for (int worker = 1; worker <= Workers; worker += wStep)
                    {
                        chart.Annotations.Add(horizontalLine(
                            wave == 0 ? 0 : workerTime*wave + assignmentTime * (worker-1),
                            workerTime * wave  + assignmentTime * worker, 
                            worker, OxyColors.Red));
                        chart.Annotations.Add(horizontalLine(
                            workerTime * wave + assignmentTime * worker,
                            workerTime * wave + assignmentTime * worker + JobTime,
                            worker, OxyColors.Green));
                        chart.Annotations.Add(horizontalLine(
                            workerTime * wave + assignmentTime * worker + JobTime,
                            workerTime * (wave + 1) + assignmentTime * (worker - 1),
                            worker, OxyColors.Red));
                    }

                    chart.Annotations.Add(new ArrowAnnotation
                    {
                        StartPoint = new DataPoint(workerTime * wave + assignmentTime + JobTime, 1),
                        EndPoint = new DataPoint(workerTime * wave + assignmentTime + JobTime, Workers + 1),
                        LineStyle = LineStyle.Dash,
                        Color = OxyColors.Goldenrod
                    }); ;
                    if (JobTime < limit)
                    {
                        chart.Annotations.Add(new ArrowAnnotation
                        {
                            StartPoint = new DataPoint(workerTime * wave + assignmentTime + JobTime, Workers + 1),
                            EndPoint = new DataPoint(workerTime * (wave + 1), Workers + 1),
                            Color = OxyColors.Goldenrod
                        });
                    }
                    chart.Annotations.Add(new ArrowAnnotation
                    {
                        StartPoint = new DataPoint(workerTime * (wave + 1), Workers + 1),
                        EndPoint = new DataPoint(workerTime * (wave + 1), 1),
                        LineStyle = LineStyle.Dash,
                        Color = OxyColors.Goldenrod
                    });
                    var separator = verticalAnnotation("", workerTime*(wave+1), 0.25, OxyColors.Goldenrod);
                    separator.StrokeThickness = 3;
                    chart.Annotations.Add(separator);
                }
                chart.Annotations.Add(horizontalLine(waves * workerTime, waves * workerTime + assignmentTime,
                    1, OxyColors.Red));
                chart.Annotations.Add(horizontalLine(0, maxX,
                    0.125, OxyColors.Goldenrod));
                chart.Annotations.Add(
                    new TextAnnotation
                    {
                        Text = "WT = " + (JobTime < limit ? "Workers * AT" : "JT + AT") + $" = {workerTime:F2}",
                        TextPosition = new DataPoint(maxX / 2, 0.5)
                    }
                );
                return chart;
            }
        }
    }
}
