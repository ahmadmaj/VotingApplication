﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;


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
                StreamReader sr = new StreamReader(file);
                
                //number_of_players
                int numOfPlayers = -1;
                string nextLine = sr.ReadLine();
                string[] line = nextLine.Split(' ');
                if (line[0] == "number_of_players:")
                    numOfPlayers = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - number_of_players");
                    return null;
                }

                //number_of_candidates
                int numberOfCandidates = -1;
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "number_of_candidates:")
                    numberOfCandidates = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - number_of_candidates");
                    return null;
                }

                //players_order
                List<string> playersOrder = new List<string>();
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "players_order:")
                {
                    for (int i = 1; i < numOfPlayers + 1; i++)
                    {
                        playersOrder.Add(line[i]);
                    }
                }
                else
                {
                    Console.WriteLine("error while reading the file - players_order");
                    return null;
                }

                //number_of_rounds
                int numOfRounds = -1;
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "number_of_rounds:")
                    numOfRounds = Convert.ToInt32(line[1]);
                else
                {
                    Console.WriteLine("error while reading the file - number_of_rounds");
                    return null;
                }

                int numOfHumanPlayers = 0;
                for (int i = 0; i < numOfPlayers; i++)
                {
                    if (playersOrder[i] == "human")
                        numOfHumanPlayers++;
                }

                //show_who_voted
                int whoVoted = 0;
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "show_who_voted:")
                {
                    if (line[1] == "full")
                        whoVoted = 2;
                    else if (line[1] == "yes")
                        whoVoted = 1;
                }
                else
                {
                    Console.WriteLine("error while reading the file - show_who_voted: [full\\yes\\no]");
                    return null;
                }

                //start_from_second_round
                string startSecondRnd = "";
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                if (line[0] == "start_from_second_round:")
                {
                    if (line[1] == "no" || line[1] == "first" || line[1] == "random")
                        startSecondRnd = line[1];
                    else
                    {
                        Console.WriteLine("error while reading the file - start_from_second_round, wrond argument");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("error while reading the file - start_from_second_round");
                    return null;
                }

                //agents
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
                }

                //candidates_names
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                List<string> candnames = new List<string>();
                if (line[0] == "candidates_names:")
                {
                    string names = nextLine.Substring(17);
                    for (int i = 0; i < numberOfCandidates; i++)
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
                    Console.WriteLine("error while reading the file - candidates_names");
                    return null;
                }

                //points
                nextLine = sr.ReadLine();
                line = nextLine.Split(' ');
                 List<int> points = new List<int>();

                if (line[0] == "points:")
                {
                    for (int i = 1; i < numberOfCandidates + 1; i++)
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
                    string path = Path.GetDirectoryName(file) + '/' + sr.ReadLine();
                    priorities = readPriorityFile(path, numOfPlayers, numberOfCandidates);
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
                Debug.WriteLine("the file could not be read - prioritiesFile");
                Debug.WriteLine(e.Message);
                return null;
            }

            return priority;
        }
    }

}
