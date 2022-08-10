using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility previding different utility functionalities to the powerups.
/// </summary>
public static class PowerupsUtility
{
    public static float epsilon = 0.1f;
    public static Dictionary<int, Transform> m_extraToOriginalParent = new Dictionary<int, Transform>();

    /// <summary>
    /// Get the gameObjects from the pool that correspond to the the given pooledType and position them at the given tileIndices positions.
    /// </summary>
    public static List<GameObject> CreateExtraListFromPool(List<(int, int)> tileIndices, string pooledType)
    {
        List<GameObject> extras = new List<GameObject>();
        for (int i = 0; i < tileIndices.Count; i++)
        {
            extras.Add(CreateExtraFromPool(tileIndices[i], pooledType));
        }
        return extras;
    }

    /// <summary>
    /// Get the gameObject from the pool that corresponds to the the given pooledType and position it at the given tileIndices position.
    /// </summary>
    public static GameObject CreateExtraFromPool((int, int) tileIndices, string pooledType)
    {
        GameObject extraGO = ObjectPooler.Instance.GetPooledObject(pooledType);
        int extraInstanceID = extraGO.GetInstanceID();
        if (m_extraToOriginalParent.ContainsKey(extraInstanceID))
        {
            m_extraToOriginalParent[extraInstanceID] = extraGO.transform.parent;
        }
        else
        {
            m_extraToOriginalParent.Add(extraInstanceID, extraGO.transform.parent);
        }
        extraGO.transform.SetParent(PowerupManager.Instance.PowerupsExtraParent);
        extraGO.transform.position = Board.Instance.GetTilePosition(tileIndices);
        extraGO.SetActive(true);
        return extraGO;
    }

    /// <param name="min"> Minimum amount of tiles on each column. </param>
    /// <param name="max"> Maximum amount of tiles on each column. </param>
    /// <returns> List of indices of random tiles according to the range of amount of tiles to generate in every column. </returns>
    public static List<(int, int)> GetRandomTilesFullBoard(int min, int max)
    {
        List<(int, int)> result = new List<(int, int)>();
        for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
        {
            int amountOnColumn = UnityEngine.Random.Range(min, max);
            HashSet<int> selectedRows = new HashSet<int>();
            for (int i = 0; i < amountOnColumn; i++)
            {
                int row = UnityEngine.Random.Range(0, Board.Instance.COUNT_ROWS);
                int whileCounter = 10;
                while (whileCounter > 0 && (selectedRows.Contains(row) || !TilesUtility.IsTilePowerupEnabled((row, col))))
                {
                    row = UnityEngine.Random.Range(0, Board.Instance.COUNT_ROWS);
                    whileCounter--;
                }
                if (whileCounter > 0)
                {
                    selectedRows.Add(row);
                    result.Add((row, col));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Set the given animatorController on all the extras list of GameObjects, once the current animation completes return every extra to the pool.
    /// </summary>
    public static void ReturnExtraListToPool(MonoBehaviour powerup, List<GameObject> extras, RuntimeAnimatorController animatorController)
    {
        for (int i = 0; i < extras.Count; i++)
        {
            ReturnExtraToPool(powerup, extras[i], animatorController);
        }
    }

    /// <summary>
    /// Set the given animatorController on the extra GameObject, once the current animation completes return it to the pool.
    /// </summary>
    public static void ReturnExtraToPool(MonoBehaviour powerup, GameObject extra, RuntimeAnimatorController animatorController)
    {
        Animator anim = extra.GetComponent<Animator>();
        RuntimeAnimatorController originalAnimController = anim.runtimeAnimatorController;
        anim.runtimeAnimatorController = animatorController;
        powerup.StartCoroutine(ReturnOnAnimationComplete(anim, extra, originalAnimController));
    }

    /// <summary>
    /// Once the given animation completes return the given gameObject to the pool.
    /// </summary>
    public static IEnumerator ReturnOnAnimationComplete(Animator anim, GameObject gameObject, RuntimeAnimatorController originalAnimController = null)
    {
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }
        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

        if (originalAnimController)
        {
            anim.runtimeAnimatorController = originalAnimController;
        }

        Transform extraOriginalParent;
        int extraInstanceID = gameObject.GetInstanceID();
        m_extraToOriginalParent.TryGetValue(extraInstanceID, out extraOriginalParent);
        gameObject.transform.SetParent(extraOriginalParent);

        ObjectPooler.Instance.ReturnToPool(gameObject);
    }
}
