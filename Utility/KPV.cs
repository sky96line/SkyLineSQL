using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkyLineSQL.Utility
{
    public class KPV : INotifyPropertyChanged
    {
        public int Key { get; set; }
        public string Value { get; set; }

        public KPV(int k, string v)
        {
            this.Key = k;
            this.Value = v;
        }

        public override string ToString()
        {
            return $"{Key}. {Value}";
        }

        #region Notify

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
