using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatherTwoSides : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private GameObject m_calloutInstance;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "GatherTwoSides";
    private string m_tileTypeSide1;
    private string m_tileTypeSide2;
    private float m_singleSwapDuration = 0.4f;
    private float m_startGatheringDelay = 1f;
    private int m_totalSelectedTiles = 0;
    private Dictionary<Tile, int> m_tileToTargetMap;
    private GatherTwoSidesCallout m_gatherTwoSidesCallout;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected){}

    public void OnPowerupActivated()
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        GetTileTypes();
        InitCallout();
        CalcTileTargets();
        StartCoroutine(StartGathering());
    }

    /// <summary>
    /// Get the tile types of the tiles to gather to each side.
    /// </summary>
    private void GetTileTypes()
    {
        m_tileTypeSide1 = TilesUtility.GetRandomTileType(true);
        m_tileTypeSide2 = TilesUtility.GetRandomTileType(true);
        while (m_tileTypeSide1 == m_tileTypeSide2)
        {
            m_tileTypeSide2 = TilesUtility.GetRandomTileType(true);
        }
        print("GatherTwoSides: tileTypeSide1 = " + m_tileTypeSide1 + " tileTypeSide2 = " + m_tileTypeSide2);
    }

    /// <summary>
    /// Iterate the board's tiles, track all the tiles that need to be gathered and calculate for each of them their destination column.
    /// </summary>
    private void CalcTileTargets()
    {
        m_tileToTargetMap = new Dictionary<Tile, int>();
        List<(int, int)> tilesSide1 = new List<(int, int)>();
        List<(int, int)> tilesSide2 = new List<(int, int)>();
        for (int row = 0; row < Board.Instance.COUNT_ROWS; row++)
        {
            for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
            {
                string tileType = Board.Instance.Tiles[row, col].TileType;
                if (tileType == m_tileTypeSide1)
                {
                    tilesSide1.Add((row, col));
                }
                else if (tileType == m_tileTypeSide2)
                {
                    tilesSide2.Add((row, col));
                }
            }
        }

        // Every tile to be gathered in the tilesSide1 moves in the direction of column 0.
        // Every tile to be gathered in the tilesSide2 moves in the direction of column (Board.Instance.COUNT_COLUMNS - 1).
        // For every tile to be gathered set its target column according to the amount of tiles ahead of it to be gathered in its row.
        tilesSide1.Sort(TilesUtility.SortLowestRowLowestColumn);
        tilesSide2.Sort(TilesUtility.SortLowestRowHighestColumn);
        int amonutInRow = 0;
        for (int i = 0; i < tilesSide1.Count; i++)
        {
            if (i > 0 && tilesSide1[i].Item1 != tilesSide1[i - 1].Item1)
            {
                amonutInRow = 0;
            }
            // If the target isn't movable then we can't move there, so move to the next possible target.
            while (!TilesUtility.IsTileMovable((tilesSide1[i].Item1, amonutInRow)))
            {
                amonutInRow++;
            }

            m_tileToTargetMap.Add(Board.Instance.Tiles[tilesSide1[i].Item1, tilesSide1[i].Item2], amonutInRow);
            amonutInRow++;
        }

        amonutInRow = 0;
        for (int i = 0; i < tilesSide2.Count; i++)
        {
            if (i > 0 && tilesSide2[i].Item1 != tilesSide2[i - 1].Item1)
            {
                amonutInRow = 0;
            }

            // If the target isn't movable then we can't move there, so move to the next possible target.
            while (!TilesUtility.IsTileMovable((tilesSide2[i].Item1, Board.Instance.COUNT_COLUMNS - amonutInRow - 1)))
            {
                amonutInRow++;
            }

            m_tileToTargetMap.Add(Board.Instance.Tiles[tilesSide2[i].Item1, tilesSide2[i].Item2], Board.Instance.COUNT_COLUMNS - amonutInRow - 1);
            amonutInRow++;
        }
    }

    /// <summary>
    /// Callout showing the type of tiles to be gathered and their direction (column 0 / (Board.Instance.COUNT_COLUMNS - 1)).
    /// </summary>
    private void InitCallout()
    {
        if (!m_gatherTwoSidesCallout)
        {
            m_gatherTwoSidesCallout = Instantiate(m_calloutInstance).GetComponent<GatherTwoSidesCallout>();
            m_gatherTwoSidesCallout.GetComponent<Canvas>().overrideSorting = true;
        }
        else
        {
            m_gatherTwoSidesCallout.gameObject.SetActive(true);
        }
        m_gatherTwoSidesCallout.Init(m_tileTypeSide1, m_tileTypeSide2);
    }

    IEnumerator StartGathering()
    {
        yield return new WaitForSeconds(m_startGatheringDelay);
        StartCoroutine(GatherTiles());
    }

    /// <summary>
    /// Swap all the tiles to be gathered that haven't reached their target in the direction of their target. 
    /// After Waiting for the swap to complete - call this coroutine again.
    /// If all the tiles reached their target column - end this powerup.
    /// </summary>
    IEnumerator GatherTiles()
    {
        bool isAllTileGatheringComplete = true;
        // List of all the tiles being swapped in this current swap cycle, we do this to avoid a case of adjacent tiles that need to be swapped in opposite directions - only call to swap them once.
        List<(int, int)> partOfSwapProcessList = new List<(int, int)>();
        foreach (KeyValuePair<Tile, int> tileToTarget in m_tileToTargetMap)
        {
            (int, int) tileIndices = Board.Instance.GetTileIndices(tileToTarget.Key.gameObject);
            if (tileIndices.Item2 == tileToTarget.Value)
            {
                continue;
            }
            else if (tileIndices.Item2 < tileToTarget.Value)
            {
                (int, int) targetTileIndices = (tileIndices.Item1, tileIndices.Item2 + 1);

                // Don't swap with a non-movable target, so swap with the next movable tile.
                while (!TilesUtility.IsTileMovable(targetTileIndices))
                {
                    targetTileIndices.Item2++;
                }
                
                if (!partOfSwapProcessList.Contains(targetTileIndices) && !partOfSwapProcessList.Contains(tileIndices))
                {
                    isAllTileGatheringComplete = false;
                    partOfSwapProcessList.Add(tileIndices);
                    partOfSwapProcessList.Add(targetTileIndices);
                    Board.Instance.SwapTiles(tileIndices, targetTileIndices, m_singleSwapDuration);
                }
            }
            else if (tileIndices.Item2 > tileToTarget.Value)
            {
                (int, int) targetTileIndices = (tileIndices.Item1, tileIndices.Item2 - 1);

                // Don't swap with a non-movable target, so swap with the next movable tile.
                while (!TilesUtility.IsTileMovable(targetTileIndices))
                {
                    targetTileIndices.Item2--;
                }

                if (!partOfSwapProcessList.Contains(targetTileIndices) && !partOfSwapProcessList.Contains(tileIndices))
                {
                    isAllTileGatheringComplete = false;
                    partOfSwapProcessList.Add(tileIndices);
                    partOfSwapProcessList.Add(targetTileIndices);
                    Board.Instance.SwapTiles(tileIndices, targetTileIndices, m_singleSwapDuration);
                }
            }
        }
        yield return new WaitForSeconds(m_singleSwapDuration);

        if (isAllTileGatheringComplete)
        {
            OnGatherComplete();
        }
        else
        {
            StartCoroutine(GatherTiles());
        }
    }

    private void OnGatherComplete()
    {
        PowerupManager.Instance.DeactivatePowerup();
        Board.Instance.OnBoardUpdated(0);
    }
}
