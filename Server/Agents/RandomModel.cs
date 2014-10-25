using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Agents
{
    class RandomModel : Agent
    {
        public RandomModel(Game newgame, List<string> priolist) : base(newgame, priolist)
        {
        }

        public RandomModel(Game newgame, List<int> priolist, int playerID)
            : base(newgame, priolist, playerID)
        {
        }

        public override int vote()
        {

            int num = CurGame._rnd.Next(0, Priorities.Count);
            return num;
        }

        public override string agentType()
        {
            return "Random_Model";
        }
    }
}
