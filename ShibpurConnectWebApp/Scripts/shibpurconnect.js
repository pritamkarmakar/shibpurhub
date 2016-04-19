
var SERVER = "http://shibpur.azurewebsites.net/api/";
var IMGURPATH = "http://i.imgur.com/";

$(window).on('onunload', function () {
    console.log("App OnUnload");
    localStorage.clear();
});

$(document).ready(function () {
    jQuery.support.cors = true;
    var userID = $("#hdnUserID").val();
    if (userID) {
        $.ajax({
            url: SERVER + "Account/Register",
            type: "POST",
            data: {
                "userName": userID,
                "password": "SuperPass",
                "confirmPassword": "SuperPass"
            },
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            processData: false,
            success: function (result) {
                if (result) {
                    //alert(result.data);
                }

            }
        });
    }
});

function getApiToken() {
    var token = localStorage.getItem("TOKEN");
    if (!token) {
        alert("Hi");
        $.ajax({
            url: "http://shibpur.azurewebsites.net/token",
            type: "POST",
            data: {
                "grant_type": "password",
                "userName": "holbol.msd@gmail.com",
                "password": "P@ssw0rd"
            },
            contentType: "application/x-www-form-urlencoded",
            dataType: "json",
            async: false,
            processData: false,
            success: function (result) {
                if (result) {
                    token = result.access_token;
                    localStorage.setItem("TOKEN", result.access_token);
                }
            }
        });
    }

    return token;
}

function scAjax(options) {
    try {
        //var token = $.parseJSON(localStorage.getItem("TOKEN"));
        var server = "/api/";
        var ajaxOptions = {
            url: server + options.url,
            cache: false,
            type: options.type || "GET",
            dataType: options.dataType || "json",
            contentType: options.contentType || "application/json; charset=utf-8",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
                'Authorization': 'Bearer ' + localStorage.getItem("accessToken")
            },
            success: function (result) {
                if (options.success && typeof options.success == "function") {
                    options.success(result);
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                // if the request become unauthorize then redirect user 
                // to login page (this will happen once token will get expire)
                if (XMLHttpRequest.status == "401")
                    window.location.href = "/account/login";
                    //window.location = "/Account/Authorize?client_id=web&response_type=token&state=" + encodeURIComponent(window.location.hash);
                if (options.error && typeof options.error == "function") {
                    options.error(XMLHttpRequest, textStatus, errorThrown);
                }
            }
        };

        if (options.data) {
            ajaxOptions.data = options.data;
        }

        $.ajax(ajaxOptions);
    }
    catch (e) {
        console.log(e.name + ": " + e.message);
    }
}

function getDateFormatted(date) {
    var utcDate = new Date(date);
    var hour = utcDate.getHours();
    if (hour < 10) {
        hour = "0" + hour;
    }

    var minutes = utcDate.getMinutes();
    if (minutes < 10) {
        minutes = "0" + minutes;
    }

    var dateString = getMonth(utcDate.getMonth().toString()) + " " + utcDate.getDate() + ", " + utcDate.getFullYear() + " at " + hour + ":" + minutes;
    return dateString;
}

function getDateFormattedByMonthYear(date) {
    var utcDate = new Date(date);
    if(utcDate == new Date("0001-01-01"))
    {
        return "";
    }
    var hour = utcDate.getHours();

    var dateString = getMonth(utcDate.getMonth().toString()) + " " + utcDate.getDate() + ", " + utcDate.getFullYear();
    return dateString;
}

function getMonth(month) {
    var monthArray = {
        "0": "Jan",
        "1": "Feb",
        "2": "Mar",
        "3": "Apr",
        "4": "May",
        "5": "Jun",
        "6": "Jul",
        "7": "Aug",
        "8": "Sep",
        "9": "Oct",
        "10": "Nov",
        "11": "Dec",
    }

    return monthArray[month];
}

