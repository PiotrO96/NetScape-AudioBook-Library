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
    public partial class Register : Form
    {
        public Register()
        {
            InitializeComponent();
        }

        string connectionString = null;
        MySqlConnection connection;

        private void Register_Load(object sender, EventArgs e)
        {
            connectionString = "SERVER=mfixit.cba.pl;USERNAME=mfixit;PASSWORD=Qwertymyfixit1;DATABASE=mfixit;SslMode=none;Convert Zero Datetime = true";
            connection = new MySqlConnection(connectionString);
        }

        private void SignIn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SignUp_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text) && !string.IsNullOrWhiteSpace(textBox2.Text) && !string.IsNullOrWhiteSpace(textBox1.Text))
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    MySqlCommand cmd = new MySqlCommand("INSERT INTO Users(Login, Email, Password, Rank) VALUES(@LOGIN, @EMAIL, @PASSWORD, 'user')", connection);
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@LOGIN", textBox1.Text);
                    cmd.Parameters.AddWithValue("@EMAIL", textBox2.Text);
                    cmd.Parameters.AddWithValue("@PASSWORD", textBox3.Text);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Registration successful.");
                }
                catch
                {
                    MessageBox.Show("Something went wrong.");
                }
            }
        }

        
    }
}
