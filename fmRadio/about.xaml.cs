using fmRadio.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Tasks;
using System.Reflection;
using System.Windows;
using System;
using Windows.ApplicationModel.Store;
using System.IO.IsolatedStorage;

namespace fmRadio
{
    public partial class hakkinda : PhoneApplicationPage
    {
        IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public hakkinda()
        {
            InitializeComponent();
            App.progressIndicatorSetter(true, false, AppResources.ApplicationTitle, this);
            OrganizeLayout();
        }

        private void OrganizeLayout()
        {
            if (appSettings.Contains("isPurchased"))
            {
                donationStoreLink_Btn.IsEnabled = Convert.ToBoolean(appSettings["isPurchased"]);
            }
            else
            {
                donationStoreLink_Btn.IsEnabled = true;
            }
        }

        private void moreAppsFromDeveloper(object sender, RoutedEventArgs e)
        {
            MarketplaceSearchTask marketplaceSearchTask = new MarketplaceSearchTask();
            marketplaceSearchTask.SearchTerms = "garen yöndem";
            marketplaceSearchTask.Show();
        }

        private void feedBackEmail(object sender, RoutedEventArgs e)
        {
            var assemblyinfo = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            var version = assemblyinfo.Version;

            var marka = DeviceStatus.DeviceManufacturer;
            var model = DeviceStatus.DeviceName;

            EmailComposeTask emailcomposer = new EmailComposeTask();
            emailcomposer.To = "mailto:gareny@gmail.com";
            emailcomposer.Subject = AppResources.FmRadioAppV + version + " " + marka + " " + model;
            emailcomposer.Show();
        }

        private void rateThisApp(object sender, RoutedEventArgs e)
        {
            new MarketplaceReviewTask().Show();
        }

        private async void donationStoreLink(object sender, RoutedEventArgs e)
        {
            try
            {
                await CurrentApp.RequestProductPurchaseAsync("fmRadioDonation", false);
                DoFulfillment();
            }
            catch (Exception)
            {
                if (appSettings.Contains("language"))
                {
                    var languageKey = appSettings["language"].ToString();
                    switch (languageKey)
                    {
                        case App.Languages.English:
                            MessageBox.Show("Purchase incomplete.", "Info", MessageBoxButton.OK);
                            break;
                        case App.Languages.Turkce:
                            MessageBox.Show("Satın alım tamamlanamadı.", "Bilgi", MessageBoxButton.OK);
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Purchase incomplete.", "Info", MessageBoxButton.OK);
                }
            }
        }

        private void DoFulfillment()
        {
            if (!appSettings.Contains("isPurchased"))
            {
                appSettings.Add("isPurchased", true);
            }
            appSettings.Save();
            OrganizeLayout();

            CurrentApp.ReportProductFulfillment("fmRadioDonation");
            if (appSettings.Contains("language"))
            {
                var languageKey = appSettings["language"].ToString();
                switch (languageKey)
                {
                    case App.Languages.English:
                        MessageBox.Show("Thank you for your support and donation, it is very much appreciated.", "Thanks", MessageBoxButton.OK);
                        break;
                    case App.Languages.Turkce:
                        MessageBox.Show("Destek ve yardımınız için çok teşekkürler.", "Teşekkürler", MessageBoxButton.OK);
                        break;
                }
            }
            else
            {
                MessageBox.Show("Thank you for your support and donation, it is very much appreciated.", "Thanks", MessageBoxButton.OK);
            }
        }
    }
}