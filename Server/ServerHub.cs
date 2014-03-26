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
                    Program.gameDetails.getPlayers(), Program.gameDetails.getNumOfCandidates(),
                    Program.gameDetails.getCandidatesNames(), Program.gameDetails.getRounds(),
                    Program.gameDetails.getVotesList(), Program.gameDetails.getPoints(),
                    Program.gameDetails.getPriorities(), Program.gameDetails.getAgents(),
                    Program.gameDetails.getIsRounds());
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
            //msg,playerID, numOfCandidates, numPlayers, numVotes, numTurns, priority, candNames, candIndex, point, voted, turn
            Clients.Client(connectionId).GameDetails("start", thegame.getPlayerIndex(connectionId), 
                Program.gameDetails.getNumOfCandidates(),
                Program.gameDetails.getNumOfTotalPlayers(),
                thegame.getNumOfRounds(), thegame.getTurnsLeft(),
                Program.createPrioritiesString(connectionId),
                Program.createCandNamesString(connectionId),
                Program.createCandIndexString(connectionId), 0,
                Program.createPointsString(connectionId), 
                Program.createNumOfVotesString(connectionId), turn);
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
                {
                    List<int> playersPoints = thegame.gameOverPoints();
                    Clients.Client(id).GameOver(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, playerIndex), thegame.getVotesLeft(playerIndex), thegame.getTurnsLeft(), Program.createGameOverString(playersPoints), thegame.getWinner());

                    for (int i = 0; i < thegame.getPlayersIDList().Count; i++)
                    {
                        int player = thegame.getPlayerIndex(thegame.getPlayersIDList()[i]);
                        Clients.Client(thegame.getPlayersIDList()[i]).GameOver(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, player), thegame.getVotesLeft(player), thegame.getTurnsLeft(), Program.createGameOverString(playersPoints), thegame.getWinner());
                    }
                }
                else // game cont.
                {
                    //numOfCandidates, voted, turnsLeft
                    for(int i=0; i<thegame.getPlayersIDList().Count;i++){
                        int player = thegame.getPlayerIndex(thegame.getPlayersIDList()[i]);
                        Clients.Client(thegame.getPlayersIDList()[i]).OtherVotedUpdate(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, player), thegame.getVotesLeft(player), thegame.getTurnsLeft());
                    }
                    //numOfCandidates, voted, turnsLeft, candIndex, defaultCand
                    Clients.Client(id).VotedUpdate(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, playerIndex), thegame.getVotesLeft(playerIndex), thegame.getTurnsLeft(), Program.createCandIndexString(id), thegame.getDefault(playerIndex));

                    Clients.Client(thegame.getPlayerID(next)).YourTurn();

                }

            }
            else if (status == -1) //game over
            {
                List<int> playersPoints = thegame.gameOverPoints();
                Clients.Client(id).GameOver(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame, playerIndex), thegame.getVotesLeft(playerIndex), thegame.getTurnsLeft(), Program.createGameOverString(playersPoints), thegame.getWinner());

                for (int i = 0; i < thegame.getPlayersIDList().Count; i++)
                {
                    int player = thegame.getPlayerIndex(thegame.getPlayersIDList()[i]);
                    Clients.Client(thegame.getPlayersIDList()[i]).GameOver(thegame.getNumOfCandidates(), Program.createNumOfVotesString(thegame,player), thegame.getVotesLeft(player), thegame.getTurnsLeft(), Program.createGameOverString(playersPoints), thegame.getWinner());
                }
            }
        }
    }
}
