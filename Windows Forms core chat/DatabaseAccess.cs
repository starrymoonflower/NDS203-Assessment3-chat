using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using Windows_Forms_Chat;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    public class DatabaseAccess
    {
        // Path to the database file
        // The file will be created in project folder (bin/Debug)
        public static readonly string DATABASE_ADDRESS = "Data Source=.\\DemoDB.db;Version=3;";

        // METHOD: Initialize the database and create Users table if it doesn't exist
        public static void StartupDatabase()
        {
            // "using" ensures connection closes automatically
            using (SQLiteConnection cnn = new SQLiteConnection(DATABASE_ADDRESS))
            {
                cnn.Open();

                // SQL to create Users table
                string sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                    Username TEXT UNIQUE NOT NULL, 
                    Password TEXT NOT NULL,
                    Wins INTEGER DEFAULT 0, 
                    Losses INTEGER DEFAULT 0, 
                    Draws INTEGER DEFAULT 0 
                );";

                // SQL: Drop a Table
                /*string sql = "DROP TABLE if exists Users";
                SQLiteCommand sql_dropTable = new SQLiteCommand(sql, cnn);
                sql_dropTable.ExecuteNonQuery();*/

                // SQL: Create a Table
                //string sql1 = "CREATE TABLE if not exists Users (id INTEGER PRIMARY KEY, first_name TEXT NOT NULL, last_name TEXT NOT NULL, wins INT, losses INT, draws INT)";
                //SQLiteCommand sql_createTable = new SQLiteCommand(sql1, cnn);
                //sql_createTable.ExecuteNonQuery();

                // SQL: Insert Into
                //string sql = "INSERT INTO Users (first_name, last_name) VALUES (@firstname, @lastname)";

                // Execute SQL command
                SQLiteCommand cmd = new SQLiteCommand(sql, cnn);
                cmd.ExecuteNonQuery();
            
                //string firstname = "Linda";
                //string lastname = "Moose";
                //sql_insertTable.Parameters.AddWithValue("@firstname", firstname);
                //sql_insertTable.Parameters.AddWithValue("@lastname", lastname);
                //sql_insertTable.ExecuteNonQuery();

                //cnn.Close();
            }
        }

        // METHOD: Check if username + password exist (LOGIN)
        // static means we can call this function from anywhere inside our project
        public static bool DoesUserExist(string username, string password)
        {
            using (SQLiteConnection cnn = new SQLiteConnection(DATABASE_ADDRESS))
            {  cnn.Open();

                // SQL query to count matching users 
                string sql = "SELECT COUNT(*) FROM Users WHERE LOWER(Username) = LOWER(@username) AND Password = @password";

                SQLiteCommand cmd = new SQLiteCommand(sql, cnn);

                // Add values safely (prevents SQL injection)
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                // Execute query and get result
                long count = (long)cmd.ExecuteScalar();

                // If count > 0 → user exists
                return count > 0;
            }
        }

        // METHOD: Add a new user (REGISTER)
        public static bool AddUser(string username, string password)
        {
            try
            {
                using (SQLiteConnection cnn = new SQLiteConnection(DATABASE_ADDRESS))
                {
                    cnn.Open();

                    // SQL to insert a new user
                    string sql = @"
                    INSERT INTO Users (Username, Password, Wins, Losses, Draws)
                    VALUES (@username, @password, 0, 0, 0);";

                    SQLiteCommand cmd = new SQLiteCommand(sql, cnn);

                    // Add values
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    // Execute insert
                    int rows = cmd.ExecuteNonQuery();

                    // If rows > 0 → insert worked
                    return rows > 0;
                }
            }
            catch
            {
                // This happens if username already exists (UNIQUE constraint)
                return false;
            }
        }


        // METHOD: Get all scores (for !scores command)
        public static string GetScores()
        {
            using (SQLiteConnection cnn = new SQLiteConnection(DATABASE_ADDRESS))
            {
                cnn.Open();

                // SQL to get users sorted by Wins (highest first)
                string sql = @"
                SELECT Username, Wins, Losses, Draws
                FROM Users
                ORDER BY Wins DESC;";

                SQLiteCommand cmd = new SQLiteCommand(sql, cnn);
                SQLiteDataReader reader = cmd.ExecuteReader();

                string result = "Scores:\n";
                result += "Username        Wins   Losses   Draws\n";
                result += "--------------------------------------\n";

                while (reader.Read())
                {
                    result += string.Format(
                        "{0,-15} {1,4} {2,8} {3,7}\n",
                        reader["Username"],
                        reader["Wins"],
                        reader["Losses"],
                        reader["Draws"]
                    );
                }

                return result;
            }
        }


        // METHOD: Increase Wins by 1
        public static void UserWon(string username)
        {
            UpdateScore(username, "Wins");
        }

        // METHOD: Increase Losses by 1
        public static void UserLost(string username)
        {
            UpdateScore(username, "Losses");
        }

        // METHOD: Increase Draws by 1
        public static void UserDrew(string username)
        {
            UpdateScore(username, "Draws");
        }


        // METHOD: Helper method to update score column
        private static void UpdateScore(string username, string column)
        {
            using (SQLiteConnection cnn = new SQLiteConnection(DATABASE_ADDRESS))
            {
                cnn.Open();

                // SQL to increment column by 1
                string sql = "UPDATE Users SET " + column + " = " + column + " + 1 WHERE Username = @username;";

                SQLiteCommand cmd = new SQLiteCommand(sql, cnn);
                cmd.Parameters.AddWithValue("@username", username);

                cmd.ExecuteNonQuery();
            }
        }
    }
}



