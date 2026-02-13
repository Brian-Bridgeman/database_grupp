using Microsoft.Data.Sqlite;

class Program
{
    static string connectionString = "Data Source=creditcard.db";
    static void Main()
    {
        CreateDatabase();
        ShowMenu();
    }

    static void CreateDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS persons (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            first_name TEXT NOT NULL,
            last_name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS mock_data (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            first_name TEXT,
            last_name TEXT
        );
        ";

        command.ExecuteNonQuery();
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("\n==== CREDIT CARD SYSTEM ====");
            Console.WriteLine("1. Import mock data (run once)");
            Console.WriteLine("2. Generate persons");
            Console.WriteLine("0. Exit");
            Console.Write("Choice: ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    ImportMockData();
                    break;
                case "2":
                    GeneratePersonsMenu();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }

    static void ImportMockData()
    {
        if (!File.Exists("mockdata.sql"))
        {
            Console.WriteLine("mockdata.sql not found!");
            return;
        }

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        string sql = File.ReadAllText("mockdata.sql");

        var command = connection.CreateCommand();
        command.CommandText = sql;

        command.ExecuteNonQuery();

        Console.WriteLine("Mock data imported successfully!");
    }

    static (List<string>, List<string>) LoadNames()
    {
        var firstNames = new List<string>();
        var lastNames = new List<string>();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT first_name, last_name FROM mock_data;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            firstNames.Add(reader.GetString(0));
            lastNames.Add(reader.GetString(1));
        }

        return (firstNames, lastNames);
    }

    static void GeneratePersonsMenu()
    {
        Console.Write("How many persons? (default 100000): ");
        var input = Console.ReadLine();

        int amount = 100000;

        if (!string.IsNullOrWhiteSpace(input))
        {
            int.TryParse(input, out amount);
        }

        GeneratePersons(amount);
    }

    static void GeneratePersons(int amount)
    {
        var (firstNames, lastNames) = LoadNames();

        if (firstNames.Count == 0)
        {
            Console.WriteLine("No mock data found. Import mock data first!");
            return;
        }

        var random = new Random();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        for (int i = 0; i < amount; i++)
        {
            string first = firstNames[random.Next(firstNames.Count)];
            string last = lastNames[random.Next(lastNames.Count)];

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO persons (first_name, last_name)
            VALUES ($first, $last);
            ";

            command.Parameters.AddWithValue("$first", first);
            command.Parameters.AddWithValue("$last", last);

            command.ExecuteNonQuery();
        }

        Console.WriteLine($"{amount} persons generated!");
    }
}