using NAudio.Wave;
using SoxSharp;
using SoxSharp.Effects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FakeYou_Wrapper.FakeYouCSharp;

namespace FakeYou_Wrapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public List<VoiceModel> availableVoices = new List<VoiceModel>();
        public List<VoiceModel> CurrentList = new List<VoiceModel>();
        byte[] currentTTSDataWav = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            availableVoices = FakeYouCSharp.GetListOfVoices();
            CurrentList = new List<VoiceModel>();
            foreach (var item in availableVoices)
            {
                CurrentList.Add(item);
            }

            foreach (var item in CurrentList)
            {
                listBox1.Items.Add(item.title + " : " + item.creator_display_name);
            }
        }


        public class HolderTemp
        {
            public int index = 0;
            public byte[] data;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            string inputtext = textBox2.Text;

            string path = "fileDONE.wav";

            Generator generator = new Generator();
            generator.GenerateInput(inputtext, path, CurrentList[listBox1.SelectedIndex].model_token, (int)numericUpDownJoinLength.Value);

            System.Media.SoundPlayer player = new System.Media.SoundPlayer(Application.StartupPath + "\\" + path);
            player.Play();

        }

        public static void Concatenate(List<byte[]> sourceByteList, string outputFile)
        {
            byte[] buffer = new byte[1024];
            WaveFileWriter waveFileWriter = null;
            try
            {
                foreach (byte[] sourceFile in sourceByteList)
                {
                    using (var WavStream = new MemoryStream(sourceFile))
                    {
                        using (var reader = new WaveFileReader(WavStream))
                        {
                            if (waveFileWriter == null)
                            {
                                // first time in create new Writer
                                waveFileWriter = new WaveFileWriter(outputFile, reader.WaveFormat);
                            }
                            else
                            {
                                if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
                                {
                                    throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
                                }
                            }
                            int read;
                            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                waveFileWriter.WriteData(buffer, 0, read);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (waveFileWriter != null)
                {
                    waveFileWriter.Dispose();
                }
            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            CurrentList = new List<VoiceModel>();
            listBox1.Items.Clear();
            CurrentList = availableVoices.Where(x => x.title.ToUpper().Contains(textBox1.Text.ToUpper())).ToList();
            foreach (var item in CurrentList)
            {
                listBox1.Items.Add(item.title + " : " + item.creator_display_name);
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\file.wav"))
            {
                SoundPlayer player = new SoundPlayer(Application.StartupPath + "\\File.wav");
                player.PlaySync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Stopwatch stpw = new Stopwatch();
                stpw.Start();

                if (listBox1.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a voice!");
                    return;
                }
                if (textBox2.Text.Length < 1)
                {
                    MessageBox.Show("Please include text to speak!");
                    return;
                }

                labelStatus.Text = "";
                buttonPlay.Enabled = false;
                Application.DoEvents();
                var Req = FakeYouCSharp.MakeTTSRequest(CurrentList[listBox1.SelectedIndex].model_token, textBox2.Text);
                if (Req != "Failed")
                {

                    var poll = PollTTSRequestStatus(Req);
                    DateTime LastCheck = DateTime.Now;
                    labelStatus.Text = poll.status;
                    Application.DoEvents();
                    while (true)
                    {
                        DateTime CurrCheck = DateTime.Now;
                        if (CurrCheck.AddMilliseconds(-100) > LastCheck)
                        {
                            poll = PollTTSRequestStatus(Req);
                            labelStatus.Text = poll.status;
                            Application.DoEvents();
                            if (poll.status == "complete_success" | poll.status == "complete_failure" | poll.status == "attempt_failed" | poll.status == "dead")
                            {

                                break;
                            }
                            LastCheck = DateTime.Now;

                        }
                    }
                    stpw.Stop();
                    Console.WriteLine(stpw.Elapsed);
                    switch (poll.status)
                    {
                        case "complete_success":
                            var bytes = FakeYouCSharp.StreamTTSAudioClip(FakeYouCSharp.m_cdn + poll.maybe_public_bucket_wav_audio_path);
                            currentTTSDataWav = bytes;
                            buttonPlay.Enabled = true;
                            break;
                    }

                }
            });
        }

        public static (double[] audio, int sampleRate) ReadWav(string filePath)
        {
            var afr = new NAudio.Wave.AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample / 8);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => (double)x));



            return (audio.ToArray(), sampleRate);
        }


        public static (double[] audio, int sampleRate) ReadWav(byte[] bytes)
        {
            File.WriteAllBytes("tempwav.wav", bytes);

            var afr = new NAudio.Wave.AudioFileReader("tempwav.wav");
            int sampleRate = afr.WaveFormat.SampleRate;
            int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample / 8);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => (double)x));


            afr.Close();
            return (audio.ToArray(), sampleRate);
        }



        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\fileDONE.wav"))
            {
                SoundPlayer player = new SoundPlayer(Application.StartupPath + "\\FileDONE.wav");
                player.PlaySync();
            }
        }

        private void checkBoxRemStatic_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}
