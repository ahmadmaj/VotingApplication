using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class WriteData
    {
        private string path;
        private System.IO.StreamWriter file;

        public WriteData(int gameID)
        {
            string filename = gameID.ToString() + "_" + DateTime.Now.ToString("hhmm_ddMMyy") + ".csv";
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\logs\\" + Program.logFolder + "\\");
            this.path = string.Format(Directory.GetCurrentDirectory() + "{0}" + filename, "\\logs\\" + Program.logFolder + "\\");
            this.file = new System.IO.StreamWriter(path, true);
        }

        public void write(string line)
        {
            if(file.BaseStream != null)
                file.WriteLine(line);
        }

        public void close()
        {
            file.Close();
        }
    }
}
