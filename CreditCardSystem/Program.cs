using Microsoft.Data.Sqlite;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        CreateDatabase();
        ShowMenu();
    }

    static void CreateDatabase()
    {
        using (var connection = new SqliteConnection("Data Source=creditcard.db"))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS CreditCards (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CardNumber TEXT NOT NULL,
                    ExpiryDate TEXT NOT NULL,
                    CVV TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("Credit Card Management System");
            Console.WriteLine("1. Add Credit Card");
            Console.WriteLine("2. View Credit Cards");
            Console.WriteLine("3. Exit");
            Console.Write("Select an option: ");

            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    AddCreditCard();
                    break;
                case "2":
                    ViewCreditCards();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }
}