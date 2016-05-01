using fmRadio.Resources;
using Microsoft.Devices.Radio;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;

namespace fmRadio
{
    public partial class frequencyInput : PhoneApplicationPage
    {
        MainPage mainPage = new MainPage();
        private bool isJackPlugged;
        FMRadio fmRadio = FMRadio.Instance;
        IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public frequencyInput()
        {
            InitializeComponent();
            App.progressIndicatorSetter(true, false, AppResources.ApplicationTitle, this);

            if (appSettings.Contains("language"))
            {
                var languageKey = appSettings["language"].ToString();
                switch (languageKey)
                {
                    case App.Languages.English:
                        switch (fmRadio.CurrentRegion)
                        {
                            case RadioRegion.Europe:
                                frequencyRangeTip.Text = AppResources.Between + " 87.5 " + AppResources.And + " 108.0";
                                break;
                            case RadioRegion.UnitedStates:
                                frequencyRangeTip.Text = AppResources.Between + " 87.9 " + AppResources.And + " 107.9";
                                break;
                            case RadioRegion.Japan:
                                frequencyRangeTip.Text = AppResources.Between + " 87.5 " + AppResources.And + " 90.0";
                                break;
                        }
                        break;
                    case App.Languages.Turkce:
                        switch (fmRadio.CurrentRegion)
                        {
                            case RadioRegion.Europe:
                                frequencyRangeTip.Text = "87.5 " + AppResources.And + " 108.0 " + AppResources.Between;
                                break;
                            case RadioRegion.UnitedStates:
                                frequencyRangeTip.Text = "87.9 " + AppResources.And + " 107.9 " + AppResources.Between;
                                break;
                            case RadioRegion.Japan:
                                frequencyRangeTip.Text = "87.5 " + AppResources.And + " 90.0 " + AppResources.Between;
                                break;
                        }
                        break;
                }
            }

            organizeAppBarLanguage();
        }

        private void organizeAppBarLanguage()
        {
            if (appSettings.Contains("language"))
            {
                ApplicationBarIconButton appBarOkBtn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;
                ApplicationBarIconButton appBarClearBtn = ApplicationBar.Buttons[1] as ApplicationBarIconButton;

                var languageKey = appSettings["language"].ToString();
                switch (languageKey)
                {
                    case App.Languages.English:
                        appBarOkBtn.Text = "Ok";
                        appBarClearBtn.Text = "Clear";
                        break;
                    case App.Languages.Turkce:
                        appBarOkBtn.Text = "Tamam";
                        appBarClearBtn.Text = "Temizle";
                        break;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            isJackPlugged = bool.Parse(NavigationContext.QueryString["isJackPlugged"]);
        }

        private void submitFrequency(object sender, EventArgs e)
        {
            string frequencyInput = submitedFrequency.Text;
            if (!string.IsNullOrEmpty(frequencyInput))
            {
                if (isJackPlugged)
                {
                    try
                    {
                        if (frequencyInput.Contains(","))
                        {
                            frequencyInput = frequencyInput.Replace(",", ".");
                        }
                        double frequency = Convert.ToDouble(frequencyInput);
                        switch (fmRadio.CurrentRegion)
                        {
                            case RadioRegion.Europe:
                                if (frequency >= 87.5 & frequency <= 108.0)
                                {
                                    mainPage.frequencySetter(frequencyInput);
                                    NavigationService.GoBack();
                                }
                                else
                                {
                                    MessageBox.Show(AppResources.FrequencyOutOfBounds, AppResources.UnreachableValue, MessageBoxButton.OK);
                                }
                                break;
                            case RadioRegion.UnitedStates:
                                if (frequency >= 87.9 & frequency <= 107.9)
                                {
                                    string[] parts = frequencyInput.Split('.');
                                    int secondPart = Convert.ToInt32(parts[1]);
                                    if (App.IsOdd(secondPart))
                                    {
                                        mainPage.frequencySetter(frequencyInput);
                                        NavigationService.GoBack();
                                    }
                                    else
                                    {
                                        MessageBox.Show(AppResources.FrequencyOutOfBounds, AppResources.CantTuneIntoThatFrequencyInYourRegion, MessageBoxButton.OK);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(AppResources.FrequencyOutOfBounds, AppResources.UnreachableValue, MessageBoxButton.OK);
                                }
                                break;
                            case RadioRegion.Japan:
                                if (frequency >= 87.5 & frequency <= 90.0)
                                {
                                    mainPage.frequencySetter(frequencyInput);
                                    NavigationService.GoBack();
                                }
                                else
                                {
                                    MessageBox.Show(AppResources.FrequencyOutOfBounds, AppResources.UnreachableValue, MessageBoxButton.OK);
                                }
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(AppResources.InvalidInput, AppResources.Error, MessageBoxButton.OK);
                    }
                }
                else
                {
                    MessageBox.Show(AppResources.NoHeadPhoneIsPluggedError, string.Empty, MessageBoxButton.OK);
                }
            }
            else
            {
                MessageBox.Show(AppResources.FrequencyCantBeEmpty, AppResources.MissingValue, MessageBoxButton.OK);
            }
        }

        private void sbmtFrequencyLoaded(object sender, RoutedEventArgs e)
        {
            submitedFrequency.Focus();
        }
    }
}