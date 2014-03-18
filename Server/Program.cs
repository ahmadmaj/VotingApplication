using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;


namespace Server
{
    public class Program
    {
        public static int init = 0;
        public static GameDetails gameDetails;
        public static List<string> awaitingPlayersID = new List<string>();
        public static List<Game> games = new List<Game>();
        public static WriteData file;

        public static int count = 0;
        public static string first = "";
        static void Main(string[] args)
        {
            string url = "http://localhost:8080";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                //Console.WriteLine("Please enter a configuration file");
                //while (true)
                //{
                //    String file = Console.ReadLine();
                //    if (file != "")
                //    {
                //        gameDetails = readFromFile(file);
                //        if (gameDetails == null)
                //        {
                //            Console.WriteLine("Error! Please enter a configuration file again");
                //        }
                //        else
                //        {
                //            Console.WriteLine("Finished reading the file");
                //            break;
                //        }
                //    }
                //}
                gameDetails = readConfigFile("C://Users//lena//Documents//Visual Studio 2013//Projects//VotingApplication//Server//configFile2_check.txt");
                init = 1;

                file = new WriteData();

                Console.ReadLine();
            }
        }

        //// **** file format ****
        //  participants: <participants in experiment>
        //  humanPlayers: <human palyers in a game>
        //  totalPlayers: <total palyers in a game>
        //  candidates: <candidates in a game>
        //  votesPlayer: <votes per player>
        //  turns: <turns>
        //  playersType: <player type> <player type> ..
        //  agents: <number of agent files>
        //  <path>
        //  ...
        //  <path>
        //  candidatesNames: <candidate1 name> <candidate2 name> ....
        //  points: <1st priority points> <2st priority points> ..
        //  priorities: 
        //  <path>
        ////

