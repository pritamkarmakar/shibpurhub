$(document).ready(function () {
        loadFeeds(0);
    });

function getThreadClass(feedType)
{
    if(feedType == 1) return "question";
    if(feedType == 2) return "answer";
    if (feedType == 10) return "job";
}

function loadFeeds(page) {
	var userDetails = localStorage.getItem("SC_Session_UserDetails");
	var userInfo = $.parseJSON(userDetails);
	var loggedInUserId = userInfo == null ? "" : userInfo.id;

	$('#currentPage').val(page);
	var alreadyShown = parseInt($('#alreadyShown').val()) || 0;

	scAjax({
		"url": "feed/GetPersonalizedFeeds",
		"data": { "loggedInUserId": loggedInUserId, "page": page || 0, "alreadyShown": alreadyShown },
		"success": function (result) {
			if (!result) {
				return;
			}

			var feeds = result.feedItems;
			$('#alreadyShown').val(result.alreadyProcessedItemCount);

			var userIds = [];
			$(feeds).each(function (index, feed) {
				if (feed.itemHeader) {
					var htmlItem = $('div.item.hide').clone().removeClass('hide');

					var creatorImage = $(htmlItem).find('.post-creator-image');
					if(feed.activityType == 10)
					{
						$(creatorImage).removeClass('post-creator-image').addClass('topic-icon-placeholder').html('<i class="fa fa-briefcase"></i>');
						$(creatorImage).attr("href", feed.targetActionUrl);
						$(htmlItem).find('a.name-link').text(feed.itemHeader).attr("href", feed.targetActionUrl).css('font-size', '20px');
						$(htmlItem).find('h2.title').hide();
					}
					else
					{
						$(creatorImage).attr("href", feed.userProfileUrl).css('background-image', "url(http://i.imgur.com/" + feed.userProfileImageUrl + ")");
						$(htmlItem).find('a.name-link').text(feed.userName).attr("href", feed.userProfileUrl);
						$(htmlItem).find('h2.title a').html(feed.itemHeader).attr("href", feed.targetActionUrl);
						$(htmlItem).find('span.action').text(feed.actionText);
						$(htmlItem).find('a.target').text(feed.targetAction).attr("href", feed.targetActionUrl);
					}
					
					$(htmlItem).find('p.designation').text(feed.itemSubHeader);

					$(htmlItem).find('div.post-description p').html(getEmojiedString(feed.itemDetail));

					$(htmlItem).find('div.post-description img').addClass("col-md-12");
					
					$(htmlItem).find('div.thread').addClass(getThreadClass(feed.activityType));

					// 1: Ask question, 2: Answer, 3: Upvote, 4: Comment, 5: Mark as Answer,
					// 6: Register as new user, 7: Follow an user, 8: Follow a question, 9: Update profile image
					if (feed.activityType == 6 || feed.activityType == 7) {
					    $(htmlItem).find('div.thread-details').hide();					    

					    var userId = feed.targetActionUrl.split("=")[1];
					    userIds.push(userId);
					    $(htmlItem).find('div.content').hide().attr("id", "user-" + userId);
					}

					if (feed.activityType == 1 || feed.activityType == 8) {
						$(htmlItem).find('.follow-ul').show();
						var followButton = $(htmlItem).find('.follow-ul a.thumbs');
						if (feed.isFollowedByme) {
							$(followButton).addClass('active');
							$(followButton).find('span').text('Following');
							$(followButton).find('i.fa').removeClass('fa-plus-circle').addClass('fa-check');
						}
						$(followButton).click(function (event) {
							event.preventDefault();

							var button = $(this);
							var textSpan = $(button).find('span');
							var icon = $(button).find('i.fa');
							if ($(button).hasClass('active')) {
								$(button).removeClass('active');
								$(textSpan).text('Follow');
								$(icon).removeClass('fa-check').addClass('fa-plus-circle');
							}
							else {
								$(button).addClass('active');
								$(textSpan).text('Following');
								$(icon).removeClass('fa-plus-circle').addClass('fa-check');
							}
						});
					}

					if (feed.activityType == 2 || feed.activityType == 5) {
						$(htmlItem).find('.upvote-ul').show();
						var upvoteButton = $(htmlItem).find('.upvote-ul a.thumbs');
						if (feed.isUpvotedByme) {
							$(upvoteButton).addClass('active');
							$(upvoteButton).find('span').text('Upvoted');
						}
						$(upvoteButton).click(function (event) {
							event.preventDefault();

							var button = $(this);
							var textSpan = $(button).find('span');

							if ($(button).hasClass('active')) {
								$(button).removeClass('active');
								$(textSpan).text('Upvote');
							}
							else {
								$(button).addClass('active');
								$(textSpan).text('Upvoted');
							}
						});
					}

					$(htmlItem).find('span.view-count').text(feed.viewCount + " views");
					$(htmlItem).find('span.answer-count').text(feed.answersCount + " answers");
					$(htmlItem).find('span.post-pub-time').text(getDateFormattedByMonthYear(feed.postedDateInUTC));

					if (!page) {
						$("div.feed-list").append(htmlItem);
					}
					else {
						$(htmlItem).insertBefore("div.row.load:not('.hide')");
					}
				}
			});

			createUserFeeds(userIds);
			if (!page) {
				var loadMoreDiv = $("div.feed-list .load").clone().removeClass('hide');
				$("div.feed-list").append(loadMoreDiv);

				$('a.loadMore').click(function (event) {
					event.preventDefault();

					$('.toggleThis.loadlabel').hide();
					$('.toggleThis.spinner').show();

					var currentPage = parseInt($('#currentPage').val());
					loadFeeds(currentPage + 1);
				});

				$("#loading").hide();
			}
			else {
				$('.toggleThis.loadlabel').show();
				$('.toggleThis.spinner').hide();
			}
		}
	});
}

function createUserFeeds(userIds)
{
    if(!userIds || userIds.length == 0)
    {
        return;
    }

    scAjax({
        "url": "feed/GetFollowedUserDetails",
        "data": { "userIds": userIds },
        "success": function (result) {
            if (!result) {
                return;
            }

            $(result).each(function (index, user) {
                var userId = user.userId;
                var userDiv = $("#user-" + userId);

                $(userDiv).show();
                var htmlItem = $('div.user-feed.hide').clone().removeClass('hide');
                $(htmlItem).find('h2 a').text(user.fullName);
                $(htmlItem).find('p.tagline-text-content').text(user.careerDetail);

                var userImage = $(htmlItem).find('.user-feed-img');
                $(userImage).css('background-image', "url(http://i.imgur.com/" + user.imageUrl + ")");

                var profileUrl = "/Account/Profile?userId=";
                $(htmlItem).find('.useranswercount a').text(user.answerCount).attr("href", profileUrl + userId + "#tab5");
                $(htmlItem).find('.userquestioncount a').text(user.questionCount).attr("href", profileUrl + userId + "#tab4");
                $(htmlItem).find('.reputationcount span.count').text(user.reputation);

                $(userDiv).html(htmlItem);
            });
        }
    });
}