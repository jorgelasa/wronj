using Microsoft.Maui.Controls;
using WRONJ.ViewModels;

namespace WRONJ.Views;

public partial class SimulationPage : ContentPage
{
    readonly WRONJViewModel viewModel;
    readonly CancellationTokenSource cancelTokenSource;
    const string kIdleWorkerGlyph = "\uf1d8";
    public SimulationPage()
    {
        InitializeComponent();
        viewModel = (WRONJViewModel)((App)Application.Current).ViewModel;
        BindingContext = viewModel;
        viewModel.ShowExtraInfo = Width > Height;
        viewModel.NextJob = 0;
        viewModel.SimulationMaxJobTime = 0;
        viewModel.SimulationWorkerTime = 0;
        viewModel.SimulationTotalTime = 0;
        if (viewModel.Model.TotalWorkers <= 0)
            return;
        MoveJobQueue();
        viewModel.FreeWorkers = viewModel.Model.TotalWorkers;
        for (int worker = 0; worker < viewModel.Model.TotalWorkers; worker++)
        {
            fwq.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker)}, worker, 0);
        }
        int workersColumns = viewModel.Model.TotalWorkers <= 10 ? viewModel.Model.TotalWorkers : (int)Math.Sqrt(viewModel.Model.TotalWorkers);
        string fontFamily = ((FontImageSource)((Image)jobQueue.Children[0]).Source).FontFamily;
        for (int row = 0, worker = 0; row <= viewModel.Model.TotalWorkers / workersColumns; row++)
        {
            for (int col = 0; col < workersColumns && worker < viewModel.Model.TotalWorkers; col++, worker++)
            {
                activeWorkers.Add(new Image
                {
                    BackgroundColor = Colors.Silver,
                    Source = new FontImageSource { FontFamily = fontFamily, Glyph = kIdleWorkerGlyph }
                }, col, row);
            }
        }
        viewModel.Model.AssignmentStart += AssignmentStart;
        viewModel.Model.AssignmentEnd += AssignmentEnd;
        viewModel.Model.AddFreeWorker += AddFreeWorker;
        cancelTokenSource = new CancellationTokenSource();
        Simulate();
    }
    async Task<double> Simulate()
    {
        double totalTime = await viewModel.Model.Simulate(cancelTokenSource.Token, ++viewModel.Seed);
        viewModel.SimulationTotalTime = totalTime;
        return totalTime;
    }
    private void MoveJobQueue()
    {
        int jobs = jobQueue.Children.Count;
        for (int i = 0; viewModel.JobsInfo != null && i < jobs; i++)
        {
            viewModel.JobsInfo[i].JobNumber = viewModel.NextJob + i + 1;
        }
    }
    private void RefreshFWQ(List<int> freeWorkers, List<int> workers, bool assigning)
    {
        int i = 0;
        foreach (View view in fwq.Children)
        {
            if (i < freeWorkers.Count)
            {
                view.BackgroundColor = viewModel.WorkerColor(freeWorkers[i], assigning && (workers?.Contains(freeWorkers[i]) ?? false));
            }
            else
            {
                view.BackgroundColor = this.BackgroundColor;
            }
            i++;
        }
        if (workers != null)
        {
            RefreshWorkers(workers, assigning);
        }
    }

    private void RefreshWorkers(List<int> workers, bool assigning)
    {
        for (int i = 0; i < workers.Count; i++)
        {
            string glyph = viewModel.JobsInfo[i].Glyph;
            // Cast the child to Image before accessing properties
            var workerView = (Image)activeWorkers.Children[workers[i]];
            workerView.BackgroundColor = viewModel.WorkerColor(workers[i], assigning);
            ((FontImageSource)workerView.Source).Glyph = glyph;
        }
    }

    /// Remove from the free workers view all the workers assigned
    private void AssignmentStart(List<int> freeWorkers, List<int> assignedWorkers, double maxJobTime, double assignmentTime)
    {
        viewModel.NextJob+= assignedWorkers.Count;
        if (maxJobTime > viewModel.SimulationMaxJobTime)
        {
            viewModel.SimulationMaxJobTime = maxJobTime;
        }
        viewModel.FreeWorkers = freeWorkers.Count;
        RefreshFWQ(freeWorkers, assignedWorkers, true);
    }
    /// Add to the free workers view a worker that has finished its jobs, and update the image of that worker in its view
    private void AddFreeWorker(List<int> freeWorkers)
    {
        viewModel.FreeWorkers = freeWorkers.Count;
        RefreshFWQ(freeWorkers, null, true);
        
        var freedView = (Image)activeWorkers.Children[freeWorkers.Last()];
        freedView.BackgroundColor = Colors.Silver;
        ((FontImageSource)freedView.Source).Glyph = kIdleWorkerGlyph;

    }
    
    /// Assign the jobs from the job queue view to the active workers view 
    private void AssignmentEnd(List<int> freeWorkers, List<int> assignedWorkers, double workerTime)
    {
        viewModel.SimulationWorkerTime = workerTime;
        RefreshFWQ(freeWorkers, assignedWorkers, false);
        MoveJobQueue();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.Model.AssignmentStart -= AssignmentStart;
        viewModel.Model.AssignmentEnd -= AssignmentEnd;
        viewModel.Model.AddFreeWorker -= AddFreeWorker;
        cancelTokenSource?.Cancel();
    }

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        viewModel.ShowExtraInfo = Width > Height;
    }
}
