namespace BMS.Core.CalcHelpers;

/// <summary>
/// Lightweight dense matrix operations for Kalman filtering (replaces numpy/scipy in bmslogic).
/// </summary>
public static class MatrixOps
{
    public static double[,] Identity(int n)
    {
        var m = new double[n, n];
        for (int i = 0; i < n; i++) m[i, i] = 1.0;
        return m;
    }

    public static double[,] Diag(double[] values)
    {
        int n = values.Length;
        var m = new double[n, n];
        for (int i = 0; i < n; i++) m[i, i] = values[i];
        return m;
    }

    public static double[,] Transpose(double[,] a)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        var res = new double[cols, rows];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                res[j, i] = a[i, j];
        return res;
    }

    public static double[,] Multiply(double[,] a, double[,] b)
    {
        int rA = a.GetLength(0);
        int cA = a.GetLength(1);
        int rB = b.GetLength(0);
        int cB = b.GetLength(1);
        if (cA != rB)
            throw new ArgumentException($"Incompatible matrix dimensions: ({rA}x{cA}) * ({rB}x{cB}).");

        var res = new double[rA, cB];
        for (int i = 0; i < rA; i++)
        {
            for (int k = 0; k < cA; k++)
            {
                double aik = a[i, k];
                for (int j = 0; j < cB; j++)
                    res[i, j] += aik * b[k, j];
            }
        }
        return res;
    }

    public static double[,] Add(double[,] a, double[,] b)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        var res = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                res[i, j] = a[i, j] + b[i, j];
        return res;
    }

    public static double[,] Subtract(double[,] a, double[,] b)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        var res = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                res[i, j] = a[i, j] - b[i, j];
        return res;
    }

    /// <summary>
    /// Lower-triangular Cholesky decomposition L such that L * L^T = A (with tiny jitter fallback for numerical stability).
    /// </summary>
    public static double[,] CholeskyLower(double[,] a)
    {
        int n = a.GetLength(0);
        var l = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                double sum = 0.0;
                for (int k = 0; k < j; k++)
                    sum += l[i, k] * l[j, k];

                if (i == j)
                {
                    double val = a[i, i] - sum;
                    l[i, j] = val > 1e-15 ? Math.Sqrt(val) : 1e-8;
                }
                else
                {
                    l[i, j] = (a[i, j] - sum) / l[j, j];
                }
            }
        }
        return l;
    }

    /// <summary>
    /// Matrix inverse using Gauss-Jordan elimination with partial pivoting.
    /// </summary>
    public static double[,] Inverse(double[,] a)
    {
        int n = a.GetLength(0);
        var aug = new double[n, 2 * n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                aug[i, j] = a[i, j];
            aug[i, n + i] = 1.0;
        }

        for (int col = 0; col < n; col++)
        {
            int pivotRow = col;
            double maxVal = Math.Abs(aug[col, col]);
            for (int r = col + 1; r < n; r++)
            {
                if (Math.Abs(aug[r, col]) > maxVal)
                {
                    maxVal = Math.Abs(aug[r, col]);
                    pivotRow = r;
                }
            }

            if (maxVal < 1e-15)
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

            if (pivotRow != col)
            {
                for (int c = 0; c < 2 * n; c++)
                    (aug[col, c], aug[pivotRow, c]) = (aug[pivotRow, c], aug[col, c]);
            }

            double pivot = aug[col, col];
            for (int c = 0; c < 2 * n; c++)
                aug[col, c] /= pivot;

            for (int r = 0; r < n; r++)
            {
                if (r == col) continue;
                double factor = aug[r, col];
                for (int c = 0; c < 2 * n; c++)
                    aug[r, c] -= factor * aug[col, c];
            }
        }

        var inv = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                inv[i, j] = aug[i, n + j];
        return inv;
    }
}
