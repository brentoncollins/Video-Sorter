﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.IO;
using System.Threading;
using System.Globalization;
using UITimer = System.Windows.Forms.Timer;
using System.ComponentModel;

namespace Torrent_Sorter
{


    public partial class Form1 : Form
    {
        
        DataTable dt = new DataTable("Files");

        DataSet dataSet = new DataSet();
        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
        

        public Form1()
        {
            InitializeComponent();

            // Get the origianl dirs from settings.
            textBox_download_dir.Text = Properties.Settings.Default.download_dir;
            textBox_tv_dir.Text = Properties.Settings.Default.tv_dir;
            textBox_movie_dir.Text = Properties.Settings.Default.movie_dir;
        }

        public void load_data()
        {
            dataGridView1.Rows.Clear();
            string file = "data.bin";
            using (BinaryReader bw = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                
                int n = bw.ReadInt32();
                int m = bw.ReadInt32();
                for (int i = 0; i < m; ++i)
                {
                    dataGridView1.Rows.Add();
                    for (int j = 0; j < n; ++j)
                    {
                        if (bw.ReadBoolean())
                        {
                            dataGridView1.Rows[i].Cells[j].Value = bw.ReadString();
                        }
                        else bw.ReadBoolean();
                    }
                }
            }
        }
        

        public void save_data()
        {
            string file = "data.bin";

            using (BinaryWriter bw = new BinaryWriter(File.Open(file, FileMode.Create)))
            {
                bw.Write(dataGridView1.Columns.Count);
                bw.Write(dataGridView1.Rows.Count);
                foreach (DataGridViewRow dgvR in dataGridView1.Rows)
                {
                    for (int j = 0; j < dataGridView1.Columns.Count; ++j)
                    {
                        object val = dgvR.Cells[j].Value;
                        if (val == null)
                        {
                            bw.Write(false);
                            bw.Write(false);
                        }
                        else
                        {
                            bw.Write(true);
                            bw.Write(val.ToString());
                        }
                    }
                }
            }
        }
        private async void MoveFile(string sourceFile, string destinationFile)
        {
            try
            {
                using (FileStream sourceStream = File.Open(sourceFile, FileMode.Open))
                {
                    using (FileStream destinationStream = File.Create(destinationFile))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
               
                            sourceStream.Close();
                            File.Delete(sourceFile);
                        
                    }
                }
            }
            catch (IOException ioex)
            {
                Console.WriteLine("An IOException occured during move, " + ioex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An Exception occured during move, " + ex.Message);
            }
        }

