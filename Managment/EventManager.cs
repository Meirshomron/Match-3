using System.Collections.Generic;
using UnityEngine;
using System;

public class ActionParams
{
    public static readonly ActionParams EmptyParams = new ActionParams(true);

    private Dictionary<string, object> keyValuePairs;

    //This is done for optimiziation
    private ActionParams(bool seal) { }

    public ActionParams()
    {
        keyValuePairs = new Dictionary<string, object>();
    }

    public void Put<T>(string name, T val)
    {
        keyValuePairs.Add(name, val);
    }

    public static bool IsNull<T>(T obj)
    {
        return EqualityComparer<T>.Default.Equals(obj, default(T));
    }

    public T Get<T>(string name)
    {
        keyValuePairs.TryGetValue(name, out var result);
        return (T)result;
    }

    public void Update<T>(string name, T val)
    {
        keyValuePairs[name] = val;
    }
}

/// <summary>
/// Singleton EventManager.
/// </summary>
public class EventManager : MonoBehaviour
{
    protected Dictionary<string, List<Action<string, ActionParams>>> eventDictionary;

    private static EventManager _instance;

    public static EventManager Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

         print("EventManager: init");
        _instance = this;

        DontDestroyOnLoad(this.gameObject);

        // Creates dictionary for the events.
        if (eventDictionary == null)
        {
            eventDictionary = new Dictionary<string, List<Action<string, ActionParams>>>();
        }
    }

    /// <summary>
    /// function called to insert an event in the dictionary.
    /// </summary>
    /// <param name="eventName"> Event to listen to. </param>
    /// <param name="action"> Callback action to be called on event. </param>
    public static void StartListening(string eventName, Action<string, ActionParams> action)
    {
        List<Action<string, ActionParams>> actions;

        if (!Instance.eventDictionary.ContainsKey(eventName))
        {
            actions = new List<Action<string, ActionParams>>();
            Instance.eventDictionary.Add(eventName, actions);
        }
        else
        {
            Instance.eventDictionary.TryGetValue(eventName, out actions);

            if (actions == null)
            {
                Debug.Log("Wrong event name specified: " + eventName);
                return;
            }
        }

        actions.Add(action);
    }

    /// <summary>
    /// Removes an event from the dictionary.
    /// </summary>
    /// <param name="eventName"> Event to remove to. </param>
    /// <param name="action"> Callback action mapped to this event. </param>
    public static void StopListening(string eventName, Action<string, ActionParams> action)
    {
        if (_instance == null) return;
        if (Instance.eventDictionary.TryGetValue(eventName, out var actions))
        {
            // Hack: If we've failed to remove it then look for the action by method name.
            // TODO: Find out why we fail to find action listeners.
            bool success = actions.Remove(action);
            if (!success)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (actions[i].Method.Name == action.Method.Name)
                        actions.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Trigger an event with string params.
    /// </summary>
    /// <param name="eventName"> Event to trigger.</param>
    /// <param name="actionParams"> Params to pass to all the callback actions mapped to this event. </param>
    public static void TriggerEvent(string eventName, ActionParams actionParams = null)
    {
        if (Instance.eventDictionary.TryGetValue(eventName, out var actions))
        {
            // We iterate the list from end to start because invoke removes elements from the list
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                actions[i]?.Invoke(eventName, actionParams);
            }
        }
    }
}