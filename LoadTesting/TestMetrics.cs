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
