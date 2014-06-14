using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConsoleRedirection;
using Microsoft.Owin.Hosting;

namespace Server
{
    public partial class ServerManager : Form
    {
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
            _writer = new TextBoxStreamWriter(txtConsole);
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
                    Program.gameDetailsList.AddFirst(Program.readConfigFile(confile));
                Program.gameDetails = Program.gameDetailsList.First;

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
                textBox1.Text = Program.gameDetails.Value.numOfTotalPlayers.ToString();
                textBox2.Text = Program.gameDetails.Value.numOfCandidates.ToString();
                textBox3.Text = Program.gameDetails.Value.numOfRounds.ToString();
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
        }

        private void refresh(object sender, EventArgs e)
        {
            dataGridView1.DataSource = (from d in Program.ConnIDtoUser
                                        orderby d.Value
                let currGame = d.Value.CurrGame != null ? d.Value.CurrGame.gameID : -1
                select new
                                        {
                                            d.Key,
                                            d.Value.userID,
                                            d.Value.Score,
                                            currGame
                                        }).ToList();
            dataGridView1.Update();
        }
    }
}
