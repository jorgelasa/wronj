using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using WRONG.Models;
using WRONG.ViewModels;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;

namespace WRONG.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class SimulationPage : ContentPage
    {
        WRONGViewModel viewModel;
        const int FWQMaxColumns = 64;
        CancellationTokenSource cancelTokenSource;
        public SimulationPage(WRONGViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = this.viewModel = viewModel;
            viewModel.LastJob = 1;
            moveJobQueue();
            if (viewModel.Workers <= 0)
                return;
            for (int worker = 0; worker < viewModel.Workers;  worker++)
            {
                fsq.Children.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker) }, worker, 0);
            }
            /*
            for (int row=0, worker=0; row <= viewModel.Workers/ FWQMaxColumns; row++)
            {
                for (int col=0; col < FWQMaxColumns && worker < viewModel.Workers; col++, worker++)
                {
                    fsq.Children.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker) }, col, row);
                }
            }
            */
            int workersColumns = viewModel.Workers <= 10? viewModel.Workers : (int)Math.Sqrt(viewModel.Workers);
            for (int row = 0, worker=0; row <= viewModel.Workers / workersColumns; row++)
            {
                for (int col = 0; col < workersColumns && worker < viewModel.Workers; col++, worker++)
                {
                    //activeWorkers.Children.Add(new BoxView { BackgroundColor = this.BackgroundColor }, col, row);
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
            for (int i=0; i < jobs; i++)
            {
                viewModel.Jobs[i].JobNumber = viewModel.LastJob + i + 1;
            }
        }
        private async void AssignmentStart(List<int> workers, double jobTime, double assignmentTime)
        {
            int s = 0;
            viewModel.LastJob++;
            moveJobQueue();
            foreach (View view in fsq.Children)
            {
                if (s < workers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(workers[s++]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
        }
        private void AssignmentEnd(List<int> workers, int worker, double jobTime)
        {
            int s = 0;
            foreach (View view in fsq.Children)
            {
                if (s < workers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(workers[s++]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
            activeWorkers.Children[worker].BackgroundColor = viewModel.WorkerColor(worker);
        }
        private void FreeWorker(List<int> workers)
        {
            int s = 0;
            foreach (View view in fsq.Children)
            {
                if (s < workers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(workers[s++]);
                }
                else
                {
                    view.BackgroundColor = this.BackgroundColor;
                }
            }
            //activeWorkers.Children[workers[s - 1]].BackgroundColor = this.BackgroundColor;
            activeWorkers.Children[workers[s - 1]].BackgroundColor = Color.Silver;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.Model.AssignmentStart -= AssignmentStart;
            viewModel.Model.AssignmentEnd -= AssignmentEnd;
            viewModel.Model.FreeWorker -= FreeWorker;
            cancelTokenSource.Cancel();
        }
    }
}