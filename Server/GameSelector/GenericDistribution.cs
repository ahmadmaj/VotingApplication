using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Server.GameSelector
{
    public class GenericDistribution : LotteryGame
    {
        private Dictionary<string,ProportionValue<string>[]>  Featureslist = new Dictionary<string, ProportionValue<string>[]>();


        public GenericDistribution(Dictionary<string, Dictionary<string, double>> LotteryFeatureDictionary)
        {
            foreach (var feature in LotteryFeatureDictionary)
            {
                var list = new ProportionValue<string>[feature.Value.Count];
                int x = 0;
                foreach (KeyValuePair<string, double> featureprop in feature.Value)
                    list[x++]=ProportionValue.Create(featureprop.Value, featureprop.Key);
                Featureslist.Add(feature.Key,list);
            }
        }


        public override void DecideOnGame(List<UserVoter> batchofplayers)
        {
            List<GameDetails> ListOfGames = Program.gameDetailsList;
            foreach (ProportionValue<string>[] pValues in Featureslist.Values)
            {
                string selected = pValues.ChooseByRandom();
                ListOfGames = ListOfGames.Where(gameDetailse => gameDetailse.configFile.Contains(selected)).ToList();
            }
            if (ListOfGames.Count != 1)
              throw new InvalidOperationException(
                "Not enough selective features");

            AssignToGame(ListOfGames[0], batchofplayers);
        }

        public GameDetails TestDecideOnGame()
        {
            List<GameDetails> ListOfGames = Program.gameDetailsList;
            string selectedFeatures = "";
            foreach (ProportionValue<string>[] pValues in Featureslist.Values)
            {
                string selected = pValues.ChooseByRandom();
                selectedFeatures += " " + selected;
                ListOfGames = ListOfGames.Where(gameDetailse => gameDetailse.configFile.Contains(selected)).ToList();
            }
            if (ListOfGames.Count != 1)
                throw new InvalidOperationException(
                  "Not enough selective features");

            Console.WriteLine("Features: " + selectedFeatures + "\nSelected Game is: " + ListOfGames[0].configFile);
            return ListOfGames[0];
        }
    }
}