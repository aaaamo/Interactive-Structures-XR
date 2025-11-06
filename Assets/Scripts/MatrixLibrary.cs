using UnityEngine;

public class MatrixMxN
{
    public float[,] data;
    public int rows;
    public int cols;

    public MatrixMxN(int m, int n)
    {
        rows = m;
        cols = n;
        data = new float[m, n];
    }

    public void Zero()
    {
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                data[i, j] = 0f;
    }

    public void Identity()
    {
        Zero();
        int n = Mathf.Min(rows, cols);
        for (int i = 0; i < n; i++)
            data[i, i] = 1f;
    }

    public MatrixMxN Clone()
    {
        MatrixMxN result = new MatrixMxN(rows, cols);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result.data[i, j] = data[i, j];
        return result;
    }

    public static MatrixMxN Multiply(MatrixMxN A, MatrixMxN B)
    {
        if (A.cols != B.rows)
        {
            Debug.LogError("Matrix dimensions incompatible for multiplication");
            return null;
        }

        MatrixMxN result = new MatrixMxN(A.rows, B.cols);
        for (int i = 0; i < A.rows; i++)
        {
            for (int j = 0; j < B.cols; j++)
            {
                float sum = 0f;
                for (int k = 0; k < A.cols; k++)
                    sum += A.data[i, k] * B.data[k, j];
                result.data[i, j] = sum;
            }
        }
        return result;
    }

    public static float[] Multiply(MatrixMxN A, float[] x)
    {
        if (A.cols != x.Length)
        {
            Debug.LogError("Matrix-vector dimensions incompatible");
            return null;
        }

        float[] result = new float[A.rows];
        for (int i = 0; i < A.rows; i++)
        {
            float sum = 0f;
            for (int j = 0; j < A.cols; j++)
                sum += A.data[i, j] * x[j];
            result[i] = sum;
        }
        return result;
    }

    public static MatrixMxN Multiply(MatrixMxN A, float scalar)
    {
        MatrixMxN result = new MatrixMxN(A.rows, A.cols);
        for (int i = 0; i < A.rows; i++)
            for (int j = 0; j < A.cols; j++)
                result.data[i, j] = A.data[i, j] * scalar;
        return result;
    }

    public static MatrixMxN Transpose(MatrixMxN A)
    {
        MatrixMxN result = new MatrixMxN(A.cols, A.rows);
        for (int i = 0; i < A.rows; i++)
            for (int j = 0; j < A.cols; j++)
                result.data[j, i] = A.data[i, j];
        return result;
    }

    public float AbsMax()
    {
        float maxVal = float.NegativeInfinity;
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (Mathf.Abs(data[i, j]) > maxVal)
                    maxVal = data[i, j];
        return maxVal;
    }
    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Matrix ({rows}x{cols}):");
        for (int i = 0; i < rows; i++)
        {
            sb.Append("[ ");
            for (int j = 0; j < cols; j++)
            {
                sb.AppendFormat("{0,8:0.###}", data[i, j]);
                if (j < cols - 1) sb.Append(", ");
            }
            sb.AppendLine(" ]");
        }
        return sb.ToString();
    }

}

public static class MatrixSolver
{
    /// <summary>
    /// Solves Ax = b using Gaussian elimination with partial pivoting
    /// </summary>
    public static float[] SolveLinearSystem(MatrixMxN A, float[] b)
    {
        if (A.rows != A.cols)
        {
            Debug.LogError("Matrix must be square");
            return null;
        }

        if (A.rows != b.Length)
        {
            Debug.LogError("Matrix and vector dimensions don't match");
            return null;
        }

        int n = A.rows;

        // Create augmented matrix [A|b]
        float[,] aug = new float[n, n + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                aug[i, j] = A.data[i, j];
            aug[i, n] = b[i];
        }

        // Forward elimination with partial pivoting
        for (int col = 0; col < n; col++)
        {
            // Find pivot
            int pivotRow = col;
            float maxVal = Mathf.Abs(aug[col, col]);

            for (int row = col + 1; row < n; row++)
            {
                float val = Mathf.Abs(aug[row, col]);
                if (val > maxVal)
                {
                    maxVal = val;
                    pivotRow = row;
                }
            }

            // Check for singular matrix
            if (maxVal < 1e-10f)
            {
                Debug.LogError($"Matrix is singular or near-singular at column {col}");
                return null;
            }

            // Swap rows if needed
            if (pivotRow != col)
            {
                for (int j = 0; j <= n; j++)
                {
                    float temp = aug[col, j];
                    aug[col, j] = aug[pivotRow, j];
                    aug[pivotRow, j] = temp;
                }
            }

            // Eliminate below pivot
            for (int row = col + 1; row < n; row++)
            {
                float factor = aug[row, col] / aug[col, col];
                for (int j = col; j <= n; j++)
                {
                    aug[row, j] -= factor * aug[col, j];
                }
            }
        }

        // Back substitution
        float[] x = new float[n];
        for (int i = n - 1; i >= 0; i--)
        {
            float sum = aug[i, n];
            for (int j = i + 1; j < n; j++)
            {
                sum -= aug[i, j] * x[j];
            }

            if (Mathf.Abs(aug[i, i]) < 1e-10f)
            {
                Debug.LogError($"Division by zero at row {i}");
                return null;
            }

            x[i] = sum / aug[i, i];
        }

        return x;
    }

