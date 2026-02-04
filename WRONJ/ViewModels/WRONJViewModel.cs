using System.ComponentModel;
using System.Threading;
using WRONJ.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using SD = System.Drawing;
using ScottPlot.Maui;

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

        // Helpers to add annotations/lines using ScottPlot
        void AddVerticalAnnotation(ScottPlot.Plot plot, string text, double x, double y, ScottPlot.Color color)
        {
            plot.Add.VerticalLine(x, color: color);//, lineWidth: 2);
            // place text slightly to the left of the line
            plot.Add.Text(text, x - 0.02 * Math.Max(1, x == 0 ? 1 : Math.Abs(x)), y * 0.9);//, color: color, fontSize: (int)fontSize);
        }

        void AddHorizontalLine(ScottPlot.Plot plot, double x0, double x1, double y, ScottPlot.Color color, double thickness = 3)
        {
            var line = plot.Add.HorizontalLine(y, color: color); // lineWidth: (float)thickness);
            // ensure the line spans the requested x-range by setting axis limits later
        }

        public ScottPlot.Maui.MauiPlot TimesChart { get; set; }
        public ScottPlot.Maui.MauiPlot WorkersChart { get; set; }
        public ScottPlot.Maui.MauiPlot WorkerTimeChart { get; set; }
        public void FillPlots()
        {
            if (TimesChart != null)
            {
                var plot = TimesChart.Plot;
                plot.Title("Worker Time");
                double limit = WRONJModel.JobTimeLimit(Model.AssignmentTime, Model.Workers);
                double xMax = 2 * limit;
                int points = 200;
                if (xMax <= 0) xMax = 1;
                double step = xMax / points;
                var xs = new double[points + 1];
                var ysIdeal = new double[points + 1];
                var ysReal = new double[points + 1];
                for (int i = 0; i <= points; i++)
                {
                    double x = i * step;
                    xs[i] = x;
                    ysIdeal[i] = x;
                    ysReal[i] = WRONJModel.WorkerTime(Model.AssignmentTime, x, Workers);
                }

                plot.Add.Scatter(xs, ysIdeal, color: ScottPlot.Color.FromColor(SD.Color.SteelBlue));//, lineWidth: 3);
                plot.Add.Scatter(xs, ysReal, color: ScottPlot.Color.FromColor(SD.Color.DarkOrange));//, lineWidth: 3);

                plot.XLabel($"Job Time (W={Workers};AT={AssignmentTime:F2} ms)");
                plot.YLabel("Worker Time");

                double maxY = WRONJModel.WorkerTime(Model.AssignmentTime, 2 * limit, Workers);
                plot.Add.Text("WT = W * AT", limit / 2, 0.93 * WRONJModel.WorkerTime(Model.AssignmentTime, 0, Workers));//, color: SD.Color.Black, fontSize: (int)fontSize);
                plot.Add.Text("WT = JT + AT", 1.25 * limit, 0.93 * WRONJModel.WorkerTime(Model.AssignmentTime, 1.5 * limit, Workers));//, color: SD.Color.Black, fontSize: (int)fontSize);

                AddVerticalAnnotation(plot, "JT=(W-1)*AT", limit, maxY, ScottPlot.Color.FromColor(SD.Color.Blue));
                var jtColor = limit <= JobTime ? ScottPlot.Color.FromColor(SD.Color.Green) : ScottPlot.Color.FromColor(SD.Color.Red);
                AddVerticalAnnotation(plot, $"JT={JobTime:F2}", JobTime, maxY, jtColor);

                plot.Axes.SetLimits(0, xMax, 0, Math.Max(maxY, 1));
            }
        //}        
        //public ScottPlot.Plot WorkersChart
        //{
            //get
            if (WorkersChart != null)
            {
                //var basePlot = TimesChart; // reuse styles from times chart
                //var plot = new ScottPlot.Plot();
                var plot = WorkersChart.Plot;
                plot.Title("Worker Time");
                double limit = WRONJModel.WorkersLimit(Model.AssignmentTime, JobTime);
                double x0 = (int)(limit > 0 ? limit / 2 : 1);
                double x1 = (int)(limit > 0 ? Math.Max(2 * limit, Workers + 1) : Jobs);
                if (x1 <= x0) x1 = x0 + 1;
                int points = (int)Math.Min(200, Math.Max(10, x1 - x0));
                double dx = (x1 - x0) / points;
                var xs = new double[points + 1];
                var ysIdeal = new double[points + 1];
                var ysReal = new double[points + 1];
                for (int i = 0; i <= points; i++)
                {
                    double x = x0 + i * dx;
                    xs[i] = x;
                    ysIdeal[i] = WRONJModel.TotalTime(Model.JobTime, (int)Math.Round(x), Model.Jobs);
                    ysReal[i] = WRONJModel.TotalTime(
                        WRONJModel.WorkerTime(Model.AssignmentTime, JobTime, (int)Math.Round(x)),
                        (int)Math.Round(x),
                        Model.Jobs);
                }

                plot.Add.Scatter(xs, ysIdeal, color: ScottPlot.Color.FromColor(SD.Color.SteelBlue));//, lineWidth: 3);
                plot.Add.Scatter(xs, ysReal, color: ScottPlot.Color.FromColor(SD.Color.DarkOrange));// lineWidth: 3);

                plot.XLabel($"Workers (J={Jobs};JT={JobTime:F2};AT={AssignmentTime:F2} ms)");
                plot.YLabel("Total Time");

                double maxY = WRONJModel.TotalTime(
                            WRONJModel.WorkerTime(Model.AssignmentTime, JobTime, (int)Math.Round(x0)),
                            (int)Math.Round(x0),
                            Model.Jobs);
                AddVerticalAnnotation(plot, "W=JT/AT+1", limit, maxY, ScottPlot.Color.FromColor(SD.Color.Blue));
                var wColor = limit >= Workers ? ScottPlot.Color.FromColor(SD.Color.Green) : ScottPlot.Color.FromColor(SD.Color.Red);
                AddVerticalAnnotation(plot, $"W={Workers}", Workers, maxY, wColor);

                plot.Axes.SetLimits(x0, x1, 0, Math.Max(maxY, 1));
                //return plot;
            }
            //}
            //public ScottPlot.Plot WorkerTimeChart
            //{
            //get
            if (WorkerTimeChart != null)
            {
                //var plot = new ScottPlot.Plot();
                var plot = WorkerTimeChart.Plot;
                const int waves = 3;
                int wStep = Workers <= 256 ? 1 : Workers / 256 + 1;
                double assignmentTime = Model.AssignmentTime;
                double limit = WRONJModel.JobTimeLimit(assignmentTime, Workers);
                double workerTime = WRONJModel.WorkerTime(assignmentTime, JobTime, Workers);
                double maxX = waves * workerTime + assignmentTime;

                plot.YLabel("Workers (Green: processing job; Red: waiting in the queue)");
                plot.XLabel($"Time (W={Workers};JT={JobTime:F2};AT={AssignmentTime:F2} ms)");

                // Jobs queue line
                plot.Add.HorizontalLine(Workers + 1, color: ScottPlot.Color.FromColor(SD.Color.Red));//, lineWidth: 3);
                plot.Add.Text("FWQ", maxX / 2, Workers + 1.5);//, color: SD.Color.Black, fontSize: (int)fontSize);

                // Time diagram
                for (int wave = 0; wave < waves; wave++)
                {
                    for (int worker = 1; worker <= Workers; worker += wStep)
                    {
                        double startA = wave == 0 ? 0 : workerTime * wave + assignmentTime * (worker - 1);
                        double endA = workerTime * wave + assignmentTime * worker;
                        double startJob = endA;
                        double endJob = workerTime * wave + assignmentTime * worker + JobTime;
                        double endIdle = workerTime * (wave + 1) + assignmentTime * (worker - 1);

                        // waiting (red)
                        plot.Add.HorizontalLine(worker, color: ScottPlot.Color.FromColor(SD.Color.Red));//, lineWidth: 3);
                        plot.Add.Text("", startA + (endA - startA) / 2, worker);// color: ScottPlot.Color.FromColor(SD.Color.Red));//, fontSize: 1);

                        // processing (green): represent as short horizontal line by plotting a thin segment using scatter
                        var xsProc = new double[] { startJob, endJob };
                        var ysProc = new double[] { worker, worker };
                        plot.Add.Scatter(xsProc, ysProc, color: ScottPlot.Color.FromColor(SD.Color.Green));//, lineWidth: 5);

                        // back to waiting
                        // the sequence is illustrative; for clarity we add separators
                    }

                    // Add vertical separators and arrows (approximate)
                    double sepX = workerTime * (wave + 1);
                    plot.Add.VerticalLine(sepX, color: ScottPlot.Color.FromColor(SD.Color.Goldenrod));//, lineWidth: 3);
                }

                // final decorations
                plot.Add.HorizontalLine(0.125, color: ScottPlot.Color.FromColor(SD.Color.Goldenrod));//, lineWidth: 2);
                plot.Add.Text("WT = " + (JobTime < limit ? "W * AT" : "JT + AT") + $" = {workerTime:F2}", maxX / 2, 0.5);//, color: ScottPlot.Color.FromColor(SD.Color.Black));//, fontSize: (int)fontSize);

                plot.Axes.SetLimits(0, Math.Max(maxX, 1), 0, Workers + 2);
                //return plot;
            }
        }
        /*
        public ScottPlot.Plot[] Plots
        {
            get { return new ScottPlot.Plot[] { WorkerTimeChart, TimesChart, WorkersChart }; }
        }
        public ScottPlot.Maui.MauiPlot[] Plots
        {
            //get { return new ScottPlot.Plot[] { WorkerTimeChart, TimesChart, WorkersChart }; }
            get { return new ScottPlot.Maui.MauiPlot[] { TimesChart}; }
        }
        */
    }
}
