using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
            if (Program.AwaitingGame != null && Program.AwaitingGame.playersID != null)
                foreach (string ids in Program.AwaitingGame.playersID)
                    if (Context.ConnectionId == ids){
                        Program.AwaitingGame.playersID.Remove(ids);
                        break;
                    }

            if (Program.PlayingGames.Count > 0)
            {
                Game theGame = Program.getplayersGame(Context.ConnectionId);
                if (!theGame.isGameOver())
                {
                    int playerIndex = theGame.getPlayerIndex(Context.ConnectionId);
                    theGame.deletePlayerID(Context.ConnectionId);
                    theGame.replacePlayer(playerIndex);
                    if (theGame.getCurrentTurn() == playerIndex)
                    {
                        int next = theGame.getNextTurn();
                        if (next != -1){
                            if (theGame.getPlayersIDList().Count > 0)
                            {
                                if (theGame.getPlayersIDList().Count - 1 == theGame.getHumanTurn())
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
                }
                    
            }

            Program.Players.Remove(Context.ConnectionId);
            return null;
        }

        //sent when a client wants to start a game
        public void ConnectMsg(string msg, string id)
        {
            //Program.awaitingPlayersID.Add(id);
            if (Program.AwaitingGame == null)
            {
                Game newGame = new Game(Program.gameDetails.getNumOfHumanPlayers(),
                    new List<string>(Program.gameDetails.getPlayers()), Program.gameDetails.getNumOfCandidates(),
                    new List<string>(Program.gameDetails.getCandidatesNames()), Program.gameDetails.getRounds(),
                    new List<int>(Program.gameDetails.getVotesList()), new List<int>(Program.gameDetails.getPoints()),
                    new List<List<string>>(Program.gameDetails.getPriorities()),new List<Agent>(Program.gameDetails.getAgents()),
                    Program.gameDetails.getIsRounds(), Program.gameDetails.getShowWhoVoted());
                Program.AwaitingGame = newGame;
            }
            Program.AwaitingGame.addPlayerID(id);
            Program.Players.Add(id, Program.AwaitingGame.gameID);
            if (Program.AwaitingGame.getPlayersIDList().Count == Program.gameDetails.getNumOfHumanPlayers())
            {
                Game startRunning = Program.AwaitingGame;
                Program.AwaitingGame = null;
                Program.PlayingGames.Add(startRunning);
                foreach (string playerid in startRunning.getPlayersIDList())
                    Clients.Client(playerid).StartGameMsg("start");
                }
            else
                Clients.Client(id).StartGameMsg("wait");
        }

        

        //sent when the client loads the game page
        public void GameDetailsMsg(string connectionId)
        {
            Game thegame = Program.getplayersGame(connectionId);
            //if (thegame.getStatus() == "init")
           //     thegame.addPlayerID(connectionId);
            /*
           for (int i = 0; i < Program.AwayingGames.Count; i++)
            {
                if (Program.AwayingGames[i].getStatus() == "init")
                    Program.AwayingGames[i].addPlayerID(connectionId);
            }*/
            int turn = thegame.getTurn(connectionId);
            //msg,playerID, numOfCandidates, numPlayers, numVotes, numTurns, priority, candNames, candIndex, defaultCand, points, votes, isVoted, voted, turn, whoIsVoting, currentWinnersIndex
            Clients.Client(connectionId).GameDetails("start", thegame.getPlayerIndex(connectionId), thegame.getNumOfCandidates(), thegame.getNumOfPlayers(), thegame.getNumOfRounds(), thegame.getTurnsLeft(), Program.createPrioritiesString(connectionId), Program.createCandNamesString(connectionId), Program.createCandIndexString(connectionId),0,
              Program.createPointsString(connectionId), Program.createNumOfVotesString(connectionId), thegame.isVotedDisplay(), thegame.createWhoVotedString(thegame.getPlayerIndex(connectionId)), turn, thegame.getCurrentTurn(), thegame.getCurrentWinner(thegame.getPlayerIndex(connectionId)));
        }

        //sent when the client voted
        public void VoteDetails(string id, int playerIndex, int candidate)
        {
            Game thegame = Program.getplayersGame(id);
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
                            Clients.Client(thegame.getPlayersIDList()[i]).OtherVotedUpdate(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, player), thegame.getVotesLeft(player), thegame.getTurnsLeft(), (next - 1), next, thegame.getCurrentWinner(player), thegame.createWhoVotedString(player));
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

        private void updateOtherPlayers(Game game, string id, int playerIndex, int next)
        {
            //numOfCandidates, voted, turnsLeft, playerVoted, votingNow, currentWinnersIndex,whoVoted
            for (int i = 0; i < game.getPlayersIDList().Count; i++)
            {
                if (game.getPlayersIDList()[i] != id)
                {
                    int player = game.getPlayerIndex(game.getPlayersIDList()[i]);
                    Clients.Client(game.getPlayersIDList()[i]).OtherVotedUpdate(game.getNumOfCandidates(), Program.createNumOfVotesString(game, player), game.getVotesLeft(player), game.getTurnsLeft(), (next - 1), next, game.getCurrentWinner(player), game.createWhoVotedString(player));
                }
            }

        }

        private void updatePlayer(Game game, string id, int playerIndex, int next)
        {
            //numOfCandidates, voted, turnsLeft, candIndex, defaultCand, voted, votingNow, currentWinnersIndex, whoVoted
            Clients.Client(id).VotedUpdate(game.getNumOfCandidates(), Program.createNumOfVotesString(game, playerIndex), game.getVotesLeft(playerIndex), game.getTurnsLeft(), Program.createCandIndexString(id), game.getDefault(playerIndex), (next - 1), next, game.getCurrentWinner(playerIndex), game.createWhoVotedString(playerIndex));

            Clients.Client(game.getPlayerID(game.getHumanTurn())).YourTurn();
        }

        private void gameOver(Game game, string id, int playerIndex)
        {
            List<int> playersPoints = game.gameOverPoints();
            Clients.Client(id).GameOver(game.getNumOfCandidates(), Program.createNumOfVotesString(game, playerIndex), game.getVotesLeft(playerIndex), game.getTurnsLeft(), Program.createGameOverString(playersPoints), game.getWinner(), game.getCurrentWinner(playerIndex), game.createWhoVotedString(playerIndex));

            for (int i = 0; i < game.getPlayersIDList().Count; i++)
            {
                int player = game.getPlayerIndex(game.getPlayersIDList()[i]);
                Clients.Client(game.getPlayersIDList()[i]).GameOver(game.getNumOfCandidates(), Program.createNumOfVotesString(game, player), game.getVotesLeft(player), game.getTurnsLeft(), Program.createGameOverString(playersPoints), game.getWinner(), game.getCurrentWinner(playerIndex), game.createWhoVotedString(playerIndex));
            }
        }
    }
}
