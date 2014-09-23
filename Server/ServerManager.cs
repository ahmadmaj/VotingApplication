using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ConsoleRedirection;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;

namespace Server
{
    public partial class ServerManager : Form
    {
        delegate void SetTextCallback(string text);
        private TextWriter _writer = null;
        private OpenFileDialog OpenFileDialog1;

        public ServerManager()
        {
            InitializeComponent();
        }

        private void ServerManager_Load(object sender, EventArgs e)
        {
            this.OpenFileDialog1 = new OpenFileDialog();
            InitializeOpenFileDialog();
            // Instantiate the writer
            _writer = new TextBoxStreamWriter(this);
            // Redirect the out Console stream
            Console.SetOut(_writer);
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.RowHeadersWidthSizeMode =
                DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridView1.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void InitializeOpenFileDialog()
        {
            // Set the file dialog to filter for graphics files. 
            this.OpenFileDialog1.Filter =
                "Config Files (*.txt)|*.txt|" +
                "All files (*.*)|*.*";

            // Allow the user to select multiple images. 
            this.OpenFileDialog1.Multiselect = true;
            this.OpenFileDialog1.Title = "My Configuration Browser";
            this.OpenFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult dr = this.OpenFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                // Read the files 
                foreach (String confile in OpenFileDialog1.FileNames)
                    Program.gameDetailsList.Add(Program.readConfigFile(confile));
                Program.gameDetails = Program.gameDetailsList.First();

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
            Program.logFolder = DateTime.Now.ToString("ddMMyy_hhmm");


        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((Program.gameDetails != null) && (this.tabControl1.SelectedTab == this.tabPage2))
            {
                tabControl1.SelectedTab = tabPage2;
                textBox1.Text = Program.gameDetails.numOfTotalPlayers.ToString();
                textBox2.Text = Program.gameDetails.numOfCandidates.ToString();
                textBox3.Text = Program.gameDetails.numOfRounds.ToString();
            }
            else if ((Program.gameDetails == null) && (tabControl1.SelectedTab == tabPage2))
            {
                MessageBox.Show("Unable to load tab. no configurations loaded");
                tabControl1.SelectedTab = tabPage1;
            }
            if (tabControl1.SelectedTab == tabPage3)
            {
                refresh(sender, e);
            }
            if (tabControl1.SelectedTab == tabPage4)
                refreshGames(sender, e);
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
                                        let Gameid = d.gameID
                                        let PlayersInside = d.playersID.Count
                                        select new
                                        {
                                            Gameid,
                                            PlayersInside
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

        private void button3_Click(object sender, EventArgs e)
        {
            WaitingRoom.AssignPlayersToGames();
            var context = GlobalHost.ConnectionManager.GetHubContext<ServerHub>();
            foreach (string connID in Program.PlayingGames.SelectMany(playingGame => playingGame.playersID))
                context.Clients.Client(connID).StartGameMsg("start");
        }
    }
}
