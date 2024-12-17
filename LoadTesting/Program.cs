//using System;
//using System.Diagnostics;
//using System.Net.WebSockets;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using System.IO;
//using System.Collections.Generic;

//namespace LoadTesting
//{
//    class LoadTest
//    {
//        private static SemaphoreSlim _semaphore = new SemaphoreSlim(50); // Ограничение параллелизма
//        private static StreamWriter logWriter;
//        private static readonly object _logLock = new object(); // Для синхронизации записи в лог

//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("Нагрузочное тестирование сервера начато.");

//            // Настройки теста
//            int[] matrixSizes = { 10, 50, 100 }; // Размеры матриц
//            int parallelRequests = 300;         // Одновременные запросы
//            var results = new List<TestResult>();

//            // Инициализация логирования
//            logWriter = new StreamWriter("test_log.txt", true);
//            await logWriter.WriteLineAsync($"Нагрузочное тестирование начато: {DateTime.Now}");

//            try
//            {
//                foreach (var size in matrixSizes)
//                {
//                    Console.WriteLine($"\nТестирование с матрицей {size}x{size}");

//                    // Генерация данных
//                    var matrix = MatrixGenerator.GenerateMatrix(size);
//                    var vector = MatrixGenerator.GenerateVector(size);

//                    // Метрики теста
//                    var metrics = new TestMetrics();

//                    // Замер времени теста
//                    var stopwatch = Stopwatch.StartNew();
//                    var tasks = new Task[parallelRequests];

//                    for (int i = 0; i < parallelRequests; i++)
//                    {
//                        tasks[i] = Task.Run(async () =>
//                        {
//                            var requestStopwatch = Stopwatch.StartNew();
//                            bool success = await SendRequestWithSemaphoreAsync(matrix, vector);
//                            requestStopwatch.Stop();

//                            metrics.LogRequest(requestStopwatch.ElapsedMilliseconds, success);
//                        });
//                    }

//                    await Task.WhenAll(tasks);
//                    stopwatch.Stop();

//                    // Вывод результатов теста
//                    Console.WriteLine($"Время обработки {parallelRequests} запросов: {stopwatch.ElapsedMilliseconds} мс");
//                    Console.WriteLine($"Среднее время ответа: {metrics.GetAverageTime():F2} мс");
//                    Console.WriteLine($"Минимальное время ответа: {metrics.MinTime} мс");
//                    Console.WriteLine($"Максимальное время ответа: {metrics.MaxTime} мс");
//                    Console.WriteLine($"Успешных запросов: {metrics.SuccessfulRequests}");
//                    Console.WriteLine($"Ошибок: {metrics.FailedRequests}");

//                    await logWriter.WriteLineAsync($"Тест с матрицей {size}x{size}");
//                    await logWriter.WriteLineAsync($"Время обработки: {stopwatch.ElapsedMilliseconds} мс");
//                    await logWriter.WriteLineAsync($"Среднее время ответа: {metrics.GetAverageTime():F2} мс");
//                    await logWriter.WriteLineAsync($"Минимальное время: {metrics.MinTime} мс");
//                    await logWriter.WriteLineAsync($"Максимальное время: {metrics.MaxTime} мс");
//                    await logWriter.WriteLineAsync($"Успешных запросов: {metrics.SuccessfulRequests}");
//                    await logWriter.WriteLineAsync($"Ошибок: {metrics.FailedRequests}\n");

//                    // Сохранение результатов для сводного отчета
//                    results.Add(new TestResult
//                    {
//                        MatrixSize = size,
//                        ParallelRequests = parallelRequests,
//                        TotalTime = stopwatch.ElapsedMilliseconds,
//                        AverageTime = metrics.GetAverageTime(),
//                        MinTime = metrics.MinTime,
//                        MaxTime = metrics.MaxTime,
//                        SuccessfulRequests = metrics.SuccessfulRequests,
//                        FailedRequests = metrics.FailedRequests
//                    });
//                }
//            }
//            finally
//            {
//                // Завершение логирования
//                await logWriter.WriteLineAsync($"Нагрузочное тестирование завершено: {DateTime.Now}");
//                logWriter.Close();
//            }

//            // Итоговый сводный отчет
//            PrintSummary(results);

//            Console.WriteLine("Нагрузочное тестирование завершено.");
//            Console.ReadLine();
//        }

//        private static void PrintSummary(List<TestResult> results)
//        {
//            Console.WriteLine("\n=== Итоговый отчет ===");
//            Console.WriteLine("Размер матрицы | Запросов | Среднее время (мс) | Мин. время (мс) | Макс. время (мс) | Успешных | Ошибок");

//            foreach (var result in results)
//            {
//                Console.WriteLine($"{result.MatrixSize,13} | {result.ParallelRequests,8} | {result.AverageTime,18:F2} | {result.MinTime,14} | {result.MaxTime,14} | {result.SuccessfulRequests,9} | {result.FailedRequests,6}");
//            }

//            Console.WriteLine("========================");

//        }