        public void Regex(object sender, DoWorkEventArgs e)
        {
            // Sleep for one second to not over work the system
            System.Threading.Thread.Sleep(1000);
            // Get the argument from e
            FileInfo fileInfo = (FileInfo) e.Argument;
            // Method for Title text
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

            // Run PTN and convert into a dict to work with.
            var video_dict = new Dictionary<string, string>();
            ScriptEngine engine = Python.CreateEngine();
            string RunningPath = AppDomain.CurrentDomain.BaseDirectory;
            ScriptSource source = engine.CreateScriptSourceFromFile("parse.py");
            ScriptScope scope = engine.CreateScope();
            source.Execute(scope);

            dynamic python_file = scope.GetVariable("PTN");
            dynamic PTN = python_file();

            var list = PTN.parse(fileInfo.Name);

            // Loop threw list and invoke method on first go.
            foreach (var kvp in list)
                {
                    video_dict.Add(kvp.ToString(), list[kvp].ToString().TrimEnd('.'));
                }

            // Defone string to have safe threads
            string destinationFile;
            string sourceFile = fileInfo.FullName;
            
            // Testing this to see if failed PTN with no title will skip.
            if (video_dict.ContainsKey("title") == false)
                {
                    return;
                }
            // If the parse filename does not contail the key season is must be a movie.
            if (video_dict.ContainsKey("season") == false)
                {
                    // If the key year exists include this.
                    if (video_dict.ContainsKey("year"))
                        {
                                destinationFile = String.Format
                                ("{0}\\{1}\\{2} - {3}{4}{5}",
                                Properties.Settings.Default.movie_dir,
                                myTI.ToTitleCase(video_dict["title"]),
                                myTI.ToTitleCase(video_dict["title"]),
                                video_dict["year"],
                                ".",
                                video_dict["container"]);
                        }
                    // else dont bother with the year. 
                    else
                        {
                                    destinationFile = String.Format
                                ("{0}\\{1}\\{2}{3}{4}",
                                Properties.Settings.Default.movie_dir,
                                myTI.ToTitleCase(video_dict["title"]),
                                myTI.ToTitleCase(video_dict["title"]),
                                ".",
                                video_dict["container"]);
                        }

                    // Create directory for movie
                    Directory.CreateDirectory(String.Format("{0}\\{1}",
                    Properties.Settings.Default.movie_dir,
                    myTI.ToTitleCase(video_dict["title"])));



                    if (File.Exists(destinationFile) == true)
                    {
                            try
                            {
                                File.Delete(sourceFile);
                            }
                            catch (System.IO.DirectoryNotFoundException)
                            {
                                return;
                            }
                            catch (System.IO.IOException)
                            {
                                return;
                            }

                            BeginInvoke((MethodInvoker)delegate
                            {
                                //dataGridView1.Update();
                                //dataGridView1.Refresh();
                                dataGridView1.Rows.Insert
                                (0, new string[] { DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"),
                                myTI.ToTitleCase(video_dict["title"]),
                                "File Already Exists, File Removed" });
                            });

                                // add_data(DateTime.Now.ToString("h:mm:ss tt"),
                                //myTI.ToTitleCase(video_dict["title"]),
                                //"File Already Exists, File Removed");
                            }

                            else
                            {
                                try
                                {
                                       MoveFile(sourceFile, destinationFile);
                             
                                }
                            catch(System.IO.IOException)
                            {
                                return;
                            }       
                            BeginInvoke((MethodInvoker)delegate
                            {
                                //dataGridView1.Update();
                                //dataGridView1.Refresh();
                                dataGridView1.Rows.Insert
                                (0, new string[] {DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"),
                                myTI.ToTitleCase(video_dict["title"]),
                                destinationFile });
                            });

                                //add_data(DateTime.Now.ToString("h:mm:ss tt"),
                                //myTI.ToTitleCase(video_dict["title"]),
                                //destinationFile);
                                
                            }
                     }

                    // If the result has an episode it is a TV show.
                    if (video_dict.ContainsKey("episode") == true)
                    {
                         
                        Directory.CreateDirectory
                        (String.Format("{0}\\{1}",
                        Properties.Settings.Default.tv_dir,
                        myTI.ToTitleCase(video_dict["title"])));

                        Directory.CreateDirectory
                        (String.Format("{0}\\{1}\\Season {2}",
                        Properties.Settings.Default.tv_dir,
                        myTI.ToTitleCase(video_dict["title"]),
                        video_dict["season"]));

                        string seasonFolderNumber = video_dict["season"];

                        if (video_dict["episode"].Count() < 2)
                        {
                            video_dict["episode"] = string.Format("{0}{1}", 0, video_dict["episode"]);
                        }
                        if (video_dict["season"].Count() < 2)
                        {
                            video_dict["season"] = string.Format("{0}{1}", 0, video_dict["season"]);
                        }
                       


                    
                        destinationFile = String.Format
                        ("{0}\\{1}\\Season {2}\\{3} S{4}E{5}{6}{7}",
                        Properties.Settings.Default.tv_dir,
                        myTI.ToTitleCase(video_dict["title"]),
                        seasonFolderNumber,
                        myTI.ToTitleCase(video_dict["title"]),
                        video_dict["season"],
                        myTI.ToTitleCase(video_dict["episode"]),
                        ".", video_dict["container"]);


                        

                        if (File.Exists(destinationFile) == true)
                        {

                            try
                            {
                                File.Delete(sourceFile);
                            }
                            catch (System.IO.DirectoryNotFoundException)
                            {
                                return;
                            }

                            catch (System.IO.IOException)
                            {
                                return;
                            }

                            BeginInvoke((MethodInvoker)delegate
                            {
                                //dataGridView1.Update();
                                //dataGridView1.Refresh();
                                dataGridView1.Rows.Insert
                                (0, new string[] { DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"),
                                String.Format("{0} S{1}E{2}",
                                myTI.ToTitleCase(video_dict["title"]),
                                video_dict["season"],
                                video_dict["episode"]),
                                "File already exists, file removed" });

                            });


                                //add_data(DateTime.Now.ToString("h:mm:ss tt"),
                                //String.Format("{0} S{1}E{2}",
                                //myTI.ToTitleCase(video_dict["title"]),
                                //video_dict["season"],
                                //video_dict["episode"]),
                                //"File Already Exists, File Removed");
                    }
                    else
                    {   
                        try{
                            MoveFile(sourceFile, destinationFile);
                         
                    }
                            catch(System.IO.IOException)
                            {
                                return;
                            }
                            Console.WriteLine("Moved File");
                            BeginInvoke((MethodInvoker)delegate
                            {
                                //dataGridView1.Update();
                                //dataGridView1.Refresh();
                                dataGridView1.Rows.Insert
                               (0, new string[] { DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"),
                                   String.Format("{0} S{1}E{2}",
                                   myTI.ToTitleCase(video_dict["title"]),
                                   video_dict["season"],
                                   video_dict["episode"]),
                                   destinationFile });
                            });


                                //add_data(DateTime.Now.ToString("h:mm:ss tt"), String.Format("{0} S{1}E{2}",
                                //myTI.ToTitleCase(video_dict["title"]),
                                //video_dict["season"],
                                //video_dict["episode"]),
                                //destinationFile);

                }

            }
        }

