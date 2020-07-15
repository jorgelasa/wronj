using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WRONG.Views;

namespace WRONG
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