function logActivity(activity, objectId, objectUserId) {
    var userDetails = localStorage.getItem("SC_Session_UserDetails");
    if (!userDetails) {
        return;
    }

    var userInfo = $.parseJSON(userDetails);
    var data = {
        "UserId": userInfo.id,
        "Activity": activity,
        "ActedOnObjectId": objectId,
        "ActedOnUserId": objectUserId || ""
    };


    $.ajax({
        url: "/api/useractivity/PostAnActivity",
        type: "POST",
        dataType: "json",
        data: JSON.stringify(data, null, 2),
        contentType: "application/json",
        headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
        },
        beforeSend: function (xhr) {
            xhr.setRequestHeader("Authorization", "Bearer UEkvwQmR0EOsdGd-9y_bqizgm7F6_qvHSy4tyeKGY9Kb93h2ASjLyvW4BdcuB9cGgt-PcACQAy7WBycNbplGPXtHI4_r4YOLjDeXcK6S4Cswk2SQ5R_51zV1cmytfczRkGM6RnRWKmH_yiIz-LPO6tByk28wkLDeeaLDnoiy6Zg6S5zk9uZZtrreZHRx3nl4SiCD3QKLtXqn7bGYGFF71D745YBAeAjNityNKpyum7pBnQSYpL5qYZHCjI3-94bT");
        },
        success: function (result) {
        }
    });
}

// method to send email notification to website admins if there is any error
function SendNotificationForWebSiteError(error) {

    // send email notification
    var alertdata = {
        "Content": "<h2 style='color:red;'>ShibpurHub is down!!!</h2>Detaill Error message: <br>" + error,
        "EmailSentTo": "pritam83@gmail.com"
    };

    // add this record in local log file and send email alert
    scAjax({
        "url": "websitealert/SendEmailNotificationForOutage",
        "type": "POST",
        "data": JSON.stringify(alertdata, null, 2),
        "success": function (result) {

        }
    });

}

function getQueryStringParam(name) {
    try {
        if (!name || name == "") {
            return "";
        }
        name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
        var regexS = "[\\?&]" + name.toLowerCase() + "=([^&#]*)";
        var regex = new RegExp(regexS);
        var url = window.location.href;
        var results = regex.exec(url.toLowerCase());
        if (results == null)
            return null;
        else
            return decodeURIComponent(results[1].replace(/\+/g, " "));
    }
    catch (result) {
    }
}

// function to follow a new tag
function followtag(event) {
    var tagname = event.id;
    var shouldFollowTag = $("#" + event.id + " > span").text().trim().toLowerCase() == "follow topic";

    if (shouldFollowTag) {
        scAjax({
            "url": "tags/FollowNewTag?tagName=" + tagname,
            "type": "POST",
            "success": function (result) {
                if (!result) {
                    return;
                }

                $("#" + tagname + " > i").addClass('fa fa-check-circle');
                $("#" + tagname + " > span").text(' Following');

                Command: toastr["success"]("Successfully subscribed to " + tagname);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                if (XMLHttpRequest.status == "401")
                    window.location.href = "/account/login";
                else
                    Command: toastr["error"](tagname + " is not a valid tag");
            }
        });
    }
    else if ($("#" + event.id + " > span").text().trim().toLowerCase() == "unfollow" || $("#" + event.id).hasClass('active')) {
        scAjax({
            "url": "tags/unfollowtag?tagName=" + tagname,
            "type": "POST",
            "success": function (result) {
                if (!result) {
                    return;
                }

                $("#" + tagname + " > i").removeClass('fa fa-check-circle');
                $("#" + tagname + " > i").addClass('fa fa-plus-circle');
                $("#" + tagname + " > span").text(' Follow Topic');

                Command: toastr["success"]("Successfully unsubscribed " + tagname);
            },
            "error": function () {
                Command: toastr["error"]("Failed to unsubscribed this tag. Please try again");
            }
        });
    }
}

