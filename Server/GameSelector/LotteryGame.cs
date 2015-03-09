using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Connection;

namespace Server.GameSelector
{
    public abstract class LotteryGame
    {
        public abstract void DecideOnGame(List<UserVoter> batchofplayers);

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void AssignToGame(GameDetails selecteDetails, List<UserVoter> batchofplayers)
        {
            if (batchofplayers == null) throw new ArgumentNullException("batchofplayers");
            Game newGame = new Game(selecteDetails);
            if (batchofplayers.Count == newGame.numOfHumanPlayers)
            {
                foreach (UserVoter player in batchofplayers)
                    newGame.addPlayerID(player);
                Program.PlayingGames.Add(newGame);
            }
            else
                foreach (UserVoter voter in batchofplayers)
                    WaitingRoom.joinWaitingRnStart(voter);
        }
    }
}