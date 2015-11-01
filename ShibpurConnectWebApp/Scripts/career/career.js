$(document).ready(function () {

    loadJobs(0);

    //$('.pagination li a').click(function () {
    //    var anchor = $(this);
    //    $('.pagination').hide();
    //    $('#threads .thread:not(.hide)').remove();
    //    //$('#loadingdiv').show();
    //    var page = parseInt($(anchor).text());
    //    loadQuestions(page - 1);
    //    $('.pagination li.disabled').removeClass('disabled');
    //    $(anchor).parent().addClass('disabled');
    //    return false;
    //});
});

function loadJobs(pageNumber) {
    $('#currentPage').val(pageNumber);

    var url = window.location.href;
    if (url.indexOf("JobByCategory") > 1) {
        var splitArr = url.split('/');
        var category = getQueryStringParam('category');

        scAjax({
            "url": "career/getjobsbycategory",
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
            "url": "career/getjobs",
            "data": { "page": pageNumber },
            "success": function (result) {
                if (!result) {
                    return;
                }
                // hide the loading symbol
                $('#loadingdiv').hide();
                var allJobsData = result;
                if (pageNumber == 0) {
                    var totalPages = allJobsData[0].totalPages || 0;
                    //buildPagination(totalPages);
                }
                //createQuestions(questions);
                createAllJobs(allJobsData, pageNumber);
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

function createAllJobs(allJobsData, page) {
    if (allJobsData == null)
        return;

    $(allJobsData).each(function (index, job) {
        var htmlItem = $('div.item.hide').clone().removeClass('hide');
        $(htmlItem).find('h2.jobtitle a').text(job.jobTitle).attr("href", "career/jobdetails?jobid=" + job.jobId);

        var creatorImage = $(htmlItem).find('.post-creator-image');
        $(creatorImage).attr("href", "/Account/Profile?userId" + job.userId).css('background-image', "url(http://i.imgur.com/" + job.userProfileImage + ")");

        $(htmlItem).find('a.name-link').text(job.displayName).attr("href", "/Account/Profile?userId=" + job.userId);

        var tempdescription = job.jobDescription;
        if (tempdescription.length > 300) {
            tempdescription = tempdescription.substring(0, 300) + " ...";
        }
        $(htmlItem).find('div.jobdescription p').html(tempdescription);
        $(htmlItem).find('p.designation').text(job.careerDetail);
        $(htmlItem).find('span.post-pub-time').text("Posted on: " + getDateFormattedByMonthYear(job.postedOnUtc));

        // check current job status and print it accordingly
        if (job.hasClosed == false)
            $(htmlItem).find('span.jobstatus').html("<span style='color:green;font-weight:bold;'>Status: Open</span>");
        else {
            $(htmlItem).find('span.jobstatus').html("<span style='color:red;font-weight:bold;'>Status: Closed</span>");
        }

        var skillset = $(htmlItem).find('li.jobskills');
        // add the job skillset tags
        $(job.skillSets).each(function(index) {
            $(skillset).append("<i class='fa fa-tags'>" + job.skillSets[index] +"</i>&nbsp;&nbsp;");
        });

        $("div.job-list").append(htmlItem);
    });
}

