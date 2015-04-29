
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
                xhr.setRequestHeader("Authorization", "Bearer Io3jW1JzoT7LZeyqQxHTs0aA2panG9v41h-YeXpyJq8aiK9gbywJUX3EF2SoIcatHFEg63aao3Gyuuu-tDGP90FCytNkAQymb9u6wL-kSpcSuumLM1xDDDrq1sUfq82txPOozNqZIq0PcHLtnFEy0uaivwGL02mxL9zt_RWgR9D85RxKcXA1aKpgdenC0xz5douIzE3J_QahQmnvXAiSpDkHMEwiF3T4wiVxL8xrF7rR77dfo29ym2C0yp_K5rUr");
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

// method to save new educational history
function saveEducationalHistory() {

    jQuery.support.cors = true;
    var SERVER = "/api/";

    var userDetails = localStorage.getItem("SC_Session_UserDetails");
    if (!userDetails) {
        //TO-DO: Handle if userdetail is not available in Session Storage
        return;
    }

    var userInfo = $.parseJSON(userDetails);
    var token = localStorage.getItem("TOKEN") || "PTLp2jwqIgLZmDBqjtADmHdtHmKpEmy-KtrdrocIBR5dz2k0uHNdgLdeqm6bFODmjUxsgNkcr0LI2UGaT6xve1gxPaHjLAyqneD69pKNIVwpo6mGtiaemzfJFVgV_FS_Q6TzA5InNb16tGCu3VNoYdqQEJu-jLiX3uN3FNAjAUKElpYZbjk5fYZ7ZKZ3T0w7utu0rbNI2BWuxo4HADZ4wrvI1J_-8qnd4Sq1kYWBkdPFeqDO2HQ8Io-IiKSDmnFS";
    var educationalHistory = '{"Department":' + '"' + $('#DepartmentDropdown option:selected').text() + '"' + ',"UniversityName":' + '"' + $('#UniversityName').val() + '"' + ',"GraduateYear":' + '"' + $('#GraduateYear').val() + '"' + ',"UserId":' + '"' + userInfo.userId + '"' + '}';

    // add spinner animation in the save button and change the text to 'Saving..'
    $('#addNewEducation > i').addClass('fa fa-circle-o-notch fa-spin');
    $('#addNewEducation > span').text(' Saving...');
    $.ajax({
        url: SERVER + "EducationalHistories/PostEducationalHistory",
        type: "POST",
        data: educationalHistory,
        contentType: "application/json",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("Authorization", "Bearer " + token);
        },
        success: function (data, result) {
            if (result) {
                // get the educational history from the server and update the div
                $.ajax({
                    url: SERVER + "educationalhistories/geteducationalhistories",
                    type: "GET",
                    dataType: "json",
                    data: { "userEmail": userInfo.email },
                    contentType: "application/json",
                    headers: {
                        Accept: "application/json",
                        "Content-Type": "application/json",
                        //"Access-Control-Allow-Origin": "*"
                    },
                    beforeSend: function (xhr) {
                        xhr.setRequestHeader("Authorization", "Bearer UEkvwQmR0EOsdGd-9y_bqizgm7F6_qvHSy4tyeKGY9Kb93h2ASjLyvW4BdcuB9cGgt-PcACQAy7WBycNbplGPXtHI4_r4YOLjDeXcK6S4Cswk2SQ5R_51zV1cmytfczRkGM6RnRWKmH_yiIz-LPO6tByk28wkLDeeaLDnoiy6Zg6S5zk9uZZtrreZHRx3nl4SiCD3QKLtXqn7bGYGFF71D745YBAeAjNityNKpyum7pBnQSYpL5qYZHCjI3-94bT");
                    },
                    success: function (result) {
                        if (!result) {
                            return;
                        }
                        // toggle the 'Add education' button, so that the form will get close
                        $("#educationContent").slideToggle();
                        // clear div content
                        $('#educationdiv').empty();
                        // remove the loading class from save button
                        $('#addNewEducation > i').removeClass('fa fa-circle-o-notch fa-spin');
                        $('#addNewEducation > span').text('Save');

                        // remove previous error if there is any
                        $('#failuremessage').css('display', 'none');

                        // iterate over all the educational histories
                        jQuery.each(result, function (i, val) {
                            //$('#educationdiv').append("<div id=\"educationalDetails\"><h4 id=\"collegeName\">" + val.universityName + "</h4>, " + "<span id=\"department\">" + val.department + "</span><span id=\"year\">" + val.graduateYear + "</span></div><hr id='dotted'/>");
                            $('#educationdiv').append("<div id=\"educationalDetails\"><h4 id=\"collegeName\">" + val.universityName + ", " + "<span id=\"department\">" + val.department + "</span></h4><P id=\"year\">" + val.graduateYear + "</P></div><hr id='dotted'/>");
                        });
                    }
                });
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            var errorText = XMLHttpRequest.responseText.replace('{"message":"', " ").replace('"}', " ");
            //$("#errorLabel").text(errorText);
            $('#failuremessage').css('display', 'block');
            $('#failuremessage').text(errorText);

            // toggle the 'Add education' button, so that the form will get close
            $("#educationContent").slideToggle();
            // remove the loading class from save button
            $('#addNewEducation > i').removeClass('fa fa-circle-o-notch fa-spin');
            $('#addNewEducation > span').text('Save');
        }
    });
}

// if user click on the Cancel button in the newEducation div then collapse that div
function cancelSaveEducationalHistory() {
    $("#educationContent").slideToggle();
}