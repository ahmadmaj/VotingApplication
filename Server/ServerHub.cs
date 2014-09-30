using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Server
{
    [HubName("serverHub")]
    public class ServerHub : Hub
    {
        public override Task OnDisconnected()
        {
            Game theGame = Program.getplayersGame(Context.ConnectionId);
            UserVoter removedVoter = null;
            int playerIndex = -1;
            var sb = new StringBuilder();
            if (Program.ConnIDtoUser.TryGetValue(Context.ConnectionId, out removedVoter))
            {
                playerIndex = removedVoter.inGameIndex;
                sb.AppendFormat("Disconnected: Player {0} ({1}). ", removedVoter.userID,
                    removedVoter.mTurkID != "" ? removedVoter.mTurkID : Context.ConnectionId);
                if (removedVoter.CurrGame != null)
                    sb.AppendFormat("Left game {0}.", removedVoter.CurrGame.gameID);
                Console.WriteLine(sb);
                WaitingRoom.RemoveFromWaitingR(Context.ConnectionId);
                Program.ConnIDtoUser.Remove(Context.ConnectionId);
            }
            waitingRoomStats();

            if (Program.PlayingGames.Count > 0)
                if (theGame != null && !theGame.gameOver)
                {
                    theGame.replacePlayer(playerIndex, removedVoter);
                    theGame.deletePlayerID(Context.ConnectionId);
                    if (theGame.playersID.Count == 0)
                        theGame.endGame();
                    else if (theGame.playersID.Count > 0 && theGame.turn == playerIndex)
                    {
                        int next = theGame.getNextTurn();
                        if (next != -1 && theGame.playersID.Count > 0)
                        {
                            if (theGame.playersID.Count > 0)
                            {
                                if (theGame.playersID.Count - 1 == theGame.humanTurn ||
                                    theGame.playersID.Count <= theGame.humanTurn)
                                {
                                    theGame.humanTurn = 0;
                                    updateOtherPlayers(theGame, Context.ConnectionId, playerIndex, next);
                                    updatePlayer(theGame, Context.ConnectionId, playerIndex, next);
                                }
                                else
                                {
                                    updateOtherPlayers(theGame, Context.ConnectionId, playerIndex, next);
                                    updatePlayer(theGame, Context.ConnectionId, playerIndex, next);
                                }
                            }
                        }
                        else
                            gameOver(theGame, Context.ConnectionId, playerIndex);
                    }
                    else
                    {
                        if (theGame.playersID.Count <= theGame.humanTurn)
                            theGame.humanTurn = 0;
                    }
                }
            WaitingRoom.checkToStartGame();
            return null;
        }

        //sent when a client wants to start a game
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ConnectMsg(string msg, string id, Newtonsoft.Json.Linq.JContainer data)
        {
            //Program.awaitingPlayersID.Add(id);
            UserVoter newplayer;
            if (!Program.ConnIDtoUser.TryGetValue(id, out newplayer))
            {
                newplayer = data.Count != 0 ? new UserVoter(id, data["workerId"].ToString(), data["assignmentId"].ToString()) : new UserVoter(id);
                Program.ConnIDtoUser.Add(id, newplayer);
            }

            if (WaitingRoom.joinWaitingRnStart(newplayer))
                foreach (Game playingGame in Program.PlayingGames)
                    sendStartToPlayers(playingGame);
            if (newplayer.CurrGame == null)
            {
                Clients.Client(id).StartGameMsg("wait");
                waitingRoomStats();
            }
        }
        private string ToPrettyFormat(TimeSpan span)
        {
            if (span == TimeSpan.Zero) return "0 seconds";
            var sb = new StringBuilder();
            if (span.Days > 0)
                sb.AppendFormat("{0} day{1} ", span.Days, span.Days > 1 ? "s" : String.Empty);
            if (span.Hours > 0)
                sb.AppendFormat("{0} hour{1} ", span.Hours, span.Hours > 1 ? "s" : String.Empty);
            if (span.Minutes > 0)
                sb.AppendFormat("{0} minute{1} ", span.Minutes, span.Minutes > 1 ? "s" : String.Empty);
            if (span.Seconds > 0)
                sb.AppendFormat("{0} second{1} ", span.Seconds, span.Seconds > 1 ? "s" : String.Empty);
            return sb.ToString();
        }
        private void waitingRoomStats()
        {
            long averageTicks = WaitingRoom.usersWaitTimes.Count > 0
                ? Convert.ToInt64(WaitingRoom.usersWaitTimes.Average(timeSpan => timeSpan.Ticks))
                : 0;
            int waitingFor = 0;
            if (!Program.PlayingGames.Any())
                waitingFor = Program.gameDetails.numOfHumanPlayers - WaitingRoom.waitingPlayers.Count;
            Clients.All.updateWaitingRoom(waitingFor, Program.ConnIDtoUser.Count, ToPrettyFormat(new TimeSpan(averageTicks)));
        }
        public void sendStartToPlayers(Game gamestart)
        {
            if (!Program.mTurkMode)
                MessageBox.Show("Game " + gamestart.gameID + " Full Press OK to start", "Game " + gamestart.gameID + " Ready", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            foreach (string player in gamestart.playersID)
            {
                if (Program.ConnIDtoUser[player].waitDuration == null)
                {
                    TimeSpan waitTemp = DateTime.Now - Program.ConnIDtoUser[player].ConnectTime;
                    WaitingRoom.usersWaitTimes.Add(waitTemp);
                    Program.ConnIDtoUser[player].waitDuration = waitTemp;
                }
                Clients.Client(player).StartGameMsg("start");
            }
        }
        
        //sent when the client loads the game page
        public void GameDetailsMsg(string connectionId)
        {
            UserVoter playerUser = Program.ConnIDtoUser[connectionId];
            Game thegame = Program.getplayersGame(connectionId);
            int playerIndex = playerUser.inGameIndex;
            int turn = thegame.getTurn(connectionId);

            Clients.Client(connectionId).GameDetails(playerIndex, thegame.numOfCandidates, thegame.getNumOfPlayers(), thegame.getTurnsLeft(), thegame.createCandNamesString(playerUser), playerUser.currPriToString(), thegame.createPointsString(), thegame.createNumOfVotesString(playerUser), thegame.whoVoted, thegame.createWhoVotedString(playerIndex), ("p" + (playerIndex + 1)), turn, thegame.turn, thegame.getCurrentWinner(playerIndex), thegame.turnsToWait(playerIndex), thegame.prioritiesJSON);
        }

        public void hasNextGame(string id)
        {
            UserVoter tmpvoter;
            if (Program.ConnIDtoUser.TryGetValue(id, out tmpvoter))
            {
                Boolean chk = tmpvoter.GamesHistory.Count != Program.gameDetailsList.Count;
                Clients.Client(id).showNextGame(chk, tmpvoter.userID,tmpvoter.CurrScore, tmpvoter.mTurkToken);
                if (!chk) Program.ConnIDtoUser.Remove(id);
            }
        }

        public void playerQuits(string id)
        {
            UserVoter tmpvoter;
            if (Program.ConnIDtoUser.TryGetValue(id, out tmpvoter))
            {
                WaitingRoom.RemoveFromWaitingR(id);
                Clients.Client(id).showNextGame(false, tmpvoter.userID, tmpvoter.CurrScore, tmpvoter.mTurkToken);
                Program.ConnIDtoUser.Remove(Context.ConnectionId);
                Console.WriteLine("Quitter: {0} ({1}) decided to quit.. his score: {2}", tmpvoter.userID, tmpvoter.mTurkID != "" ? tmpvoter.mTurkID : tmpvoter.connectionID, tmpvoter.TotalScore);
                waitingRoomStats();
                WaitingRoom.checkToStartGame();
            }
        }
        //sent when the client voted
        public void VoteDetails(string id, int playerIndex, int candidate, int time)
        {
            Game thegame = Program.getplayersGame(id);
            if (thegame != null)
            {
                int status = thegame.vote(candidate, playerIndex,time);
                if (status == 1) //the game cont.
                {
                    int next = thegame.getNextTurn();

                    if (next == -1) //game over
                        gameOver(thegame, id, playerIndex);
                    else // game cont.
                    {
                        while (next != -1 && (thegame.playersTypeOrder[next] == "computer" || thegame.playersTypeOrder[next] == "replaced"))
                        {
                            foreach (string t in thegame.playersID)
                            {
                                UserVoter tmpvoter;
                                if (Program.ConnIDtoUser.TryGetValue(t, out tmpvoter))
                                {
                                    int playerIDX = tmpvoter.inGameIndex;
                                    Clients.Client(t)
                                        .OtherVotedUpdate(thegame.numOfCandidates,
                                            thegame.createNumOfVotesString(tmpvoter),
                                            thegame.votesPerPlayer[playerIDX], thegame.getTurnsLeft(), next,
                                            thegame.getCurrentWinner(playerIDX), thegame.createWhoVotedString(playerIDX),
                                            ("p" + (playerIDX + 1)), thegame.turnsToWait(playerIDX));
                                }
                            }
                            next = thegame.getNextTurn();
                        }
                        if (next != -1){
                            updateOtherPlayers(thegame, id, playerIndex, next);
                            updatePlayer(thegame, id, playerIndex, next);
                        }
                        
                        else
                            gameOver(thegame, id, playerIndex);
                    }

                }
                else if (status == -1) //game over
                    gameOver(thegame, id, playerIndex);
            }
        }
        
        
        private void updateOtherPlayers(Game game, string id, int playerIndex, int next)
        {
            //numOfCandidates, voted, turnsLeft, playerVoted, votingNow, currentWinnersIndex,whoVoted, playerString, turnsToWait
            foreach (string t in game.playersID)
            {
                if (t == id) continue;
                UserVoter tmpVoter;
                if (Program.ConnIDtoUser.TryGetValue(id, out tmpVoter))
                {
                    int playerIDX = game.getPlayerIndex(t);
                    Clients.Client(t)
                        .OtherVotedUpdate(game.numOfCandidates, game.createNumOfVotesString(tmpVoter),
                            game.votesPerPlayer[playerIDX], game.getTurnsLeft(), next,
                            game.getCurrentWinner(playerIDX), game.createWhoVotedString(playerIDX), ("p" + (playerIDX + 1)),
                            game.turnsToWait(playerIDX));
                }
            }
        }

        private void updatePlayer(Game game, string id, int playerIndex, int next)
        {
            UserVoter playerUser;
            if (Program.ConnIDtoUser.TryGetValue(id, out playerUser))
            {
                //numOfCandidates, votes, votesL, turnsL, defaultCand, voting, winner, whoVoted, playerString, turnsWait
                Clients.Client(id).VotedUpdate(game.numOfCandidates, game.createNumOfVotesString(playerUser), game.votesPerPlayer[playerIndex], game.getTurnsLeft(), game.getDefault(playerIndex), next, game.getCurrentWinner(playerIndex), game.createWhoVotedString(playerIndex), ("p" + (playerIndex + 1)), game.turnsToWait(playerIndex));
            }

            Clients.Client(game.getPlayerID(game.humanTurn)).YourTurn();
        }

        private void gameOver(Game game, string id, int playerIndex)
        {
            //to seperade playerIndex when msg sent in order to dend the right playerString
            List<int> playersPoints = game.currentPoints;
            string points = playersPoints.Aggregate("", (current, point) => current + ("#" + point));
            foreach (string playerid in game.playersID)
            {
                UserVoter playUser;
                if (Program.ConnIDtoUser.TryGetValue(playerid, out playUser))
                {
                    int playeridx = playUser.inGameIndex;
                    Clients.Client(playerid).GameOver(game.numOfCandidates, game.createNumOfVotesString(playUser), game.votesPerPlayer[playeridx], game.getTurnsLeft(), points, game.getWinner(), game.getCurrentWinner(playeridx), game.createWhoVotedString(playeridx), ("p" + (playeridx + 1)));
                    playUser.resetGame();
                }
            }
            Program.PlayingGames.Remove(game);
        }   
    }
}
