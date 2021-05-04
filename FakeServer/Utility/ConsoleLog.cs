using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;

namespace FakeServer.Utility
{
    public static class ConsoleLog
    {
        class TextBoxConsoleWriter : TextBoxStreamWriter
        {
            internal TextBoxConsoleWriter(TextBox textBox) : base(textBox)
            {

            }
            public override void Write(string value)
            {
                base.Write(value);
                ConsoleLog.Write(value);
            }
        }
        static TextBoxConsoleWriter Writer;
        static TextWriter OldWriter;

        public static bool SetTextBox(TextBox textBox)
        {
            if(textBox == null)
            {
                Console.WriteLine("textBoxがnullです");
                return false;
            }
            else if(Writer != null)
            {
                Console.WriteLine("既にWriterがセットされています。Resetを呼んでください");
                return false;
            }
            Writer = new TextBoxConsoleWriter(textBox);
            OldWriter = Console.Out;
            Console.SetOut(Writer);
            return true;
        }
        public static void ResetTextBox()
        {
            if(OldWriter != null)
            {
                Console.SetOut(OldWriter);
                OldWriter = null;
            }
            Writer = null;
        }
        public static void Write(string value)
        {
            if (OldWriter != null)
            {
                OldWriter.Write(value);
            }
            else
            {
                Console.Write(value);
            }
        }
        public static void WriteLine(string value)
        {
            Write(value + Environment.NewLine);
        }
    }
}
