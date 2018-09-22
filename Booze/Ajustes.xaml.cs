using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;
using Booze.Resources;
using Microsoft.Phone.Controls;

namespace Booze
{
    public partial class Ajustes : PhoneApplicationPage
    {
        public Ajustes()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == true)
                {
                    toggleSwitch.IsChecked = true;
                }
                else
                {
                    toggleSwitch.IsChecked = false;
                }
            }

            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationTimeout"))
            {
                if ((TimeSpan)IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] == TimeSpan.FromSeconds(10))
                {
                    rb10.IsChecked = true;
                }
                else if ((TimeSpan)IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] == TimeSpan.FromSeconds(30))
                {
                    rb30.IsChecked = true;
                }
                else
                {
                    rbNone.IsChecked = true;
                }
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] = TimeSpan.FromSeconds(10);

                rb10.IsChecked = true;
            }
        }

        private void toggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
        }

        private void toggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
        }

        private void locationInfo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show(AppResources.Ajustes_Timeout_Message, 
                AppResources.Ajustes_Timeout_Title, MessageBoxButton.OK);
        }

        private void rb10_Checked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] = TimeSpan.FromSeconds(10);
        }

        private void rb30_Checked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] = TimeSpan.FromSeconds(30);
        }

        private void rbNone_Checked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationTimeout"] = TimeSpan.MaxValue;
        }
    }
}