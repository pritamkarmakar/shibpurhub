using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Object to be used in the PostQuestions controller. Idea is to create a simple request object for post question
    /// </summary>
    [Serializable]
    public class QuestionDTO
    {
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "questionId")]
        public string QuestionId { get; set; }
        
        [Required]
        [MaxLength(150, ErrorMessage = "Maximum 150 characters allowed in question title")]
        public string Title { get; set; }

        [Required]
        [MaxLength(30000, ErrorMessage = "Maximum 30000 characters allowed in body")]
        public string Description { get; set; }

        [Required]
        [DataMember]
        public string[] Categories { get; set; }  
    }

    /// <summary>
    /// Object to be saved in the database
    /// </summary>
    [Serializable]
    public class Question : QuestionDTO
    { 
       
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public bool HasAnswered { get; set; }

        [DataMember]
        public DateTime? PostedOnUtc { get; set; }

        [DataMember]
        public string UrlSlug { get; set; }

        [DataMember]
        public int ViewCount { get; set; }

        public int SpamCount { get; set; }

        public List<string> Followers { get; set; }
        
        [DataMember]
        public DateTime? LastEditedOnUtc { get; set; }
    }

    /// <summary>
    /// We will use this to send data from the API. This has a new property UserName. We will add firstname, lastname as well later
    /// </summary>
    [Serializable]
    public class QuestionViewModel : Question
    {
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string UserProfileImage { get; set; }

        [DataMember]
        public bool IsAnonymous { get; set; }

        [DataMember]
        public bool IsAskedByMe { get; set; }

        [DataMember]
        public List<AnswerViewModel> Answers { get; set; }

        [DataMember]
        public int AnswerCount{ get; set; }
        
        [DataMember]
        public long TotalPages { get; set; }

        [DataMember]
        public string CareerDetail { get; set; }

        public QuestionViewModel Copy(Question question)
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
                ViewCount = question.ViewCount
            };
        }
    }    

    /// <summary>
    /// We will use this model in the most popular question model
    /// </summary>
    [Serializable]
    public class PopularQuestionModel : Question
    {
        public int AnswerCount { get; set; }
    }

    /// <summary>
    /// Model we use to post a spam report
    /// </summary>
    [Serializable]
    public class QuestionSpam
    {
        public string QuestionId { get; set; }
        public SpamType SpamType { get; set; }
    }

    /// <summary>
    /// We will use this model to save the data to database
    /// </summary>
    [Serializable]
    public class QuestionSpamAudit : QuestionSpam
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SpamId { get; set; }
        public string UserId { get; set; }
        public DateTime PostedOnUtc { get; set; }
    }

    /// <summary>
    /// spam types to use
    /// </summary>
    public enum SpamType
    {
        Spam,
        Inappropriate,        
        Hate,
        Obscene,
        Others
    }
}