using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TextFilesWpf
{
    /// <summary>
    /// Реализует операции генерации файлов, объединения в один файл, экспорта в БД
    /// </summary>
    public class TextFilesHandler
    {
        /// <summary>
        /// Генерация 100 текстовых файлов
        /// </summary>
        public static void Generate100Files()
        {
            string filesFolder = ConfigurationManager.AppSettings["FilesFolder"];
            if (!Directory.Exists(filesFolder))
                Directory.CreateDirectory(filesFolder);

            Task[] tasks = new Task[100];
            for (int i = 1; i <= 100; ++i)
            {
                //j используется для захвата лямбдой (при захвате i получаем 101)
                int j = i;
                tasks[i - 1] = Task.Run(() => GenerateFile($"{filesFolder}/{j}.txt"));
            }

            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Объединение файлов в один
        /// </summary>
        /// <param name="substr">Подстрока, при содержании которой строка файла не копируется в объединенный файл</param>
        /// <returns>Количество пропущенных строк</returns>
        public static int UniteFiles(string substr)
        {
            string filesFolder = ConfigurationManager.AppSettings["FilesFolder"];

            //Счетчик количества пропущенных строк
            int counter = 0;
            using (var writer = new StreamWriter($"{filesFolder}/united.txt"))
            {
                for (int i = 1; i <= 100; ++i)
                {
                    using (var reader = new StreamReader($"{filesFolder}/{i}.txt"))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            //В случае, если строка файла содержит введенную подстроку, строка файла не пишется в объединенный файл
                            if (substr != string.Empty && line.Contains(substr))
                            {
                                ++counter;
                                continue;
                            }

                            writer.WriteLine(line);
                        }
                    }
                }
            }

            return counter;
        }

        /// <summary>
        /// Экспорт в БД
        /// </summary>
        /// <param name="fileName">Имя файла, содержимое которого экспортируется</param>
        /// <param name="progressCallback">Колбэк, который используется для отображения прогресса выполнения</param>
        public static void ExportFilesToDB(string fileName, Action<int> progressCallback)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
            var tableName = ConfigurationManager.AppSettings["TableName"];
            var path = $"{ConfigurationManager.AppSettings["FilesFolder"]}/{fileName}";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                if (TableExists(connection, tableName))
                {
                    ClearTable(connection, tableName);
                }
                else
                {
                    CreateTable(connection, tableName);
                }

                BulkInsert(path, connection, tableName, progressCallback);
            }
        }

        /// <summary>
        /// Определяет количество строк в текстовом файле
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns>Количество строк</returns>
        public static int GetLinesCount(string fileName)
        {
            string path = $"{ConfigurationManager.AppSettings["FilesFolder"]}/{fileName}";
            return File.ReadLines(path).Count();
        }

        /// <summary>
        /// Выполняет массовую вставку в таблицу в БД
        /// </summary>
        /// <param name="path">Путь к файлу с содержимым</param>
        /// <param name="connection">Соединение с БД</param>
        /// <param name="tableName">Имя таблицы в БД</param>
        /// <param name="progressCallback">Колбэк, который используется для отображения прогресса выполнения</param>
        private static void BulkInsert(string path, SqlConnection connection, string tableName, Action<int> progressCallback)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    int previousInsertedRowsCount = 0;
                    bulkCopy.DestinationTableName = tableName;
                    bulkCopy.NotifyAfter = 1723;
                    bulkCopy.SqlRowsCopied += (s, e) => progressCallback((int)e.RowsCopied + previousInsertedRowsCount);
                    string str;
                    DataTable table = new DataTable();
                    table.Columns.Add("Id", typeof(int));
                    table.Columns.Add("Item1", typeof(DateTime));
                    table.Columns.Add("Item2", typeof(string));
                    table.Columns.Add("Item3", typeof(string));
                    table.Columns.Add("Item4", typeof(int));
                    table.Columns.Add("Item5", typeof(double));

                    while ((str = reader.ReadLine()) != null)
                    {
                        string[] fields = str.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                        DataRow row = table.NewRow();
                        row.ItemArray = new object[] { 0, fields[0],
                                fields[1], fields[2], int.Parse(fields[3]), double.Parse(fields[4])};
                        table.Rows.Add(row);

                        if (table.Rows.Count == 10000)
                        {
                            bulkCopy.WriteToServer(table);
                            table.Clear();
                            previousInsertedRowsCount += 10000;
                        }
                    }

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        /// <summary>
        /// Очищает таблицу
        /// </summary>
        /// <param name="connection">Соединение с БД</param>
        /// <param name="tableName">Имя таблицы в БД</param>
        private static void ClearTable(SqlConnection connection, string tableName)
        {
            var dropTableCommandText = $"TRUNCATE TABLE {tableName}";
            var dropTableCommand = new SqlCommand(dropTableCommandText, connection);
            dropTableCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Создает таблицу
        /// </summary>
        /// <param name="connection">Соединение с БД</param>
        /// <param name="tableName">Имя таблицы в БД</param>
        private static void CreateTable(SqlConnection connection, string tableName)
        {
            var createTableCommandText = $"CREATE TABLE {tableName}(Id INT PRIMARY KEY IDENTITY," +
                "Item1 DATE, Item2 CHAR(10), Item3 NCHAR(10), Item4 INT, Item5 FLOAT)";
            var createTableCommand = new SqlCommand(createTableCommandText, connection);
            createTableCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Проверяет, существует ли таблица
        /// </summary>
        /// <param name="connection">Соединение с БД</param>
        /// <param name="tableName">Имя таблицы в БД</param>
        /// <returns>Значение, показывающее, существует ли таблица с данным именем</returns>
        private static bool TableExists(SqlConnection connection, string tableName)
        {
            DataTable tables = connection.GetSchema("TABLES", new string[] { null, null, tableName });
            if (tables.Rows.Count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Генерация файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        private static void GenerateFile(string fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                Random random = new Random();
                const int fiveYears = 365 * 5;
                const string latinChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                const string cyrillicChars = "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
                for (int i = 0; i < 100000; ++i)
                {
                    var builder = new StringBuilder();
                    builder.Append((DateTime.Now - new TimeSpan(random.Next(fiveYears), 0, 0, 0)).ToString("dd.MM.yyyy"))
                       .Append("||");
                    for (int j = 0; j < 10; ++j)
                        builder.Append(latinChars[random.Next(latinChars.Length)]);
                    builder.Append("||");
                    for (int j = 0; j < 10; ++j)
                        builder.Append(cyrillicChars[random.Next(cyrillicChars.Length)]);
                    builder.Append("||");
                    builder.Append(random.Next(100000000) + 1);
                    builder.Append("||");
                    builder.Append((random.NextDouble() * 19 + 1).ToString("F8"));
                    builder.Append("||");
                    string s = builder.ToString();
                    writer.WriteLine(s);
                }
            }
        }
    }
}
