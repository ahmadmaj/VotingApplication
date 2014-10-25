using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server.Agents
{
    class NoiseModel : Agent
    {
        public NoiseModel(Game newgame, List<string> priolist) : base(newgame, priolist)
        {
        }
        public NoiseModel(Game newgame, List<int> priolist, int playerID)
            : base(newgame, priolist,playerID)
        {
        }
        public override int vote()
        {
            List<List<int>> currentState = CurGame.votedBy;
            int randomNumber = CurGame._rnd.Next(0, CurGame.getNumOfPlayers()+1);
            int ans = 0;
            foreach (List<int> cand in currentState)
            {
                if (randomNumber <= cand.Count)
                {
                    return ans;
                }
                randomNumber -= cand.Count;
                ans++;
            }
            return ans;
        }
        public override string agentType()
        {
            return "Noise_Model";
        }

      
    }
}