        public static GameDetails readConfigFile(string file)
        {
            try
            {
                StreamReader sr = new StreamReader(file);
                //int participants = -1;
                int numofhumanplayers = -1;
                int numoftotalplayers = -1;
                int candidates = -1;
                int votes = -1;
                int turns = -1;
                Boolean rounds = false;
                

                //string nextLine = sr.ReadLine();
                //string[] line = nextLine.Split(' ');
                //if (line[0] == "participants:")
                //    participants = Convert.ToInt32(line[1]);
                //else
                //{
                //    Console.WriteLine("error while reading the file - participants");
                //    return null;
                //}

                string nextLine = sr.ReadLine();
                string[] line = nextLine.Split(' ');
                if (line[0] == "humanPlayers:")
                    numofhumanplayers = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - humanPlayers");
                    return null;
                }

                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "totalPlayers:")
                    numoftotalplayers = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - totalPlayers");
                    return null;
                }

                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "candidates:")
                    candidates = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - candidates");
                    return null;
                }

                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "votesPlayer:")
                    votes = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - votesPlayer");
                    return null;
                }

                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "turns:")
                    turns = Convert.ToInt32(line[1]);
                else 
                {
                    Console.WriteLine("error while reading the file - turns");
                    return null;
                }

                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                List<string> players = new List<string>();
                if (line[0] == "playersType:")
                {
                    for (int i = 1; i < numoftotalplayers + 1; i++)
                    {
                        players.Add(line[i]);
                    }
                }
                else
                {
                    Console.WriteLine("error while reading the file - playersType");
                    return null;
                }
                List<Agent> agents = new List<Agent>();
                if (numofhumanplayers < numoftotalplayers)
                {
                    int numPaths = numoftotalplayers - numofhumanplayers;
                    nextLine = sr.ReadLine();
                    line = nextLine.Split(' ');
                    if (line[0] == "agents:")
                    {
                        int numOfAgents = Convert.ToInt32(line[1]);
                        Agent a;
                        for (int i = 0; i < numOfAgents; i++)
                        {
                            nextLine = sr.ReadLine(); //agent file path
                            a = readAgentFile(nextLine, votes, candidates);
                            if (a != null)
                            {
                                agents.Add(a);
                                rounds = a.isRounds();
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
                }
                
                //nextLine = sr.ReadLine(); // skip ont the "agents:" line


                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                List<string> candnames = new List<string>();
                if (line[0] == "candidatesNames:")
                {
                    string names = nextLine.Substring(16);
                    for (int i = 0; i < candidates; i++)
                    {
                        int startIndex = names.IndexOf('"');
                        names = names.Substring(startIndex + 1);
                        int nameLength = names.IndexOf('"') - startIndex;
                        candnames.Add((names.Substring(0, nameLength+1)));
                        names = names.Substring(nameLength + 2);

                    }
                }
                else
                {
                    Console.WriteLine("error while reading the file - candidatesnames");
                    return null;
                }

                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                 List<int> points = new List<int>();

                if (line[0] == "points:")
                {
                    for (int i = 1; i < candidates + 1; i++)
                    {
                        points.Add(Convert.ToInt32(line[i]));
                    }
                }
                else
                {
                    Console.WriteLine("error while reading the file - points");
                    return null;
                }
                
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                List<List<string>> priorities = new List<List<string>>();

                if (line[0] == "priorities:")
                {
                    string path = sr.ReadLine();
                    priorities = readPriorityFile(path, numoftotalplayers, candidates);
                    if (priorities == null)
                    {
                        Console.WriteLine("error while reading the file - priorities");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("error while reading the file - priotities");
                    return null;
                }



                return new GameDetails(0, numofhumanplayers, numoftotalplayers, candidates, votes, turns, candnames, players, points, priorities, agents, rounds);

            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read:");
                Debug.WriteLine(e.Message);
                return null;
            }

        }


        public static Agent readAgentFile(string file, int votes, int numOfCandidates)
        {
            try
            {
                StreamReader sr = new StreamReader(file);
                string nextLine = sr.ReadLine();
                string[] line = nextLine.Split(' ');
                if (line[0] == "rounds")
                {
                    List<List<int>> rounds = new List<List<int>>();
                    for (int i = 0; i < votes; i++) // the number rounds = number of vote per player
                    {
                        nextLine = sr.ReadLine();
                        line = nextLine.Split(' ');
                        List<int> votesForCand = new List<int>();
                        string[] numOfVotes = line[1].Split(',');
                        if (numOfVotes.Length == numOfCandidates)
                        {
                            for (int j = 0; j < numOfVotes.Length; j++)
                                votesForCand.Add(Convert.ToInt32(numOfVotes[j]));
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
                else if (line[0] == "all")
                {
                    if (line[1] == "random")
                        return new Agent("RANDOM");
                    else if (line[1] == "first")
                        return new Agent("FIRST");
                    else if (line[1] == "last")
                        return new Agent("LAST");
                    else if (line[1] == "priority")
                        return new Agent("PRIORITY", Convert.ToInt32(line[2]));
                    else
                        return null;
                }
                else if (line[0] == "turns")
                {
                    List<string> votesPerTurn = new List<string>();
                    for (int i = 0; i < votes; i++)
                    {
                        nextLine = sr.ReadLine();
                        if(nextLine != "")
                            votesPerTurn.Add(nextLine.Substring(2));
                    }
                    return new Agent("TURNS", votesPerTurn);
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read - priorities");
                Debug.WriteLine(e.Message);
                return null;
            }

        }

        public static List<List<string>> readPriorityFile(string file, int players, int candidates)
        {
            List<List<string>> priority = new List<List<string>>();
            int numLines = players;
            int numCandidates = candidates;

            try
            {
                StreamReader sr = new StreamReader(file);
                string nextLine;
                for (int i = 0; i < numLines; i++) //go over the priority lines
                {
                    nextLine = sr.ReadLine();
                    string[] line = nextLine.Split(',');
                    List<string> candPriority = new List<string>();
                    for (int j = 0; j < numCandidates; j++) //go over the priorities for a player
                    {
                        candPriority.Add(line[j].Replace("\"",""));
                    }
                    priority.Add(candPriority);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("the file could not be read - priorities");
                Debug.WriteLine(e.Message);
                return null;
            }

            return priority;
        }


        //per player: cand.1 cand.2 .. (seperated with space)
        public static string createPointsString(int game, string playerID)
        {
            string ans = "";
            List<int> points = Program.gameDetails.getPoints();
            for (int i = 0; i < Program.gameDetails.getNumOfCandidates(); i++)
            {
                ans = ans + "#" + points[i];
            }
            return ans;
        }


        public static string createCandIndexString(int game, string playerID)
        {
            string ans = "";
            int player = Program.games[game].findPlayer(playerID);
            List<string> candNames = gameDetails.getCandidatesNames();
            List<string> priority = gameDetails.getPriorities().ElementAt(player);

            for (int i = 0; i < priority.Count; i++)
            {
                ans = ans + "#" + (candNames.IndexOf(priority[i]) + 1);
            }
            return ans;
        }


        //per player: cand.1#cand.2# ... (seperated by #)
        public static string createPrioritiesString(int game, string playerID)
        {
            string ans = "";
            for (int i = 0; i < Program.gameDetails.getNumOfCandidates(); i++)
            {
                if (i == 0)
                {
                    ans = ans + "1st priority";

                }
                else if (i == 1)
                {
                    ans = ans + "#" + "2nd priority";

                }
                else if (i == 2)
                {
                    ans = ans + "#" + "3rd priority";

                }
                else
                {
                    ans = ans + "#" + (i + 1) + "th priority";

                }
            }

            return ans;
        }

        //c1 c2 c3 ... (seperated by #)
        public static string createCandNamesString(int game, string playerID)
        {
            string ans = "";
            //List<string> names = gameDetails.getCandidatesNames();
            //for (int i = 0; i < names.Count; i++)
            //    ans = ans + " " + names[i];

            int player = Program.games[game].findPlayer(playerID);
            //List<string> candNames = gameDetails.getCandidatesNames();
            List<string> priority = gameDetails.getPriorities().ElementAt(player);

            for (int i = 0; i < priority.Count; i++)
            {
                ans = ans + "#" + priority[i];
            }

            return ans;
        }

        // p1 p2#p1 p3#... (the candidates are seperated by # and the player who voted by space)
        //public static string createVotedByString(int game)
        //{
        //    string ans = "";
        //    List<List<int>> voted = games[game].getVotedBy();
        //    for (int i = 0; i < voted.Count; i++)
        //    {
        //        for (int j = 0; j < voted[i].Count; j++)
        //            if (j == voted[i].Count - 1)
        //                ans = ans + " player" + (voted[i][j] + 1);
        //            else
        //                ans = ans + " player" + (voted[i][j] + 1) + ",";
        //        ans = ans + "#";
        //    }
        //    return ans;
        //}

        // seperetad by #
        public static string createNumOfVotesString(int game, string playerID)
        {
            string ans = "";
            int player = Program.games[game].findPlayer(playerID);
            List<string> priority = gameDetails.getPriorities().ElementAt(player);
            List<string> candNames = gameDetails.getCandidatesNames();
            List<int> numOfVotes = games[game].getNumOfVotes();
            for (int i = 0; i < numOfVotes.Count; i++)
                ans = ans + "#" + numOfVotes[candNames.IndexOf(priority[i])];
            return ans;
        }

        public static string createNumOfVotesString(int game, int player)
        {
            string ans = "";
            List<string> priority = gameDetails.getPriorities().ElementAt(player);
            List<string> candNames = gameDetails.getCandidatesNames();
            List<int> numOfVotes = games[game].getNumOfVotes();
            for (int i = 0; i < numOfVotes.Count; i++)
                ans = ans + "#" + numOfVotes[candNames.IndexOf(priority[i])];
            return ans;
        }

        //seperated by #
        public static string createGameOverString(List<int> points)
        {
            string ans = "";
            for (int i = 0; i < points.Count; i++)
                ans = ans + "#" + points[i];
            return ans;
        }



    }
}
