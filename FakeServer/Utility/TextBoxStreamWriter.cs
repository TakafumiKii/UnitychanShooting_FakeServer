using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;

namespace FakeServer.Utility
{
    
    public class TextBoxStreamWriter : TextWriter
    {
        TextBox _TextBox;

        public TextBoxStreamWriter(TextBox textBox)
        {
            _TextBox = textBox;
        }
        public override Encoding Encoding { get { return Encoding.UTF8; } }
        public override void Write(string value)
        {
            base.Write(value);
            _TextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _TextBox.AppendText(value);
                _TextBox.ScrollToEnd();
            }));
        }
        public override void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }
    }
}
