using System;
using System.ComponentModel;
using Xamarin.Forms;
using WRONJ.ViewModels;
using System.Threading;
using System.Collections.Generic;
using MathNet.Numerics.Integration;

namespace WRONJ.Views
{
    [DesignTimeVisible(false)]
    public partial class SimulationPage : ContentPage
    {
        WRONJViewModel viewModel;
        const int FWQMaxColumns = 64;
        CancellationTokenSource cancelTokenSource;
        public SimulationPage(WRONJViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = this.viewModel = viewModel;
            viewModel.LastJob = 0;
            viewModel.LastJobTime = 0;
            viewModel.LastAssignmentTime = 0;
            if (viewModel.Workers <= 0)
                return;
            moveJobQueue();
            viewModel.FreeWorkers = viewModel.Workers;
            for (int worker = 0; worker < viewModel.Workers;  worker++)
            {
                fsq.Children.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(viewModel.Workers -1 - worker) }, worker, 0);
            }
            int workersColumns = viewModel.Workers <= 10? viewModel.Workers : (int) Math.Sqrt(viewModel.Workers);
            for (int row = 0, worker=0; row <= viewModel.Workers / workersColumns; row++)
            {
                for (int col = 0; col < workersColumns && worker < viewModel.Workers; col++, worker++)
                {
                    activeWorkers.Children.Add(new BoxView { BackgroundColor = Color.Silver }, col, row);
                }
            }
            viewModel.Model.AssignmentStart += AssignmentStart;
            viewModel.Model.AssignmentEnd += AssignmentEnd;
            viewModel.Model.FreeWorker += FreeWorker;
            cancelTokenSource = new CancellationTokenSource();
            viewModel.Model.Simulate(cancelTokenSource.Token);
        }
        private void moveJobQueue()
        {
            int jobs = jobQueue.Children.Count;
            for (int i=0; viewModel.JobsInfo!= null && i < jobs; i++)
            {
                viewModel.JobsInfo[i].JobNumber = viewModel.LastJob + jobs - i - 1;
            }
        }
        private void AssignmentStart(List<int> idleWorkers, double jobTime, double assignmentTime)
        {
            int s = 0;
            viewModel.LastJob++;
            viewModel.LastJobTime = jobTime;
            viewModel.LastAssignmentTime = assignmentTime*1000;
            viewModel.FreeWorkers = idleWorkers.Count;
            moveJobQueue();
            int workers = fsq.Children.Count;
            foreach (View view in fsq.Children)
            {
                if (++s > workers - idleWorkers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(idleWorkers[workers - s]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
        }
        private void AssignmentEnd(List<int> idleWorkers, int worker, double modelTime, double workerTime)
        {
            int s = 0;
            viewModel.FreeWorkers = idleWorkers.Count;
            viewModel.ModelWorkerTime = modelTime;
            viewModel.RealWorkerTime = workerTime;
            int workers = fsq.Children.Count;
            foreach (View view in fsq.Children)
            {
                if (++s > workers - idleWorkers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(idleWorkers[workers - s]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
            activeWorkers.Children[worker].BackgroundColor = viewModel.WorkerColor(worker);
        }
        private void FreeWorker(List<int> idleWorkers,double idealTime, double realTime)
        {
            int s = 0;
            viewModel.FreeWorkers = idleWorkers.Count;
            viewModel.IdealTotalTime = idealTime;
            viewModel.RealTotalTime = realTime;
            int workers = fsq.Children.Count;
            foreach (View view in fsq.Children)
            {
                if (++s > workers - idleWorkers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(idleWorkers[workers - s]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
            activeWorkers.Children[idleWorkers[s - 1]].BackgroundColor = Color.Silver;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.Model.AssignmentStart -= AssignmentStart;
            viewModel.Model.AssignmentEnd -= AssignmentEnd;
            viewModel.Model.FreeWorker -= FreeWorker;
            cancelTokenSource?.Cancel();
        }
    }
}