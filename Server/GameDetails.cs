using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class GameDetails
    {
        private int numOfParticipants;
        private int numOfTotalPlayers;
        private int numOfHumanPlayers;
        private int numOfCandidates;
        private int numOfVotes;
        private int numOfTurnsInGame;
        private List<string> candidatesNames;
        private List<string> players;
        private List<int> points;
        private List<List<string>> priorities;
        private List<Agent> agents;
        private Boolean rounds;

        public GameDetails(int participants, int humanPlayers, int players, int candidates, int votes, int turns, List<string> candNames, List<string> player, List<int> points, List<List<string>> priority, List<Agent> agent, Boolean round)
        {
            this.numOfParticipants = participants;
            this.numOfHumanPlayers = humanPlayers;
            this.numOfTotalPlayers = players;
            this.numOfCandidates = candidates;
            this.numOfVotes = votes;
            this.numOfTurnsInGame = turns;
            this.candidatesNames = candNames;
            this.players = player;
            this.points = points;
            this.priorities = priority;
            this.agents = agent;
            this.rounds = round;
        }


        public int getNumOfHumanPlayers()
        {
            return this.numOfHumanPlayers;
        }

        public int getNumOfTotalPlayers()
        {
            return this.numOfTotalPlayers;
        }

        public List<string> getPlayers()
        {
            return this.players;
        }

        public int getNumOfCandidates()
        {
            return this.numOfCandidates;
        }

        public int getVotes()
        {
            return this.numOfVotes;
        }

        public List<int> getVotesList()
        {
            List<int> ans = new List<int>();
            for (int i = 0; i < this.numOfTotalPlayers; i++)
                ans.Add(this.numOfVotes);

            return ans;
        }

        public int getTurns()
        {
            return this.numOfTurnsInGame;
        }

        public List<int> getPoints()
        {
            return this.points;
        }
        public List<List<string>> getPriorities()
        {
            return this.priorities;
        }

        public List<string> getCandidatesNames()
        {
            return this.candidatesNames;
        }

        public List<Agent> getAgents()
        {
            return this.agents;
        }

        public Boolean getRounds()
        {
            return this.rounds;
        }

    }
}
