#if false
using System;
using System.Linq;
using ILGPU;
using ILGPU.Runtime;

public class GpuLowessAlgorithm
{
    // Static context creation for ILGPU
    private static readonly Context context = Context.Create(builder => builder.Default());

    // Kernel for calculating distances
    static void CalculateDistancesKernel(
        Index1D index,
        ArrayView<float> x,
        ArrayView<float> distances,
        int targetIdx)
    {
        float targetX = x[targetIdx];
        distances[index] = Math.Abs(x[index] - targetX);
    }

    // Kernel for calculating tricube weights
    static void CalculateWeightsKernel(
        Index1D index,
        ArrayView<float> distances,
        ArrayView<float> weights,
        ArrayView<float> residualWeights,
        float bandwidthDistance)
    {
        float distance = distances[index] / bandwidthDistance;

        // Tricube weight function
        float weight = (distance < 1.0f) ?
            (float)Math.Pow(1.0f - (float)Math.Pow(distance, 3.0f), 3.0f) :
            0.0f;

        // Apply robust weights
        weights[index] = weight * residualWeights[index];
    }

    // Kernel for calculating weighted sums
    static void CalculateWeightedSumsKernel(
        Index1D index,
        ArrayView<float> x,
        ArrayView<float> y,
        ArrayView<float> weights,
        ArrayView<float> wx,
        ArrayView<float> wy,
        ArrayView<float> wxx,
        ArrayView<float> wxy,
        ArrayView<float> w)
    {
        float weight = weights[index];
        float xVal = x[index];
        float yVal = y[index];

        wx[index] = weight * xVal;
        wy[index] = weight * yVal;
        wxx[index] = weight * xVal * xVal;
        wxy[index] = weight * xVal * yVal;
        w[index] = weight;
    }

    // Kernel for calculating residuals
    static void CalculateResidualsKernel(
        Index1D index,
        ArrayView<float> y,
        ArrayView<float> yEstimated,
        ArrayView<float> residuals)
    {
        residuals[index] = Math.Abs(y[index] - yEstimated[index]);
    }

    // Kernel for calculating residual weights
    static void CalculateResidualWeightsKernel(
        Index1D index,
        ArrayView<float> residuals,
        ArrayView<float> residualWeights,
        float medianResidual)
    {
        float u = residuals[index] / (6.0f * medianResidual);
        residualWeights[index] = (u < 1.0f) ? (float)Math.Pow(1.0f - u * u, 2.0f) : 0.0f;
    }

