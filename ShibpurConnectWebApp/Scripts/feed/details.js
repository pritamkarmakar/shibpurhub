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

            // check if user following this question
            scAjax({
                "url": "questions/GetQuestionFollowers?questionId=" + questionID,
                "type": "GET",
                "success": function (result) {
                    // disable the follow button for the user who posted the original question
                    if (userId == question.userId) {
                        $(".followquestionbutton > i").removeClass();
                        $(".followquestionbutton").prop('disabled', true);
                        $(".followquestionbutton > span").text(" Follow");
                    }
                    else if (result != null && result.indexOf(userId) > -1) {
                        $(".followquestionbutton > i").addClass('fa fa-check-circle');
                        $(".followquestionbutton > span").text(" Following");
                    }
                    // add the count of the total followers of this question as an attribute
                    if(result != null) {
                        if (result.length > 0)
                            $(".followquestionbutton > span").append("&nbsp;&nbsp;<span class='badge'>" + result.length + "</span>");
                        $(".followquestionbutton").attr("count", result.length);
                    }

                }
            });

            // set page title
            document.title = question.title + " - ShibpurHub";
            $('.header h2').text(question.title);
            $('.header h2').append("<hr/>");
            $('.description').html(question.description.replace("<img", "<img style='max-width:730px;'"));

            var utcDate = new Date(result.postedOnUtc);
            var dateString = getMonth(utcDate.getMonth().toString()) + " " + utcDate.getDate() + ", " + utcDate.getFullYear();



            // get reputation of the user who asked this question
            var reputation;
            $.ajax({
                url: SERVER + "profile/getuserinfo",
                type: "GET",
                dataType: "json",
                data: { "userId": question.userId },
                contentType: "application/json",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("Authorization", "Bearer UEkvwQmR0EOsdGd-9y_bqizgm7F6_qvHSy4tyeKGY9Kb93h2ASjLyvW4BdcuB9cGgt-PcACQAy7WBycNbplGPXtHI4_r4YOLjDeXcK6S4Cswk2SQ5R_51zV1cmytfczRkGM6RnRWKmH_yiIz-LPO6tByk28wkLDeeaLDnoiy6Zg6S5zk9uZZtrreZHRx3nl4SiCD3QKLtXqn7bGYGFF71D745YBAeAjNityNKpyum7pBnQSYpL5qYZHCjI3-94bT");
                },
                success: function (userinfo) {
                    if (!userinfo) {
                        return;
                    }
                    $('#questionaskedby').show();
                    $('#btn_Edit_Question').attr('data-question-id', questionID);
                    // form the smaller imgur image by adding 's' before '.jpg'
                    if (question.userProfileImage.charAt(question.userProfileImage.indexOf('.jpg') - 1) != 's') {
                        question.userProfileImage = question.userProfileImage.replace('.jpg', 's.jpg');
                    }

                    $('#questionaskedby').append("<div class='profileimage' style='float:left;'><img class='avatar' width='60' height='60' src='" + IMGURPATH + question.userProfileImage + "'></img></div><div class='userinfo' style='float:left;'><span class='name'><a class='questionaskedbyuser' style='margin-left:15px;font-size:13px;' userid='" + userinfo.id + "'href='/Account/Profile?userId=" + userinfo.id + "'>" + userinfo.firstName + " " + userinfo.lastName + "</a></span><br/><span style='margin-left:15px;font-size:10px'>reputation: " + userinfo.reputationCount + "</span></div><div class='stat'><span class='date'>asked: " + dateString + "</span></div>");
                }
            });
            //$('#questionaskedby').append("Asked By: <a href='" + window.location.origin + "/Account/Profile?useremail=" + question.userEmail + "'>" + question.displayName + "</a>");

            isAskedByMe = question.isAskedByMe;
            // if the logged-in user and the user who asked this question is same then show the question edit option
            if(isLoggedInUserId(question.userId))
            {
                $('#spanEditQuestion').show();
                setUpEditQuestion();
            }

            getAnswers(question.answers);
            // attach the categories
            $(question.categories).each(function (i, category) {
                var tagAnchor = $('<a>').addClass('post-tag').text(category).attr('href', "/Feed/FeedByCategory?category=" + category);
                $('div.tags').append(tagAnchor);
                categoryArr.push(category);
            });

            // show the suggested user who can answer this question using elastic search
            scAjax({
                "url": "search/SearchUsers",
                "data": { "searchTerm": result.categories.toString() },
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
                        jQuery.each(result, function (i, val) {
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
                                }
                            });

                        });
                    }
                },
                "error": function (XMLHttpRequest, textStatus, errorThrown) {
                    var errorText = XMLHttpRequest.responseText.replace('{"message":"', " ").replace('"}', " ");
                    // toastr notification
                    Command: toastr["error"](errorText)
                    // hide the loading message
                    $('#loadingdiv').hide();

                    // hide the loading message in ask to answer module
                    $('#loadingasktoanswer').hide();
                }
            });
        },
        "error": function (XMLHttpRequest, textStatus, errorThrown) {
            if (XMLHttpRequest.status == "401")
                window.location.href = "/account/login";
            // if it is 404 response then redirect to 404 page
            if (XMLHttpRequest.status == "404") {
                window.location.href = "/errors/Http404";
            }
            // toastr notification
            Command: toastr["error"]("Failed to load the question")
            // hide the loading message
            $('#loadingdiv').hide();
        }
    });

    scAjax({
        "url": "questions/IncrementViewCount",
        "type": "POST",
        "data": JSON.stringify({ "QuestionID": questionID }),
        "success": function (result) {
        }
    });

    //getAnswers();

    $('.btn-submit-answer').click(function () {
        saveAnswer();
    });

    $('textarea').focus(function () {
        var textarea = $(this);
        $('.comment-form').addClass('active');
        var parentDiv = $(textarea).parent();
        $(parentDiv).removeClass("col-md-8").addClass("col-md-12");
        //$(parentDiv).next().addClass("col-md-offset-9");
    });

    $('.editQuesAnchor').click(function(){
        $('.editQuesDiv').slideToggle();
        $('.question-container').slideToggle();
        $('#txt_question_edit').focus();
        return false;
    });
});

