using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Helper
{
    public class HelperClass
    {
        // Bearer token to access the Web API
        public string token = string.Empty;
        TokenApi tokenAPi = null;

        /// <summary>
        /// Method to retrieve the response of a http GET request
        /// </summary>
        /// <param name="url">Web API URL (I'm consuming this from web.config)</param>
        /// <para name="token">BEarer token required to access Web API</para>
        /// <returns>response from the GET request</returns>
        public string HttpGETResponse(string url, string token)
        {
            // read the saved token from the Claim Identity


            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage apiVersionResp = null;

            try
            {
                apiVersionResp =
                    client.GetAsync(url).Result;
                if (apiVersionResp.StatusCode != HttpStatusCode.OK)
                    return string.Empty;
            }
            catch (AggregateException ex)
            {
                return string.Empty;
            }

            return apiVersionResp.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Method to retrieve the response after http POST request
        /// </summary>
        /// <param name="url">API URL</param>
        /// <param name="content">HttpContent httpContent</param>
        /// <returns>response after the POST request</returns>
        public static string HttpPostResponse(string url, HttpContent content)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage apiVersionResp = null;

            try
            {
                apiVersionResp = client.PostAsync(url, content).Result;

                return apiVersionResp.Content.ReadAsStringAsync().Result;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Method to check whether user is valid. This will make a call to the "/Token" API and will retrieve the Bearer token. 
        /// For invalid user the response will be blank
        /// </summary>
        /// <param name="webApiurl"></param>
        /// <param name="loginViewModel"></param>
        /// <returns></returns>
        public bool IsUserAuthenticated(string webApiurl, LoginViewModel loginViewModel)
        {
            HttpContent httpContent =
                new StringContent(
                    string.Format(
                        "grant_type=password&username={0}&password={1}",
                        loginViewModel.Email, loginViewModel.Password), Encoding.UTF8, "application/json");
            string apiResponse = HttpPostResponse(webApiurl + "/Token", httpContent);

            TokenApi tokenApi = JsonConvert.DeserializeObject<TokenApi>(apiResponse);

            if (tokenApi != null)
                token = tokenApi.access_token;

            if (apiResponse.ToLower().Contains("access_token\":"))
            {
                return true;
            }

            return false;
        }

        public string RegisterUser(string webApiUrl, RegisterViewModel registerViewModel)
        {
            HttpContent httpContent =
                new StringContent(
                    string.Format("{{\"Email\": \"{0}\",\"Password\": \"{1}\",\"ConfirmPassword\": \"{2}\"}}",
                        registerViewModel.Email, registerViewModel.Password, registerViewModel.ConfirmPassword), Encoding.UTF8, "application/json");
            var apiResponse = HttpPostResponse(webApiUrl + "/api/Account/Register", httpContent);

            RegisterAPI registerApi = JsonConvert.DeserializeObject<RegisterAPI>(apiResponse);

            if (registerApi == null) return "Succeed";
            if (registerApi.Message.Contains("The request is invalid"))
                return registerApi.Message;

            return "Succeed";
        }
    }
}