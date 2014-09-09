using System;
using System.Collections.Generic;

namespace Server
{
    public class GameDetails
    {
        public readonly int numOfTotalPlayers;// { get; private set; }
        public readonly int numOfHumanPlayers;// { get; private set; }
        public readonly int numOfCandidates;// { get; private set; }
        public readonly int numOfVotes;// { get; private set; }
        public readonly int numOfRounds;// { get; private set; }//number of rounds in the game
        public readonly List<string> candidatesNames;// { get; private set; }
        public readonly List<string> players;// { get; private set; } // players type (human, compter, replaced)
        public readonly List<int> points;// { get; private set; }
        public readonly List<List<string>> priorities;// { get; private set; }
        public readonly List<Agent> agents;// { get; private set; }
        public readonly Boolean isRounds;// { get; private set; }
        public readonly int whoVoted;// { get; private set; }
        public readonly string startSecondRound;// { get; private set; }
        public readonly string configFile;// { get; private set; }

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
