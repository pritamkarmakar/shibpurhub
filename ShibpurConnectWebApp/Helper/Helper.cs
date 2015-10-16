using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using ShibpurConnectWebApp.Providers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Collections.Generic;

namespace ShibpurConnectWebApp.Helper
{
    public class Helper
    {
        // Bearer token to access the Web API
        public string token = string.Empty;
        TokenApi tokenAPi = null;

        private static Dictionary<string, string> Icons;
        private const string TWITTEREMOJIPATH = "https://twemoji.maxcdn.com/72x72/";
        static Helper()
        {
            Icons = new Dictionary<string, string>
            {
               { ":)", TWITTEREMOJIPATH + "1f600.png"},
               { ":(", TWITTEREMOJIPATH + "1f626.png"},
               { ":-)", TWITTEREMOJIPATH + "1f603.png"},
               { ":D", TWITTEREMOJIPATH + "1f604.png"},
               { ":X", TWITTEREMOJIPATH + "1f621.png"},
               { ":P", TWITTEREMOJIPATH + "1f61c.png"},
            };
        }

        public static string GetEmojiedString(string original)
        {
            if(string.IsNullOrEmpty(original))
            {
                return string.Empty;
            }

            string transformed = original;
            string htmlFormattedEmoji = "<span class='emoji-span'><img draggable='false' class='emoji' src={0}></span>";
            foreach(var key in Icons.Keys)
            {
                if(original.Contains(key))
                {
                    var html = string.Format(htmlFormattedEmoji, Icons[key]);
                    transformed = original.Replace(key, html);
                    original = transformed;
                }
            }

            return transformed;
        }

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

        public async Task<CustomUserInfo> FindUserByEmail(string userEmail, bool needEmploymentAndEducationDetails = false)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ShibpurConnectWebApp.Models.ApplicationUser user = await _repo.FindUserByEmail(userEmail);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    var educationInfo = string.Empty;
                    var designation = string.Empty;
                    if (needEmploymentAndEducationDetails)
                    {
                        var _mongoEducationalHistoriesHelper = new MongoHelper<EducationalHistories>();
                        var educationalHistories = _mongoEducationalHistoriesHelper.Collection.AsQueryable().Where(a => a.UserId == user.Id).OrderByDescending(b => b.GraduateYear).ToList();
                        if (educationalHistories != null && educationalHistories.Count > 0)
                        {
                            educationInfo = educationalHistories.FirstOrDefault().GraduateYear.ToString() + " " + educationalHistories.FirstOrDefault().Department;
                        }

                        var _mongEmploymentHistoriesHelper = new MongoHelper<EmploymentHistories>();
                        var employmentHistories = _mongEmploymentHistoriesHelper.Collection.AsQueryable().Where(a => a.UserId == user.Id).ToList();
                        if (employmentHistories != null && employmentHistories.Count > 0)
                        {
                            var currentJob = employmentHistories.FirstOrDefault(b => !b.To.HasValue);
                            if (currentJob == null)
                            {
                                currentJob = employmentHistories.FirstOrDefault();
                            }
                            designation = currentJob == null ? string.Empty :
                                currentJob.Title + ", " + currentJob.CompanyName;
                        }
                    }

