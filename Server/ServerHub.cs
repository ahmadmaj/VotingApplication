using System;
using System.Collections.Generic;
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
        //sent when a client wants to start a game
        public void ConnectMsg(string msg, string id)
        {
            Program.awaitingPlayersID.Add(id);
            if (Program.awaitingPlayersID.Count == Program.gameDetails.getNumOfHumanPlayers())
            {
                Program.games.Add(new Game(Program.gameDetails.getNumOfHumanPlayers(), Program.gameDetails.getPlayers(), Program.gameDetails.getNumOfCandidates(), Program.gameDetails.getCandidatesNames(), Program.gameDetails.getRounds(), Program.gameDetails.getVotesList(), Program.gameDetails.getPoints(), Program.gameDetails.getPriorities(), Program.gameDetails.getAgents(), Program.gameDetails.getIsRounds()));

                for (int i = 0; i < Program.awaitingPlayersID.Count; i++) //inform the players the game begins
                    Clients.Client(Program.awaitingPlayersID.ElementAt(i)).StartGameMsg("start");
            }
            else
                Clients.Client(id).StartGameMsg("wait");
        }

        //sent when the client loads the game page
        public void GameDetailsMsg(string connectionId)
        {
            for (int i = 0; i < Program.games.Count; i++)
            {
                if (Program.games[i].getStatus() == "init")
                    Program.games[i].addPlayerID(connectionId);
            }
            int turn = Program.games[0].getTurn(connectionId);
            //msg,playerID, numOfCandidates, numPlayers, numVotes, numTurns, priority, candNames, candIndex, point, voted, turn
            Clients.Client(connectionId).GameDetails("start", Program.games[0].getPlayerIndex(connectionId), Program.gameDetails.getNumOfCandidates(), Program.gameDetails.getNumOfTotalPlayers(), Program.games[0].getNumOfRounds(), Program.games[0].getTurnsLeft(), Program.createPrioritiesString(0, connectionId), Program.createCandNamesString(0, connectionId), Program.createCandIndexString(0, connectionId), 0, Program.createPointsString(0, connectionId), Program.createNumOfVotesString(0, connectionId), turn);
        }

        //sent when the client voted
        public void VoteDetails(string id, int playerIndex, int candidate)
        {
            int status = Program.games[0].vote(candidate, playerIndex);
            if (status == 1) //the game cont.
            {
                int next = Program.games[0].getNextTurn();

                if (next == -1) //game over
                {
                    List<int> playersPoints = Program.games[0].gameOverPoints();
                    Clients.Client(id).GameOver(Program.games[0].getNumOfCandidates(), Program.createNumOfVotesString(0, playerIndex), Program.games[0].getVotesLeft(playerIndex), Program.games[0].getTurnsLeft(), Program.createGameOverString(playersPoints), Program.games[0].getWinner());

                    for (int i = 0; i < Program.games[0].getPlayersIDList().Count; i++)
                    {
                        int player = Program.games[0].getPlayerIndex(Program.games[0].getPlayersIDList()[i]);
                        Clients.Client(Program.games[0].getPlayersIDList()[i]).GameOver(Program.games[0].getNumOfCandidates(), Program.createNumOfVotesString(0, player), Program.games[0].getVotesLeft(player), Program.games[0].getTurnsLeft(), Program.createGameOverString(playersPoints), Program.games[0].getWinner());
                    }
                }
                else // game cont.
                {
                    //numOfCandidates, voted, turnsLeft
                    for(int i=0; i<Program.games[0].getPlayersIDList().Count;i++){
                        int player = Program.games[0].getPlayerIndex(Program.games[0].getPlayersIDList()[i]);
                        Clients.Client(Program.games[0].getPlayersIDList()[i]).OtherVotedUpdate(Program.games[0].getNumOfCandidates(), Program.createNumOfVotesString(0, player), Program.games[0].getVotesLeft(player), Program.games[0].getTurnsLeft());
                    }
                    //numOfCandidates, voted, turnsLeft, candIndex, defaultCand
                    Clients.Client(id).VotedUpdate(Program.games[0].getNumOfCandidates(), Program.createNumOfVotesString(0, playerIndex), Program.games[0].getVotesLeft(playerIndex), Program.games[0].getTurnsLeft(), Program.createCandIndexString(0, id), Program.games[0].getDefault(playerIndex));
                   
                    Clients.Client(Program.games[0].getPlayerID(next)).YourTurn();

                }

            }
            else if (status == -1) //game over
            {
                List<int> playersPoints = Program.games[0].gameOverPoints();
                Clients.Client(id).GameOver(Program.games[0].getNumOfCandidates(), Program.createNumOfVotesString(0, playerIndex), Program.games[0].getVotesLeft(playerIndex), Program.games[0].getTurnsLeft(), Program.createGameOverString(playersPoints), Program.games[0].getWinner());

                for (int i = 0; i < Program.games[0].getPlayersIDList().Count; i++)
                {
                    int player = Program.games[0].getPlayerIndex(Program.games[0].getPlayersIDList()[i]);
                    Clients.Client(Program.games[0].getPlayersIDList()[i]).GameOver(Program.games[0].getNumOfCandidates(), Program.createNumOfVotesString(0, player), Program.games[0].getVotesLeft(player), Program.games[0].getTurnsLeft(), Program.createGameOverString(playersPoints), Program.games[0].getWinner());
                }
            }
        }
    }
}
