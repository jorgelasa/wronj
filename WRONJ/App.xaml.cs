using WRONJ.ViewModels;

namespace WRONJ
{
    public partial class App : Application
    {
        public WRONJViewModel ViewModel { get; set; }
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnSleep()
        {
            ViewModel?.Model.Save();
        }

    }
}