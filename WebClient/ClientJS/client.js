var Tries = 3;
// Insert number of questions
var numQues = 6;
// Insert number of choices in each question
var numChoi = 4;
// Insert number of questions displayed in answer area
var answers = new Array(6);

// Insert answers to questions
answers[0] = "4";
answers[1] = "P1";
answers[2] = "C (Red)";
answers[3] = "C (Red) and B (Blue)";
answers[4] = "B (Blue) and A (Grey)";
answers[5] = "70";

// Do not change anything below here ...
function getScore(form) {
    var score = 0;
    var currElt;
    var currSelection;
    for (i = 0; i < numQues; i++) {
        currElt = i * numChoi;
        for (j = 0; j < numChoi; j++) {
            currSelection = form.elements[currElt + j];
            if (currSelection.checked) {
                if (currSelection.value == answers[i]) {
                    score++;
                    break;
                }
            }
        }
    }
    score = Math.round(score / numQues * 100);
    var correctAnswers = "";
    for (i = 1; i <= numQues; i++) {
        correctAnswers += i + ". " + answers[i - 1] + "\r\n";
    }
    if (score == 100) {
        alert("\tSuccess!!\t\n\tYou have completed the quiz\t");
        $('#Quiz').hide();
        $('#MainPage').show();
    } else {
        Tries--;
        if (Tries == 0)
            $('body').replaceWith("<div><h1 style=\"text-align: center;\">You have failed passing this quiz please return your HIT</h1></div>");
        else {
            if (Tries == 1)
                alert("\tFAILED!\t (" + score + "% correct answers)\n\t Final try\t");
            else
                alert("\tFAILED!\t (" + score + "% correct answers)\n\t" + Tries + " tries left..\t");
            document.forms.quiz.reset();
        }
    }
}
var defaultVote;
    function progressBarFunc() {
        $('#secbar').progressbar({ value: '105' });
        $('#secbar').show();
        sec = 30;
        var val = 105;
        barInterval = setInterval(function() {
            val = val - 3.5;
            sec--;
            $('#secbar').progressbar({
                value: val,
                change: function() {
                    $('.progress-label').text(sec);
                },
                complete: function() {}
            });
            if (sec == 0) {
                backFromDown = true;
                $('#' + defaultVote).trigger('mouseup');
            }

        }, 1000);
    };

$.connection.hub.url = "http://localhost:8010/signalr";
var sec; //counting the time left to vote
var barInterval;
var playerIndex;
var imageClick;
var backFromDown;
var game;

