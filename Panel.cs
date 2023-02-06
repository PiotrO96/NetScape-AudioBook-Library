using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MySql.Data.MySqlClient;
using MySql.Data;
using NAudio.Wave;
using System.IO.Compression;
using FluentFTP;
using FluentFTP.Proxy;
using System.Threading;
using System.Speech.Synthesis;

namespace NetScape_AudioBook_Library
{
    public partial class Panel : Form
    {   
        //user information//
        int ID = 0;
        string login = null;

        //playlist//
        List<string> chapter_List = new List<string>();
        List<string> chapter_playlist = new List<string>();
        int current_chapter = 0;

        //mysql connection//
        string connectionString = null;
        MySqlConnection connection;

        //audio//
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        //queue//
        private int stop_cause = 0;

        //time archive//
        private System.Windows.Forms.Timer timer;
        private int played_seconds = 0; //track current countdown//
        private int left_seconds = 0; //track left countdown//
        private int total_seconds = 0; // total track time//

        //creation//
        private string category_selection = null;

        //speech//
        string[] ebook_sentences = null;
        SpeechSynthesizer synth = new SpeechSynthesizer();
        PromptBuilder sentence = new PromptBuilder();
        int sentence_index = 0;

        //threading//

        public Panel()
        {
            Load += new EventHandler(Panel_Load);
        }

        private static Panel _mf;

        public Panel(int ID)
        {
            InitializeComponent();
            this.ID = ID;
        }

        private void Panel_Load(object sender, EventArgs e)
        {
            synth.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(synth_SpeakCompleted);
            connectionString = "SERVER=mfixit.cba.pl;USERNAME=mfixit;PASSWORD=Qwertymyfixit1;DATABASE=mfixit;SslMode=none;Convert Zero Datetime = true";
            connection = new MySqlConnection(connectionString);
            firstLogin();
            listCategories();
            blank_create();
            _mf = this;
        }

        private void firstLogin()
        {
            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                MySqlCommand cmd = new MySqlCommand("SELECT Login FROM Users WHERE ID=@ID;)", connection);
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ID", ID);

                MySqlDataReader rdr = null;

                rdr = cmd.ExecuteReader();
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        login = rdr[0].ToString();
                    }
                }
                DirectoryInfo audiobooks = Directory.CreateDirectory(login + "\\Audiobooks");
                DirectoryInfo ebooks = Directory.CreateDirectory(login + "\\Ebooks");


                FtpClient client = new FtpClient("www.mkwk019.cba.pl");
                client.Credentials = new System.Net.NetworkCredential("mfixit", "Qwertymyfixit12");
                client.Connect();

