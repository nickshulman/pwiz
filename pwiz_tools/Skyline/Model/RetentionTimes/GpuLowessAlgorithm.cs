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
        float weight = distance < 1.0f ? Cube(1.0f - Cube(distance)) : 0.0f;

        // Apply robust weights
        weights[index] = weight * residualWeights[index];
    }

    static float Cube(float value)
    {
        return value * value * value;
    }

    static float Square(float value)
    {
        return value * value;
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
        residualWeights[index] = u < 1.0f ? Square(1.0f - u * u) : 0.0f;
    }

    public static double[] LowessGpu(double[] xDoubles, double[] yDoubles, double bandwidth = 0.25, int robustIterations = 3)
    {
        var x = xDoubles.Select(v => (float)v).ToArray();
        var y = yDoubles.Select(v => (float)v).ToArray();
        if (x.Length != y.Length)
            throw new ArgumentException("Input arrays must have the same length");

        int n = x.Length;
        float[] yEstimated = new float[n];
        float[] residualWeights = Enumerable.Repeat(1.0f, n).ToArray();

        // Create accelerator with option to force CPU if needed
        using var accelerator = context.GetPreferredDevice(preferCPU: false)
            .CreateAccelerator(context);

        Console.WriteLine("Using accelerator: {0}", accelerator.Name);

        // Force ILGPU to use double precision on RTX 4090
        accelerator.DefaultStream.Synchronize();

        // Try to determine API version by checking method existence
        bool isOldApi = false; //typeof(Accelerator).GetMethod("LoadAutoGroupedStreamKernel") != null;
        bool isNewerApi = true; //typeof(Accelerator).GetMethod("LoadAutoGroupedKernel") != null;

        if (!isOldApi && !isNewerApi)
        {
            throw new InvalidOperationException("Could not determine ILGPU API version");
        }

        // Allocate GPU memory
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
        using var stream = accelerator.CreateStream();

        if (isOldApi)
        {
            // Old API with AcceleratorStream

            var distancesKernel =
                accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, int>(
                    CalculateDistancesKernel);
            var weightsKernel =
                accelerator
                    .LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>,
                        float>(CalculateWeightsKernel);
            var weightedSumsKernel =
                accelerator
                    .LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>,
                        ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>>(
                        CalculateWeightedSumsKernel);
            var residualsKernel =
                accelerator
                    .LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>>(
                        CalculateResidualsKernel);
            var residualWeightsKernel =
                accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>, float>(
                    CalculateResidualWeightsKernel);

            // Process robust iterations
            for (int iter = 0; iter < robustIterations; iter++)
            {
                // For each point, compute the LOWESS estimate
                for (int i = 0; i < n; i++)
                {
                    distancesKernel(n, dX.View, dDistances.View, i);
                    stream.Synchronize();

                    var distances = dDistances.GetAsArray1D();
                    var sortedDistances = (float[])distances.Clone();
                    Array.Sort(sortedDistances);
                    var bandwidthDistance = sortedDistances[(int)(bandwidth * n)];

                    if (bandwidthDistance == 0)
                        bandwidthDistance = sortedDistances.Cast<float?>().FirstOrDefault(d => d > 0) ?? 1.0f;

                    weightsKernel(n, dDistances.View, dWeights.View, dResidualWeights.View, bandwidthDistance);
                    stream.Synchronize();

                    var weights = dWeights.GetAsArray1D();
                    var sumWeights = weights.Sum();

                    if (sumWeights > 0)
                    {
                        for (int j = 0; j < n; j++)
                            weights[j] /= sumWeights;
                    }

                    dWeights.CopyFromCPU(weights);

                    weightedSumsKernel(n, dX.View, dY.View, dWeights.View, dWX.View, dWY.View, dWXX.View, dWXY.View,
                        dW.View);
                    stream.Synchronize();

                    double sumWX = dWX.GetAsArray1D().Sum();
                    double sumWY = dWY.GetAsArray1D().Sum();
                    double sumWXX = dWXX.GetAsArray1D().Sum();
                    double sumWXY = dWXY.GetAsArray1D().Sum();
                    double sumW = dW.GetAsArray1D().Sum();

                    if (sumW > 0)
                    {
                        double meanX = sumWX / sumW;
                        double meanY = sumWY / sumW;
                        double slope = (sumWXY - sumWX * meanY) / (sumWXX - sumWX * meanX);
                        double intercept = meanY - slope * meanX;
                        yEstimated[i] = (float) (intercept + slope * x[i]);
                    }
                    else
                    {
                        yEstimated[i] = 0;
                    }
                }

                if (iter < robustIterations - 1)
                {
                    dYEstimated.CopyFromCPU(yEstimated);

                    residualsKernel(n, dY.View, dYEstimated.View, dResiduals.View);
                    stream.Synchronize();

                    var residuals = dResiduals.GetAsArray1D();
                    var sortedResiduals = (float[])residuals.Clone();
                    Array.Sort(sortedResiduals);
                    var medianResidual = sortedResiduals[n / 2];

                    residualWeightsKernel(n, dResiduals.View, dResidualWeights.View, medianResidual);
                    stream.Synchronize();

                    residualWeights = dResidualWeights.GetAsArray1D();
                }
            }
        }
        else
        {
            // Newer API
            var distancesKernel =
                accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, int>(
                    CalculateDistancesKernel);
            var weightsKernel =
                accelerator
                    .LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>, float>(
                        CalculateWeightsKernel);
            var weightedSumsKernel =
                accelerator
                    .LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>,
                        ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>, ArrayView<float>>(
                        CalculateWeightedSumsKernel);
            var residualsKernel =
                accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, ArrayView<float>>(
                    CalculateResidualsKernel);
            var residualWeightsKernel =
                accelerator.LoadAutoGroupedKernel<Index1D, ArrayView<float>, ArrayView<float>, float>(
                    CalculateResidualWeightsKernel);

            // Process robust iterations
            for (int iter = 0; iter < robustIterations; iter++)
            {
                // For each point, compute the LOWESS estimate
                for (int i = 0; i < n; i++)
                {
                    distancesKernel(stream, n, dX.View, dDistances.View, i);
                    accelerator.Synchronize();

                    var distances = dDistances.GetAsArray1D();
                    var sortedDistances = (float[])distances.Clone();
                    Array.Sort(sortedDistances);
                    var bandwidthDistance = sortedDistances[(int)(bandwidth * n)];

                    if (bandwidthDistance == 0)
                        bandwidthDistance = sortedDistances.Cast<float?>().FirstOrDefault(d => d > 0) ?? 1.0f;

                    weightsKernel(stream, n, dDistances.View, dWeights.View, dResidualWeights.View, bandwidthDistance);
                    accelerator.Synchronize();

                    var weights = dWeights.GetAsArray1D();
                    var sumWeights = weights.Sum();

                    if (sumWeights > 0)
                    {
                        for (int j = 0; j < n; j++)
                            weights[j] /= sumWeights;
                    }

                    dWeights.CopyFromCPU(weights);

                    weightedSumsKernel(stream, n, dX.View, dY.View, dWeights.View, dWX.View, dWY.View, dWXX.View,
                        dWXY.View, dW.View);
                    accelerator.Synchronize();

                    var sumWX = dWX.GetAsArray1D().Sum();
                    var sumWY = dWY.GetAsArray1D().Sum();
                    var sumWXX = dWXX.GetAsArray1D().Sum();
                    var sumWXY = dWXY.GetAsArray1D().Sum();
                    var sumW = dW.GetAsArray1D().Sum();

                    if (sumW > 0)
                    {
                        var meanX = sumWX / sumW;
                        var meanY = sumWY / sumW;
                        var slope = (sumWXY - sumWX * meanY) / (sumWXX - sumWX * meanX);
                        var intercept = meanY - slope * meanX;
                        yEstimated[i] = intercept + slope * x[i];
                    }
                    else
                    {
                        yEstimated[i] = 0;
                    }
                }

                if (iter < robustIterations - 1)
                {
                    dYEstimated.CopyFromCPU(yEstimated);

                    residualsKernel(stream, n, dY.View, dYEstimated.View, dResiduals.View);
                    accelerator.Synchronize();

                    var residuals = dResiduals.GetAsArray1D();
                    var sortedResiduals = (float[])residuals.Clone();
                    Array.Sort(sortedResiduals);
                    var medianResidual = sortedResiduals[n / 2];

                    residualWeightsKernel(stream, n, dResiduals.View, dResidualWeights.View, medianResidual);
                    accelerator.Synchronize();

                    residualWeights = dResidualWeights.GetAsArray1D();
                }
            }
        }

        return yEstimated.Select(v=>(double) v).ToArray();
    }


    public static void Cleanup()
    {
        context?.Dispose();

    }

    public static class Rtx4090Configuration
    {
        public static void ConfigureForDoublePrecision()
        {
            // Set environment variables
            Environment.SetEnvironmentVariable("CUDA_FORCE_PTX_JIT", "1");

            // Configure ILGPU context with explicit device settings
            var context = Context.Create(builder => builder
                    .Default()
                    .Math(MathMode.Fast32BitOnly) // Try different math modes if needed
                    .Optimize(OptimizationLevel.Debug) // Try Debug mode first for better error messages
            );

            // Print available devices and their capabilities
            foreach (var device in context.Devices)
            {
                Console.WriteLine($"Device: {device.Name}");
//                Console.WriteLine($"  Supports Double: {device.}");
                Console.WriteLine($"  Max Grid Size: {string.Join(", ", device.MaxGridSize)}");
                Console.WriteLine($"  Max Group Size: {device.MaxGroupSize}");
//                Console.WriteLine($"  Max Shared Memory: {device.MaxSharedMemory}");
            }

            context.Dispose();
        }
    }
}