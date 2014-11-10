var Tries = 3;
// Insert number of questions displayed in answer area
var answers = new Array(6);

// Insert answers to questions
answers[0] = "4";
answers[1] = "P1";
answers[2] = "C (Red)";
answers[3] = "C (Red) and B (Blue)";
answers[4] = "B (Blue) and A (Grey)";
answers[5] = "70";
answers[6] = "Grey";
answers[7] = "Red";
answers[8] = "10";
// Do not change anything below here ...
function getScore(form, numQues, numChoi,offset) {
    var score = 0;
    var currElt;
    var currSelection;
    for (i = 0; i < numQues; i++) {
        currElt = i * numChoi;
        for (j = 0; j < numChoi; j++) {
            currSelection = form.elements[currElt + j];
            if (currSelection.checked) {
                if (currSelection.value == answers[offset+i]) {
                    score++;
                    break;
                }
            }
        }
    }
    score = Math.round(score / numQues * 100);
    var correctAnswers = "";
    for (i = 1; i <= numQues; i++) {
        correctAnswers += i + ". " + answers[offset+ (i - 1)] + "\r\n";
    }
    if (score == 100) {
        alert("\tSuccess!!\t\n\tYou have completed the quiz\t");
        $('#Quiz').hide();
        $('#Quiz2').hide();
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

var sec; //counting the time left to vote
var MaxSec = 30;
var barInterval;
var playerIndex;
var imageClick;
var backFromDown;
var uID;
var game;
var names;
var moregames = true;


$.connection.hub.url = "http://localhost:8010/signalr";


function progressBarFunc(x) {
    var x = x || function () { };
    $("#prog_status").html(MaxSec);
    sec = MaxSec;
    $(".progress_bar").animate({ width: "99%" }, 0, function () {
        $('#timeremaining').show();
        x();
        animate2();
        barInterval = window.setInterval(animate2, 1000);
    });
}

function animate2() {
    $("#prog_status").html(sec);
    sec--;
    val = sec / MaxSec * 100;
    $(".progress_bar").animate({ width: val + "%" }, 1000, "linear");
    if (sec < 0) {
        window.clearInterval(barInterval);
        backFromDown = true;
        $('#' + defaultVote).trigger('mouseup');
    } 
}

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


$(function () {
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
                $('#nextGamestatus').show();
        } else if (msg == "start") {
            if (clock) {
                clock.stop();
                clock = null;
            }
            $('#nextGamestatus').hide();
            $("#MainPage").hide();
            $('#tableContainer').show();
            game.server.gameDetailsMsg($.connection.hub.id);
            $('#turnStatus').text('Please wait for your turn');
        }
    };

    game.client.gameDetails = function(playerI, numOfCandidates, numPlayers, numTurns, candNames, candIndex, point, votes, isVoted, voted, playerString, turn, voting, winner, turnsWait, allpriorities) {
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
            cell11 = $('<td width="25%" align="center" valign="middle"></td>').html('<td id=infopanel>' +
                '<span> There are <span id=numP>' + numPlayers + '</span> players' +
                '<span id=showPlayers>, you are <span id=currplayer>' + playerString + '</span></span></span> <br/>' +
                '<span id="turnsLeft" style="display: none;"> Game ends in: <span id=turnsLeftnum>' + numTurns + '</span> turns' +
                '</span></td>');

            cell12 = $('<td width="50%" align="center" style="vertical-align:middle;"></td>').html(

               '<div id="InGameMessages">' + 
                   '<div><span id="turnStatus"></span></div>' +

                   '<div><span id="voteStatus" style="display: none;">VOTE NOW!</span></div>' +

                   '<div id="waitImg"><img src="images/wait-trans.gif" style="height:inherit;"></div>' +

                   '<div id="timeremaining" style="display: none;">' +
                        '<div class="progress">' +
                        '<div class="progress_bar"></div>' +
                        '<div id="prog_status"></div>' +
                   '</div></div>' +

                   '<div><span id="turnsToWait">Your turn is in <span id="numToWait"></span> steps</span></div>' +
                    
                '</div>' +

                '<div id="mypopupGameOver" class="popup-ui">' +
                    '<div class="popup-ui-wrapper">' +
                        '<div class="popup-ui-content">' +
                    '<div class="ribbon">' +
                        '<h3>GAME OVER!!</h3>' +
                        '<p style="text-align:left;"><br/>' +
                        '<span id="gameOverTxt"></span></p>' +
                        '<div style="text-align: center;padding-bottom: 5px;"><button id="cont" class="small color green button">continue</button></div>' +
                        '</div>' +
                '</div></div></div>' +

                '<div id="mypopupPlayMore" class="popup-ui">' +
                    '<div class="popup-ui-wrapper">' +
                        '<div class="popup-ui-content">' +
                         '<div class="ribbon">' +
                             '<h3>Would you like to play another game?</h3>' +
                             '<p><ul id="nextGameP" class="button-group">' +
                             '<li><button id="nextGameB" class="small color green button">Ready for another game</button></li>' +
                             '<li><button id="QuitNow" class="small color red button">Enough waiting! let me out!</button></li>' +
                             '</ul></p><span id="nextGamestatus" style="display: none;">' +
                             'Please wait for the other players, the game will start shortly...</span>' +
               '</div></div></div></div>' +
                             
                '<div id="mypopupNoMORE" class="popup-ui">' +
                    '<div class="popup-ui-wrapper">' +
                        '<div class="popup-ui-content">' +
                        '<div class="ribbon">' +
                         '<h3>Thank you for participating</h3>' +
                          '<p style="text-align:left;"> You recieved total of: <span id=playerPoints style="color:rgb(247, 110, 246);font-size:larger;"></span> Coins<br/>' +
                          'Your Token is: <span id=gamerID style="color:chartreuse;font-size:larger;"></span></p>' +
                          '<div style="text-align:center;padding-bottom: 5px;"><button id="survy" class="large color red button">Fill our Survy</button></div>' +
                '</div></div></div></div>' +
                
                '<p id="ToWhoYou" style="text-shadow: 2px 1px 2px rgba(150, 150, 150, 1);' +
                'font-size: xx-large; margin-top: 20px;display: none;">You voted for <span id="towhoyouVoted"><b></b></span></p>');
                


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
            var points = point.split('#');
            var numOfVotes = votes.split('#');
            names = candNames.split('#');
            var candIndexes = candIndex.split('#');
            var whoVoted = voted.split("#");


            for (var i = 0; i < numOfCandidates; i++) {
                cell21 = $('<td width=cellWidth + "%" align="center"></td>').html('<label style="font-family:Arial;font-weight:bold;">' + getGetOrdinal(i + 1) + ' priority</label>');
                row21.append(cell21);
                cell22 = $('<td width=cellWidth + "%" align="center"></td>').html('<label style="font-family:Arial;font-weight:bold;">' + points[i + 1] + ' Coins</label>');
                row22.append(cell22);
                var url = "images/user" + (candIndexes[i + 1]) + ".png";

                cell23 = $('<td class="imgCells" align="center" style="height:60%; width:cellWidth + %"></td>');
                imgTable = $('<table width="100%" align="center" style="height:100%;"></table>');
                imgrow = $('<tr width=100% style="height:100%"></tr>');
                imgCell1 = $('<td width="25%" style="height:100%" align="right"></td>');
                imgCell2 = $('<td width="75%" style="height:100%;" align="left" valign="middle"></td>');
                imgCellpicCap = $('<tr></tr>').html('<img class="images" id="' + i + '" src="' + url + '" style=" margin-top:20px; height:80%"><div class="candsNames">' + names[i + 1] + '</div>');
                imgCell2.append(imgCellpicCap);
                pTable = $('<table class="progBars" id="progBars' + i + '"height="100%" width="30%"></table><label class="numVoteslabel" id="progNum' + i + '"></label>');

                var pheight = 100 / numPlayers;
                if (pheight > 1) {
                    for (var k = 0; k < numPlayers; k++) {
                        prow = $('<tr width="100%" style="height:' + pheight + '%"></tr>');
                        pcell = ('<td id="pcell' + i + k + '" width="100%" align="center" style="border: none;"><label class="progLabel" id="plabel' + i + k + '" style="margin-right:0px"></label></td>');
                        prow.append(pcell);
                        pTable.append(prow);
                    }
                } else {
                    prow = $('<tr width="100%" id="pRowTop' + i + '" style="height:' + 34 + '%"></tr>');
                    pcell = ('<td id="Bpcell' + i + '0" width="100%" align="center" style="border: none;"><label class="progLabel" id="plabel" style="margin-right:0px"></label></td>');
                    prow.append(pcell);
                    pTable.append(prow);
                    prow = $('<tr width="100%" id="pRowBottom' + i + '" style="height:' + 33 + '%"></tr>');
                    pcell = ('<td id="Bpcell' + i + '2" width="100%" align="center" style="border: none;"><div style="height: inherit;"></div></td>');
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
            numOfVotes[1] -= 1;
            updateVoteBars(numOfCandidates, whoVoted, winners, numOfVotes, playerString);

            if (turn == 1) {
                imageClick = true;
                $('#waitImg').hide();
                $('#turnsToWait').hide();
                $('#turnStatus').text("It's your turn");
                $('#voteStatus').show();
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
                $('#voteStatus').hide();
                $('#turnsToWait').show();
                //$('#turnStatus').text('Please wait for player ' + (voting + 1) + ' to vote');
                $('#numToWait').text(turnsWait);
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
                document.getElementById('mypopupPlayMore').className = 'popup-ui';
                document.getElementById('mypopupNoMORE').className = 'popup-ui show dark';
                game.server.playerQuits($.connection.hub.id);
            } 
        });
        $('#survy').click(function() {
            if (QueryString['workerId'])
                window.open('https://docs.google.com/forms/d/1JPqyhlU-kuRE9XGGKD4_lBCk5FCv9iSbTbKMiHNzHLs/viewform?entry.683314448=' + uID + '&entry.641831269=' + QueryString['workerId']);
            else
                window.open('https://docs.google.com/forms/d/1JPqyhlU-kuRE9XGGKD4_lBCk5FCv9iSbTbKMiHNzHLs/viewform?entry.683314448=' + uID);
        });
        $('#cont').click(function() {
            document.getElementById('mypopupGameOver').className = 'popup-ui';
            if (moregames)
                document.getElementById("mypopupPlayMore").className = 'popup-ui show dark';
            else {
                document.getElementById('mypopupNoMORE').className = 'popup-ui show dark';
            }
        });
        
    }; //details function

    game.client.yourTurn = function() {
        progressBarFunc(function () {
            $('#waitImg').hide();
            $('#turnsToWait').hide();
            $(".images").each(function () {
                var originalSrc = $(this).attr("src");
                var imgsrc = "images/user" + parseInt(originalSrc.substring(11, 12)) + ".png";
                $(this).attr("src", imgsrc);
            });
            imageClick = true;
            $('#turnStatus').text("It's your turn");
            $('#voteStatus').show();
            $(".images").css('cursor', 'pointer');
            $('.images').hover(
                function (event) {
                    var originalSrc = $(this).attr("src");
                    var srcon = "images/user" + parseInt(originalSrc.substring(11, 12)) + "_hover.png";
                    $("#" + event.target.id).attr("src", srcon);
                },
                function (event) {
                    var originalSrc = $(this).attr("src");
                    var imgsrc = "images/user" + parseInt(originalSrc.substring(11, 12)) + ".png";
                    $("#" + event.target.id).attr("src", imgsrc);
                }
            );
        });
        
    };

    game.client.votedUpdate = function(numOfCandidates, votes, votesL, turnsL, defaultCand, voting, winner, whoVoted, playerString, turnsWait) {
        defaultVote = defaultCand;
        $('#turnsToWait').show();
        $('#numToWait').text(turnsWait);
        //$('#turnStatus').text('Please wait for player ' + (voting + 1) + ' to vote');
        //$('#votesLeft').text('Remaining votes: ' + votesL);
        $('#turnsLeftnum').text(turnsL);
        var numOfVotes = votes.split("#");
        var winners = winner.split("#");
        var whoVotedString = whoVoted.split("#");
        updateVoteBars(numOfCandidates, whoVotedString, winners, numOfVotes, playerString);


    };

    game.client.otherVotedUpdate = function(numOfCandidates, votes, votesL, turnsL, voting, winner, whoVoted, playerString, turnswait) {
        //$('#turnStatus').text('Please wait for player ' + (voting + 1) + ' to vote');
        //$('#votesLeft').text('Remaining votes: ' + votesL);
        $('#turnsLeftnum').text(turnsL);
        var numOfVotes = votes.split("#");
        var winners = winner.split("#");
        var whoVotedString = whoVoted.split("#");
        updateVoteBars(numOfCandidates, whoVotedString, winners, numOfVotes, playerString);

        $('#turnsToWait').show();
        $('#numToWait').text(turnswait);

    };

    game.client.gameOver = function(numOfCandidates, votes, votesL, turnsL, points, winner, currentWinner, whoVoted, playerString) {
        imageClick = false;
        $('#InGameMessages').hide();
        $(".images").css('cursor', 'auto');
        $(".images").hover().unbind();
        //$('#votesLeft').text('Remaining votes: ' + votesL);
        $('#turnsLeftnum').text(turnsL);
        var numOfVotes = votes.split("#");
        var winners = currentWinner.split("#");
        var whoVotedString = whoVoted.split("#");
        clearInterval(barInterval);
        $('#timeremaining').hide();
        $('#voteStatus').hide();
        updateVoteBars(numOfCandidates, whoVotedString, winners, numOfVotes, playerString);

        var pPoints = points.split('#');
        $('#waitImg').hide();
        $('#turnsToWait').hide();
        $('#turnStatus').hide();
        if (winner.indexOf(',') >= 0)
            $('#gameOverTxt').html('The winning candidates are: <b>' + winner + '</b><br/> Your score is: ' + pPoints[playerIndex + 1]);
        else
            $('#gameOverTxt').html('The winning candidate is: <b>' + winner + '</b><br/> Your score is: ' + pPoints[playerIndex + 1]);
        game.server.hasNextGame($.connection.hub.id);
        document.getElementById('mypopupGameOver').className = 'popup-ui' + ' show';
    };

    game.client.showNextGame = function (hasNext, userID, totalpoints, mturkToken) {
        moregames = hasNext;
        uID = userID;
        $('#gamerID').text(mturkToken).css('font-weight', 'bold');
        $('#playerPoints').text(totalpoints).css('font-weight', 'bold');
        if (!hasNext) {
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
                $('#pcell' + i + j).removeClass('Vote');
                $('#plabel' + i + j).text("");
            }
        }
    }

    function updateVoteBars(numOfCandidates, whoVoted, winners, numOfVotes, playerString) {
        resetVoteBars(numOfCandidates);
        for (var i = 0; i < numOfCandidates; i++) {
            var cVoted = whoVoted[i + 1].split(',');
            if (winners.indexOf(i.toString()) != -1)
                $('#progBars' + i).css({
                    "border-color": "gold",
                    "-moz-box-shadow": "inset 2px 2px 2px 2px #888, 0 0 30px gold",
                    "-webkit-box-shadow": "inset 2px 2px 2px 2px #888, 0 0 30px gold",
                    "box-shadow": "inset 2px 2px 2px 2px #888, 0 0 30px gold"
                });

            else {
                $('#progBars' + i).css({
                    "border-color": "#c0c0c0",
                    "-moz-box-shadow": "inset 2px 2px 2px 2px #888 ",
                    "-webkit-box-shadow": "inset 2px 2px 2px 2px #888",
                    "box-shadow": "inset 2px 2px 2px 2px #888"
                });
            }
            if (numOfPlayers <= 30) {
                for (var j = numOfPlayers - 1, k = 0; j >= (numOfPlayers - numOfVotes[i + 1]), k < cVoted.length; j--, k++) {
                    if (cVoted[k] != "") {
                        $('#pcell' + i + j).addClass('Vote');
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
                    }
                }
                $('#progNum' + i).text(numOfVotes[i + 1]);
                $('#progNum' + i).css('margin-right', '10px');
            } else {
                $("#pRowTop" + i).css('height', numOfPlayers - numOfVotes[i + 1] + "%");
                $("#pRowBottom" + i ).css('height', numOfVotes[i + 1] + "%");
                if (numOfVotes[i + 1] > 0) {
                    $('#Bpcell' + i + 2).find('div').html(numOfVotes[i + 1] + " Votes").css('color', 'whitesmoke').css('height','100%');
                }
                //$('#pcell' + i + 2).css('background-image', "url('images/progressBar2p2.png')");
                //$('#plabel' + i + 2).css('font-weight', 'normal').css('font-size', 'smaller');
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
        $('#timeremaining').hide();
        $('#voteStatus').hide();
        $('#turnStatus').text('Please wait for your turn');
        $('#waitImg').show();
        imageClick = false;
        backFromDown = false;
        $(".images").css('cursor', 'auto');
        var duration = (MaxSec - sec);
        if (duration < 0)
            duration = 0;
        $("#ToWhoYou").show();
        $("#towhoyouVoted").text(names[parseInt(event.target.id)+1] );
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
