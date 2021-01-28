using System.ComponentModel;

namespace MoeLoaderDelta.WindowData
{
    internal class MainLoginSite : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string loc_loginuser;

        public string Loginuser
        {
            get { return loc_loginuser; }
            set
            {
                loc_loginuser = $"{value}(_L)";
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Loginuser"));
            }
        }
    }
}
