
angular.module("AdminApp", [])
.controller("MainController", function($scope, $http, $sce){

    var page = 0;
    $scope.questions = [];
    var deletedQuestionId;
    var onReceiveRespone = function(response){
        
        angular.forEach(response.data, function (key, value) {            
            key.description = $sce.trustAsHtml(key.description);
            $scope.questions.push(key);
        });
        
        $scope.isLoading = false;
    };
    
    var onDelete = function()
    {
        var len = $scope.questions.length;
        var index = -1;
        for (var i = 0; i < len; i += 1) {
            if ($scope.questions[i].questionId === deletedQuestionId) {
                index = i;
                break;
            }
        }

        if (index !== -1) {
            $scope.questions.splice(index,1);
        }
    }
 
    function loadQuestions(page)
    {
        $scope.isLoading = true;
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
        deletedQuestionId = questionId;
        $http.post('api/questions/DeleteQuestion', { 'QuestionId': questionId }).then(onDelete);
    };

    loadQuestions(0);
   
});

