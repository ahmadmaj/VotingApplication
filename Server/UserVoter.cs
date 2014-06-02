using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Server
{
    public class UserVoter : IComparable
    {
        static int nextId;
        public int userID { get; private set; }
        public string connectionID { get; private set; }
        public Game CurrGame { get; set; }
        public List<int> CurrPriority { get; set; } 
        private List<Game> GamesHistory { get; set; }
        public double Points { get; set; }
        public int score { get; set; }

        public UserVoter(string connectionId)
        {
            this.userID = Interlocked.Increment(ref nextId);
            this.connectionID = connectionId;
            this.Points = 0;
            this.CurrPriority = new List<int>();
            this.GamesHistory = new List<Game>();
        }

        public void JoinGame(Game newgame)
        {
            CurrGame = newgame;
        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            UserVoter otherPlayer = obj as UserVoter;
            if (otherPlayer != null)
                return System.String.Compare(this.connectionID, otherPlayer.connectionID, System.StringComparison.Ordinal);
            else 
               throw new ArgumentException("Object is not a Player");
        }

        public string currPriToString()
        {
            return CurrPriority.Aggregate("", (current, t) => current + "#" + (t));
        }

        public void StoreHistory()
        {
            GamesHistory.Add(CurrGame);
            CurrGame = null;
        }
    }

}
