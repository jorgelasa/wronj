using WRONJ.ViewModels;

namespace WRONJ.Views;

public partial class ChartsPage : ContentPage
{
    WRONJViewModel viewModel;
    //ScottPlot.Maui.MauiPlot timesChart = new ScottPlot.Maui.MauiPlot();
    public ChartsPage()
    {
        viewModel = (WRONJViewModel)((App)Application.Current).ViewModel;
        BindingContext = this.viewModel = viewModel;
        InitializeComponent();
        viewModel.WorkerTimeChart = workerTimeChart;
        viewModel.TimesChart = timesChart;
        viewModel.WorkersChart = workersChart;
        viewModel.FillPlots();
        //viewModel.TimesChart(TimesChart);
    }
}
