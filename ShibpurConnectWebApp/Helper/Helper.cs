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
using ShibpurConnectWebApp.Controllers.WebAPI;
using System.Web.Http.Results;
using System.Configuration;

namespace ShibpurConnectWebApp.Helper
{
    public class Helper
    {
        // read the BEC university present and past names
        private static string becNames = ConfigurationManager.AppSettings["becnames"];

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

        public string GetUserCareerDetail(CustomUserInfo userInfo)
        {
            if(userInfo == null)
            {
                return string.Empty;
            }

            return userInfo.Designation + " " +
                        (string.IsNullOrEmpty(userInfo.EducationInfo) ? string.Empty : (
                        string.IsNullOrEmpty(userInfo.Designation) ? userInfo.EducationInfo :
                            "(" + userInfo.EducationInfo + ")")
                        );
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
                    return ConstructUserInformation(user, needEmploymentAndEducationDetails);
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
                    return ConstructUserInformation(user, needEmploymentAndEducationDetails);
                }
            }
        }

        private CustomUserInfo ConstructUserInformation(ApplicationUser user, bool needEmploymentAndEducationDetails)
        {
            var educationInfo = string.Empty;
            var designation = string.Empty;
            List<EducationalHistories> EducationalHistories = null;
            List<EmploymentHistories> EmploymentHistories = null;
            if (needEmploymentAndEducationDetails)
            {
                var _mongoEducationalHistoriesHelper = new MongoHelper<EducationalHistories>();
                var educationalHistories = _mongoEducationalHistoriesHelper.Collection.AsQueryable().Where(a => a.UserId == user.Id).OrderByDescending(b => b.GraduateYear).ToList();
                if (educationalHistories != null && educationalHistories.Count > 0)
                {
                    // see if user has any BEC education otherwise consider the latest one. If user has multiple BEC education (BE, ME) then we are considering the BE education
                    var becEducation = educationalHistories.FindLast(m => m.IsBECEducation == true);
                    if (becEducation != null)
                        educationInfo = becEducation.GraduateYear.ToString() + " " + becEducation.Department;
                    else
                        educationInfo = educationalHistories.FirstOrDefault().GraduateYear.ToString() + " " + educationalHistories.FirstOrDefault().Department;

                    // set the EducationalHistories properties that will have all the education details, required for ProfileController and activity feed to create user card (new user sign-up, follow user)
                    EducationalHistories = educationalHistories;
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

                    // set the EmploymentHistories properties that will have all the education details, required for ProfileController and activity feed to create user card (new user sign-up, follow user)
                    EmploymentHistories = employmentHistories;
                }
            }

            return GetCustomUserInfoFromAppicationUser(user, educationInfo, designation, EducationalHistories, EmploymentHistories);
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
                    return GetCustomUserInfoFromAppicationUser(user, null, null, null, null);

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
                    return GetCustomUserInfoFromAppicationUser(user, null, null, null, null);

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
                   return GetCustomUserInfoFromAppicationUser(user, null, null, null, null);
                }
            }
        }

        public CustomUserInfo UpdateCareerInfo(string userId, string designation, string education)
        {
            using (AuthRepository _repo = new AuthRepository())
            {
                ApplicationUser user = _repo.UpdateCareerInfo(userId, designation, education);

                if (user == null)
                {
                    return null;
                }
                else
                {
                    return GetCustomUserInfoFromAppicationUser(user, null, null, null, null);
                }
            }
        }

        private CustomUserInfo GetCustomUserInfoFromAppicationUser(ApplicationUser user, string edh, string emh, List<EducationalHistories> educationalHistories, List<EmploymentHistories> employmentHistories)
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
                RegisteredOn = user.RegisteredOn,
                LastSeenOn = user.LastSeenOn,
                FollowedQuestions = user.FollowedQuestions,
                AboutMe = user.AboutMe,
                Designation = emh,
                EducationInfo = edh,
                EducationalHistories = educationalHistories,
                EmploymentHistories = employmentHistories
            };
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
        /// Method to post a new skillset, this is not in api as we don't want end user to create skillset using api
        /// It will be called while posting a new question
        /// </summary>
        /// <param name="skillSets">SkillSets object</param>
        /// <returns></returns>
        public async Task<SkillSets> PostSkillSet(SkillSets skillSets)
        {
            MongoHelper _mongoHelper = new MongoHelper("skillsets");
            var result = _mongoHelper.Collection.Save(skillSets);

            // if mongo failed to save the data then send null response
            if (!result.Ok)
                return null;

            return new SkillSets { SkillSetId = skillSets.SkillSetId, SkillSetName = skillSets.SkillSetName };
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
        internal QuestionDTO GetQuestionIdFromSlug(string slugUrl)
        {
            MongoHelper<Question> _mongoHelper = new MongoHelper<Question>();

            //find out the question object using the slugurl
            var questionObj = _mongoHelper.Collection.AsQueryable().FirstOrDefault(m => m.UrlSlug == slugUrl);

            if (questionObj != null)
                return new QuestionDTO
                {
                    QuestionId = questionObj.QuestionId,
                    Title = questionObj.Title
                };               

            return null;
        }

        /// <summary>
        /// Method to check whether the university name is belongs to BEC
        /// </summary>
        /// <param name="universityName"></param>
        /// <returns></returns>
        internal bool CheckUniversityName(string universityName)
        {
            if(!String.IsNullOrEmpty(becNames))
            {
                foreach(string name in becNames.Split(','))
                {
                    // before sending for comparison we want to remove the 'Shibpur' word if it is there
                    string newName = universityName.ToLower().Replace("shibpur", "").Replace(",", "");
                    int distance = Compute(name.Trim().ToLower(), newName.Trim());

                    if (distance == 0)
                        return true;

                    if (universityName.Length > 20 && distance < 8)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Compute the distance between two strings. Levenshtein distance algorithm
        /// </summary>
        private int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
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