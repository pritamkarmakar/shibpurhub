
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

        $http.post('api/questions/DeleteQuestion', { 'QuestionId': questionId });
    };

    loadQuestions(0);
   
});

