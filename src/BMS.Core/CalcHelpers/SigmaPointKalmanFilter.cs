namespace BMS.Core.CalcHelpers;

/// <summary>
/// Normally distributed random vector (mean nx1 and covariance nxn), ported from bmslogic.
/// </summary>
public class NormalRandomVector
{
    public double[,] Vector { get; set; }
    public double[,] Cov { get; set; }
    public int Dim => Vector.GetLength(0);

    public NormalRandomVector(double[,] vectorInit, double[,] covInit)
    {
        if (vectorInit.GetLength(1) != 1)
            throw new ArgumentException("Mean vector must be nx1.", nameof(vectorInit));
        int n = vectorInit.GetLength(0);
        if (covInit.GetLength(0) != n || covInit.GetLength(1) != n)
            throw new ArgumentException("Covariance matrix must be nxn matching vector dimension.", nameof(covInit));

        Vector = (double[,])vectorInit.Clone();
        Cov = (double[,])covInit.Clone();
    }

    public static NormalRandomVector FromScalar(double mean, double variance) =>
        new(new double[,] { { mean } }, new double[,] { { variance } });
}

/// <summary>
/// Central-Difference Sigma-Point Kalman Filter (CDKF), ported from bmslogic/calc_helpers/kalman_filter.py.
/// </summary>
public class SigmaPointKalmanFilter
{
    private readonly NormalRandomVector _x;
    private readonly NormalRandomVector _w;
    private readonly NormalRandomVector _v;
    private readonly int _yDim;
    private readonly Func<double[,], double, double[,], double[,]> _stateEquation;
    private readonly Func<double[,], double, double[,], double[,]> _outputEquation;

    public int Nx => _x.Dim;
    public int Nw => _w.Dim;
    public int Nv => _v.Dim;
    public int L => Nx + Nw + Nv;
    public int P => 2 * L;
    public double H => Math.Sqrt(3.0);
    public double Gamma => Math.Sqrt(3.0);

    public double[,] State => _x.Vector;
    public double[,] Covariance => _x.Cov;

    public SigmaPointKalmanFilter(
        NormalRandomVector x,
        NormalRandomVector w,
        NormalRandomVector v,
        int yDim,
        Func<double[,], double, double[,], double[,]> stateEquation,
        Func<double[,], double, double[,], double[,]> outputEquation)
    {
        _x = x ?? throw new ArgumentNullException(nameof(x));
        _w = w ?? throw new ArgumentNullException(nameof(w));
        _v = v ?? throw new ArgumentNullException(nameof(v));
        _yDim = yDim;
        _stateEquation = stateEquation ?? throw new ArgumentNullException(nameof(stateEquation));
        _outputEquation = outputEquation ?? throw new ArgumentNullException(nameof(outputEquation));
    }

    private double[,] BuildAlphaM()
    {
        double alphaM0 = (H * H - L) / (H * H);
        double alphaM = 1.0 / (2.0 * H * H);
        var vec = new double[P + 1, 1];
        vec[0, 0] = alphaM0;
        for (int i = 1; i <= P; i++) vec[i, 0] = alphaM;
        return vec;
    }

    private double[] BuildAlphaC()
    {
        double alphaC0 = (H * H - L) / (H * H);
        double alphaC = 1.0 / (2.0 * H * H);
        var arr = new double[P + 1];
        arr[0] = alphaC0;
        for (int i = 1; i <= P; i++) arr[i] = alphaC;
        return arr;
    }

    private double[,] BuildAugmentedVector()
    {
        var aug = new double[L, 1];
        for (int i = 0; i < Nx; i++) aug[i, 0] = _x.Vector[i, 0];
        for (int i = 0; i < Nw; i++) aug[Nx + i, 0] = _w.Vector[i, 0];
        for (int i = 0; i < Nv; i++) aug[Nx + Nw + i, 0] = _v.Vector[i, 0];
        return aug;
    }

