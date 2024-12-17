using System;
using Xunit;
using MatrixSolverServer; // Директива для доступа к классу Solver
using System.Collections.Generic;
using static MatrixSolverServer.Program;

public class MatrixSolverTests
{
    private readonly SolverService _solverService;

    public MatrixSolverTests()
    {
        _solverService = new SolverService();
    }

    private void AssertArraysEqual(double[] expected, double[] actual, int precision = 5)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], precision);
        }
    }

    [Fact]
    public void SolveSLAUWithStripeMultiplication_ValidInput_ReturnsExpectedSolution()
    {
        var matrix = new double[][]
        {
                new double[] { 2, -1, 0 },
                new double[] { -1, 2, -1 },
                new double[] { 0, -1, 2 }
        };
        var vector = new double[] { 1, 0, 1 };
        var expected = new double[] { 1, 1, 1 };

        var result = _solverService.SolveSLAUWithStripeMultiplication(matrix, vector);

        AssertArraysEqual(expected, result);
    }

   

    [Fact]
    public void SolveSLAU_IdentityMatrix_ReturnsSameVector()
    {
        var matrix = new double[][]
        {
                new double[] { 1, 0, 0 },
                new double[] { 0, 1, 0 },
                new double[] { 0, 0, 1 }
        };
        var vector = new double[] { 5, -3, 10 };

        var stripeResult = _solverService.SolveSLAUWithStripeMultiplication(matrix, vector);
       
        AssertArraysEqual(vector, stripeResult);
          }

    [Fact]
    public void SolveSLAU_ZeroVector_ReturnsZeroVector()
    {
        var matrix = new double[][]
        {
                new double[] { 3, -1, 1 },
                new double[] { 2, 4, 1 },
                new double[] { 1, -2, 5 }
        };
        var vector = new double[] { 0, 0, 0 };
        var expected = new double[] { 0, 0, 0 };

        var stripeResult = _solverService.SolveSLAUWithStripeMultiplication(matrix, vector);
      

        AssertArraysEqual(expected, stripeResult);
       
    }

   

    [Fact]
    public void SolveSLAU_LargeValues_ReturnsCorrectSolution()
    {
        var matrix = new double[][]
        {
                new double[] { 1e10, 2, 3 },
                new double[] { 2, 1e10, 4 },
                new double[] { 3, 4, 1e10 }
        };
        var vector = new double[] { 1e10, 1e10, 1e10 };
        var expected = new double[] { 1, 1, 1 };

        var stripeResult = _solverService.SolveSLAUWithStripeMultiplication(matrix, vector);
       

        AssertArraysEqual(expected, stripeResult);
       
    }

   
    [Fact]
    public void ValidateMatrixRequest_EmptyMatrix_ThrowsException()
    {
        var request = new MatrixRequest
        {
            Matrix = new double[][] { },
            Vector = new double[] { }
        };

        Assert.Throws<ArgumentException>(() => _solverService.ValidateMatrixRequest(request));
    }

    [Fact]
    public void ValidateMatrixRequest_MatrixVectorSizeMismatch_ThrowsException()
    {
        var request = new MatrixRequest
        {
            Matrix = new double[][]
            {
                    new double[] { 1, 2 },
                    new double[] { 3, 4 }
            },
            Vector = new double[] { 1 }
        };

        Assert.Throws<ArgumentException>(() => _solverService.ValidateMatrixRequest(request));
    }
}