// function to send notification to the specific user from 'Ask to Answer' module
function SendNotification(elm) {
    if(userInfo){
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

function saveAnswer() {
    // hide the error div
    $('#errormessage').empty();
    $('#errormessage').hide();

    // disable the button
    $('.btn-submit-answer').prop('disabled', true);

    // add spinner animation in the save button and change the text to 'Saving..'
    $('.btn-submit-answer > i').addClass('fa fa-circle-o-notch fa-spin');
    $('.btn-submit-answer > span').text(' Saving...');

    jQuery.support.cors = true;
    var userDetails = localStorage.getItem("SC_Session_UserDetails");

    if (!userDetails) {
        //TO-DO: Handle if userdetail is not available in Session Storage
        window.location.href = "/account/login";
    }

    var answer = advancedEditor.getHTML();

    // verify if answer text is empty
    if (!answer || $.trim(advancedEditor.getText()) == "") {
        // enable the button
        $('.btn-submit-answer').prop('disabled', false);

        // remove the loading class from save button
        $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
        $('.btn-submit-answer > span').text('Submit Answer');

        // parse the error json
        $('#errormessage').show();
        $('#errormessage').append("<p>Answer can't be blank</p>");

        return;
    }
    // verify whether length of answer text is less than 30
    //if ($.trim(advancedEditor.getText()).length < 30) {
    //    // enable the button
    //    $('.btn-submit-answer').prop('disabled', false);

    //    // remove the loading class from save button
    //    $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
    //    $('.btn-submit-answer > span').text('Submit Answer');

    //    // toastr error notification
    //    Command: toastr["error"]("Answer should be more than 30 characters long")
    //    return;
    //}

    var userInfo = $.parseJSON(userDetails);

    //var SERVER = "/api/";
    var userid = userInfo.id;

    //var token = localStorage.getItem("TOKEN") || "PTLp2jwqIgLZmDBqjtADmHdtHmKpEmy-KtrdrocIBR5dz2k0uHNdgLdeqm6bFODmjUxsgNkcr0LI2UGaT6xve1gxPaHjLAyqneD69pKNIVwpo6mGtiaemzfJFVgV_FS_Q6TzA5InNb16tGCu3VNoYdqQEJu-jLiX3uN3FNAjAUKElpYZbjk5fYZ7ZKZ3T0w7utu0rbNI2BWuxo4HADZ4wrvI1J_-8qnd4Sq1kYWBkdPFeqDO2HQ8Io-IiKSDmnFS";
    var answerObject = { "AnswerText": answer, "QuestionId": questionID };

    advancedEditor.setHTML("");

    //This is for Edit
    if($('.btn-submit-answer').attr('data-answer-id'))
    {
        var editAnswerId = $('.btn-submit-answer').attr('data-answer-id');
        $('.btn-submit-answer').removeAttr('data-answer-id');
        $('#'+ editAnswerId).html(answer);
        $('html,body').animate({scrollTop: $('#'+ editAnswerId).offset().top - 70},'slow');

        answerObject.AnswerId = editAnswerId;
        //Save the edit
        scAjax({
            "url": "Answers/EditAnswer",
            "type": "POST",
            "data": JSON.stringify(answerObject),
            "success": function (result, data) {
                if (!result) {
                    return;
                }
                // enable the button
                $('.btn-submit-answer').prop('disabled', false);

                // remove the loading class from save button
                $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
                $('.btn-submit-answer > span').text('Submit Answer');
            }
        });

        return;
    }

    scAjax({
        "url": "Answers/PostAnswer",
        "type": "POST",
        "data": JSON.stringify(answerObject),
        "success": function (result, data) {
            if (!result) {
                return;
            }

            // enable the button
            $('.btn-submit-answer').prop('disabled', false);

            // remove the loading class from save button
            $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
            $('.btn-submit-answer > span').text('Submit Answer');


            var utcDate = new Date(result.postedOnUtc);
            var dateString = getMonth(utcDate.getMonth().toString()) + " " + utcDate.getDate() + ", " + utcDate.getFullYear() + "at" + utcDate.getHours() + ":" + utcDate.getMinutes();

            var answerBlock = $('#answerlist li.answer.hide').clone().attr('data-answerid', result.answerId);
            $(answerBlock).removeClass('hide');
            // add fixed width if user add any image as part of the answer
            $(answerBlock).find('.text').html(result.answerText.replace("<img", "<img style='max-width:530px;'"));
            $(answerBlock).find('.userName').text(result.displayName);
            $(answerBlock).find('.date').text(dateString);
            $(answerBlock).find('.upvote-span').addClass('disabled');

            var editAnswerSpan = $(answerBlock).find('.edit-answer-span');
            $(editAnswerSpan).show();
            $(editAnswerSpan).find('.edit-answer-anchor').click(function(){
                handleEditAnswerClick({"answerText": result.answerText, "answerId": result.answerId});
            });

            $(answerBlock).find('.answer-comments').hide();
            $(answerBlock).find('.answer-comments .add-comment').hide();

            var userNameAnchor = $(answerBlock).find('div.user a.userName');
            $(userNameAnchor).text(userInfo.firstName + " " + userInfo.lastName);
            var userProfileImage = $(answerBlock).find('div.user img.avatar');
            // form the smaller imgur image by adding 's' before '.jpg'
            if (userInfo.profileImageURL.charAt(userInfo.profileImageURL.indexOf('.jpg') - 1) != 's') {
                userInfo.profileImageURL = userInfo.profileImageURL.replace('.jpg', 's.jpg');
            }
            $(userProfileImage).attr('src', IMGURPATH + userInfo.profileImageURL);
            if (result.userEmail != null) {
                var url = $(userNameAnchor).attr('href') + '?userId=' + result.userId;
                $(userNameAnchor).attr('href', url);
            }

            $('#answerlist').append(answerBlock);
            $('.textarea').val('');
            logActivity(2, result.answerId);

            $(answerBlock).find('.anchor-add-comment').unbind("click").bind("click", handleAddComment);
            $(answerBlock).find('.anchor-cancel-comment').unbind('click').bind('click', handleCancelComment);
            $(answerBlock).find('.btn-save-comment').unbind("click").bind("click", handleSaveComment);

            $(answerBlock).find('.comment-anchor').unbind("click").bind("click", handleCommentLinkClick);
            $(answerBlock).find('.upvote-anchor').unbind("click").bind("click", handleUpvote);
        },
        "error": function (XMLHttpRequest, textStatus, errorThrown) {
            var errorText = XMLHttpRequest.responseText.replace('{"message":"', " ").replace('"}', " ");
            $("#errorLabel").text(errorText);

            // enable the button
            $('.btn-submit-answer').prop('disabled', false);

            // remove the loading class from save button
            $('.btn-submit-answer > i').removeClass('fa fa-circle-o-notch fa-spin');
            $('.btn-submit-answer > span').text('Submit Answer');

            // parse the error json
            var errorresponse = jQuery.parseJSON(XMLHttpRequest.responseText);
            $('#errormessage').show();
            $('#errormessage').append("<p>" + errorresponse.modelState['answer.AnswerText'][0] + "</p>");
        }
    });

}

function getComments(answerBlock, answer) {
    if (!answerBlock) {
        return;
    }

    var result = answer.comments;
    if (!result) {
        return;
    }

    // hide the loading symbol for the answers
    $('#loadingdiv').hide();

    // show the submit answer rich text control
    $('.wirte-answer').show();

    // show the 'ask to answer module'
    $('#userToAnswer').show();

    if (result.length == 0) {
        $(answerBlock).find('.answer-comments').hide();
    }

    $(result.reverse()).each(function (i, e) {

        var commentBlock = $(answerBlock).find('.answer-comments .comment-list.hide').clone();
        $(commentBlock).removeClass('hide');
        var commentTextSpan = $(commentBlock).find('.comment-text');
        $(commentTextSpan).find('span').text(e.commentText);
        if(isLoggedInUserId(e.userId))
        {
            var editCommentAnchor = $(commentTextSpan).find('a').show();

            $(editCommentAnchor).click(function(){
                //first hide if any open edit comment
                var commentSection = $('.edit-comment:not(.hide)').parent();
                $(commentSection).find('.edit-comment').remove();
                $(commentSection).children().show();

                $('.comment-box:not(.hide)').hide();
                $('.add-comment:hidden').show();

                var editCommentDiv = $('.edit-comment.hide').clone().removeClass('hide');
                var editCommentInput = $(editCommentDiv).find('.txt_comment');
                $(editCommentInput).val(e.commentText);
                var editCommentButton = $(editCommentDiv).find('.btn-edit-comment');
                $(editCommentButton).attr('data-comment-id', e.commentId);
                $(commentBlock).children().hide();
                $(commentBlock).append(editCommentDiv);

                $(editCommentButton).click(function(){
                    var commentText = $(editCommentInput).val();
                    $(commentTextSpan).find('span').text(commentText);
                    $(commentBlock).children().show();
                    $(commentBlock).find('.edit-comment').remove();

                    var commentObject = {'CommentText': commentText, 'CommentId': $(this).attr('data-comment-id')};

                    scAjax({
                        "url": "comments/editcomment",
                        "type": "POST",
                        "data": JSON.stringify(commentObject)
                    });

                    return false;
                });

                var editCommentCancel = $(editCommentDiv).find('.anchor-cancel-comment');
                $(editCommentCancel).click(function(){
                    $(commentBlock).children().show();
                    $(commentBlock).find('.edit-comment').remove();
                    return false;
                });

                return false;
            });

        }

        $(commentBlock).find('.comment-date').text(getDateFormatted(e.postedOnUtc));
        $(commentBlock).find('.comment-user').text(e.displayName);
        $(commentBlock).find('.comment-user').attr("href", "/Account/Profile?userId=" + e.userId);
        $(answerBlock).find('.answer-comments').prepend(commentBlock);
    });
}

function handleAddComment(event) {
    event.preventDefault();
    var anchor = $(event.target);

    var commentBox = $(anchor).closest('.add-comment').siblings('.comment-box');
    $(commentBox).show();
    $(commentBox).find('input[type=text]').focus();
    $(anchor).parent().hide();
}

function handleCommentLinkClick(event) {
    event.preventDefault();
    var anchor = $(this);
    var commentBox = $(anchor).closest('li.answer').find('.answer-comments');
    $(commentBox).slideToggle();
    if ($(commentBox).find('.comment-list').length == 1) {
        $(commentBox).find('.add-comment .anchor-add-comment').hide();
        $(commentBox).find('.comment-box').show();
    }
}

function handleSaveComment(event) {

    // disable the button
    $('.btn-save-comment').prop('disabled', true);

    // add spinner animation in the save button and change the text to 'Saving..'
    $('.btn-save-comment > i').addClass('fa fa-circle-o-notch fa-spin');
    $('.btn-save-comment > span').text(' Saving...');

    var button = $(event.target);
    var commentBox = $(button).closest('.comment-box');
    var input = $(commentBox).find('.txt_comment');

    if (commentBox.hasClass("has-error")) {
        commentBox.removeClass("has-error");
    }
    var comment = $(input).val();
    // take user comment as placeholder text
    input.attr("placeholder", comment);
    if (!comment) {
        if (!commentBox.hasClass("has-error")) {
            commentBox.addClass("has-error");
        }
        input.attr("placeholder", "Comment can't be blank");

        // enable the button
        $('.btn-save-comment').prop('disabled', false);

        // remove the loading class from save button
        $('.btn-save-comment > i').removeClass('fa fa-circle-o-notch fa-spin');
        $('.btn-save-comment > span').text('Save');

        return;
    }

    var userDetails = localStorage.getItem("SC_Session_UserDetails");
    if (!userDetails) {
        //TO-DO: Handle if userdetail is not available in Session Storage
        window.location.href = "/account/login";
    }
    var userInfo = $.parseJSON(userDetails);
    var userId = userInfo.id;

    var answerId = $(button).closest('li.answer').attr('data-answerid');
    var data = { "CommentText": $.trim(comment), "AnswerId": answerId };
    scAjax({
        "url": "comments/postcomment",
        "type": "POST",
        "data": JSON.stringify(data, null, 2),
        "success": function (result) {
            // clear the previous comment text
            $(input).val('');
            // enable the button
            $('.btn-save-comment').prop('disabled', false);
            // remove the loading class from save button
            $('.btn-save-comment > i').removeClass('fa fa-circle-o-notch fa-spin');
            $('.btn-save-comment > span').text('Save');

            // reset commentbox placeholder
            input.attr("placeholder", "Add Comment");

            var answerBlock = $(button).closest('li.answer');
            var commentBlock = $(answerBlock).find('.answer-comments .comment-list.hide').clone();
            $(commentBlock).removeClass('hide');
            $(commentBlock).find('.comment-text').find('span').text(comment);
            $(commentBlock).find('.comment-date').text(getDateFormatted(result.postedOnUtc));
            $(commentBlock).find('.comment-user').text(userInfo.firstName + " " + userInfo.lastName);
            //$(answerBlock).find('.answer-comments').append(commentBlock);
            $(commentBlock).insertBefore($(answerBlock).find('.answer-comments .comment-list.hide'));

            // retrieve email of the user who posted the answer
            //var emailToSend = $(answerBlock).find('.user').find('a.userName').attr("href").substring($(answerBlock).find('.user').find('a.userName').attr("href").indexOf("=") + 1);
            //var notificationToSend = $(answerBlock).attr("data-answerby");
            //NotificationForNewComment(emailToSend, notificationToSend);

            var editCommentAnchor = $(commentBlock).find('.comment-text a');
            $(editCommentAnchor).show();

            $(editCommentAnchor).click(function(){
                //first hide if any open edit comment
                var commentSection = $('.edit-comment:not(.hide)').parent();
                $(commentSection).find('.edit-comment').remove();
                $(commentSection).children().show();

                $('.comment-box:not(.hide)').hide();
                $('.add-comment:hidden').show();

                var editCommentDiv = $('.edit-comment.hide').clone().removeClass('hide');
                var editCommentInput = $(editCommentDiv).find('.txt_comment');
                $(editCommentInput).val($(this).siblings('span').text());
                var editCommentButton = $(editCommentDiv).find('.btn-edit-comment');
                $(editCommentButton).attr('data-comment-id', result.commentId);
                $(commentBlock).children().hide();
                $(commentBlock).append(editCommentDiv);

                $(editCommentButton).click(function(){
                    var commentText = $(editCommentInput).val();
                    $(commentBlock).find('.comment-text span').text(commentText);
                    $(commentBlock).children().show();
                    $(commentBlock).find('.edit-comment').remove();

                    var commentObject = {'CommentText': commentText, 'CommentId': $(this).attr('data-comment-id')};

                    scAjax({
                        "url": "comments/editcomment",
                        "type": "POST",
                        "data": JSON.stringify(commentObject)
                    });

                    return false;
                });

                var editCommentCancel = $(editCommentDiv).find('.anchor-cancel-comment');
                $(editCommentCancel).click(function(){
                    $(commentBlock).children().show();
                    $(commentBlock).find('.edit-comment').remove();
                    return false;
                });

                return false;
            });
        },
        "error": function (err) {
            // parse the error json
            var errorresponse = jQuery.parseJSON(err.responseText);

            // enable the button
            $('.btn-save-comment').prop('disabled', false);

            // remove the loading class from save button
            $('.btn-save-comment > i').removeClass('fa fa-circle-o-notch fa-spin');
            $('.btn-save-comment > span').text('Save');

            // check if comment box has the 'has-error' class or not
            if (!commentBox.hasClass("has-error")) {
                commentBox.addClass("has-error");
            }

            // toastr notification
            Command: toastr["error"](errorresponse.modelState['comment.CommentText'][0])
        }
    });
}

function handleUpvote(event) {
    event.preventDefault();
    var anchor = $(event.target);
    if ($(anchor).parent().hasClass('disabled')) {
        return false;
    }
    var answerId = $(anchor).closest('li.answer').attr('data-answerid');
    //var answerBy = $(anchor).closest('li.answer').attr('data-answerBy');
    //logActivity(3, answerId, answerBy);

    scAjax({
        "url": "answers/UpdateUpVoteCount",
        "type": "POST",
        "data": JSON.stringify({
            "AnswerID": answerId
        }),
        "success": function (result) {
            $(anchor).find('.count').text(result);
            $(anchor).parent().addClass('disabled');
        }
    });
}

function handleCancelComment(event) {
    event.preventDefault();
    var anchor = $(event.target);
    var commentBox = $(anchor).closest('.comment-box');
    $(commentBox).hide();
    $(commentBox).siblings('.add-comment').show();
    var comments = $(commentBox).siblings('.comment-list:not(.hide)');
    if(comments.length == 0)
    {
        $(commentBox).closest('.answer-comments').hide();
    }
}

function setUpAnswerUserActions() {
    $('.comment-anchor').unbind("click").bind("click", handleCommentLinkClick);

    $('.anchor-add-comment').unbind("click").bind("click", handleAddComment);

    $('.anchor-cancel-comment').unbind('click').bind('click', handleCancelComment);

    $('.btn-save-comment').unbind("click").bind("click", handleSaveComment);

    $('.upvote-anchor').unbind("click").bind("click", handleUpvote);
}

function createAnswerHtml(answerBlock, answer) {
    $(answerBlock).removeClass('hide').attr({ 'data-answerid': answer.answerId, 'data-answerBy': answer.userId });
    $(answerBlock).find('.answerdiv').attr('id', answer.answerId);
    // set a max width for any image that is part of the answer
    $(answerBlock).find('.text').html(answer.answerText.replace("<img", "<img style='max-width:530px;'")).attr('id', answer.answerId);
    $(answerBlock).find('.upvote-anchor .count').text(answer.upVoteCount);
    if (answer.isUpvotedByMe) {
        $(answerBlock).find('.upvote-span').addClass('disabled');
    }
    //$(answerBlock).find('.userName').text(answer.displayName);

    $(answerBlock).find('.answer-comments .comment-box').hide();

    var userNameAnchor = $(answerBlock).find('div.user a.userName');
    $(userNameAnchor).text(answer.displayName);
    if (answer.userProfileImage) {
        var userProfileImage = $(answerBlock).find('div.user img.avatar');
        // form the smaller imgur image by adding 's' before '.jpg'
        if (answer.userProfileImage.charAt(answer.userProfileImage.indexOf('.jpg') - 1) != 's') {
            answer.userProfileImage = answer.userProfileImage.replace('.jpg', 's.jpg');
        }
        $(userProfileImage).attr('src', IMGURPATH + answer.userProfileImage);
    }
    if (answer.userEmail != null) {
        var url = $(userNameAnchor).attr('href') + '?userId=' + answer.userId;
        $(userNameAnchor).attr('href', url);
    }

    var utcDate = new Date(answer.postedOnUtc);
    var dateString = getDateFormatted(answer.postedOnUtc);
    $(answerBlock).find('.date').text(dateString);

    if (answer.markedAsAnswer) {
        $(answerBlock).find('.mark-answer').removeClass('hide').addClass('btn-success');
    }

    if(isLoggedInUserId(answer.userId))
    {
        var span = $(answerBlock).find('.edit-answer-span');
        $(span).show();
        $(span).find('.edit-answer-anchor').click(function(){
            handleEditAnswerClick(answer)
        });

    }

    $('#answerlist').append(answerBlock);
}

function handleEditAnswerClick(answer)
{
    if(!answer)
    {
        return;
    }

    advancedEditor.setHTML(answer.answerText);
    $('html,body').animate({scrollTop: $('.wirte-answer').offset().top - 70},'slow');
    $('.btn-submit-answer').attr('data-answer-id', answer.answerId);
}

function scrollToADivOnPageLoad()
{
    var hash = window.location.hash;
    if(hash.length > 0 && $('#'+ hash).size() > 0){
        $('html, body').animate({ scrollTop: $(hash).offset().top - 70});
    }
}

var isAskedByMe = false;
function getAnswers(result) {

    if (!result) {
        // hide the loading symbol for the answers
        $('#loadingdiv').hide();

        // show the submit answer rich text control
        $('.wirte-answer').show();

        // show the 'ask to answer module'
        $('#userToAnswer').show();
        return;
    }

    $('#answerlist').before("<div class='answerlabel col-md-12'><h4>Answers:</h4></div>");
    var save = $('#answerlist .answer.hide').detach();
    $('#answerlist').empty().append(save);

    $(result).each(function (index, answer) {
        var answerBlock = $('#answerlist li.answer.hide').clone();
        createAnswerHtml(answerBlock, answer);
        if (isAskedByMe) {
            var markAnswer = $(answerBlock).find('.mark-answer');
            $(markAnswer).removeClass('hide').click(function () {
                var span = $(this);
                if ($(span).hasClass('btn-success')) {
                    return;
                }

                var answerId = $(span).closest('li.answer').attr('data-answerid');
                var answerBy = $(span).closest('li.answer').attr('data-answerBy');
                var answers = [];
                answers.push({ "AnswerID": answerId, "MarkedAsAnswer": true });
                $('.mark-answer.btn-success').each(function (i, e) {
                    $(e).removeClass('btn-success');
                    var oldAnswerId = $(e).closest('li.answer').attr('data-answerid');
                    answers.push({ "AnswerID": oldAnswerId, "MarkedAsAnswer": false });
                });

                $(span).addClass('btn-success');
                logActivity(5, answerId, answerBy);

                scAjax({
                    "url": "answers/UpdateMarkAsAnswer",
                    "type": "POST",
                    "data": JSON.stringify(answers)
                });
            });
        }

        getComments(answerBlock, answer);
        setUpAnswerUserActions();
    });

    
    if(answerId && answerId != "")
    {
        $('html,body').animate({scrollTop: $('#'+ answerId).offset().top - 20},'fast');
    }
}

function setUpEditQuestion()
{
    var rtContainer = $('.editQuesDiv .richtextbox-container');
    $(rtContainer).find('.control-label').text('Question');
    $('.editQuesDiv #txt_question_edit').val($('#questiontitle').text());

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

    qustionRTBoxEditor.setHTML($('.description').html());

    $('#btn_Edit_Question').click(function(){
        var data = {
            "questionId" : $(this).attr('data-question-id'),
            "title": $('#txt_question_edit').val(),
            "description": qustionRTBoxEditor.getHTML(),
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

        $('.editQuesDiv').toggle();
        $('.header h2').text($('#txt_question_edit').val());
        $('.description').html(qustionRTBoxEditor.getHTML());
        $('.question-container').slideToggle();
    });

    $('#btn_Cancel_Edit_Question').click(function() {
        $('.editQuesDiv').toggle();
        $('.question-container').slideToggle();
    });
}

// This method will execute when user will type user details in the 'Ask To Answer' serach box
$(function () {
    $('#usernametext').keyup(function (event) {
        var searchValue = $(this).val();
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
                    // update the div with the user data received from the API
                    jQuery.each(userListAskToAnswer, function (i, val) {
                        // check if this user already requested to answer this question, if that true then keep the 'Ask' button disabled
                        if (val.profileImageURL != null) {
                            // form the smaller imgur image by adding 's' before '.jpg'
                            if (val.profileImageURL.charAt(val.profileImageURL.indexOf('.jpg') - 1) != 's') {
                                val.profileImageURL = val.profileImageURL.replace('.jpg', 's.jpg');
                            }
                            $('#userlistasktoanswer').append("<div class='wantedanswersuggestion' id='" + val.id + "'><div class='profileimage' style='float:left;'><img class='avatar' width='60' height='60' src='" + IMGURPATH + val.profileImageURL + "'></img></div><div class='userinfo' style='float:left;'><span class='name'><a class='userName' style='margin-left:15px;' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br/><span id='reputationcount' style='margin-left:15px;font-size:12px'>Reputation: " + val.reputationCount + "</span><br/><span id='" + val.id + "' style='margin-left:15px;font-size:12px'></span></div><div class='askbuton'><a id='" + val.id + "' data-id='" + val.id + "'class='btn btn-primary btn-xs' style='float:right;' onclick='SendNotification(this)'>Ask</a></div></div><hr id='smallseparator'/>");
                        }
                        else
                            $('#userlistasktoanswer').append("<div class='wantedanswersuggestion' id='" + val.id + "'><div class='profileimage' style='float:left;'><img class='avatar' width='60' height='60' src='/Content/images/profile-image.jpg'></img></div><div class='userinfo' style='float:left;'><span class='name'><a class='userName' style='margin-left:15px;' href='/Account/Profile?userId=" + val.id + "'>" + val.firstName + " " + val.lastName + "</a></span><br/><span id='reputationcount' style='margin-left:15px;font-size:12px'>Reputation: " + val.reputationCount + "</span><br/><span id='" + val.id + "' style='margin-left:15px;font-size:12px'></span></div><div class='askbuton'><a id='" + val.id + "' data-id='" + val.id + "'class='btn btn-primary btn-xs' style='float:right;' onclick='SendNotification(this)'>Ask</a></div></div><hr id='smallseparator'/>");
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
                            }
                        });

                    });
                }
            }
        }, 400);
    });
});
