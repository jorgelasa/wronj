using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WRONJ.Views;
using WRONJ.ViewModels;
using WRONJ.Models;

namespace WRONJ
{
    public partial class App : Application
    {

        public WRONJViewModel ViewModel { get; set;}
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
            ViewModel?.Model.Save();
        }

        protected override void OnResume()
        {
        }
    }
}
