using System.Collections.Generic;
using BusJam.Data;
using BusJam.Events;
using UnityEngine;
using Zenject;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BusJam.Core
{
    public class LevelManager : MonoBehaviour, IInitializable
    {
        private SignalBus _signalBus;
        private List<LevelData> _allLevels = new List<LevelData>();
        private int _currentLevelIndex = 0;

        public int CurrentLevelIndex => _currentLevelIndex;
        public int TotalLevelsCount => _allLevels.Count;
        public LevelData CurrentLevel => _currentLevelIndex >= 0 && _currentLevelIndex < _allLevels.Count ? _allLevels[_currentLevelIndex] : null;
        public bool HasNextLevel => _currentLevelIndex < _allLevels.Count - 1;

        public void Initialize()
        {
            LoadAllLevelsFromResources();
            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void LoadAllLevelsFromResources()
        {
            _allLevels.Clear();
            
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_Game" });
            
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var levelData = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(path);
                
                if (levelData != null && levelData.ValidateLevel())
                {
                    _allLevels.Add(levelData);
                }
            }
#else
            var levelDataAssets = Resources.LoadAll<LevelData>("Levels");
            foreach (var levelData in levelDataAssets)
            {
                if (levelData != null && levelData.ValidateLevel())
                {
                    _allLevels.Add(levelData);
                }
            }
#endif
            
            _allLevels.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            
            Debug.Log($"[LEVEL MANAGER] Loaded {_allLevels.Count} levels from Assets/_Game");
        }

        public bool LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= _allLevels.Count)
            {
                Debug.LogError($"[LEVEL MANAGER] Invalid level index: {levelIndex}");
                return false;
            }

            var previousIndex = _currentLevelIndex;
            _currentLevelIndex = levelIndex;
            var currentLevel = _allLevels[_currentLevelIndex];
            
            _signalBus.Fire(new LevelChangedSignal(previousIndex, _currentLevelIndex, currentLevel));
            
            return true;
        }

        public bool LoadNextLevel()
        {
            if (!HasNextLevel) return false;
            return LoadLevel(_currentLevelIndex + 1);
        }

        public bool LoadPreviousLevel()
        {
            if (_currentLevelIndex <= 0) return false;
            return LoadLevel(_currentLevelIndex - 1);
        }

        public void RestartCurrentLevel()
        {
            if (CurrentLevel != null)
            {
                _signalBus.Fire(new LevelRestartedSignal(_currentLevelIndex, CurrentLevel));
            }
        }

        public List<LevelData> GetAllLevels()
        {
            return new List<LevelData>(_allLevels);
        }

        public LevelData GetLevel(int index)
        {
            if (index >= 0 && index < _allLevels.Count)
            {
                return _allLevels[index];
            }
            return null;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
        }

        private void OnLevelCompleted()
        {
            if (HasNextLevel)
            {
                _signalBus.Fire(new NextLevelAvailableSignal(_currentLevelIndex + 1));
            }
            else
            {
                _signalBus.Fire<AllLevelsCompletedSignal>();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelCompletedSignal>(OnLevelCompleted);
        }
    }
}