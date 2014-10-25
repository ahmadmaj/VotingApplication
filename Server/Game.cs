using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Agents;

namespace Server
{
    public class Game : IComparable
    {
        public string status { get; private set; }
        static int nextId;
        public string configFile { get; private set; }
        public int gameID {get; private set;}
        public int numOfHumanPlayers { get; private set; }
        public List<string> playersTypeOrder { get; private set; }// players type (human, compter, replaced)
        public List<string> playersID { get; private set; }
        public int numOfCandidates { get; private set; }
        public int rounds { get; private set; } //number of rounds in the game
        public List<string> candidatesNames { get; private set; }
        public List<int> votesPerPlayer { get; private set; } //votes left for each player
        public int whoVoted { get; private set; } //to show in the game who voted for the player
        
        public List<List<int>> votedBy {get; private set; }//candidates, players who voted
        public List<int> votes { get; private set; } //number of votes each candidate got
        public List<int> points { get; private set; }
        public List<List<string>> priorities { get; private set; }
        public string prioritiesJSON { get; private set; }
        public int turn { get; private set; } //index of the player who's turn it is
        public int humanTurn { get; set; } //index of the human player who's turn it is
        private int computerTurn; //index of the agent who's turn it is
        private int replaceTurn; //index of the replacing agent who's turn it is
        private Boolean firstTurn;
        private List<int> curWinners;
        public List<int> currentPoints { get; private set; } 
        private WriteData file;
        private List<string> writeToFile;
        private List<Agent> agents;
        private List<Agent> replacingAgents;
        private List<string> playersDisconnected;
        private List<List<int>> playersVotes;
        private int roundNumber; //current round
        public Boolean gameOver { get; private set; }
        private Boolean startSecondrnd;
        private Boolean firstRound;
        private Boolean fileCreated;
        private Boolean logUpdated;
        public Boolean endNextVote = false;
        protected internal Random _rnd = new Random();
        public Game(GameDetails gamedets)
        {
            string start = gamedets.startSecondRound;
            gameID = Interlocked.Increment(ref nextId);
            this.status = "init";
            this.numOfHumanPlayers = gamedets.numOfHumanPlayers;
            this.configFile = gamedets.configFile;
            this.playersTypeOrder = new List<string>(gamedets.players);
            this.playersID = new List<string>();
            this.numOfCandidates = gamedets.numOfCandidates;
            this.candidatesNames = new List<string>(gamedets.candidatesNames);
            this.points = new List<int>(gamedets.points);
            this.priorities = new List<List<string>>(gamedets.priorities);
            this.prioritiesJSON = "";
            foreach (List<string> prio in this.priorities)
                prioritiesJSON += string.Join(",", prio) + "#";
            this.prioritiesJSON=this.prioritiesJSON.Remove(this.prioritiesJSON.Length - 1);
            this.votesPerPlayer = new List<int>(gamedets.getVotesList());
            this.rounds = gamedets.numOfRounds;
            this.whoVoted = gamedets.whoVoted;
            this.votedBy = new List<List<int>>();
            for (int i = 0; i < numOfCandidates; i++)
            {
                votedBy.Add(new List<int>());
            }
            this.playersVotes = new List<List<int>>();
            for (int i = 0; i < this.playersTypeOrder.Count; i++)
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
            this.curWinners = new List<int>();
            this.currentPoints = new List<int>();
            this.file = new WriteData(gameID);
            this.fileCreated = false;
            this.writeToFile = new List<string>();
            this.writeToFile.Add("config file: ," + gamedets.configFile);
            this.writeToFile.Add("number of players:," + gamedets.players.Count);
            this.writeToFile.Add("number of candidates:," + this.numOfCandidates);
            this.writeToFile.Add("game ended after:,");
            this.writeToFile.Add("player,priorities");
            this.logUpdated = false;
            this.agents = new List<Agent>();
            int idx = -1;
            foreach (string poa in playersTypeOrder)
            {
                idx++;
                if (poa == "human") continue;
                switch (poa)
                {
                    case "Random":
                        agents.Add(new RandomModel(this, priorities[idx]));
                        break;
                    case "First":
                        agents.Add(new FirstModel(this, priorities[idx]));
                        break;
                    case "Last":
                        agents.Add(new LastModel(this, priorities[idx]));
                        break;
                    case "Noise":
                        agents.Add(new NoiseModel(this, priorities[idx]));
                        break;
                }
            }

            this.replacingAgents = new List<Agent>();
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

        public int vote(int candidatePriority, int player, int duration, string connID = "")
        {
            if (votesPerPlayer[player] <= 0) throw new InvalidOperationException("player voted with 0 votes..");
            
            int candIndex = this.candidatesNames.IndexOf(priorities[player][candidatePriority]);
            updateVotedBy(candIndex, player);

            playersVotes[player][1] = playersVotes[player][0];
            playersVotes[player][0] = candIndex;

            votesPerPlayer[player]--;

            //update log
            string time = DateTime.Now.ToString();
            updateCurWinner(); //update current winner and scores
            string winnersString = string.Join(" ", curWinners.Select(x => (x + 1).ToString()).ToArray());
            string candidatesString = string.Join(",", votedBy.Select(x => x.Count.ToString()).ToArray());
            string pointsString = string.Join(",", currentPoints.Select(x => x.ToString()).ToArray());

            String playerID = "";
            if (playersTypeOrder[player] == "replaced")
                playerID = "replace_" + replacingAgents[replaceTurn].replacedplayerID + "_" +
                           replacingAgents[replaceTurn].agentType();
            else if (playersTypeOrder[player] != "human")
            {
                playerID = "comp_" + computerTurn + "_" + agents[computerTurn].agentType();
            }
            else
                playerID = Program.ConnIDtoUser[connID].userID.ToString();

            writeToFile.Add(time + "," + playerID + "," + (candIndex + 1) + "," + duration + "," +
                                 winnersString + "," + candidatesString + "," + pointsString);

            gameOver = checkGameConverged();

            //Boolean gameOver = false;
            if (player == getNumOfPlayers() - 1)
                roundNumber++;

            if (rounds >= roundNumber && !gameOver && !endNextVote)
                return 1; //the game is not over

            GameOver();
            return -1;
            /*
            if (rounds >= roundNumber) return -2;
            playersDump();
            writeToCSVFile();
            return -1; //game over
            */
        }

        public void GameOver()
        {
            if (gameOver)
                writeToFile[3] = "game ended after:,the players voted for the same candidate for 2 turns";
            else
                if (!endNextVote) writeToFile[3] = "game ended after:,the turns are over";
                else writeToFile[3] = "game ended after:,player disconnected";
            playersDump();
            writeToCSVFile();
        }

        void playersDump()
        {
            Console.WriteLine("[{0}] DONE!: Game {1} finished...", DateTime.Now.ToString("HH:mm:ss"), gameID);
            updatePlayersScores();
            string[] lines = new string[playersID.Count];
            int x = 0;
            foreach(string playID in playersID){
                UserVoter playObjer;
                if (Program.ConnIDtoUser.TryGetValue(playID, out playObjer))
                    lines[x++] = playObjer.ToString();
            }
            string dumppath = string.Format("{0}/playersDump/{1}/", Directory.GetCurrentDirectory(), Program.logFolder);
            Directory.CreateDirectory(dumppath);
            File.WriteAllLines(dumppath + gameID + ".txt", lines);
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public int getTurn(string playerID)
        {
            if (playersTypeOrder[turn] == "human")
            {
                firstTurn = false;
                firstRound = false;
                return playersID[humanTurn] == playerID ? 1 : 0;
            }
            getNextTurn();
            firstTurn = false;
            while (playersTypeOrder[turn] != "human")
                getNextTurn();
            firstRound = false;
            return playersID[humanTurn] == playerID ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int getNextTurn()
        {
            //if (!this.firstTurn)
            //    this.turn++;
            if (turn >= getNumOfPlayers())
                turn = 0;
            int gameStatus = 1;
            if (playersTypeOrder[turn] == "replaced")
            {
                int votefor = replacingAgents[replaceTurn].vote();
                if (!firstRound)
                    Thread.Sleep(5000);
                gameStatus = vote(votefor, turn, 5);
                replaceTurn = ++replaceTurn % replacingAgents.Count;
            }
            else if (playersTypeOrder[turn] != "human")
            {
                int votefor = agents[computerTurn].vote();
                if (!firstRound)
                    Thread.Sleep(5000);
                gameStatus = vote(votefor, turn, 5);
                computerTurn = ++computerTurn % agents.Count;
            }
            else
            {
                if (!firstTurn)
                    humanTurn++;
                humanTurn %= playersID.Count;
            }
            turn = ++turn % getNumOfPlayers();
            switch (gameStatus)
            {
                case 1:
                    return turn;
                case -1:
                    return gameStatus;
                default:
                    return -2;
            }
        }

        public string getPlayerID(int player)
        {
            var playersId = playersID;
            return playersId != null ? playersId.ElementAt(player) : null;
        }

        public void deletePlayerID(string id)
        {
            var playersId = playersID;
            if (playersId != null) playersId.Remove(id);
        }

        public int getPlayerIndex(string player)
        {
            int humanCounter = this.playersID.IndexOf(player);
            int ans = 0;
            foreach (string type in playersTypeOrder)
            {
                if (type == "human") 
                    if (humanCounter==0)
                        return ans;
                    else
                        humanCounter--;
                ans++;
            }
            return -1; //error: should never reach this point
        }


        public void replacePlayer(int index, UserVoter user)
        {
            playersTypeOrder[index] = "replaced";
            replacingAgents.Add(new FirstModel(this, user.CurrPriority,user.userID));
            replaceTurn++;
            string disconnect = user.userID + "," + roundNumber;
            playersDisconnected.Add(disconnect);
        }

        public int getNumOfPlayers()
        {
            return this.playersTypeOrder.Count;
        }


        public Boolean addPlayerID(UserVoter playerID)
        {
            if (status == "playing") return false;
            playersID.Add(playerID.connectionID);
            int indexofP = getPlayerIndex(playerID.connectionID);
            playerID.JoinGame(this, indexofP, priorities[indexofP]);
            if (playersID.Count == numOfHumanPlayers)
            {
                status = "playing";
                updateLog();
            }
            return true;
        }

        public int getTurnsLeft()
        {
            int ans = 0;
            for (int i = 0; i < this.votesPerPlayer.Count; i++)
                ans = ans + this.votesPerPlayer[i];
            return ans;
        }

        public void updateCurWinner()
        {            
            //find current winning candidates
            List<int> winningCandidates = new List<int>();
            int maxVotes = this.votedBy[0].Count;

            foreach (List<int> votes in votedBy)
            {
                if (votes.Count == maxVotes)
                    winningCandidates.Add(votedBy.IndexOf(votes));
                else if (votes.Count > maxVotes)
                {
                    winningCandidates.Clear();
                    winningCandidates.Add(votedBy.IndexOf(votes));
                    maxVotes = votes.Count;
                }
            }
            curWinners = winningCandidates;
            updatePlayersPoints(); //update players scores
        }

        public void updatePlayersPoints()
        {
            //calc the points for each player based on current winner
            currentPoints.Clear();
            for (int i = 0; i < getNumOfPlayers(); i++)
            {
                int pointsSum = 0;
                for (int j = 0; j < curWinners.Count; j++)
                {
                    int priority = priorities[i].IndexOf(candidatesNames.ElementAt(curWinners[j]));
                    pointsSum += points[priority];
                }
                currentPoints.Add(pointsSum / curWinners.Count);
            }
            foreach (string playerid in playersID)
            {
                UserVoter playerUser;
                if (Program.ConnIDtoUser.TryGetValue(playerid, out playerUser))
                    playerUser.CurrScore = currentPoints[playerUser.inGameIndex];
            }
        } 

        // check if all players voted for the same candidate for the last 2 turns
        public Boolean checkGameConverged()
        {
            return ((startSecondrnd && roundNumber >= 2) || !startSecondrnd) && playersVotes.All(t => t[0] == t[1]);
        }

        public void updatePlayersScores()
        {
            foreach (string playerid in playersID)
            {
                UserVoter playerUser;
                if (!Program.ConnIDtoUser.TryGetValue(playerid, out playerUser)) continue;
                playerUser.TotalScore += playerUser.CurrScore;
                playerUser.StoreHistory();
            }
        }

        public string getWinner()
        {
            string ans = "";
            for (int i = 0; i < this.curWinners.Count; i++)
            {
                if (i == 0)
                    ans = this.candidatesNames[this.curWinners[i]];
                else
                    ans = ans + ", " + this.candidatesNames[this.curWinners[i]];

            }
                
            return ans;
        }

        public string getCurrentWinner(int player)
        {
            string ans = "";
            for (int i = 0; i < this.curWinners.Count; i++)
                ans = ans + "#" + this.priorities[player].IndexOf(this.candidatesNames[this.curWinners[i]]);
            return ans;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void updateLog()
        {
            if (this.logUpdated == false)
            {
                //priorities
                int j = 5;
                for (int i = 0; i < getNumOfPlayers(); i++)
                {
                    string priorityString;
                    if(this.playersTypeOrder[i] != "human")
                        priorityString = "comp_" + getAgentNumber(i).ToString();
                    else
                        priorityString = Program.ConnIDtoUser[playersID[gethumanNumber(i)]].userID.ToString();
                    for (int k = 0; k < this.priorities[i].Count; k++)
                        priorityString = priorityString + "," + (candidatesNames.IndexOf(priorities[i][k]) +1);
                    this.writeToFile.Insert(j, priorityString);
                }

                //titles
                string titles = "time,player,vote,duration,current winner,";
                for (int i = 0; i < this.numOfCandidates; i++)
                {
                    titles = titles + "votes for candidate " + (i+1) + ",";
                }

                for (int i = 0; i < getNumOfPlayers(); i++)
                {
                    if (i == getNumOfPlayers() - 1)
                    {
                        if(this.playersTypeOrder[i] == "human")
                            titles = titles + "points player" + Program.ConnIDtoUser[this.playersID[gethumanNumber(i)]].userID.ToString();
                        else
                            titles = titles + "points player comp_" + getAgentNumber(i).ToString();

                    }
                    else
                    {
                        if (this.playersTypeOrder[i] == "human")
                            titles = titles + "points player" + Program.ConnIDtoUser[this.playersID[gethumanNumber(i)]].userID.ToString() + ",";
                        else
                            titles = titles + "points player comp_" + getAgentNumber(i).ToString() + ",";
                    }

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
            int firstRows = 5 + getNumOfPlayers();
            var toFile = this.writeToFile;
            if (toFile != null)
            {
                toFile.Insert(firstRows, "disconnected player id, round");
                int j = firstRows+1;
                for (int i = 0; i < this.playersDisconnected.Count; i++)
                {
                    toFile.Insert(j, this.playersDisconnected[i]);
                    j++;
                }

                for (int i = 0; i < toFile.Count; i++)
                    file.write(toFile[i]);
            }
            file.Close();
            
        }

        public int getDefault(int player)
        {
            return playersVotes[player][0] == -1 ? 0 : priorities[player].IndexOf(candidatesNames[playersVotes[player][0]]);
        }

        private void updateVotedBy(int candIndex, int player)
        {
            foreach (List<int> candidate in votedBy.Where(candidate => candidate.Contains(player)))
                candidate.Remove(player);
            votedBy[candIndex].Add(player);
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void endGame()
        {
            this.gameOver = true;
            if (!fileCreated)
            {
                this.writeToFile[3] = "game ended after:,all player\\s left";
                writeToCSVFile();
            }
            Program.PlayingGames.Remove(this);
        }

        private void startFromSecondRound(string firstRound)
        {
            if (firstRound == "first")
            { //voted by
                // votes
                for (int i = 0; i < getNumOfPlayers(); i++)
                {
                    int candIndex = this.candidatesNames.IndexOf(this.priorities[i][0]);
                    this.votedBy[candIndex].Add(i);
                    //this.playersVotes[i][0] = candIndex;
                }
                updateCurWinner();
            }
            else if (firstRound == "random")
            {
                for (int i = 0; i < getNumOfPlayers(); i++)
                {
                    Random rnd = new Random();
                    int randomCand = rnd.Next(0, this.priorities[i].Count - 1);
                    this.votedBy[randomCand].Add(i);
                    //this.playersVotes[i][0] = randomCand;
                }
                updateCurWinner();
            }
        }

        public int turnsToWait(int player)
        {
            if (player > this.turn)
                return (player - this.turn);
            else if (player == this.turn)
                return 0;
            else
                return (getNumOfPlayers() - this.turn + player);
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

        public int getAgentNumber(int index)
        {
            int ans = 0;
            for (int i = 0; i < index; i++)
                if (this.playersTypeOrder[i] != "human")
                    ans++;
            return ans;
        }

        public int gethumanNumber(int index)
        {
            int ans = 0;
            for (int i = 0; i < index; i++)
                if (this.playersTypeOrder[i] == "human")
                    ans++;
            return ans;
        }



        /**
         * String Generators
         *  seperetad by #
         */

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
                    if (j == 0 && playersWhoVoted.Contains(player))
                    {
                        index = playersWhoVoted.IndexOf(player);
                        if (j != index)
                            ans = ans + "p" + (playersWhoVoted[j] + 1);
                        else if (playersWhoVoted.Count > 1)
                        {
                            j++;
                            ans = ans + "p" + (playersWhoVoted[j] + 1);
                            if (j == playersWhoVoted.Count - 1)
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
                    else if (playersWhoVoted.Contains(player))
                    {
                        if (j == index)
                            continue;
                        else
                        {
                            ans = ans + ",p" + (playersWhoVoted[j] + 1);
                        }
                    }
                    else
                    {
                        if (j == 0)
                            ans = ans + "p" + (playersWhoVoted[j] + 1);
                        else
                            ans = ans + ",p" + (playersWhoVoted[j] + 1);
                    }

                }
            }

            return ans;
        }

        public string createCandNamesString(UserVoter userPlayer)
        {
            List<string> priority = priorities.ElementAt(userPlayer.inGameIndex);
            return priority.Aggregate("", (current, t) => current + "#" + t);
        }
        
        public string createNumOfVotesString(UserVoter player)
        {
            string numOfvotesString = "";
            List<string> priority = priorities.ElementAt(player.inGameIndex);
            List<string> candNames = candidatesNames;
            for (int i = 0; i < votedBy.Count; i++)
                numOfvotesString = numOfvotesString + "#" + votedBy[candNames.IndexOf(priority[i])].Count;
            return numOfvotesString;
        }
        public string createPointsString()
        {
            return points.Aggregate("", (current, point) => current + ("#" + point));
        }
 
        
    }
}
