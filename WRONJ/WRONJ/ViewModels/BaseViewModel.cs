using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Xamarin.Forms;

using WRONJ.Models;
using System.Reflection;

namespace WRONJ.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        protected bool SetProperty<T>(object backingObject,
            T value,
            [CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            PropertyInfo backingProperty = backingObject.GetType().GetProperty(propertyName);
            if (EqualityComparer<T>.Default.Equals((T)backingProperty.GetValue(backingObject), value))
                return false;

            backingProperty.SetValue(backingObject,value);
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
