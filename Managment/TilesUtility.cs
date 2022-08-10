using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TilesUtility
{
    private const string m_tileTypePrefix = "Tile";
    private const string m_blockTileType = "Block";
    private const string m_blankTileType = "Blank";
    private static float m_tileWidth = 0;
    private static float m_tileHeight = 0;
    private static List<int> m_tileTypesAvailable;

    public static string TILE_TYPE_PREFIX => m_tileTypePrefix;
    public static string BLOCK_TILE_TYPE => m_blockTileType;
    public static string BLANK_TILE_TYPE => m_blankTileType;
    public static int TOTAL_AMOUNT_OF_TILE_TYPES => m_tileTypesAvailable.Count;

    public static void SetTileTypesAvailable(List<int> tilesTypes)
    {
        if (tilesTypes.Count == 0)
        {
            m_tileTypesAvailable = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                m_tileTypesAvailable.Add(i);
            }
        }
        else
        {
            m_tileTypesAvailable = tilesTypes;
        }
    }

    public static float GetTileWidth()
    {
        if (m_tileWidth == 0)
        {
            GameObject tileGO = Board.Instance.CreateTile(m_tileTypePrefix + "0");
            m_tileWidth = tileGO.GetComponent<SpriteRenderer>().bounds.size.x;
            m_tileHeight = tileGO.GetComponent<SpriteRenderer>().bounds.size.y;
            Board.Instance.RemoveTile(tileGO.GetComponent<Tile>());
        }

        return m_tileWidth;
    }

    public static float GetTileHeight()
    {
        if (m_tileWidth == 0)
        {
            GameObject tileGO = Board.Instance.CreateTile(m_tileTypePrefix + "0");
            m_tileWidth = tileGO.GetComponent<SpriteRenderer>().bounds.size.x;
            m_tileHeight = tileGO.GetComponent<SpriteRenderer>().bounds.size.y;
            Board.Instance.RemoveTile(tileGO.GetComponent<Tile>());
        }

        return m_tileHeight;
    }

    /// <summary>
    /// Sort function, sort according to lowest column first and inside the column, the highest row first.
    /// </summary>
    public static int SortLowestColumnHighestRow((int, int) p1, (int, int) p2)
    {
        if (p1.Item2 > p2.Item2)
            return 1;
        else if (p1.Item2 == p2.Item2 && (p1.Item1 < p2.Item1))
            return 1;

        return -1;
    }

    /// <summary>
    /// Sort function, sort according to lowest row first and inside the row, the highest column first.
    /// </summary>
    public static int SortLowestRowHighestColumn((int, int) p1, (int, int) p2)
    {
        if (p1.Item1 > p2.Item1)
            return 1;
        else if (p1.Item1 == p2.Item1 && (p1.Item2 < p2.Item2))
            return 1;

        return -1;
    }

    /// <summary>
    /// Sort function, sort according to lowest row first and inside the row, the lowest column first.
    /// </summary>
    public static int SortLowestRowLowestColumn((int, int) p1, (int, int) p2)
    {
        if (p1.Item1 > p2.Item1)
            return 1;
        else if (p1.Item1 == p2.Item1 && (p1.Item2 > p2.Item2))
            return 1;

        return -1;
    }

    /// <summary>
    /// Given a start and end position, check if the change of the end poisiton from the start position is larger on the X axis or Y axis. 
    /// If the Y axis has a larger change then we return (-1, 0) for increase on the Y axis and (1, 0) for a decrease in the Y axis from the start to the end position.
    /// If the X axis has a larger change then we return (0, 1) for increase on the X axis and (0, -1) for a decrease in the X axis from the start to the end position.
    /// The returned Tuple represents the change in the tiles 2D array from the startPosition's tile indices.
    /// </summary>
    public static (int, int) GetDirection(Vector3 startPosition, Vector3 endPosition)
    {
        (int, int) direction = (0, 0);
        Vector3 distance = endPosition - startPosition;
        if (Mathf.Abs(distance.y) > Mathf.Abs(distance.x))
            direction.Item1 = distance.y > 0 ? -1 : 1;
        else
            direction.Item2 = distance.x > 0 ? 1 : -1;
        return direction;
    }

    /// <summary>
    /// Return a random tile type.
    /// </summary>
    public static string GetRandomTileType(bool withPrefix = true)
    {
        int tileTypeIdx = UnityEngine.Random.Range(0, TOTAL_AMOUNT_OF_TILE_TYPES);
        return withPrefix ? m_tileTypePrefix + m_tileTypesAvailable[tileTypeIdx] : m_tileTypesAvailable[tileTypeIdx].ToString();
    }

    public static void PlayTilesAnimations(List<(int, int)> tilesHit, RuntimeAnimatorController animatorController, bool isRemoveOnComplete = false, MonoBehaviour powerup = null)
    {
        for (int i = 0; i < tilesHit.Count; i++)
        {
            PlayTileAnimation(tilesHit[i], animatorController, isRemoveOnComplete, powerup);
        }
    }

    public static void PlayTileAnimation((int, int) tile, RuntimeAnimatorController animatorController, bool isRemoveOnComplete = false, MonoBehaviour powerup = null)
    {
        Tile currentTile = Board.Instance.Tiles[tile.Item1, tile.Item2];
        Animator anim = currentTile.GetComponent<Animator>();
        anim.runtimeAnimatorController = animatorController;
        if (isRemoveOnComplete)
        {
            powerup.StartCoroutine(OnTileAnimationComplete(currentTile, anim));

        }
    }

    static IEnumerator OnTileAnimationComplete(Tile tile, Animator anim)
    {
        // Animation clip is currently null, so we wait.
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }

        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

        Board.Instance.RemoveTile(tile, anim);
    }

    public static bool IsTilesMatchEnabled((int, int) tile1, (int, int) tile2)
    {
        return IsTileMatchEnabled(tile1) && IsTileMatchEnabled(tile2);
    }

    public static bool IsTileMatchEnabled((int, int) tile)
    {
        return Board.Instance.Tiles[tile.Item1, tile.Item2].IsMatchEnabled;
    }

    public static bool IsTilePowerupEnabled((int, int) tile)
    {
        return Board.Instance.Tiles[tile.Item1, tile.Item2].IsPowerupEnabled;
    }

    public static bool IsTileMovable((int, int) tile)
    {
        return Board.Instance.Tiles[tile.Item1, tile.Item2].IsMovable;
    }
}
