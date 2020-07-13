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
        const int FSQMaxColumns = 64;
        public SimulationPage(SQRTViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = this.viewModel = viewModel;
            if (viewModel.Slots <= 0)
                return;
            for (int row=0, slot=0; row <= viewModel.Slots/ FSQMaxColumns; row++)
            {
                for (int col=0; col < FSQMaxColumns && slot < viewModel.Slots; col++, slot++)
                {
                    fsq.Children.Add(new BoxView { BackgroundColor = viewModel.SlotColor(slot, false, true) }, col, row);
                }
            }
            int slotsColumns = (int)Math.Sqrt(viewModel.Slots);
            for (int row = 0, slot=0; row <= viewModel.Slots / slotsColumns; row++)
            {
                for (int col = 0; col < slotsColumns && slot < viewModel.Slots; col++, slot++)
                {
                    activeSlots.Children.Add(new BoxView { BackgroundColor = viewModel.SlotColor(slot, false,false) }, col, row);
                }
            }
            viewModel.Model.AssignmentStart += (slots, time) =>
            {
                int s = 0;
                foreach (View view in fsq.Children)
                {
                    if (s < slots.Count)
                    {
                        view.BackgroundColor = viewModel.SlotColor(slots[s++], s == 0, true);
                    }
                    else
                    {
                        view.BackgroundColor = Color.DarkGray;
                    }
                }
            };
            viewModel.Model.AssignmentEnd += (slots, slot) =>
            {
                int s = 0;
                foreach (View view in fsq.Children)
                {
                    if (s < slots.Count)
                    {
                        view.BackgroundColor = viewModel.SlotColor(slots[s++], false, true);
                    }
                    else
                    {
                        view.BackgroundColor = Color.DarkGray;
                    }
                }
                activeSlots.Children[slot].BackgroundColor = viewModel.SlotColor(slot, true, false);
            };
            viewModel.Model.FreeSlot += (slots) =>
            {
                int s = 0;
                foreach (View view in fsq.Children)
                {
                    if (s < slots.Count)
                    {
                        view.BackgroundColor = viewModel.SlotColor(slots[s++], false, true);
                    }
                    else
                    {
                        view.BackgroundColor = Color.DarkGray;
                    }
                }
                activeSlots.Children[slots[s - 1]].BackgroundColor = viewModel.SlotColor(slots[s - 1], false, false);
            };
            viewModel.Model.Simulate();
        }

        public SimulationPage()
        {
            InitializeComponent();

            var model = new Models.SQRTModel
            {
                AssignmentTime = 0.001,
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