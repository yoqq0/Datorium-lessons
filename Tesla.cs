using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Tesla
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Data Source=tesla_rental.db;Version=3;";
            InitializeDatabase(connectionString);

            Console.WriteLine("Welcome to Tesla Rental Platform!");
            // Example flow (for simplicity, user input not implemented)
            AddCar(connectionString, "Model 3", 20, 0.5);
            AddCustomer(connectionString, "Andris Ozols", "andris.ozols@gmail.com");
            int rentalId = StartRental(connectionString, 1, 1, DateTime.Now);
            EndRental(connectionString, rentalId, DateTime.Now.AddHours(5), 100);
            Console.WriteLine("Rental process completed!");
        }

        static void InitializeDatabase(string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string createCarsTable = @"CREATE TABLE IF NOT EXISTS Cars (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Model TEXT NOT NULL,
                    HourlyRate REAL NOT NULL,
                    PerKmRate REAL NOT NULL
                );";

                string createCustomersTable = @"CREATE TABLE IF NOT EXISTS Customers (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Email TEXT NOT NULL
                );";

                string createRentalsTable = @"CREATE TABLE IF NOT EXISTS Rentals (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    CarID INTEGER NOT NULL,
                    CustomerID INTEGER NOT NULL,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME,
                    KmDriven REAL,
                    TotalCost REAL,
                    FOREIGN KEY(CarID) REFERENCES Cars(ID),
                    FOREIGN KEY(CustomerID) REFERENCES Customers(ID)
                );";

                using (var command = new SQLiteCommand(createCarsTable, connection))
                    command.ExecuteNonQuery();

                using (var command = new SQLiteCommand(createCustomersTable, connection))
                    command.ExecuteNonQuery();

                using (var command = new SQLiteCommand(createRentalsTable, connection))
                    command.ExecuteNonQuery();

                Console.WriteLine("Database initialized.");
            }
        }

        static void AddCar(string connectionString, string model, double hourlyRate, double perKmRate)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Cars (Model, HourlyRate, PerKmRate) VALUES (@Model, @HourlyRate, @PerKmRate);";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Model", model);
                    command.Parameters.AddWithValue("@HourlyRate", hourlyRate);
                    command.Parameters.AddWithValue("@PerKmRate", perKmRate);
                    command.ExecuteNonQuery();
                }
                Console.WriteLine($"Car '{model}' added.");
            }
        }

        static void AddCustomer(string connectionString, string name, string email)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Customers (Name, Email) VALUES (@Name, @Email);";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Email", email);
                    command.ExecuteNonQuery();
                }
                Console.WriteLine($"Customer '{name}' added.");
            }
        }

        static int StartRental(string connectionString, int carId, int customerId, DateTime startTime)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Rentals (CarID, CustomerID, StartTime) VALUES (@CarID, @CustomerID, @StartTime);";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CarID", carId);
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    command.Parameters.AddWithValue("@StartTime", startTime);
                    command.ExecuteNonQuery();
                    int rentalId = (int)connection.LastInsertRowId;
                    Console.WriteLine($"Rental started (ID: {rentalId}).");
                    return rentalId;
                }
            }
        }

        static void EndRental(string connectionString, int rentalId, DateTime endTime, double kmDriven)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Retrieve rental details
                string query = "SELECT StartTime, Cars.HourlyRate, Cars.PerKmRate FROM Rentals INNER JOIN Cars ON Rentals.CarID = Cars.ID WHERE Rentals.ID = @RentalID;";
                DateTime startTime;
                double hourlyRate, perKmRate;

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RentalID", rentalId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            startTime = DateTime.Parse(reader["StartTime"].ToString());
                            hourlyRate = double.Parse(reader["HourlyRate"].ToString());
                            perKmRate = double.Parse(reader["PerKmRate"].ToString());
                        }
                        else
                        {
                            throw new Exception("Rental not found.");
                        }
                    }
                }

                // Calculate cost
                double rentalHours = (endTime - startTime).TotalHours;
                double totalCost = (rentalHours * hourlyRate) + (kmDriven * perKmRate);

                // Update rental
                query = "UPDATE Rentals SET EndTime = @EndTime, KmDriven = @KmDriven, TotalCost = @TotalCost WHERE ID = @RentalID;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EndTime", endTime);
                    command.Parameters.AddWithValue("@KmDriven", kmDriven);
                    command.Parameters.AddWithValue("@TotalCost", totalCost);
                    command.Parameters.AddWithValue("@RentalID", rentalId);
                    command.ExecuteNonQuery();
                }

                Console.WriteLine($"Rental ended. Total cost: EUR {totalCost:F2}.");
            }
        }
    }
}
