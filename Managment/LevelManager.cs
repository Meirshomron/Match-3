using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LevelData
{
    public int numOfRows;
    public int numOfColumns;
    public int totalSwaps;
    public int amountOfPowerupsInLevel;
    public List<int> tileTypesAvailable;
    public List<int> blockTiles;
    public List<int> blankTiles;
    public List<string> powerupsAvailable;
    public List<int> targetScores;
    public List<string> targetTypes;
}

public class LevelManager : MonoBehaviour
{
    private string m_gameSceneId = "GameScene";
    private string m_mainMenuSceneId = "MainMenu";

    private int m_playerCurrentLevel;
    private LevelData m_currentLevelData;

    public LevelData LevelData => m_currentLevelData;
    public int PlayerCurrentLevel { get { return m_playerCurrentLevel; } set { m_playerCurrentLevel = value; } }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        AddListeners();
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioManager.Instance.PlayMusic("BG");
    }

    private void AddListeners()
    {
        EventManager.StartListening(EventNames.ON_LEVEL_CLICKED, OnLevelClicked);
        EventManager.StartListening(EventNames.ON_PLAY_NEXT_LEVEL_CLICKED, OnPlayNextLevelClicked);
        EventManager.StartListening(EventNames.ON_RESTART_LEVEL_CLICKED, OnRestartLevelClicked);
        EventManager.StartListening(EventNames.ON_PLAY_LEVEL_CLICKED, OnPlayLevelClicked);
        EventManager.StartListening(EventNames.ON_MENU_CLICKED, OnMenuClicked);
        EventManager.StartListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StartListening(EventNames.SHOW_LEVEL_SUCCESS_POPUP, ShowLevelSuccessPopup);
    }

    private void ShowLevelSuccessPopup(string eventName, ActionParams _data)
    {
        if (PlayerData.Instance.PlayerMaxLevel == m_playerCurrentLevel)
        {
            PlayerData.Instance.PlayerMaxLevel++;
        }
    }

    private void OnLoadScene(string eventName, ActionParams _data)
    {
        string sceneId = _data.Get<string>("sceneId");
        SceneManager.LoadScene(sceneId, LoadSceneMode.Single);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActionParams data = new ActionParams();
        data.Put("sceneId", scene.name);
        data.Put("levelData", m_currentLevelData);
        data.Put("playerCurrentLevel", m_playerCurrentLevel);
        EventManager.TriggerEvent(EventNames.ON_SCENE_LOADED, data);
    }

    private void OnPlayLevelClicked(string eventName, ActionParams _data)
    {
        print("LevelManager: OnPlayLevelClicked");

        Events.CustomData("OnLevelStarted", new Dictionary<string, object> { { "Level", m_playerCurrentLevel } });
        Events.Flush();

        string[] selectedPowerupIds = _data.Get<string[]>("selectedPowerupIds");
        PowerupManager.Instance.AvailablePowerups = selectedPowerupIds;
        ActionParams data = new ActionParams();
        data.Put("sceneId", m_gameSceneId);
        EventManager.TriggerEvent(EventNames.ON_LOAD_SCENE, data);
    }

    private void OnPlayNextLevelClicked(string eventName, ActionParams _data)
    {
        if (m_playerCurrentLevel < PlayerData.MAX_AVAILABLE_LEVELS)
        {
            m_playerCurrentLevel++;
            LoadCurrentLevel();
        }
        else
        {
            EventManager.TriggerEvent(EventNames.ON_MENU_CLICKED);
        }
    }

    private void OnRestartLevelClicked(string eventName, ActionParams _data)
    {
        LoadCurrentLevel();
    }

    private void OnLevelClicked(string eventName, ActionParams _data)
    {
        m_playerCurrentLevel = _data.Get<int>("level");
        LoadCurrentLevel();
    }

    /// <summary>
    /// Load the json of the current level and save the parsed level data. Show the previewLevelPopup.
    /// </summary>
    private void LoadCurrentLevel()
    {
        print("LevelManager: LoadLevel " + m_playerCurrentLevel);
        TextAsset fileData = Resources.Load("LevelData/level" + m_playerCurrentLevel) as TextAsset;
        if (fileData != null)
        {
            bool isInGame = SceneManager.GetActiveScene().name == m_gameSceneId;
            m_currentLevelData = JsonUtility.FromJson<LevelData>(fileData.text);
            ActionParams data = new ActionParams();
            data.Put("levelData", m_currentLevelData);
            data.Put("isInGame", isInGame);
            EventManager.TriggerEvent(EventNames.SHOW_PREVIEW_LEVEL_POPUP, data);
        }
    }

    private void OnMenuClicked(string eventName, ActionParams _data)
    {
        ActionParams data = new ActionParams();
        data.Put("sceneId", m_mainMenuSceneId);
        EventManager.TriggerEvent(EventNames.ON_LOAD_SCENE, data);
    }
}
