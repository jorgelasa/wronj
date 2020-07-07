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
        const int MaxColumns = 64;
        public SimulationPage(SQRTViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = this.viewModel = viewModel;
            int slot = 0;
            for (int row=0; row <= viewModel.Slots/MaxColumns; row++)
            {
                for (int col=0; col < MaxColumns && slot < viewModel.Slots; col++)
                {
                    slot++;
                    fsq.Children.Add(new BoxView { BackgroundColor = viewModel.SlotColor(slot, false) }, col, row);
                    slots.Children.Add(new BoxView { BackgroundColor = viewModel.SlotColor(slot, false) }, col, row);
                }
            }
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