                client.SetWorkingDirectory(@"/mfixit.cba.pl/Files");
                if (!client.DirectoryExists(login))
                {
                    client.CreateDirectory(login);
                }
                client.Disconnect();
            }
            catch
            {
                MessageBox.Show("Something went wrong.");
                throw;
            }
        }
        
        private void listCategories()
        {
            libraryList.Clear();
            List<string> categoriesPath = new List<string>(Directory.GetDirectories(login+"\\Audiobooks"));
            ListViewGroup group = null;
            foreach (var categoryPath in categoriesPath)
            {
                var name = new DirectoryInfo(categoryPath).Name;
                group = new ListViewGroup(name);
                libraryList.Groups.Add(group); //categories as groups
                listAudiobooks(categoryPath, group); //get books in category folder
            }
            
        }

        private void listAudiobooks(string path, ListViewGroup group)
        {
            List<string> audiobooksPath = new List<string>();
            List<string> audiobooks = new List<string>();
            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems;
            audiobooksPath.AddRange(Directory.GetDirectories(path));
            foreach (var bookPath in audiobooksPath)
            {
                var name = new DirectoryInfo(bookPath).Name;
                var fullpath = new DirectoryInfo(bookPath).FullName;
                audiobooks.Add(name);
                var book = audiobooks.Last();
                item = new ListViewItem(book);
                subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, fullpath)};
                item.SubItems.AddRange(subItems);
                item.Group = group;
                item.ImageIndex = libraryList.Items.Count;
                libraryList.Items.Add(item);
                var index = libraryList.Items.Count-1;
                try
                {
                    var cover = Directory.GetFiles(bookPath, "*.jpg").First();

                    var image = Image.FromFile(cover);

                    Image imgThumb = image.GetThumbnailImage(100, 100, null, new IntPtr());

                    imageList1.Images.Add(index.ToString(), imgThumb);

                    image.Dispose();
                }
                catch
                {
                    throw;
                }
            }
        }

        private void libraryList_DoubleClick(object sender, EventArgs e)
        {
            chapterList.Clear();
            chapter_playlist.Clear();
            int number = 1;
            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems;
            var path = libraryList.SelectedItems[0].SubItems[1].Text;
            var chapters = Directory.GetFiles(path, "*.mp3");
            foreach(var chapter in chapters)
            {
                item = new ListViewItem("Chapter " + number);
                subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, chapter)};
                number++;
                item.SubItems.AddRange(subItems);
                chapterList.Items.Add(item);
                chapter_playlist.Add(chapter);
            }
        }

        private void chapterList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                stop_cause = 1;
                stop_chapter();
                current_chapter = chapterList.SelectedItems[0].Index;
                var child = new DirectoryInfo(chapter_playlist[current_chapter]);
                var parent = child.Parent.FullName;

                var cover = Directory.GetFiles(parent, "*.jpg").First();
                pictureBox1.Image = Image.FromFile(cover);
                play_chapter();
            }
            catch
            {

            }
        }

        public void InitTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 1000; // in miliseconds
            timer.Start();
        }

        //countdown timer//
        private void timer_Tick(object sender, EventArgs e)
        {
            total_seconds = (int)audioFile.TotalTime.TotalSeconds;
            TimeSpan t_s = TimeSpan.FromSeconds(total_seconds);
            played_seconds = (int)audioFile.CurrentTime.TotalSeconds;
            TimeSpan p_s = TimeSpan.FromSeconds(played_seconds);
            left_seconds = (int)(audioFile.TotalTime.TotalSeconds - audioFile.CurrentTime.TotalSeconds);
            TimeSpan l_s = TimeSpan.FromSeconds(left_seconds);
            if (audioFile != null)
            {
                trackBar1.Value = played_seconds;
                label1.Text = p_s + "/" + t_s;
            }
            if (played_seconds >= trackBar1.Maximum)
            {
                timer.Stop();
            }
        }

        private void play_chapter()
        {
            try
            {
                if (outputDevice == null)
                {
                    outputDevice = new WaveOutEvent();
                    outputDevice.PlaybackStopped += OnPlaybackStopped;
                }
                if (audioFile == null)
                {
                    audioFile = new AudioFileReader(@chapter_playlist[current_chapter]);
                    outputDevice.Init(audioFile);
                }
                double total_time = audioFile.TotalTime.TotalSeconds;
                trackBar1.Maximum = (int)total_time;
                played_seconds = 0;
                outputDevice.Play();
                InitTimer();
            }
            catch (System.ArgumentOutOfRangeException)
            {
                //throw;
                MessageBox.Show("Nothing to play. Playlist empty.");
            }
        }

        private void stop_chapter()
        {
            outputDevice?.Stop();

            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            if (audioFile != null)
            {
                try
                {
                    audioFile.Dispose();
                    audioFile = null;
                    play_chapter();
                }
                catch
                {
                    
                }
                
                timer.Stop();
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            if (stop_cause != 1)
            {
                if (current_chapter == (chapter_playlist.Count - 1))
                {

                }
                else
                {
                    current_chapter++;
                }
                stop_chapter();
                play_chapter();
            }
            else
            {
                stop_cause = 0;
            }
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            play_chapter();
        }

        private void pauseButton_Click(object sender, EventArgs e)
        {
            outputDevice?.Pause();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            stop_cause = 1;
            stop_chapter();
        }

        private void prevButton_Click(object sender, EventArgs e)
        {
            if (audioFile != null)
            {
                current_chapter = Math.Max((current_chapter - 1), 0);
                stop_cause = 1;
                stop_chapter();
                play_chapter();
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (audioFile != null)
            {

                if (current_chapter == (chapter_playlist.Count - 1))
                {
                    current_chapter = (chapter_playlist.Count - 1);
                }
                else
                {
                    current_chapter++;
                }

                stop_cause = 1;
                stop_chapter();
                play_chapter();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            TimeSpan scroll = TimeSpan.FromSeconds(trackBar1.Value);
            audioFile.CurrentTime = scroll;
        }

        //add audiobook to library//
        private void button2_Click(object sender, EventArgs e)
        {
            if (category_selection != null)
            {
                if (!string.IsNullOrWhiteSpace(textBox2.Text))
                {
                    if (listView1.Items.Count > 0)
                    {
                        if (pictureBox2.Image != null)
                        {
                            var path = login + "\\Audiobooks\\" + category_selection + "\\" + textBox2.Text;
                            Directory.CreateDirectory(path);
                            foreach (ListViewItem chapter in listView1.Items)
                            {
                                FileInfo fi = new FileInfo(chapter.SubItems[1].Text);
                                int index = chapter.Index + 1;
                                var filename = "Chapter " + index;
                                fi.CopyTo(path + "\\" + filename +".mp3");
                                Image cover = pictureBox2.Image;
                                cover.Save(path + "\\cover.jpg");
                            }
                            blank_create();
                            MessageBox.Show("Audiobook created");

                        }
                    }
                }
            }
        }

        //add chapter to list//
        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                var path = textBox1.Text;
                var chapter_count = listView1.Items.Count + 1;
                ListViewItem item = null;
                ListViewItem.ListViewSubItem[] subItems;
                item = new ListViewItem("Chapter " + chapter_count);
                subItems = new ListViewItem.ListViewSubItem[]
                                  {new ListViewItem.ListViewSubItem(item, path)};
                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
                textBox1.Text = null;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            var drop = listView1.SelectedIndices[0];
            listView1.Items.RemoveAt(drop);
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            category_selection = listView2.SelectedItems[0].Text;
            label6.Text = "Selected Category: " + category_selection;
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            textBox1.Text = null;
            var filepath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "MP3 Files (*.mp3)|*.mp3";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filepath = openFileDialog.FileName;
                    textBox1.Text = filepath;
                }
                openFileDialog.Dispose();
            }
        }

        private void blank_create()
        {
            listView1.Items.Clear();
            textBox1.Clear();
            listView2.Items.Clear();
            textBox2.Clear();
            pictureBox2.Image = null;
            label6.Text = "Selected Category:";
            category_selection = null;
            ListViewItem item = null;
            var path = login + "\\Audiobooks";
            var categories = Directory.GetDirectories(path);
            foreach(string category in categories)
            {
                string category_name = new DirectoryInfo(@category).Name;
                item = new ListViewItem(category_name);
                listView2.Items.Add(item);
            }
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (tabControl1.SelectedTab == createTab)
            {
                blank_create();
            }
            else if (tabControl1.SelectedTab == libraryTab)
            {
                listCategories();
            }
            else if (tabControl1.SelectedTab == readTab)
            {
                listEbooks();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                
                openFileDialog.Filter = "JPG Files (*.jpg)|*.jpg";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var image = openFileDialog.FileName;
                    pictureBox2.Image = Image.FromFile(image);
                }
                openFileDialog.Dispose();
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var selected = libraryList.SelectedItems;
            foreach (ListViewItem item in selected)
            {
                try
                {
                    chapterList.Clear();
                    var path = item.SubItems[1].Text;
                    Directory.Delete(path, true);
                    item.Remove();
                }
                catch
                {
                    throw;
                }
            }
            listCategories();
        }

        private void Edit_Click(object sender, EventArgs e)
        {
            ListViewItem edit = libraryList.SelectedItems[0];
            if(edit != null)
            {
                Edit editDialog = new Edit(edit, login);

                if (editDialog.ShowDialog(this) == DialogResult.OK)
                {
                    listCategories();
                    editDialog.Dispose();
                }
                else
                {

                }
                editDialog.Dispose();
            }
        }

        public string AddCategory()
        {
            Form2 testDialog = new Form2();
            string category = null;

            if (testDialog.ShowDialog(this) == DialogResult.OK)
            {
                category = testDialog.textBox1.Text;
            }
            else
            {
            }
            testDialog.Dispose();
            return category;
        }

        public string RemoveCategory()
        {
            Form1 testDialog = new Form1(login);
            string category = null;

            if (testDialog.ShowDialog(this) == DialogResult.OK)
            {
                category = testDialog.path;
            }
            else
            {
            }
            testDialog.Dispose();
            return category;
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var path = AddCategory();
            if (path != null)
            {
                Directory.CreateDirectory(login + "\\Audiobooks\\" + path);
                listCategories();
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var path = RemoveCategory();
                if (path != null)
                {
                    Directory.Delete(login + "\\Audiobooks\\" + path, true);
                    listCategories();
                }
                
            }
            catch
            {
                throw;
            }
        }

        private void pack()
        {
            zipClean();
            ZipFile.CreateFromDirectory(login+"//Audiobooks", login+"//Audiobooks.zip");
        }

        private void zipClean()
        {
            try
            {
                FileInfo zip = new FileInfo(login + "//Audiobooks.zip");
                zip.Delete();
            }
            catch
            {

            }
        }

        private void unpack()
        {
            if (Directory.Exists(login + "//Audiobooks"))
            {
                imageList1.Images.Clear();
                Directory.Delete(login + "//Audiobooks", true);
            }
            ZipFile.ExtractToDirectory(login + "//Audiobooks.zip", login + "//Audiobooks");
        }

        private void localToRemoteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FtpClient client = new FtpClient("www.mkwk019.cba.pl");
            client.Credentials = new System.Net.NetworkCredential("mfixit", "Qwertymyfixit12");
            client.Connect();

            client.SetWorkingDirectory(@"/mfixit.cba.pl/Files//" + login);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                pack();

                Progress<double> progress = new Progress<double>(x => {
                    // When progress in unknown, -1 will be sent
                    if (x < 0)
                    {
                        _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Visible = false; });
                    }
                    else
                    {
                        _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Visible = true; });
                        _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Value = (int)x; });
                    }
                });

                client.UploadFile(@login + "/Audiobooks.zip", "Audiobooks.zip", FtpExists.Overwrite, false, FtpVerify.Retry, progress);
                client.Disconnect();
                zipClean();
                MessageBox.Show("Synchronization succesful.");
                _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Value = 0; });
                _mf.Invoke((MethodInvoker)delegate { _mf.listCategories(); });

            }).Start();

        }

        private void remoteToLocalToolStripMenuItem_Click(object sender, EventArgs e)
        {

            FtpClient client = new FtpClient("www.mkwk019.cba.pl");
            client.Credentials = new System.Net.NetworkCredential("mfixit", "Qwertymyfixit12");
            client.Connect();

            client.SetWorkingDirectory(@"/mfixit.cba.pl/Files//" + login);
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Progress<double> progress = new Progress<double>(x => {
                    // When progress in unknown, -1 will be sent
                    if (x < 0)
                    {
                        _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Visible = false; });
                    }
                    else
                    {
                        _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Visible = true; });
                        _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Value = (int)x; });
                    }
                });
                client.DownloadFile(@login + "\\Audiobooks.zip", "Audiobooks.zip", true, FtpVerify.Retry, progress);
                _mf.Invoke((MethodInvoker)delegate { _mf.unpack(); });
                

                zipClean();
                MessageBox.Show("Synchronization succesful.");
                client.Disconnect();
                _mf.Invoke((MethodInvoker)delegate { _mf.progressBar.Value = 0; });
                _mf.Invoke((MethodInvoker)delegate { _mf.listCategories(); });
                

            }).Start();
        }
        

        private void listEbooks()
        {
            ebookList.Clear();
            List<string> ebooksPath = new List<string>(Directory.GetFiles(login + "\\Ebooks", "*.txt"));
            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems = null;
            foreach (var bookPath in ebooksPath)
            {
                var name = new DirectoryInfo(bookPath).Name;
                var fullpath = new DirectoryInfo(bookPath).FullName;
                item = new ListViewItem(name);
                subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, fullpath)};
                item.SubItems.AddRange(subItems);
                ebookList.Items.Add(item);
            }
        }

        private void fillSentences()
        {
            int counter = 1;
            
            foreach (string sentence in ebook_sentences)
            {
                listBox1.Items.Add(counter);
                counter++;
            }
        }

        private void listView3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                
                try
                {
                    var protompt = synth.GetCurrentlySpokenPrompt();
                    synth.SpeakAsyncCancel(protompt);
                    sentence_index = 0;
                }
                catch
                {

                }
                

                ebook_sentences = null;
                string ebook_path = ebookList.SelectedItems[0].SubItems[1].Text;
                string ebook_text = System.IO.File.ReadAllText(ebook_path);
                richTextBox1.Text = ebook_text;
                ebook_sentences = ebook_text.Split('!', '.', '?');
                fillSentences();

            }
            catch
            {
                MessageBox.Show("Error: Could not read file from disk.");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            openFileDialog1.Filter = "TXT|*.txt";
            if (result == DialogResult.OK)
            {
                try
                {
                    string ebook_path = openFileDialog1.FileName;
                    var name = new DirectoryInfo(ebook_path).Name;
                    string ebook_destination = login + "\\Ebooks\\" + name;
                    System.IO.File.Copy(ebook_path, ebook_destination);
                    listEbooks();
                }
                catch
                {
                    MessageBox.Show("Error: Could not read file from disk.");
                }
            }
        }

        private void start_reading()
        {
            if (synth.State == SynthesizerState.Paused)
            {
                synth.Resume();
            }
            if (synth.State == SynthesizerState.Speaking)
            {
                stop_reading();
                sentence_index = listBox1.SelectedIndex;
                read_sentence();
            }
            else
            {
                if (listBox1.SelectedItems.Count == 1)
                {
                    sentence_index = listBox1.SelectedIndex;
                    read_sentence();
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            start_reading();
        }
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            start_reading();
        }

        private void read_sentence()
        {
            sentence.ClearContent();
            sentence.AppendText(ebook_sentences[sentence_index]);
            synth.SpeakAsync(sentence);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            synth.Pause();
        }

        private void stop_reading()
        {
            var protompt = synth.GetCurrentlySpokenPrompt();
            synth.SpeakAsyncCancel(protompt);
            sentence_index = 0;
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            stop_reading();
        }

        private void synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (e.Cancelled != true)
            {
                if (sentence_index < listBox1.Items.Count-1)
                {
                    sentence_index++;
                    read_sentence();
                }
                else if (sentence_index >= listBox1.Items.Count)
                {
                    sentence_index = 0;
                    read_sentence();
                }
            }
        }

        private void Panel_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
    }
}
