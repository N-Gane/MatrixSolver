using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using OpenTelemetry;

namespace MatrixSolverServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<ISolverService, SolverService>();

            var app = builder.Build();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.Map("/ws", WebSocketHandler);

            string serverUrl = "http://localhost:5145";
            Console.WriteLine($"Сервер запущен по адресу: {serverUrl}");
            app.Run(serverUrl);
        }

        private static readonly TaskQueue TaskQueue = new TaskQueue(5); // Пулинг задач с ограничением на 5 одновременных задач

        private static async Task WebSocketHandler(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("WebSocket соединение установлено.");

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // Таймаут на 5 минут для сеанса
                try
                {
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    using var ms = new MemoryStream();

                    // Получение данных
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, cts.Token);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    var requestJson = Encoding.UTF8.GetString(ms.ToArray());
                    var solverService = context.RequestServices.GetRequiredService<ISolverService>();

                    // Валидация JSON
                    if (string.IsNullOrWhiteSpace(requestJson))
                    {
                        throw new ArgumentException("Получен пустой запрос.");
                    }

                    // Добавление задачи в очередь
                    await TaskQueue.Enqueue(async () =>
                    {
                        try
                        {
                            var request = JsonSerializer.Deserialize<MatrixRequest>(requestJson);
                            solverService.ValidateMatrixRequest(request);

                            // Выполняем вычисления
                            var stripeSolutionTask = Task.Run(() => solverService.SolveSLAUWithStripeMultiplication(request.Matrix, request.Vector));
                          //  var gaussSolutionTask = Task.Run(() => solverService.SolveSLAUWithGauss(request.Matrix, request.Vector));

                            await Task.WhenAll(stripeSolutionTask);
                           // await Task.WhenAll(stripeSolutionTask, gaussSolutionTask);

                            var response = new
                            {
                                StripeSolution = stripeSolutionTask.Result,
                              //  GaussSolution = gaussSolutionTask.Result
                            };
                            var responseJson = JsonSerializer.Serialize(response);
                            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                            await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cts.Token);
                            logger.LogInformation("Результаты отправлены клиенту.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Ошибка при обработке задачи.");
                            var errorBytes = Encoding.UTF8.GetBytes($"{{\"Error\": \"{ex.Message}\"}}");
                            await webSocket.SendAsync(new ArraySegment<byte>(errorBytes), WebSocketMessageType.Text, true, cts.Token);
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("WebSocket сеанс завершён из-за таймаута.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сеанс завершён из-за таймаута", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Необработанная ошибка.");
                }
                finally
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сеанс завершён", CancellationToken.None);
                    }
                    logger.LogInformation("WebSocket соединение закрыто.");
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        public class MatrixRequest
        {
            public double[][] Matrix { get; set; }
            public double[] Vector { get; set; }
        }

        // Интерфейс сервиса
        public interface ISolverService
        {
            double[] SolveSLAUWithStripeMultiplication(double[][] matrix, double[] vector);
           // double[] SolveSLAUWithGauss(double[][] matrix, double[] vector);
            void ValidateMatrixRequest(MatrixRequest request);
        }

        // Реализация сервиса
        public class SolverService : ISolverService
        {
            public double[] SolveSLAUWithStripeMultiplication(double[][] matrix, double[] vector)
            {
                ValidateMatrix(matrix, vector);

                int n = matrix.Length;
                double[] solution = new double[n];

                for (int k = 0; k < n - 1; k++)
                {
                    if (Math.Abs(matrix[k][k]) < 1e-10)
                        throw new ArgumentException("Нулевой элемент на диагонали. Решение невозможно.");

                    Parallel.For(k + 1, n, i =>
                    {
                        double factor = matrix[i][k] / matrix[k][k];
                        for (int j = k; j < n; j++)
                        {
                            matrix[i][j] -= factor * matrix[k][j];
                        }
                        vector[i] -= factor * vector[k];
                    });
                }

                for (int i = n - 1; i >= 0; i--)
                {
                    solution[i] = vector[i];
                    for (int j = i + 1; j < n; j++)
                    {
                        solution[i] -= matrix[i][j] * solution[j];
                    }
                    solution[i] /= matrix[i][i];
                }

                return solution;
            }

            //public double[] SolveSLAUWithGauss(double[][] matrix, double[] vector)
            //{
            //    ValidateMatrix(matrix, vector);

            //    int n = matrix.Length;
            //    double[] solution = new double[n];
            //    double[][] augmentedMatrix = new double[n][];
            //    for (int i = 0; i < n; i++)
            //    {
            //        augmentedMatrix[i] = new double[n + 1];
            //        Array.Copy(matrix[i], augmentedMatrix[i], n);
            //        augmentedMatrix[i][n] = vector[i];
            //    }

            //    for (int k = 0; k < n; k++)
            //    {
            //        if (Math.Abs(augmentedMatrix[k][k]) < 1e-10)
            //            throw new DivideByZeroException("Нулевой элемент на диагонали.");

            //        for (int i = k + 1; i < n; i++)
            //        {
            //            double factor = augmentedMatrix[i][k] / augmentedMatrix[k][k];
            //            for (int j = k; j <= n; j++)
            //            {
            //                augmentedMatrix[i][j] -= factor * augmentedMatrix[k][j];
            //            }
            //        }
            //    }

            //    for (int i = n - 1; i >= 0; i--)
            //    {
            //        solution[i] = augmentedMatrix[i][n];
            //        for (int j = i + 1; j < n; j++)
            //        {
            //            solution[i] -= augmentedMatrix[i][j] * solution[j];
            //        }
            //        solution[i] /= augmentedMatrix[i][i];
            //    }

            //    return solution;
            //}

            public void ValidateMatrixRequest(MatrixRequest request)
            {
                if (request == null)
                    throw new ArgumentNullException("Запрос не должен быть null.");

                ValidateMatrix(request.Matrix, request.Vector);
            }

            private void ValidateMatrix(double[][] matrix, double[] vector)
            {
                if (matrix == null || vector == null)
                    throw new ArgumentNullException("Матрица и вектор не должны быть null.");

                int n = matrix.Length;
                if (n == 0 || vector.Length != n)
                    throw new ArgumentException("Размеры матрицы и вектора не совпадают или матрица пуста.");

                foreach (var row in matrix)
                {
                    if (row.Length != n)
                        throw new ArgumentException("Матрица должна быть квадратной.");
                }

                if (IsInconsistent(matrix, vector))
                    throw new ArgumentException("Система несовместна.");
            }

            private static bool IsInconsistent(double[][] matrix, double[] vector)
            {
                int n = matrix.Length;
                for (int i = 0; i < n; i++)
                {
                    if (IsZeroRow(matrix[i]) && Math.Abs(vector[i]) > 1e-10)
                    {
                        return true; // несовместимая система
                    }
                }
                return false;
            }

            private static bool IsZeroRow(double[] row)
            {
                return row.All(val => Math.Abs(val) < 1e-10);
            }
        }
    }
}
