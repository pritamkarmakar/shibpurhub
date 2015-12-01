
angular.module("AdminApp", [])
.controller("MainController", function($scope, $http, $sce){

    var page = 0;
    $scope.questions = [];
    var onReceiveRespone = function(response){
        
        angular.forEach(response.data, function (key, value) {            
            key.description = $sce.trustAsHtml(key.description);
            $scope.questions.push(key);
        });
    };

    function loadQuestions(page)
    {
        var questions = $http({
            url: "api/questions/GetQuestions",
            method: "GET",
            params: { page: page }
        }).then(onReceiveRespone);
    }

    $scope.laodMore = function () {
        page = page + 1;
        loadQuestions(page);
    };

    $scope.deleteQuestion = function (questionId) {
        var question = $.param({ 'questionId': questionId });

        $http({
            url: "api/questions/DeleteQuestion",
            method: "DELETE",
            data: question
        });

        //var config = {
        //    headers: {
        //        'Content-Type': 'application/x-www-form-urlencoded;charset=utf-8;'
        //    }
        //};

        //$http.post('api/questions/DeleteQuestion', question);
    };

    loadQuestions(0);
   
});

