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
    public partial class Form1 : Form
    {
        public string path = null;
        private string login = null;
        public Form1(string login)
        {
            this.login = login;
            InitializeComponent();
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            listCategories();
        }

        private void listCategories()
        {
            ListViewItem item = null;
            var path = login + "\\Audiobooks";
            var categories = Directory.GetDirectories(path);
            foreach (string category in categories)
            {
                string category_name = new DirectoryInfo(@category).Name;
                item = new ListViewItem(category_name);
                listView1.Items.Add(item);
            }
        }
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            path = listView1.SelectedItems[0].Text;
            label2.Text = "Selected: " + path;
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
