// javascript being used in the question details page
var advancedEditor;
// current logged-in user
var userId = null;
// we will keep the default users that can answer this question and we will retrieve it during page load
var userListAskToAnswer;

// default user profile image path, when user signup in our site we assign this image as profile image by default
var profiledefaultimage = '/Content/images/profile-image.jpg';

// get the current logged-in user details
var userDetails = localStorage.getItem("SC_Session_UserDetails");
var userInfo = $.parseJSON(userDetails);
if (userInfo != null) {
    userId = userInfo.id;
    profiledefaultimage = userInfo.profileImageURL;
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

    if ($('.write-answer .text-wrapper .editor-container').length > 0) {
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
        },
        error: function (xhr, ajaxOptions, thrownError) {
            if (xhr.status == 404) {
                window.location.href = "/errors/Http404";
            }
        }

    });

    $('.btn-to-login').click(function () {
        window.location = "/account/login";
    });
});

function RedirectToLogin() {
    window.location = "/account/login";
}

function createQuestion(question) {
    document.title = question.title + " - ShibpurHub";

    var htmlItem = $('div.item.question.hide').clone().removeClass('hide');

    var creatorImage = $(htmlItem).find('.post-creator-image');
    if (question.userProfileImage != profiledefaultimage)
        $(creatorImage).attr("href", "/Account/Profile?userId" + question.userId).css('background-image', "url(http://i.imgur.com/" + question.userProfileImage + ")");
    else
        $(creatorImage).attr("href", "/Account/Profile?userId" + question.userId).css('background-image', "url(" + question.userProfileImage + ")");

    $(htmlItem).find('a.name-link').text(question.displayName).attr("href", "/Account/Profile?userId=" + question.userId);

    var titleEl = $(htmlItem).find('h2.title a').addClass('top');
    $(titleEl).text(question.title);

    var descriptionEl = $(htmlItem).find('div.post-description p').addClass('top');
    $(descriptionEl).html(question.description);


    $(htmlItem).find('p.designation').text(question.careerDetail);
    $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");

    if (question.viewCount && question.viewCount > 1) {
        $(htmlItem).find('span.view-count').text(question.viewCount + " views");
    }
    $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(question.postedOnUtc));

    var followButton = $(htmlItem).find('.follow-ul a.thumbs');
    $(followButton).attr({ 'data-questionId': question.questionId, 'id': question.questionId });

    var questionIds = [];
    questionIds.push(question.questionId);


    if (!question.isAskedByMe) {
        $(htmlItem).find('.follow-ul').show();
    }
    else {
        $('.markanswer-ul').show();
    }

    var categoryArr = [];
    var tagListUL = $(htmlItem).find('.tagsList');
    $(question.categories).each(function (i, e) {
        var anchor = $('<a>').attr('href', "/Feed/FeedByCategory?category=" + e).text(e);
        var li = $('<li>').append(anchor);
        $(tagListUL).append(li);
        categoryArr.push(e);
    });

    $("div.question-container").append(htmlItem);

    $(followButton).click(function (event) {
        event.preventDefault();
        followQuestion(question.questionId);
    });

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
    $('.submit-answer-container').show();
    if (advancedEditor) {
        advancedEditor.on('text-change', function (delta, source) {
            if (!$('.toolbar-container').is(":visible")) {
                $('.toolbar-container').show("slide", { "direction": "down" });
            }
        });
    }

    $('.btn-submit-answer').click(function () {
        saveAnswer();
    });

    // show the 'ask to answer module'
    $('#userToAnswer').show();

    var answers = question.answers;
    if (!answers) {
        return;
    }

    var answerIds = [];
    $(answers).each(function (index, answer) {
        answerIds.push(answer.answerId);
        createAnswer(answer);
    });

    updateQnAStatus(questionIds, answerIds);

    if (answerId && answerId != "") {
        $('html,body').animate({ scrollTop: $('#' + answerId).offset().top - 70 }, 'fast');
    }

    if (question.userId == userId) {
        $('.spanEditQuestion').removeClass('hide');
    }
    $('.spanEditQuestion').magnificPopup({
        delegate: 'a',
        removalDelay: 500, //delay removal by X to allow out-animation
        callbacks: {
            beforeOpen: function () {
                this.st.mainClass = this.st.el.attr('data-effect');
                $('#btn_Edit_Question').attr('data-question-id', question.questionId);
                setUpEditQuestion();
            }
        },
        midClick: true // allow opening popup on middle mouse click. Always set it to true if you don't provide alternative source.
    });
}


