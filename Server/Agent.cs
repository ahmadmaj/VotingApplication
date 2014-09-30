using System;
using System.Collections.Generic;

namespace Server
{
    public class Agent
    {
        private int id; //1-random, 2-first, 3-last, 4-priority, 5-rounds, 6-turns
        private int priority;
        private List<List<int>> roundsList;
        private List<string> turns;
        private int Id;
        private string agentType;

        public Agent(string type)
        {
            this.agentType = type;
            if (type == "RANDOM")
            {
                this.id = 1;
                this.priority = -1;
            }
            else if (type == "FIRST")
            {
                this.id = 2;
                this.priority = -1;
            }
            else if (type == "LAST")
            {
                this.id = 3;
                this.priority = -1;
            }
        }

        public Agent(string type, int p)
        {
            this.agentType = type;
            this.id = 4;
            this.priority = p;
        }

        public Agent(string type, List<List<int>> rounds)
        {
            this.agentType = type;
            if (type == "ROUNDS")
            {
                this.id = 5;
                this.roundsList = rounds;
            }
        }

        public Agent(string type, List<string> votes)
        {
            this.agentType = type;
            if (type == "TURNS")
            {
                this.id = 6;
                this.turns = votes;
            }
        }
        public int vote(List<string> priorities, List<string> candNames, int round)
        {
            if (this.id == 1) //random
            {
                Random rnd = new Random();
                int num = rnd.Next(0, priorities.Count-1);
                return num;
            }
            else if (this.id == 2) //first
                return 0;
            else if (this.id == 3) //last
                return priorities.Count - 1;
            else if (this.id == 4) //priority
                return this.priority - 1;
            else if (this.id == 5) //rounds
            {
                int ans = -2;
                for (int i = 0; i < candNames.Count; i++)
                {
                    if (this.roundsList[round - 1][i] > 0)
                    {
                        this.roundsList[round - 1][i]--;
                        ans = priorities.IndexOf(candNames[i]);
                        break;
                    }
                }
                return ans;
            }
            else if (this.id == 6) //turns
            {
                string v = this.turns[round - 1];
                if (v == "random")
                {
                    Random rnd = new Random();
                    int num = rnd.Next(0, priorities.Count - 1);
                    return num;
                }
                else if (v == "first")
                    return 0;
                else if(v == "last")
                    return priorities.Count - 1;
                else
                {
                    string[] p = v.Split(' ');
                    if (p[0] == "priority")
                    {
                        return Convert.ToInt32(p[1])-1;
                    }
                    else
                        return -2;
                }
            }
            else
                return -2;
        }

        public Boolean isRounds()
        {
            return id == 5;
        }

        public string getType(){
            return this.agentType;
        }
    }
}
