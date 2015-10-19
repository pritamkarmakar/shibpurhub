using System;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShibpurConnectWebApp.Models.WebAPI
{
    /// <summary>
    /// Object to be used in the PostAnswer controller. Idea is to create a simple request object for post answer
    /// </summary>
    [Serializable]
    public class AnswerDTO
    {
        [DataMember]
        [Required]
        [MaxLength(10015, ErrorMessage = "Answer can be maximum 10000 characters long")]
        public string AnswerText { get; set; }

        [DataMember]
        public string QuestionId { get; set; }        
    }

    /// <summary>
    /// We use this model to save into database
    /// </summary>
    [BsonIgnoreExtraElements]
    [Serializable]
    public class Answer : AnswerDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty(PropertyName = "answerId")]
        public string AnswerId { get; set; }

        [DataMember]
        public string UserId { get; set; } 

        [DataMember]
        public bool MarkedAsAnswer { get; set; }

        [DataMember]
        public int UpVoteCount { get; set; }

        [DataMember]
        public int DownVoteCount { get; set; }

        [DataMember]
        public DateTime PostedOnUtc { get; set; }

        [DataMember]
        public List<string> UpvotedByUserIds { get; set; }
        
        [DataMember]
        public DateTime? LastEditedOnUtc { get; set; }
    }

    [Serializable]
    public class AnswerViewModel : Answer
    {
        [DataMember]
        public string UserEmail { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string UserProfileImage { get; set; }

        [DataMember]
        public List<CommentViewModel> Comments { get; set; }        

        [DataMember]
        public bool IsUpvotedByMe { get; set; }

        [DataMember]
        public List<CustomUserInfo> UpvotedByUsers { get; set; }
        
        public bool IsAnsweredByMe { get; set; }

        public AnswerViewModel Copy(Answer answer)
        {
            return new AnswerViewModel
            {
                AnswerId = answer.AnswerId,
                UserId = answer.UserId,
                QuestionId = answer.QuestionId,
                AnswerText = answer.AnswerText,
                DownVoteCount = answer.DownVoteCount,
                MarkedAsAnswer = answer.MarkedAsAnswer,
                PostedOnUtc = answer.PostedOnUtc,
                UpVoteCount = answer.UpVoteCount,
                UpvotedByUserIds = answer.UpvotedByUserIds
            };
        }
    }

    /// <summary>
    /// We will use this model in the user profile page, to show what all answers user have posted
    /// </summary>
    public class AnswerWithQuestionTitle : Answer
    {
        public string QuestionTitle { get; set; }
    }
}