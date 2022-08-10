using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoolItemType
{
    public string type;
    public int amountToPool;
    public GameObject objectToPool;
    public bool shouldExpand;
}

/// <summary>
/// Singleton gameObjects pool.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    private static ObjectPooler _instance;

    public static ObjectPooler Instance
    {
        get { return _instance; }
    }

    [SerializeField] private List<PoolItemType> poolItemTypes;
    private Dictionary<int, Coroutine> autoDeactivateDic;
    private Dictionary<string, List<GameObject>> poolObjectsDic;

    // Container to hold all the pool objects in the scene.
    private GameObject poolParent;

    public bool IsPoolReady = false;

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

    void Start()
    {
        poolObjectsDic = new Dictionary<string, List<GameObject>>();
        autoDeactivateDic = new Dictionary<int, Coroutine>();
        poolParent = new GameObject("PoolParent");
        DontDestroyOnLoad(poolParent);

        // Create all the gameObjects for every PoolItemType.
        // All the gameObjects of every PoolItemType are set under a paren with the name of the PoolItemType.type. 
        // All the PoolItemType parents are set under the PoolParent.
        foreach (PoolItemType item in poolItemTypes)
        {
            GameObject poolItemType = new GameObject(item.type);
            poolItemType.transform.SetParent(poolParent.transform);
            List<GameObject> PoolItemsOfType = new List<GameObject>();

            for (int i = 0; i < item.amountToPool; i++)
            {
                GameObject obj = (GameObject)Instantiate(item.objectToPool);
                obj.SetActive(false);
                PoolItemsOfType.Add(obj);
                obj.transform.SetParent(poolItemType.transform);
            }
            poolObjectsDic.Add(item.type, PoolItemsOfType);
        }
        IsPoolReady = true;
    }

    /// <summary>
    /// Get a GameObject from the pool of the given type.
    /// </summary>
    /// <param name="type"> The type of PoolItemType that we want to get its configured GameObject. </param>
    /// <returns>GameObject instance of the set prefab of the given type.</returns>
    public GameObject GetPooledObject(string type)
    {
        //print("GetPooledObject: type = " + type);
        // Find a matching in-active GameObject from the pool and return it.
        List<GameObject> poolItemsOfType;
        if (poolObjectsDic.TryGetValue(type, out poolItemsOfType))
        {
            for (int i = 0; i < poolItemsOfType.Count; i++)
            {
                if (!poolItemsOfType[i].activeInHierarchy)
                {
                    return poolItemsOfType[i];
                }
            }
        }

        // No Available desired GameObject so check if the 'shouldExpand' flag is turned on for the desired type, and if so - create, add and return it. 
        foreach (PoolItemType item in poolItemTypes)
        {
            if (item.type == type)
            {
                if (item.shouldExpand)
                {
                    GameObject obj = (GameObject)Instantiate(item.objectToPool);
                    obj.SetActive(false);
                    GameObject poolItemType = poolParent.transform.Find(type).gameObject;
                    obj.transform.SetParent(poolItemType.transform);
                    poolItemsOfType.Add(obj);
                    poolObjectsDic[type] = poolItemsOfType;
                    return obj;
                }
                else
                {
                    break;
                }
            }
        }

        return null;
    }

    /// <summary>
    ///  Overload function of GetPooledObject above, but add the ability to receive the autoDeactivateCountdown param.
    /// </summary>
    /// <param name="type">The type of PoolItemType that we want to get its configured GameObject.</param>
    /// <param name="autoDeactivateCountdown">Countdown to destroy the returned GameObject.</param>
    /// <returns>>GameObject instance of the set prefab of the given type.</returns>
    public GameObject GetPooledObject(string type, float autoDeactivateCountdown)
    {
        GameObject pooledObject = GetPooledObject(type);
        if (pooledObject)
        {
            Coroutine pooledObjectCoroutine = StartCoroutine(AutoReturnWithDelay(pooledObject, autoDeactivateCountdown));
            autoDeactivateDic.Add(pooledObject.GetInstanceID(), pooledObjectCoroutine);
        }
        return pooledObject;
    }

    /// <summary>
    /// Return a GameObject to the pool.
    /// </summary>
    /// <param name="pooledObject"> The GameObject to return to the pool.</param>
    public void ReturnToPool(GameObject pooledObject)
    {
        // If a Coroutine is mapped to this gameObject to auto-destroy it then stop it.
        Coroutine autoDeactivateCoroutine = null;
        if (autoDeactivateDic.TryGetValue(pooledObject.GetInstanceID(), out autoDeactivateCoroutine))
        {
            StopCoroutine(autoDeactivateCoroutine);
            autoDeactivateDic.Remove(pooledObject.GetInstanceID());
        }
        pooledObject.SetActive(false);
    }

    public void ReturnTypeToPool(string type)
    {
        //print("Pool: Returning type = " + type);

        List<GameObject> poolItemsOfType;
        if (poolObjectsDic.TryGetValue(type, out poolItemsOfType))
        {
            for (int i = 0; i < poolItemsOfType.Count; i++)
            {
                ReturnToPool(poolItemsOfType[i]);
            }
        }
    }

    /// <summary>
    /// Auto destroy the given GameObject after the the given seconds.
    /// </summary>
    /// <param name="pooledObject">The GameObject to return to the pool.</param>
    /// <param name="countdown">The delay time before returning the given GameObject.</param>
    IEnumerator AutoReturnWithDelay(GameObject pooledObject, float countdown)
    {
        yield return new WaitForSeconds(countdown);

        ReturnToPool(pooledObject);
    }
}