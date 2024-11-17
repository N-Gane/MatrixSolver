using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var uri = new Uri("ws://localhost:5145");
        using var client = new ClientWebSocket();

        try
        {
            Console.WriteLine("Подключение к серверу...");
            await client.ConnectAsync(uri, CancellationToken.None);
            Console.WriteLine("Соединение установлено.");

            // Исходные данные
            var matrix = new double[][]
            {
                new double[] { 2, 1 },
                new double[] { 5, 7 }
            };
            var vector = new double[] { 11, 13 };

            // Формируем JSON-запрос
            var request = new { Matrix = matrix, Vector = vector };
            var requestJson = JsonSerializer.Serialize(request);
            var bytesToSend = Encoding.UTF8.GetBytes(requestJson);

            // Отправка данных серверу
            await client.SendAsync(new ArraySegment<byte>(bytesToSend), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Запрос отправлен: {requestJson}");

            // Получение решения от сервера
            var buffer = new byte[1024 * 4];
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // Десериализация ответа
            var solution = JsonSerializer.Deserialize<double[]>(responseJson);
            Console.WriteLine("Решение СЛАУ:");
            Console.WriteLine(string.Join(", ", solution));
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocketException: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
        }
        finally
        {
            if (client.State == WebSocketState.Open)
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие соединения", CancellationToken.None);
                Console.WriteLine("Соединение WebSocket закрыто.");
            }
        }
    }
}
