using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoMatch : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private int m_totalAutoMatches;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "AutoMatch";
    private float m_autoMatchDuration = 0.5f;
    private int m_totalSelectedTiles = 0;
    private int m_autoMatchCounter;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected) { }

    public void OnPowerupActivated() 
    {
        m_autoMatchCounter = 0;
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);
        StartCoroutine(PerformAutoMatch());
    }

    /// <summary>
    /// Try to find a move on the board that creates a match and perform the moves.
    /// Re call the coroutine <m_totalAutoMatches> times. 
    /// Once we found <m_totalAutoMatches> matches or their wasn't enough single moves to create a matche call OnAutoMatchComplete() to complete the powerup.
    /// </summary>
    IEnumerator PerformAutoMatch()
    {
        m_autoMatchCounter++;
        bool found = false;
        for (int row = 0; row < Board.Instance.COUNT_ROWS && !found; row++)
        {
            for (int col = 0; col < Board.Instance.COUNT_COLUMNS && !found; col++)
            {
                if (TilesUtility.IsTileMatchEnabled((row, col)))
                {
                    (int, int) targetTile = MatchesUtility.IsOneMoveFromMatch(row, col, Board.Instance.Tiles[row, col].TileType);
                    if (!Utils.IsTupleEmpty(targetTile))
                    {
                        Board.Instance.SwapTiles((row, col), targetTile, m_autoMatchDuration);
                        found = true;
                    }
                }
            }
        }

        if (found && m_autoMatchCounter < m_totalAutoMatches)
        {
            yield return new WaitForSeconds(m_autoMatchDuration);
            StartCoroutine(PerformAutoMatch());
        }
        else
        {
            yield return null;
            StartCoroutine(OnAutoMatchComplete());
        }
    }

    IEnumerator OnAutoMatchComplete()
    {
        yield return new WaitForSeconds(m_autoMatchDuration);
        PowerupManager.Instance.DeactivatePowerup();
        Board.Instance.OnBoardUpdated();
    }
}
