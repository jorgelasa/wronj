using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using SQRT.Models;
using SQRT.Views;
using SQRT.ViewModels;

namespace SQRT.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemsPage : ContentPage
    {
        SQRTViewModel viewModel;

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new SQRTViewModel(new SQRTModel());
        }


        async void Simulate_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SimulationPage(viewModel));
        }
        async void Calculate_Clicked(object sender, EventArgs e)
        {
            //await Navigation.PushModalAsync(new NavigationPage(new SimulationPage()));
            await viewModel.CalculateDataUntilConvergence();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}