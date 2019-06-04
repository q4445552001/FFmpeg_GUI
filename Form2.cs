using System;
using System.Windows.Forms;

namespace FFmpeg_GUI
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent(); 
        }

        public string ffmpeg
        {
            set
            {
                richTextBox1.AppendText(value.ToString() + "\n");
            }
        }

        public string ffencode
        {
            set
            {
                richTextBox2.AppendText(value);
            }
        }

        public void clear2()
        {
            richTextBox2.Clear();
        }

        public void clear1()
        {
            richTextBox1.Clear();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }
    }
}
