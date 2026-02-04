using WRONJ.ViewModels;

namespace WRONJ.Views;

public partial class SimulationPage : ContentPage
{
    readonly WRONJViewModel viewModel;
    readonly CancellationTokenSource cancelTokenSource;
    const string idleWorker = "\uf1d8";
    public SimulationPage()
    {
        InitializeComponent();
        viewModel = (WRONJViewModel)((App)Application.Current).ViewModel;
        BindingContext = viewModel;
        viewModel.ShowExtraInfo = Width > Height;
        viewModel.NextJob = 0;
        viewModel.NextJobTime = 0;
        viewModel.NextAssignmentTime = 0;
        if (viewModel.Workers <= 0)
            return;
        MoveJobQueue();
        viewModel.FreeWorkers = viewModel.Workers;
        for (int worker = 0; worker < viewModel.Workers; worker++)
        {
            fsq.Add(new BoxView { BackgroundColor = viewModel.WorkerColor(worker)}, worker, 0);
        }
        int workersColumns = viewModel.Workers <= 10 ? viewModel.Workers : (int)Math.Sqrt(viewModel.Workers);
        string fontFamily = ((FontImageSource)((Image)jobQueue.Children[0]).Source).FontFamily;
        for (int row = 0, worker = 0; row <= viewModel.Workers / workersColumns; row++)
        {
            for (int col = 0; col < workersColumns && worker < viewModel.Workers; col++, worker++)
            {
                activeWorkers.Add(new Image
                {
                    BackgroundColor = Colors.Silver,
                    Source = new FontImageSource { FontFamily = fontFamily, Glyph = idleWorker }
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
        for (int i = 0; viewModel.JobsInfo != null && i < jobs; i++)
        {
            viewModel.JobsInfo[i].JobNumber = viewModel.NextJob + i + 1;
        }
    }
    private void AssignmentStart(List<int> idleWorkers, double jobTime, double assignmentTime)
    {
        int s = 0;
        viewModel.NextJob++;
        viewModel.NextJobTime = jobTime;
        viewModel.NextAssignmentTime = assignmentTime * 1000;
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

        // Cast the child to Image before accessing properties
        var workerView = (Image)activeWorkers.Children[worker];
        workerView.BackgroundColor = viewModel.WorkerColor(worker);
        ((FontImageSource)workerView.Source).Glyph = glyph;
    }
    private void FreeWorker(List<int> idleWorkers, double timeBetweenEndings)
    {
        int s = 0;
        viewModel.FreeWorkers = idleWorkers.Count;
        viewModel.TimeBetweenEndings = 1000 * timeBetweenEndings;
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

        int lastIdx = idleWorkers[idleWorkers.Count - 1];
        var freedView = (Image)activeWorkers.Children[lastIdx];
        freedView.BackgroundColor = Colors.Silver;
        ((FontImageSource)freedView.Source).Glyph = idleWorker;
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
