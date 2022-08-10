using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Contagious : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "Contagious";
    private string m_newEffectID = "StarsBag";
    private float m_singleMovementDuration = 0.6f;
    private float m_probabilityAddingTarget = 0.6f;
    private int m_totalSelectedTiles = 1;
    private int m_effectsCounter = 0;
    private int m_maxAmountOfEffects = 9;
    private List<(int, int)> m_allTargetTiles;
    private List<(int, int)> m_activeTargetTiles;
    private List<GameObject> m_extras;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnPowerupActivated() { }

    public void OnTilesSelected((int, int)[] m_powerupTilesSelected)
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        m_effectsCounter = 0;
        (int, int) sourceTileIndices = m_powerupTilesSelected[0];
        m_extras = new List<GameObject>();
        m_allTargetTiles = new List<(int, int)>();
        m_activeTargetTiles = new List<(int, int)>();
        m_allTargetTiles.Add(sourceTileIndices);
        m_activeTargetTiles.Add(sourceTileIndices);

        List<(int, int)> nextTargets = GetNextTargets(sourceTileIndices);
        m_allTargetTiles.AddRange(nextTargets);
        m_activeTargetTiles.AddRange(nextTargets);
        CreateEffect(sourceTileIndices, sourceTileIndices);
        StartCoroutine(PlayEffectOnTileAndTargets(sourceTileIndices, nextTargets));
    }

    private List<(int, int)> GetNextTargets((int, int) sourceTileIndices)
    {
        List<(int, int)> nextTargets = new List<(int, int)>();

        (int, int) optionalTarget = (sourceTileIndices.Item1 + 1, sourceTileIndices.Item2);
        if (m_effectsCounter < m_maxAmountOfEffects && !m_allTargetTiles.Contains(optionalTarget) && UnityEngine.Random.Range(0, 101) <= m_probabilityAddingTarget * 100 && Board.Instance.IsValidTileIndices(optionalTarget) && TilesUtility.IsTilePowerupEnabled(optionalTarget))
        {
            m_effectsCounter++;
            nextTargets.Add(optionalTarget);
        }
        optionalTarget = (sourceTileIndices.Item1 - 1, sourceTileIndices.Item2);
        if (m_effectsCounter < m_maxAmountOfEffects && !m_allTargetTiles.Contains(optionalTarget) && UnityEngine.Random.Range(0, 101) <= m_probabilityAddingTarget * 100 && Board.Instance.IsValidTileIndices(optionalTarget) && TilesUtility.IsTilePowerupEnabled(optionalTarget))
        {
            m_effectsCounter++;
            nextTargets.Add(optionalTarget);
        }
        optionalTarget = (sourceTileIndices.Item1, sourceTileIndices.Item2 + 1);
        if (m_effectsCounter < m_maxAmountOfEffects && !m_allTargetTiles.Contains(optionalTarget) && UnityEngine.Random.Range(0, 101) <= m_probabilityAddingTarget * 100 && Board.Instance.IsValidTileIndices(optionalTarget) && TilesUtility.IsTilePowerupEnabled(optionalTarget))
        {
            m_effectsCounter++;
            nextTargets.Add(optionalTarget);
        }
        optionalTarget = (sourceTileIndices.Item1, sourceTileIndices.Item2 - 1);
        if (m_effectsCounter < m_maxAmountOfEffects && !m_allTargetTiles.Contains(optionalTarget) && UnityEngine.Random.Range(0, 101) <= m_probabilityAddingTarget * 100 && Board.Instance.IsValidTileIndices(optionalTarget) && TilesUtility.IsTilePowerupEnabled(optionalTarget))
        {
            m_effectsCounter++;
            nextTargets.Add(optionalTarget);
        }
        return nextTargets;
    }

    IEnumerator PlayEffectOnTileAndTargets((int, int) sourceTileIndices, List<(int, int)> targetTileIndices)
    {
        for (int i = 0; i < targetTileIndices.Count; i++)
        {
            CreateEffect(sourceTileIndices, targetTileIndices[i]);
        }

        yield return new WaitForSeconds(m_singleMovementDuration);

        if (m_effectsCounter >= m_maxAmountOfEffects)
            yield return null;

        bool found = false;
        m_activeTargetTiles.Remove(sourceTileIndices);
        for (int i = 0; i < m_activeTargetTiles.Count && !found; i++)
        {
            List<(int, int)> nextTargets = GetNextTargets(m_activeTargetTiles[i]);
            if (nextTargets.Count > 0)
            {
                found = true;
                m_allTargetTiles.AddRange(nextTargets);
                m_activeTargetTiles.AddRange(nextTargets);
                StartCoroutine(PlayEffectOnTileAndTargets(m_activeTargetTiles[i], nextTargets));
            }
            else
            {
                m_activeTargetTiles.Remove(m_activeTargetTiles[i]);
            }
        }
        if (!found)
        {
            OnPowerupComplete();
        }

    }

    private void CreateEffect((int, int) sourceTileIndices, (int, int) targetTileIndices)
    {
        Vector3 targetPosition = Board.Instance.GetTilePosition(targetTileIndices);
        GameObject extraGO = PowerupsUtility.CreateExtraFromPool(sourceTileIndices, m_newEffectID);
        extraGO.transform.DOMove(targetPosition, m_singleMovementDuration);
        m_extras.Add(extraGO);
        TilesUtility.PlayTileAnimation(targetTileIndices, m_animController);
    }

    private void OnPowerupComplete()
    {
        print("OnPowerupComplete");
        PowerupsUtility.ReturnExtraListToPool(this, m_extras, m_animController);
        PowerupManager.Instance.SetTileHit(m_allTargetTiles);
    }
}