//        private static async Task<bool> SendRequestWithSemaphoreAsync(double[][] matrix, double[] vector)
//        {
//            await _semaphore.WaitAsync();
//            try
//            {
//                return await SendRequestAsync(matrix, vector);
//            }
//            finally
//            {
//                _semaphore.Release();
//            }
//        }

//        private static async Task<bool> SendRequestAsync(double[][] matrix, double[] vector)
//        {
//            string serverAddress = "ws://localhost:5145/ws";

//            try
//            {
//                using var webSocket = new ClientWebSocket();
//                await webSocket.ConnectAsync(new Uri(serverAddress), CancellationToken.None);

//                // Создаем запрос
//                var request = new { Matrix = matrix, Vector = vector };
//                string jsonRequest = JsonSerializer.Serialize(request);
//                byte[] requestBytes = Encoding.UTF8.GetBytes(jsonRequest);

//                // Отправка данных
//                await webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, CancellationToken.None);

//                // Получение ответа
//                var buffer = new byte[4096];
//                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

//                string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
//                Console.WriteLine($"Ответ сервера: {response}");
//                await LogResponseAsync(response);
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка запроса: {ex.Message}");
//                await LogResponseAsync(null, ex.Message);
//                return false;
//            }
//        }

//        private static async Task LogResponseAsync(string response, string error = null)
//        {
//            response ??= "Нет ответа";
//            error ??= "Нет ошибки";

//            try
//            {
//                lock (_logLock)
//                {
//                    logWriter.WriteLine($"[{DateTime.Now}] Ответ: {response}, Ошибка: {error}");
//                }

//                await logWriter.FlushAsync();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
//            }
//        }
//    }

//    public static class MatrixGenerator
//    {
//        public static double[][] GenerateMatrix(int size)
//        {
//            var random = new Random();
//            var matrix = new double[size][];
//            for (int i = 0; i < size; i++)
//            {
//                matrix[i] = new double[size];
//                for (int j = 0; j < size; j++)
//                {
//                    matrix[i][j] = random.NextDouble() * 100;
//                }
//            }
//            return matrix;
//        }

//        public static double[] GenerateVector(int size)
//        {
//            var random = new Random();
//            var vector = new double[size];
//            for (int i = 0; i < size; i++)
//            {
//                vector[i] = random.NextDouble() * 100;
//            }
//            return vector;
//        }
//    }

//    public class TestMetrics
//    {
//        public long TotalTime { get; private set; }
//        public int SuccessfulRequests { get; private set; }
//        public int FailedRequests { get; private set; }
//        public long MinTime { get; private set; } = long.MaxValue;
//        public long MaxTime { get; private set; } = 0;

//        public void LogRequest(long elapsedTime, bool success)
//        {
//            if (success)
//            {
//                SuccessfulRequests++;
//                TotalTime += elapsedTime;
//                MinTime = Math.Min(MinTime, elapsedTime);
//                MaxTime = Math.Max(MaxTime, elapsedTime);
//            }
//            else
//            {
//                FailedRequests++;
//            }
//        }

//        public double GetAverageTime() => SuccessfulRequests == 0 ? 0 : (double)TotalTime / SuccessfulRequests;
//    }

//    public class TestResult
//    {
//        public int MatrixSize { get; set; }
//        public int ParallelRequests { get; set; }
//        public long TotalTime { get; set; }
//        public double AverageTime { get; set; }
//        public long MinTime { get; set; }
//        public long MaxTime { get; set; }
//        public int SuccessfulRequests { get; set; }
//        public int FailedRequests { get; set; }
//    }
//}



