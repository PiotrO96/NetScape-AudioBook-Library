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

namespace NetScape_AudioBook_Library
{
    public partial class Edit : Form
    {
        private string category_selection = null;
        ListViewItem local_edit = null;
        string login = null;
        public Edit(ListViewItem edit, string login)
        {
            InitializeComponent();
            local_edit = edit;
            this.login = login;
        }

        private void Edit_Load(object sender, EventArgs e)
        {
            list_Chapters();
            load_data();
        }

        private void load_data()
        {
            list_Chapters();
            list_categories();
            show_cover();
            textBox2.Text = local_edit.SubItems[0].Text;
        }

        private void list_categories()
        {
            listView2.Clear();
            ListViewItem item = null;
            var path = login + "\\Audiobooks";
            var categories = Directory.GetDirectories(path);
            foreach (string category in categories)
            {
                string category_name = new DirectoryInfo(@category).Name;
                item = new ListViewItem(category_name);
                listView2.Items.Add(item);
            }
            category_selection = local_edit.Group.ToString();
            label6.Text = "Selected Category: " + category_selection;
        }

        private void list_Chapters()
        {
            listView1.Clear();
            var path = local_edit.SubItems[1].Text;
            List<string> filesPath = new List<string>();
            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems;
            filesPath.AddRange(Directory.GetFiles(path, "*.mp3"));
            foreach (var file in filesPath)
            {
                var chapter_count = listView1.Items.Count + 1;
                var fullpath = new DirectoryInfo(file).FullName;
                item = new ListViewItem("Chapter " + chapter_count);
                subItems = new ListViewItem.ListViewSubItem[]
                              {new ListViewItem.ListViewSubItem(item, fullpath)};
                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }
        }

        private void show_cover()
        {
            try
            {
                var cover = Directory.GetFiles(local_edit.SubItems[1].Text, "*.jpg").First();

                var image = Image.FromFile(cover);

                Image imgThumb = image.GetThumbnailImage(100, 100, null, new IntPtr());

                pictureBox2.Image = imgThumb;

                image.Dispose();
            }
            catch
            {
                throw;
            }
        }

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
                                fi.CopyTo(path + "\\" + filename + ".mp3");
                                Image cover = pictureBox2.Image;
                                cover.Save(path + "\\cover.jpg");
                            }
                            Directory.Delete(local_edit.SubItems[1].Text, true);
                            this.Close();
                        }
                    }
                }
            }
        }

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

        private void button4_Click(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {

                openFileDialog.Filter = "JPG Files (*.jpg)|*.jpg";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox2.Image = null;
                    var image = openFileDialog.FileName;
                    pictureBox2.Image = Image.FromFile(image);
                }
                openFileDialog.Dispose();
            }
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
    }
}
