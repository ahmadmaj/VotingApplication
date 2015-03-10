using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Server.GameSelector;

namespace Server.Connection
{
    static class WaitingRoom
    {
        private static LotteryGame chooseGame = new GenericDistribution(Program.LotteryFeatureDictionary);  //new MostlikelyRandom();
        public static List<UserVoter> waitingPlayers = new List<UserVoter>(); //for each playID the User class he is
        private static List<List<UserVoter>> sortedPlayers = new List<List<UserVoter>>();

        public static List<TimeSpan> usersWaitTimes = new List<TimeSpan>();
        public static TimeSpan AvgTimeSpan = new TimeSpan(0,0,0,0);


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Boolean checkToStartGame()
        {
            //if there are no games AND all current players are in waitinglist AND number of players in waitingroom is sufficent for new game
            //OR number of waiting players equal twice as players needed to start game
            if (Program.SinglePMode || ((waitingPlayers.Count >= Program.gameDetails.numOfHumanPlayers && !Program.PlayingGames.Any() &&
                 waitingPlayers.Count == Program.ConnIDtoUser.Count) ||
                waitingPlayers.Count == (Program.gameDetails.numOfHumanPlayers * 2)))
            {
                AssignPlayersToGames();
                return true;
            }
            return false;
        }

        public static Boolean joinWaitingRnStart(UserVoter Player)
        {
            if (!waitingPlayers.Contains(Player))
                waitingPlayers.Add(Player);
            return (checkToStartGame() && Program.PlayingGames.Any());
        }



        public static void RemoveFromWaitingR(string connID)
        {
            UserVoter tmpVoter;
            if (!Program.ConnIDtoUser.TryGetValue(connID, out tmpVoter)) return;
            if (waitingPlayers.Count > 0 && waitingPlayers.Contains(tmpVoter))
                waitingPlayers.Remove(tmpVoter);
        }


        private static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void AssignPlayersToGames()
        {
            //Create number of games needed
            for (int x=0; x < waitingPlayers.Count/Program.gameDetails.numOfHumanPlayers; x++)
                sortedPlayers.Add(new List<UserVoter>());

            if (sortedPlayers.Any())
            {
                List<UserVoter> garanteedToPlay = waitingPlayers.GetRange(0,
                    sortedPlayers.Count*Program.gameDetails.numOfHumanPlayers);

                splitPlayersToGames(garanteedToPlay);

                foreach (List<UserVoter> batchofplayers in sortedPlayers)
                {

                    chooseGame.DecideOnGame(batchofplayers);
                    batchofplayers.Clear();
                }
                sortedPlayers.Clear();
            }
        }


        private static void splitPlayersToGames(List<UserVoter> playersList)
        {

            playersList.Shuffle();
            int numPlayersinGame = Program.gameDetails.numOfHumanPlayers;
            //while (playersList.Count > 0)
            //foreach (Game currGame in Program.PlayingGames)
            foreach (List<UserVoter> BatchofPlayers in sortedPlayers)
                if (Program.gameDetails.numOfHumanPlayers > BatchofPlayers.Count)
                    for (int x = 0; (playersList.Count > 0) && (x == 0 || x < numPlayersinGame); x++)
                    {
                        UserVoter newP = playersList.First();
                        BatchofPlayers.Add(newP);
                        playersList.Remove(newP);
                        RemoveFromWaitingR(newP.connectionID);
                    }
        }
    }
}
