using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Server
{
    [HubName("serverHub")]
    public class ServerHub : Hub
    {
        public override Task OnDisconnected()
        {
            if (Program.waitingRoom != null && Program.waitingRoom.Contains(Context.ConnectionId))
                Program.waitingRoom.Remove(Context.ConnectionId);

            if (Program.AwaitingGame != null && Program.AwaitingGame.playersID != null)
                foreach (string ids in Program.AwaitingGame.playersID)
                    if (Context.ConnectionId == ids){
                        Program.AwaitingGame.playersID.Remove(ids);
                        break;
                    }

            if (Program.PlayingGames.Count > 0)
            {
                Game theGame = Program.getplayersGame(Context.ConnectionId);  
                if (theGame != null && !theGame.isGameOver())
                {
                    int playerIndex = theGame.getPlayerIndex(Context.ConnectionId);
                    theGame.replacePlayer(playerIndex, Context.ConnectionId);
                    theGame.deletePlayerID(Context.ConnectionId);
                    if (theGame.getPlayersIDList().Count == 0)
                        theGame.endGame();
                    else if (theGame.getPlayersIDList().Count > 0 && theGame.getCurrentTurn() == playerIndex)
                    {
                        int next = theGame.getNextTurn();
                        if (next != -1 && theGame.getPlayersIDList().Count > 0)
                        {
                            if (theGame.getPlayersIDList().Count > 0)
                            {
                                if (theGame.getPlayersIDList().Count - 1 == theGame.getHumanTurn() || theGame.getPlayersIDList().Count <= theGame.getHumanTurn())
                                {
                                    theGame.setHumanTurn(0);
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
                        if (theGame.getPlayersIDList().Count == 0)
                            theGame.endGame(); 

                        if (theGame.getPlayersIDList().Count <= theGame.getHumanTurn())
                        {
                            theGame.setHumanTurn(0);
                        }

                    }
                }  
            }

            Program.ConnIDtoUser.Remove(Context.ConnectionId);
            return null;
        }

        //sent when a client wants to start a game
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ConnectMsg(string msg, string id)
        {
            //Program.awaitingPlayersID.Add(id);
            if (Program.AwaitingGame == null)
            {
                Game newGame = new Game(Program.gameDetails.Value);
                Program.AwaitingGame = newGame;
            }
            UserVoter newplayer;
            if (!Program.ConnIDtoUser.TryGetValue(id,out newplayer))
                newplayer = new UserVoter(id);
            
            Program.AwaitingGame.addPlayerID(newplayer);
            newplayer.JoinGame(Program.AwaitingGame);


            if (Program.AwaitingGame.getPlayersIDList().Count == Program.AwaitingGame.numOfHumanPlayers)
            {
                Game startRunning = Program.AwaitingGame;
                Program.AwaitingGame = null;
                Program.PlayingGames.Add(startRunning);
                foreach (string player in startRunning.getPlayersIDList())
                    Clients.Client(player).StartGameMsg("start");
                }
            else
                Clients.Client(id).StartGameMsg("wait");
        }

        //sent when the client loads the game page
        public void GameDetailsMsg(string connectionId)
        {
            Game thegame = Program.getplayersGame(connectionId);
            int player = thegame.getPlayerIndex(connectionId);
            List<string> candNames = thegame.candidatesNames;
            List<string> priority = thegame.priorities.ElementAt(player);
            UserVoter playerUser = Program.ConnIDtoUser[connectionId];
            foreach (string t in priority)
                playerUser.CurrPriority.Add(candNames.IndexOf(t) + 1);

            int turn = thegame.getTurn(connectionId);
            Clients.Client(connectionId).GameDetails("start", thegame.getPlayerIndex(connectionId), thegame.getNumOfCandidates(), thegame.getNumOfPlayers(), thegame.getNumOfRounds(), thegame.getTurnsLeft(), Program.createPrioritiesString(connectionId), Program.createCandNamesString(connectionId), playerUser.currPriToString(),0,
              Program.createPointsString(connectionId), Program.createNumOfVotesString(connectionId), thegame.isVotedDisplay(), thegame.createWhoVotedString(thegame.getPlayerIndex(connectionId)), ("p" + (thegame.getPlayerIndex(connectionId)+1).ToString()), turn, thegame.getCurrentTurn(), thegame.getCurrentWinner(thegame.getPlayerIndex(connectionId)), thegame.turnsToWait(thegame.getPlayerIndex(connectionId)));
        }

        //sent when the client voted
        public void VoteDetails(string id, int playerIndex, int candidate)
        {
            Game thegame = Program.getplayersGame(id);
            if (thegame != null)
            {
                int status = thegame.vote(candidate, playerIndex);
                if (status == 1) //the game cont.
                {
                    int next = thegame.getNextTurn();

                    if (next == -1) //game over
                        gameOver(thegame, id, playerIndex);
                    else // game cont.
                    {
                        while (next != -1 && (thegame.getPlayersType(next) == "computer" || thegame.getPlayersType(next) == "replaced"))
                        {
                            for (int i = 0; i < thegame.getPlayersIDList().Count; i++)
                            {
                                int player = thegame.getPlayerIndex(thegame.getPlayersIDList()[i]);
                                Clients.Client(thegame.getPlayersIDList()[i]).OtherVotedUpdate(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, player), thegame.getVotesLeft(player), thegame.getTurnsLeft(), (next - 1), next, thegame.getCurrentWinner(player), thegame.createWhoVotedString(player), ("p" + (player + 1).ToString()), thegame.turnsToWait(player));
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


        public void nextGame(string id)
        {

            if (Program.waitingRoom == null) Program.waitingRoom = new List<string>();
            Program.waitingRoom.Add(id);
            if (Program.waitingRoom.Count == 10)
            {
                var random = new Random();
                var result = Program.waitingRoom.OrderBy(i => random.Next()).Take(Program.waitingRoom.Count).OrderBy(i => i);
                Program.gameDetails = Program.gameDetails.Next ?? Program.gameDetails.List.First;
                foreach (string player in result)
                    ConnectMsg("connect",player);
                Program.waitingRoom = null;
            }
            else
                Clients.Client(id).StartGameMsg("wait");
        }
        
        
        private void updateOtherPlayers(Game game, string id, int playerIndex, int next)
        {
            //numOfCandidates, voted, turnsLeft, playerVoted, votingNow, currentWinnersIndex,whoVoted, playerString, turnsToWait
            for (int i = 0; i < game.getPlayersIDList().Count; i++)
            {
                if (game.getPlayersIDList()[i] != id)
                {
                    int player = game.getPlayerIndex(game.getPlayersIDList()[i]);
                    Clients.Client(game.getPlayersIDList()[i]).OtherVotedUpdate(game.getNumOfCandidates(), Program.createNumOfVotesString(game, player), game.getVotesLeft(player), game.getTurnsLeft(), (next - 1), next, game.getCurrentWinner(player), game.createWhoVotedString(player), ("p" + (player + 1).ToString()), game.turnsToWait(player));
                }
            }

        }

        private void updatePlayer(Game game, string id, int playerIndex, int next)
        {
            if (Program.ConnIDtoUser.ContainsKey(id))
            {
                UserVoter playerUser = Program.ConnIDtoUser[id];
                //numOfCandidates, voted, turnsLeft, candIndex, defaultCand, voted, votingNow, currentWinnersIndex, whoVoted, playerString, turnsToWait
                Clients.Client(id).VotedUpdate(game.getNumOfCandidates(), Program.createNumOfVotesString(game, playerIndex), game.getVotesLeft(playerIndex), game.getTurnsLeft(), playerUser.currPriToString(), game.getDefault(playerIndex), (next - 1), next, game.getCurrentWinner(playerIndex), game.createWhoVotedString(playerIndex), ("p" + (playerIndex+1).ToString()), game.turnsToWait(playerIndex));
            }

            Clients.Client(game.getPlayerID(game.getHumanTurn())).YourTurn();
        }

        private void gameOver(Game game, string id, int playerIndex)
        {
            //to seperade playerIndex when msg sent in order to dend the right playerString
            List<int> playersPoints = game.gameOverPoints();
            foreach (string playerid in game.getPlayersIDList())
            {
                int playeridx = game.getPlayerIndex(playerid);
                UserVoter playerUser = Program.ConnIDtoUser[playerid];
                playerUser.score += playersPoints[playeridx];
                Clients.Client(playerid).GameOver(game.getNumOfCandidates(), Program.createNumOfVotesString(game, playeridx), game.getVotesLeft(playeridx), game.getTurnsLeft(), Program.createGameOverString(playersPoints), game.getWinner(), game.getCurrentWinner(playeridx), game.createWhoVotedString(playeridx), ("p" + (playeridx + 1).ToString()));
                playerUser.StoreHistory();
            }
            Program.PlayingGames.Remove(game);
        }
   
        
    }
}
