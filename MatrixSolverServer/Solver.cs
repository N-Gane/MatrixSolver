namespace MatrixSolverServer
{
    public static class Solver
    {
        // Уже существующий метод
        public static double[] SolveSLAU(double[][] matrix, double[] vector)
        {
            int n = vector.Length;
            double[] solution = new double[n];

            Parallel.For(0, n, i =>
            {
                solution[i] = 0;
                for (int j = 0; j < n; j++)
                {
                    solution[i] += matrix[i][j] * vector[j];
                }
            });

            return solution;
        }

        // Новый метод с ленточным перемножением
        public static double[] SolveSLAUWithStripeMultiplication(double[][] matrix, double[] vector)
        {
            int n = matrix.Length;
            double[] solution = new double[n];

            // Ленточное перемножение матриц (разделение на полосы)
            Parallel.For(0, n, i =>
            {
                for (int j = 0; j < n; j++)
                {
                    solution[i] += matrix[i][j] * vector[j];
                }
            });

            // Обратная подстановка для решения
            for (int i = n - 1; i >= 0; i--)
            {
                solution[i] /= matrix[i][i];
                for (int j = i - 1; j >= 0; j--)
                {
                    solution[j] -= matrix[j][i] * solution[i];
                }
            }

            return solution;
        }
    }
}
