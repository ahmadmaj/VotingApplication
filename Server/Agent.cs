using System;
using System.Collections.Generic;
using Server.Agents;

namespace Server
{
    public abstract class Agent
    {
        protected Game CurGame;
        protected List<int> Priorities = new List<int>();
        public bool replaced { get; set; }
        public int replacedplayerID { get; set; }

        protected Agent(Game newgame,List<string> priolist)
        {
            CurGame = newgame;
            foreach (string t in priolist)
                    Priorities.Add(newgame.candidatesNames.IndexOf(t)+1);
        }
        protected Agent(Game newgame, List<int> priolist, int playerID)
        {
            replaced = true;
            replacedplayerID = playerID;
            CurGame = newgame;
            Priorities = new List<int>(priolist);
        }

        public abstract int vote();
        public abstract string agentType();
    }
}