var QueryString = function() {
    // This function is anonymous, is executed immediately and 
    // the return value is assigned to QueryString!
    var query_string = {};
    var query = window.location.search.substring(1);
    var vars = query.split("&");
    for (var i = 0; i < vars.length; i++) {
        var pair = vars[i].split("=");
        // If first entry with this name
        if (typeof query_string[pair[0]] === "undefined") {
            query_string[pair[0]] = pair[1];
            // If second entry with this name
        } else if (typeof query_string[pair[0]] === "string") {
            var arr = [query_string[pair[0]], pair[1]];
            query_string[pair[0]] = arr;
            // If third or later entry with this name
        } else {
            query_string[pair[0]].push(pair[1]);
        }
    }
    return query_string;
}();
var getGetOrdinal = function(n) {
    var s = ["th", "st", "nd", "rd"],
        v = n % 100;
    return n + (s[(v - 20) % 10] || s[v] || s[0]);
};
$(function() {
    //Set the hubs URL for the connection
    var numOfPlayers = 0;
    var showWhoVoted = 0;
    var clock; //The Clock Object


    // Declare a proxy to reference the hub.

    game = $.connection.serverHub;
    if (game == null) {
        $('body').replaceWith("<div><h1 style=\"text-align: center;\">We are sorry!<br/> There seems to be a problem with the server.</h1></div>");
        return;
    }
    game.client.startGameMsg = function(msg) {

        if (msg == "wait") {
            if ($('#MainPage').is(":visible")) {
                $('#StatusInfo').show();
                $('#StatusInfo').addClass("pullDown");
                $('#TimePassed').show();
                $('#TimePassed').addClass("pullDown");
                clock = $('.clock').FlipClock({ clockFace: 'MinuteCounter' });
            } else
                $('#nextGamestatus').text("Please wait for the other players, the game will start shortly...");
        } else if (msg == "start") {
            if (clock) {
                clock.stop();
                clock = null;
            }
            $('#nextGamestatus').text('');
            $("#MainPage").hide();
            $('#tableContainer').show();
            game.server.gameDetailsMsg($.connection.hub.id);
            $('#turnStatus').text('Please wait for your turn');
        }
    };

    game.client.gameDetails = function(playerI, numOfCandidates, numPlayers, numTurns, candNames, candIndex, point, votes, isVoted, voted, playerString, turn, voting, winner, turnsWait, allpriorities) {
        //$('playerTurn').val(turn);
        $("#tableContainer").empty();
        numOfPlayers = numPlayers;
        playerIndex = playerI;
        showWhoVoted = isVoted;
        imageClick = false;
        backFromDown = false;

        defaultVote = 0;
        //create table
        $(function() {
            //first table
            var table1 = $('<table id=topTable></table>');
            row11 = $('<tr border="0" width="100%"></tr>');
            cell11 = $('<td width="25%" align="left" valign="middle"></td>').html('<td id=infopanel>' +
                '<label style="font-family:Arial"> There are <label id=numP>' + numPlayers + '</label> players' +
                '<label id=showPlayers>, you are <label id=currplayer>' + playerString + '</label></label></label> <br/>' +
                ' <label id="turnsLeft" style="font-family:Arial" hidden> Game ends in: <label id=turnsLeftnum>' + numTurns + '</label> turns' +
                '</label></td>');

            cell12 = $('<td width="50%" align="center" style="vertical-align:middle;"></td>').html(
                '<p><label id="turnStatus"></label></p>' +
                '<p><label id="voteStatus"></label></p>' +
                '<p id="waitImg"><img src="images/wait-trans.gif" style="height:inherit;"></p>' +
                '<p><label id="turnsToWait">Your turn is in <label id="numToWait"></label> steps</label></p>' +
                '<p><label id="playerVoted"></label></p>' +
                '<p><label id="gameOverTxt" hidden></label></p>' +
                '<p id="nextGameP" hidden><input id="nextGameB" type="button" value="Ready for another game" />' +
                '<input id="QuitNow" type="button" value="Enough waiting! Let me out!" /><br/>' +
                '<div id="noMoreGames" hidden>No more games.<p id=gamerID></p><p> you recieved Total of: <label id=playerPoints></label> Coins</p></div>' +
                '<p id="survy" hidden><a href="#" class="survyB" target="_blank">Fill our Survy</a></p>' +
                '<label id="nextGamestatus"></label>' +
                '<div id="secbar"><div class="progress-label">' +
                '</div></div>');
            cell13 = $('<td width="25%" align="middle" valign="middle"></td>');
            if (showWhoVoted == 2) {
                var priolist = allpriorities.split('#');
                var prefTable = '<div class=prefTable style="width: 50%;"><b>Global Preference Table</b><table><tr><td style="width: 30%;">Voter</td>';
                var s = ["th", "st", "nd", "rd"];
                for (var i = 1; i <= numOfCandidates; i++)
                    prefTable += '<td>' + getGetOrdinal(i) + '</td>';

                prefTable += '</tr>';
                var prionames = priolist[playerI].split(',');
                prefTable += '<tr><td>p' + (playerI + 1) + '<b> (Me)</b></td>';
                for (var j = 0; j < prionames.length; j++)
                    prefTable += '<td style="color:' + prionames[j] + ';">' + prionames[j] + '</td>';
                prefTable += '</tr>';
                for (var i = 0; i < priolist.length; i++) {
                    prionames = priolist[i].split(',');
                    if (i == playerI)
                        continue;
                    else
                        prefTable += '<tr><td>p' + (i + 1) + '</td>';
                    for (var j = 0; j < prionames.length; j++)
                        prefTable += '<td style="color:' + prionames[j] + ';">' + prionames[j] + '</td>';
                    prefTable += '</tr>';
                }
                prefTable += '</tbody></table></div>';
                cell13.append(prefTable);
            }
            table1.append(row11);
            row11.append(cell11);
            row11.append(cell12);
            row11.append(cell13);
            $('#tableContainer').append(table1);

            //second table
            var table2 = $('<table id=bottomTable></table>');
            row21 = $('<tr width="100%" style="height:10%"></tr>'); //priority
            row22 = $('<tr width="100%" style="height:10%"></tr>'); //points
            row23 = $('<tr width="100%" style="height:60%"></tr>'); //pic
            row24 = $('<tr width="100%" style="height:20%"></tr>');
            var cellWidth = 100 / numOfCandidates;
            var points = point.split('#');
            var numOfVotes = votes.split('#');
            var names = candNames.split('#');
            var candIndexes = candIndex.split('#');
            var whoVoted = voted.split("#");


            for (var i = 0; i < numOfCandidates; i++) {
                cell21 = $('<td width=cellWidth + "%" align="center"></td>').html('<label style="font-family:Arial;font-weight:bold;">' + getGetOrdinal(i + 1) + ' priority</label>');
                row21.append(cell21);
                cell22 = $('<td width=cellWidth + "%" align="center"></td>').html('<label style="font-family:Arial;font-weight:bold;">' + points[i + 1] + ' Coins</label>');
                row22.append(cell22);
                var url = "images/user" + (candIndexes[i + 1]) + ".png";
                var size = 80; // - (8 * i);                      
                cell23 = $('<td class="imgCells" align="center" style="height:60%; width:cellWidth + %"></td>');
                imgTable = $('<table width="100%" align="center" style="height:100%;"></table>');
                imgrow = $('<tr width=100% style="height:100%"></tr>');
                imgCell1 = $('<td width="25%" style="height:100%" align="right"></td>');
                imgCell2 = $('<td width="75%" style="height:100%;" align="left" valign="middle"></td>');
                imgCellpicCap = $('<tr></tr>').html('<img class="images" id="' + i + '" src="' + url + '" style=" margin-top:20px; height:' + size + '%"><div class="candsNames">' + names[i + 1] + '</div>');
                imgCell2.append(imgCellpicCap);
                pTable = $('<table class="progBars" id="progBars' + i + '" width="30%" style="height:93%"></table><label class="numVoteslabel" id="progNum' + i + '"></label>');
                var pheight = 100 / numPlayers;
                for (var k = 0; k < numPlayers; k++) {
                    prow = $('<tr width="100%" style="height:' + pheight + '%"></tr>');
                    pcell = ('<td id="pcell' + i + k + '" width="100%" align="center" style="border: none;"><label class="progLabel" id="plabel' + i + k + '" style="margin-right:0px"></label></td>');
                    prow.append(pcell);
                    pTable.append(prow);
                }
                imgCell1.append(pTable);
                imgrow.append(imgCell1);
                imgrow.append(imgCell2);
                imgTable.append(imgrow);

                cell23.append(imgTable);
                row23.append(cell23);
            }


            table2.append(row21);
            table2.append(row22);
            table2.append(row23);
            table2.append(row24);
            $('#tableContainer').append(table2);

            var winners = winner.split("#");
            updateVoteBars(numOfCandidates, whoVoted, winners, numOfVotes, playerString);

            if (turn == 1) {
                imageClick = true;
                $('#waitImg').hide();
                //$('#playerVoted').text('');
                $('#turnsToWait').hide();
                $('#turnStatus').text("It's your turn");
                $('#voteStatus').text('VOTE NOW!');
                $(".images").css('cursor', 'pointer');
                $('.images').hover(
                    function(event) {
                        var originalSrc = $(this).attr("src");
                        var srcon = "images/user" + parseInt(originalSrc.substring(11, 12)) + "_hover.png";
                        $("#" + event.target.id).attr("src", srcon);
                    },
                    function(event) {
                        var originalSrc = $(this).attr("src");
                        var imgsrc = "images/user" + parseInt(originalSrc.substring(11, 12)) + ".png";
                        $("#" + event.target.id).attr("src", imgsrc);
                    }
                );
                progressBarFunc();
            } else {
                imageClick = false;
                $('#turnsToWait').show();
                $('#turnStatus').text('Please wait for player ' + (voting + 1) + ' to vote');
                $('#numToWait').text(turnsWait);
                $('#voteStatus').text('');
                $('#waitImg').show();
                $(".images").css('cursor', 'auto');
            }
        });
        $('#nextGameB').click(function() {
            $('#nextGameB').prop('disabled', true);
            game.server.connectMsg('connect', $.connection.hub.id, QueryString);
        });

        $('#QuitNow').click(function () {
            var r = confirm("Are you sure? There can still earn some money..");
            if (r) {
                game.server.playerQuits($.connection.hub.id);
            } 
        });
    }; //details function

    game.client.yourTurn = function() {
        imageClick = true;
        $('#waitImg').hide();
        $(".images").each(function() {
            var originalSrc = $(this).attr("src");
            var imgsrc = "images/user" + parseInt(originalSrc.substring(11, 12)) + ".png";
            $(this).attr("src", imgsrc);
        });
        //$('#playerVoted').text('');
        $('#turnsToWait').hide();
        $('#turnStatus').text("It's your turn");
        $('#voteStatus').text('VOTE NOW!');
        //$(".images").fadeTo("fast", 1);
        $(".images").css('cursor', 'pointer');
        $('.images').hover(
            function(event) {
                var originalSrc = $(this).attr("src");
                var srcon = "images/user" + parseInt(originalSrc.substring(11, 12)) + "_hover.png";
                $("#" + event.target.id).attr("src", srcon);
            },
            function(event) {
                var originalSrc = $(this).attr("src");
                var imgsrc = "images/user" + parseInt(originalSrc.substring(11, 12)) + ".png";
                $("#" + event.target.id).attr("src", imgsrc);
            }
        );
        progressBarFunc();
    };

    game.client.votedUpdate = function(numOfCandidates, votes, votesL, turnsL, defaultCand, voting, winner, whoVoted, playerString, turnsWait) {
        defaultVote = defaultCand;
        $('#turnsToWait').show();
        $('#numToWait').text(turnsWait);
        //$('#playerVoted').text('player ' + (playerV + 1) + ' voted');
        $('#turnStatus').text('Please wait for player ' + (voting + 1) + ' to vote');
        //$('#votesLeft').text('Remaining votes: ' + votesL);
        $('#turnsLeftnum').text(turnsL);
        var numOfVotes = votes.split("#");
        var winners = winner.split("#");
        var whoVotedString = whoVoted.split("#");

        resetVoteBars(numOfCandidates);
        updateVoteBars(numOfCandidates, whoVotedString, winners, numOfVotes, playerString);


    };

    game.client.otherVotedUpdate = function(numOfCandidates, votes, votesL, turnsL, voting, winner, whoVoted, playerString, turnswait) {
        $('#turnStatus').text('Please wait for player ' + (voting + 1) + ' to vote');
        //$('#votesLeft').text('Remaining votes: ' + votesL);
        $('#turnsLeftnum').text(turnsL);
        var numOfVotes = votes.split("#");
        var winners = winner.split("#");
        var whoVotedString = whoVoted.split("#");

        resetVoteBars(numOfCandidates);

        updateVoteBars(numOfCandidates, whoVotedString, winners, numOfVotes, playerString);

        $('#turnsToWait').show();
        $('#numToWait').text(turnswait);
        //$('#playerVoted').text('player ' + (playerV+1) + ' voted');

    };

    game.client.gameOver = function(numOfCandidates, votes, votesL, turnsL, points, winner, currentWinner, whoVoted, playerString) {
        imageClick = false;
        $(".images").css('cursor', 'auto');
        $(".images").hover().unbind();
        //$('#votesLeft').text('Remaining votes: ' + votesL);
        $('#turnsLeftnum').text(turnsL);
        var numOfVotes = votes.split("#");
        var winners = currentWinner.split("#");
        var whoVotedString = whoVoted.split("#");


        resetVoteBars(numOfCandidates);

        updateVoteBars(numOfCandidates, whoVotedString, winners, numOfVotes, playerString);

        var pPoints = points.split('#');
        $('#waitImg').hide();
        $('#turnsToWait').hide();
        //$('#playerVoted').text('');
        $('#turnStatus').text('GAME OVER!!!');
        if (winner.indexOf(',') >= 0)
            $('#gameOverTxt').html('The winning candidates are: <i><b>' + winner + '</b></i><br/> Your score is: ' + pPoints[playerIndex + 1]);
        else
            $('#gameOverTxt').html('The winning candidate is: <i><b>' + winner + '</b></i><br/> Your score is: ' + pPoints[playerIndex + 1]);
        //main.server.connectMsg('connect', $.connection.hub.id);
        $('#gameOverTxt').show();
        game.server.hasNextGame($.connection.hub.id);
    };

    game.client.showNextGame = function (hasNext, userID, points, mturkToken) {
        if (hasNext) {
            $('#nextGameP').show();
        } else {
            $('#nextGameP').hide();
            $('#QuitNow').hide();
            $('#nextGamestatus').hide();
            $('#noMoreGames').show();
            $('#survy').show();
            if (mturkToken)
                $('#gamerID').text('Your mTurk Token is: ' + mturkToken).css('font-weight', 'bold');
            else {
                $('#gamerID').text('Your gamer id is ' + userID + ' (' + $.connection.hub.id + ')').css('font-weight', 'bold');
            }
            $('#playerPoints').text(points).css('font-weight', 'bold').css('font-size', 'larger').css('color', 'mediumvioletred');
            $('.survyB').attr('href', 'https://docs.google.com/forms/d/1RFKflpeYkfWApm1tYqouKvv75cz_pS2S0ZusBfTCPsI/viewform?entry.683314448=' + userID + '&entry.641831269=' + $.connection.hub.id);
            $.connection.hub.stop();
        }
    };

    game.client.updateWaitingRoom = function (waitingFor, totalOnline, avgTime) {
        if (waitingFor == 0)
            $('#waitStatus').html("Game will commence shortly!");
        else
            $('#waitStatus').html("Please Wait for <span style='font-weight: bolder;color: rgb(132, 171, 241);font-size: x-large;'>" + waitingFor + "</span> more players");
        $('#playersOnline').text(totalOnline);
        $('#avgWaitTime').text(avgTime);
    };

    function resetVoteBars(numOfCandidates) {
        for (var i = 0; i < numOfCandidates; i++) {
            for (var j = 0; j < numOfPlayers; j++) {
                $('#pcell' + i + j).css('background-image', 'none');
                $('#plabel' + i + j).text("");
            }
        }
    }

    function updateVoteBars(numOfCandidates, whoVoted, winners, numOfVotes, playerString) {
        for (var i = 0; i < numOfCandidates; i++) {
            var cVoted = whoVoted[i + 1].split(',');
            var glow = 0;
            for (var j = 1; j < winners.length; j++) {
                if (winners[j] == i)
                    glow = 1;
            }

            for (var j = numOfPlayers - 1, k = 0; j >= (numOfPlayers - numOfVotes[i + 1]), k < cVoted.length; j--, k++) {
                if (cVoted[k] != "") {
                    if (glow == 1) {
                        $('#progBars' + i).css('border-color', '#FFCC00');
                        $('#progBars' + i).css('border-width', '2px');
                        $('#progBars' + i).css('box-shadow', '0 0 15px #FFCC00');
                    } else {
                        $('#progBars' + i).css('border-color', '#C0C0C0');
                        $('#progBars' + i).css('border-width', '2px');
                        $('#progBars' + i).css('box-shadow', 'none');
                    }
                    if (playerString == cVoted[k]) {
                        $('#pcell' + i + j).css('background-image', "url('images/progressBar2green2.png')");
                        $('#plabel' + i + j).css('font-weight', 'bold').css('font-size', 'large');
                    } else {
                        $('#pcell' + i + j).css('background-image', "url('images/progressBar2p2.png')");
                        $('#plabel' + i + j).css('font-weight', 'normal').css('font-size', 'smaller');
                    }

                    if (showWhoVoted > 0)
                        $('#plabel' + i + j).text(cVoted[k]);
                } else {
                    $('#progBars' + i).css('border-color', '#C0C0C0');
                    $('#progBars' + i).css('border-width', '2px');
                    $('#progBars' + i).css('box-shadow', 'none');
                }
            }

            if (numOfVotes[i + 1] < 10) {
                $('#progNum' + i).text(numOfVotes[i + 1]);
                $('#progNum' + i).css('margin-right', '10px');
            } else if (numOfVotes[i + 1] < 100) {
                $('#progNum' + i).text(numOfVotes[i + 1]);
                $('#progNum' + i).css('margin-right', '8px');
            }

        }
    }

    $.connection.hub.start().done(function() {
        $('#StartGame').click(function() { // Call the Send method on the hub.
            $('#StartGame').prop('disabled', true);
            game.server.connectMsg('connect', $.connection.hub.id, QueryString);
        });
    });
});
$(document).on('mousedown', '.images', function(event) {
    if (imageClick) {
        $(".images").hover().unbind();
        //$(".images:not(#" + event.target.id + ")").fadeTo("fast", 0.5);
        //$("#" + event.target.id).fadeTo("fast", 1);
        var originalSrc = $(this).attr("src");
        var srcClick = "images/user" + parseInt(originalSrc.substring(11, 12)) + "_click.png";
        $("#" + event.target.id).attr("src", srcClick);
        backFromDown = true;
    }
});
$(document).on('mouseup', '.images', function(event) {
    if (imageClick && backFromDown) {
        var originalSrc = $(this).attr("src");
        var srcClick = "images/user" + parseInt(originalSrc.substring(11, 12)) + "_hover.png";
        $("#" + event.target.id).attr("src", srcClick);

        clearInterval(barInterval);
        $('#secbar').hide();
        $('#turnStatus').text('Please wait for your turn');
        $('#waitImg').show();
        //$('#test').text('');
        $('#voteStatus').text('');
        imageClick = false;
        backFromDown = false;
        $(".images").css('cursor', 'auto');
        var duration = (30 - sec);
        if (duration < 0)
            duration = 0;
        game.server.voteDetails($.connection.hub.id, playerIndex, event.target.id, duration);
    }
});
    window.onbeforeunload = function(e) {
        e = e || window.event;

        // For IE and Firefox prior to version 4
        if (e) {
            e.returnValue = 'Leaving will lose all your progress?';
        }

        // For Safari
        return 'Leaving will lose all your progress?';
    };
