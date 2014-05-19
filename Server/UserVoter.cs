using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class UserVoter : IComparable
    {
        public string id { get; private set; }
        public Game CurrGame { get; private set; }
        public List<int> CurrPriority { get; set; } 
        private List<Game> GamesHistory { get; set; }
        public double Points { get; set; }
        public int score { get; set; }

        public UserVoter(string id, Game game)
        {
            this.id = id;
            this.CurrGame = game;
            this.Points = 0;
            this.CurrPriority = new List<int>();
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            UserVoter otherPlayer = obj as UserVoter;
            if (otherPlayer != null)
                return System.String.Compare(this.id, otherPlayer.id, System.StringComparison.Ordinal);
            else 
               throw new ArgumentException("Object is not a Player");
        }

        public string currPriToString()
        {
            return CurrPriority.Aggregate("", (current, t) => current + "#" + (t));
        }
    }

}
