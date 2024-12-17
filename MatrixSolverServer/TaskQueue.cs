using System.Collections.Concurrent;
using System.Threading.Tasks;

public class TaskQueue
{
    private readonly SemaphoreSlim _semaphore; // Ограничивает количество одновременно выполняемых задач
    private readonly ConcurrentQueue<Func<Task>> _queue; // Очередь задач

    public TaskQueue(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency); // Ограничение на количество одновременных задач
        _queue = new ConcurrentQueue<Func<Task>>();
    }

    public async Task Enqueue(Func<Task> taskFunc)
    {
        // Добавление задачи в очередь
        _queue.Enqueue(taskFunc);

        // Ожидание освобождения слота для выполнения задачи
        await _semaphore.WaitAsync();

        // Если задача доступна, выполняем её
        if (_queue.TryDequeue(out var taskToExecute))
        {
            try
            {
                await taskToExecute();
            }
            finally
            {
                // Освобождаем слот после завершения задачи
                _semaphore.Release();
            }
        }
    }
}
