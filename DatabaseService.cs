using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PersonalExpenseTracker
{
    public class DatabaseService
    {
        private const string DbName = "expenses.db";
        private string _connectionString = $"Data Source={DbName}";

        public DatabaseService()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(DbName))
            {
                // Create empty file first to ensure it exists for SQLite
                // Although SqliteConnection usually creates it, explicit creation can be safer in some envs
                // But standard practice is just letting Open() create it.
                // We'll stick to standard Sqlite behavior.
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Transactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Type TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Amount DECIMAL NOT NULL,
                        Description TEXT
                    );";
                command.ExecuteNonQuery();
            }
        }

        public void AddTransaction(Transaction transaction)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Transactions (Date, Type, Category, Amount, Description)
                    VALUES ($date, $type, $category, $amount, $description)";

                command.Parameters.AddWithValue("$date", transaction.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$type", transaction.Type);
                command.Parameters.AddWithValue("$category", transaction.Category);
                command.Parameters.AddWithValue("$amount", transaction.Amount);
                command.Parameters.AddWithValue("$description", transaction.Description ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }

        public void DeleteTransaction(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Transactions WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }

        public List<Transaction> GetAllTransactions()
        {
            var transactions = new List<Transaction>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Date, Type, Category, Amount, Description FROM Transactions ORDER BY Date DESC";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        transactions.Add(new Transaction
                        {
                            Id = reader.GetInt32(0),
                            Date = DateTime.Parse(reader.GetString(1)),
                            Type = reader.GetString(2),
                            Category = reader.GetString(3),
                            Amount = reader.GetDecimal(4),
                            Description = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            return transactions;
        }
    }
}
