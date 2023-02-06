using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using MySql.Data;

namespace NetScape_AudioBook_Library
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        string connectionString = null;
        MySqlConnection connection;

        private void Login_Load(object sender, EventArgs e)
        {
            connectionString = "SERVER=mfixit.cba.pl;USERNAME=mfixit;PASSWORD=Qwertymyfixit1;DATABASE=mfixit;SslMode=none;Convert Zero Datetime = true";
            connection = new MySqlConnection(connectionString);
        }

        private void SignUp_Click(object sender, EventArgs e)
        {
            Register registerForm = new Register();
            registerForm.Show();
        }

        private void SignIn_Click(object sender, EventArgs e)
        {
            int ID = 6;
            Panel panel = new Panel(ID);
            panel.Show();
            panel.Activate();
            this.Hide();
            /*
            string pass = null;
            if (!string.IsNullOrWhiteSpace(textBox1.Text) && !string.IsNullOrWhiteSpace(textBox2.Text))
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    MySqlCommand cmd = new MySqlCommand("SELECT ID, Password FROM Users WHERE Email=@EMAIL;)", connection);
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@EMAIL", textBox1.Text);

                    MySqlDataReader rdr = null;

                    rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            ID = Int32.Parse(rdr[0].ToString());
                            pass = rdr[1].ToString();
                        }
                    }

                    if(pass == textBox2.Text)
                    {
                        Panel panel = new Panel(ID);
                        panel.Show();
                        panel.Activate();
                        this.Hide();
                    }
                }
                catch
                {
                    MessageBox.Show("Something went wrong.");
                }
            }
            */
        }


    }
}
