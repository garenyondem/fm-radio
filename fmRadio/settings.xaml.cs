using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace fmRadio
{
    public partial class settings : PhoneApplicationPage
    {
        IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public settings()
        {
            InitializeComponent();
            regionSettingManager(false);
            languageSettingManager(false);

            organizeAppBarLanguage();
        }

        private void organizeAppBarLanguage()
        {
            if (appSettings.Contains("language"))
            {
                ApplicationBarIconButton appBarOkBtn = ApplicationBar.Buttons[0] as ApplicationBarIconButton;

                var languageKey = appSettings["language"].ToString();
                switch (languageKey)
                {
                    case "English":
                        appBarOkBtn.Text = "ok";
                        break;
                    case "Türkçe":
                        appBarOkBtn.Text = "tamam";
                        break;
                }
            }
        }

        private void setRadioRegion(object sender, EventArgs e)
        {
            regionSettingManager(true);
            languageSettingManager(true);
            NavigationService.GoBack();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            regionSettingManager(true);
            languageSettingManager(true);
        }

        private void regionSettingManager(bool command)
        {
            //true = save
            //false = read
            if (command)
            {
                if (!appSettings.Contains("region"))
                {
                    appSettings.Add("region", regionListPicker.SelectedIndex);
                }
                else
                {
                    appSettings["region"] = regionListPicker.SelectedIndex;
                }
                appSettings.Save();
            }
            else if (!command)
            {
                //0 World
                //1 North America
                //2 Japan
                if (appSettings.Contains("region"))
                {
                    int listIndex = Convert.ToInt32(appSettings["region"]);
                    switch (listIndex)
                    {
                        case 0:
                            regionListPicker.SelectedIndex = 0;
                            break;
                        case 1:
                            regionListPicker.SelectedIndex = 1;
                            break;
                        case 2:
                            regionListPicker.SelectedIndex = 2;
                            break;
                    }
                }
            }
        }

        private void languageSettingManager(bool command)
        {
            //true = save
            //false = read
            if (command)
            {
                if (!appSettings.Contains("language"))
                {
                    appSettings.Add("language", (languageListPicker.SelectedItem as ListPickerItem).Content);
                }
                else
                {
                    appSettings["language"] = (languageListPicker.SelectedItem as ListPickerItem).Content;
                }
                appSettings.Save();
            }
            else if (!command)
            {
                if (appSettings.Contains("language"))
                {
                    string languageKey = appSettings["language"].ToString();
                    switch (languageKey)
                    {
                        case "English":
                            languageListPicker.SelectedIndex = 0;
                            break;
                        case "Türkçe":
                            languageListPicker.SelectedIndex = 1;
                            break;
                    }
                }
            }
        }
    }
}