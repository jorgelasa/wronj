using System;
using System.Transactions;
using SQRT.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Dynamic;

namespace SQRT.ViewModels
{
    public class SQRTViewModel : BaseViewModel
    {
        public Models.SQRTModel Model{ get;}
        public SQRTViewModel(Models.SQRTModel model)
        {
            Title = "SQRT Model";
            Model = model??new Models.SQRTModel();
        }
        public double AssignmentTime
        {
            get { return Model.AssignmentTime; }
            set {
                double v=Model.AssignmentTime;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTime = v;
                    OnPropertyChanged("ModelTime");
                }
            }                
        }
        public double AssignmentTimeVolatility
        {
            get { return Model.AssignmentTimeVolatility; }
            set
            {
                double v = Model.AssignmentTimeVolatility;
                if (SetProperty(ref v, value))
                {
                    Model.AssignmentTimeVolatility = v;
                    OnPropertyChanged("ModelTime");
                }
            }
        }
        public double TaskTime
        {
            get { return Model.TaskTime; }
            set
            {
                double v = Model.TaskTime;
                if (SetProperty(ref v, value))
                {
                    Model.TaskTime = v;
                    OnPropertyChanged("ModelTime");
                }
            }
        }
        public double TaskTimeVolatility
        {
            get { return Model.TaskTimeVolatility; }
            set
            {
                double v = Model.TaskTimeVolatility;
                if (SetProperty(ref v, value))
                {
                    Model.TaskTimeVolatility = v;
                    OnPropertyChanged("ModelTime");
                }
            }
        }
        public int Slots
        {
            get { return Model.Slots; }
            set
            {
                int v = Model.Slots;
                if (SetProperty(ref v, value))
                {
                    Model.Slots = v;
                    OnPropertyChanged("ModelTime");
                }
            }
        }
        public double ModelTime
        {
            get { return Model.ModelTime; }
        }
        public double RealTaskTime
        {
            get { return Model.RealTaskTime; }
        }
        public int TaskNumber
        {
            get { return Model.TaskNumber; }
            set
            {
                int v = Model.TaskNumber;
                if (SetProperty(ref v, value))
                {
                    Model.TaskNumber = v;
                }
            }
        }
        public async Task CalculateDataUntilConvergence()
        {
            await Model.CalculateUntilConvergence();
            OnPropertyChanged("RealTaskTime");
            OnPropertyChanged("TaskNumber");
        }
        public Color SlotColor(int slot, bool active, bool fsq) {
            return Color.FromHsla(slot*0.7 / Model.Slots, active? 0.85: 0.15,0.5);
        }
    }
}