// change text of the follow button on mouse hover
function changetextonmouseover(event) {
    //$("#" + event.id + " > span").attr('style', 'color:black;font-weight:bold');

    if ($("#" + event.id + " > span").text().trim().toLowerCase().search("following") == 0) {
        $("#" + event.id + " > i").removeClass('fa fa-check-circle');
        $("#" + event.id + " > i").addClass('fa fa-minus-circle');
        $("#" + event.id + " > i").attr('style', 'color:white;');
        $("#" + event.id).attr('style', 'background-color:#A5152A;');
        $("#" + event.id + " > span").attr('style', 'color:white;font-weight:bold;background-color:#A5152A;');
        $("#" + event.id + " > span").text(" Unfollow");
    }
    else {
        // change button color when somone will hover on the follow button and if the text is 'Follow'
        $("#" + event.id).attr('style', 'background-color:#2098D1;');
        $("#" + event.id + " > i").attr('style', 'color:white;');
        $("#" + event.id + " > span").attr('style', 'color:white;font-weight:bold;');
    }
}

// mouseover effect for 'Account/Profile' page user follow button
function changeuserfollowonmouseover(event) {
    if ($("#" + event.id + " > span").text().trim().toLowerCase() == "following") {
        $("#" + event.id + " > i").removeClass('fa fa-check-circle');
        $("#" + event.id + " > i").addClass('fa fa-minus-circle');

        $("#" + event.id).removeClass();
        $("#" + event.id).addClass('btn btn-xs btn-danger');
        $("#" + event.id + " > span").text(" Unfollow");
    }
    else {
        // change button css when somone will hover on the follow button and if the text is 'Follow'       
        $("#" + event.id).removeClass();
        $("#" + event.id).addClass('btn btn-xs btn-warning');
        $("#" + event.id + " > i").attr('style', 'color:white;');
        $("#" + event.id + " > span").attr('style', 'color:white;font-weight:bold;');
    }
}

// change text of the follow button on mouse out
function changetextonmouseout(event) {
    //$("#" + event.id + " > span").attr('style', 'color:black;font-weight:normal');

    if ($("#" + event.id + " > span").text().trim().toLowerCase().search("unfollow") == 0) {
        $("#" + event.id + " > i").removeClass('fa fa-minus-circle');
        $("#" + event.id + " > i").addClass('fa fa-check-circle');
        $("#" + event.id).attr('style', 'background-color:#bed4e4;');
        $("#" + event.id + " > span").attr('style', 'color:black;');
        //$("#" + event.id + " > span").text(" Following");
        $("#" + event.id + " > i").attr('style', 'color:green;');

        if (event.attributes["count"] != null)
            $("#" + event.id + " > span").html(" Following" + "&nbsp;&nbsp;<span class='badge'>" + event.attributes["count"].nodeValue + "</span>");
        else {
            $("#" + event.id + " > span").text(" Following");
        }
    }
    else {
        $("#" + event.id).attr('style', 'background-color:#bed4e4;');
        $("#" + event.id + " > span").attr('style', 'color:black;');
        $("#" + event.id + " > i").attr('style', 'color:green;');
    }
}

// mouseout effect for 'Account/Profile' page user follow button
function changeuserfollowonmouseout(event) {
    if ($("#" + event.id + " > span").text().trim().toLowerCase() == "unfollow") {
        $("#" + event.id + " > i").removeClass('fa fa-minus-circle');
        $("#" + event.id + " > i").addClass('fa fa-check-circle');

        $("#" + event.id).removeClass();
        $("#" + event.id).addClass('btn btn-xs btn-success');
        $("#" + event.id + " > span").text(" Following");
    }
    else {
        $("#" + event.id).removeClass();
        $("#" + event.id).addClass('btn btn-xs btn-success');
        $("#" + event.id + " > span").attr('style', 'color:white;');
        $("#" + event.id + " > i").attr('style', 'color:white;');
    }
}

