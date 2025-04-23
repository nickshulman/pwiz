using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU;

namespace pwiz.Skyline.Model.RetentionTimes
{
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
                Console.WriteLine($"  Max Grid Size: {string.Join(", ", device.MaxGridSize)}");
                Console.WriteLine($"  Max Group Size: {device.MaxGroupSize}");
            }

            context.Dispose();
        }
    }
}
