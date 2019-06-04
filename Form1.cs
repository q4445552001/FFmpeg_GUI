using System;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Text;
using System.Windows.Forms.VisualStyles;
using System.Collections.Generic;

namespace FFmpeg_GUI
{
    public partial class Form1 : Form
    {
        string[] 
            FFopcode = new string[99]
          , FFtpcode = new string[99]
          , data = new string[99]
          , mediainfo = new string[99]
          , Infilename = new string[99]
          , Outfilename = new string[99]
          , Duration = new string[99]
          , Xsubtitles = new string[99]
          , databuff = new string[99]
          , Xopcode = new string[99]
          , Xtpcode = new string[99]
          , XOfilename = new string[99];

        int count = 0 ,ffmpegcount = 0;
        Thread Nico_t0, Nico_t1, Nico_t2, Nico_mp4box;
        Form2 f2 = new Form2();

        //***********************************************************************************************************
        //錯誤訊息
        public static void ShowError(string MessageText)
        {
            MessageBox.Show(MessageText, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        //***********************************************************************************************************
        //處理DragEnter事件
        private void listBox1_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        //處理拖放事件
        private void listBox1_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            for (int i = 0; i < s.Length; i++)
            {
                string[] sdata = s[i].Split('.');
                if (sdata[sdata.Length - 1] == "mp4" || sdata[sdata.Length - 1] == "mkv" || sdata[sdata.Length - 1] == "txt")
                    listBox1.Items.Add(s[i]);
            }
            button2.Enabled = true;
            ffmpegcount = 0;
            Encode();
        }

        //***********************************************************************************************************
        //ffprobe
        static string ffprobe(string input)
        {
            string data, FrameRateMode;
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\bin\ffmpeg\ffprobe.exe",
                    Arguments = @"-of json -show_streams -show_format -v quiet " + "\"" + input + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd().Replace("\'", "").Replace("\"", "\'");
            dynamic stuff = JObject.Parse(output);
            if (stuff.streams != null || stuff.format != null)
            {
                dynamic stuff_streams = stuff.streams[0];
                dynamic stuff_format = stuff.format;

                if (stuff_streams.r_frame_rate == stuff_streams.avg_frame_rate)
                    FrameRateMode = "CBR";
                else
                    FrameRateMode = "VBR";

                string bit_rate = stuff_streams.bit_rate;
                if (bit_rate == null)
                    bit_rate = stuff_format.bit_rate;

                data = bit_rate + "," + FrameRateMode + "," + stuff_streams.r_frame_rate + "," + stuff_streams.width + "x" + stuff_streams.height + "," + stuff_format.duration + "," + stuff_format.size;
                return data;
            }
            else
            {
                data = null;
                return data;
            }
        }

        //***********************************************************************************************************
        //ffmpeg 1 pass code
        static string FFonepass(string Infilename, string concat,string FrameRateMode, string FrameRate, string threads, double BitValue)
        {
            string opcode = concat + "-i " + "\"" + Infilename + "\""
                + " -an -pass 1 -pix_fmt yuv420p -vcodec libx264 -vsync " + FrameRateMode
                + " -r " + FrameRate
                + " -threads " + threads
                + " -b:v " + BitValue
                + " -maxrate " + BitValue
                + " -bufsize " + (BitValue * 2)
                + " -y -f rawvideo NUL";
            return opcode;
        }

        //***********************************************************************************************************
        //ffmpeg 2 pass code
        static string FFtwopass(string Infilename,string sub ,string Outfilename, string concat, string FrameRateMode, string FrameRate, string threads, double BitValue, string outres)
        {
            string tpcode = concat + "-i " + "\"" + Infilename + "\"" + sub
                + " -pass 2 -pix_fmt yuv420p -vcodec libx264 -vsync " + FrameRateMode
                + " -r " + FrameRate
                + " -threads " + threads
                + " -b:v " + BitValue
                + " -maxrate " + BitValue
                + " -bufsize " + (BitValue * 2)
                + " -map_chapters -1 -y " + outres + "\"" + Outfilename + "\"";
            return tpcode;
        }

        //***********************************************************************************************************
        //X264 1 pass code
        static string Xonepass(string Infilename, string concat, string FrameRateMode, string FrameRate, string threads, double BitValue)
        {
            string opcode = "--x26x-binary \"" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase 
                + "bin\\x264\\x264.exe\" --bitrate " + Math.Round(BitValue / 1000,0) 
                + " --pass 1 --threads " + threads + " --stats \"x2642pass.stats\" -o NUL " + "\"avsbuff.avs\"";
            return opcode;
        }

        //***********************************************************************************************************
        //X264 2 pass code
        static string Xtwopass(string Infilename, string sub, string Outfilename, string concat, string FrameRateMode, string FrameRate, string threads, double BitValue, string outres)
        {
            string opcode = "";
            if (outres != "")
            {
                opcode = "--x26x-binary \"" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                    + "bin\\x264\\x264.exe\" --bitrate " + Math.Round(BitValue / 1000, 0) + " --pass 2 --threads " + threads
                    + " --stats \"x2642pass.stats\" --vf resize:width=" + outres.Split('x')[0].Split(' ')[1] + ",height=" + outres.Split('x')[1] + ",sar=1:1 -o "
                    + "\"avsbuff.264\" \"avsbuff.avs\"";
            }
            else
                opcode = "--x26x-binary \"" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                    + "bin\\x264\\x264.exe\" --bitrate " + Math.Round(BitValue / 1000, 0) + " --pass 2 --threads " + threads
                    + " --stats \"x2642pass.stats\" -o " + "\"avsbuff.264\" \"avsbuff.avs\"";
            return opcode;
        }

        //***********************************************************************************************************
        public Form1()
        {
            InitializeComponent();
            if (!File.Exists(@".\bin\ffmpeg\ffprobe.exe") || !File.Exists(@".\bin\ffmpeg\ffmpeg.exe"))
            {
                ShowError("缺少主要檔案，強制關閉。");
                System.Environment.Exit(System.Environment.ExitCode);
            }

            //拖放程式碼開始************************************************************************
            this.listBox1.DragDrop += new
                System.Windows.Forms.DragEventHandler(this.listBox1_DragDrop);
            this.listBox1.DragEnter += new
                System.Windows.Forms.DragEventHandler(this.listBox1_DragEnter);
            //拖放程式碼結束************************************************************************


            //Combox5
            comboBox5.Items.Add(new ComboboxItem("Auto", "Auto"));
            comboBox5.Items.Add(new ComboboxItem("23.976", "24000/1001"));
            comboBox5.Items.Add(new ComboboxItem("29.970", "30000/1001"));

            //CPU Processor
            int cpunumber = Environment.ProcessorCount;
            System.Object[] ItemObject = new System.Object[cpunumber + 1];
            for (int i = 0; i <= cpunumber; i++)
                ItemObject[i] = i;
            comboBox4.Items.AddRange(ItemObject);

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;
            comboBox6.SelectedIndex = 0;

            //f2.Show();
            //f2.Hide();
        }

        //***********************************************************************************************************
        //讀檔
        private void button1_Click(object sender, EventArgs e)
        {
            count = listBox1.Items.Count + 1;
            //讀取檔案
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Multiselect = true;
            openfile.Filter = "*.mov;*.mp4;*mkv;*.txt|*.mov;*.mp4;*mkv;*.avi;*.txt";
            if (openfile.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in openfile.FileNames)
                {
                    listBox1.AllowDrop = false;
                    listBox1.Items.Add(file);
                    listBox1.AllowDrop = true;
                }
            }
            ffmpegcount = 0;

            Encode();
        }

