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
            viewModel = new WRONJViewModel (new WRONJModel()){ Title = "Model" };
            viewModel.Model.Load();
            viewModel.ChangeOutputData();
            BindingContext = ((App)Application.Current).ViewModel = viewModel;
        }
        async void Simulate_Clicked(object sender, EventArgs e)
        {
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
    }
}