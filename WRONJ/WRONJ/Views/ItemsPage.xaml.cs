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
        private readonly WRONJViewModel viewModel;
        CancellationTokenSource cancelTokenSource;
        public ItemsPage()
        {
            InitializeComponent();
            ChangeOrientation();
            viewModel = new WRONJViewModel (new WRONJModel()){ Title = "Model" };
            viewModel.Model.Load();
            viewModel.ChangeOutputData();
            BindingContext = ((App)Application.Current).ViewModel = viewModel;
        }
        async void Simulate_Clicked(object sender, EventArgs e)
        {
            viewModel.ModelWorkerTimeVol = 0;
            viewModel.IdealTotalTimeVol = 0;
            viewModel.ModelTotalTimeVol = 0;
            viewModel.TimeBetweenEndings = 0;
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

        async void Charts_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ChartsPage(viewModel));
        }
        private  void ChangeOrientation()
        {
            // Portrait mode.
            if (Width < Height)
            {
                itemsGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                itemsGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Absolute);
                Grid.SetRow(outputData, 1);
                Grid.SetColumn(outputData, 0);
            }
            // Landscape mode.
            else
            {
                itemsGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Absolute);
                itemsGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                Grid.SetRow(outputData, 0);
                Grid.SetColumn(outputData, 1);
            }
        }
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            ChangeOrientation();
        }
    }
}