using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Game
    {
        private string status;
        private int id;
        private int numOfHumanPlayers;
        private List<string> players;
        private List<string> playersID;
        private int numOfCandidates;
        private int numOfTurns;
        private List<string> candidatesNames;
        private List<int> votesPerPlayer; //votes left for each player
        //private List<List<int>> votedBy; //candidates, players who voted
        private List<int> votes; //number of votes each candidate got
        private List<int> points;
        private List<List<string>> priorities;
        private int turn; //index of the player who's turn it is
        private List<int> winners;
        private List<string> writeToFile;
        private List<Agent> agents;
        private Boolean rounds;
        private List<List<int>> playersVotes;
        private int roundNumber;


        public Game(int humans, List<string> players, int candidates, int turns, List<string> candNames, List<int> vote, List<int> points, List<List<string>> priority, List<Agent> agent, Boolean round)
        {
            this.status = "init";
            this.numOfHumanPlayers = humans;
            this.players = players;
            this.playersID = new List<string>();
            this.numOfCandidates = candidates;
            this.numOfTurns = turns;
            this.candidatesNames = candNames;
            this.points = points;
            this.priorities = priority;
            this.votesPerPlayer = vote;
            //this.votedBy = new List<List<int>>();
            //for (int i = 0; i < numOfCandidates; i++)
            //{
            //    votedBy.Add(new List<int>());
            //}
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
            this.roundNumber = 1;
            this.winners = new List<int>();

            this.writeToFile = new List<string>();
            this.writeToFile.Add("number of players:," + players.Count.ToString());
            this.writeToFile.Add("number of candidates:," + this.numOfCandidates.ToString());
            this.writeToFile.Add("game ended after:,");
            string titles = "time,player,vote,current winner,";
            for (int i = 0; i < this.players.Count; i++)
            {
                if (i == this.players.Count - 1)
                    titles = titles + "points player" + (i+1);
                else
                    titles = titles + "points player" + (i+1) + ",";
            }
            this.writeToFile.Add(titles);
            this.agents = agent;
            this.rounds = round;

        }


        public int vote(int candidate, int player)
        {
            if (this.numOfTurns > 0 && this.votesPerPlayer[player] > 0)
            {
                int candIndex = this.candidatesNames.IndexOf(this.priorities[player][candidate]);
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
                this.numOfTurns--;

                //update log
                string time = DateTime.Now.ToString();
                List<int> currentPoints = gameOverPoints(); //also updates the current winners
                string winnersString = "";
                for(int i=0;i<this.winners.Count;i++){
                    if (i == 0)
                        winnersString = this.candidatesNames[this.winners[i]];
                    else
                        winnersString = winnersString + " " + this.candidatesNames[this.winners[i]];
                }
                string pointsString = "";
                for (int i = 0; i < currentPoints.Count; i++)
                {
                    if (i == 0)
                        pointsString = currentPoints[i].ToString();
                    else
                        pointsString = pointsString + "," + currentPoints[i].ToString();
                }
                this.writeToFile.Add(time + "," + player.ToString() + "," + this.candidatesNames[candIndex] + "," + winnersString + "," + pointsString);


                Boolean gameOver = false;
                if (player == this.players.Count - 1)
                {
                    this.roundNumber++;
                    gameOver = checkGameOver();
                }

                if (this.numOfTurns > 0 && !gameOver)
                    return 1; //the game is not over
                else
                {
                    if (gameOver)
                        this.writeToFile[2] = "game ended after:,the players voted for the same candidate for 2 turns";
                    else
                        this.writeToFile[2] = "game ended after:,the turns are over";
                    writeToCSVFile();
                    return -1; //game over
                }
                    
            }
            else if (this.numOfTurns == 0){
                writeToCSVFile();
                return -1; //game over
            }
                
            else
                return -2;
        }


        public int getTurn(string playerID)
        {
            if (this.playersID[turn] == playerID)
                return 1;
            else
                return 0;
        }

        public int getNextTurn(string currentPlayerID)
        {
            int ans = turn + 1;
            if (ans >= this.players.Count)
                ans = 0;
            int gameStatus = 1;
            while (this.players[ans] != "human")
            {
                //gameStatus = vote(0, ans);
                if (this.rounds){
                    int votefor = this.agents[0].vote(this.priorities[ans], this.candidatesNames, this.roundNumber);
                    gameStatus = vote(votefor, ans);
                }
                else{
                    int votefor = this.agents[ans-this.numOfHumanPlayers].vote(this.priorities[ans], this.candidatesNames, this.roundNumber);
                    gameStatus = vote(votefor, ans);
                }
                ans++;
                if (ans >= this.players.Count)
                    ans = 0;
            }
            turn = ans;
            if (gameStatus == 1)
                return ans;
            else if (gameStatus == -1)
                return gameStatus;
            else
                return -2;
        }

        public string getPlayerID(int player)
        {
            return this.playersID.ElementAt(player);
        }

        public List<string> getPlayersIDList()
        {
            return this.playersID;
        }

        public int getPlayerIndex(string player)
        {
            return this.playersID.IndexOf(player);
        }

        public List<string> getPlayers()
        {
            return this.players;
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

        public void addPlayerID(string playerID)
        {
            this.playersID.Add(playerID);
            if (playersID.Count == this.numOfHumanPlayers)
                this.status = "playing";
        }

        public int getVotesLeft(int player)
        {
            return this.votesPerPlayer[player];
        }

        public int getTurnsLeft()
        {
            return this.numOfTurns;
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
            for (int i = 0; i < this.playersVotes.Count; i++)
                if (this.playersVotes[i][0] != this.playersVotes[i][1])
                    return false;
            return true;
        }

        public string getWinner()
        {
            string ans = "";
            for (int i = 0; i < this.winners.Count; i++)
                ans = ans + " ," + this.candidatesNames[this.winners[i]];
            return ans;
        }

        public void writeToCSVFile()
        {
            for (int i = 0; i < this.writeToFile.Count; i++)
                Program.file.write(this.writeToFile[i]);
            Program.file.close();
        }

        public int getDefault(int player)
        {
            return this.priorities[player].IndexOf(this.candidatesNames[this.playersVotes[player][0]]);
        } 

    }
}
