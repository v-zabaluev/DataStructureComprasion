using System;
using System.Collections.Generic;
using System.IO;
using Benchmark.Core;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Benchmark.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Benchmark.UI
{
    public class BenchmarkLauncherView : MonoBehaviour
    {
        [Header("Buttons")]

        [SerializeField] private Button _runButton;

        [Header("Output")]

        [SerializeField] private TMP_Text _statusText;

        [Header("Benchmark")]

        [SerializeField] private TMP_Dropdown _structureDropdown;

        [SerializeField] private TMP_Dropdown _scenarioDropdown;

        [Header("Load")]

        [SerializeField] private TMP_InputField _objectCountInput;

        [SerializeField] private TMP_InputField _operationCountInput;
        [SerializeField] private TMP_InputField _warmupRunsInput;
        [SerializeField] private TMP_InputField _measuredRunsInput;

        [Header("Rules")]

        [SerializeField] private Toggle _preallocateCapacityToggle;

        [SerializeField] private Toggle _preserveOrderOnRemoveToggle;

        [Header("Random")]

        [SerializeField] private TMP_InputField _baseSeedInput;

        [Header("Projectile")]

        [SerializeField] private TMP_InputField _deltaTimeInput;

        [SerializeField] private TMP_InputField _projectileSpeedInput;
        [SerializeField] private TMP_InputField _projectileLifeTimeInput;

        [Header("World")]

        [SerializeField] private TMP_InputField _spawnMinXInput;

        [SerializeField] private TMP_InputField _spawnMinYInput;
        [SerializeField] private TMP_InputField _spawnMaxXInput;
        [SerializeField] private TMP_InputField _spawnMaxYInput;

        [Header("Effect Area")]

        [SerializeField] private TMP_InputField _effectCenterXInput;

        [SerializeField] private TMP_InputField _effectCenterYInput;
        [SerializeField] private TMP_InputField _effectRadiusInput;

        [Header("Grid")]

        [SerializeField] private TMP_InputField _cellsPerRowInput;

        [SerializeField] private Toggle _autoCalculateCellSizeToggle;
        [SerializeField] private TMP_InputField _cellSizeInput;

        [Header("Wave")]

        [SerializeField] private TMP_InputField _waveCountInput;

        [SerializeField] private TMP_InputField _projectilesPerWaveInput;

        [Header("Factory")]

        [SerializeField] private BenchmarkFactory _benchmarkFactory;

        [Header("CSV")]

        [SerializeField] private string _csvFolderName = "BenchmarkResults";

        private bool _isRunning;

        private readonly BenchmarkRunner _runner = new BenchmarkRunner();

        private BenchmarkCsvWriter _csvWriter;
        private Coroutine _runningCoroutine;
        private BenchmarkConfigData _currentConfig;
        private IProjectileStorageBenchmark _currentBenchmark;

        private void Awake()
        {
            _runButton.onClick.AddListener(RunBenchmark);

            string directoryPath = Path.Combine(
                Application.persistentDataPath,
                _csvFolderName);

            _csvWriter = new BenchmarkCsvWriter(directoryPath);
            FillStructureDropdown();
            FillScenarioDropdown();
            SetStatus(BenchmarkStatus.Idle);
        }

        private void OnDestroy()
        {
            _runButton.onClick.RemoveListener(RunBenchmark);
        }

        private void RunBenchmark()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            _runButton.interactable = false;

            try
            {
                SetStatus(BenchmarkStatus.Preparing);

                BenchmarkConfigData config = CreateConfigFromUI();

                IProjectileStorageBenchmark benchmark =
                    _benchmarkFactory.Create(config.StructureKind);

                _currentConfig = config;
                _currentBenchmark = benchmark;

                SetStatus(BenchmarkStatus.Running);

                BenchmarkResult result =
                    _runner.Run(
                        benchmark,
                        config,
                        OnProgressChanged);

                SetStatus(BenchmarkStatus.Saving);

                string filePath = _csvWriter.Save(result);

                Debug.Log(_csvWriter.CreateLogText(result, filePath));

                SetCompletedStatus(result, filePath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                SetStatus(BenchmarkStatus.Failed);
            }

            _runButton.interactable = true;
            _isRunning = false;
        }

        private BenchmarkConfigData CreateConfigFromUI()
        {
            BenchmarkConfigData config = new BenchmarkConfigData();

            config.StructureKind = GetSelectedStructure();
            config.Scenario = GetSelectedScenario();

            config.ObjectCount = ReadInt(_objectCountInput, 10000);
            config.OperationCount = ReadInt(_operationCountInput, 10000);
            config.WarmupRuns = ReadInt(_warmupRunsInput, 3);
            config.MeasuredRuns = ReadInt(_measuredRunsInput, 10);

            config.PreallocateCapacity = _preallocateCapacityToggle.isOn;
            config.PreserveOrderOnRemove = _preserveOrderOnRemoveToggle.isOn;

            config.BaseSeed = ReadInt(_baseSeedInput, 1);

            config.DeltaTime = ReadFloat(_deltaTimeInput, 0.016f);
            config.ProjectileSpeed = ReadFloat(_projectileSpeedInput, 5f);
            config.ProjectileLifeTime = ReadFloat(_projectileLifeTimeInput, 5f);

            config.SpawnMin = new Vector2(
                ReadFloat(_spawnMinXInput, 0f),
                ReadFloat(_spawnMinYInput, 0f));

            config.SpawnMax = new Vector2(
                ReadFloat(_spawnMaxXInput, 100f),
                ReadFloat(_spawnMaxYInput, 100f));

            config.EffectCenter = new Vector2(
                ReadFloat(_effectCenterXInput, 50f),
                ReadFloat(_effectCenterYInput, 50f));

            config.EffectRadius = ReadFloat(_effectRadiusInput, 15f);

            config.CellsPerRow = ReadInt(_cellsPerRowInput, 10);
            config.AutoCalculateCellSize = ReadBool(_autoCalculateCellSizeToggle, true);
            config.CellSize = ReadFloat(_cellSizeInput, 10f);

            config.WaveCount = ReadInt(_waveCountInput, 5);
            config.ProjectilesPerWave = ReadInt(_projectilesPerWaveInput, 1000);

            config.Normalize();

            return config;
        }

        private int ReadInt(TMP_InputField input, int defaultValue)
        {
            int value;

            if (input == null)
            {
                return defaultValue;
            }

            if (int.TryParse(input.text, out value))
            {
                return value;
            }

            return defaultValue;
        }

        private float ReadFloat(TMP_InputField input, float defaultValue)
        {
            float value;

            if (input == null)
            {
                return defaultValue;
            }

            if (float.TryParse(input.text, out value))
            {
                return value;
            }

            return defaultValue;
        }

        private bool ReadBool(Toggle toggle, bool defaultValue)
        {
            if (toggle == null)
            {
                return defaultValue;
            }

            return toggle.isOn;
        }

        private void OnProgressChanged(BenchmarkProgress progress)
        {
            if (progress.Status == "Warmup")
            {
                SetProgressText(
                    "Выполняется прогрев",
                    progress.CurrentStep,
                    progress.TotalSteps);
            }
            else
            {
                SetProgressText(
                    "Выполняется измеряемый запуск",
                    progress.CurrentStep,
                    progress.TotalSteps);
            }
        }

        private void SetProgressText(string title, int currentStep, int totalSteps)
        {
            int percent = Mathf.RoundToInt(
                (float) currentStep / totalSteps * 100f);

            string structureName = _currentBenchmark != null
                ? _currentBenchmark.StructureName
                : "Unknown";

            string scenarioName = _currentConfig != null
                ? _currentConfig.Scenario.ToString()
                : "Unknown";

            _statusText.text =
                title + "\n" +
                "Structure: " + structureName + "\n" +
                "Scenario: " + scenarioName + "\n" +
                "Progress: " + currentStep + " / " + totalSteps + "\n" +
                "Percent: " + percent + "%";
        }

        private void SetStatus(BenchmarkStatus status)
        {
            switch (status)
            {
                case BenchmarkStatus.Idle:
                    _statusText.text = "Ожидание запуска теста";

                    break;

                case BenchmarkStatus.Preparing:
                    _statusText.text = "Подготовка тестирования...";

                    break;

                case BenchmarkStatus.Warmup:
                    _statusText.text = "Выполняется прогрев...";

                    break;

                case BenchmarkStatus.Running:
                    _statusText.text = "Выполняется тестирование...";

                    break;

                case BenchmarkStatus.Saving:
                    _statusText.text = "Сохранение результатов в CSV...";

                    break;

                case BenchmarkStatus.Completed:
                    _statusText.text = "Тестирование завершено";

                    break;

                case BenchmarkStatus.Failed:
                    _statusText.text = "Тестирование завершилось с ошибкой";

                    break;
            }
        }

        private void SetCompletedStatus(BenchmarkResult result, string filePath)
        {
            _statusText.text =
                "Тестирование завершено\n" +
                "Structure: " + result.Structure + "\n" +
                "Scenario: " + result.Scenario + "\n" +
                "CSV сохранён:\n" +
                filePath;
        }

        private void FillStructureDropdown()
        {
            _structureDropdown.ClearOptions();

            List<string> options = new List<string>();

            foreach (BenchmarkStructureKind value in
                     Enum.GetValues(typeof(BenchmarkStructureKind)))
            {
                options.Add(value.ToString());
            }

            _structureDropdown.AddOptions(options);
        }

        private void FillScenarioDropdown()
        {
            _scenarioDropdown.ClearOptions();

            List<string> options = new List<string>();

            foreach (BenchmarkScenario value in
                     Enum.GetValues(typeof(BenchmarkScenario)))
            {
                options.Add(value.ToString());
            }

            _scenarioDropdown.AddOptions(options);
        }

        private BenchmarkStructureKind GetSelectedStructure()
        {
            return (BenchmarkStructureKind)
                _structureDropdown.value;
        }

        private BenchmarkScenario GetSelectedScenario()
        {
            return (BenchmarkScenario)
                _scenarioDropdown.value;
        }
    }
}