    /// <summary>
    /// Inverts a matrix using Gauss-Jordan elimination
    /// </summary>
    public static MatrixMxN Invert(MatrixMxN A)
    {
        if (A.rows != A.cols)
        {
            Debug.LogError("Matrix must be square to invert");
            return null;
        }

        int n = A.rows;

        // Create augmented matrix [A|I]
        float[,] aug = new float[n, 2 * n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                aug[i, j] = A.data[i, j];
            aug[i, n + i] = 1f;
        }

        // Gauss-Jordan elimination
        for (int col = 0; col < n; col++)
        {
            // Find pivot
            int pivotRow = col;
            float maxVal = Mathf.Abs(aug[col, col]);

            for (int row = col + 1; row < n; row++)
            {
                float val = Mathf.Abs(aug[row, col]);
                if (val > maxVal)
                {
                    maxVal = val;
                    pivotRow = row;
                }
            }

            if (maxVal < 1e-10f)
            {
                Debug.LogError("Matrix is singular, cannot invert");
                return null;
            }

            // Swap rows
            if (pivotRow != col)
            {
                for (int j = 0; j < 2 * n; j++)
                {
                    float temp = aug[col, j];
                    aug[col, j] = aug[pivotRow, j];
                    aug[pivotRow, j] = temp;
                }
            }

            // Scale pivot row
            float pivot = aug[col, col];
            for (int j = 0; j < 2 * n; j++)
                aug[col, j] /= pivot;

            // Eliminate column
            for (int row = 0; row < n; row++)
            {
                if (row != col)
                {
                    float factor = aug[row, col];
                    for (int j = 0; j < 2 * n; j++)
                        aug[row, j] -= factor * aug[col, j];
                }
            }
        }

        // Extract inverse from right side
        MatrixMxN inv = new MatrixMxN(n, n);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                inv.data[i, j] = aug[i, n + j];

        return inv;
    }

    /// <summary>
    /// Computes the determinant using LU decomposition
    /// </summary>
    public static float Determinant(MatrixMxN A)
    {
        if (A.rows != A.cols)
        {
            Debug.LogError("Matrix must be square");
            return 0f;
        }

        int n = A.rows;
        float[,] m = new float[n, n];

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                m[i, j] = A.data[i, j];

        float det = 1f;
        int swaps = 0;

        for (int col = 0; col < n; col++)
        {
            int pivotRow = col;
            float maxVal = Mathf.Abs(m[col, col]);

            for (int row = col + 1; row < n; row++)
            {
                float val = Mathf.Abs(m[row, col]);
                if (val > maxVal)
                {
                    maxVal = val;
                    pivotRow = row;
                }
            }

            if (maxVal < 1e-10f)
                return 0f;

            if (pivotRow != col)
            {
                swaps++;
                for (int j = 0; j < n; j++)
                {
                    float temp = m[col, j];
                    m[col, j] = m[pivotRow, j];
                    m[pivotRow, j] = temp;
                }
            }

            det *= m[col, col];

            for (int row = col + 1; row < n; row++)
            {
                float factor = m[row, col] / m[col, col];
                for (int j = col; j < n; j++)
                    m[row, j] -= factor * m[col, j];
            }
        }

        if (swaps % 2 == 1)
            det = -det;

        return det;
    }
}