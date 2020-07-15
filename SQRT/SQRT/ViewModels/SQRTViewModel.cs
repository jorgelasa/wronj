using System;
using System.Transactions;
using WRONG.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Dynamic;

namespace WRONG.ViewModels
{
    public class WRONGViewModel : BaseViewModel
    {
        public Models.WRONGModel Model{ get;}
        public WRONGViewModel(Models.WRONGModel model)
        {
            Title = "WRONG Model";
            Model = model??new Models.WRONGModel();
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
        public int Workers
        {
            get { return Model.Workers; }
            set
            {
                int v = Model.Workers;
                if (SetProperty(ref v, value))
                {
                    Model.Workers = v;
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
        public Color WorkerColor(int worker, bool active, bool fsq) {
            //return Color.FromHsla(worker*0.7 / Model.Workers, active? 0.85: 0.15,0.5);
            return Color.FromHsla(worker * 0.7 / Model.Workers, 0.8, 0.5);
        }
    }
}
