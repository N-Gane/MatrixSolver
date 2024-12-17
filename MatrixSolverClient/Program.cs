using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace MatrixSolverClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            // Обслуживание веб-страницы клиента
            app.UseDefaultFiles(); // Позволяет открывать index.html автоматически
            app.UseStaticFiles();  // Обслуживание статических файлов

            Console.WriteLine("Клиентский интерфейс доступен по адресу: http://localhost:8080");
            app.Run("http://localhost:8080");
        }
    }
}