function createAnswer(answer) {
    var htmlItem = $('div.item.answer.hide').clone().removeClass('hide').attr('id', answer.answerId);

    var creatorImage = $(htmlItem).find('.post-creator-image');
    if (answer.userProfileImage != profiledefaultimage)
        $(creatorImage).attr("href", "/Account/Profile?userId" + answer.userId).css('background-image', "url(http://i.imgur.com/" + answer.userProfileImage + ")");
    else
        $(creatorImage).attr("href", "/Account/Profile?userId" + answer.userId).css('background-image', "url(" + answer.userProfileImage + ")");


    $(htmlItem).find('a.name-link').text(answer.displayName).attr("href", "/Account/Profile?userId=" + answer.userId);

    var answerText = getEmojiedString(answer.answerText);
    $(htmlItem).find('div.post-description p').html(answerText);
    $(htmlItem).find('p.designation').text(answer.careerDetail);
    $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");

    $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(answer.postedOnUtc));

    var upvoteButton = $(htmlItem).find('.upvote-ul a.thumbs');
    $(upvoteButton).attr({ 'data-answerId': answer.answerId, 'id': answer.answerId });

    var markAsAnswerButton = $(htmlItem).find('.markanswer-ul a.thumbs');
    $(markAsAnswerButton).attr({ 'data-answerId': answer.answerId });

    $("div.answer-container").append(htmlItem);

    $(upvoteButton).click(function (event) {
        event.preventDefault();
        updateUpVote(answer.answerId);
    });

    $(markAsAnswerButton).click(function (event) {
        event.preventDefault();
        markAsAnswer(answer.answerId);
    });

    var deleteButton = $(htmlItem).find('.delete-ul a.thumbs');
    if (deleteButton.length > 0) {
        $(deleteButton).attr({ 'data-delete-answerId': answer.answerId });
        $(deleteButton).click(function (event) {
            event.preventDefault();
            deleteAnswer(answer.answerId);
        });
    }

    $('.myimg').css('background-image', "url(" + profiledefaultimage + ")");

    var comments = answer.comments;
    if (!comments) {
        return;
    }

    $(comments).each(function (index, comment) {
        createComment(comment);
    });

    $('#' + answer.answerId).find('textarea.write-comment').bind('keyup', function (event) {
        if (event.keyCode === 13) {
            var commentText = $(this).val();
            if (!commentText || commentText == "") {
                return;
            }

            var postComment = {};
            postComment.commentText = commentText;
            postComment.answerId = answer.answerId;
            postComment.userId = userInfo.id;
            postComment.userProfileImage = userInfo.profileImageURL;
            postComment.displayName = userInfo.firstName + " " + userInfo.lastName;
            postComment.postedOnUtc = new Date();

            saveComment(postComment);

            $(this).val("");
        }
    });


}