        private static void processDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                Console.WriteLine(directory);
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public void add_data(string time, string title, string destination)
        {
            bool tryAgain = true;
            DataRow newRow = dt.NewRow();
            newRow["Time"] = time;
            newRow["Title"] = title;
            newRow["Destination"] = destination;
            dt.Rows.Add(newRow);
            dataSet.Tables["Files"].AcceptChanges();
            while (tryAgain)
            {

                
                FileInfo fi1 = new FileInfo(@"MyData.xml");
                bool fileLocked = IsFileLocked(fi1);
                if (fileLocked == true)
                {
                    Thread.Sleep(100);

                }
                else
                {
                    try
                    {
                        
                        dataSet.WriteXml(@"MyData.xml");
                    }
                    catch (System.IO.IOException)
                    {
                        tryAgain = false;
                    }
                    tryAgain = false;
                }
                
            }

        }
        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        public void SearchFiles(object sender, EventArgs e)
        {
            // Delete empty dirs
            processDirectory(Properties.Settings.Default.download_dir);

            // Get all files in all dirs.
            DirectoryInfo d = new DirectoryInfo(Properties.Settings.Default.download_dir);
            FileInfo[] Files = d.GetFiles("*.*", SearchOption.AllDirectories);
                
            foreach (FileInfo file in Files)
            {   
                //Get file size and delete if too small
                try
                    {
                    long length = new FileInfo(file.FullName).Length;
                    if (length < 1000000000)
                        {
                            try
                                {
                                    File.Delete(file.FullName);
                                    continue;
                                }
                            catch(System.IO.IOException)
                                {
                                    return;
                                }
                        }
                    // Create new background worker
                    var worker = new BackgroundWorker();
                    worker.DoWork += new DoWorkEventHandler(Regex);
                    worker.RunWorkerAsync(argument: file);
                    }

                catch (FileNotFoundException)
                    {
                        break;
                    }

            }


            
        }

        private void button_download_dir(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox_download_dir.Text = fbd.SelectedPath.ToString();
                    Properties.Settings.Default.download_dir = fbd.SelectedPath.ToString();
                    Properties.Settings.Default.Save();

                }
            }
        }

        private void button_tv_dir(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox_tv_dir.Text = fbd.SelectedPath.ToString();
                    Properties.Settings.Default.tv_dir = fbd.SelectedPath.ToString();
                    Properties.Settings.Default.Save();

                }
            }
        }

        private void button_movie_dir(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox_movie_dir.Text = fbd.SelectedPath.ToString();
                    Properties.Settings.Default.movie_dir = fbd.SelectedPath.ToString();
                    Properties.Settings.Default.Save();

                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //try
            //{
            //    dataSet.ReadXml(@"MyData.xml");
            //}
            //catch (System.IO.FileNotFoundException)
            //{
           
            //}
           

            //dt.Clear();
            //dt.Columns.Add("Time");
            //dt.Columns.Add("Title");
            //dt.Columns.Add("Destination");
            //try
            //{
            //    dataSet.Tables.Add(dt);
            //}
            //catch (System.Data.DuplicateNameException)
            //{

            //}
            //dataGridView1.DataSource = dataSet.Tables["Files"];


            myTimer.Tick += new EventHandler(SearchFiles);

            // Sets the timer interval to 15 seconds.
            myTimer.Interval = 15000;
            
            // Check if all setting are entered
            bool setting_missing;
            setting_missing = false;

            if (string.IsNullOrEmpty(Properties.Settings.Default.download_dir))
            {
                setting_missing = true;
            }
            if (string.IsNullOrEmpty(Properties.Settings.Default.tv_dir))
            {
                setting_missing = true;
            }
            if (string.IsNullOrEmpty(Properties.Settings.Default.movie_dir))
            {
                setting_missing = true;
            }

            // If all settings are entered start the timer.
            if(setting_missing == false)
                {
                    myTimer.Start();
                }
        }

        private void button_start(object sender, EventArgs e)
        {
            // Start timer in new thread.
            myTimer.Start();
        }

    }
    
}
