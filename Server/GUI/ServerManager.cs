using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Server.Connection;
using Server.GameSelector;

namespace Server.GUI
{
    public partial class ServerManager : Form
    {
        private delegate void SetTextCallback(string text);

        private TextWriter _writer = null;
        private OpenFileDialog _openFileDialog1, _openFileDialog2;

        public ServerManager()
        {
            InitializeComponent();
        }

        private void ServerManager_Load(object sender, EventArgs e)
        {
            _openFileDialog1 = new OpenFileDialog();
            _openFileDialog2 = new OpenFileDialog();
            InitializeOpenFileDialog();
            InitializeOpenDistFileDialog();
            // Instantiate the writer
            _writer = new TextBoxStreamWriter(this);
            // Redirect the out Console stream
            Console.SetOut(_writer);
        }

        private void InitializeOpenDistFileDialog()
        {
            // Set the file dialog to filter for graphics files. 
            _openFileDialog2.Filter =
                "Config Files (*.xml)|*.xml|" +
                "All files (*.*)|*.*";

            // Allow the user to select multiple images. 
            _openFileDialog2.Multiselect = false;
            _openFileDialog2.RestoreDirectory = true;
            _openFileDialog2.Title = "Select configuration files";
            _openFileDialog2.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void InitializeOpenFileDialog()
        {
            // Set the file dialog to filter for graphics files. 
            _openFileDialog1.Filter =
                "Config Files (*.xml)|*.xml|" +
                "All files (*.*)|*.*";

            // Allow the user to select multiple images. 
            _openFileDialog1.Multiselect = true;
            _openFileDialog1.RestoreDirectory = true;
            _openFileDialog1.Title = "Select configuration files";
            _openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult dr = _openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                // Read the files 
                foreach (String confile in _openFileDialog1.FileNames)
                    Program.gameDetailsList.Add(Program.readConfigFile(confile));

                //loaded Config enable components
                StartServ.Enabled = true;
                tabPage2.Enabled = true;
            }
        }

        private void StartServ_Click(object sender, EventArgs e)
        {
            string url = "http://localhost:8010";
            WebApp.Start<Startup>("http://+:8010");
            Console.WriteLine("Server running on {0}", url);
            Program.logFolder = DateTime.Now.ToString("ddMMyy_HHmm");


        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage2)
            {
                if (Program.gameDetailsList.Count > 0)
                    ConfigTab();
                else
                {
                    MessageBox.Show("Unable to load tab. no configurations loaded");
                    tabControl1.SelectedTab = tabPage1;
                }
            }
            if (tabControl1.SelectedTab == tabPage3)
            {
                refresh(sender, e);
            }
            if (tabControl1.SelectedTab == tabPage4)
                refreshGames(sender, e);
        }

        private void ConfigTab()
        {

            tabControl1.SelectedTab = tabPage2;
            comboBox1.DataSource = Program.gameDetailsList;
            comboBox1.DisplayMember = "configFile";
            comboBox1.ValueMember = "configFile";

        }

        private void refresh(object sender, EventArgs e)
        {
            dataGridView1.DataSource = (from d in Program.ConnIDtoUser
                orderby d.Value
                let currGame = d.Value.CurrGame != null ? d.Value.CurrGame.gameID : -1
                let id = d.Value.mTurkID != "" ? d.Value.mTurkID : d.Key
                select new
                {
                    id,
                    d.Value.userID,
                    d.Value.TotalScore,
                    currGame
                }).ToList();
            dataGridView1.Update();
        }

        private void refreshGames(object sender, EventArgs e)
        {
            dataGridView2.DataSource = (from d in Program.PlayingGames
                orderby d.gameID
                let GameId = d.gameID
                let PlayersInside = d.playersID.Count
                let Conf = d.configFile
                let Rounds = d.rounds
                let Timeleft = d.timeStarted.AddSeconds(30*(d.rounds*d.numOfHumanPlayers)) - DateTime.Now
                select new
                {
                    GameId,
                    Conf,
                    Rounds,
                    PlayersInside,
                    Timeleft
                }).ToList();
            dataGridView2.Update();
        }


