var advancedEditor;
// current logged-in user
var userId = null;
// we will keep the default users that can answer this question and we will retrieve it during page load
var userListAskToAnswer;

// get the current logged-in user details
var userDetails = localStorage.getItem("SC_Session_UserDetails");
var userInfo = $.parseJSON(userDetails);
if (userInfo != null) {
    userId = userInfo.id;
}

//used to reference rich textbox editor for question edit
var qustionRTBoxEditor;
var categoryArr = [];
$(document).ready(function () {
    scrollToADivOnPageLoad();
    // hide the submit answer rich text control
    $('.wirte-answer').hide();

    // hide the right column modules, we will enable inside respective the partial view once receive the server response
    $('.tagcontainermaindiv').hide();
    $('.popularquestioncontainerdiv').hide();

    // hide 'ask to answer' module
    $('#userToAnswer').hide();

    if($('.wirte-answer .text-wrapper .editor-container').length > 0)
    {
        advancedEditor = new Quill('.wirte-answer .text-wrapper .editor-container', {
            modules: {
                'authorship': {
                    authorId: 'advanced',
                    enabled: true
                },
                'toolbar': {
                    container: '.wirte-answer .text-wrapper .toolbar-container'
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
    $(".followquestionbutton").attr("id", questionID);
    $(".followquestionbutton").attr('style', 'background-color:#E4EDF4;');
    
    scAjax({
        "url": "questions/GetQuestion",
        "data": { "questionId": questionID },
        "success": function (result) {
            if (!result) {
                return;
            }
            // hide the loading div
            $('#loadingdiv').hide();
            var question = result;
            $('#details').show();
            
            createQuestion(question);
        }
        
    });
	
});

function createQuestion(question)
{
    document.title = question.title + " - ShibpurHub";
    
    var htmlItem = $('div.item.question.hide').clone().removeClass('hide');

    var creatorImage = $(htmlItem).find('.post-creator-image');
    $(creatorImage).attr("href", "/Account/Profile?userId" + question.userId).css('background-image', "url(http://i.imgur.com/" + question.userProfileImage + ")");

    $(htmlItem).find('a.name-link').text(question.displayName).attr("href", "/Account/Profile?userId=" + question.userId);
    
    $(htmlItem).find('h2.title a').text(question.title);
    
    $(htmlItem).find('div.post-description p').html(question.description);
    $(htmlItem).find('p.designation').text(question.careerDetail);
    $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");

    if (question.viewCount && question.viewCount > 1)
    {
        $(htmlItem).find('span.view-count').text(question.viewCount + " views");
    }
    $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(question.postedOnUtc));
    
    $("div.feed-list").append(htmlItem);
    
    showAskToAnswer(question);
    
    scAjax({
        "url": "questions/IncrementViewCount",
        "type": "POST",
        "data": JSON.stringify({ "QuestionID": questionID }),
        "success": function (result) {
        }
    });
    
    // hide the loading symbol for the answers
    $('#loadingdiv').hide();

    // show the submit answer rich text control
    $('.wirte-answer').show();

    // show the 'ask to answer module'
    $('#userToAnswer').show();
        
    var answers = question.answers;
    if (!answers) {
        return;
    }
    
    $(answers).each(function (index, answer) {
        createAnswer(answer);
    });
}

function createAnswer(answer)
{
    var htmlItem = $('div.item.answer.hide').clone().removeClass('hide');

    var creatorImage = $(htmlItem).find('.post-creator-image');
    $(creatorImage).attr("href", "/Account/Profile?userId" + answer.userId).css('background-image', "url(http://i.imgur.com/" + answer.userProfileImage + ")");

    $(htmlItem).find('a.name-link').text(answer.displayName).attr("href", "/Account/Profile?userId=" + answer.userId);
    
    $(htmlItem).find('div.post-description p').html(answer.answerText);
    $(htmlItem).find('p.designation').text(answer.careerDetail);
    $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");
    
    $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(answer.postedOnUtc));
    
    $("div.feed-list").append(htmlItem);
}

function showAskToAnswer(question)
{
    // show the suggested user who can answer this question using elastic search
    scAjax({
        "url": "search/SearchUsers",
        "data": { "searchTerm": question.categories.toString() },
        "success": function (result) {
            if (!result) {
                return;
            }

            userListAskToAnswer = result;

            if (result.length == 0) {
                $('#userlistasktoanswer').append("<span>Sorry we have not found anyone to answer this question. Use above search box to request someone to answer this question</span>");
                // hide the loading message
                $('#loadingasktoanswer').hide();
            }
            else {
                // update the div with the user data received from the API
                $.each(result, function (i, val) {
                    if (val.profileImageURL != null) {
                        // form the smaller imgur image by adding 's' before '.jpg'
                        if (val.profileImageURL.charAt(val.profileImageURL.indexOf('.jpg') - 1) != 's') {
                            val.profileImageURL = val.profileImageURL.replace('.jpg', 's.jpg');
                        }
                        $('#userlistasktoanswer').append("<div class='wantedanswersuggestion' id='" + val.id + "'><div class='profileimage col-md-2 col-xs-3'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "'></img></div><div class='userinfo col-md-8 col-xs-8' style='float:left;'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br/><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br/><span id='" + val.id + "' style='font-size:12px'></span></div><div class='askbuton'><a id='" + val.id + "' data-id='" + val.id + "'class='btn btn-primary btn-xs' style='float:right;' onclick='SendNotification(this)'>Ask</a></div></div><hr id='smallseparator'/>");
                    }
                    else
                        $('#userlistasktoanswer').append("<div class='wantedanswersuggestion' id='" + val.id + "'><div class='profileimage col-md-2 col-xs-3'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='/Content/images/profile-image.jpg'></img></div><div class='userinfo col-md-8 col-xs-8' style='float:left;'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br/><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br/><span id='" + val.id + "' style='font-size:12px'></span></div><div class='askbuton'><a id='" + val.id + "' data-id='" + val.id + "'class='btn btn-primary btn-xs' style='float:right;' onclick='SendNotification(this)'>Ask</a></div></div><hr id='smallseparator'/>");
                    // check if this user already requested to answer this question, if that true then keep the 'Ask' button disabled
                    scAjax({
                        "url": "asktoanswer/GetAskToAnswer",
                        "data": { "questionId": questionID, "userId": val.id },
                        "success": function (result) {
                            if (result != null) {
                                $('a[id$="' + val.id + '"]:first').text("Already Asked");
                                $('a[id$="' + val.id + '"]:first').attr('disabled', 'disabled');
                            }
                            //get the response rate for each user
                            var responseRate;
                            scAjax({
                                "url": "asktoanswer/GetResponseRate",
                                "data": { "userId": val.id },
                                "success": function (rRate) {
                                    //wantedanswersuggestion
                                    responseRate = rRate;
                                    $('span[id$="' + val.id + '"]:first').text("Response Rate: " + rRate);

                                    // hide the loading message
                                    $('#loadingasktoanswer').hide();
                                }
                            });
                        },
                        "error": function (XMLHttpRequest, textStatus, errorThrown) {
                                    handleAjaxError(XMLHttpRequest, textStatus, errorThrown)
                                }
                    });
                });
            }
        },
        "error": function (XMLHttpRequest, textStatus, errorThrown) {
            handleAjaxError(XMLHttpRequest, textStatus, errorThrown)
        }
    });
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