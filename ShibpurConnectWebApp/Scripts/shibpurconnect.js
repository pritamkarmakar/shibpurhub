
var SERVER = "http://shibpur.azurewebsites.net/api/";
var IMGURPATH = "http://i.imgur.com/";
$(document).ready(function () {
    jQuery.support.cors = true;
    var userID = $("#hdnUserID").val();
    if(userID)
    {
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
                if(result)
                {
                    //alert(result.data);
                }

            }
        });
    }

    var token = localStorage.getItem("TOKEN");
    if(!token)
    {
        $.ajax({
            url: "http://shibpur.azurewebsites.net/token",
            type: "POST",
            data: {
                "grant_type": "password",
                "userName": "Taiseer@gmail.com",
                "password": "SuperPass"
            },
            contentType: "application/x-www-form-urlencoded",
            dataType: "json",
            processData: false,
            success: function (result) {
                if (result) {
                    localStorage.setItem("TOKEN", result.access_token);
                }
            }
        });
    }

    //$('#li_discussion').click(function () {
    //    $.ajax({
    //        url: SERVER + "questions/GetQuestions",
    //        type: "GET",            
    //        dataType: "json",
    //        contentType: "application/json; charset=utf-8",
    //        processData: false,
    //        success: function (result) {
    //            if (!result) {
    //                return;
    //            }

    //            alert($.parseJSON(result));
    //        }
    //    });
    //});
});

function getApiToken()
{   
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

function scAjax(options)
{
    try
    {
        //var token = getApiToken();
        //if (!token) {
            //TO-DO: show a user friendly message
            //return;
        //}
        var token = $.parseJSON(localStorage.getItem("TOKEN"));
        var server = "/api/"
        var ajaxOptions = {
            url: server + options.url,
            type: options.type || "GET",
            dataType: options.dataType || "json",
            contentType: options.contentType || "application/json; charset=utf-8",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
            },
            beforeSend: function (xhr) {
                //TO-DO: Check new token mechanism
                xhr.setRequestHeader("Authorization", "Bearer " + token.access_token);
            },
            success: function (result) {
                if(options.success && typeof options.success == "function")
                {
                    options.success(result);
                }
            },
            error: function (XMLHttpRequest, textStatus, errorThrown)
            {
                if (options.error && typeof options.error == "function") {
                    options.error(XMLHttpRequest, textStatus, errorThrown);
                }
            }
        };

        if(options.data)
        {
            ajaxOptions.data = options.data;
        }

        $.ajax(ajaxOptions);
    }
    catch(e)
    {
        console.log(e.name + ": " + e.message);
    }
}
 
function getDateFormatted(date)
{
    var utcDate = new Date(date);
    var hour = utcDate.getHours();
    if (hour < 10)
    {
        hour = "0" + hour;        
    }

    var minutes = utcDate.getMinutes();
    if (minutes < 10)
    {
        minutes = "0" + minutes;
    }

    var dateString = getMonth(utcDate.getMonth().toString()) + " " + utcDate.getDate() + ", " + utcDate.getFullYear() + " at " +hour +":" + minutes;
    return dateString;
}

function getDateFormattedByMonthYear(date) {
    var utcDate = new Date(date);
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
        "Content": "<h2 style='color:red;'>ShibpurConnect is down!!!</h2>Detaill Error message: <br>" + error,
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