using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace LoadTesting
{
    class LoadTest
    {
        private static StreamWriter logWriter;
        private static readonly object _logLock = new object(); // Синхронизация логирования

        static async Task Main(string[] args)
        {
            Console.WriteLine("Нагрузочное тестирование сервера начато.");

            // Настройки теста
            int[] userCounts = { 10, 50, 100, 200, 300 }; // Количество параллельных пользователей
            int matrixSize = 100;                        // Размер матрицы (фиксированный)
            var results = new List<UserLoadResult>();

            // Инициализация логирования
            logWriter = new StreamWriter("user_load_test_log.txt", true);
            await logWriter.WriteLineAsync($"Нагрузочное тестирование начато: {DateTime.Now}");

            try
            {
                foreach (var userCount in userCounts)
                {
                    Console.WriteLine($"\nТестирование с {userCount} пользователями...");

                    // Генерация данных
                    var matrix = MatrixGenerator.GenerateMatrix(matrixSize);
                    var vector = MatrixGenerator.GenerateVector(matrixSize);

                    // Метрики теста
                    var metrics = new TestMetrics();

                    // Замер времени теста
                    var stopwatch = Stopwatch.StartNew();
                    var tasks = new Task[userCount];

                    for (int i = 0; i < userCount; i++)
                    {
                        tasks[i] = Task.Run(async () =>
                        {
                            var requestStopwatch = Stopwatch.StartNew();
                            bool success = await SendRequestAsync(matrix, vector);
                            requestStopwatch.Stop();

                            metrics.LogRequest(requestStopwatch.ElapsedMilliseconds, success);
                        });
                    }

                    await Task.WhenAll(tasks);
                    stopwatch.Stop();

                    // Вывод результатов теста
                    Console.WriteLine($"Время обработки {userCount} запросов: {stopwatch.ElapsedMilliseconds} мс");
                    Console.WriteLine($"Среднее время ответа: {metrics.GetAverageTime():F2} мс");
                    Console.WriteLine($"Минимальное время ответа: {metrics.MinTime} мс");
                    Console.WriteLine($"Максимальное время ответа: {metrics.MaxTime} мс");
                    Console.WriteLine($"Успешных запросов: {metrics.SuccessfulRequests}");
                    Console.WriteLine($"Ошибок: {metrics.FailedRequests}");

                    await logWriter.WriteLineAsync($"Тест с {userCount} пользователями:");
                    await logWriter.WriteLineAsync($"Время обработки: {stopwatch.ElapsedMilliseconds} мс");
                    await logWriter.WriteLineAsync($"Среднее время ответа: {metrics.GetAverageTime():F2} мс");
                    await logWriter.WriteLineAsync($"Минимальное время: {metrics.MinTime} мс");
                    await logWriter.WriteLineAsync($"Максимальное время: {metrics.MaxTime} мс");
                    await logWriter.WriteLineAsync($"Успешных запросов: {metrics.SuccessfulRequests}");
                    await logWriter.WriteLineAsync($"Ошибок: {metrics.FailedRequests}\n");

                    // Сохранение результатов для сводного отчета
                    results.Add(new UserLoadResult
                    {
                        UserCount = userCount,
                        TotalTime = stopwatch.ElapsedMilliseconds,
                        AverageTime = metrics.GetAverageTime(),
                        MinTime = metrics.MinTime,
                        MaxTime = metrics.MaxTime,
                        SuccessfulRequests = metrics.SuccessfulRequests,
                        FailedRequests = metrics.FailedRequests
                    });
                }
            }
            finally
            {
                // Завершение логирования
                await logWriter.WriteLineAsync($"Нагрузочное тестирование завершено: {DateTime.Now}");
                logWriter.Close();
            }

            // Итоговый сводный отчет
            PrintSummary(results);

            Console.WriteLine("Нагрузочное тестирование завершено.");
            Console.ReadLine();
        }

        private static void PrintSummary(List<UserLoadResult> results)
        {
            Console.WriteLine("\n=== Итоговый отчет ===");
            Console.WriteLine("Пользователи | Общее время (мс) | Среднее время (мс) | Мин. время (мс) | Макс. время (мс) | Успешных | Ошибок");

            foreach (var result in results)
            {
                Console.WriteLine($"{result.UserCount,12} | {result.TotalTime,16} | {result.AverageTime,18:F2} | {result.MinTime,14} | {result.MaxTime,14} | {result.SuccessfulRequests,9} | {result.FailedRequests,6}");
            }

            Console.WriteLine("========================");
        }

        private static async Task<bool> SendRequestAsync(double[][] matrix, double[] vector)
        {
            string serverAddress = "ws://localhost:5145/ws";

            try
            {
                using var webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(serverAddress), CancellationToken.None);

                // Создаем запрос
                var request = new { Matrix = matrix, Vector = vector };
                string jsonRequest = JsonSerializer.Serialize(request);
                byte[] requestBytes = Encoding.UTF8.GetBytes(jsonRequest);

                // Отправка данных
                await webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                // Получение ответа
                var buffer = new byte[4096];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class MatrixGenerator
    {
        public static double[][] GenerateMatrix(int size)
        {
            var random = new Random();
            var matrix = new double[size][];
            for (int i = 0; i < size; i++)
            {
                matrix[i] = new double[size];
                for (int j = 0; j < size; j++)
                {
                    matrix[i][j] = random.NextDouble() * 100;
                }
            }
            return matrix;
        }

        public static double[] GenerateVector(int size)
        {
            var random = new Random();
            var vector = new double[size];
            for (int i = 0; i < size; i++)
            {
                vector[i] = random.NextDouble() * 100;
            }
            return vector;
        }
    }

    public class TestMetrics
    {
        public long TotalTime { get; private set; }
        public int SuccessfulRequests { get; private set; }
        public int FailedRequests { get; private set; }
        public long MinTime { get; private set; } = long.MaxValue;
        public long MaxTime { get; private set; } = 0;

        public void LogRequest(long elapsedTime, bool success)
        {
            if (success)
            {
                SuccessfulRequests++;
                TotalTime += elapsedTime;
                MinTime = Math.Min(MinTime, elapsedTime);
                MaxTime = Math.Max(MaxTime, elapsedTime);
            }
            else
            {
                FailedRequests++;
            }
        }

        public double GetAverageTime() => SuccessfulRequests == 0 ? 0 : (double)TotalTime / SuccessfulRequests;
    }

    public class UserLoadResult
    {
        public int UserCount { get; set; }
        public long TotalTime { get; set; }
        public double AverageTime { get; set; }
        public long MinTime { get; set; }
        public long MaxTime { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
    }
}
