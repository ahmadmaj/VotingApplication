using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Agents
{
    class FirstModel : Agent
    {
        public FirstModel(Game newgame, List<string> priolist,int aID) : base(newgame, priolist, aID)
        {
        }
        public FirstModel(Game newgame, List<int> priolist, int playerID)
            : base(newgame, priolist, playerID)
        {
        }
        public override int vote()
        {
            return 0; //always vote for first priority
        }
        public override string agentType()
        {
            return "First_Priority_Model";
        }
    }
}
