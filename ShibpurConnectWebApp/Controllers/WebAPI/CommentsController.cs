using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;

namespace ShibpurConnectWebApp.Controllers.WebAPI
{    
    
    public class CommentsController : ApiController
    {
        private MongoHelper<Comment> _mongoHelper;

        public CommentsController()
        {
            _mongoHelper = new MongoHelper<Comment>();
        }

        public IList<Comment> GetCommentsForAnswer(string answerId)
        {
            var result = _mongoHelper.Collection.AsQueryable().Where(a => a.AnswerId == answerId).OrderBy(a => a.PostedOnUtc).ToList();
            return result;
        }

        public int GetCommentCountForAnswer(string answerId)
        {
            var result = _mongoHelper.Collection.AsQueryable().Where(a => a.AnswerId == answerId);
            return result.Count();
        }

        public IHttpActionResult AddComment(Comment comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (comment == null)
            {
                return BadRequest("Request body is null. Please send a valid Questions object");
            }

            comment.PostedOnUtc = DateTime.UtcNow;

            // save the question to the database
            var result = _mongoHelper.Collection.Save(comment);

            // if mongo failed to save the data then send error
            if (!result.Ok)
                return InternalServerError();

            return CreatedAtRoute("DefaultApi", new { id = comment.CommentId }, comment);
        }
    }
}