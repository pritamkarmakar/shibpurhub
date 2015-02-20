
var SRVER = "http://localhost:57604/";
$(document).ready(function () {
    jQuery.support.cors = true;
    var userID = $("#hdnUserID").val();
    if(userID)
    {
        $.ajax({
            url: "api/Register/Register",
            type: "POST",
            data: {
                "userName": "Taiseer",
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

    $('#li_discussion').click(function () {
        $.ajax({
            url: "api/questions/GetQuestions",
            type: "GET",            
            dataType: "json",
            processData: false,
            success: function (result) {
                if (result) {
                    alert(resu.data);
                }
            }
        });
    });
});