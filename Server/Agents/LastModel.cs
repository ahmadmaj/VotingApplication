using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Agents
{
    class LastModel : Agent
    {
        public LastModel(Game newgame, List<string> priolist,int aID) : base(newgame, priolist, aID)
        {
        }
        public LastModel(Game newgame, List<int> priolist, int playerID)
            : base(newgame, priolist, playerID)
        {
        }

        public override int vote()
        {
            return Priorities.Count-1; //always vote for last priority
        }

        public override string agentType()
        {
            return "Last_Priority_Model";
        }
    }
}
