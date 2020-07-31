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
        const string idleWorker = "\uf1d8";
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
                fsq.Children.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker) }, worker, 0);
            }
            int workersColumns = viewModel.Workers <= 10? viewModel.Workers : (int) Math.Sqrt(viewModel.Workers);
            string fontFamily = ((FontImageSource)((Image)jobQueue.Children[0]).Source).FontFamily;
            for (int row = 0, worker=0; row <= viewModel.Workers / workersColumns; row++)
            {
                for (int col = 0; col < workersColumns && worker < viewModel.Workers; col++, worker++)
                {
                    activeWorkers.Children.Add(new Image { BackgroundColor = Color.Silver,
                                Source = new FontImageSource { FontFamily=fontFamily, Glyph=idleWorker}
                    }, col, row);
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
                viewModel.JobsInfo[i].JobNumber = viewModel.LastJob + i + 1;
            }
        }
        private void AssignmentStart(List<int> idleWorkers, double jobTime, double assignmentTime)
        {
            int s = 0;
            viewModel.LastJob++;
            viewModel.LastJobTime = jobTime;
            viewModel.LastAssignmentTime = assignmentTime*1000;
            viewModel.FreeWorkers = idleWorkers.Count;
            int workers = fsq.Children.Count;
            foreach (View view in fsq.Children)
            {
                if (s < idleWorkers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(idleWorkers[s++]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
        }
        private void AssignmentEnd(List<int> idleWorkers, int worker, double workerTime)
        {
            int s = 0;
            viewModel.FreeWorkers = idleWorkers.Count;
            viewModel.ModelWorkerTimeVol = workerTime;
            string glyph = viewModel.JobsInfo[0].Glyph;
            moveJobQueue();
            int workers = fsq.Children.Count;
            foreach (View view in fsq.Children)
            {
                if (s < idleWorkers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(idleWorkers[s++]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
            activeWorkers.Children[worker].BackgroundColor = viewModel.WorkerColor(worker);
            ((FontImageSource)((Image)activeWorkers.Children[worker]).Source).Glyph = glyph;
        }
        private void FreeWorker(List<int> idleWorkers,double idealTime, double realTime)
        {
            int s = 0;
            viewModel.FreeWorkers = idleWorkers.Count;
            viewModel.IdealTotalTimeVol = idealTime;
            viewModel.ModelTotalTimeVol = realTime;
            int workers = fsq.Children.Count;
            foreach (View view in fsq.Children)
            {
                if (s < idleWorkers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(idleWorkers[s++]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
            activeWorkers.Children[idleWorkers[idleWorkers.Count - 1]].BackgroundColor = Color.Silver;
            ((FontImageSource)((Image)activeWorkers.Children[idleWorkers[idleWorkers.Count - 1]]).Source).Glyph = idleWorker;

        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.Model.AssignmentStart -= AssignmentStart;
            viewModel.Model.AssignmentEnd -= AssignmentEnd;
            viewModel.Model.FreeWorker -= FreeWorker;
            if (!viewModel.VariableTimes)
            {
                viewModel.IdealTotalTimeVol = 0;
                viewModel.ModelTotalTimeVol = 0;
                viewModel.ModelWorkerTimeVol = 0;
            }
            cancelTokenSource?.Cancel();
        }
    }
}