        public void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (txtConsole.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] {text});
            }
            else
                txtConsole.AppendText(text);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Program.mTurkMode = !Program.mTurkMode;
        }

        private void SinglePMode_CheckedChanged(object sender, EventArgs e)
        {
            Program.SinglePMode = !Program.SinglePMode;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WaitingRoom.AssignPlayersToGames();
            var context = GlobalHost.ConnectionManager.GetHubContext<ServerHub>();
            foreach (string connID in Program.PlayingGames.SelectMany(playingGame => playingGame.playersID))
                context.Clients.Client(connID).StartGameMsg("start");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedCells.Count == 0) return;
            string gameid = dataGridView2.SelectedCells[0].Value.ToString();
            Game game = Program.PlayingGames.Find(gamei => gamei.gameID.ToString() == gameid);
            if (game != null)
            {
                game.endGame();
                var context = GlobalHost.ConnectionManager.GetHubContext<ServerHub>();

                List<int> playersPoints = game.currentPoints;
                string points = playersPoints.Aggregate("", (current, point) => current + ("#" + point));
                foreach (string playerid in game.playersID)
                {
                    UserVoter playUser;
                    if (Program.ConnIDtoUser.TryGetValue(playerid, out playUser))
                    {
                        int playeridx = playUser.inGameIndex;
                        context.Clients.Client(playerid)
                            .GameOver(game.numOfCandidates, game.createNumOfVotesString(playUser),
                                game.votesPerPlayer[playeridx], game.getTurnsLeft(), points, game.getWinner(),
                                game.getCurrentWinner(playeridx), game.createWhoVotedString(playeridx),
                                ("p" + (playeridx + 1)));
                        playUser.resetGame();
                    }
                }
                Program.PlayingGames.Remove(game);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Program.LogAgents = !Program.LogAgents;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                GameDetails selectedConf = (GameDetails) comboBox1.SelectedItem;
                textBox1.Text = selectedConf.numOfTotalPlayers.ToString();
                textBox2.Text = selectedConf.numOfCandidates.ToString();
                textBox3.Text = selectedConf.numOfRounds.ToString();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult dr = _openFileDialog2.ShowDialog();
            if (dr == DialogResult.OK)
            {
                // Read the files 
                Program.readDistribConfigFile(_openFileDialog2.FileName);

                //loaded Config enable components
                UpdateTree();
            }
        }

        private void UpdateTree()
        {
            treeView1.BeginUpdate();

            // Clear the TreeView each time the method is called.
            treeView1.Nodes.Clear();
            foreach (var pairValue in Program.LotteryFeatureDictionary)
            {
                TreeNode mainNode = new TreeNode();
                mainNode.Name = pairValue.Key;
                mainNode.Text = pairValue.Key;
                treeView1.Nodes.Add(mainNode);
                foreach (KeyValuePair<string, double> keyValuePair in pairValue.Value)
                {
                    TreeNode newNode = new TreeNode();
                    newNode.Name = keyValuePair.Key;
                    newNode.Text = keyValuePair.Key + " [p: " + keyValuePair.Value + "]";
                    newNode.Tag = keyValuePair;
                    mainNode.Nodes.Add(newNode);
                }
            }
             treeView1.EndUpdate();
        }

        private
            void button6_Click(object sender, EventArgs e)
        {
            GenericDistribution chooseGame;
            chooseGame = new GenericDistribution(Program.LotteryFeatureDictionary);
            chooseGame.TestDecideOnGame();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                textBox4.Text = "";
                textBox5.Text = "";

                KeyValuePair<string, double> obj = (KeyValuePair<string, double>) treeView1.SelectedNode.Tag;
                textBox4.Text = obj.Key;
                textBox5.Text = obj.Value.ToString();
            }
            catch { }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                KeyValuePair<string, double> obj = (KeyValuePair<string, double>)treeView1.SelectedNode.Tag;

                Dictionary<string, double> parent = Program.LotteryFeatureDictionary[treeView1.SelectedNode.Parent.Name];
                parent.Remove(obj.Key);
                parent.Add(textBox4.Text, Convert.ToDouble(textBox5.Text));
                UpdateTree();
            }
        }
    }
}
