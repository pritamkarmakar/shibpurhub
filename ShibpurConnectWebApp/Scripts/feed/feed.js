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

$(document).ready(function () {

    loadQuestions(0);

    $('.pagination li a').click(function () {
        var anchor = $(this);
        $('.pagination').hide();
        $('#threads .thread:not(.hide)').remove();
        //$('#loadingdiv').show();
        var page = parseInt($(anchor).text());
        loadQuestions(page - 1);
        $('.pagination li.disabled').removeClass('disabled');
        $(anchor).parent().addClass('disabled');
        return false;
    });
});

function loadQuestions(pageNumber) {
    $('#currentPage').val(pageNumber);

    var url = window.location.href;
    if (url.indexOf("FeedByCategory") > 1) {
        var splitArr = url.split('/');
        //var category = splitArr[splitArr.length - 1];
        var category = getQueryStringParam('category');
        //category = decodeURIComponent(category);

        scAjax({
            "url": "questions/GetQuestionsByCategory",
            "data": { "category": category, "page": pageNumber },
            "success": function (result) {
                if (!result) {
                    return;
                }
                // hide the loading div
                $('#loadingdiv').hide();
                var questions = result;
                var totalPages = questions[0].totalPages || 0;
                //buildPagination(totalPages);
                //createQuestions(questions);
                createAllQuestions(questions, pageNumber)
                $('.pagination').show();
            }
        });
    }
    else {
        scAjax({
            "url": "questions/GetQuestions",
            "data": { "page": pageNumber },
            "success": function (result) {
                if (!result) {
                    return;
                }
                // hide the loading symbol
                $('#loadingdiv').hide();
                var questions = result;
                if (pageNumber == 0) {
                    var totalPages = questions[0].totalPages || 0;
                    //buildPagination(totalPages);
                }
                //createQuestions(questions);
                createAllQuestions(questions, pageNumber)
                $('.pagination').show();
            },
            "error": function (err) {
                // hide the loading symbol
                $('#loadingdiv').hide();

                // temporarily adding this, will remove it later. this is to identify whats going wrong here.
                alert(err);
            }
        });
    }
}

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
        $(anchor).click(function () {
            var link = $(this);
            $('.pagination').hide();
            $('#threads .thread:not(.hide)').remove();
            $('#loadingdiv').show();
            var page = parseInt($(link).text());
            loadQuestions(page - 1);
            $('.pagination li.disabled').removeClass('disabled');
            $(link).parent().addClass('disabled');
            return false;
        });
    }
}

var questionIds;
function createAllQuestions(questions, page)
{
    if (!questions) {
        return;
    }
    
    questionIds = [];
    $(questions).each(function (index, question) {
        questionIds.push(question.questionId);

        var htmlItem = $('div.item.hide').clone().removeClass('hide');

        var creatorImage = $(htmlItem).find('.post-creator-image');
        $(creatorImage).attr("href", "/Account/Profile?userId" + question.userId).css('background-image', "url(http://i.imgur.com/" + question.userProfileImage + ")");

        $(htmlItem).find('a.name-link').text(question.displayName).attr("href", "/Account/Profile?userId=" + question.userId);

        $(htmlItem).find('span.action').text(" asked a ");

        var questionUrl = '/feed/' + (question.urlSlug == null ? question.questionId : question.urlSlug);
        $(htmlItem).find('a.target').text("Question").attr("href", questionUrl);

        //$(htmlItem).find('p.designation').text(feed.userDesignation);

        $(htmlItem).find('h2.title a').text(question.title).attr("href", questionUrl);;

        var tempdescription = question.description;
        if (tempdescription.length > 300)
        {
            tempdescription = tempdescription.substring(0, 300) + " ...";
        }
        $(htmlItem).find('div.post-description p').html(tempdescription);
        $(htmlItem).find('p.designation').text(question.careerDetail);
        $(htmlItem).find('div.post-description img').addClass("col-md-12 col-md-12 col-xs-12");

        if (question.viewCount && question.viewCount > 1)
        {
            $(htmlItem).find('span.view-count').text(question.viewCount + " views");
        }
        
        var answerCount = question.answerCount || 0;
        if (answerCount > 1) {
            $(htmlItem).find('span.answer-count').text(answerCount + " answers");
        }
        
        $(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(question.postedOnUtc));
        
        if (!page) {
            $("div.feed-list").append(htmlItem);
        }
        else {
            $(htmlItem).insertBefore("div.row.load:not('.hide')");
        }
        
        //if(!question.isAskedByMe)
        //{
            //$(htmlItem).find('.follow-ul').show();
            var followButton = $(htmlItem).find('.follow-ul a.thumbs');
            $(followButton).attr({ 'data-questionId': question.questionId, 'id': question.questionId });
            //if (question.isFollowedByMe) 
            //{
            //    $(followButton).addClass('active');
            //    $(followButton).find('span').text('Following');
            //    $(followButton).find('i.fa').removeClass('fa-plus-circle').addClass('fa-check');
            //}
            
            $(followButton).click(function(event){
                event.preventDefault();
    
                var button = $(this);
                var questionId = $(button).attr('data-questionId');
                var textSpan = $(button).find('span');
                var icon = $(button).find('i.fa');
                $(icon).addClass('fa-circle-o-notch fa-spin');
                
                if($(button).hasClass('active'))
                {
                    updateFollowQuestion(false, questionId, function(){
                        $(button).removeClass('active');
                        $(textSpan).text('Follow');
                        $(icon).removeClass('fa-check fa-circle-o-notch fa-spin').addClass('fa-plus-circle');
                    });
                }
                else
                { 
                    updateFollowQuestion(true, questionId, function(){
                        $(button).addClass('active');
                        $(textSpan).text('Following');
                        $(icon).removeClass('fa-plus-circle fa-circle-o-notch fa-spin').addClass('fa-check');
                    });
                }
            });
       //}
    });

    if (!page) {
        var loadMoreDiv = $("div.feed-list .load").clone().removeClass('hide');
        $("div.feed-list").append(loadMoreDiv);

        $('a.loadMore').click(function (event) {
            event.preventDefault();

            $('.toggleThis.loadlabel').hide();
            $('.toggleThis.spinner').show();

            var currentPage = parseInt($('#currentPage').val());
            loadQuestions(currentPage + 1);
        });

        $("#loading").hide();
    }
    else {
        $('.toggleThis.loadlabel').show();
        $('.toggleThis.spinner').hide();
    }

    updateQnAStatus(questionIds, []);
}

