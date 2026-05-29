namespace WRONJ.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        async void OnOpenMauiClicked(object sender, EventArgs e)
        {
            var url = "https://dotnet.microsoft.com/en-us/apps/maui";
            try
            {
                await Launcher.Default.OpenAsync(new Uri(url));
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo abrir la página web.", "OK");
            }
        }
    }
}