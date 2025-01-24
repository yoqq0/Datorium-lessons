using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace mebeluveikals
{
    public partial class Form1 : Form
    {
        private FurnitureManager furnitureManager;

        public Form1()
        {
            InitializeComponent();

            furnitureManager = new FurnitureManager("Data Source=furniture.db");

            var furniture = furnitureManager.ReadFurniture();
            var furnitureNames = new List<string>();

            foreach (var f in furniture)
            {
                furnitureNames.Add(f.Name);
            }

            selectProductComboBox.DataSource = furnitureNames;
        }

        private void selectBtn_Click(object sender, EventArgs e)
        {
            var furniture = furnitureManager.ReadFurnitureByName(selectProductComboBox.Text);

            nameTextBox.Text = furniture.Name;
            descTextBox.Text = furniture.Description;
            priceTextBox.Text = furniture.Price.ToString();
            hTextBox.Text = furniture.Height.ToString();
            wTextBox.Text = furniture.Width.ToString();
            lTextBox.Text = furniture.Length.ToString();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(nameTextBox.Text))
                {
                    MessageBox.Show("Nav norādīts nosaukums.");
                }
                else if (string.IsNullOrEmpty(descTextBox.Text))
                {
                    MessageBox.Show("Nav norādīts apraksts.");
                }
                else if (string.IsNullOrEmpty(priceTextBox.Text))
                {
                    MessageBox.Show("Nav norādīta cena.");
                }
                else if (string.IsNullOrEmpty(hTextBox.Text))
                {
                    MessageBox.Show("Nav norādīts augstums.");
                }
                else if (string.IsNullOrEmpty(wTextBox.Text))
                {
                    MessageBox.Show("Nav norādīts platums.");
                }
                else if (string.IsNullOrEmpty(lTextBox.Text))
                {
                    MessageBox.Show("Nav norādīts garums.");
                }


                furnitureManager.AddFurniture(nameTextBox.Text, descTextBox.Text,
                    Convert.ToDouble(priceTextBox.Text), Convert.ToInt32(hTextBox.Text),
                    Convert.ToInt32(wTextBox.Text), Convert.ToInt32(lTextBox.Text));

                List<string> furnitureList = (List<string>)selectProductComboBox.DataSource;
                furnitureList.Add(nameTextBox.Text);

                selectProductComboBox.DataSource = null;
                selectProductComboBox.DataSource = furnitureList;

                MessageBox.Show("Ieraksts tika pievienots datubāzei");
            }
            catch (SqliteException ex)
            {
                MessageBox.Show("Notikusi SQL kļūda.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Notikusi kļūda.");
            }
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            furnitureManager.DeleteFurnitureByName(selectProductComboBox.Text);

            List<string> furnitureList = (List<string>)selectProductComboBox.DataSource;
            furnitureList.Remove(selectProductComboBox.Text);

            selectProductComboBox.DataSource = null;
            selectProductComboBox.DataSource = furnitureList;

            MessageBox.Show("Mēbele tika izdzēsta no datubāzes.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=furniture.db"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Furniture";

                    using (var reader = command.ExecuteReader())
                    {
                        // Define file path
                        var filePath = Path.Combine(Environment.CurrentDirectory, "furniture_export.csv");

                        using (var writer = new StreamWriter(filePath))
                        {
                            // Write header
                            writer.WriteLine("Name,Description,Price,Height,Width,Length");

                            // Write rows
                            while (reader.Read())
                            {
                                var line = $"{reader["Name"]},{reader["Description"]},{reader["Price"]},{reader["Height"]},{reader["Width"]},{reader["Length"]}";
                                writer.WriteLine(line);
                            }
                        }

                        MessageBox.Show($"CSV fails veiksmīgi izveidots un tas ir atrodams šeit {filePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }



        private void importbtn_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    openFileDialog.Title = "Select a CSV File";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var filePath = openFileDialog.FileName;

                        using (var connection = new SqliteConnection("Data Source=furniture.db"))
                        {
                            connection.Open();

                            var lines = File.ReadAllLines(filePath);

                            // Skip header row
                            for (int i = 1; i < lines.Length; i++)
                            {
                                var line = lines[i];
                                var fields = line.Split(',');

                                if (fields.Length != 6)
                                {
                                    MessageBox.Show($"Invalid data format on line {i + 1}.");
                                    continue;
                                }

                                var name = fields[0].Trim();
                                var description = fields[1].Trim();
                                var price = Convert.ToDouble(fields[2].Trim());
                                var height = Convert.ToInt32(fields[3].Trim());
                                var width = Convert.ToInt32(fields[4].Trim());
                                var length = Convert.ToInt32(fields[5].Trim());

                                // Insert or update record
                                var command = connection.CreateCommand();
                                command.CommandText = @"
                            INSERT INTO Furniture (Name, Description, Price, Height, Width, Length)
                            VALUES ($name, $description, $price, $height, $width, $length)
                            ON CONFLICT(Name) DO UPDATE SET
                                Description=excluded.Description,
                                Price=excluded.Price,
                                Height=excluded.Height,
                                Width=excluded.Width,
                                Length=excluded.Length";

                                command.Parameters.AddWithValue("$name", name);
                                command.Parameters.AddWithValue("$description", description);
                                command.Parameters.AddWithValue("$price", price);
                                command.Parameters.AddWithValue("$height", height);
                                command.Parameters.AddWithValue("$width", width);
                                command.Parameters.AddWithValue("$length", length);

                                command.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("Dati tika veiksmīgi importēti.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

    }
}
