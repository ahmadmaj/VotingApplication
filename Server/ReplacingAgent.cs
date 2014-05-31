using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class ReplacingAgent : Agent
    {
        private int playerID;
        public ReplacingAgent(string type, int playerId):base(type){
            this.playerID = playerId;
        }

        public int getPlayerID()
        {
            return this.playerID;
        }
            
    }
}
