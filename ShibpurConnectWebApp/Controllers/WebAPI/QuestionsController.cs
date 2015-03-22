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

namespace ShibpurConnectWebApp.Controllers.WebAPI
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class QuestionsController : ApiController
    {
        private MongoHelper<QuestionsDTO> _mongoHelper;
        private ElasticSearchHelper _elasticSearchHealer;

        public QuestionsController()
        {
            _mongoHelper = new MongoHelper<QuestionsDTO>();
            _elasticSearchHealer = new ElasticSearchHelper();
        }

        // GET: api/Questions
        /// <summary>
        /// Will return all available questions
        /// </summary>
        /// <returns></returns>
        public IList<QuestionsDTO> GetQuestions()
        {
            var result = _mongoHelper.Collection.FindAll().OrderByDescending(a => a.PostedOnUtc).ToList();

            return result;
        }

        public IList<QuestionsDTO> GetQuestionsByCategory(string category)
        {
            var result = new List<QuestionsDTO>();
            var allQuestions = _mongoHelper.Collection.FindAll().ToList();
            foreach (var question in allQuestions)
            {
                var searchedCategory = question.Categories.Where(a => a.ToLower().Trim() == category.ToLower().Trim()).FirstOrDefault();
                if (!string.IsNullOrEmpty(searchedCategory))
                {
                    result.Add(question);
                }
            }

            return result;
        }

        // GET: api/Questions/5
        // Will return a specific question with comments
        [ResponseType(typeof(QuestionsDTO))]
        public IHttpActionResult GetQuestion(string questionId)
        {
            // validate questionId is valid hex string

            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return BadRequest(String.Format("Supplied questionId: {0} is not a valid object id", questionId));
            }


            var questions = _mongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId);
            if (questions.Count() == 0)
            {
                return NotFound();
            }

            return Ok(questions.ToList()[0]);
        }

        public int GetAnswersCount(string questionId)
        {
            try
            {
                ObjectId.Parse(questionId);
            }
            catch (Exception)
            {
                return 0;
            }

            var answerMongoHelper = new MongoHelper<Answer>();
            var count = answerMongoHelper.Collection.AsQueryable().Where(m => m.QuestionId == questionId).ToList().Count;
            return count;
        }

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

        [ResponseType(typeof(int))]
        [ActionName("IncrementViewCount")]
        public int IncrementViewCount(QuestionsDTO question)
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

        // PUT: api/Questions/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutQuestions(string id, Question questions)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != questions.QuestionId)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(questions).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!QuestionsExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        // POST: api/Questions
        [ResponseType(typeof(Questions))]
        public IHttpActionResult PostQuestions(QuestionsDTO question)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (question == null)
                return BadRequest("Request body is null. Please send a valid Questions object");

            // if Question doesn't have any category tagging then its a bad request
            if (question.Categories == null || question.Categories.Length == 0)
                return BadRequest("Question must have a category association");

            // create the new categories            
            List<Categories> categoryList = new List<Categories>();
            CategoriesController categoriesController = new CategoriesController();
            foreach (string category in question.Categories)
            {
                var actionResult = categoriesController.GetCategory(category);
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
                        ct.QuestionId = question.QuestionId;
                        categoryTaggingController.PostCategoryTagging(ct);
                    }
                    else
                        return InternalServerError(new Exception()); 
                }               
            }          

            // add the datetime stamp for this question
            question.PostedOnUtc = DateTime.UtcNow;
            // create the question id
            question.QuestionId = ObjectId.GenerateNewId().ToString();
            // save the question to the database
            var result = _mongoHelper.Collection.Save(question);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();     

            return CreatedAtRoute("DefaultApi", new { id = question.QuestionId }, question);
        }

        //// DELETE: api/Questions/5
        //[ResponseType(typeof(Question))]
        //public IHttpActionResult DeleteQuestions(string id)
        //{
        //    //Question questions = db.Questions.Find(id);
        //    //if (questions == null)
        //    //{
        //    //    return NotFound();
        //    //}

        //    //db.Questions.Remove(questions);
        //    //db.SaveChanges();

        //    //return Ok(questions);
        //}

        public async Task<IHttpActionResult> GetSuggestedUserProfiles(string categories)
        {
            // validate categories is not empty
            if (string.IsNullOrEmpty(categories))
            {
                return null;
            }

            // convert the comma(,) separated categories into space separeted single string. We will use this string as search text in elastic search
            StringBuilder sb = new StringBuilder();

            foreach (string str in categories.Split(','))
            {
                sb.Append(str.Trim() + " ");
            }

            var client = _elasticSearchHealer.ElasticClient();
            var result = client.Search<object>(s => s.AllIndices().AllTypes().Query(query => query
        .QueryString(qs => qs.Query(sb.ToString().TrimEnd()))));

                   
            // retrieve the unique user info from the result
            HashSet<string> hash = new HashSet<string>();
            List<UserProfile> userProfileList = new List<UserProfile>();
            
            foreach (object obj in result.Documents)
            {
                var userData = JObject.Parse(obj.ToString());
                if (!hash.Contains((string)userData["userId"]))
                {
                  hash.Add((string)userData["userId"]);

                    // retrieve the userInfo
                  ProfileController profileController = new ProfileController();
                  IHttpActionResult actionResult = await profileController.GetProfileByUserId((string)userData["userId"]);
                  var userProfile = actionResult as OkNegotiatedContentResult<UserProfile>;

                  userProfileList.Add(userProfile.Content);
                }
            }


            return Ok(userProfileList);
        }
    }
}