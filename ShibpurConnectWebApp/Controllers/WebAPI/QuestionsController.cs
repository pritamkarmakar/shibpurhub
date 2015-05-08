using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models.WebAPI;
using System.Text;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Claims;
using ShibpurConnectWebApp.Providers;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class QuestionsController : ApiController
    {
        private const int PAGESIZE = 30;

        private MongoHelper<Question> _mongoHelper;

        public QuestionsController()
        {
            _mongoHelper = new MongoHelper<Question>();
        }

        // GET: api/Questions
        /// <summary>
        /// Will return all available questions
        /// </summary>
        /// <param name="page">provide the page index</param>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetQuestions(int page = 0)
        {
            try
            {
                var questions = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.PostedOnUtc).Skip(page * PAGESIZE).Take(PAGESIZE).ToList();
                var userIds = questions.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, CustomUserInfo>();
                var helper = new Helper.Helper();
                foreach (var userId in userIds)
                {
                    Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                    var userDetail = await actionResult;
                    userDetails.Add(userId, userDetail);
                }

                var result = new List<QuestionViewModel>();
                foreach (var question in questions)
                {
                    var userData = userDetails[question.UserId];
                    var questionVM = GetQuestionViewModel(question, userData);
                    result.Add(questionVM);
                }

                return Ok(result);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// qquestions for a particular category/tag
        /// </summary>
        /// <param name="category">category/tag name</param>
        /// <param name="page">page index</param>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetQuestionsByCategory(string category, int page)
        {
            try
            {
                var result = new List<QuestionViewModel>();
                var allQuestions = _mongoHelper.Collection.FindAll().ToList();
                var matchedQuestions = new List<Question>();
                foreach (var question in allQuestions)
                {
                    var searchedCategory = question.Categories.Where(a => a.ToLower().Trim() == category.ToLower().Trim()).FirstOrDefault();
                    if (!string.IsNullOrEmpty(searchedCategory))
                    {
                        matchedQuestions.Add(question);
                    }
                }

                var questions = matchedQuestions.OrderByDescending(a => a.PostedOnUtc).Skip(page * PAGESIZE).Take(PAGESIZE).ToList();

                var userIds = questions.Select(a => a.UserId).Distinct();
                var userDetails = new Dictionary<string, CustomUserInfo>();
                var helper = new Helper.Helper();
                foreach (var userId in userIds)
                {
                    Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                    var userDetail = await actionResult;
                    userDetails.Add(userId, userDetail);
                }

                foreach (var question in questions)
                {
                    var userData = userDetails[question.UserId];
                    var questionVM = GetQuestionViewModel(question, userData);
                    result.Add(questionVM);
                }

                return Ok(result);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Will return a specific question with comments
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetQuestion(string questionId)
        {
            try
            {
                var question = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).FirstOrDefault();
                if (question == null)
                {
                    return NotFound();
                }

                ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
                var claim = principal.FindFirst("sub");

                Helper.Helper helper = new Helper.Helper();
                var userResult = helper.FindUserByEmail(claim.Value);
                var userInfo = await userResult;

                if(userInfo == null)
                {
                    return BadRequest("No UserId is found");
                }

                var questionVM = new QuestionViewModel().Copy(question);
                questionVM.IsAskedByMe = question.UserId == userInfo.UserId;

                var _answerMongoHelper = new MongoHelper<Answer>();
                var answers = _answerMongoHelper.Collection.AsQueryable().Where(a => a.QuestionId == questionId).OrderByDescending(a => a.MarkedAsAnswer).ThenBy(b => b.PostedOnUtc).ToList();
                var userDetails = new Dictionary<string, CustomUserInfo>();
                
                Task<CustomUserInfo> actionResult1 = helper.FindUserById(question.UserId);
                var questionUserDetail = await actionResult1;
                userDetails.Add(question.UserId, questionUserDetail);
                questionVM.UserEmail = questionUserDetail.Email;
                questionVM.DisplayName = questionUserDetail.FirstName;
                questionVM.UserProfileImage = questionUserDetail.ProfileImageURL;

                if(answers.Count() == 0)
                {
                    return Ok(questionVM);
                }                
                
                var answerVMs = new List<AnswerViewModel>();
                var _commentsMongoHelper = new MongoHelper<Comment>();
                var allUserIds = new List<string>();
                var allComments = new List<Comment>();
                foreach(var answer in answers)
                {
                    var comments = _commentsMongoHelper.Collection.AsQueryable().Where(a => a.AnswerId == answer.AnswerId).OrderBy(a => a.PostedOnUtc).ToList();
                    allComments.AddRange(comments);
                    var answervm = new AnswerViewModel().Copy(answer);
                    answervm.Comments = comments;
                    answervm.IsUpvotedByMe = answervm.UpvotedByUserIds != null && answervm.UpvotedByUserIds.Contains(userInfo.UserId);
                    answerVMs.Add(answervm);
                }

                allUserIds.Add(question.UserId);
                allUserIds.AddRange(answers.Select(a => a.UserId));
                allUserIds.AddRange(allComments.Select(c => c.UserId));

                var uniqueUserIds = new List<string>(allUserIds.Distinct());

                    
                foreach (var userId in uniqueUserIds)
                {
                    Task<CustomUserInfo> actionResult = helper.FindUserById(userId);
                    var userDetail = await actionResult;
                    if (!userDetails.ContainsKey(userId))
                    {
                        userDetails.Add(userId, userDetail);
                    }
                }

                foreach(var answerVM in answerVMs)
                {
                    var userData = userDetails[answerVM.UserId];
                    answerVM.UserEmail = userData.Email;
                    answerVM.DisplayName = userData.FirstName + " " + userData.LastName;
                    answerVM.UserProfileImage = userData.ProfileImageURL;
                }

                questionVM.Answers = answerVMs;
                return Ok(questionVM);
            }
            catch (FormatException fe)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Get the answer count for a particular question
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <returns></returns>
        public IHttpActionResult GetAnswersCount(string questionId)
        {
            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return Ok(0);
            }

            try
            {
                var answerMongoHelper = new MongoHelper<Answer>();
                var count = answerMongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).ToList().Count;
                return Ok(count);
            }
            catch (MongoDB.Driver.MongoConnectionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get total user view count for a particular question
        /// </summary>
        /// <param name="questionId">question id</param>
        /// <returns></returns>
        public int GetViewCount(string questionId)
        {
            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return 0;
            }

            var question = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).FirstOrDefault();
            return question == null ? 0 : question.ViewCount;
        }

        /// <summary>
        /// Increment view count for a question
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        [ResponseType(typeof(int))]
        [ActionName("IncrementViewCount")]
        public int IncrementViewCount(Question question)
        {
            try
            {
                ObjectId.Parse(question.QuestionId);
            }
            catch (Exception)
            {
                return 0;
            }

            var questionInDB = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == question.QuestionId).FirstOrDefault();
            if (questionInDB != null)
            {
                var count = questionInDB.ViewCount + 1;
                questionInDB.ViewCount = count;
                _mongoHelper.Collection.Save(questionInDB);
                return count;
            }

            return 0;
        }

        /// <summary>
        /// API to post a new question
        /// </summary>
        /// <param name="question">QuestionDTO object</param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(Question))]
        public async Task<IHttpActionResult> PostQuestions(QuestionDTO question)
        {
            // validate title
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (question == null)
                return BadRequest("Request body is null. Please send a valid QuestionDTO object"); 
         
            // validate if incase any category is just space
            foreach(string cat in question.Categories)
            {
                if(string.IsNullOrWhiteSpace(cat))
                {
                    ModelState.AddModelError("", "One of the question category is empty string or contains only whitespace");
                    return BadRequest(ModelState); 
                }                
            }
         
            ClaimsPrincipal principal = Request.GetRequestContext().Principal as ClaimsPrincipal;
            var claim = principal.FindFirst("sub");

            Helper.Helper helper = new Helper.Helper();
            var userResult = helper.FindUserByEmail(claim.Value);
            var userInfo = await userResult;
            if (userInfo == null)
            {
                return BadRequest("No UserId is found");
            }

            // new question object that we will save in the database
            Question questionToPost = new Question()
            {
                Title = question.Title,
                UserId = userInfo.UserId,
                Description = question.Description,
                Categories = question.Categories.Select(c => c.Trim()).ToArray(),
                QuestionId = ObjectId.GenerateNewId().ToString(),
                PostedOnUtc = DateTime.UtcNow
            };

           
            // create the new categories            
            List<Categories> categoryList = new List<Categories>();
            CategoriesController categoriesController = new CategoriesController();
            foreach (string category in question.Categories)
            {
                var actionResult = categoriesController.GetCategory(category.Trim());
                var contentResult = actionResult as OkNegotiatedContentResult<Categories>;
                if (contentResult == null)
                {
                    Categories catg = new Categories()
                    {
                        CategoryId = ObjectId.GenerateNewId(),
                        CategoryName = category.Trim().ToLower(),
                        HasPublished = false
                    };
                    var actionResult2 = categoriesController.PostCategories(catg);
                    var contentResult2 = actionResult2 as CreatedAtRouteNegotiatedContentResult<Categories>;
                    if (contentResult2 != null)
                    {
                        // update the CategoryTagging collection
                        CategoryTaggingController categoryTaggingController = new CategoryTaggingController();

                        CategoryTagging ct = new CategoryTagging();
                        ct.Id = ObjectId.GenerateNewId();
                        ct.CategoryId = catg.CategoryId;
                        ct.QuestionId = questionToPost.QuestionId;
                        categoryTaggingController.PostCategoryTagging(ct);
                    }
                    else
                        return InternalServerError(new Exception());
                }
            }  
      
            // save the question to the database
            var result = _mongoHelper.Collection.Save(questionToPost);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = questionToPost.QuestionId }, questionToPost);
        }
        
        private QuestionViewModel GetQuestionViewModel(Question question, CustomUserInfo userData)
        {
            return new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                Title = question.Title,
                Description = question.Description,
                UserId = question.UserId,
                HasAnswered = question.HasAnswered,
                PostedOnUtc = question.PostedOnUtc,
                Categories = question.Categories,
                ViewCount = question.ViewCount,
                UserEmail = userData.Email,
                UserProfileImage = userData.ProfileImageURL,
                DisplayName = userData.FirstName + " " + userData.LastName
            };
        }
    }
}