                    return new CustomUserInfo()
                    {
                        Email = user.Email,
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount,
                        RegisteredOn = user.RegisteredOn,
                        AboutMe = user.AboutMe,
                        ProfileImageURL = user.ProfileImageURL,
                        Tags = user.Tags,
                        Followers = user.Followers,
                        Following = user.Following,
                        FollowedQuestions = user.FollowedQuestions,
                        Designation = designation,
                        EducationInfo = educationInfo
                    };

                }
            }
        }

        internal async Task<CustomUserInfo> FindUserById(string userId, bool needEmploymentAndEducationDetails = false)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = await _repo.FindUserById(userId);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    var educationInfo = string.Empty;
                    var designation = string.Empty;
                    if (needEmploymentAndEducationDetails)
                    {
                        var _mongoEducationalHistoriesHelper = new MongoHelper<EducationalHistories>();
                        var educationalHistories = _mongoEducationalHistoriesHelper.Collection.AsQueryable().Where(a => a.UserId == user.Id).OrderByDescending(b => b.GraduateYear).ToList();
                        if (educationalHistories != null && educationalHistories.Count > 0)
                        {
                            educationInfo = educationalHistories.FirstOrDefault().GraduateYear.ToString() + " " + educationalHistories.FirstOrDefault().Department;
                        }

                        var _mongEmploymentHistoriesHelper = new MongoHelper<EmploymentHistories>();
                        var employmentHistories = _mongEmploymentHistoriesHelper.Collection.AsQueryable().Where(a => a.UserId == user.Id).ToList();
                        if (employmentHistories != null && employmentHistories.Count > 0)
                        {
                            var currentJob = employmentHistories.FirstOrDefault(b => !b.To.HasValue);
                            if (currentJob == null)
                            {
                                currentJob = employmentHistories.FirstOrDefault();
                            }
                            designation = currentJob == null ? string.Empty :
                                currentJob.Title + ", " + currentJob.CompanyName;
                        }
                    }

                    return new CustomUserInfo()
                    {
                        Email = user.Email,
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount,
                        AboutMe = user.AboutMe,
                        ProfileImageURL = user.ProfileImageURL,
                        Tags = user.Tags,
                        Followers = user.Followers,
                        Following = user.Following,
                        FollowedQuestions = user.FollowedQuestions,
                        Designation = designation,
                        EducationInfo = educationInfo
                    };

                }
            }
        }

        public CustomUserInfo UpdateReputationCount(string userId, int deltaReputation, bool addReputaion = true)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = _repo.UpdateReputationCount(userId, deltaReputation, addReputaion);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return new CustomUserInfo()
                    {
                        Email = user.Email,
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount,
                        ProfileImageURL = user.ProfileImageURL,
                        Tags = user.Tags,
                        Followers = user.Followers,
                        Following = user.Following,
                        FollowedQuestions = user.FollowedQuestions
                    };

                }
            }
        }

        public CustomUserInfo UpdateFollowQuestion(string userId, string questionId, bool follow = true)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = _repo.UpdateFollowQuestion(userId, questionId, follow);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return new CustomUserInfo()
                    {
                        Email = user.Email,
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount,
                        ProfileImageURL = user.ProfileImageURL,
                        Tags = user.Tags,
                        Followers = user.Followers,
                        Following = user.Following,
                        FollowedQuestions = user.FollowedQuestions
                    };

                }
            }
        }

        public CustomUserInfo UpdateProfileImageURL(string userId, string url)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = _repo.UpdateProfileImageURL(userId, url);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return new CustomUserInfo()
                    {
                        Email = user.Email,
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location,
                        ReputationCount = user.ReputationCount,
                        ProfileImageURL = user.ProfileImageURL,
                        Tags = user.Tags,
                        Followers = user.Followers,
                        Following = user.Following,
                        FollowedQuestions = user.FollowedQuestions
                    };

                }
            }
        }

        /// <summary>
        /// Method to post a new tag, this is not in api as we don't want end user to create tag using api
        /// It will be called while posting a new question
        /// </summary>
        /// <param name="category">Categories object</param>
        /// <returns></returns>
        public async Task<Categories> PostTag(Categories category)
        {
            MongoHelper _mongoHelper = new MongoHelper("categories");
            var result = _mongoHelper.Collection.Save(category);

            // if mongo failed to save the data then send null response
            if (!result.Ok)
                return null;

            return new Categories { CategoryId = category.CategoryId, CategoryName = category.CategoryName };
        }

        /// <summary>
        /// internal method to update the QuestionSpamDTO collection
        /// </summary>
        /// <param name="questionSpamDto"></param>
        /// <returns></returns>
        internal async Task<QuestionSpamAudit> ReportSpam(QuestionSpamAudit questionSpamDto)
        {
            MongoHelper<QuestionSpamAudit> _mongoHelper = new MongoHelper<QuestionSpamAudit>();

            //check same user already reported this question or not
            var spamObj = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionSpamDto.QuestionId & m.UserId == questionSpamDto.UserId).FirstOrDefault();

            if (spamObj == null)
            {
                var result = _mongoHelper.Collection.Save(questionSpamDto);
                // if mongo failed to save the data then send null response
                if (!result.Ok)
                    return null;
            }
            else
                return new QuestionSpamAudit { SpamId = spamObj.SpamId, QuestionId = spamObj.QuestionId };

            return new QuestionSpamAudit { SpamId = questionSpamDto.SpamId, QuestionId = questionSpamDto.QuestionId };
        }

        /// <summary>
        /// Generate url slug using quetion title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        internal async Task<string> GenerateSlug(string title)
        {
            //First to lower case
            title = title.ToLowerInvariant();

            //Remove all accents
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(title);
            title = Encoding.ASCII.GetString(bytes);

            //Replace spaces
            title = Regex.Replace(title, @"\s", "-", RegexOptions.Compiled);

            //Remove invalid chars
            title = Regex.Replace(title, @"[^a-z0-9\s-_]", "", RegexOptions.Compiled);

            //Trim dashes from end
            title = title.Trim('-', '_');

            //Replace double occurences of - or _
            title = Regex.Replace(title, @"([-_]){2,}", "$1", RegexOptions.Compiled);

            return title;
        }

        /// <summary>
        /// Get the question id using the slug url
        /// </summary>
        /// <param name="slugUrl">question slug url</param>
        /// <returns></returns>
        internal string GetQuestionIdFromSlug(string slugUrl)
        {
            MongoHelper<Question> _mongoHelper = new MongoHelper<Question>();

            //find out the question object using the slugurl
            var questionObj = _mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.UrlSlug == slugUrl);

            if (questionObj != null) return questionObj.QuestionId;

            return null;
        }
    }

    /// <summary>
    /// Custom attribute to set NoStore: this will restrict chrome to cache the API request
    /// </summary>
    public class CacheControlAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            context.Response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                NoStore = true,
                MaxAge = TimeSpan.FromSeconds(0)
            };

            base.OnActionExecuted(context);
        }
    }
}