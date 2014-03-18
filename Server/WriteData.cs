using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    public class WriteData
    {
        private string path;
        private System.IO.StreamWriter file;
        public WriteData()
        {
            string filename = generateName();
            while (System.IO.File.Exists(filename))
            {
                filename = generateName();
            }

            path = @"C:\Users\Lena\Documents\Visual Studio 2013\Projects\VotingApplication\Server\" + filename;
            file = new System.IO.StreamWriter(path, true);
        }

        private string generateName()
        {
            Random rnd = new Random();
            int num = rnd.Next(1,10000);

            return DateTime.Now.ToString("dd_MM_yyyy") + "_" + num + ".csv";
        }

        public void write(string line)
        {
            file.WriteLine(line);
        }

        public void close()
        {
            file.Close();
        }
    }
}
