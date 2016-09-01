using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Windows.Web.Http;
using System.Net.NetworkInformation;

namespace CyberConnectRuntime
{
    public sealed class Connection : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral; // Note: defined at class scope so we can mark it complete inside the OnCancel() callback if we choose to support cancellation
        string loginURL = "http://172.16.0.30:8090/login.xml";
        string username = "", password = "";
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
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
            Uri requestUri = new Uri("http://www.google.com");
            //Send the GET request asynchronously and retrieve the response as a string.
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            try
            {
                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
            if(httpResponseBody.Contains("172.16.0.30")) 
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                try
                {
                    if (localSettings.Values.ContainsKey("CyberRoam_UserName"))
                        username = localSettings.Values["CyberRoam_UserName"].ToString();
                    if (localSettings.Values.ContainsKey("CyberRoam_Password"))
                        password = localSettings.Values["CyberRoam_Password"].ToString();
                    //Create an HTTP client object
                    httpClient = new HttpClient();
                    //Add a user-agent header to the GET request. 
                    headers = httpClient.DefaultRequestHeaders;
                    //The safe way to add a header value is to use the TryParseAdd method and verify the return value is true,
                    //especially if the header value is coming from user input.
                    header = "ie";
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
                    requestUri = new Uri(loginURL);
                    //Send the GET request asynchronously and retrieve the response as a string.
                    httpResponse = new HttpResponseMessage();
                    httpResponseBody = "";
                    //Send the GET request
                    httpResponse = await httpClient.PostAsync(requestUri, new HttpFormUrlEncodedContent(content));
                    httpResponse.EnsureSuccessStatusCode();
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponseBody.Contains("You have successfully logged in"))
                    {
                        var toastTemplate = ToastTemplateType.ToastImageAndText01;
                        var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
                        var toastTextElements = toastXml.GetElementsByTagName("text");
                        toastTextElements[0].AppendChild(toastXml.CreateTextNode("CyberConnect Logged you to Cyberroam."));
                        var toast = new ToastNotification(toastXml);
                        ToastNotificationManager.CreateToastNotifier().Show(toast);
                    }
                }
                catch (Exception e)
                {
                }
            }
            _deferral.Complete();
        }
    }
}
