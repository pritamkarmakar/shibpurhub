var advancedEditor;
// current logged-in user
var userId = null;
// we will keep the default users that can answer this question and we will retrieve it during page load
var userListAskToAnswer;
var myImageUrl = "/Content/images/profile-image.jpg";

// get the current logged-in user details
var userDetails = localStorage.getItem("SC_Session_UserDetails");
var userInfo = $.parseJSON(userDetails);
if (userInfo != null) {
    userId = userInfo.id;
    if (userInfo.profileImageURL != null)
        myImageUrl = "http://i.imgur.com/" + userInfo.profileImageURL;
}

//used to reference rich textbox editor for question edit
var qustionRTBoxEditor;
var categoryArr = [];
$(document).ready(function () {
    scrollToADivOnPageLoad();
    // hide the submit answer rich text control
    $('.submit-answer-container').hide();

    // hide the right column modules, we will enable inside respective the partial view once receive the server response
    $('.tagcontainermaindiv').hide();
    $('.popularquestioncontainerdiv').hide();

    // hide 'ask to answer' module
    $('#userToAnswer').hide();

    if($('.write-answer .text-wrapper .editor-container').length > 0)
    {
        advancedEditor = new Quill('.write-answer .text-wrapper .editor-container', {
            modules: {
                'authorship': {
                    authorId: 'advanced',
                    enabled: true
                },
                'toolbar': {
                    container: '.write-answer .text-wrapper .toolbar-container'
                },
                'link-tooltip': true,
                'image-tooltip': true,
                'multi-cursor': true
            },
            styles: false,
            theme: 'snow'
        });
    }


    jQuery.support.cors = true;
    var SERVER = "/api/";
    

    // set the questionID attribute of the 'Follow' button
    $(".followquestionbutton").attr("id", jobId);
    $(".followquestionbutton").attr('style', 'background-color:#E4EDF4;');
    
    scAjax({
        "url": "career/getjob?jobId=" + jobId,
        "success": function (result) {
            if (!result) {
                return;
            }
            // hide the loading div
            $('#loadingdiv').hide();
            var jobdetails = result;
            $('#details').show();
            
            createJobDetails(jobdetails);
        }
        
    });
	
});

function createJobDetails(question)
{
    document.title = question.jobTitle + " - ShibpurHub";
    
    var htmlItem = $('div.item.question.hide').clone().removeClass('hide');

    var creatorImage = $(htmlItem).find('.post-creator-image');
    if (question.userProfileImage != null)
        $(creatorImage).attr("href", "/Account/Profile?userId" + question.userId).css('background-image', "url(http://i.imgur.com/" + question.userProfileImage + ")");
    else
        $(creatorImage).attr("href", "/Account/Profile?userId" + question.userId).css('background-image', "url(/Content/images/profile-image.jpg)");

    $(htmlItem).find('a.name-link').text(question.displayName).attr("href", "/Account/Profile?userId=" + question.userId);
    
    $(htmlItem).find('h2.title a').text(question.jobTitle);
    
    $(htmlItem).find('div.job-company p').html(question.jobCompany);
    $(htmlItem).find('div.job-location p').html(question.jobCity+ ", " + question.jobCountry);
    $(htmlItem).find('div.post-description p').html(question.jobDescription);
    $(htmlItem).find('p.designation').text(question.careerDetail);
    $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");

    if (question.viewCount && question.viewCount > 1)
    {
        $(htmlItem).find('span.view-count').text(question.viewCount + " views");
    }
    $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(question.postedOnUtc));
    
    var followButton = $(htmlItem).find('.follow-ul a.thumbs');
    $(followButton).attr({ 'data-questionId': question.questionId, 'id': question.questionId });
    
    var questionIds = [];
    questionIds.push(question.questionId);

    // add the job skillset tags
    var skillset = $(htmlItem).find('li.jobskills');
    $(question.skillSets).each(function (index) {
        $(skillset).append("<i class='fa fa-tags'>" + question.skillSets[index] + "</i>&nbsp;&nbsp;");
    });
    

    if(!question.isAskedByMe)
    {
        $(htmlItem).find('.follow-ul').show();
    }
    
    $("div.question-container").append(htmlItem);
    
    $(followButton).click(function(event){
          event.preventDefault();
          followQuestion(question.questionId);
    });
  
    scAjax({
        "url": "career/incrementviewcount?jobId=" + jobId,
        "type": "POST",
        "success": function (result) {
        }
    });
    
    // hide the loading symbol for the answers
    $('#loadingdiv').hide();

    // show the submit answer rich text control
    $('.submit-answer-container').show();
    advancedEditor.on('text-change', function(delta, source) {
        if(!$('.toolbar-container').is(":visible"))
        {
            $('.toolbar-container').show("slide", {"direction": "down"});
        }
    });
    
    $('.btn-submit-answer').click(function () {
        saveApplication();
    });
       
    var jobapplications = question.jobApplications;
    if (!jobapplications) {
        return;
    }
    
    var answerIds = [];
    $(jobapplications).each(function (index, jobapplication) {
        answerIds.push(jobapplication.applicationId);
        createApplication(jobapplication);
    });

    updateQnAStatus(questionIds, answerIds);

    if (answerId && answerId != "") {
        $('html,body').animate({ scrollTop: $('#' + answerId).offset().top - 70 }, 'fast');
    }
}

