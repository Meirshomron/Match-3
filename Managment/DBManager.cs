using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DBManager : MonoBehaviour
{
    private const string DATABASE_URL = "https://matchtrash-b4117-default-rtdb.europe-west1.firebasedatabase.app/";
    private FirebaseDatabase m_database;
    private static DBManager _instance;
    private DatabaseReference rootReference;

    public static DBManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(this.gameObject);

        m_database = FirebaseDatabase.GetInstance(DATABASE_URL);
        rootReference = m_database.RootReference;
    }

    public void WriteLevel(string key, HighscoreData highscoreData)
    {
        string json = JsonUtility.ToJson(highscoreData);
        rootReference.Child("Levels").Child(key).SetRawJsonValueAsync(json);
    }

    public void ReadLevel(string key, Action<HighscoreData> callback, Action<string> onFailedCallback)
    {
        //print("DBManager: ReadValue key = " + key);
        rootReference.Child("Levels").Child(key).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                 print(task.Exception.Message);
                // TODO: Handle the error...
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                string jsonStr = snapshot.GetRawJsonValue();
                //Debug.Log(jsonStr);
                if (snapshot.Exists)
                {
                    HighscoreData highscoreData = JsonUtility.FromJson<HighscoreData>(jsonStr);
                    callback?.Invoke(highscoreData);
                }
                else
                {
                    onFailedCallback?.Invoke(key);
                }
            }
        });
    }
}
