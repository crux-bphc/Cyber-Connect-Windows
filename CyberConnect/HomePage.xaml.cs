using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.ApplicationModel.Background;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CyberConnect
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public string username = ""; string logoutURL = "http://172.16.0.30:8090/logout.xml";

        public HomePage()
        {
            this.InitializeComponent();
            Initialize();
        }
        public void Initialize()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("CyberRoam_UserName"))
                username = localSettings.Values["CyberRoam_UserName"].ToString();
            txtUsername.Text = username;
            if (localSettings.Values.ContainsKey("CyberRoam_Auto"))
                checkBox.IsChecked = (bool)localSettings.Values["CyberRoam_Auto"];
        }
        public async void Logout()
        {

            //Create an HTTP client object
            HttpClient httpClient = new HttpClient();
            //Add a user-agent header to the GET request. 
            var headers = httpClient.DefaultRequestHeaders;
            //The safe way to add a header value is to use the TryParseAdd method and verify the return value is true,
            //especially if the header value is coming from user input.
            string header = "ie";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }
            header = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }
            List<KeyValuePair<string, string>> content = new List<KeyValuePair<string, string>>();
            content.Add(new KeyValuePair<string, string>("mode", "193"));
            content.Add(new KeyValuePair<string, string>("username", username));
            content.Add(new KeyValuePair<string, string>("a", (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + ""));
            content.Add(new KeyValuePair<string, string>("producttype", "0"));
            Uri requestUri = new Uri(logoutURL);
            //Send the GET request asynchronously and retrieve the response as a string.
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            try
            {
                //Send the GET request
                httpResponse = await httpClient.PostAsync(requestUri, new HttpFormUrlEncodedContent(content));
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponseBody.Contains("You have successfully logged off"))
                {
                    btnLogin.Content = "Login"; txtError.Text = "";this.Frame.GoBack();
                }
                else if (httpResponseBody.Contains("Server is not responding."))
                {
                    txtError.Text = "Server error occured. Please Retry!";
                }
                else txtError.Text = "You are not connected to Internet";
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                txtError.Text = httpResponseBody;
            }

        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["CyberRoam_UserName"] = "";
            localSettings.Values["CyberRoam_Password"] = "";
            Logout();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var TaskName = "CyberRoam";
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == TaskName)
                {
                    task.Value.Unregister(true);
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["CyberRoam_Auto"] = false;
                    break;
                }
            }
        }
        public async void CheckBackgroundTask()
        {
            var taskRegistered = false;
            var TaskName = "CyberRoam";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == TaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }
            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = TaskName;
                builder.TaskEntryPoint = "CyberConnectRuntime.Connection";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.NetworkStateChange, false));
                //builder.AddCondition(new SystemCondition(SystemConditionType.InternetNotAvailable));
                var access = await BackgroundExecutionManager.RequestAccessAsync();
                if (access == BackgroundAccessStatus.Denied)
                {
                    var dialog = new MessageDialog("Background Tasks are denied! Make sure you allow background tasks to run.");
                    await dialog.ShowAsync();
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["CyberRoam_Auto"] = false;
                }
                else
                {
                    BackgroundTaskRegistration task = builder.Register();
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["CyberRoam_Auto"] = true;
                }
            }
        }
        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBackgroundTask();
        }
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

    }
}
