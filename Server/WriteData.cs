using System;
using System.IO;


namespace Server
{
    public class WriteData
    {
        private readonly StreamWriter file;

        public WriteData(int gameID)
        {
            string filename = gameID + "_" + DateTime.Now.ToString("HHmm_ddMMyy") + ".csv";
            string path = string.Format("{0}/logs/{1}/", Directory.GetCurrentDirectory(), Program.logFolder);
            Directory.CreateDirectory(path);
            file = new StreamWriter(path + filename, true);
        }

        public void write(string line)
        {
            if(file != null)
                file.WriteLine(line);
        }

        public void Close()
        {
            file.Close();
        }
    }
}
