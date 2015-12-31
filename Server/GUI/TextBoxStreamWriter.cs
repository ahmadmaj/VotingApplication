using System.IO;
using System.Text;

namespace Server.GUI
{
    public class TextBoxStreamWriter : TextWriter
    {
        ServerManager _output = null;

        public TextBoxStreamWriter(ServerManager output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            _output.SetText(value.ToString()); // When character data is written, append it to the text box.
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}