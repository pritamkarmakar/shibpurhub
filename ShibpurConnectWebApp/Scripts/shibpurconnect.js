﻿
var SERVER = "http://shibpur.azurewebsites.net/api/";
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

function scAjax(options)
{
    try
    {
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
                xhr.setRequestHeader("Authorization", "Bearer UEkvwQmR0EOsdGd-9y_bqizgm7F6_qvHSy4tyeKGY9Kb93h2ASjLyvW4BdcuB9cGgt-PcACQAy7WBycNbplGPXtHI4_r4YOLjDeXcK6S4Cswk2SQ5R_51zV1cmytfczRkGM6RnRWKmH_yiIz-LPO6tByk28wkLDeeaLDnoiy6Zg6S5zk9uZZtrreZHRx3nl4SiCD3QKLtXqn7bGYGFF71D745YBAeAjNityNKpyum7pBnQSYpL5qYZHCjI3-94bT");
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

function logActivity(activity, objectId, objectUserId) {
    var userDetails = localStorage.getItem("SC_Session_UserDetails");
    if (!userDetails) {
        return;
    }

    var userInfo = $.parseJSON(userDetails);
    var data = {
        "UserId": userInfo.userId,
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
 