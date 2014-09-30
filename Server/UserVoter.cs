using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Server
{
    public class UserVoter : IComparable
    {
        static int nextId;
        public TimeSpan? waitDuration = null;
        public string mTurkToken { get; private set; }
        public string mTurkID { get; private set; }
        public string assID { get; private set; }
        public DateTime ConnectTime { get; private set; }
        public int userID { get; private set; }
        public string connectionID { get; private set; }
        public Game CurrGame { get; set; }
        public int inGameIndex { get; private set; } 
        public List<int> CurrPriority { get; set; } 
        public List<Game> GamesHistory { get; private set; }
        public int TotalScore { get; set; }
        public int CurrScore { get; set; } 


        public UserVoter(string connectionId, string workerID = "", string assID= "")
        {
            this.userID = Interlocked.Increment(ref nextId);  
            this.connectionID = connectionId;
            this.mTurkID = workerID;
            this.assID = assID;
            if (workerID != "")
            {
                Guid g = Guid.NewGuid();
                string GuidString = Convert.ToBase64String(g.ToByteArray());
                GuidString = GuidString.Replace("=", "");
                GuidString = GuidString.Replace("+", "");
                this.mTurkToken = GuidString;
            }
            this.CurrScore = 0;
            this.TotalScore = 0;
            this.CurrPriority = new List<int>();
            this.GamesHistory = new List<Game>();
            this.ConnectTime = DateTime.Now;
            Console.WriteLine("Connected: Player {0} ({1})", userID, mTurkID != "" ? mTurkID : connectionId);
        }

        public void JoinGame(Game newgame, int igIndex, List<string> priority)
        {
            CurrGame = newgame;
            inGameIndex = igIndex;
            //TODO: fix that
            foreach (string t in priority)
                CurrPriority.Add(newgame.candidatesNames.IndexOf(t) + 1);
            Console.WriteLine("Join: Player {0} ({1}) joins game {2} ({3})", userID, mTurkID != "" ? mTurkID : connectionID, newgame.gameID, newgame.configFile);
        }

        public void LeaveGame()
        {
            if (CurrGame != null)
                Console.WriteLine("[Error] Left: Player {0} left game {1} ({2}) insufficiant players - returned to waiting room", userID, CurrGame.gameID, CurrGame.configFile);
            CurrGame = null;
        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            UserVoter otherPlayer = obj as UserVoter;
            if (otherPlayer != null)
                return String.Compare(this.connectionID, otherPlayer.connectionID, StringComparison.Ordinal);
            else 
               throw new ArgumentException("Object is not a Player");
        }

        public string currPriToString()
        {
            return CurrPriority.Aggregate("", (current, t) => current + "#" + (t));
        }

        //reset game settings and store history
        public void StoreHistory()
        {
            GamesHistory.Add(CurrGame);
        }
        public void resetGame()
        {
            CurrGame = null;
            CurrPriority = new List<int>();
            inGameIndex = -1;
        }

        public override string ToString()
        {
            string ans = "";
            ans = "User ID:\t" + userID + "\n";
            ans += "Game ID:\t" + GamesHistory[GamesHistory.Count - 1].gameID + "\n";
            ans += "Connected at:\t" + ConnectTime + "\n";
            ans += "mTurk:\t" + mTurkID + "\n";
            ans += "Assignment ID:\t" + assID + "\n";
            ans += "mTurk Token:\t" + mTurkToken + "\n";
            ans += "Wait Time:\t" + waitDuration + "\n";
            ans += "Current Score:\t" + CurrScore + "\n";
            ans += "Total Score:\t" + TotalScore + "\n\n";
            return ans;
        }

        public int NumGamesPlayed()
        {
            return GamesHistory.Count;
        }

        public Boolean HasPlayed(string configFile)
        {
            return GamesHistory.Any(playedGame => playedGame.configFile.Equals(configFile));
        }
    }

}
