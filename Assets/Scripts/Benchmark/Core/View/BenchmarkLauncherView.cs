using System;
using System.Collections;
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
        [SerializeField] private Button _runAllScenariosButton;

        [Header("Output")]
        [SerializeField] private TMP_Text _statusText;

        [Header("Benchmark")]
        [SerializeField] private TMP_Dropdown _structureDropdown;
        [SerializeField] private TMP_Dropdown _scenarioDropdown;

        [Header("Batch Research")]
        [SerializeField] private BenchmarkBatchConfigSet _batchConfigSet;
        [SerializeField] private bool _saveUnsupportedScenarioResults = true;

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

        [Header("Wave")]
        [SerializeField] private TMP_InputField _waveCountInput;
        [SerializeField] private TMP_InputField _projectilesPerWaveInput;

        [Header("Factory")]
        [SerializeField] private BenchmarkFactory _benchmarkFactory;

        [Header("CSV")]
        [SerializeField] private string _csvFolderName = "BenchmarkResults";

        private bool _isRunning;
        private int _batchCompletedRuns;
        private int _batchTotalRuns;
        private int _batchConfigIndex;
        private int _batchConfigCount;

        private readonly BenchmarkRunner _runner = new BenchmarkRunner();

        private BenchmarkCsvWriter _csvWriter;
        private Coroutine _runningCoroutine;
        private BenchmarkConfigData _currentConfig;
        private IProjectileStorageBenchmark _currentBenchmark;

        private void Awake()
        {
            if (_runButton != null)
            {
                _runButton.onClick.AddListener(RunBenchmark);
            }

            if (_runAllScenariosButton != null)
            {
                _runAllScenariosButton.onClick.AddListener(
                    RunAllScenariosForSelectedStructure);
            }

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
            if (_runButton != null)
            {
                _runButton.onClick.RemoveListener(RunBenchmark);
            }

            if (_runAllScenariosButton != null)
            {
                _runAllScenariosButton.onClick.RemoveListener(
                    RunAllScenariosForSelectedStructure);
            }
        }

        private void RunBenchmark()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            SetButtonsInteractable(false);

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

            _currentConfig = null;
            _currentBenchmark = null;
            SetButtonsInteractable(true);
            _isRunning = false;
        }

        private void RunAllScenariosForSelectedStructure()
        {
            if (_isRunning)
            {
                return;
            }

            _runningCoroutine = StartCoroutine(
                RunAllScenariosForSelectedStructureCoroutine());
        }

        private IEnumerator RunAllScenariosForSelectedStructureCoroutine()
        {
            _isRunning = true;
            SetButtonsInteractable(false);

            BenchmarkStructureKind selectedStructure = GetSelectedStructure();
            List<BenchmarkConfigData> baseConfigs = CreateBatchBaseConfigs();
            BenchmarkScenario[] scenarios = GetAllScenarios();

            _batchCompletedRuns = 0;
            _batchTotalRuns = baseConfigs.Count * scenarios.Length;
            _batchConfigCount = baseConfigs.Count;

            int savedResults = 0;
            string lastFilePath = "";
            bool isFailed = false;

            SetBatchStatus(
                "Подготовка полного прогона",
                selectedStructure,
                0,
                _batchTotalRuns);

            yield return null;

            for (int configIndex = 0; configIndex < baseConfigs.Count; configIndex++)
            {
                _batchConfigIndex = configIndex + 1;

                for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                {
                    BenchmarkConfigData config = CreateScenarioConfig(
                        baseConfigs[configIndex],
                        selectedStructure,
                        scenarios[scenarioIndex]);

                    SetBatchStatus(
                        "Выполняется полный прогон",
                        selectedStructure,
                        _batchCompletedRuns + 1,
                        _batchTotalRuns);

                    BatchRunResult runResult = RunSingleBatchBenchmark(config);

                    if (!runResult.IsSuccess)
                    {
                        isFailed = true;
                        break;
                    }

                    if (runResult.IsSaved)
                    {
                        savedResults++;
                        lastFilePath = runResult.FilePath;
                    }

                    _batchCompletedRuns++;

                    yield return null;
                }

                if (isFailed)
                {
                    break;
                }
            }

            if (!isFailed)
            {
                SetBatchCompletedStatus(
                    selectedStructure,
                    baseConfigs.Count,
                    scenarios.Length,
                    savedResults,
                    lastFilePath);
            }

            _currentConfig = null;
            _currentBenchmark = null;
            _runningCoroutine = null;
            _batchCompletedRuns = 0;
            _batchTotalRuns = 0;
            _batchConfigIndex = 0;
            _batchConfigCount = 0;

            SetButtonsInteractable(true);
            _isRunning = false;
        }

        private BatchRunResult RunSingleBatchBenchmark(BenchmarkConfigData config)
        {
            try
            {
                IProjectileStorageBenchmark benchmark =
                    _benchmarkFactory.Create(config.StructureKind);

                _currentConfig = config;
                _currentBenchmark = benchmark;

                BenchmarkResult result = _runner.Run(
                    benchmark,
                    config,
                    OnBatchProgressChanged);

                if (result.IsSupported || _saveUnsupportedScenarioResults)
                {
                    string filePath = _csvWriter.Save(result);
                    Debug.Log(_csvWriter.CreateLogText(result, filePath));

                    return BatchRunResult.CreateSaved(filePath);
                }

                return BatchRunResult.CreateNotSaved();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(BenchmarkStatus.Failed);

                return BatchRunResult.CreateFailed();
            }
        }

        private BenchmarkConfigData CreateScenarioConfig(
            BenchmarkConfigData baseConfig,
            BenchmarkStructureKind structureKind,
            BenchmarkScenario scenario)
        {
            BenchmarkConfigData config = baseConfig.Clone();

            config.StructureKind = structureKind;
            config.Scenario = scenario;
            config.OperationCount = GetScenarioOperationCount(scenario, config);
            config.Normalize();

            return config;
        }

        private int GetScenarioOperationCount(
            BenchmarkScenario scenario,
            BenchmarkConfigData config)
        {
            switch (scenario)
            {
                case BenchmarkScenario.AddElements:
                case BenchmarkScenario.MassFill:
                case BenchmarkScenario.RemoveElement:
                    return config.ObjectCount;

                case BenchmarkScenario.SearchById:
                case BenchmarkScenario.ContainsElement:
                case BenchmarkScenario.UpdateOne:
                    return config.OperationCount;

                default:
                    return config.OperationCount;
            }
        }

        private List<BenchmarkConfigData> CreateBatchBaseConfigs()
        {
            List<BenchmarkConfigData> configs = new List<BenchmarkConfigData>();

            if (_batchConfigSet != null && _batchConfigSet.Count > 0)
            {
                for (int i = 0; i < _batchConfigSet.Count; i++)
                {
                    BenchmarkConfigData config = _batchConfigSet.GetConfig(i);

                    if (config != null)
                    {
                        configs.Add(config.Clone());
                    }
                }
            }

            if (configs.Count == 0)
            {
                configs.Add(CreateConfigFromUI());
            }

            return configs;
        }

        private BenchmarkScenario[] GetAllScenarios()
        {
            Array values = Enum.GetValues(typeof(BenchmarkScenario));
            BenchmarkScenario[] scenarios = new BenchmarkScenario[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                scenarios[i] = (BenchmarkScenario) values.GetValue(i);
            }

            return scenarios;
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

            config.PreallocateCapacity = ReadBool(_preallocateCapacityToggle, true);
            config.PreserveOrderOnRemove = ReadBool(_preserveOrderOnRemoveToggle, true);

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

        private void OnBatchProgressChanged(BenchmarkProgress progress)
        {
            if (_currentConfig == null || _currentBenchmark == null)
            {
                return;
            }

            string phase = progress.Status == "Warmup"
                ? "Прогрев"
                : "Измеряемый запуск";

            int globalPercent = _batchTotalRuns <= 0
                ? 0
                : Mathf.RoundToInt(
                    (float) _batchCompletedRuns / _batchTotalRuns * 100f);

            _statusText.text =
                "Выполняется полный прогон\n" +
                "Structure: " + _currentBenchmark.StructureName + "\n" +
                "Scenario: " + _currentConfig.Scenario + "\n" +
                "Objects: " + _currentConfig.ObjectCount + "\n" +
                "Operations: " + _currentConfig.OperationCount + "\n" +
                "Config: " + _batchConfigIndex + " / " + _batchConfigCount + "\n" +
                "Global run: " + (_batchCompletedRuns + 1) + " / " + _batchTotalRuns + "\n" +
                "Global percent: " + globalPercent + "%\n" +
                phase + ": " + progress.CurrentStep + " / " + progress.TotalSteps;
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

        private void SetBatchStatus(
            string title,
            BenchmarkStructureKind structureKind,
            int currentRun,
            int totalRuns)
        {
            int percent = totalRuns <= 0
                ? 0
                : Mathf.RoundToInt((float) currentRun / totalRuns * 100f);

            _statusText.text =
                title + "\n" +
                "Structure: " + structureKind + "\n" +
                "Run: " + currentRun + " / " + totalRuns + "\n" +
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

        private void SetBatchCompletedStatus(
            BenchmarkStructureKind selectedStructure,
            int baseConfigCount,
            int scenarioCount,
            int savedResults,
            string lastFilePath)
        {
            _statusText.text =
                "Полный прогон завершён\n" +
                "Structure: " + selectedStructure + "\n" +
                "Base configs: " + baseConfigCount + "\n" +
                "Scenarios: " + scenarioCount + "\n" +
                "Total runs: " + _batchTotalRuns + "\n" +
                "Saved rows: " + savedResults + "\n" +
                "Last CSV:\n" + lastFilePath;
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (_runButton != null)
            {
                _runButton.interactable = isInteractable;
            }

            if (_runAllScenariosButton != null)
            {
                _runAllScenariosButton.interactable = isInteractable;
            }
        }

        private void FillStructureDropdown()
        {
            if (_structureDropdown == null)
            {
                return;
            }

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
            if (_scenarioDropdown == null)
            {
                return;
            }

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
            if (_structureDropdown == null)
            {
                return BenchmarkStructureKind.Array;
            }

            return (BenchmarkStructureKind) _structureDropdown.value;
        }

        private BenchmarkScenario GetSelectedScenario()
        {
            if (_scenarioDropdown == null)
            {
                return BenchmarkScenario.SequentialIteration;
            }

            return (BenchmarkScenario) _scenarioDropdown.value;
        }

        private struct BatchRunResult
        {
            public readonly bool IsSuccess;
            public readonly bool IsSaved;
            public readonly string FilePath;

            private BatchRunResult(
                bool isSuccess,
                bool isSaved,
                string filePath)
            {
                IsSuccess = isSuccess;
                IsSaved = isSaved;
                FilePath = filePath;
            }

            public static BatchRunResult CreateSaved(string filePath)
            {
                return new BatchRunResult(true, true, filePath);
            }

            public static BatchRunResult CreateNotSaved()
            {
                return new BatchRunResult(true, false, "");
            }

            public static BatchRunResult CreateFailed()
            {
                return new BatchRunResult(false, false, "");
            }
        }
    }
}