function createApplication(jobapplication)
{
    var htmlItem = $('div.item.answer.hide').clone().removeClass('hide').attr('id', jobapplication.applicationId);

    var creatorImage = $(htmlItem).find('.post-creator-image');
    if (jobapplication.userProfileImage != null)
        $(creatorImage).attr("href", "/Account/Profile?userId" + jobapplication.userId).css('background-image', "url(http://i.imgur.com/" + jobapplication.userProfileImage + ")");
    else {
        $(creatorImage).attr("href", "/Account/Profile?userId" + jobapplication.userId).css('background-image', "url(/Content/images/profile-image.jpg)");
    }

    $(htmlItem).find('a.name-link').text(jobapplication.displayName).attr("href", "/Account/Profile?userId=" + jobapplication.userId);
    
    $(htmlItem).find('div.post-description p').html(jobapplication.coverLetter);
    $(htmlItem).find('p.designation').text(jobapplication.careerDetail);
    $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");
    
    $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(jobapplication.postedOnUtc));
    
    var upvoteButton = $(htmlItem).find('.upvote-ul a.thumbs');
    $(upvoteButton).attr({ 'data-answerId': jobapplication.applicationId, 'id': jobapplication.applicationId });
    
    $("div.answer-container").append(htmlItem);

    $(upvoteButton).click(function (event) {
        event.preventDefault();
        updateUpVote(jobapplication.applicationId);
    });
    
    $('.myimg').css('background-image',"url("+ myImageUrl +")");
    
    var comments = jobapplication.applicationComments;
    if (!comments) {
        return;
    }
    
    $(comments).each(function (index, comment) {
        createComment(comment);
    });
    
    $('#' + jobapplication.applicationId).find('textarea.write-comment').bind('keyup', function (event) {
        if (event.keyCode === 13){
            var commentText = $(this).val();
            if(!commentText || commentText == "")
            {
                return;
            }
            
            var postComment = {};
            postComment.commentText = commentText;
            postComment.applicationId = jobapplication.applicationId;
            postComment.userId = userInfo.id;
            postComment.userProfileImage = userInfo.profileImageURL;
            postComment.displayName = userInfo.firstName + " " + userInfo.lastName;
            postComment.postedOnUtc = new Date();
            
            saveComment(postComment);
            
            $(this).val("");
        }
    });

    
}

function createComment(comment)
{
    var applicationId = comment.applicationId;
    var htmlItem = $('#'+ answerId +" .comments .post-comment.hide").clone().removeClass('hide');
    
    $(htmlItem).find('.img-container').css('background-image', "url(http://i.imgur.com/" + comment.userProfileImage + ")");
    
    $(htmlItem).find('.commentContent p').html(comment.commentText);
    $(htmlItem).find('a.comment-author-name').text(comment.displayName).attr("href", "/Account/Profile?userId=" + comment.userId);
    
    $(htmlItem).find('span.comment-time').text(getDateFormattedByMonthYear(comment.postedOnUtc));
    
    $('#'+ answerId +" .comments").append(htmlItem);
}

// function to send notification to the specific user from 'Ask to Answer' module
function SendNotification(elm) {
    if (userInfo) {
        // disable the Ask button and change the test to Asked
        $(elm).text("Asked");
        $(elm).attr("disabled", "disabled");

        // save this 'ask to answer' record in database collection
        var asktoanswer = {
            "AskedTo": elm.attributes["data-id"].value,
            "AskedBy": userInfo.id,
            "QuestionId": questionID
        };
        scAjax({
            "url": "asktoanswer/postasktoanswer",
            "type": "POST",
            "data": JSON.stringify(asktoanswer, null, 2),
            "success": function (result) {

            }
        });
    }
}

function handleAjaxError(XMLHttpRequest, textStatus, errorThrown)
{
    var errorText = XMLHttpRequest.responseText.replace('{"message":"', " ").replace('"}', " ");
    // toastr notification
    Command: toastr["error"](errorText)
    // hide the loading message
    $('#loadingdiv').hide();

    // hide the loading message in ask to answer module
    $('#loadingasktoanswer').hide();
}

