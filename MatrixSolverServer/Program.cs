using MatrixSolverServer;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ��������� ������������� WebSocket-��������
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("����� WebSocket-���������� �����������.");
        await HandleWebSocketConnection(webSocket);
    }
    else
    {
        await next();
    }
});

// ���������� WebSocket-����������
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
                Console.WriteLine($"�������� ��������� �� �������: {receivedMessage}");

                // �������������� �������� ������
                var request = JsonSerializer.Deserialize<MatrixRequest>(receivedMessage);

                // ������� ���� � ��������� �������������
                var solution = Solver.SolveSLAUWithStripeMultiplication(request.Matrix, request.Vector);

                // ��������� JSON-�����
                var responseJson = JsonSerializer.Serialize(solution);
                var responseMessage = Encoding.UTF8.GetBytes(responseJson);

                // ���������� ��������� �������
                await webSocket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("������� ���������� �������.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"������ ��������� JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��������� ������: {ex.Message}");
            }

            // ������� ��������� ������
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        Console.WriteLine("���������� WebSocket �������.");
    }
    catch (WebSocketException ex)
    {
        Console.WriteLine($"WebSocketException: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"����������� ������: {ex.Message}");
    }
}

app.Run("http://localhost:5145");


