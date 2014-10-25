using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                List<string> candnames = cands.Select(q => q.Value).ToList();
                int numberOfCandidates = candnames.Count;

                int numOfHumanPlayers = 0;
                var order = res.Elements("PlayersOrder").Descendants();
                List<string> playersOrder = new List<string>();
                foreach (var q in order)
                {
                    int rep = 1;
                    if (q.HasAttributes)
                        rep = Convert.ToInt32(q.Attribute("num").Value);
                    while (rep > 0)
                    {
                        if (q.Value == "human")
                            numOfHumanPlayers++;
                        playersOrder.Add(q.Value);
                        rep--;
                    }

                }
                int numOfPlayers = playersOrder.Count;
                int numOfRounds = Convert.ToInt32(res.Element("NumRounds").Value);

                var xtmp = res.Element("ShowWhoVoted").Value;
                int whoVoted;
                switch (xtmp)
                {
                    case "full":
                        whoVoted = 2;
                        break;
                    case "True":
                        whoVoted = 1;
                        break;
                    default:
                        whoVoted = 0;
                        break;
                }

                string startSecondRnd = "no";
                xtmp = res.Element("StartSecondRound").Value;
                if (xtmp != "no" && xtmp != "first" && xtmp != "random")
                    Console.WriteLine(
                        "Start From Second round parameter is invalid. using default (StartSecondRound: %s)",
                        startSecondRnd);
                else
                    startSecondRnd = xtmp;

                var xpoints = res.Elements("Points").Descendants();
                List<int> points = xpoints.Select(q => Convert.ToInt32(q.Value)).ToList();

                List<List<string>> priorities = new List<List<string>>();
                var xprefs = res.Element("Preferences");
                if (xprefs != null)
                    foreach (var q in xprefs.Elements("Pref"))
                    {
                        int rep = 1;
                        if (q.HasAttributes)
                            rep = Convert.ToInt32(q.Attribute("num").Value);
                        while (rep > 0)
                        {
                            List<string> candPriority = new List<string>();
                            foreach (var elm in q.Elements())
                                candPriority.Add(elm.Value);
                            if (candPriority.Count != numberOfCandidates)
                            {
                                Console.WriteLine("[Error:] some preferences dont have enough candidates");
                                return null;
                            }
                            priorities.Add(candPriority);
                            rep--;
                        }
                    }
                if (priorities.Count != numOfPlayers)
                {
                    Console.WriteLine("[Error:] preferences count mismatch!");
                    return null;
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

                return new GameDetails(numOfHumanPlayers, numOfPlayers, numberOfCandidates, numOfRounds, candnames,
                    playersOrder, points, priorities, whoVoted, startSecondRnd, Path.GetFileName(file));
            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read:");
                Debug.WriteLine(e.Message);
                return null;
            }
        }
    }

}
