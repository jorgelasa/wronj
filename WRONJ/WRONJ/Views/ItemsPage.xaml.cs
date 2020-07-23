using System;
using System.ComponentModel;
using Xamarin.Forms;
using WRONJ.Models;
using WRONJ.ViewModels;
using System.Threading;

namespace WRONJ.Views
{
    [DesignTimeVisible(false)]
    public partial class ItemsPage : ContentPage
    {
        WRONJViewModel viewModel;
        CancellationTokenSource cancelTokenSource;
        public ItemsPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new WRONJViewModel(new WRONJModel()) { Title = "Model"};            
        }


        async void Simulate_Clicked(object sender, EventArgs e)
        {
            viewModel.Model.EndSimulation += (idealTotalTime, realTotalTime) =>
            {
                viewModel.IdealTotalTime = idealTotalTime;
                viewModel.RealTotalTime = realTotalTime;
                viewModel.ShowOutputData = true;
            };
            viewModel.ModelWorkerTime = 0;
            viewModel.RealWorkerTime = 0;
            viewModel.IdealTotalTime = 0;
            viewModel.RealTotalTime = 0;
            await Navigation.PushAsync(new SimulationPage(viewModel));
        }
        async void Calculate_Clicked(object sender, EventArgs e)
        {
            cancelTokenSource = new CancellationTokenSource();
            await viewModel.Calculate(cancelTokenSource.Token);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
        protected override void OnDisappearing()
        {
            if (cancelTokenSource != null) cancelTokenSource.Cancel();
            base.OnDisappearing();
        }
    }
}