function createComment(comment) {
    var answerId = comment.answerId;
    var htmlItem = $('#' + answerId + " .comments .post-comment.hide").clone().removeClass('hide');

    if (comment.userProfileImage != profiledefaultimage)
        $(htmlItem).find('.img-container').css('background-image', "url(http://i.imgur.com/" + comment.userProfileImage + ")").attr("href", "/Account/Profile?userId=" + comment.userId);
    else
        $(htmlItem).find('.img-container').css('background-image', "url(" + comment.userProfileImage + ")").attr("href", "/Account/Profile?userId=" + comment.userId);

    var commentText = getEmojiedString(comment.commentText);
    $(htmlItem).find('.commentContent p').html(commentText);
    $(htmlItem).find('a.comment-author-name').text(comment.displayName).attr("href", "/Account/Profile?userId=" + comment.userId);

    $(htmlItem).find('span.comment-time').text(getDateFormattedByMonthYear(comment.postedOnUtc));

    $('#' + answerId + " .comments").append(htmlItem);
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

function showAskToAnswer(question) {
    if ($('#userToAnswer').length == 0) {
        return;
    }

    // show the suggested user who can answer this question using elastic search
    scAjax({
        "url": "search/SearchUsersAskToAnswer",
        "data": { "searchTerm": question.categories.toString(), "questionId": questionID },
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
                    // don't show the user who is currently logged-in
                    if (val.id != userId) {
                        if (val.profileImageURL != profiledefaultimage) {
                            // form the smaller imgur image by adding 's' before '.jpg'
                            if (val.profileImageURL.charAt(val.profileImageURL.indexOf('.jpg') - 1) != 's') {
                                val.profileImageURL = val.profileImageURL.replace('.jpg', 's.jpg');
                            }
                            if (userDetails) {
                                //asktoansweruserlisthtml += "<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>";
                                $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>");
                            }
                            else {
                                //asktoansweruserlisthtml += "<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>";
                                $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>");
                            }
                        }
                        else {
                            if (userDetails) {
                                //asktoansweruserlisthtml += "<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>";
                                $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>");
                            }
                            else {
                                //asktoansweruserlisthtml += "<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>";
                                $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>");
                            }
                        }
                        // check if this user already requested to answer this question, if that true then keep the 'Ask' button disabled
                        $.ajax({
                            "url": "/api/asktoanswer/GetAskToAnswer",
                            "data": { "questionId": questionID, "userId": val.id },
                            "success": function (result) {
                                if (result != null) {
                                    $('button[id$="' + val.id + '"]:first').text("Already Asked");
                                    $('button[id$="' + val.id + '"]:first').attr('disabled', 'disabled');
                                    $(asktoansweruserlisthtml).find('button[id$="' + val.id + '"]:first').text("Already Asked");
                                }
                                //get the response rate for each user
                                var responseRate;
                                $.ajax({
                                    "url": "/api/asktoanswer/GetResponseRate",
                                    "data": { "userId": val.id },
                                    "success": function (rRate) {
                                        //wantedanswersuggestion
                                        responseRate = rRate;
                                        $('span[id$="' + val.id + '"]:first').text("Response rate: " + rRate);

                                        // hide the loading message
                                        $('#loadingasktoanswer').hide();
                                    }
                                });
                            },
                            "error": function (XMLHttpRequest, textStatus, errorThrown) {
                                handleAjaxError(XMLHttpRequest, textStatus, errorThrown)
                            }
                        });
                    }
                });
            }
        },
        "error": function (XMLHttpRequest, textStatus, errorThrown) {
            handleAjaxError(XMLHttpRequest, textStatus, errorThrown)
        }
    });
}

function handleAjaxError(XMLHttpRequest, textStatus, errorThrown) {
    var errorText = XMLHttpRequest.responseText.replace('{"message":"', " ").replace('"}', " ");
    // toastr notification
    Command: toastr["error"](errorText)
    // hide the loading message
    $('#loadingdiv').hide();

    // hide the loading message in ask to answer module
    $('#loadingasktoanswer').hide();
}

function scrollToADivOnPageLoad() {
    var hash = window.location.hash;
    if (hash.length > 0 && $('#' + hash).size() > 0) {
        $('html, body').animate({ scrollTop: $(hash).offset().top - 70 });
    }
}

function enableOrDisableSubmitAnswer(enable) {
    if (enable) {
        // enable the button
        $('.btn-submit-answer').prop('disabled', false).removeClass("disabled");

        // remove the loading class from save button
        $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
        $('.btn-submit-answer > span').text('Submit Answer');
    }
    else {
        // disable the button
        $('.btn-submit-answer').prop('disabled', true).addClass("disabled");

        // add spinner animation in the save button and change the text to 'Saving..'
        $('.btn-submit-answer > i').addClass('fa fa-circle-o-notch fa-spin');
        $('.btn-submit-answer > span').text(' Saving...');
    }
}

