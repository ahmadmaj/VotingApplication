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
        private List<int> initialState; 
        public NoiseModel(Game newgame, List<string> priolist) : base(newgame, priolist)
        {
            initialState = new List<int>(newgame.numOfCandidates);
            for (int i = 0; i < newgame.numOfCandidates; i++)
                initialState.Add(0);
            foreach (int candIndex in newgame.priorities.Select(pref => newgame.candidatesNames.IndexOf(pref[0])))
                initialState[candIndex]++;
        }

        public NoiseModel(Game newgame, List<int> priolist, int playerID)
            : base(newgame, priolist,playerID)
        {
        }
        public override int vote()
        {
            //List<List<int>> currentState = CurGame.votedBy;
            int randomNumber = CurGame._rnd.Next(1, initialState.Sum()+1);
            int ans = 1;
            foreach (int cand in initialState)
            {
                if (randomNumber <= cand)
                    return Priorities.IndexOf(ans);
                randomNumber -= cand;
                ans++;
            }
            return Priorities.IndexOf(ans);
        }
        public override string agentType()
        {
            return "Noise_Model";
        }
    }
}
