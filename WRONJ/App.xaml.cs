using WRONJ.ViewModels;

namespace WRONJ
{
    public partial class App : Application
    {
        public WRONJViewModel ViewModel { get; set; }
        public App()
        {
            InitializeComponent();
            // Disable  automatic themes (dark/light)
            Application.Current.UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Destroying += (s, e) =>
            {
                ViewModel?.Model.Save();
            };
            return window;
        }

        protected override void OnSleep()
        {
            ViewModel?.Model.Save();
        }
    }
}