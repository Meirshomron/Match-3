using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HighscoreData
{
    public string username;
    public int score;
    public int level;

    public override string ToString()
    {
        return "level = " + level + " score = " + score + " username = " + username;
    }
}

public static class ScoreCalculator
{
    public static int Calc(int swapsLeft, int totalHit, int targetTotalHit)
    {
        int baseScore = 200;
        float swapLeftMult = swapsLeft * 0.2f;
        float hitMult = (totalHit - targetTotalHit) * 0.015f;
        return Mathf.RoundToInt(baseScore * (1 + swapLeftMult + hitMult));
    }
}


public class PlayerData : MonoBehaviour
{
    [SerializeField] private int m_playerMaxLevel;

    private Dictionary<int, int> m_playerTopScores;
    private Dictionary<int, HighscoreData> m_highscores;
    private static PlayerData _instance;

    public const string levelPrefix = "level";
    public const int MAX_AVAILABLE_LEVELS = 20;

    public int PlayerMaxLevel { get { return m_playerMaxLevel; } set { m_playerMaxLevel = Mathf.Clamp(value, 0, MAX_AVAILABLE_LEVELS); } }
    public static PlayerData Instance{ get { return _instance; }}


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        InitPlayerTopSores();
        InitHighscores();
    }

    private void InitPlayerTopSores()
    {
        //print("PlayerData: InitPlayerTopSores");
        m_playerTopScores = PlayerLevelScores.ReadPlayerTopScores();
        m_playerMaxLevel = Mathf.Min(MAX_AVAILABLE_LEVELS, m_playerTopScores.Count + 1);
    }

    private void InitHighscores()
    {
        m_highscores = new Dictionary<int, HighscoreData>();
        for (int i = 0; i < m_playerTopScores.Count; i++)
        {
            int level = (i + 1);
            Action<HighscoreData> callback = new Action<HighscoreData>(OnGetHighscoreComplete);
            Action<string> onFailedCallback = new Action<string>(OnFailedReadingHighscore);
            DBManager.Instance.ReadLevel(levelPrefix + level, callback, onFailedCallback);
        }
    }

    /// <summary>
    /// In case of failing to read the value of the given key from the DB, set the value for this session as the same top value saved to disk on this player.
    /// We hope next session we wont fail to read it from the DB.
    /// </summary>
    public void OnFailedReadingHighscore(string key)
    {
        print("PlayerData: OnFailedReadingHighscore key = " + key);

        int level = int.Parse(key.Substring(levelPrefix.Length));
        HighscoreData highscoreData = new HighscoreData();
        highscoreData.level = level;
        highscoreData.score = m_playerTopScores[level];
        highscoreData.username = AuthManager.Instance.GetMyDisplayName();

        if (m_highscores.ContainsKey(level))
        {
            m_highscores[level] = highscoreData;
        }
        else
        {
            m_highscores.Add(level, highscoreData);
        }
    }

    public void OnGetHighscoreComplete(HighscoreData data)
    {
        m_highscores.Add(data.level, data);
    }

    private void OnGetHighscoreOnLevelComplete(HighscoreData data)
    {
        //print("PlayerData: OnGetHighscoreOnLevelComplete " + data.ToString());
        OnGetHighscoreComplete(data);
        int levelCompleteScore = m_playerTopScores[data.level];
        if (levelCompleteScore > data.score)
        {
            UpdateTopScoreOnDB(data.level, levelCompleteScore);
            EventManager.TriggerEvent(EventNames.ON_NEW_HIGHSCORE);
        }
        else
        {
            //print("current player top score = " + levelCompleteScore + " highscore = " + data.score);
        }
    }

    /// <summary>
    /// Make sure the highscores on disk are smaller/equal to the values in the db.
    /// </summary>
    public void ValidateHighscoresDB()
    {
        int highscoreDisk;
        HighscoreData highscoreDB;
        bool success;
        for (int level = 1; level <= m_playerTopScores.Count; level++)
        {
            highscoreDisk = m_playerTopScores[level];
            success = m_highscores.TryGetValue(level, out highscoreDB);
            if (success)
            {
                if (highscoreDisk > highscoreDB.score)
                {
                    UpdateTopScoreOnDB(level, highscoreDisk);
                }
            }
            else
            {
                Debug.LogError("Failed reading level " + level + " from DB highscores.");
            }
        }
    }

    public Dictionary<int, int> GetAllPlayerTopScores()
    {
        return m_playerTopScores; 
    }

    public Dictionary<int, HighscoreData> GetAllHighscores()
    {
        return m_highscores;
    }

    public void UpdateTopScoreOnDB(int level, int score)
    {
        m_highscores[level].score = score;
        m_highscores[level].username = AuthManager.Instance.GetMyDisplayName();
        DBManager.Instance.WriteLevel(levelPrefix + level, m_highscores[level]);
    }

    public void SetScoreAtLevel(int level, int score)
    {
        if (m_playerTopScores.Count >= level)
        {
            if (m_playerTopScores[level] < score)
            {
                m_playerTopScores[level] = score;
                UpdateTopScoresOnDisk();
            }
        }
        else
        {
            m_playerTopScores.Add(level, score);
            UpdateTopScoresOnDisk();
        }

        HighscoreData highscore;
        int playerTopScore = m_playerTopScores[level];
        bool success = m_highscores.TryGetValue(level, out highscore);
        if (success)
        {
            //print("highscore = " + highscore);
            //print("current player top score = " + playerTopScore + " highscore = " + highscore.score);

            if (playerTopScore > highscore.score)
            {
                UpdateTopScoreOnDB(level, playerTopScore);
                EventManager.TriggerEvent(EventNames.ON_NEW_HIGHSCORE);
            }
        }
        else
        {
            Action<HighscoreData> callback = new Action<HighscoreData>(OnGetHighscoreOnLevelComplete);
            Action<string> onFailedCallback = new Action<string>(OnFailedReadingHighscore);
            DBManager.Instance.ReadLevel(levelPrefix + level, callback, onFailedCallback);
        }
    }

    private void UpdateTopScoresOnDisk()
    {
        if (m_playerTopScores != null && m_playerTopScores.Count > 0)
        {
            PlayerLevelScores.WritePlayerTopScores(m_playerTopScores);
        }
    }
}
