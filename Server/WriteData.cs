using System;
using System.IO;


namespace Server
{
    public class WriteData
    {
        private string path;
        private StreamWriter file;

        public WriteData(int gameID)
        {
            string filename = gameID.ToString() + "_" + DateTime.Now.ToString("hhmm_ddMMyy") + ".csv";
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\logs\\" + Program.logFolder + "\\");
            this.path = string.Format(Directory.GetCurrentDirectory() + "{0}" + filename, "\\logs\\" + Program.logFolder + "\\");
            this.file = new StreamWriter(path, true);
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
