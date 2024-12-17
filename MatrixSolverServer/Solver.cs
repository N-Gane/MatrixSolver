using System;
using System.Threading.Tasks;

namespace MatrixSolverServer
{
    public static class Solver
    {
        // Метод для решения СЛАУ с ленточным перемножением
        public static double[] SolveSLAUWithStripeMultiplication(double[][] matrix, double[] vector)
        {
            int n = matrix.Length;
            double[] solution = new double[n];

            // Прямой ход с ленточным подходом
            for (int k = 0; k < n - 1; k++)
            {
                if (Math.Abs(matrix[k][k]) < 1e-10)
                {
                    throw new ArgumentException("Нулевой элемент на диагонали. Решение невозможно.");
                }

                // Нормализация текущей строки
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

            // Обратный ход (подстановка)
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
    }
}

