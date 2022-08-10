using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public struct HeartData
{
    public (int, int) tileIndices;
    public Vector3 tilePosition;
    public GameObject heart;
    public int targetTileType;
    public Color color;

    public HeartData((int, int) tileIndices, Vector3 tilePosition, GameObject heart, int targetTileType, Color color)
    {
        this.tileIndices = tileIndices;
        this.tilePosition = tilePosition;
        this.targetTileType = targetTileType;
        this.heart = heart;
        this.color = color;
    }
}

public class SpreadLove : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "SpreadLove";
    private string m_newEffectID = "Heart";
    private float m_spreadHeartsDuration = 0.5f;
    private float m_heartsLingerDuration = 0.7f;
    private int m_totalSelectedTiles = 0;
    private const int m_totalAmountOfHeartsSent = 3;
    private List<HeartData> m_heartsData;
    private List<(int, int)> m_tilesSpread;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => false; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected) { }

    public void OnPowerupActivated()
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        List<(int, int)> tilesLanded = SelectedTiles();
        List<GameObject> hearts = SendHearts(tilesLanded);
        List<int> tileTypes = GenerateTileTypes(hearts.Count);
        m_tilesSpread = new List<(int, int)>();
        m_heartsData = new List<HeartData>();

        for (int i = 0; i < hearts.Count; i++)
        {
            GameObject tileGO = Board.Instance.CreateTile(TilesUtility.TILE_TYPE_PREFIX + tileTypes[i]);
            Color color = tileGO.GetComponent<SpriteRenderer>().color;
            Board.Instance.RemoveTile(tileGO.GetComponent<Tile>());

            hearts[i].GetComponent<SpriteRenderer>().color = color;
            m_heartsData.Add(new HeartData(tilesLanded[i], hearts[i].transform.position, hearts[i], tileTypes[i], color));
        }
    }

    /// <summary>
    /// Select <m_totalAmountOfHeartsSent> tiles to be the tiles we send hearts to land on.
    /// </summary>
    private List<(int, int)> SelectedTiles()
    {
        List<(int, int)> tilesLanded = new List<(int, int)>();

        tilesLanded.Add(GetRandomTileInRange(1, 3, (Board.Instance.COUNT_COLUMNS / 2) - 1, (Board.Instance.COUNT_COLUMNS / 2) + 2));
        tilesLanded.Add(GetRandomTileInRange(Board.Instance.COUNT_ROWS / 2, Board.Instance.COUNT_ROWS / 2 + 2, 1, 3));
        tilesLanded.Add(GetRandomTileInRange(Board.Instance.COUNT_ROWS / 2, Board.Instance.COUNT_ROWS / 2 + 2, Board.Instance.COUNT_COLUMNS - 1, Board.Instance.COUNT_COLUMNS - 3));
        return tilesLanded;
    }

    private (int, int) GetRandomTileInRange(int min1, int max1, int min2, int max2)
    {
        int randomRow = UnityEngine.Random.Range(min1, max1);
        int randomColumn = UnityEngine.Random.Range(min2, max2);
        while (!TilesUtility.IsTilePowerupEnabled((randomRow, randomColumn)))
        {
            randomRow = UnityEngine.Random.Range(min1, max1);
            randomColumn = UnityEngine.Random.Range(min2, max2);
        }
        return (randomRow, randomColumn);
    }

    /// <summary>
    /// Send heart to land on the selected tiles.
    /// </summary>
    private List<GameObject> SendHearts(List<(int, int)> tilesLanded)
    {
        List<GameObject> hearts = PowerupsUtility.CreateExtraListFromPool(tilesLanded, m_newEffectID);
        for (int i = 0; i < tilesLanded.Count; i++)
        {
            Animator anim = hearts[i].GetComponent<Animator>();
            anim.SetBool("isLand", true);
            StartCoroutine(OnHeartsLanded(anim, i));
        }
        TilesUtility.PlayTilesAnimations(tilesLanded, m_animController, true, this);

        return hearts;
    }

    /// <summary>
    /// Generate the tile types of the tiles to be created on every landing spot.
    /// </summary>
    private List<int> GenerateTileTypes(int count)
    {
        List<int> tileTypes = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int tileType;
            int.TryParse(TilesUtility.GetRandomTileType(false), out tileType);
            tileTypes.Add(tileType);
        }
        return tileTypes;
    }

    IEnumerator OnHeartsLanded(Animator anim, int index)
    {
        // Animation clip is currently null, so we wait.
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }

        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        SpreadHeartsAtIndex(index);
    }

    /// <summary>
    /// Called for every heart landed, create a spread of hearts of the same tile type in the tiles around it.
    /// </summary>
    private void SpreadHeartsAtIndex(int index)
    {
        for (int row = -1; row <= 1; row++)
        {
            for (int col = -1; col <= 1; col++)
            {
                if (!(row == 0 && col == 0))
                {
                    (int, int) targetIndices = (m_heartsData[index].tileIndices.Item1 + row, m_heartsData[index].tileIndices.Item2 + col);
                    if (Board.Instance.IsValidTileIndices(targetIndices) && TilesUtility.IsTilePowerupEnabled(targetIndices) && !m_tilesSpread.Contains(targetIndices))
                    {
                        // Spread the heart to the target tile's position and update it's spriteRenderer to the target color.
                        m_tilesSpread.Add(targetIndices);
                        GameObject heart = PowerupsUtility.CreateExtraFromPool(m_heartsData[index].tileIndices, m_newEffectID);
                        Animator anim2 = heart.GetComponent<Animator>();
                        anim2.SetBool("isBeat", true);

                        Vector3 targetPosition = Board.Instance.GetTilePosition(targetIndices);
                        heart.GetComponent<SpriteRenderer>().color = m_heartsData[index].color;
                        m_heartsData.Add(new HeartData(targetIndices, targetPosition, heart, m_heartsData[index].targetTileType, m_heartsData[index].color));

                        heart.transform.DOMove(targetPosition, m_spreadHeartsDuration);

                        // Fade out and remove the tile that was at this positon.
                        TilesUtility.PlayTileAnimation(targetIndices, m_animController, true, this);
                    }
                }
            }
        }

        if (index == (m_totalAmountOfHeartsSent - 1))
        {
            StartCoroutine(OnHeartsSpread());
        }
    }

    /// <summary>
    /// Called once all the spread hearts reached their tile destionat, remove all the hearts and create tiles of matching types to the hearts the're replacing.
    /// </summary>
    IEnumerator OnHeartsSpread()
    {
        yield return new WaitForSeconds(m_spreadHeartsDuration + m_heartsLingerDuration);

        for (int i = 0; i < m_heartsData.Count; i++)
        {
            PowerupsUtility.ReturnExtraToPool(this, m_heartsData[i].heart, m_animController);
            Board.Instance.CreateTile(m_heartsData[i].targetTileType, m_heartsData[i].tilePosition, m_heartsData[i].tileIndices);
        }
        Board.Instance.OnBoardUpdated();
    }
}