        //***********************************************************************************************************
        //產生編碼
        private void Encode()
        {
        restart: 
            if (listBox1.Items.Count != 0)
            {
                textBox1.Text = "0/" + listBox1.Items.Count.ToString();
                try //檔案錯誤例外處理
                {
                    count = 0;
                    foreach (string file in listBox1.Items)
                    {
                        Infilename[count] = file;
                        Outfilename[count] = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + "-0.mp4";
                        XOfilename[count] = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + ".264";
                        Directory.SetCurrentDirectory(Path.GetDirectoryName(file)); //指定程式路徑
                        //textBox1.Text = System.Environment.CurrentDirectory; // 程式目前路徑

                        f2.clear2();
                        //txt
                        if (Path.GetExtension(file).ToString() == ".txt")
                        {
                            string Intext, textdata, line, Framestr = "", FPSM, FrameRateMode = "", outres = "";
                            double FrameMax = 0, DurationMax = 0, BitRate = 0, sizecount = 0, BitValue = 0;
                            int WidthMin = 0, HeightMin = 0;
                            if (ffmpegcount == 0 && listView1.Items.Count <= count)
                            {
                                StreamReader opfile = new StreamReader(file);
                                while ((line = opfile.ReadLine()) != null)
                                {
                                    Intext = Path.GetDirectoryName(file) + "\\" + line.Split('\'')[1];
                                    if (!File.Exists(Intext))
                                    {
                                        ShowError(line.Split('\'')[1] + " 檔案不存在");
                                        listBox1.Items.Remove(file);
                                        goto restart;
                                    }
                                    textdata = ffprobe(Intext);

                                    String[] substrings = textdata.Split(',');
                                    Framestr = substrings[2];
                                    double countfps = Convert.ToDouble(Framestr.Split('/')[0]) / Convert.ToDouble(Framestr.Split('/')[1]);
                                    if (BitRate < Convert.ToDouble(substrings[0]))
                                        BitRate = Convert.ToDouble(substrings[0]);
                                    if (FrameMax < countfps)
                                        FrameMax = countfps;
                                    if (WidthMin < Convert.ToInt32(substrings[3].Split('x')[0]))
                                        WidthMin = Convert.ToInt32(substrings[3].Split('x')[0]);
                                    if (HeightMin < Convert.ToInt32(substrings[3].Split('x')[1]))
                                        HeightMin = Convert.ToInt32(substrings[3].Split('x')[1]);
                                    DurationMax = DurationMax + Convert.ToDouble(substrings[4]);
                                    sizecount = sizecount + Convert.ToDouble(substrings[5]);
                                }
                                opfile.Close();
                                Duration[count] = DurationMax.ToString();
                                Infilename[count] = file.Replace("\\", "//");
                                Outfilename[count] = Outfilename[count].Replace("\\", "//");
                                XOfilename[count] = XOfilename[count].Replace("\\", "//");
                                string res = WidthMin.ToString() + "x" + HeightMin.ToString();

                                if (FrameMax < 24)
                                {
                                    FrameRateMode = "1";
                                    FPSM = "CBR";
                                }
                                else
                                {
                                    FrameRateMode = "2";
                                    FPSM = "VBR";
                                }

                                BitValue = Math.Round(sizecount * 8 / DurationMax - 150000, 0);

                                data[count] = BitValue + "," + FPSM + "," + Framestr + "," + res + "," + DurationMax.ToString() + "," + sizecount;
                                databuff[count] = data[count];
                                mediainfo[count] = BitRate + "," + FPSM + "," + Framestr + "," + res + "," + DurationMax.ToString() + "," + sizecount;
                            }
                            else
                            {
                                String[] listdata = data[count].Split(',');
                                BitValue = Convert.ToDouble(listdata[0]);
                                Framestr = listdata[2];

                                if (listdata[1] == "CBR") FrameRateMode = "1";
                                else FrameRateMode = "2";

                                if (listdata[3] != databuff[count].Split(',')[3])
                                    if (listdata[3] == "640x480") outres = "-s 640x480 ";
                                    else if (listdata[3] == "1280x720") outres = "-s 1280x720 ";
                                    else if (listdata[3] == "1920x1080") outres = "-s 1920x1080 ";
                                    else outres = "";
                            }

                            FFopcode[count] = FFonepass(file, "-f concat ", FrameRateMode, Framestr, comboBox4.SelectedItem.ToString(), BitValue);
                            FFtpcode[count] = FFtwopass(file, "", Outfilename[count], "-f concat ", FrameRateMode, Framestr, comboBox4.SelectedItem.ToString(), BitValue, outres);
                            Xopcode[count] = Xonepass(file, "-f concat ", FrameRateMode, Framestr, comboBox4.SelectedItem.ToString(), BitValue);
                            Xtpcode[count] = Xtwopass(file, "", XOfilename[count], "-f concat ", FrameRateMode, Framestr, comboBox4.SelectedItem.ToString(), BitValue, outres);

                            if (comboBox6.SelectedItem.ToString() == "FFmpeg")
                                for (int i = 0; i < listBox1.Items.Count; i++)
                                {
                                    f2.ffencode = listBox1.Items[i].ToString() + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "One Pass :\n";
                                    f2.ffencode = FFopcode[i] + "\n";
                                    f2.ffencode = "Two Pass :\n";
                                    f2.ffencode = FFtpcode[i] + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "\n";
                                } 

                            else if (comboBox6.SelectedItem.ToString() == "x264")
                                for (int i = 0; i < listBox1.Items.Count; i++)
                                {
                                    f2.ffencode = listBox1.Items[i].ToString() + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "One Pass :\n";
                                    f2.ffencode = Xopcode[i] + "\n";
                                    f2.ffencode = "Two Pass :\n";
                                    f2.ffencode = Xtpcode[i] + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "\n";
                                }

                            if (ffmpegcount == 0)
                                if (listView1.Items.Count < listBox1.Items.Count && count >= listView1.Items.Count)
                                {
                                    listView1.Items.Add(Path.GetFileName(file));
                                    String[] showdata = data[count].Split(',');
                                    String[] showOriginal = mediainfo[count].Split(',');
                                    double Audio_capacity = 128000 * Convert.ToDouble(showdata[4]) / 8; //計算Audio大小
                                    double Video_estimate_capacity = Convert.ToDouble(showdata[0]) * Convert.ToDouble(showdata[4]) / 8; //Video預估大小
                                    showdata[5] = Math.Round((Audio_capacity + Video_estimate_capacity) / 1024 / 1024, 2).ToString() + " MB";
                                    showdata[0] = Math.Round(Convert.ToDouble(showOriginal[0]) / 1000, 0) + ">" + Math.Round((Convert.ToDouble(showdata[0]) / 1000), 0) + " kb/s";
                                    if (comboBox6.SelectedItem.ToString() == "x264") showdata[1] = showOriginal[1] + ">CBR";
                                    else if (comboBox6.SelectedItem.ToString() == "FFmpeg") showdata[1] = showOriginal[1] + ">" + showdata[1]; //fps mode
                                    showdata[2] = Math.Round(Convert.ToDouble(showOriginal[2].Split('/')[0]) / Convert.ToDouble(showOriginal[2].Split('/')[1]), 3) + ">" 
                                        + Math.Round((Convert.ToDouble(showdata[2].Split('/')[0]) / Convert.ToDouble(showdata[2].Split('/')[1])), 3).ToString();
                                    showdata[3] = showOriginal[3] + ">" + showdata[3];
                                    showdata[4] = TimeSpan.FromSeconds(Convert.ToDouble(showdata[4])).ToString(@"hh\:mm\:ss");
                                    string capacity = Math.Round(Convert.ToDouble(showOriginal[5]) / 1024 / 1024, 2).ToString(); //原始大小
                                    showdata[5] = capacity + ">" + showdata[5];
                                    listView1.Items[count].SubItems.AddRange(showdata);
                                    listView1.Items[count].SubItems.Add("00.00 %");
                                    listView1.Items[count].SubItems.Add("00:00:00");
                                    listView1.Items[count].SubItems.Add(file.Replace("//", @"\"));
                                    listView1.Items[count].ToolTipText = Path.GetFileName(file) + "\nBitRate: " + Math.Round(Convert.ToDouble(showOriginal[0]) / 1000, 0) + " kb/s\nFPS模式: " + showOriginal[1] + "\nFPS: "
                                        + Math.Round(Convert.ToDouble(showOriginal[2].Split('/')[0]) / Convert.ToDouble(showOriginal[2].Split('/')[1]), 3) + "\n解析度: " + showOriginal[3] + "\n檔案大小: " + capacity + " MB";
                                }
                        }

                        //mp4,mkv
                        else
                        {
                            if (ffmpegcount == 0 && listView1.Items.Count <= count)
                            {
                                mediainfo[count] = ffprobe(file);

                                String[] mediabuff = mediainfo[count].Split(',');
                                double BitValue = 0;
                                if (Convert.ToDouble(mediabuff[0]) >= 1100000) BitValue = 1100000;
                                else BitValue = Convert.ToDouble(mediabuff[0]) - 50000;
                                data[count] = BitValue + "," + mediabuff[1] + "," + mediabuff[2] + "," + mediabuff[3] + "," + mediabuff[4] + "," + mediabuff[5];
                                databuff[count] = data[count];
                            }

                            //ffmpeg code
                            Infilename[count] = file.Replace("\\", "//");
                            Outfilename[count] = Outfilename[count].Replace("\\", "//");
                            XOfilename[count] = XOfilename[count].Replace("\\", "//");

                            string outres = "", FrameRateMode;
                            String[] substrings = data[count].Split(',');
                            double BitRate = Convert.ToDouble(substrings[0]);
                            Duration[count] = substrings[4];
                            if (substrings[1] == "CBR") FrameRateMode = "1";
                            else FrameRateMode = "2";

                            if (data[count].Split(',')[3] != databuff[count].Split(',')[3])
                            {
                                if (data[count].Split(',')[3] == "640x480") outres = "-s 640x480 ";
                                else if (data[count].Split(',')[3] == "1280x720") outres = "-s 1280x720 ";
                                else if (data[count].Split(',')[3] == "1920x1080") outres = "-s 1920x1080 ";
                            }
                            else outres = "";

                            if (File.Exists(Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".ass"))
                            {
                                string subtitles = " -vf \"subtitles='" + Path.GetFileNameWithoutExtension(file) + ".ass" + "'\"";
                                Xsubtitles[count] = Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".ass";
                                FFopcode[count] = FFonepass(file, "", FrameRateMode, substrings[2], comboBox4.SelectedItem.ToString(), BitRate);
                                FFtpcode[count] = FFtwopass(file, subtitles, Outfilename[count], "", FrameRateMode, substrings[2], comboBox4.SelectedItem.ToString(), BitRate, outres);

                            }
                            else
                            {
                                FFopcode[count] = FFonepass(file, "", FrameRateMode, substrings[2], comboBox4.SelectedItem.ToString(), BitRate);
                                FFtpcode[count] = FFtwopass(file, "", Outfilename[count], "", FrameRateMode, substrings[2], comboBox4.SelectedItem.ToString(), BitRate, outres);
                            }

                            Xopcode[count] = Xonepass(file, "", FrameRateMode, substrings[2], comboBox4.SelectedItem.ToString(), BitRate);
                            Xtpcode[count] = Xtwopass(file, "", XOfilename[count], "", FrameRateMode, substrings[2], comboBox4.SelectedItem.ToString(), BitRate, outres);

                            if (comboBox6.SelectedItem.ToString() == "FFmpeg")
                                for (int i = 0; i < listBox1.Items.Count; i++)
                                {
                                    f2.ffencode = listBox1.Items[i].ToString() + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "One Pass :\n";
                                    f2.ffencode = FFopcode[i] + "\n";
                                    f2.ffencode = "Two Pass :\n";
                                    f2.ffencode = FFtpcode[i] + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "\n";
                                }

                            else if (comboBox6.SelectedItem.ToString() == "x264")
                                for (int i = 0; i < listBox1.Items.Count; i++)
                                {
                                    f2.ffencode = listBox1.Items[i].ToString() + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "One Pass :\n";
                                    f2.ffencode = Xopcode[i] + "\n";
                                    f2.ffencode = "Two Pass :\n";
                                    f2.ffencode = Xtpcode[i] + "\n";
                                    f2.ffencode = "---------------------------------------------------------------------------------------------------------------------------------------------------------------\n";
                                    f2.ffencode = "\n";
                                }

                            if (ffmpegcount == 0)
                                if (listView1.Items.Count < listBox1.Items.Count && count >= listView1.Items.Count)
                                {
                                    listView1.Items.Add(Path.GetFileName(file));
                                    String[] showdata = data[count].Split(',');
                                    String[] showOriginal = mediainfo[count].Split(',');
                                    double Audio_capacity = Convert.ToDouble(showOriginal[5]) - (Convert.ToDouble(showOriginal[0]) * Convert.ToDouble(showOriginal[4]) / 8); //計算Audio大小
                                    double Video_estimate_capacity = Convert.ToDouble(showdata[0]) * Convert.ToDouble(showdata[4]) / 8; //Video預估大小
                                    showdata[5] = Math.Round((Audio_capacity + Video_estimate_capacity) / 1024 / 1024, 2).ToString() + " MB";
                                    showdata[0] = Math.Round(Convert.ToDouble(showOriginal[0]) / 1000, 0) + ">" + Math.Round((Convert.ToDouble(showdata[0]) / 1000), 0) + " kb/s"; //bitrate
                                    if (comboBox6.SelectedItem.ToString() == "x264") showdata[1] = showOriginal[1] + ">CBR";
                                    else if (comboBox6.SelectedItem.ToString() == "FFmpeg") showdata[1] = showOriginal[1] + ">" + showdata[1]; //fps mode
                                    showdata[2] = Math.Round(Convert.ToDouble(showOriginal[2].Split('/')[0]) / Convert.ToDouble(showOriginal[2].Split('/')[1]), 3) + ">" 
                                        + Math.Round((Convert.ToDouble(showdata[2].Split('/')[0]) / Convert.ToDouble(showdata[2].Split('/')[1])), 3).ToString(); //fps
                                    showdata[3] = showOriginal[3] + ">" + showdata[3];
                                    showdata[4] = TimeSpan.FromSeconds(Convert.ToDouble(showdata[4])).ToString(@"hh\:mm\:ss"); //時間
                                    string capacity = Math.Round(Convert.ToDouble(showOriginal[5]) / 1024 / 1024, 2).ToString(); //原始大小
                                    showdata[5] = capacity + ">" + showdata[5];
                                    listView1.Items[count].SubItems.AddRange(showdata);
                                    listView1.Items[count].SubItems.Add("00.00 %");
                                    listView1.Items[count].SubItems.Add("00:00:00");
                                    listView1.Items[count].SubItems.Add(file.Replace("//", @"\"));
                                    listView1.Items[count].ToolTipText = Path.GetFileName(file) + "\nBitRate: " + Math.Round(Convert.ToDouble(showOriginal[0]) / 1000, 0) + " kb/s\nFPS模式: " + showOriginal[1] + "\nFPS: "
                                        + Math.Round(Convert.ToDouble(showOriginal[2].Split('/')[0]) / Convert.ToDouble(showOriginal[2].Split('/')[1]), 3) + "\n解析度: " + showOriginal[3] + "\n檔案大小: " + capacity + " MB";
                                }
                        }
                        count++;
                    }
                }
                catch (IndexOutOfRangeException print)
                {
                    listBox1.Items.Remove(Infilename[count]);
                    ShowError(Infilename[count] + " 格式錯誤\n" + print);
                    goto restart;
                }
                catch (NullReferenceException print)
                {
                    Infilename[count] = Infilename[count].Replace("//", "\\");
                    listBox1.Items.Remove(Infilename[count]);
                    ShowError(Infilename[count] + " 格式錯誤\n" + print);
                    goto restart;
                }
            }

            if (listBox1.Items.Count == 0)
            {
                button2.Enabled = false;
            }
            else if (listBox1.SelectedIndex == -1)
            {
                listBox1.SelectedIndex = 0;
                button2.Enabled = true;
            }
        }

        //***********************************************************************************************************
        //開始轉檔
        private void button2_Click(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            if (listBox1.Items.Count != 0)
            {
                Nico_t0 = new Thread(() =>
                {
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    comboBox6.Enabled = false;
                    numericUpDown1.Enabled = false;
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        listView1.Items[i].SubItems[7].ForeColor = Color.Black;
                        textBox1.Text = (i+1) + "/" + listBox1.Items.Count.ToString();
                        //int clearcount = 0;
                        sw.Reset();
                        sw.Start();
                        button5.Enabled = true;
                        Directory.SetCurrentDirectory(Path.GetDirectoryName(@Infilename[i]));
                        progressBar1.Value = 0;
                        double prodatabar = 0;
                        //FFmpeg
                        //one pass
                        if (comboBox6.SelectedItem.ToString() == "FFmpeg")
                        {
                            Nico_t1 = new Thread(() =>
                            {
                                var run = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\ffmpeg\", "ffmpeg.exe"),
                                        Arguments = FFopcode[i],
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardError = true
                                    }
                                };
                                run.Start();
                                StreamReader sr = run.StandardError;
                                while (!sr.EndOfStream)
                                {
                                    TimeSpan Timemint = TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds);
                                    listView1.Items[i].SubItems[8].Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Timemint.Hours, Timemint.Minutes, Timemint.Seconds);
                                    string srstring = sr.ReadLine();
                                    string[] split = srstring.Split(' ');
                                    /*f2.ffmpeg = srstring;
                                    clearcount++;
                                    if (clearcount >= 1000)
                                    {
                                        f2.clear1();
                                        clearcount = 0;
                                    }*/
                                    foreach (var row in split)
                                    {
                                        if (row.StartsWith("time="))
                                        {
                                            var time = row.Split('=');
                                            var Progress = TimeSpan.Parse(time[1]).TotalSeconds;
                                            double prodata = Progress / Convert.ToDouble(Duration[i]) * 50;
                                            if (prodata < 100)
                                            {
                                                progressBar1.Value = (int)prodata;
                                                /*progressBar1.CreateGraphics().DrawString(String.Format("{0:00.00}", prodata) + " %" + " %", new Font("微軟正黑體", (float)11, FontStyle.Regular)
                                                , Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 10));*/
                                                listView1.Items[i].SubItems[7].Text = String.Format("{0:00.00}", prodata) + " %";
                                            }
                                            prodatabar = prodata;
                                        }
                                        if (srstring.StartsWith("Error writing"))
                                        {
                                            ShowError("磁碟已滿");
                                            button5_Click(sender, e);
                                        }
                                    }
                                }
                            });
                            Nico_t1.Start();
                            Nico_t1.Join();
                            //two pass
                            Nico_t2 = new Thread(() =>
                            {
                                var run = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\ffmpeg\", "ffmpeg.exe"),
                                        Arguments = FFtpcode[i],
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardError = true
                                    }
                                };
                                run.Start();
                                StreamReader sr = run.StandardError;
                                while (!sr.EndOfStream)
                                {
                                    TimeSpan Timemint = TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds);
                                    listView1.Items[i].SubItems[8].Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Timemint.Hours, Timemint.Minutes, Timemint.Seconds);
                                    string srstring = sr.ReadLine();
                                    string[] split = srstring.Split(' ');
                                    /*f2.ffmpeg = srstring;
                                    clearcount++;
                                    if (clearcount >= 1000)
                                    {
                                        f2.clear1();
                                        clearcount = 0;
                                    }*/
                                    foreach (var row in split)
                                    {
                                        if (row.StartsWith("time="))
                                        {
                                            var time = row.Split('=');
                                            var Progress = TimeSpan.Parse(time[1]).TotalSeconds;
                                            double prodata = prodatabar + (Progress / Convert.ToDouble(Duration[i]) * 50);
                                            if (prodata < 100)
                                            {
                                                progressBar1.Value = (int)prodata;
                                                /*progressBar1.CreateGraphics().DrawString(String.Format("{0:00.00}", prodata) + " %", new Font("Times New Roman", (float)11, FontStyle.Regular)
                                                   , Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 10));*/
                                                listView1.Items[i].SubItems[7].Text = String.Format("{0:00.00}", prodata) + " %";
                                            }
                                        }
                                        if (srstring.StartsWith("Error writing"))
                                        {
                                            ShowError("磁碟已滿");
                                            button5_Click(sender, e);
                                        }
                                    }
                                    if (sr.EndOfStream)
                                    {
                                        progressBar1.Value = 100;
                                        /*progressBar1.CreateGraphics().DrawString("100 %", new Font("微軟正黑體", (float)11, FontStyle.Regular)
                                                       , Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 10));*/
                                        listView1.Items[i].SubItems[7].Text = "100 %";
                                        File.Delete(@".\ffmpeg2pass-0.log");
                                        File.Delete(@".\ffmpeg2pass-0.log.mbtree");
                                        File.Delete(@".\ffmpeg2pass-0.log.temp");
                                        File.Delete(@".\ffmpeg2pass-0.log.mbtree.temp");
                                    }
                                }
                            });
                            Nico_t2.Start();
                            Nico_t2.Join();
                            sw.Stop();
                        }

