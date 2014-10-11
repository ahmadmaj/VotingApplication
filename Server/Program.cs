using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;


namespace Server
{
    public static class Program
    {
        public static List<GameDetails> gameDetailsList = new List<GameDetails>();
        public static GameDetails gameDetails;
        public static Dictionary<string, UserVoter> ConnIDtoUser = new Dictionary<string, UserVoter>(); //for each playID the User class he is
        public static List<Game> PlayingGames = new List<Game>();
        public static String logFolder = "";
        public static Boolean mTurkMode = false;

        public static UserVoter getplayerUser(string id)
        {
            UserVoter player = null;
            ConnIDtoUser.TryGetValue(id, out player);
            return player;
        }
        
        public static Game getplayersGame(string id)
        {
            UserVoter tmpvoter;
            return ConnIDtoUser.TryGetValue(id,out tmpvoter) ? tmpvoter.CurrGame : null;
        }
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();

            Application.Run(new ServerManager());
        }

        //// **** file format ****
        //  number_of_players: <number>
        //  number_of_candidates: <number>
        //  players_order: <human/computer>
        //  number_of_rounds: <number>
        //  show_who_voted: <yes/no>
        //  agents: <number of agent files>
        //  <agent type> <path>
        //  ...
        //  <agent type> <path>
        //  candidates_names: <candidate1 name> <candidate2 name> ....
        //  points: <1st priority points> <2st priority points> ..
        //  priorities: 
        //  <path>
        ////

        public static GameDetails readConfigFile(string file)
        {
            try
            {
                XDocument xDoc = XDocument.Load(file);
                var res = xDoc.Element("Config");
                var cands = res.Elements("CandidateNames").Descendants();
                List<string> candnames = new List<string>();
                foreach (var q in cands)
                    candnames.Add(q.Value);
                int numberOfCandidates = candnames.Count;

                int numOfHumanPlayers = 0;
                var order = res.Elements("PlayersOrder").Descendants();
                List<string> playersOrder = new List<string>();
                foreach (var q in order)
                {
                    if (q.Value == "human")
                        numOfHumanPlayers++;
                    playersOrder.Add(q.Value);
                }
                int numOfPlayers = playersOrder.Count;
                int numOfRounds = Convert.ToInt32(res.Element("NumRounds").Value);

                var xtmp = res.Element("ShowWhoVoted").Value;
                int whoVoted = 0;
                if (xtmp == "full")
                    whoVoted = 2;
                else if (xtmp == "True")
                    whoVoted = 1;

                string startSecondRnd = "no";
                xtmp = res.Element("StartSecondRound").Value;
                if (xtmp != "no" && xtmp != "first" && xtmp != "random")
                    Console.WriteLine("Start From Second round parameter is invalid. using default (StartSecondRound: %s)", startSecondRnd);
                else
                 startSecondRnd = xtmp;

                List<int> points = new List<int>();
                var xpoints = res.Elements("Points").Descendants();
                foreach (var q in xpoints)
                   points.Add(Convert.ToInt32(q.Value));


                List<List<string>> priorities = new List<List<string>>();
                var xprefs = res.Element("Preferences");
                foreach (var q in xprefs.Elements("Pref"))
                {
                    int i = 0;
                    int repeatedPref = Convert.ToInt32(q.Attribute("num").Value);
                    while (i < repeatedPref)
                    {
                        List<string> candPriority = new List<string>();
                        foreach (string cand in q.Elements())
                            candPriority.Add(cand);
                        if (candPriority.Count != numberOfCandidates)
                        {
                            Console.WriteLine("[Error:] some priorities dont have enough candidates");
                            return null;
                        }
                        priorities.Add(candPriority);
                        i++;
                    }
                }
                if (priorities.Count != numOfPlayers)
                {
                    Console.WriteLine("[Error:] Not enough priorities!");
                    return null;
                }

                Boolean isRounds = false;
                List<Agent> agents = new List<Agent>();
                if (numOfPlayers > numOfHumanPlayers)
                        {
                            //TODO: Create Agents
                        }





               
                /*agents
                List<Agent> agents = new List<Agent>();
                Boolean isRounds = false;
                if (numOfHumanPlayers < numOfPlayers)
                {
                    nextLine = sr.ReadLine();
                    line = nextLine.Split(' ');
                    if (line[0] == "agents:")
                    {
                        int numOfAgents = Convert.ToInt32(line[1]);
                        Agent a;
                        for (int i = 0; i < numOfAgents; i++)
                        {
                            nextLine = sr.ReadLine();
                            a = readAgent(nextLine, numOfRounds, numberOfCandidates);
                            if (a != null)
                            {
                                agents.Add(a);
                                isRounds = a.isRounds();
                            }
                            else
                            {
                                Console.WriteLine("error while reading agent file");
                                return null;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("error while reading agent file");
                        return null;
                    }
                }
                else // no agents
                {
                    nextLine = sr.ReadLine(); // skip ont the "agents:" line
                }*/

                return new GameDetails(numOfHumanPlayers, numOfPlayers, numberOfCandidates, numOfRounds, candnames, playersOrder, points, priorities, agents, isRounds, whoVoted, startSecondRnd, Path.GetFileName(file));

            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read:");
                Debug.WriteLine(e.Message);
                return null;
            }
        }


        public static Agent readAgent(string agentLine, int roundsNum, int numOfCandidates)
        {
            try
            {
                string[] line = agentLine.Split(' ');
                if (line[0] == "random")
                    return new Agent("RANDOM");

                else if(line[0] == "first")
                    return new Agent("FIRST");
                else if(line[0] == "last")
                    return new Agent("LAST");
                else if (line[0] == "priority")
                {
                    StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + "\\" + line[1]);
                    string nextLine = sr.ReadLine();
                    line = nextLine.Split(' ');
                    return new Agent("PRIORITY", Convert.ToInt32(line[1]));
                }
                else if (line[0] == "turns")
                {
                    StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + "\\" + line[1]);
                    string nextLine;

                    List<string> votesPerTurn = new List<string>();
                    for (int i = 0; i < roundsNum; i++)
                    {
                        nextLine = sr.ReadLine();
                        if (nextLine != "")
                            votesPerTurn.Add(nextLine.Substring(2));
                    }
                    return new Agent("TURNS", votesPerTurn);
                }
                else if (line[0] == "rounds")
                {
                    StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + "\\" + line[1]);
                    string nextLine;
                    List<List<int>> rounds = new List<List<int>>(); // for each round a list with number of votes per candidate
                    for (int i = 0; i < roundsNum; i++)
                    {
                        nextLine = sr.ReadLine();
                        line = nextLine.Split(' ');
                        List<int> votesForCand = new List<int>();
                        string[] votesInRound = line[1].Split(',');
                        if (votesInRound.Length == numOfCandidates)
                        {
                            for (int j = 0; j < votesInRound.Length; j++)
                                votesForCand.Add(Convert.ToInt32(votesInRound[j]));
                            rounds.Add(votesForCand);
                        }
                        else
                        {
                            Console.WriteLine("error while reading the agents file - wrong number of candidates in round");
                            return null;
                        }

                    }
                    return new Agent("ROUNDS", rounds);
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read - agentsFile");
                Debug.WriteLine(e.Message);
                return null;
            }

        }
    }

}
