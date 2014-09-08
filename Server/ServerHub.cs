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
            var sb = new StringBuilder();
            if (Program.ConnIDtoUser.TryGetValue(Context.ConnectionId, out removedVoter))
            {
                sb.AppendFormat("Disconnected: Player {0} ({1}). ", removedVoter.userID,
                    Context.ConnectionId);
                if (removedVoter.CurrGame != null)
                    sb.AppendFormat("Left game {0}.", removedVoter.CurrGame.gameID);
                Console.WriteLine(sb);
                Program.ConnIDtoUser.Remove(Context.ConnectionId);
            }
            
            WaitingRoom.RemoveFromWaitingR(Context.ConnectionId);
            waitingRoomStats();


            if (Program.PlayingGames.Count > 0)
                if (theGame != null && !theGame.gameOver)
                {
                    int playerIndex = theGame.getPlayerIndex(Context.ConnectionId);
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
                        if (theGame.playersID.Count == 0)
                            theGame.endGame();

                        if (theGame.playersID.Count <= theGame.humanTurn)
                        {
                            theGame.humanTurn = 0;
                        }
                    }
                }
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
                newplayer = data.Count != 0 ? new UserVoter(id, data["workerId"].ToString()) : new UserVoter(id);
                Program.ConnIDtoUser.Add(id, newplayer);
            }


            if (WaitingRoom.joinWaitingRnStart(newplayer) && Program.PlayingGames.Any())
                foreach (Game newGame in Program.PlayingGames)
                    sendStartToPlayers(newGame);
            else
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
            Game thegame = Program.getplayersGame(connectionId);
            int playerIndex = thegame.getPlayerIndex(connectionId);
            List<string> candNames = thegame.candidatesNames;
            List<string> priority = thegame.priorities[playerIndex];
            UserVoter playerUser = Program.ConnIDtoUser[connectionId];
            foreach (string t in priority)
                playerUser.CurrPriority.Add(candNames.IndexOf(t) + 1);

            int turn = thegame.getTurn(connectionId);
            Clients.Client(connectionId).GameDetails("start", thegame.getPlayerIndex(connectionId), thegame.numOfCandidates, thegame.getNumOfPlayers(), thegame.rounds, thegame.getTurnsLeft(), Program.createPrioritiesString(connectionId), Program.createCandNamesString(connectionId), playerUser.currPriToString(), 0,
              Program.createPointsString(connectionId), Program.createNumOfVotesString(connectionId), thegame.whoVoted, thegame.createWhoVotedString(thegame.getPlayerIndex(connectionId)), ("p" + (thegame.getPlayerIndex(connectionId) + 1).ToString()), turn, thegame.turn, thegame.getCurrentWinner(thegame.getPlayerIndex(connectionId)), thegame.turnsToWait(thegame.getPlayerIndex(connectionId)), thegame.prioritiesJSON);
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
                        while (next != -1 && (thegame.players[next] == "computer" || thegame.players[next] == "replaced"))
                        {
                            foreach (string t in thegame.playersID)
                            {
                                int player = thegame.getPlayerIndex(t);
                                Clients.Client(t).OtherVotedUpdate(thegame.numOfCandidates, Program.createNumOfVotesString(thegame, player), thegame.votesPerPlayer[player], thegame.getTurnsLeft(), (next - 1), next, thegame.getCurrentWinner(player), thegame.createWhoVotedString(player), ("p" + (player + 1).ToString()), thegame.turnsToWait(player));
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
            for (int i = 0; i < game.playersID.Count; i++)
            {
                if (game.playersID[i] != id)
                {
                    int player = game.getPlayerIndex(game.playersID[i]);
                    Clients.Client(game.playersID[i]).OtherVotedUpdate(game.numOfCandidates, Program.createNumOfVotesString(game, player), game.votesPerPlayer[player], game.getTurnsLeft(), (next - 1), next, game.getCurrentWinner(player), game.createWhoVotedString(player), ("p" + (player + 1).ToString()), game.turnsToWait(player));
                }
            }

        }

        private void updatePlayer(Game game, string id, int playerIndex, int next)
        {
            UserVoter playerUser;
            if (Program.ConnIDtoUser.TryGetValue(id, out playerUser))
            {
                //numOfCandidates, voted, turnsLeft, candIndex, defaultCand, voted, votingNow, currentWinnersIndex, whoVoted, playerString, turnsToWait
                Clients.Client(id).VotedUpdate(game.numOfCandidates, Program.createNumOfVotesString(game, playerIndex), game.votesPerPlayer[playerIndex], game.getTurnsLeft(), playerUser.currPriToString(), game.getDefault(playerIndex), (next - 1), next, game.getCurrentWinner(playerIndex), game.createWhoVotedString(playerIndex), ("p" + (playerIndex + 1).ToString()), game.turnsToWait(playerIndex));
            }

            Clients.Client(game.getPlayerID(game.humanTurn)).YourTurn();
        }

        private void gameOver(Game game, string id, int playerIndex)
        {
            //to seperade playerIndex when msg sent in order to dend the right playerString
            List<int> playersPoints = game.gameOverPoints();
            foreach (string playerid in game.playersID)
            {
                int playeridx = game.getPlayerIndex(playerid);
                if (Program.ConnIDtoUser.ContainsKey(playerid))
                {
                    Clients.Client(playerid).GameOver(game.numOfCandidates, Program.createNumOfVotesString(game, playeridx), game.votesPerPlayer[playeridx], game.getTurnsLeft(), Program.createGameOverString(playersPoints), game.getWinner(), game.getCurrentWinner(playeridx), game.createWhoVotedString(playeridx), ("p" + (playeridx + 1).ToString()));
                }
            }
            Program.PlayingGames.Remove(game);
        }   
    }
}