function saveAnswer() {
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

    var answerObject = { "AnswerText": answer, "QuestionId": questionID };

    advancedEditor.setHTML("");

    scAjax({
        "url": "Answers/PostAnswer",
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

            $('html, body').animate({ scrollTop: $("#" + answer.answerId).offset().top - 70 });
        }
    });
}

function saveComment(comment) {
    if (!comment) {
        return;
    }

    createComment(comment);
    var data = { "CommentText": $.trim(comment.commentText), "AnswerId": comment.answerId };
    scAjax({
        "url": "comments/postcomment",
        "type": "POST",
        "data": JSON.stringify(data, null, 2),
        "success": function (result) {

        }
    });
}

function setUpEditQuestion() {
    var rtContainer = $('.editQuesDiv .richtextbox-container');
    $(rtContainer).find('.control-label').text('Question');
    $('.editQuesDiv #txt_question_edit').val($('h2.title a.top').text());

    qustionRTBoxEditor = new Quill('.editQuesDiv .richtextbox-container .text-wrapper .editor-container', {
        modules: {
            'authorship': {
                authorId: 'advanced',
                enabled: true
            },
            'toolbar': {
                container: '.editQuesDiv .richtextbox-container .text-wrapper .toolbar-container'
            },
            'link-tooltip': true,
            'image-tooltip': true,
            'multi-cursor': true
        },
        styles: false,
        theme: 'snow'
    });

    qustionRTBoxEditor.setHTML($('div.post-description p.top').html());

    $('#btn_Edit_Question').click(function () {
        $.magnificPopup.close();
        var newTitle = $('#txt_question_edit').val();
        var newDescription = qustionRTBoxEditor.getHTML();
        var data = {
            "questionId": $(this).attr('data-question-id'),
            "title": newTitle,
            "description": newDescription,
            "categories": categoryArr
        };

        scAjax({
            "url": "questions/EditQuestion",
            "type": "POST",
            "data": JSON.stringify(data, null, 2),
            "success": function (result) {
                if (!result) {
                    return;
                }
            }
        });

        $('h2.title a.top').text(newTitle);
        $('div.post-description p.top').html(newDescription);

    });
}

function deleteAnswer(id) {
    var answerObject = { "AnswerId": id, "QuestionId": questionID };
    scAjax({
        "url": "answers/DeleteAnswer",
        "type": "POST",
        "data": JSON.stringify(answerObject),
        "success": function (result) {
            if (!result) {
                return;
            }

            $('.answer-container #' + id).remove();
        }
    });
}