function createQuestions(questions) {
    if (!questions) {
        return;
    }

    $(questions).each(function (index, question) {
        var thread = $('div.thread.hide').clone();
        var utcDate = new Date(question.postedOnUtc);
        var dateString = getMonth(utcDate.getMonth().toString()) + " " + utcDate.getDate() + ", " + utcDate.getFullYear();

        $(thread).removeClass('hide');
        var userNameAnchor = $(thread).find('div.user a.userName');
        if (question.userProfileImage) {
            var userProfileImage = $(thread).find('div.user img.avatar');
            // form the smaller imgur image by adding 's' before '.jpg'
            if (question.userProfileImage.charAt(question.userProfileImage.indexOf('.jpg') - 1) != 's') {
                question.userProfileImage = question.userProfileImage.replace('.jpg', 's.jpg');
            }
            else
                question.userProfileImage = question.userProfileImage;

            $(userProfileImage).attr('src', IMGURPATH + question.userProfileImage);
        }
        $(userNameAnchor).text(question.displayName);
        if (question.userEmail != null) {
            var url = $(userNameAnchor).attr('href') + '?userId=' + question.userId;
            $(userNameAnchor).attr('href', url);
        }
        //$(thread).find('div.user').attr('onclick', "window.location='.././Account/Profile?userEmail=" + question.userEmail + "'");
        if (question.urlSlug != null)
            $(thread).find('div.detail a').text(question.title).attr('href', '/feed/' + question.urlSlug);
        else
            $(thread).find('div.detail a').text(question.title).attr('href', '/feed/' + question.questionId);
        $(thread).find('div.detail a').attr('class', 'question-hyperlink');
        // extract only text from the question description, if user has image in the question description we will be able to remove those
        var tempdescription = '<p>' + question.description + '</p>';
        $(thread).find('div.detail .excerpt').append($(tempdescription).text().substring(0, 250));
        $(thread).find('div.stat span.date').text(dateString);
        //updateAnswerCount(question.questionId, $(thread).find('div.stat span.answercount'));
        var answerCount = question.answerCount || 0;
        var answerCountSpan = $(thread).find('div.stat span.answercount');
        if (answerCount > 1) {
            $(answerCountSpan).html("<b>" + answerCount + "</b>" + " answers");
        }
        else {
            $(answerCountSpan).html("<b>" + answerCount + "</b>" + " answer");
        }
        if (question.viewCount > 1)
            $(thread).find('div.stat span.viewcount').html("<b>" + question.viewCount + "</b>" + " views");
        else
            $(thread).find('div.stat span.viewcount').html("<b>" + question.viewCount + "</b>" + " view");
        //$(thread).find('div.stat span.answercount').text((question.comments || "0") + " answers");

        if (question.categories) {
            $(question.categories).each(function (i, category) {
                var tagAnchor = $('<a>').addClass('post-tag').text(category).attr('href', "/Feed/FeedByCategory?category=" + category);
                $(thread).find('div.tags').append(tagAnchor);
            });
        }

        $('#threads').append(thread);
    });
}

function updateAnswerCount(questionId, span) {
    if (!questionId || !span) {
        return;
    }

    scAjax({
        "url": "questions/GetAnswersCount",
        "data": { "questionId": questionId },
        "success": function (result) {
            if (result > 1)
                $(span).html("<b>" + (result || "0") + "</b>" + " answers");
            else
                $(span).html("<b>" + (result || "0") + "</b>" + " answer");
        }
    });
}