    private double[,] BuildAugmentedCov()
    {
        var cov = new double[L, L];
        for (int i = 0; i < Nx; i++)
            for (int j = 0; j < Nx; j++)
                cov[i, j] = _x.Cov[i, j];
        for (int i = 0; i < Nw; i++)
            for (int j = 0; j < Nw; j++)
                cov[Nx + i, Nx + j] = _w.Cov[i, j];
        for (int i = 0; i < Nv; i++)
            for (int j = 0; j < Nv; j++)
                cov[Nx + Nw + i, Nx + Nw + j] = _v.Cov[i, j];
        return cov;
    }

    private double[,] BuildSigmaPoints()
    {
        var augVec = BuildAugmentedVector();
        var sqrtCov = MatrixOps.CholeskyLower(BuildAugmentedCov());
        var sp = new double[L, P + 1];

        for (int col = 0; col <= P; col++)
        {
            for (int row = 0; row < L; row++)
            {
                double delta = 0.0;
                if (col >= 1 && col <= L)
                    delta = Gamma * sqrtCov[row, col - 1];
                else if (col > L)
                    delta = -Gamma * sqrtCov[row, col - L - 1];

                sp[row, col] = augVec[row, 0] + delta;
            }
        }
        return sp;
    }

    private static double[,] SliceRows(double[,] m, int startRow, int count)
    {
        int cols = m.GetLength(1);
        var res = new double[count, cols];
        for (int i = 0; i < count; i++)
            for (int j = 0; j < cols; j++)
                res[i, j] = m[startRow + i, j];
        return res;
    }

    private static double[,] TileColumn(double[,] colVec, int numCols)
    {
        int rows = colVec.GetLength(0);
        var res = new double[rows, numCols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < numCols; j++)
                res[i, j] = colVec[i, 0];
        return res;
    }

    /// <summary>
    /// Executes one predict-and-update CDKF step given input u and measured output yTrue.
    /// </summary>
    public void Solve(double u, double[,] yTrue)
    {
        var sp = BuildSigmaPoints();
        var spX = SliceRows(sp, 0, Nx);
        var spW = SliceRows(sp, Nx, Nw);
        var spV = SliceRows(sp, Nx + Nw, Nv);

        var alphaM = BuildAlphaM();
        var alphaCDiag = MatrixOps.Diag(BuildAlphaC());

        // Step 1a: State prediction
        var xx = _stateEquation(spX, u, spW);
        _x.Vector = MatrixOps.Multiply(xx, alphaM);

        // Step 1b: Covariance prediction
        var xs = MatrixOps.Subtract(xx, TileColumn(_x.Vector, P + 1));
        _x.Cov = MatrixOps.Multiply(MatrixOps.Multiply(xs, alphaCDiag), MatrixOps.Transpose(xs));

        // Step 1c: Output prediction
        var y = _outputEquation(xx, u, spV);
        var yHat = MatrixOps.Multiply(y, alphaM);

        // Step 2a: Gain matrix (with Tikhonov regularization for numerical stability)
        var ys = MatrixOps.Subtract(y, TileColumn(yHat, P + 1));
        var sigmaXY = MatrixOps.Multiply(MatrixOps.Multiply(xs, alphaCDiag), MatrixOps.Transpose(ys));
        var sigmaY = MatrixOps.Multiply(MatrixOps.Multiply(ys, alphaCDiag), MatrixOps.Transpose(ys));
        for (int i = 0; i < _yDim; i++)
            sigmaY[i, i] += 1e-9;
        var lx = MatrixOps.Multiply(sigmaXY, MatrixOps.Inverse(sigmaY));

        // Step 2b: State update
        var innovation = MatrixOps.Subtract(yTrue, yHat);
        _x.Vector = MatrixOps.Add(_x.Vector, MatrixOps.Multiply(lx, innovation));

        // Step 2c: Covariance update (enforce positive diagonal floor)
        var update = MatrixOps.Multiply(MatrixOps.Multiply(lx, sigmaY), MatrixOps.Transpose(lx));
        _x.Cov = MatrixOps.Subtract(_x.Cov, update);
        for (int i = 0; i < Nx; i++)
        {
            if (_x.Cov[i, i] < 1e-9)
                _x.Cov[i, i] = 1e-9;
        }
    }

    public void Solve(double u, double yTrueScalar) =>
        Solve(u, new double[,] { { yTrueScalar } });
}