    public static double[] LowessGpu(double[] xInput, double[] yInput, double bandwidth = 0.25, int robustIterations = 3)
    {
        if (xInput.Length != yInput.Length)
            throw new ArgumentException("Input arrays must have the same length");

        int n = xInput.Length;

        // Convert double arrays to float arrays for GPU processing
        float[] x = xInput.Select(d => (float)d).ToArray();
        float[] y = yInput.Select(d => (float)d).ToArray();

        float[] yEstimatedFloat = new float[n];
        float[] residualWeights = Enumerable.Repeat(1.0f, n).ToArray();

        // Create accelerator with using declaration to ensure proper disposal
        using var accelerator = context.GetPreferredDevice(preferCPU: false).CreateAccelerator(context);
        Console.WriteLine($"Using accelerator: {accelerator.Name}");

        // Create a default accelerator stream
        using var stream = accelerator.CreateStream();

        // Compile kernels - older ILGPU with proper Action signatures
        var distancesKernel = accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, int>(CalculateDistancesKernel);
        var weightsKernel = accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>, float>(CalculateWeightsKernel);
        var weightedSumsKernel = accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>>(CalculateWeightedSumsKernel);
        var residualsKernel = accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>>(CalculateResidualsKernel);
        var residualWeightsKernel = accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, float>(CalculateResidualWeightsKernel);

        // Allocate GPU memory with using declarations to ensure proper disposal
        using var dX = accelerator.Allocate1D(x);
        using var dY = accelerator.Allocate1D(y);
        using var dResidualWeights = accelerator.Allocate1D(residualWeights);
        using var dYEstimated = accelerator.Allocate1D<float>(n);
        using var dDistances = accelerator.Allocate1D<float>(n);
        using var dWeights = accelerator.Allocate1D<float>(n);
        using var dWX = accelerator.Allocate1D<float>(n);
        using var dWY = accelerator.Allocate1D<float>(n);
        using var dWXX = accelerator.Allocate1D<float>(n);
        using var dWXY = accelerator.Allocate1D<float>(n);
        using var dW = accelerator.Allocate1D<float>(n);
        using var dResiduals = accelerator.Allocate1D<float>(n);

        // Perform robust iterations
        for (int iter = 0; iter < robustIterations; iter++)
        {
            // For each point, compute the LOWESS estimate
            for (int i = 0; i < n; i++)
            {
                // Calculate distances on GPU - pass stream as first parameter
                distancesKernel(stream, n, dX.View, dDistances.View, i);
                stream.Synchronize();

                // Get distances array back to sort and find bandwidth distance
                float[] distances = dDistances.GetAsArray1D();
                float[] sortedDistances = (float[])distances.Clone();
                Array.Sort(sortedDistances);
                float bandwidthDistance = sortedDistances[(int)(bandwidth * n)];

                // Handle case where multiple points have same x value
                if (bandwidthDistance == 0)
                    bandwidthDistance = sortedDistances.FirstOrDefault(d => d > 0) ?? 1.0f;

                // Calculate weights on GPU - pass stream as first parameter
                weightsKernel(stream, n, dDistances.View, dWeights.View, dResidualWeights.View, bandwidthDistance);
                stream.Synchronize();

                // Normalize weights (this part is done on CPU because it needs global sum)
                float[] weights = dWeights.GetAsArray1D();
                float sumWeights = weights.Sum();

                if (sumWeights > 0)
                {
                    for (int j = 0; j < n; j++)
                        weights[j] /= sumWeights;
                }

                // Upload normalized weights back to GPU
                dWeights.CopyFromCPU(weights);

                // Calculate weighted sums for regression on GPU
                weightedSumsKernel(stream, n, dX.View, dY.View, dWeights.View, dWX.View, dWY.View, dWXX.View, dWXY.View, dW.View);
                stream.Synchronize();

                // Download results and calculate regression
                float sumWX = dWX.GetAsArray1D().Sum();
                float sumWY = dWY.GetAsArray1D().Sum();
                float sumWXX = dWXX.GetAsArray1D().Sum();
                float sumWXY = dWXY.GetAsArray1D().Sum();
                float sumW = dW.GetAsArray1D().Sum();

                // Calculate local regression
                if (sumW > 0)
                {
                    float meanX = sumWX / sumW;
                    float meanY = sumWY / sumW;
                    float slope = (sumWXY - sumWX * meanY) / (sumWXX - sumWX * meanX);
                    float intercept = meanY - slope * meanX;
                    yEstimatedFloat[i] = intercept + slope * x[i];
                }
                else
                {
                    yEstimatedFloat[i] = 0;
                }
            }

            // Skip residual weight updates on the last iteration
            if (iter < robustIterations - 1)
            {
                // Copy estimated y values to GPU
                dYEstimated.CopyFromCPU(yEstimatedFloat);

                // Calculate residuals on GPU
                residualsKernel(stream, n, dY.View, dYEstimated.View, dResiduals.View);
                stream.Synchronize();

                // Download residuals to calculate median
                float[] residuals = dResiduals.GetAsArray1D();
                float[] sortedResiduals = (float[])residuals.Clone();
                Array.Sort(sortedResiduals);
                float medianResidual = sortedResiduals[n / 2];

                // Calculate residual weights on GPU
                residualWeightsKernel(stream, n, dResiduals.View, dResidualWeights.View, medianResidual);
                stream.Synchronize();

                // Download the updated residual weights
                residualWeights = dResidualWeights.GetAsArray1D();
            }
        }

        // Convert back to double for consistency with output signature
        return yEstimatedFloat.Select(f => (double)f).ToArray();
    }

    // Alternative method that uses CPU if GPU is not available
    public static double[] LowessWithFallback(double[] x, double[] y, double bandwidth = 0.25, int robustIterations = 3)
    {
        try
        {
            return LowessGpu(x, y, bandwidth, robustIterations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU computation failed: {ex.Message}");
            Console.WriteLine("Falling back to CPU implementation");
            return LowessAlgorithm.Lowess(x, y, bandwidth, robustIterations);
        }
    }

    // Dispose the context when the application is shutting down
    public static void Cleanup()
    {
        context?.Dispose();
    }
}
#endif