// This method will execute when user will type user details in the 'Ask To Answer' serach box
$(function () {
    $('#usernametext').keyup(function (event) {
        var searchValue = $(this).val();
        $('#loadingasktoanswer').show();
        setTimeout(function () {
            if (searchValue == $('#usernametext').val() && searchValue != null && searchValue != "") {
                //alert(searchValue);
                scAjax({
                    "url": "search/SearchUsersByNameEmail",
                    "data": { "userDetails": searchValue },
                    "success": function (result) {
                        if (!result) {
                            return;
                        }

                        // clear the previous users
                        $('#userlistasktoanswer').empty();

                        if (result.length == 0) {
                            $('#userlistasktoanswer').append("<span>Sorry no matching profile available</span>");
                            // hide the loading message
                            $('#loadingasktoanswer').hide();
                        }
                        else {
                            // update the div with the user data received from the API
                            jQuery.each(result, function (i, val) {
                                if (val.profileImageURL != profiledefaultimage) {
                                    // form the smaller imgur image by adding 's' before '.jpg'
                                    if (val.profileImageURL.charAt(val.profileImageURL.indexOf('.jpg') - 1) != 's') {
                                        val.profileImageURL = val.profileImageURL.replace('.jpg', 's.jpg');
                                    }
                                    if (userDetails)
                                        $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>");
                                    else
                                        $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>");
                                }
                                else
                                    if (userDetails)
                                        $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>");
                                    else
                                        $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>");
                                // check if this user already requested to answer this question, if that true then keep the 'Ask' button disabled
                                scAjax({
                                    "url": "asktoanswer/GetAskToAnswer",
                                    "data": { "questionId": questionID, "userId": val.id },
                                    "success": function (result) {
                                        if (result != null) {
                                            $('button[id$="' + val.id + '"]:first').text("Already Asked");
                                            $('button[id$="' + val.id + '"]:first').attr('disabled', 'disabled');
                                        }
                                        //get the response rate for each user
                                        var responseRate;
                                        scAjax({
                                            "url": "asktoanswer/GetResponseRate",
                                            "data": { "userId": val.id },
                                            "success": function (rRate) {
                                                //wantedanswersuggestion
                                                responseRate = rRate;
                                                $('span[id$="' + val.id + '"]:first').text("Response rate: " + rRate);

                                                // hide the loading message
                                                $('#loadingasktoanswer').hide();
                                            }
                                        });
                                    }
                                });
                            });
                        }
                    }
                });
            }
            else if (searchValue == '') {
                // user have cleared all text from the text field so load all previous users
                // clear the existing users
                $('#userlistasktoanswer').empty();

                if (userListAskToAnswer.length == 0) {
                    $('#userlistasktoanswer').append("<span>Sorry we haven't found anyone to answer this question. Use above search box to request someone to answer this question</span>");
                    // hide the loading message
                    $('#loadingasktoanswer').hide();
                }
                else {
                    $('#loadingasktoanswer').hide();
                    // update the div with the user data received from the API
                    jQuery.each(userListAskToAnswer, function (i, val) {
                        // don't show the user who is currently logged-in
                        if (val.id != userId) {
                            // check if this user already requested to answer this question, if that true then keep the 'Ask' button disabled
                            if (val.profileImageURL != profiledefaultimage) {
                                // form the smaller imgur image by adding 's' before '.jpg'
                                if (val.profileImageURL.charAt(val.profileImageURL.indexOf('.jpg') - 1) != 's') {
                                    val.profileImageURL = val.profileImageURL.replace('.jpg', 's.jpg');
                                }
                                if (userDetails)
                                    $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>");
                                else
                                    $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>");
                            }
                            else
                                if (userDetails)
                                    $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-xs' onclick='SendNotification(this)'>Ask</button></div></div>");
                                else
                                    $('#userlistasktoanswer').append("<div class='wantedanswersuggestion col-xs-12' id='" + val.id + "'><div class='userContainer'><div class='profileimage col-md-3 col-xs-4'><img class='avatar avatarasktoanswer center-block' width='60' height='60' src='" + profiledefaultimage + "' /></div><div class='userinfo col-md-9 col-xs-8'><span class='name'><a class='userName' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br /><span id='reputationcount' style='font-size:12px'>Reputation: " + val.reputationCount + "</span><br /><span id='" + val.id + "' style='font-size:12px'></span></div></div> <div class='askbuton col-md-12 col-xs-12'> <button type='submit' id='" + val.id + "' data-id='" + val.id + "' class='btn btn-primary btn-to-login btn-xs' onclick='RedirectToLogin()'>Login To Ask</button></div></div>");
                            // check if this user already requested to answer this question, if that true then keep the 'Ask' button disabled
                            scAjax({
                                "url": "asktoanswer/GetAskToAnswer",
                                "data": { "questionId": questionID, "userId": val.id },
                                "success": function (result) {
                                    if (result != null) {
                                        $('button[id$="' + val.id + '"]:first').text("Already Asked");
                                        $('button[id$="' + val.id + '"]:first').attr('disabled', 'disabled');
                                    }
                                    //get the response rate for each user
                                    var responseRate;
                                    scAjax({
                                        "url": "asktoanswer/GetResponseRate",
                                        "data": { "userId": val.id },
                                        "success": function (rRate) {
                                            //wantedanswersuggestion
                                            responseRate = rRate;
                                            $('span[id$="' + val.id + '"]:first').text("Response rate: " + rRate);

                                            // hide the loading message
                                            $('#loadingasktoanswer').hide();
                                        }
                                    });
                                }
                            });
                        }
                    });
                }
            }
        }, 400);
    });
});