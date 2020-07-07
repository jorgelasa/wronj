using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using SQRT.Models;
using SQRT.ViewModels;

namespace SQRT.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class SimulationPage : ContentPage
    {
        SQRTViewModel viewModel;

        public SimulationPage(SQRTViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;
        }

        public SimulationPage()
        {
            InitializeComponent();

            var model = new Models.SQRTModel
            {
                Q = 0.001,
                Slots = 10
            };

            viewModel = new SQRTViewModel(model);
            BindingContext = viewModel;
        }

        async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

    }
}