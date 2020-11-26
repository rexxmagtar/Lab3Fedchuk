using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

namespace Fedchuk3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private List<byte[]> DivideFile(string file)
        {
            List<byte[]> parts = new List<byte[]>();

            long fileSize = new FileInfo(file).Length;

            long partSize = (long)Math.Ceiling(1.0 * fileSize / 128);

            long partCount = fileSize / partSize;

            outputTextBox.Text = "";

            outputTextBox.Text += "part size: " + partSize + Environment.NewLine;

            outputTextBox.Text += "part count: " + partCount + Environment.NewLine;

            using (BufferedStream reader = new BufferedStream(new FileStream(file, FileMode.OpenOrCreate)))
            {
                for (int i = 0; i < partCount - 1; i++)
                {
                    byte[] readBytes = new byte[partSize];

                    reader.Read(readBytes, 0, (int)partSize);
                    parts.Add(readBytes);

                }

                if (fileSize % partSize == 0)
                {
                    byte[] readBytes = new byte[partSize];

                    reader.Read(readBytes, 0, (int)partSize);
                    parts.Add(readBytes);
                }
                else
                {
                    long lastPartSize = (fileSize - partSize * (partCount - 1));

                    byte[] readBytes = new byte[lastPartSize];

                    reader.Read(readBytes, 0, (int)lastPartSize);
                    parts.Add(readBytes);
                }
            }

            return parts;
        }

        private static byte[] MD5(byte[] input)
        {
            return System.Security.Cryptography.MD5.Create().ComputeHash(input);
        }

        private async void ConfirmButton_Click(object sender, EventArgs e)
        {
            //var result = DivideFile(InputFileText.Text);

            //for (int i = 0; i < result.Count; i++)
            //{
            //    outputTextBox.Text += Encoding.UTF8.GetString(MD5(result[i])) + Environment.NewLine;
            //}

            Stopwatch stopwatchAllTime = new Stopwatch();

            Stopwatch stopwatchHashTime = new Stopwatch();

            stopwatchAllTime.Start();

            var buffer = new BufferBlock<byte[]>();

            long timeRead = 0;

            Produce(buffer, InputFileText.Text, ref timeRead);

            stopwatchHashTime.Start();

            var result = await ConsumeAsync(buffer);

            stopwatchHashTime.Stop();

            stopwatchAllTime.Stop();

            long fileSize = 0;

            try
            {
                fileSize = new FileInfo(InputFileText.Text).Length;
            }
            catch (Exception ex)
            {
                outputTextBox.Text += ex.Message;

                return;
            }

            outputTextBox.Text += $"File size: {fileSize} bytes" +  Environment.NewLine;

            try
            {
                outputTextBox.Text += "Time taken for all: " + stopwatchAllTime.ElapsedMilliseconds + " ms" + ". Speed:  " + (fileSize / stopwatchAllTime.ElapsedMilliseconds) + " byte/ms" + Environment.NewLine;
                outputTextBox.Text += "Time taken for hash: " + stopwatchHashTime.ElapsedMilliseconds + " ms" + ". Speed:  " + (fileSize / stopwatchHashTime.ElapsedMilliseconds) + " byte/ms" + Environment.NewLine;
                outputTextBox.Text += "Time taken for read: " + timeRead + " ms" + ". Speed:  " + (fileSize / timeRead) + " byte/ms" + Environment.NewLine;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            for (int i = 0; i < result.Count; i++)
            {
                outputTextBox.Text += $"{i}: " + GetHashFromBytes((result[i])) + Environment.NewLine;
            }

        }

        private static string GetHashFromBytes(byte[] bytes)
        {
            string result = "";

            foreach (var b in bytes)
            {
                result += b.ToString("x2");
            }

            return result;
        }


        void Produce(ITargetBlock<byte[]> target, string file, ref long timeTaken)
        {

            Stopwatch stopwatchReadTime = new Stopwatch();

            stopwatchReadTime.Start();

            List<byte[]> parts = new List<byte[]>();

            long fileSize = 0;

            try
            {
                fileSize = new FileInfo(file).Length;
            }
            catch (Exception e)
            {
                outputTextBox.Text = e.Message;

                return;
            }

            long partSize = (long)Math.Ceiling(1.0 * fileSize / 128);

            long partCount = fileSize / partSize;

            outputTextBox.Text = "";

            using (BufferedStream reader = new BufferedStream(new FileStream(file, FileMode.OpenOrCreate)))
            {
                for (int i = 0; i < partCount - 1; i++)
                {
                    byte[] readBytes = new byte[partSize];

                    reader.Read(readBytes, 0, (int)partSize);
                    target.Post(readBytes);

                }

                if (fileSize % partSize == 0)
                {
                    byte[] readBytes = new byte[partSize];

                    reader.Read(readBytes, 0, (int)partSize);
                    target.Post(readBytes);
                }
                else
                {
                    long lastPartSize = (fileSize - partSize * (partCount - 1));

                    byte[] readBytes = new byte[lastPartSize];

                    reader.Read(readBytes, 0, (int)lastPartSize);
                    target.Post(readBytes);
                }
            }

            target.Complete();

            stopwatchReadTime.Stop();

            timeTaken = stopwatchReadTime.ElapsedMilliseconds;
        }

        static async Task<List<byte[]>> ConsumeAsync(ISourceBlock<byte[]> source)
        {
            List<byte[]> result = new List<byte[]>();

            while (await source.OutputAvailableAsync())
            {
                byte[] data = await source.ReceiveAsync();

                result.Add(MD5(data));
            }

            return result;
        }
    }
}