using System.Globalization;
using System.IO;
using Benchmark.Core;
using Benchmark.Data;

namespace Benchmark.IO
{
    public class BenchmarkCsvWriter
    {
        private readonly string _directoryPath;

        public BenchmarkCsvWriter(string directoryPath)
        {
            _directoryPath = directoryPath;
        }

        public string Save(BenchmarkResult result)
        {
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }

            string fileName = CreateFileName(result);
            string filePath = Path.Combine(_directoryPath, fileName);

            bool needHeader = !File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                if (needHeader)
                {
                    writer.WriteLine(CreateHeader());
                }

                writer.WriteLine(CreateRow(result));
            }

            return filePath;
        }

        public string CreateLogText(BenchmarkResult result, string filePath)
        {
            BenchmarkConfigData config = result.Config;

            return
                "Structure: " + result.Structure + "\n" +
                "Scenario: " + result.Scenario + "\n" +
                "Objects: " + result.Objects + "\n" +
                "Operations: " + result.Operations + "\n" +
                "WarmupRuns: " + config.WarmupRuns + "\n" +
                "MeasuredRuns: " + config.MeasuredRuns + "\n" +
                "PreallocateCapacity: " + config.PreallocateCapacity + "\n" +
                "PreserveOrderOnRemove: " + config.PreserveOrderOnRemove + "\n" +
                "BaseSeed: " + config.BaseSeed + "\n" +
                "DeltaTime: " + Format(config.DeltaTime) + "\n" +
                "ProjectileSpeed: " + Format(config.ProjectileSpeed) + "\n" +
                "ProjectileLifeTime: " + Format(config.ProjectileLifeTime) + "\n" +
                "SpawnMin: (" + Format(config.SpawnMin.x) + "; " + Format(config.SpawnMin.y) + ")\n" +
                "SpawnMax: (" + Format(config.SpawnMax.x) + "; " + Format(config.SpawnMax.y) + ")\n" +
                "EffectCenter: (" + Format(config.EffectCenter.x) + "; " + Format(config.EffectCenter.y) + ")\n" +
                "EffectRadius: " + Format(config.EffectRadius) + "\n" +
                "CellsPerRow: " + config.CellsPerRow + "\n" +
                "AutoCalculateCellSize: " + config.AutoCalculateCellSize + "\n" +
                "CellSize: " + Format(config.CellSize) + "\n" +
                "ObjectsPerCell: " + Format(result.ObjectsPerCell) + "\n" +
                "WaveCount: " + config.WaveCount + "\n" +
                "ProjectilesPerWave: " + config.ProjectilesPerWave + "\n" +
                "AverageScenarioExecutionTimeMs: " + Format(result.AverageScenarioExecutionTimeMs) + "\n" +
                "MedianScenarioExecutionTimeMs: " + Format(result.MedianScenarioExecutionTimeMs) + "\n" +
                "StandardDeviationScenarioExecutionTimeMs: " + Format(result.StandardDeviationScenarioExecutionTimeMs) +
                "\n" +
                "MinScenarioExecutionTimeMs: " + Format(result.MinScenarioExecutionTimeMs) + "\n" +
                "MaxScenarioExecutionTimeMs: " + Format(result.MaxScenarioExecutionTimeMs) + "\n" +
                "AllocatedBytes: " + result.AllocatedBytes + "\n" +
                "GCCollections: " + result.GCCollections + "\n" +
                "Checksum: " + result.Checksum + "\n" +
                "IsSupported: " + result.IsSupported + "\n" +
                "Message: " + result.Message + "\n" +
                "CsvFilePath: " + filePath;
        }

        private string CreateFileName(BenchmarkResult result)
        {
            string name = result.Structure + "_" + result.Scenario + ".csv";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }

            return name;
        }

        private string CreateHeader()
        {
            return
                "Structure;" +
                "Scenario;" +
                "Objects;" +
                "Operations;" +
                "WarmupRuns;" +
                "MeasuredRuns;" +
                "PreallocateCapacity;" +
                "PreserveOrderOnRemove;" +
                "BaseSeed;" +
                "DeltaTime;" +
                "ProjectileSpeed;" +
                "ProjectileLifeTime;" +
                "SpawnMinX;" +
                "SpawnMinY;" +
                "SpawnMaxX;" +
                "SpawnMaxY;" +
                "EffectCenterX;" +
                "EffectCenterY;" +
                "EffectRadius;" +
                "CellsPerRow;" +
                "AutoCalculateCellSize;" +
                "CellSize;" +
                "ObjectsPerCell;" +
                "WaveCount;" +
                "ProjectilesPerWave;" +
                "AverageScenarioExecutionTimeMs;" +
                "MedianScenarioExecutionTimeMs;" +
                "StandardDeviationScenarioExecutionTimeMs;" +
                "MinScenarioExecutionTimeMs;" +
                "MaxScenarioExecutionTimeMs;" +
                "AllocatedBytes;" +
                "GCCollections;" +
                "Checksum;" +
                "IsSupported;" +
                "Message";
        }

        private string CreateRow(BenchmarkResult result)
        {
            BenchmarkConfigData config = result.Config;

            return
                Escape(result.Structure) + ";" +
                Escape(result.Scenario) + ";" +
                result.Objects + ";" +
                result.Operations + ";" +
                config.WarmupRuns + ";" +
                config.MeasuredRuns + ";" +
                config.PreallocateCapacity + ";" +
                config.PreserveOrderOnRemove + ";" +
                config.BaseSeed + ";" +
                Format(config.DeltaTime) + ";" +
                Format(config.ProjectileSpeed) + ";" +
                Format(config.ProjectileLifeTime) + ";" +
                Format(config.SpawnMin.x) + ";" +
                Format(config.SpawnMin.y) + ";" +
                Format(config.SpawnMax.x) + ";" +
                Format(config.SpawnMax.y) + ";" +
                Format(config.EffectCenter.x) + ";" +
                Format(config.EffectCenter.y) + ";" +
                Format(config.EffectRadius) + ";" +
                config.CellsPerRow + ";" +
                config.AutoCalculateCellSize + ";" +
                Format(config.CellSize) + ";" +
                Format(result.ObjectsPerCell) + ";" +
                config.WaveCount + ";" +
                config.ProjectilesPerWave + ";" +
                Format(result.AverageScenarioExecutionTimeMs) + ";" +
                Format(result.MedianScenarioExecutionTimeMs) + ";" +
                Format(result.StandardDeviationScenarioExecutionTimeMs) + ";" +
                Format(result.MinScenarioExecutionTimeMs) + ";" +
                Format(result.MaxScenarioExecutionTimeMs) + ";" +
                result.AllocatedBytes + ";" +
                result.GCCollections + ";" +
                result.Checksum + ";" +
                result.IsSupported + ";" +
                Escape(result.Message);
        }

        private string Format(double value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private string Escape(string value)
        {
            if (value == null)
            {
                return "";
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}