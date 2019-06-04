using System;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Drawing;

namespace FFmpeg_GUI
{
    public partial class Form3 : Form
    {
        Thread Shutdown;
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Shutdown != null && Shutdown.IsAlive) Shutdown.Abort();
            Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            Shutdown = new Thread(() =>
            {
                int timer = 30;
                while (timer > 0)
                {
                    Thread.Sleep(1000);
                    timer--;
                    label2.Text = timer.ToString().PadLeft(2, '0');
                }
                Process.Start("Shutdown.exe", " -s -t 0");
            });
            Shutdown.Start();
        }
    }
}
