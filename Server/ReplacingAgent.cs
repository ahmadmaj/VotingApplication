namespace Server
{
    class ReplacingAgent : Agent
    {
        private int playerID;
        public ReplacingAgent(string type, int playerId):base(type){
            this.playerID = playerId;
        }

        public int getPlayerID()
        {
            return this.playerID;
        }
            
    }
}