// method to report spam for a question
function reportspam() {
    var data = {
        "QuestionId": questionID,
        "SpamType": $("input[name=spamtyperadio]:checked").val()
    };

    scAjax({
        "url": "questions/reportspam",
        "data": JSON.stringify(data, null, 2),
        "type": "POST",
        "success": function (result) {
            if (!result) {
                return;
            }
            // close the modal
            $('#reportspammodal').toggle();
            if (result == "you have alrady reported this before") {
                Command: toastr["info"](result);
            }
            else
                Command: toastr["success"](result);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            // close the modal
            $('#reportspammodal').toggle();
            if (XMLHttpRequest.status == "401")
                window.location.href = "/account/login";
            else
                Command: toastr["error"]("Error: " + textStatus);
        }
    });


}

// method to close the report spam modal dialog
function togglemodal() {
    $('#reportspammodal').toggle();
}

// method to follow a user
function followuser(obj, profileId) {
    scAjax({
        "url": "profile/followuser?userIdToFollow=" + profileId,
        "type": "POST",
        "success": function (result) {
            if (!result) {
                return;
            }
            if (result == "you have alrady reported this before") {
                Command: toastr["info"](result);
            }
            else {
                Command: toastr["success"](result);
                if (!obj)
                {
                    return;
                }
                $("#" + obj.id + "> i").removeClass();
                $("#" + obj.id + "> i").addClass('fa fa-check-circle');
                $("#" + obj.id + "> span").text(' Following');
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            if (XMLHttpRequest.status == "401")
                window.location.href = "/account/login";
            else
                Command: toastr["error"]("Error: " + textStatus);
        }
    });
}

// method to unfollow user
function unfollowuser(obj, profileId) {
    scAjax({
        "url": "profile/unfollowuser?userToUnfollow=" + profileId,
        "type": "POST",
        "success": function (result) {
            if (!result) {
                return;
            }
            if (result == "you are not following this user") {
                Command: toastr["info"](result);
            }
            else {
                Command: toastr["success"](result);
                if (!obj) {
                    return;
                }
                $("#" + obj.id + "> i").removeClass();
                $("#" + obj.id + "> i").addClass('fa fa-plus-circle');
                $("#" + obj.id + "> span").text(' Follow');
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            if (XMLHttpRequest.status == "401")
                window.location.href = "/account/login";
            else
                Command: toastr["error"]("Error: " + textStatus);
        }
    });
}

// create bootstrap pagination
function buildPagination(pages) {
    $('.pagination li').remove();

    if (pages == 1) {
        return;
    }
    for (var i = 1; i <= pages; i++) {
        var anchor = $('<a>').text(i).attr('href', '#');
        var li = $('<li>').append(anchor);
        if (i == 1) {
            $(li).addClass('disabled');
        }
        $('.pagination').append(li);
    }
}

var LOGGEDINUSERDETAILS;
function isLoggedInUserId(userId) {
    if (!userId) {
        return false;
    }

    if (LOGGEDINUSERDETAILS == null) {
        LOGGEDINUSERDETAILS = localStorage.getItem("SC_Session_UserDetails");
    }

    var userInfo = $.parseJSON(LOGGEDINUSERDETAILS);

    // check if userInfo is null, this can happen when local storage doesn't have the userinfo
    // then check whether the localstorage userinfo and logged-in user is same or not
    if (userInfo != null && userInfo.id == userId) {
        return true;
    }

    return false;
}

// method to follow a question
function followquestion(obj) {
    if ($("#" + obj.id + " > span").text().trim().toLowerCase().search("follow") == 0) {
        // add a spinner once user will click on the 'Follow' button
        $("#" + obj.id + "> i").removeClass();
        $("#" + obj.id + "> i").addClass('fa fa-circle-o-notch fa-spin');
        scAjax({
            "url": "questions/followquestion?questionId=" + obj.id,
            "type": "POST",
            "success": function (result) {
                if (!result) {
                    return;
                }
                if (result == "you are already following this question") {
                    Command: toastr["info"](result);
                    $("#" + obj.id + "> i").removeClass();
                    $("#" + obj.id + "> i").addClass('fa fa-check-circle');
                    $("#" + obj.id + "> span").text(" Following");

                    // read current follower count
                    var followercount = parseInt(obj.attributes["count"].nodeValue);
                    $("#" + obj.id).attr("count", followercount);
                    $("#" + obj.id + "> span").append("&nbsp;&nbsp;<span class='badge'>" + followercount + "</span>");
                } else {
                    Command: toastr["success"](result);
                    $("#" + obj.id + "> i").removeClass();
                    $("#" + obj.id + "> i").addClass('fa fa-check-circle');
                    $("#" + obj.id + "> span").text(" Following");

                    // read current follower count and add +1 into that
                    var followercount = parseInt(obj.attributes["count"].nodeValue) + 1;
                    $("#" + obj.id).attr("count", followercount);
                    $("#" + obj.id + "> span").append("&nbsp;&nbsp;<span class='badge'>" + followercount + "</span>");
                }

            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                if (XMLHttpRequest.status == "401")
                    window.location.href = "/account/login";
                else
                    Command: toastr["error"]("Error: " + textStatus);
            }
        });
    }
    else if ($("#" + obj.id + " > span").text().trim().toLowerCase().search("unfollow") == 0) {
        scAjax({
            "url": "questions/unfollowquestion?questionId=" + obj.id,
            "type": "POST",
            "success": function (result) {
                if (!result) {
                    return;
                }

                $("#" + obj.id + "> i").removeClass();
                $("#" + obj.id + "> i").addClass('fa fa-plus-circle');
                $("#" + obj.id + "> span").text(' Follow');

                // read current follower count and add +1 into that
                var followercount = parseInt(obj.attributes["count"].nodeValue) - 1;
                $("#" + obj.id).attr("count", followercount);

                // don't show the nuymber if it is '0'
                if (followercount > 0)
                    $("#" + obj.id + "> span").append("&nbsp;&nbsp;<span class='badge'>" + followercount + "</span>");

                Command: toastr["success"]("Successfully unsubscribed this question");
            },
            "error": function () {
                Command: toastr["error"]("Failed to unsubscribed this question. Please try again");
            }
        });
    }
}

function updateFollowQuestion(follow, questionId, success) {
    if (follow) {
        scAjax({
            "url": "questions/followquestion?questionId=" + questionId,
            "type": "POST",
            "success": function (result) {
                if (!result) {
                    return;
                }
                success();
            }
        });
        return;
    }

    scAjax({
        "url": "questions/unfollowquestion?questionId=" + questionId,
        "type": "POST",
        "success": function (result) {
            if (!result) {
                return;
            }
            success();
        }
    });
}



function updateQnAStatus(questionIds, answerIds) {
    var userDetails = localStorage.getItem("SC_Session_UserDetails");
    var userInfo = $.parseJSON(userDetails);
    if (userInfo == null) {
        return;
    }

    var loggedInUserId = userInfo.id;

    scAjax({
        "url": "feed/GetPersonalizedQAStatus",
        "data": { "userId": loggedInUserId, "questionIds": questionIds, "answerIds": answerIds },
        "success": function (result) {
            if (!result) {
                return;
            }

            $(result).each(function (i, item) {
                var button;

                if (item.isQuestion) {
                    button = $("a.thumbs#" + item.id);
                    if (!item.isAskedByMe) {
                        $(button).closest('.follow-ul').show();
                    }

                    if (item.isFollowedByMe) {
                        $(button).addClass('active');
                        $(button).find('span').text('Following');
                        $(button).find('i.fa').removeClass('fa-plus-circle').addClass('fa-check');
                    }
                }
                else {
                    var upvoteButton = $(".upvote-ul a[data-answerId='" + item.id + "']");
                    var markAsAnswerButton = $(".markanswer-ul a[data-answerId='" + item.id + "']");
                    if (!item.isAnsweredByMe) {
                        $(upvoteButton).closest('.upvote-ul').show();
                    }
                    if (item.isUpvotedByMe) {
                        $(upvoteButton).addClass('active');
                        $(upvoteButton).find('span').text('Upvoted');
                        $(upvoteButton).find('i.fa').removeClass('fa-arrow-up').addClass('fa-thumbs-up');
                    }
                    if(item.markedAsAnswer)
                    {
                        $(markAsAnswerButton).addClass('active');
                        $(markAsAnswerButton).find('span').text('Accepted');
                        $('.thread.answer#' + item.id).addClass('accepted-answer');
                    }
                }
            });
        }
    });
}

var TWITTEREMOJIPATH = "https://twemoji.maxcdn.com/72x72/";
var Icons =
            [
               { "Emoji": ":)", "Path": TWITTEREMOJIPATH + "1f600.png" },
               { "Emoji": ":(", "Path": TWITTEREMOJIPATH + "1f626.png" },
               { "Emoji": ":-)", "Path": TWITTEREMOJIPATH + "1f603.png" },
               { "Emoji": ":D", "Path": TWITTEREMOJIPATH + "1f604.png" },
               { "Emoji": ":X", "Path": TWITTEREMOJIPATH + "1f621.png" },
               { "Emoji": ":P", "Path": TWITTEREMOJIPATH + "1f61c.png" },
            ];

function getEmojiedString(original) {
    if (!original) {
        return "";
    }

    var transformed = original;
    var htmlFormattedEmoji = "<span class='emoji-span'><img draggable='false' class='emoji' src={0}></span>";

    $.each(Icons, function (i, icon) {
        if (original.indexOf(icon.Emoji) > -1) {
            var html = htmlFormattedEmoji.replace("{0}", icon.Path);
            transformed = original.replace(icon.Emoji, html);
            original = transformed;
        }
    });

    return transformed;
}

function updateUpVote(answerId, success) {
    var upvoteButton = $(".upvote-ul a[data-answerId='" + answerId + "']");

    if ($(upvoteButton).hasClass('active')) {
        $(upvoteButton).attr('title', 'You have already upvoted this answer.');
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

function markAsAnswer(answerId, success) {
    $('.answer.accepted-answer').removeClass('accepted-answer');
    
    var answers = [];
    var uiAnswers = $('.markanswer-ul:visible');
    var markAsAnswerButton;
    $(uiAnswers).each(function(i,ul){
        var anchor = $(ul).find('a');
        var id = $(anchor).attr('data-answerId');
        var accepted = $(anchor).hasClass('active');
        
        if(id == answerId)
        {
            markAsAnswerButton = anchor;
            answers.push({ "AnswerID": id, 
                       "MarkedAsAnswer": !accepted });
        }
        else if(accepted)
        {
            answers.push({ "AnswerID": id, 
                       "MarkedAsAnswer": false });
            $(anchor).removeClass('active');
            $(anchor).find('span').text('Accept');
        }
    });

    if(markAsAnswerButton)
    {
        var icon = $(markAsAnswerButton).find('i.fa');
        $(icon).addClass('fa-circle-o-notch fa-spin');
    }
    
    scAjax({
        "url": "answers/UpdateMarkAsAnswer",
        "type": "POST",
        "data": JSON.stringify(answers),
        "success": function (result) {
            if (!result) {
                return;
            }
            
            if (success && typeof success === 'function') {
                success();
            }
            
            if(markAsAnswerButton)
            {
                $(markAsAnswerButton).toggleClass('active');
                $(icon).removeClass('fa-arrow-up fa-circle-o-notch fa-spin');
                
                var text = "Accept";
                if($(markAsAnswerButton).hasClass('active'))
                {
                    $('.thread.answer#' + answerId).addClass('accepted-answer');
                    text = "Accepted";
                }
                
                $(markAsAnswerButton).find('span').text(text);
            }
        }
    });
}

function followQuestion(questionId) {
    var button = $("#" + questionId);
    //var questionId = $(button).attr('data-questionId');
    var textSpan = $(button).find('span');
    var icon = $(button).find('i.fa');
    $(icon).addClass('fa-circle-o-notch fa-spin');

    if ($(button).hasClass('active')) {
        updateFollowQuestion(false, questionId, function () {
            $(button).removeClass('active');
            $(textSpan).text('Follow');
            $(icon).removeClass('fa-check fa-circle-o-notch fa-spin').addClass('fa-plus-circle');
        });
    }
    else {
        updateFollowQuestion(true, questionId, function () {
            $(button).addClass('active');
            $(textSpan).text('Following');
            $(icon).removeClass('fa-plus-circle fa-circle-o-notch fa-spin').addClass('fa-check');
        });
    }
}


// method to retrieve users based on current user graduation year, used in Users > index page
function getUsers() {
    var batchmates = null;
    var seniors = null;
    var juniors = null;
    var allusers = null;
    var allunknownusers = null;

    // find user graduation year, for multiple BEC educations consider the graduation year
    var userDetails = localStorage.getItem("SC_Session_UserDetails");

    var graduationyear = -1;
    if (userDetails) {
        var userInfo = $.parseJSON(userDetails);
        var educationInfo = userInfo.educationalHistories;
        $(educationInfo).each(function (i, education) {
            if (education.isbecEducation == true) {
                if (graduationyear == -1)
                    graduationyear = education.graduateYear;
                else {
                    if (education.graduateYear < graduationyear)
                        graduationyear = education.graduateYear;
                }
            }
        });
    }

    $('.table').hide();

    // if user blongs to BEC and has a graduation year then find batchmates, immediate seniors and juniors otherwise get all the users
    if (graduationyear != -1) {
        var seniorgradyear = graduationyear - 1;
        var juniorgradyear = graduationyear + 1;

        var doSomethingOnceValueIsPresent = function () {
            if (batchmates != null && seniors != null && juniors != null && allusers != null && allunknownusers != null) {
                // hide the loading div
                $('#loadingusers').hide();
                // add batchmates
                if (batchmates && batchmates.length > 1) {
                    // add the heading
                    $('#userlist').append("<h2 class='usertype col-md-12'>Batchmates</h2>");
                    $(batchmates).each(function (i, userinfo) {
                        if (userInfo.id != userinfo.id)
                            $('#userlist').append(getProfileHtml(userinfo));
                    });
                }

                // add seniors
                if (seniors && seniors.length > 0) {
                    // add the heading
                    if (seniors.length > 1) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Immediate Seniors</h2>");
                    }
                    else if (seniors.length == 1 && userInfo.id != seniors[0].id) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Immediate Seniors</h2>");
                    }
                    $(seniors).each(function (i, userinfo) {
                        if (userInfo.id != userinfo.id)
                            $('#userlist').append(getProfileHtml(userinfo));
                    });

                }

                // add immediate juniors
                if (juniors && juniors.length > 0) {
                    // add the heading
                    if (juniors.length > 1) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Immediate Juniors</h2>");
                    }
                    else if (juniors.length == 1 && userInfo.id != juniors[0].id) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Immediate Juniors</h2>");
                    }

                    $(juniors).each(function (i, userinfo) {
                        if (userInfo.id != userinfo.id)
                            $('#userlist').append(getProfileHtml(userinfo));
                    });
                }

                // add all other users
                if (allusers && allusers.length > 0) {
                    $(allusers).each(function (i, userbybatch) {
                        // add the heading
                        if (userbybatch.userList.length > 1) {
                            $('#userlist').append("<h2 class='usertype col-md-12'>" + userbybatch.graduateYear + "</h2>");
                        }
                        else if (userbybatch.userList.length == 1 && userbybatch.userList[0].id != userInfo.id) {
                            $('#userlist').append("<h2 class='usertype col-md-12'>" + userbybatch.graduateYear + "</h2>");
                        }


                        // iterate all users from this graduate year and add here
                        $(userbybatch.userList).each(function (i, user) {
                            if (userInfo.id != user.id)
                                $('#userlist').append(getProfileHtml(user));
                        });
                    });
                }

                // add all unkownd users
                if (allunknownusers && allunknownusers.length > 0) {
                    if (allunknownusers.length > 1) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Unknown Users</h2>");
                    }
                    else if (allunknownusers.length == 1 && userInfo.id != allunknownusers[0].id) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Unknown Users</h2>");
                    }

                    $(allunknownusers).each(function (i, userinfo) {
                        if (userInfo.id != userinfo.id)
                            $('#userlist').append(getProfileHtml(userinfo));
                    });
                }
            }
            else {
                setTimeout(function () {
                    doSomethingOnceValueIsPresent()
                }, 2000);
            }
        };

        doSomethingOnceValueIsPresent();
        scAjax({
            "url": "users/findusersforayear?graduationYear=" + graduationyear,
            "success": function (result) {

                batchmates = result;

                handleFollowClick();
                handleToggleViewClick();
            }
        });

        // get immediate senior users
        scAjax({
            "url": "users/findusersforayear?graduationYear=" + seniorgradyear,
            "success": function (result) {
                // set the seniors variabe to API result
                seniors = result;

                handleFollowClick();
                handleToggleViewClick();
            }
        });

        // get immediate junior users
        scAjax({
            "url": "users/findusersforayear?graduationYear=" + juniorgradyear,
            "success": function (result) {
                // set the juniors
                juniors = result;
            }
        });

        var skipYears = graduationyear + "," + seniorgradyear + "," + juniorgradyear;

        scAjax({
            "url": "users/FindAllBECUsers?skipyears=" + skipYears,
            "success": function (result) {
                // save the api result in the global variable 'userlist'
                allusers = result;

                handleFollowClick();
                handleToggleViewClick();
            }
        });

        scAjax({
            "url": "users/GetNonBECUsers",
            "success": function (result) {
                // save the api result in the global variable 'userlist'
                allunknownusers = result;

                handleFollowClick();
                handleToggleViewClick();
            }
        });
    }
    else {
        scAjax({
            "url": "users/FindAllBECUsers?skipyears=",
            "success": function (result) {
                // save the api result in the global variable 'userlist'
                allusers = result;
                handleFollowClick();
                handleToggleViewClick();
            }
        });

        scAjax({
            "url": "users/GetNonBECUsers",
            "success": function (result) {
                // save the api result in the global variable 'userlist'
                allunknownusers = result;
                handleFollowClick();
                handleToggleViewClick();
            }
        });

        var doSomethingOnceValueIsPresent = function () {
            if (allusers != null && allunknownusers != null) {
                // hide the loading div
                $('#loadingusers').hide();
                // add all other users
                if (allusers && allusers.length > 0) {
                    $(allusers).each(function (i, userbybatch) {
                        // add the heading
                        $('#userlist').append("<h2 class='usertype col-md-12'>" + userbybatch.graduateYear + " Batch" + "</h2>");

                        // iterate all users from this graduate year and add here
                        $(userbybatch.userList).each(function (i, user) {
                            $('#userlist').append(getProfileHtml(user));
                        });
                    });
                }
                // add all unkownd users
                if (allunknownusers && allunknownusers.length > 0) {
                    // add all unkownd users
                    if (allunknownusers && allunknownusers.length > 0) {
                        $('#userlist').append("<h2 class='usertype col-md-12'>Unknown Users</h2>");
                        $(allunknownusers).each(function (i, userinfo) {
                            $('#userlist').append(getProfileHtml(userinfo));
                        });
                    }
                }
            }
            else {
                setTimeout(function () {
                    doSomethingOnceValueIsPresent()
                }, 2000);
            }
        };

        doSomethingOnceValueIsPresent();
    }

}


