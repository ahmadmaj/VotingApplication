using System;
using System.Collections.Generic;

namespace Server
{
    public class GameDetails
    {
        public int numOfTotalPlayers { get; private set; }
        public int numOfHumanPlayers { get; private set; }
        public int numOfCandidates { get; private set; }
        public int numOfVotes { get; private set; }
        public int numOfRounds { get; private set; }//number of rounds in the game
        public List<string> candidatesNames { get; private set; }
        public List<string> players { get; private set; } // players type (human, compter, replaced)
        public List<int> points { get; private set; }
        public List<List<string>> priorities { get; private set; }
        public List<Agent> agents { get; private set; }
        public Boolean isRounds { get; private set; }
        public int whoVoted { get; private set; }
        public string startSecondRound { get; private set; }
        public string configFile { get; private set; }

        public GameDetails(int humanPlayers, int players, int candidates, int rounds, List<string> candNames, List<string> player, List<int> points, List<List<string>> priority, List<Agent> agent, Boolean round, int voted, string start, string config)
        {
            this.numOfHumanPlayers = humanPlayers;
            this.numOfTotalPlayers = players;
            this.numOfCandidates = candidates;
            this.numOfRounds = rounds;
            this.candidatesNames = candNames;
            this.players = player;
            this.points = points;
            this.priorities = priority;
            this.agents = agent;
            this.isRounds = round;
            this.whoVoted = voted;
            this.startSecondRound = start;
            this.configFile = config;
        }

        public List<int> getVotesList()
        {
            List<int> ans = new List<int>();
            for (int i = 0; i < this.numOfTotalPlayers; i++)
                ans.Add(this.numOfRounds);

            return ans;
        }

    }
}
