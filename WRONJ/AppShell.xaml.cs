using WRONJ.Views;

namespace WRONJ
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Register routes for navigation
            Routing.RegisterRoute(nameof(SimulationPage), typeof(SimulationPage));
        }

        async void OnHelpClicked(object sender, EventArgs e)
        {
            var menu = sender as MenuItem;
            var url = menu?.CommandParameter as string ?? "https://github.com/jorgelasa/wronj/blob/master/README.md#the-wronj-problem";

            try
            {
                await Launcher.Default.OpenAsync(new Uri(url));
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo abrir la página web.", "OK");
            }
        }
    }
}
