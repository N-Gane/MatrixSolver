using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class MatrixSolverBenchmarks
{
    private double[][] matrix;
    private double[] vector;

    [GlobalSetup]
    public void Setup()
    {
        matrix = new double[][] {
            new double[] { 4, -1, 0, 0, 0, 0 },
            new double[] { -1, 4, -1, 0, 0, 0 },
            new double[] { 0, -1, 4, -1, 0, 0 },
            new double[] { 0, 0, -1, 4, -1, 0 },
            new double[] { 0, 0, 0, -1, 4, -1 },
            new double[] { 0, 0, 0, 0, -1, 3 }
        };
        vector = new double[] { 2, 1, 0, 1, 2, 3 };
    }

    [Benchmark]
    public double[] BenchmarkSolveSLAUWithStripeMultiplication()
    {
        return Solver.SolveSLAUWithStripeMultiplication(matrix, vector);
    }

    [Benchmark]
    public double[] BenchmarkSolveSLAUWithGauss()
    {
        return Solver.SolveSLAUWithGauss(matrix, vector);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<MatrixSolverBenchmarks>();
    }
}
