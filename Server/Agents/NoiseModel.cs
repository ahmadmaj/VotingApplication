using System.Collections.Generic;
using System.Linq;

namespace Server.Agents
{
    class NoiseModel : Agent
    {
        private List<int> initialState; 
        public NoiseModel(Game newgame, List<string> priolist,int ID,List<int> initState) : base(newgame, priolist, ID)
        {
            initialState = initState;
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
