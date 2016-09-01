using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using System.Net.NetworkInformation;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CyberConnect
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string loginURL = "http://172.16.0.30:8090/login.xml";
        string logoutURL = "http://172.16.0.30:8090/logout.xml";
        string username = "", password = "";
        Boolean login = false;
        public MainPage()
        {
            this.InitializeComponent();
            AttemptLogin();
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
            if(!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = TaskName;
                builder.TaskEntryPoint = "CyberConnectRuntime.Connection";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.NetworkStateChange, false));
                //builder.AddCondition(new SystemCondition(SystemConditionType.InternetNotAvailable));
                var access =await BackgroundExecutionManager.RequestAccessAsync();
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
        public void AttemptLogin()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            try
            {
                if (localSettings.Values.ContainsKey("CyberRoam_UserName"))
                    username = localSettings.Values["CyberRoam_UserName"].ToString();
                if (localSettings.Values.ContainsKey("CyberRoam_Password"))
                    password = localSettings.Values["CyberRoam_Password"].ToString();
                txtUserName.Text = username;
                txtPassword.Password = password;
                txtError.Text = "";
            }
            catch (Exception e) { }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (login)
            {
                Logout();
            }
            else
            {
                try
                {
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    if (txtUserName.Text.Equals("")) txtError.Text = "Username field is empty.";
                    if (txtPassword.Password.Equals("")) txtError.Text = "Password Field is empty.";
                    username = txtUserName.Text;
                    password = txtPassword.Password;
                    Login();
                }
                catch (Exception e1) { }
            }
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
                        btnLogin.Content = "Login"; txtError.Text = ""; login = false;
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

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void txtPassword_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (temp) { temp = false; return; }
                try
                {
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    if (txtUserName.Text.Equals("")) txtError.Text = "Username field is empty.";
                    if (txtPassword.Password.Equals("")) txtError.Text = "Password Field is empty.";
                    username = txtUserName.Text;
                    password = txtPassword.Password;
                    Login();
                }
                catch (Exception e1) { }
            }
        }
        bool temp = false;

        private void txtUserName_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                temp = true;
                txtPassword.Focus(FocusState.Keyboard);
            }
        }

        public async void Login()
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
                content.Add(new KeyValuePair<string, string>("mode", "191"));
                content.Add(new KeyValuePair<string, string>("username", username));
                content.Add(new KeyValuePair<string, string>("password", password));
                content.Add(new KeyValuePair<string, string>("a", (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + ""));
                content.Add(new KeyValuePair<string, string>("producttype", "0"));
                Uri requestUri = new Uri(loginURL);
                //Send the GET request asynchronously and retrieve the response as a string.
                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";
                try
                {
                    //Send the GET request
                    httpResponse = await httpClient.PostAsync(requestUri, new HttpFormUrlEncodedContent(content));
                    httpResponse.EnsureSuccessStatusCode();
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponseBody.Contains("You have successfully logged in"))
                    {
                        btnLogin.Content = "Logout"; txtError.Text = "";login = true;
                        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                        localSettings.Values["CyberRoam_UserName"] = username;
                        localSettings.Values["CyberRoam_Password"] = password;
                        CheckBackgroundTask();
                        this.Frame.Navigate(typeof(HomePage));
                    }
                    else if (httpResponseBody.Contains("Your data transfer has been exceeded"))
                    {
                        txtError.Text = "Data limit exceeded";
                    }
                    else if (httpResponseBody.Contains("Your credentials were incorrect"))
                    {
                        txtError.Text = "Please check your credentials!";
                    }
                    else if (httpResponseBody.Contains("Server is not responding."))
                    {
                        txtError.Text = "Server error occured. Please Retry!";
                    }
                    else if(httpResponseBody.Contains("Make sure your password is correct"))
                    {
                        txtError.Text = "Make sure your password is correct!";
                    }
                    else txtError.Text = "You are not connected to Internet";
                txtError.Focus(FocusState.Pointer);
                }
                catch (Exception ex)
                {
                    httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    txtError.Text = httpResponseBody;
                }
            }
           
    }
}
