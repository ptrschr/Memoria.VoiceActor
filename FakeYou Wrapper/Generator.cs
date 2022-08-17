using SoxSharp.Effects;
using SoxSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FakeYou_Wrapper.Form1;
using System.Windows.Forms;
using System.IO;

namespace FakeYou_Wrapper
{
    public class Generator
    {
        public string GenerateInput(string inputtext, string filePath, string model_token, int audioSplitLengthCharacters)
        {
            List<string> inputparsed = new List<string>();
            while (inputtext.Length > audioSplitLengthCharacters)
            {
                string currentTest = inputtext.Remove(0, (int)audioSplitLengthCharacters);
                if (currentTest.Contains(" "))
                {
                    int ind = currentTest.IndexOf(" ");
                    string currentM = inputtext.Substring(0, (int)audioSplitLengthCharacters + ind);
                    inputtext = inputtext.Remove(0, (int)audioSplitLengthCharacters + ind);
                    inputparsed.Add(currentM);
                }
            }
            inputparsed.Add(inputtext);

            List<byte[]> WavDataList = new List<byte[]>();
            var primeNumbers = new ConcurrentBag<HolderTemp>();
            object lockCurrent = new object();
            Console.WriteLine("INPUT COUNT: " + inputparsed.Count.ToString());
            List<Task> Tasks = new List<Task>();

            for (int index = 0; index < inputparsed.Count; index++)
            {
                // Console.WriteLine(index.ToString() + " " + "INIT 1");
                string modeltok = "";

                //this.Invoke((MethodInvoker)delegate
                //{
                modeltok = model_token;
                //});
                // Console.WriteLine(index.ToString() + " " + "INIT 2");
                string textM = inputparsed[index];
                var ind = index;
                Tasks.Add(Task.Run(() =>
                {
                    //Console.WriteLine(ind.ToString() + " " + "INIT 1");
                    var ReqM = FakeYouCSharp.MakeTTSRequest(modeltok, textM);
                    //Console.WriteLine(ind.ToString() + " " + "INIT 3");
                    if (ReqM != "Failed")
                    {
                        //Console.WriteLine(ind.ToString() + " " + "INIT 4");
                        var poll = FakeYouCSharp.PollTTSRequestStatus(ReqM);
                        while (poll.status == "started" | poll.status == "pending")
                        {
                            Thread.Sleep(100);
                            poll = FakeYouCSharp.PollTTSRequestStatus(ReqM);
                            //Console.WriteLine(ind.ToString() + " " + "INIT 5");
                        }
                        var bytes = FakeYouCSharp.StreamTTSAudioClip(FakeYouCSharp.m_cdn + poll.maybe_public_bucket_wav_audio_path, false);
                        HolderTemp hhh = new HolderTemp();
                        hhh.index = ind;
                        hhh.data = bytes;
                        primeNumbers.Add(hhh);
                        //Console.WriteLine(ind.ToString() + " " + "INIT 6");
                    }
                }));
            }
            Task.WaitAll(Tasks.ToArray());

            var tempL = primeNumbers.ToList();
            tempL = tempL.OrderBy(x => x.index).ToList();

            List<byte[]> outb = new List<byte[]>();
            foreach (var item in tempL)
            {
                outb.Add(item.data);
            }
            (double[] audio, int sampleRate) = ReadWav(outb[0]);//("fileSilence.wav");
            List<double[]> posPotentail = new List<double[]>();

            double threshDef = 0.008;
            double thresh = 0.008;
            int currentstart = 0;
            int currentend = 0;
            int currentcount = 0;
            for (int i = 0; i < audio.Length; i++)
            {
                if (audio[i] < thresh)
                {
                    if (currentstart == 0)
                    {
                        currentstart = i;
                        currentend = 0;
                        currentcount = 0;
                        thresh = threshDef * 8;
                    }
                    else
                    {
                        currentcount++;
                    }
                }
                else if (audio[i] > -thresh && audio[i] <= 0)
                {
                    if (currentstart == 0)
                    {
                        currentstart = i;
                        currentend = 0;
                        currentcount = 0;
                        thresh = threshDef * 8;
                    }
                    else
                    {
                        currentcount++;
                    }
                }
                else
                {
                    if (currentstart > 0)
                    {
                        currentend = i;
                        double[] ent = new double[3] { currentstart, currentend, currentcount };
                        currentcount = 0;
                        currentend = 0;
                        currentstart = 0;
                        thresh = threshDef;
                        posPotentail.Add(ent);
                    }
                }
            }


            double longest = 0;
            double find = 0;
            double[] outt = new double[3];
            for (int i = 0; i < posPotentail.Count; i++)
            {
                if (posPotentail[i][2] > longest)
                {
                    longest = posPotentail[i][2];
                    find = i;
                    outt = posPotentail[i];
                }
            }
            outt[0] = (outt[0] / 32000);
            outt[1] = (outt[1] / 32000);
            outt[2] = (outt[2] / 32000);

            //REMOVE NOISE

            using (var sox = new Sox("sox.exe"))
            {
                sox.Output.Type = FileType.WAV;
                var tsS = TimeSpan.FromSeconds(outt[0]);
                var tsE = TimeSpan.FromSeconds(outt[2]);

                var posstart = new SoxSharp.Effects.Types.Position(tsS);
                var posend = new SoxSharp.Effects.Types.Position(tsE);
                sox.Effects.Add(new TrimEffect(posstart, posend));


                sox.Process("tempwav.wav", "fileSilenceCut.wav");
            }
            File.Delete("tempwav.wav");
            using (var sox = new Sox("sox.exe"))
            {
                sox.Output.Type = FileType.WAV;
                var posstart = new SoxSharp.Effects.Types.Position((uint)outt[0]);
                var posend = new SoxSharp.Effects.Types.Position((uint)outt[1]);
                sox.Effects.Add(new NoiseProfileEffect("noiseprof.prof"));
                //sox.Effects.Add(new NoiseReductionEffect("fileSilenceCut.wav")) ;
                sox.Process("fileSilenceCut.wav");
            }

            Concatenate(outb, Application.StartupPath + "\\file.wav");
            using (var sox = new Sox("sox.exe"))
            {
                sox.Output.Type = FileType.WAV;
                sox.Effects.Add(new NoiseReductionEffect("noiseprof.prof", 0.05));
                sox.Process("file.wav", filePath);
            }
            return filePath;
        }
    }
}
