using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Server.GUI;


namespace Server
{
    public static class Program
    {
        public static List<GameDetails> gameDetailsList = new List<GameDetails>();
        public static Dictionary<string, Dictionary<string, double>> LotteryFeatureDictionary = new Dictionary<string, Dictionary<string, double>>();
        public static GameDetails gameDetails;
        public static Dictionary<string, UserVoter> ConnIDtoUser = new Dictionary<string, UserVoter>(); //for each playID the User class he is
        public static List<Game> PlayingGames = new List<Game>();
        public static String logFolder = "";
        public static Boolean mTurkMode = false;
        public static Boolean SinglePMode = false;
        public static bool LogAgents = true;

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

        public static void readDistribConfigFile(string fileName)
        {
            int x = 0;
            LotteryFeatureDictionary.Clear();
            try
            {
                XDocument xDoc = XDocument.Load(fileName);
                var res = xDoc.Element("Lottery");
                var features = res.Elements("Feature");
                foreach (XElement dist in features)
                {

                    Dictionary<string, double> propvalues =
                        dist.Elements()
                            .Where(prob => prob.HasAttributes)
                            .ToDictionary(prob => prob.Value, prob => Convert.ToDouble(prob.Attribute("num").Value));
                    if (dist.HasAttributes)
                        LotteryFeatureDictionary.Add(dist.Attribute("name").Value, propvalues);
                    else
                        LotteryFeatureDictionary.Add("Feature" + x++, propvalues);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read:");
                Debug.WriteLine(e.Message);
            }
        }
    }

}
