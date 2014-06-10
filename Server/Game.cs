﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Server
{
    public class Game : IComparable
    {
        private string status;
        static int nextId;
        public int gameID {get; private set;}
        public int numOfHumanPlayers { get; private set; }
        private List<string> players; // players type (human, compter, replaced)
        public List<string> playersID;
        private int numOfCandidates;
        private int rounds; //number of rounds in the game
       // private int numOfTurns;
        public List<string> candidatesNames { get; private set; }
        private List<int> votesPerPlayer; //votes left for each player
        private Boolean whoVoted; //to show in the game who voted for the player
        private List<List<int>> votedBy; //candidates, players who voted
        private List<int> votes; //number of votes each candidate got
        public List<int> points { get; private set; }
        public List<List<string>> priorities { get; private set; }
        private int turn; //index of the player who's turn it is
        private int humanTurn; //index of the human player who's turn it is
        private int computerTurn; //index of the agent who's turn it is
        private int replaceTurn; //index of the replacing agent who's turn it is
        private Boolean firstTurn;
        private List<int> winners;
        private WriteData file;
        private List<string> writeToFile;
        private List<Agent> agents;
        private List<ReplacingAgent> replacingAgents;
        private List<string> playersDisconnected;
        private Boolean isRounds;
        private List<List<int>> playersVotes;
        private int roundNumber; //current round
        private Boolean gameOver;
        private Boolean startSecondrnd;
        private Boolean firstRound;
        private Boolean fileCreated;
        private Boolean logUpdated;


        public Game(GameDetails gamedets)
        {
            string start = gamedets.startSecondRound;
            gameID = Interlocked.Increment(ref nextId);
            this.status = "init";
            this.numOfHumanPlayers = gamedets.numOfHumanPlayers;
            this.players = new List<string>(gamedets.players);
            this.playersID = new List<string>();
            this.numOfCandidates = gamedets.numOfCandidates;
           // this.numOfTurns = turns;
            this.candidatesNames = new List<string>(gamedets.candidatesNames);
            this.points = new List<int>(gamedets.points);
            this.priorities = new List<List<string>>(gamedets.priorities);
            this.votesPerPlayer = new List<int>(gamedets.getVotesList());
            this.rounds = gamedets.numOfRounds;
            this.whoVoted = gamedets.whoVoted;
            this.votedBy = new List<List<int>>();
            for (int i = 0; i < numOfCandidates; i++)
            {
                votedBy.Add(new List<int>());
            }
            this.votes = new List<int>();
            for (int i = 0; i < this.numOfCandidates; i++)
                this.votes.Add(0);

            this.playersVotes = new List<List<int>>();
            for (int i = 0; i < this.players.Count; i++)
            {
                this.playersVotes.Add(new List<int>());
                this.playersVotes[i].Add(-1);
                this.playersVotes[i].Add(-1);
            }
            this.turn = 0;
            this.humanTurn = 0;
            this.computerTurn = 0;
            this.firstTurn = true;
            this.roundNumber = 1;
            this.winners = new List<int>();

            this.file = new WriteData(gameID);
            this.fileCreated = false;
            this.writeToFile = new List<string>();
            this.writeToFile.Add("config file: ," + gamedets.configFile);
            this.writeToFile.Add("number of players:," + gamedets.players.Count.ToString());
            this.writeToFile.Add("number of candidates:," + this.numOfCandidates.ToString());
            this.writeToFile.Add("game ended after:,");
            this.writeToFile.Add("player,priorities");
            this.logUpdated = false;
            this.agents = new List<Agent>(gamedets.agents);
            this.isRounds = gamedets.isRounds;
            this.replacingAgents = new List<ReplacingAgent>();
            this.playersDisconnected = new List<string>();
            this.replaceTurn = -1;
            this.gameOver = false;
            if (start != "no")
            {
                this.startSecondrnd = true;
                this.firstRound = true;
                startFromSecondRound(start);     
            }
            else
                startSecondrnd = false;
        }

        public int vote(int candidatePriority, int player)
        {
            if (this.votesPerPlayer[player] > 0)
            {
                int candIndex = this.candidatesNames.IndexOf(this.priorities[player][candidatePriority]);
                updateVotedBy(candIndex, player);
                if (this.playersVotes[player][0] == -1)
                    this.votes[candIndex]++;
                else if (this.playersVotes[player][0] != -1 && this.playersVotes[player][0] != candIndex)
                { // different from the last vote
                    this.votes[this.playersVotes[player][0]]--;
                    this.votes[candIndex]++; 
                }

                this.playersVotes[player][1] = this.playersVotes[player][0];
                this.playersVotes[player][0] = candIndex;

                this.votesPerPlayer[player]--;
               // this.numOfTurns--;

                //update log
                string time = DateTime.Now.ToString();
                List<int> currentPoints = gameOverPoints(); //also updates the current winners
                string winnersString = "";
                for(int i=0;i<this.winners.Count;i++){
                    if (i == 0)
                        winnersString = this.winners[i].ToString();
                    else
                        winnersString = winnersString + " " + this.winners[i].ToString();
                }
                string candidatesString = "";
                for (int i = 0; i < numOfCandidates; i++)
                {
                    if (i == 0)
                        candidatesString = this.votes[i].ToString();
                    else
                        candidatesString = candidatesString + "," + this.votes[i].ToString();
                }
                string pointsString = "";
                for (int i = 0; i < currentPoints.Count; i++)
                {
                    if (i == 0)
                        pointsString = currentPoints[i].ToString();
                    else
                        pointsString = pointsString + "," + currentPoints[i].ToString();
                }

                String playerID = "";
                if (this.players[player] == "computer")
                {
                    if (this.isRounds)
                        playerID = "comp_" + this.computerTurn + "_" + this.agents[0].getType();
                    else
                        playerID = "comp_" + this.computerTurn + "_" + this.agents[this.computerTurn].getType();
                }
                else if (this.players[player] == "replaced")
                    playerID = "replace_" + this.replacingAgents[this.replaceTurn].getPlayerID().ToString() + "_" + this.replacingAgents[this.replaceTurn].getType();
                else
                    playerID = Program.ConnIDtoUser[this.playersID[this.humanTurn]].userID.ToString();
                     
                this.writeToFile.Add(time + "," + playerID.ToString() + "," + candIndex + "," + winnersString + "," + candidatesString + "," + pointsString);


                //Boolean gameOver = false;
                if (player == this.players.Count - 1)
                {
                    this.roundNumber++;
                    gameOver = checkGameOver();
                }

                if (this.rounds >= this.roundNumber && !gameOver)
                    return 1; //the game is not over
                else
                {
                    if (gameOver)
                        this.writeToFile[3] = "game ended after:,the players voted for the same candidate for 2 turns";
                    else
                        this.writeToFile[3] = "game ended after:,the turns are over";
                    writeToCSVFile();
                    return -1; //game over
                }
                    
            }
            else if (this.rounds < this.roundNumber)
            {
                writeToCSVFile();
                return -1; //game over
            }
                
            else
                return -2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int getTurn(string playerID)
        {
            if (this.players[this.turn] == "human")
            {
                this.firstTurn = false;
                this.firstRound = false;
                if (this.playersID[this.humanTurn] == playerID)
                    return 1;
                else
                    return 0;
            }
            else
            {
                getNextTurn();
                this.firstTurn = false;
                while (this.players[this.turn] == "computer")
                    getNextTurn();
                this.firstRound = false;
                if (this.playersID[this.humanTurn] == playerID)
                    return 1;
                else
                    return 0;                  
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int getNextTurn()
        {
            //if (!this.firstTurn)
            //    this.turn++;
            if (this.turn >= this.players.Count)
                this.turn = 0;
            int gameStatus = 1;
            if (this.players[this.turn] == "computer"){
                if (this.isRounds)
                {
                    int votefor = this.agents[0].vote(this.priorities[this.turn], this.candidatesNames, this.roundNumber);
                    if (!firstRound)
                        Thread.Sleep(5000);
                    gameStatus = vote(votefor, this.turn);
                }
                else
                {
                    int votefor = this.agents[this.computerTurn].vote(this.priorities[this.turn], this.candidatesNames, this.roundNumber);
                    if (!firstRound)
                        Thread.Sleep(5000);
                    gameStatus = vote(votefor, this.turn);
                }

                this.turn++;
                this.computerTurn++;
                if (this.turn >= this.players.Count)
                    this.turn = 0;
                if (this.computerTurn >= this.agents.Count)
                    this.computerTurn = 0;

            }
            else if (this.players[this.turn] == "replaced")
            {
                int votefor = this.replacingAgents[this.replaceTurn].vote(this.priorities[this.turn], this.candidatesNames, this.roundNumber);
                if (!firstRound)
                    Thread.Sleep(5000);
                gameStatus = vote(votefor, this.turn);
                this.replaceTurn++;
                this.turn++;
                if (this.replaceTurn >= this.replacingAgents.Count)
                    this.replaceTurn = 0;
                if (this.turn >= this.players.Count)
                    this.turn = 0;
            }
            else
            {
                this.turn++;
                if (!this.firstTurn)
                    this.humanTurn++;
                if (this.humanTurn >= this.playersID.Count)
                    this.humanTurn = 0;
                if (this.turn >= this.players.Count)
                    this.turn = 0;
                //if (this.replaceTurn >= this.replacingAgents.Count)
                //    this.replaceTurn = 0;
            }
            if (gameStatus == 1)
                return this.turn;
            else if (gameStatus == -1)
                return gameStatus;
            else
                return -2;
        }

        public string getPlayerID(int player)
        {
            return this.playersID.ElementAt(player);
        }
        public void deletePlayerID(string id)
        {
            this.playersID.Remove(id);
        }

        public List<string> getPlayersIDList()
        {
            return this.playersID;
        }

        public int getPlayerIndex(string player)
        {
            int humanCounter = this.playersID.IndexOf(player)+1;
            int ans = 0;
            for (int i = 0; i < this.players.Count; i++)
            {
                if (this.players[i] == "human")
                {
                    humanCounter--;
                    if (humanCounter == 0)
                    {
                        ans = i;
                        break;
                    }
                }
            }
                return ans;
        }

        public List<string> getPlayers()
        {
            return this.players;
        }

        public void replacePlayer(int index, string id)
        {
            this.players[index] = "replaced";
            this.replacingAgents.Add(new ReplacingAgent("FIRST", Program.ConnIDtoUser[id].userID));
            this.replaceTurn++;
            string disconnect = Program.ConnIDtoUser[id].userID.ToString() + "," + this.roundNumber.ToString();
            this.playersDisconnected.Add(disconnect);
        }

        public int getNumOfCandidates()
        {
            return this.numOfCandidates;
        }

        public int getNumOfPlayers()
        {
            return this.players.Count;
        }

        public int findPlayer(string playerID)
        {
            int ans = -1;
            for (int i = 0; i < this.playersID.Count; i++)
            {
                if (this.playersID[i] == playerID)
                {
                    ans = i;
                    break;
                }
            }
            return ans;
        }

        public string getStatus()
        {
            return this.status;
        }

        public List<int> getNumOfVotes()
        {
            return this.votes;
        }

        public void addPlayerID(UserVoter playerID)
        {
            this.playersID.Add(playerID.connectionID);
            if (!Program.ConnIDtoUser.ContainsKey(playerID.connectionID)) 
                Program.ConnIDtoUser.Add(playerID.connectionID,playerID);
            if (playersID.Count == this.numOfHumanPlayers)
                this.status = "playing";
        }

        public int getVotesLeft(int player)
        {
            return this.votesPerPlayer[player];
        }

        public List<int> getVotesPerPlayer()
        {
            return this.votesPerPlayer;
        }

        public int getNumOfRounds()
        {
            return this.rounds;
        }

        public int getTurnsLeft()
        {
            int ans = 0;
            for (int i = 0; i < this.votesPerPlayer.Count; i++)
                ans = ans + this.votesPerPlayer[i];
            return ans;
        }

        public List<int> gameOverPoints()
        {
            List<int> currentPoints = new List<int>();
            
            //find the candidates that won
            List<int> winningCandidates = new List<int>();
            int tmp = this.votes[0];
            for (int i = 0; i < this.numOfCandidates; i++)
            {
                if (this.votes[i] == tmp)
                {
                    winningCandidates.Add(i);
                }
                else if (this.votes[i] > tmp)
                {
                    winningCandidates.Clear();
                    winningCandidates.Add(i);
                    tmp = votes[i];
                }
            }

            this.winners = winningCandidates;

            //calc the points each player gets
            for (int i = 0; i < this.players.Count; i++)
            {
                int pointsSum = 0;
                for (int j = 0; j < winningCandidates.Count; j++)
                {
                    int priority = this.priorities[i].IndexOf(this.candidatesNames.ElementAt(winningCandidates[j]));
                    pointsSum = pointsSum + this.points[priority];

                }

                currentPoints.Add(pointsSum / winningCandidates.Count);
            }

                return currentPoints;
        }

        // check if all players voted for the same candidate for the last 2 turns
        public Boolean checkGameOver()
        {
            if (startSecondrnd && this.roundNumber > 2)
            {
                for (int i = 0; i < this.playersVotes.Count; i++)
                    if (this.playersVotes[i][0] != this.playersVotes[i][1])
                        return false;
                return true;
            }
            else if (!startSecondrnd)
            {
                for (int i = 0; i < this.playersVotes.Count; i++)
                    if (this.playersVotes[i][0] != this.playersVotes[i][1])
                        return false;
                return true;
            }
            else
                return false;

        }

        public string getWinner()
        {
            string ans = "";
            for (int i = 0; i < this.winners.Count; i++)
            {
                if (i == 0)
                    ans = this.candidatesNames[this.winners[i]];
                else
                    ans = ans + ", " + this.candidatesNames[this.winners[i]];

            }
                
            return ans;
        }

        public string getCurrentWinner(int player)
        {
            string ans = "";
            for (int i = 0; i < this.winners.Count; i++)
                ans = ans + "#" + this.priorities[player].IndexOf(this.candidatesNames[this.winners[i]]);
            return ans;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void updateLog()
        {
            if (this.logUpdated == false)
            {
                //priorities
                int j = 5;
                for (int i = 0; i < this.players.Count; i++)
                {
                    string priorityString = Program.ConnIDtoUser[this.playersID[i]].userID.ToString();
                    for (int k = 0; k < this.priorities[i].Count; k++)
                        priorityString = priorityString + "," + this.candidatesNames.IndexOf(this.priorities[i][k]);
                    this.writeToFile.Insert(j, priorityString);
                }

                //titles
                string titles = "time,player,vote,current winner,";
                for (int i = 0; i < this.numOfCandidates; i++)
                {
                    titles = titles + "votes for candidate " + (i) + ",";
                }
                
                for (int i = 0; i < this.players.Count; i++)
                {
                    if (i == this.players.Count - 1)
                        titles = titles + "points player" + Program.ConnIDtoUser[this.playersID[i]].userID.ToString();
                    else
                        titles = titles + "points player" + Program.ConnIDtoUser[this.playersID[i]].userID.ToString() + ",";
                }

                this.writeToFile.Add(titles);
                this.logUpdated = true;
            }


        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void writeToCSVFile()
        {
            this.fileCreated = true;
            //add disconnected players tp log
            int firstRows = 5 + this.players.Count;
            this.writeToFile.Insert(firstRows, "disconnected player id, round");
            int j = firstRows+1;
            for (int i = 0; i < this.playersDisconnected.Count; i++)
            {
                this.writeToFile.Insert(j, this.playersDisconnected[i]);
                j++;
            }

            for (int i = 0; i < this.writeToFile.Count; i++)
                file.write(this.writeToFile[i]);
            file.close();
            
        }

        public int getDefault(int player)
        {
            return this.priorities[player].IndexOf(this.candidatesNames[this.playersVotes[player][0]]);
        }

        public string getPlayersType(int player)
        {
            return this.players[player];
        }

        public int getHumanTurn()
        {
            return this.humanTurn;
        }

        public void setHumanTurn(int n)
        {
            this.humanTurn = n;
        }

        public int getCurrentTurn()
        {
            return this.turn;
        }

        public Boolean isGameOver()
        {
            return this.gameOver;
        }

        public Boolean isVotedDisplay()
        {
            return this.whoVoted;
        }

        private void updateVotedBy(int candIndex, int player){
            for (int i = 0; i < this.numOfCandidates; i++)
            {
                if (this.votedBy[i].Contains(player))
                    this.votedBy[i].Remove(player);
            }
            this.votedBy[candIndex].Add(player);
        }

        public string createWhoVotedString(int player)
        {
            string ans = "";
            for (int i = 0; i < this.priorities[player].Count; i++)
            {
                ans = ans + "#";
                //Debug.Print("---- candidate" + i.ToString());
                List<int> playersWhoVoted = this.votedBy[this.candidatesNames.IndexOf(this.priorities[player][i])];
                int index = -1;
                for (int j = 0; j < playersWhoVoted.Count; j++)
                {
                    //Debug.Print(playersWhoVoted[j].ToString());
                    if (j==0 && playersWhoVoted.Contains(player)){
                        index = playersWhoVoted.IndexOf(player);
                        if (j != index)
                            ans = ans + "p" + (playersWhoVoted[j] + 1);
                        else if (playersWhoVoted.Count > 1)
                        {
                            j++;
                            ans = ans + "p" + (playersWhoVoted[j] + 1);
                            if(j == playersWhoVoted.Count-1)
                                ans = ans + ",p" + (player + 1);

                        }
                        else
                            ans = ans + "p" + (player + 1);
                    }
                    else if (j == (playersWhoVoted.Count - 1) && playersWhoVoted.Contains(player))
                    {
                        if (j != index)
                        {
                            ans = ans + ",p" + (playersWhoVoted[j] + 1);
                            ans = ans + ",p" + (player + 1);
                        }
                        else
                            ans = ans + ",p" + (player + 1);
                    }
                    else if(playersWhoVoted.Contains(player)){
                        if (j == index)
                            continue;
                        else
                        {
                            ans = ans + ",p" + (playersWhoVoted[j] + 1);
                        }
                    }
                    else{
                        if (j == 0)
                            ans = ans + "p" + (playersWhoVoted[j] + 1);
                        else
                            ans = ans + ",p" + (playersWhoVoted[j] + 1);
                    }

                }
            }

            return ans;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void endGame()
        {
            this.gameOver = true;
            if (!fileCreated)
            {
                this.writeToFile[3] = "game ended after:,all players left";
                writeToCSVFile();
            }
        }

        private void startFromSecondRound(string firstRound)
        {
            if (firstRound == "first")
            { //voted by
                // votes
                for (int i = 0; i < this.players.Count; i++)
                {
                    int candIndex = this.candidatesNames.IndexOf(this.priorities[i][0]);
                    this.votedBy[candIndex].Add(i);
                    this.votes[candIndex]++;
                    this.playersVotes[i][0] = candIndex;
                }
                gameOverPoints();
            }
            else if (firstRound == "random")
            {
                for (int i = 0; i < this.players.Count; i++)
                {
                    Random rnd = new Random();
                    int randomCand = rnd.Next(0, this.priorities[i].Count - 1);
                    this.votedBy[randomCand].Add(i);
                    this.votes[randomCand]++;
                    this.playersVotes[i][0] = randomCand;
                }
                gameOverPoints();
            }
        }

        public int turnsToWait(int player)
        {
            if (player > this.turn)
                return (player - this.turn);
            else if (player == this.turn)
                return 0;
            else
                return (this.players.Count - this.turn + player);
        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Game otherGame = obj as Game;
            if (otherGame != null)
                return this.gameID - otherGame.gameID;
            else
                throw new ArgumentException("Object is not a Game");
        }

    }
}
