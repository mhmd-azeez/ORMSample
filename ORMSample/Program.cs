using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ORMSample
{
    class Program
    {
        public const string ConnectionString = "Write your db connection string here!";
        static async Task Main(string[] args)
        {
            List<Person> people = await GetAll<Person>();

            var person = new Person
            {
                Id = 12,
                Birthdate = new DateTime(1984, 1, 1),
                Height = 174,
                Name = "Aram"
            };

            await Insert(person);

            var updatedPerson = people[0];
            updatedPerson.Height += 2;

            await Update(nameof(Person.Id), updatedPerson);
        }

        public static string GetSelectQuery<T>()
        {
            var columnNames = typeof(T).GetProperties().Select(p => p.Name);
            var columns = string.Join(", ", columnNames);

            return $"SELECT {columns} FROM {typeof(T).Name}";
        }

        public static async Task<List<T>> GetAll<T>() where T : new()
        {
            var list = new List<T>();

            var properties = typeof(T).GetProperties();

            var columnNames = properties.Select(p => p.Name);
            var columns = string.Join(", ", columnNames);

            var sql = $"SELECT {columns} FROM {typeof(T).Name}";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var model = new T();

                        foreach (var property in properties)
                        {
                            property.SetValue(model, reader[property.Name]);
                        }

                        list.Add(model);
                    }
                }
            }

            return list;
        }

        public static async Task Update<T>(string idPropertyName, T item)
        {
            var properties = typeof(T).GetProperties();

            var columnUpdates = properties.Where(p => p.Name != idPropertyName)
                                          .Select(p => $"{p.Name} = @{p.Name}");
            var columns = string.Join(", ", columnUpdates);

            var sql = $"UPDATE {typeof(T).Name} SET {columns} WHERE {idPropertyName} = @{idPropertyName}";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();

                foreach (var property in properties)
                {
                    command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(item));
                }

                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task Insert<T>(T item)
        {
            var properties = typeof(T).GetProperties();

            var columns = string.Join(", ", properties.Select(p => p.Name));
            var columnParameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var sql = $"INSERT INTO {typeof(T).Name} ({columns}) VALUES ({columnParameters})";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();

                foreach (var property in properties)
                {
                    command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(item));
                }

                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task<List<Person>> GetPeople()
        {
            var list = new List<Person>();

            var sql = "SELECT Id, Name, Height, Birthdate From Person";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var person = new Person
                        {
                            Id = (int)reader["Id"],
                            Name = (string)reader["Name"],
                            Height = (double)reader["Height"],
                            Birthdate = (DateTime)reader["Birthdate"],
                        };

                        list.Add(person);
                    }
                }
            }

            return list;
        }
    }

    class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Height { get; set; }
        public DateTime Birthdate { get; set; }
    }
}