function scrollToADivOnPageLoad()
{
    var hash = window.location.hash;
    if(hash.length > 0 && $('#'+ hash).size() > 0){
        $('html, body').animate({ scrollTop: $(hash).offset().top - 70});
    }
}

function enableOrDisableSubmitAnswer(enable)
{
    if(enable)
    { 
        // enable the button
        $('.btn-submit-answer').prop('disabled', false).removeClass("disabled");

        // remove the loading class from save button
        $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
        $('.btn-submit-answer > span').text('Submit Answer');
    }
    else
    {
        // disable the button
        $('.btn-submit-answer').prop('disabled', true).addClass("disabled");
    
        // add spinner animation in the save button and change the text to 'Saving..'
        $('.btn-submit-answer > i').addClass('fa fa-circle-o-notch fa-spin');
        $('.btn-submit-answer > span').text(' Saving...');
    }
}

function saveApplication() {
    // hide the error div
    $('#errormessage').empty().hide();
    
    enableOrDisableSubmitAnswer(false);

    jQuery.support.cors = true;
    var userDetails = localStorage.getItem("SC_Session_UserDetails");

    if (!userDetails) {
        //TO-DO: Handle if userdetail is not available in Session Storage
        window.location.href = "/account/login";
    }

    var answer = advancedEditor.getHTML();

    // verify if answer text is empty
    if (!answer || $.trim(advancedEditor.getText()) == "") {
        enableOrDisableSubmitAnswer(true);

        // parse the error json
        $('#errormessage').show().append("<p>Answer can't be blank</p>");
        return;
    }
    
    var userInfo = $.parseJSON(userDetails);
    
    var answerObject = { "CoverLetter": answer, "JobId": jobId };

    advancedEditor.setHTML("");
    
    scAjax({
        "url": "career/applyforajob",
        "type": "POST",
        "data": JSON.stringify(answerObject),
        "success": function (result, data) {
            if (!result) {
                return;
            }
            
            var answer = result;
            enableOrDisableSubmitAnswer(true);
            //logActivity(2, answer.answerId);
            
            answer.userId = userInfo.id;
            answer.userProfileImage = userInfo.profileImageURL;
            answer.displayName = userInfo.firstName + " " + userInfo.lastName;
            createAnswer(answer);
            
            $('html, body').animate({ scrollTop: $("#"+ answer.answerId).offset().top - 70});
        }
    });
}

function saveComment(comment)
{
    if(!comment)
    {
        return;
    }
    
    createComment(comment);
    var data = { "CommentText": $.trim(comment.commentText), "ApplicationId": comment.applicationId };
    scAjax({
        "url": "jobapplicationcomments/postcomment",
        "type": "POST",
        "data": JSON.stringify(data, null, 2),
        "success": function (result) {
            
        }
    });
}

function followQuestion(questionId)
{
    var button = $("#" + questionId);
    //var questionId = $(button).attr('data-questionId');
    var textSpan = $(button).find('span');
    var icon = $(button).find('i.fa');
    $(icon).addClass('fa-circle-o-notch fa-spin');
    
    if($(button).hasClass('active'))
    {
        updateFollowQuestion(false, questionId, function(){
            $(button).removeClass('active');
            $(textSpan).text('Follow');
            $(icon).removeClass('fa-check fa-circle-o-notch fa-spin').addClass('fa-plus-circle');
        });
    }
    else
    { 
        updateFollowQuestion(true, questionId, function(){
            $(button).addClass('active');
            $(textSpan).text('Following');
            $(icon).removeClass('fa-plus-circle fa-circle-o-notch fa-spin').addClass('fa-check');
        });
    }
}

function updateUpVote(answerId, success) {
    var upvoteButton = $(".upvote-ul a[data-answerId='" + answerId + "']");    

    if ($(upvoteButton).hasClass('active')) {
        $(upvoteButton).attr('tutle', 'You have already upvoted this answer.');
        return;
    }

    var textSpan = $(upvoteButton).find('span');
    var icon = $(upvoteButton).find('i.fa');
    $(icon).addClass('fa-circle-o-notch fa-spin');

    scAjax({
        "url": "answers/UpdateUpVoteCount",
        "type": "POST",
        "data": JSON.stringify({
            "AnswerID": answerId
        }),
        "success": function (result) {
            if (!result) {
                return;
            }
            if (success && typeof success === 'function') {
                success();
            }

            $(upvoteButton).addClass('active');
            $(textSpan).text('Upvoted');
            $(icon).removeClass('fa-arrow-up fa-circle-o-notch fa-spin').addClass('fa-thumbs-up');
        }
    });
}