                        //x264
                        else if (comboBox6.SelectedItem.ToString() == "x264")
                        {
                            listView1.Items[i].UseItemStyleForSubItems = false;
                            listView1.Items[i].SubItems[7].ForeColor = Color.Black;
                            string avs = "";
                            string[] fpsmode = data[i].Split(',')[2].Split('/');

                            //txt
                            if (Path.GetExtension(@Infilename[i]).ToString() == ".txt")
                            {
                                string line;
                                string[] avsdata = new string[99];
                                int avscount = 1;
                                StreamReader opfile = new StreamReader(@Infilename[i]);
                                while ((line = opfile.ReadLine()) != null)
                                {
                                    avsdata[avscount] = @"FFVideoSource(""" + Path.GetDirectoryName(Infilename[i].Replace("//", "\\")) + "\\" + line.Split('\'')[1] + @""", fpsnum=" + fpsmode[0] + ", fpsden=" + fpsmode[1] + @")";
                                    avscount++;
                                }
                                opfile.Close();
                                string avsb1 = @"LoadPlugin(""" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\bin\x264\ffms2.dll"") ";
                                for (int k = 1; k < avscount; k++)
                                {
                                    if (k != 1) avsb1 = avsb1 + "+" + avsdata[k];
                                    else avsb1 = avsb1 + avsdata[k];
                                }
                                string avsb99 = " #deinterlace #crop #resize #denoise";
                                avs = avsb1 + avsb99;
                                avscount = 1;
                            }
                            //mp4,mkv
                            else
                            {
                                if (Xsubtitles[i] != null)
                                {
                                    avs = @"LoadPlugin(""" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\bin\x264\ffms2.dll"") FFVideoSource(""" + Path.GetFileName(Infilename[i].Replace("//", "\\")) + @""", fpsnum=" + fpsmode[0] + ", fpsden=" + fpsmode[1] + @") LoadPlugin(""" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\bin\x264\VSFilter.dll"") TextSub(""avsbuff.ass"",1) #deinterlace #crop #resize #denoise";
                                    File.Copy(Xsubtitles[i], "avsbuff.ass",true);
                                }
                                else
                                    avs = @"LoadPlugin(""" + AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\bin\x264\ffms2.dll"") FFVideoSource(""" + Path.GetFileName(Infilename[i].Replace("//", "\\")) + @""", fpsnum=" + fpsmode[0] + ", fpsden=" + fpsmode[1] + @") #deinterlace #crop #resize #denoise";
                            }

                            System.IO.File.WriteAllText(@".\avsbuff.avs", avs);

                            //mkv audio
                            if (Path.GetExtension(@Infilename[i]).ToString() == ".mkv" && !checkBox2.Checked)
                            {
                                Thread Nico_mkvextract = new Thread(() =>
                                    {
                                        var run = new Process
                                        {
                                            StartInfo = new ProcessStartInfo
                                            {
                                                FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\x264\", "mkvextract.exe"),
                                                Arguments = "tracks \"" + Infilename[i].Replace("//", "\\") + "\" 1:\"" + Path.GetDirectoryName(Infilename[i]) + "\\avsbuff.aac\"",
                                                UseShellExecute = false,
                                                CreateNoWindow = true,
                                                RedirectStandardError = true
                                            }
                                        };
                                        run.Start();
                                    });
                                Nico_mkvextract.Start();
                                Nico_mkvextract.Join();
                            }

                            //txt audio
                            if (Path.GetExtension(@Infilename[i]).ToString() == ".txt")
                            {
                                Thread Nico_txtconcat = new Thread(() =>
                                {
                                    var run = new Process
                                    {
                                        StartInfo = new ProcessStartInfo
                                        {
                                            FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\ffmpeg\", "ffmpeg.exe"),
                                            Arguments = @"-f concat -i """ + Infilename[i].Replace("//", "\\") + @""" -vn -acodec copy -y ""avsbuff.aac""",
                                            UseShellExecute = false,
                                            CreateNoWindow = true,
                                            RedirectStandardError = true
                                        }
                                    };
                                    run.Start();
                                });
                                Nico_txtconcat.Start();
                                Nico_txtconcat.Join();
                            }

                            //mov audio
                            if (Path.GetExtension(@Infilename[i]).ToString() == ".MOV" || checkBox2.Checked)
                            {
                                Thread Nico_movaudio = new Thread(() =>
                                {
                                    var run = new Process
                                    {
                                        StartInfo = new ProcessStartInfo
                                        {
                                            FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\ffmpeg\", "ffmpeg.exe"),
                                            Arguments = @"-i """ + Infilename[i].Replace("//", "\\") + @""" -vn -acodec copy -y ""avsbuff.aac""",
                                            UseShellExecute = false,
                                            CreateNoWindow = true,
                                            RedirectStandardError = true
                                        }
                                    };
                                    run.Start();
                                });
                                Nico_movaudio.Start();
                                Nico_movaudio.Join();
                            }

                            //onepass
                            Nico_t1 = new Thread(() =>
                            {
                                var run = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\x264\", "avs4x26x.exe"),
                                        Arguments = Xopcode[i],
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardError = true
                                    }
                                };
                                run.Start();
                                StreamReader sr = run.StandardError;
                                while (!sr.EndOfStream)
                                {
                                    TimeSpan Timemint = TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds);
                                    listView1.Items[i].SubItems[8].Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Timemint.Hours, Timemint.Minutes, Timemint.Seconds);
                                    string srstring = sr.ReadLine();
                                    /*f2.ffmpeg = srstring;
                                    clearcount++;
                                    if (clearcount >= 1000)
                                    {
                                        f2.clear1();
                                        clearcount = 0;
                                    }*/
                                    if (srstring.IndexOf("frames,") != -1 && srstring.IndexOf("[") != -1)
                                    {
                                        double prodata = Math.Round(Convert.ToDouble(srstring.Substring(srstring.IndexOf("[") + 1, srstring.LastIndexOf("%") - 1)) / 2.01,1);
                                        if (prodata < 99)
                                        {
                                            progressBar1.Value = (int)prodata;
                                            listView1.Items[i].SubItems[7].Text = prodata + " %";
                                            /*progressBar1.CreateGraphics().DrawString(prodata.ToString() + " %", new Font("微軟正黑體", (float)11, FontStyle.Regular)
                                                , Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 10));*/
                                        }
                                        prodatabar = prodata;
                                    }
                                }
                            });
                            Nico_t1.Start();
                            Nico_t1.Join();
                            //two pass
                            Nico_t2 = new Thread(() =>
                            {
                                var run = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\x264\", "avs4x26x.exe"),
                                        Arguments = Xtpcode[i],
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardError = true
                                    }
                                };
                                run.Start();
                                StreamReader sr = run.StandardError;
                                while (!sr.EndOfStream)
                                {
                                    TimeSpan Timemint = TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds);
                                    listView1.Items[i].SubItems[8].Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Timemint.Hours, Timemint.Minutes, Timemint.Seconds);
                                    string srstring = sr.ReadLine();
                                    /*f2.ffmpeg = srstring;
                                    clearcount++;
                                    if (clearcount >= 1000)
                                    {
                                        f2.clear1();
                                        clearcount = 0;
                                    }*/
                                    if (srstring.IndexOf("frames,") != -1 && srstring.IndexOf("[") != -1)
                                    {
                                        double prodata = Math.Round((prodatabar + Convert.ToDouble(srstring.Substring(srstring.IndexOf("[") + 1, srstring.LastIndexOf("%") - 1)) / 2.01), 1);
                                        if (prodata < 99)
                                        {
                                            progressBar1.Value = (int)prodata;
                                            listView1.Items[i].SubItems[7].Text = prodata + " %";
                                            /*progressBar1.CreateGraphics().DrawString(prodata.ToString() + " %", new Font("微軟正黑體", (float)11, FontStyle.Regular)
                                                   , Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 10));*/
                                        }
                                    }
                                }
                            });
                            Nico_t2.Start();
                            Nico_t2.Join();

                            Nico_mp4box = new Thread(() =>
                            {
                                StreamReader sr;
                                if (Path.GetExtension(@Infilename[i]).ToString() == ".mkv" || Path.GetExtension(@Infilename[i]).ToString() == ".txt" || Path.GetExtension(@Infilename[i]).ToString() == ".MOV")
                                {
                                    var run = new Process
                                    {
                                        StartInfo = new ProcessStartInfo
                                        {
                                            FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\x264\", "mp4box.exe"),
                                            Arguments = "-add \"avsbuff.264\" -add \"avsbuff.aac\" \"" + Outfilename[i].Replace("//", "\\") + "\"",
                                            UseShellExecute = false,
                                            CreateNoWindow = true,
                                            RedirectStandardError = true
                                        }
                                    };
                                    //run.Exited += new EventHandler(process_Exited);
                                    //run.EnableRaisingEvents = true;
                                    run.Start();
                                    sr = run.StandardError;
                                }
                                else
                                {
                                    var run = new Process
                                    {
                                        StartInfo = new ProcessStartInfo
                                        {
                                            FileName = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @".\bin\x264\", "mp4box.exe"),
                                            Arguments = "-add \"avsbuff.264\" -add \"" + Infilename[i].Replace("//", "\\") + "\"#audio \"" + Outfilename[i].Replace("//", "\\") + "\"",
                                            UseShellExecute = false,
                                            CreateNoWindow = true,
                                            RedirectStandardError = true
                                        }
                                    };
                                    //run.Exited += new EventHandler(process_Exited);
                                    //run.EnableRaisingEvents = true;
                                    run.Start();
                                    sr = run.StandardError;
                                }
                                string buff = sr.ReadToEnd();
                                f2.ffmpeg = buff;
                                if (buff.IndexOf("Error") != -1)
                                {
                                    listView1.Items[i].SubItems[7].Text = "ERROR";
                                    listView1.Items[i].SubItems[7].ForeColor = Color.Red;
                                }
                                else if (sr.EndOfStream)
                                {
                                    progressBar1.Value = 100;
                                    /*progressBar1.CreateGraphics().DrawString("100 %", new Font("微軟正黑體", (float)11, FontStyle.Regular)
                                                   , Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 10));*/
                                    listView1.Items[i].SubItems[7].Text = "100 %";
                                    File.Delete(@"./x2642pass.stats");
                                    File.Delete(@"./x2642pass.stats.mbtree");
                                    File.Delete(@"./avsbuff.avs");
                                    File.Delete(@"./avsbuff.ass");
                                    File.Delete(@"./avsbuff.264");
                                    File.Delete(@"./avsbuff.aac");
                                    File.Delete(Infilename[i].Replace("//", "\\") + @".ffindex");
                                    TimeSpan Timemint = TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds);
                                    listView1.Items[i].SubItems[8].Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Timemint.Hours, Timemint.Minutes, Timemint.Seconds);
                                }
                            });
                            Nico_mp4box.Start();
                            Nico_mp4box.Join();
                            sw.Stop();
                        }
                    }
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button5.Enabled = false;
                    button8.Enabled = true;
                    button9.Enabled = true;
                    if (comboBox6.SelectedItem.ToString() == "FFmpeg")
                        comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                    comboBox6.Enabled = true;
                    numericUpDown1.Enabled = true;
                    if (checkBox1.Checked.ToString() == "True")
                    {
                        Form3 f3 = new Form3();
                        Application.Run(f3);
                    }
                });
                Nico_t0.Start();
            }
        }

        //***********************************************************************************************************
        //Bitrate
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem.ToString() == "Auto")
                numericUpDown1.Enabled = false;

            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].Checked)
                {
                    String[] listdata = data[i].Split(',');
                    if (comboBox2.SelectedItem.ToString() == "Auto")
                    {
                        numericUpDown1.Enabled = false;
                        if (Convert.ToDouble(databuff[i].Split(',')[0]) >= 1100000)
                            data[i] = "1100000" + "," + listdata[1] + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                        else
                            data[i] = databuff[i].Split(',')[0] + "," + listdata[1] + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                        if (comboBox1.SelectedItem.ToString() == "Auto" && comboBox3.SelectedItem.ToString() == "Auto" && comboBox5.SelectedItem.ToString() == "Auto")
                            data[i] = databuff[i];
                    }
                    else if (comboBox2.SelectedItem.ToString() == "Manual")
                    {
                        numericUpDown1.Enabled = true;
                        data[i] = numericUpDown1.Value.ToString() + "000," + listdata[1] + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                    }
                    listdata = data[i].Split(',');
                    String[] showOriginal = mediainfo[i].Split(',');
                    listView1.Items[i].SubItems[1].Text = Math.Round(Convert.ToDouble(showOriginal[0]) / 1000, 0) + ">" + Math.Round(Convert.ToDouble(listdata[0]) / 1000, 0).ToString() + " kb/s";
                    double Audio_capacity = 0;
                    if (Path.GetExtension(@Infilename[i]).ToString() == ".txt")
                        Audio_capacity = 128000 * Convert.ToDouble(showOriginal[4]) / 8; //計算Audio大小
                    else
                        Audio_capacity = Convert.ToDouble(showOriginal[5]) - (Convert.ToDouble(showOriginal[0]) * Convert.ToDouble(showOriginal[4]) / 8); //計算Audio大小
                    double Video_estimate_capacity = Convert.ToDouble(listdata[0]) * Convert.ToDouble(listdata[4]) / 8; //Video預估大小
                    listView1.Items[i].SubItems[6].Text = Math.Round(Convert.ToDouble(showOriginal[5]) / 1024 / 1024, 2).ToString() + ">" + Math.Round((Video_estimate_capacity + Audio_capacity) / 1024 / 1024, 2).ToString() + " MB";
                }
                ffmpegcount = 1;
            }
            Encode();
        }

        //***********************************************************************************************************
        //Bitrate change
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].Checked)
                {
                    String[] listdata = data[i].Split(',');
                    if (numericUpDown1.Value > 0)
                    {
                        data[i] = numericUpDown1.Value.ToString() + "000," + listdata[1] + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                        String[] showOriginal = mediainfo[i].Split(',');
                        listView1.Items[i].SubItems[1].Text = Math.Round(Convert.ToDouble(showOriginal[0]) / 1000, 0) + ">" + data[i].Split(',')[0].Substring(0, data[i].Split(',')[0].ToString().Length - 3) + " kb/s";
                        double Audio_capacity = 0;
                        if (Path.GetExtension(@Infilename[i]).ToString() == ".txt")
                            Audio_capacity = 128000 * Convert.ToDouble(showOriginal[4]) / 8; //計算Audio大小
                        else
                            Audio_capacity = Convert.ToDouble(showOriginal[5]) - (Convert.ToDouble(showOriginal[0]) * Convert.ToDouble(showOriginal[4]) / 8); //計算Audio大小
                        double Video_estimate_capacity = Convert.ToDouble(numericUpDown1.Value.ToString() + "000") * Convert.ToDouble(listdata[4]) / 8; //Video預估大小
                        listView1.Items[i].SubItems[6].Text = Math.Round(Convert.ToDouble(showOriginal[5]) / 1024 / 1024, 2).ToString() + ">" + Math.Round((Video_estimate_capacity + Audio_capacity) / 1024 / 1024, 2).ToString() + " MB";
                    }
                }
                ffmpegcount = 1;
            }
            Encode();
        }

        //***********************************************************************************************************
        //FrameRate_Mode
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                String[] listdata = data[i].Split(',');
                if (listView1.Items[i].Checked)
                {
                    if (comboBox1.SelectedItem.ToString() == "Auto")
                    {
                        data[i] = listdata[0] + "," + databuff[i].Split(',')[1] + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                        if (comboBox2.SelectedItem.ToString() == "Auto" && comboBox3.SelectedItem.ToString() == "Auto" && comboBox5.SelectedItem.ToString() == "Auto")
                            data[i] = databuff[i];
                    }
                    else if (comboBox1.SelectedItem.ToString() == "CBR")
                        data[i] = listdata[0] + "," + "CBR" + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                    else if (comboBox1.SelectedItem.ToString() == "VBR")
                        data[i] = listdata[0] + "," + "VBR" + "," + listdata[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                }
                listView1.Items[i].SubItems[2].Text = mediainfo[i].Split(',')[1] + ">" + data[i].Split(',')[1];
                ffmpegcount = 1;
            }
            Encode();
        }

        //***********************************************************************************************************
        //FPS
        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                ComboboxItem item = comboBox5.Items[comboBox5.SelectedIndex] as ComboboxItem;
                String[] listdata = data[i].Split(',');
                if (listView1.Items[i].Checked)
                {
                    if (item.Value.ToString() == "Auto")
                        data[i] = listdata[0] + "," + listdata[1] + "," + databuff[i].Split(',')[2] + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                    else
                        data[i] = listdata[0] + "," + listdata[1] + "," + item.Value.ToString() + "," + listdata[3] + "," + listdata[4] + "," + listdata[5];
                    if (comboBox1.SelectedItem.ToString() == "Auto" && comboBox2.SelectedItem.ToString() == "Auto" && comboBox3.SelectedItem.ToString() == "Auto" && comboBox5.SelectedItem.ToString() == "Auto")
                        data[i] = databuff[i];
                }
                String[] showOriginal = mediainfo[i].Split(',');
                try
                {
                    listView1.Items[i].SubItems[3].Text = Math.Round(Convert.ToDouble(showOriginal[2].Split('/')[0]) / Convert.ToDouble(showOriginal[2].Split('/')[1]), 3) + ">" 
                        + Math.Round(Convert.ToDouble(data[i].Split(',')[2].Split('/')[0]) / Convert.ToDouble(data[i].Split(',')[2].Split('/')[1]), 3).ToString();
                }
                catch
                {
                    listView1.Items[i].SubItems[3].Text = data[i].Split(',')[2] + ">" + data[i].Split(',')[2];
                }
                ffmpegcount = 1;
            }
            Encode();
        }

        //***********************************************************************************************************
        //Resolution
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                String[] listdata = data[i].Split(',');
                if (listView1.Items[i].Checked)
                {
                    if (comboBox3.SelectedItem.ToString() == "Auto")
                    {
                        data[i] = listdata[0] + "," + listdata[1] + "," + listdata[2] + "," + databuff[i].Split(',')[3] + "," + listdata[4] + "," + listdata[5];
                        if (comboBox1.SelectedItem.ToString() == "Auto" && comboBox2.SelectedItem.ToString() == "Auto" && comboBox5.SelectedItem.ToString() == "Auto")
                            data[i] = databuff[i];
                    }
                    else if (comboBox3.SelectedItem.ToString() == "480p")
                        data[i] = listdata[0] + "," + listdata[1] + "," + listdata[2] + "," + "640x480" + "," + listdata[4] + "," + listdata[5];
                    else if (comboBox3.SelectedItem.ToString() == "720p")
                        data[i] = listdata[0] + "," + listdata[1] + "," + listdata[2] + "," + "1280x720" + "," + listdata[4] + "," + listdata[5];
                    else if (comboBox3.SelectedItem.ToString() == "1080p")
                        data[i] = listdata[0] + "," + listdata[1] + "," + listdata[2] + "," + "1920x1080" + "," + listdata[4] + "," + listdata[5];
                }
                listView1.Items[i].SubItems[4].Text = mediainfo[i].Split(',')[3] + ">" + data[i].Split(',')[3];
                ffmpegcount = 1;
            }
            Encode();
        }

        //***********************************************************************************************************
        //線程強制停止
        private void button5_Click(object sender, EventArgs e)
        {
            while (true)
            {
                if (Nico_t0 != null && Nico_t0.IsAlive) Nico_t0.Abort();
                if (Nico_t1 != null && Nico_t1.IsAlive) Nico_t1.Abort();
                if (Nico_t2 != null && Nico_t2.IsAlive) Nico_t2.Abort();
                if (Nico_mp4box != null && Nico_mp4box.IsAlive) Nico_mp4box.Abort();
                if (!Nico_t0.IsAlive)
                {
                    if (comboBox6.SelectedItem.ToString() == "FFmpeg")
                    foreach (Process p in Process.GetProcessesByName("ffmpeg")) p.Kill();
                    else if (comboBox6.SelectedItem.ToString() == "x264")
                    {
                        foreach (Process p in Process.GetProcessesByName("x264")) p.Kill();
                        foreach (Process p in Process.GetProcessesByName("avs4x26x")) p.Kill();
                        //foreach (Process p in Process.GetProcessesByName("mp4box")) p.Kill();
                    }
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button5.Enabled = false;
                    button8.Enabled = true;
                    button9.Enabled = true;
                    if (comboBox6.SelectedItem.ToString() == "FFmpeg")
                        comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                    comboBox6.Enabled = true;
                    numericUpDown1.Enabled = true;
                    File.Delete(@"./ffmpeg2pass-0.log");
                    File.Delete(@"./ffmpeg2pass-0.log.mbtree");
                    File.Delete(@"./ffmpeg2pass-0.log.temp");
                    File.Delete(@"./ffmpeg2pass-0.log.mbtree.temp");
                    File.Delete(@"./x2642pass.stats");
                    File.Delete(@"./x2642pass.stats.mbtree");
                    File.Delete(@"./avsbuff.avs");
                    File.Delete(@"./avsbuff.ass");
                    File.Delete(@"./avsbuff.264");
                    File.Delete(@"./avsbuff.aac");
                    for (int i = 0; i < listView1.Items.Count; i++)
                        File.Delete(Infilename[i].Replace("//", "\\") + @".ffindex");
                    ShowError("以強制停止");
                    break;
                }
            }
        }

        //***********************************************************************************************************
        //check debug window
        private void button6_Click(object sender, EventArgs e)
        {
            f2.Show();
        }

        //***********************************************************************************************************
        //檢查 mp4box 是否結束
        void process_Exited(object sender, EventArgs e)
        {
        }

        //***********************************************************************************************************
        private void Form1_Load(object sender, EventArgs e)
        {
            Form.CheckForIllegalCrossThreadCalls = false;
        }

        //***********************************************************************************************************
        //listView1 全選
        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                e.DrawBackground();
                CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(e.Bounds.Left + 4, e.Bounds.Top + 4),
                    new Rectangle(e.Bounds.X + 18, e.Bounds.Y + 4, e.Bounds.Width - 24, e.Bounds.Height - 4),
                    "檔案名稱", new Font("微軟正黑體", 9.0f, FontStyle.Regular), TextFormatFlags.Left, false,
                    Convert.ToBoolean(e.Header.Tag) ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
            }
            else
                e.DrawDefault = true;
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                bool value = Convert.ToBoolean(listView1.Columns[e.Column].Tag);
                listView1.Columns[e.Column].Tag = !value;
                foreach (ListViewItem item in listView1.Items)
                    item.Checked = !value;
                listView1.Invalidate();
            }
        }

        //***********************************************************************************************************
        //清空
        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listView1.Items.Clear();
            f2.clear2();
            count = 0;
            button2.Enabled = false;
            button5.Enabled = false;
            textBox1.Text = "0/0";
        }

        //***********************************************************************************************************
        //移除檔案
        private void button8_Click(object sender, EventArgs e)
        {
            for (int i = listBox1.SelectedItems.Count - 1; i >= 0; i--)
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[i]);
            count = 0;
            listView1.Items.Clear();
            ffmpegcount = 0; 
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;
            Encode();
            textBox1.Text = "0/" + listBox1.Items.Count.ToString();
        }

        //***********************************************************************************************************
        //限制關閉程式
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Nico_t0 != null && Nico_t0.IsAlive && checkBox1.Checked.ToString() == "False")
            {
                ShowError("轉檔中，請先停止後再關閉程式");
                e.Cancel = true;
            }
            else
                e.Cancel = false;
        }

        //***********************************************************************************************************
        //Set combox5
        private class ComboboxItem
        {
            public ComboboxItem(string text, string value) { Text = text; Value = value; }
            public string Value { get; set; }
            public string Text { get; set; }
            public override string ToString() { return Text; }
        }

        //***********************************************************************************************************
        //回復預設值
        private void button9_Click(object sender, EventArgs e)
        {
            count = 0;
            listView1.Items.Clear();
            ffmpegcount = 0;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0;
            Encode();
            textBox1.Text = "0/" + listBox1.Items.Count.ToString();
        }

        //***********************************************************************************************************
        //CPU
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.Items.Count != 0)
                Encode();
        }

        //***********************************************************************************************************
        //選擇編譯程式
        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.Items.Count != 0)
                Encode();
            if (comboBox6.SelectedItem.ToString() == "x264")
                comboBox1.Enabled = false;
            else
                comboBox1.Enabled = true;
        }
    }
}