using MatrixSolverServer;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Настройка прослушивания WebSocket-запросов
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Новое WebSocket-соединение установлено.");
        await HandleWebSocketConnection(webSocket);
    }
    else
    {
        await next();
    }
});

// Обработчик WebSocket-соединений
async Task HandleWebSocketConnection(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    try
    {
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            try
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Получено сообщение от клиента: {receivedMessage}");

                // Десериализация входящих данных
                var request = JsonSerializer.Deserialize<MatrixRequest>(receivedMessage);

                // Решение СЛАУ с ленточным перемножением
                var solution = Solver.SolveSLAUWithStripeMultiplication(request.Matrix, request.Vector);

                // Формируем JSON-ответ
                var responseJson = JsonSerializer.Serialize(solution);
                var responseMessage = Encoding.UTF8.GetBytes(responseJson);

                // Отправляем результат клиенту
                await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Решение отправлено клиенту.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка обработки JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки данных: {ex.Message}");
            }

            // Слушаем следующий запрос
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        Console.WriteLine("Соединение WebSocket закрыто.");
    }
    catch (WebSocketException ex)
    {
        Console.WriteLine($"WebSocketException: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
    }
}

app.Run("http://localhost:5145");


