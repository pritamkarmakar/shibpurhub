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
        [Required]
        [MinLength(20, ErrorMessage = "Minimum length of the question title should be more than 20 characters")]
        [MaxLength(150, ErrorMessage = "Maximum 150 characters allowed in question title")]
        public string Title { get; set; }

        [Required]
        [MinLength(40, ErrorMessage = "Minimum length of body should be more than 40 characters")]
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
        // Primary key
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "questionId")]
        public string QuestionId { get; set; }
       
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public bool HasAnswered { get; set; }

        [DataMember]
        public DateTime? PostedOnUtc { get; set; }       

        [DataMember]
        public int ViewCount { get; set; }
    }

    /// <summary>
    /// We will use this to send data from the API. This has a new property UserName. We will add firstname, lastname as well later
    /// </summary>
    public class QuestionViewModel : Question
    {
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string UserProfileImage { get; set; }

        [DataMember]
        public bool IsAskedByMe { get; set; }

        [DataMember]
        public List<AnswerViewModel> Answers { get; set; }

        [DataMember]
        public int AnswerCount{ get; set; }
        
        [DataMember]
        public long TotalPages { get; set; }

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
}