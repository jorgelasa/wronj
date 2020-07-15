using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using WRONG.Models;
using WRONG.ViewModels;
using System.Threading;
using System.Collections.Generic;

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
            if (viewModel.Workers <= 0)
                return;
            for (int row=0, worker=0; row <= viewModel.Workers/ FWQMaxColumns; row++)
            {
                for (int col=0; col < FWQMaxColumns && worker < viewModel.Workers; col++, worker++)
                {
                    fsq.Children.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker, false, true) }, col, row);
                }
            }
            int workersColumns = (int)Math.Sqrt(viewModel.Workers);
            for (int row = 0, worker=0; row <= viewModel.Workers / workersColumns; row++)
            {
                for (int col = 0; col < workersColumns && worker < viewModel.Workers; col++, worker++)
                {
                    activeWorkers.Children.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker, false,false) }, col, row);
                }
            }
            viewModel.Model.AssignmentStart += AssignmentStart;
            viewModel.Model.AssignmentEnd += AssignmentEnd;
            viewModel.Model.FreeWorker += FreeWorker;
            cancelTokenSource = new CancellationTokenSource();
            viewModel.Model.Simulate(cancelTokenSource.Token);
        }

        public SimulationPage()
        {
            InitializeComponent();

            var model = new Models.WRONGModel
            {
                AssignmentTime = 0.001,
                Workers = 10
            };

            viewModel = new WRONGViewModel(model);
            BindingContext = viewModel;
        }

        public delegate void FWQ(List<int> workers);
        public delegate void FWQAndTime(List<int> workers, double time);
        public delegate void FWQSAndWorker(List<int> workers, int worker);

        private void AssignmentStart(List<int> workers, double time)
        {
            int s = 0;
            foreach (View view in fsq.Children)
            {
                if (s < workers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(workers[s++], s == 0, true);
                }
                else
                {
                    view.BackgroundColor = Color.DarkGray;
                }
            }
        }
        private void AssignmentEnd(List<int> workers, int worker)
        {
            int s = 0;
            foreach (View view in fsq.Children)
            {
                if (s < workers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(workers[s++], false, true);
                }
                else
                {
                    view.BackgroundColor = Color.DarkGray;
                }
            }
            activeWorkers.Children[worker].BackgroundColor = viewModel.WorkerColor(worker, true, false);
        }
        private void FreeWorker(List<int> workers)
        {
            int s = 0;
            foreach (View view in fsq.Children)
            {
                if (s < workers.Count)
                {
                    view.BackgroundColor = viewModel.WorkerColor(workers[s++], false, true);
                }
                else
                {
                    view.BackgroundColor = Color.DarkGray;
                }
            }
            activeWorkers.Children[workers[s - 1]].BackgroundColor = viewModel.WorkerColor(workers[s - 1], false, false);
        }
        async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
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