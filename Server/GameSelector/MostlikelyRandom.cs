using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.GameSelector
{
    internal class MostlikelyRandom : LotteryGame
    {
        public override void DecideOnGame(List<UserVoter> batchofplayers)
        {
            List<int> tmpWeight =
                Program.gameDetailsList.Select(
                    possibGameDetails =>
                        batchofplayers.Sum(player => Convert.ToInt32(!player.HasPlayed(possibGameDetails.configFile))))
                    .ToList();
            int totalWeight = tmpWeight.Sum();
            Random _rnd = new Random();
            int randomNumber = _rnd.Next(1, totalWeight + 1);
            int x = 0;
            GameDetails selecteDetails = Program.gameDetails;
            foreach (int i in tmpWeight)
            {
                if (randomNumber <= i)
                {
                    selecteDetails = Program.gameDetailsList[x];
                    break;
                }
                randomNumber -= i;
                x++;
            }
            AssignToGame(selecteDetails, batchofplayers);
        }
    }
}
