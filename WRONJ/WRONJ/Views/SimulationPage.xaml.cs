﻿using System;
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
        readonly WRONJViewModel viewModel;
        readonly CancellationTokenSource cancelTokenSource;
        const string idleWorker = "\uf1d8";
        public SimulationPage(WRONJViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = this.viewModel = viewModel;
            viewModel.ShowExtraInfo = Width > Height;
            viewModel.NextJob = 0;
            viewModel.NextJobTime = 0;
            viewModel.NextAssignmentTime = 0;
            if (viewModel.Workers <= 0)
                return;
            MoveJobQueue();
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
            viewModel.Model.EndSimulation += (idealTime, realTime) =>
            {
                viewModel.IdealTotalTimeVol = idealTime;
                viewModel.ModelTotalTimeVol = realTime;
            };
            cancelTokenSource = new CancellationTokenSource();
            viewModel.Model.Simulate(cancelTokenSource.Token);
        }
        private void MoveJobQueue()
        {
            int jobs = jobQueue.Children.Count;
            for (int i=0; viewModel.JobsInfo!= null && i < jobs; i++)
            {
                viewModel.JobsInfo[i].JobNumber = viewModel.NextJob + i + 1;
            }
        }
        private void AssignmentStart(List<int> idleWorkers, double jobTime, double assignmentTime)
        {
            int s = 0;
            viewModel.NextJob++;
            viewModel.NextJobTime = jobTime;
            viewModel.NextAssignmentTime = assignmentTime*1000;
            viewModel.FreeWorkers = idleWorkers.Count;
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
            MoveJobQueue();
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
        private void FreeWorker(List<int> idleWorkers,double timeBetweenEndings)
        {
            int s = 0;
            viewModel.FreeWorkers = idleWorkers.Count;
            viewModel.TimeBetweenEndings = 1000*timeBetweenEndings;
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

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            viewModel.ShowExtraInfo = Width > Height;
        }
    }
}