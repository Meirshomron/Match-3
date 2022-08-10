using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

struct PowerupTileData
{
    public string type;
    public GameObject tile;

    public PowerupTileData(string type, GameObject tile)
    {
        this.type = type;
        this.tile = tile;
    }
}

public class PowerupsBoard : MonoBehaviour
{
    [SerializeField] private float m_activityMovementDuration = 1f;
    [SerializeField] private GameObject m_boardParent;
    [SerializeField] private RectTransform m_background;
    [SerializeField] private RectTransform m_backgroundShadow;
    [SerializeField] private RectTransform m_activityBtn;
    [SerializeField] private RectTransform m_infoBtn;
    [SerializeField] private GameObject m_infoPopupPrefab;

    private string m_emptyTileType = "EmptyPowerupTile";
    private string m_fullTileType = "FullPowerupTile";
    private float m_tileWidth;
    private float m_tileHeight;
    private float m_tileScaleFactor;
    private float m_boardWidth;
    private float m_boardOffset;
    private float m_boardHeight;
    private bool m_isBoardActive;
    private Transform m_boardOriginalParent;
    private PowerupTileData[,] m_tiles;
    private Tween m_activityTween;
    private GameObject m_infoPopup;
    private Vector3 m_turnFullTilePunchScale = new Vector3(.25f, .25f);
    private Vector2 m_m_backgroundShadowOffset = new Vector2(10, 10);

    private void Start()
    {
        print("PowerupsBoard: Start");
        Init();
        CreateBoard();
        AddListeners();
    }

    private void AddListeners()
    {
        EventManager.StartListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StartListening(EventNames.ON_TILES_HIT, OnTilesHit);
    }

    private void RemoveListeners()
    {
        EventManager.StopListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StopListening(EventNames.ON_TILES_HIT, OnTilesHit);
    }

    private void OnLoadScene(string eventName, ActionParams _data)
    {
        ReturnAllTilesToPool();
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    public void OnShowInfoPopup()
    {
        if (!m_infoPopup)
        {
            m_infoPopup = Instantiate(m_infoPopupPrefab.gameObject);
        }
        m_infoPopup.GetComponent<PowerupsBoardInfoPopup>().Init();
    }

    private void ReturnAllTilesToPool()
    {
        for (int row = 0; row < Board.Instance.COUNT_ROWS; row++)
        {
            for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
            {
                PowerupTileData powerupTileData = m_tiles[row, col];
                if (powerupTileData.tile != null)
                {
                    powerupTileData.tile.transform.SetParent(m_boardOriginalParent);
                    ObjectPooler.Instance.ReturnToPool(powerupTileData.tile);
                }
            }
        }
    }

    private void Init()
    {
        m_boardOffset = m_boardParent.GetComponent<RectTransform>().anchoredPosition.x;
        m_isBoardActive = true;
        GameObject emptyTile = ObjectPooler.Instance.GetPooledObject(m_emptyTileType);
        m_tileWidth = emptyTile.GetComponent<RectTransform>().rect.width;
        m_tileHeight = emptyTile.GetComponent<RectTransform>().rect.height;
        m_boardOriginalParent = emptyTile.transform.parent;
        emptyTile.transform.SetParent(m_boardParent.transform);
        m_tileScaleFactor = emptyTile.GetComponent<RectTransform>().localScale.x;
        emptyTile.transform.SetParent(m_boardOriginalParent);
        ObjectPooler.Instance.ReturnToPool(emptyTile);
        m_boardWidth = Board.Instance.COUNT_COLUMNS * m_tileWidth * m_tileScaleFactor;
        m_boardHeight = Board.Instance.COUNT_ROWS * m_tileHeight * m_tileScaleFactor;
        m_activityBtn.localPosition = new Vector2(m_boardWidth, m_boardHeight / 2.0f);
        m_infoBtn.localPosition = new Vector2(m_infoBtn.rect.width / 2.0f, m_boardHeight + m_infoBtn.rect.height/2.0f + m_infoBtn.rect.width / 2.0f);
    }

    /// <summary>
    /// Create a board the same amount of rows and columns as the game board. Handle tiles that aren't movable or have matching enabled on them.
    /// </summary>
    private void CreateBoard()
    {
        m_tiles = new PowerupTileData[Board.Instance.COUNT_ROWS, Board.Instance.COUNT_COLUMNS];
        for (int row = 0; row < Board.Instance.COUNT_ROWS; row++)
        {
            for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
            {
                if (TilesUtility.IsTileMatchEnabled((row, col)) || TilesUtility.IsTileMovable((row, col)))
                {
                    GameObject tileGO = ObjectPooler.Instance.GetPooledObject(m_emptyTileType);
                    tileGO.SetActive(true);
                    tileGO.transform.SetParent(m_boardParent.transform);

                    tileGO.GetComponent<RectTransform>().localPosition = new Vector3(col * m_tileWidth * m_tileScaleFactor + (m_tileWidth * m_tileScaleFactor / 2), (Board.Instance.COUNT_ROWS - row - 1) * m_tileHeight * m_tileScaleFactor + (m_tileHeight * m_tileScaleFactor / 2));
                    m_tiles[row, col] = new PowerupTileData(m_emptyTileType, tileGO);
                }
                else
                {
                    m_tiles[row, col] = new PowerupTileData();
                    m_tiles[row, col].type = m_emptyTileType;
                }
            }
        }

        m_background.sizeDelta = new Vector2(m_boardWidth, m_boardHeight);
        m_backgroundShadow.sizeDelta = new Vector2(m_boardWidth, m_boardHeight) + m_m_backgroundShadowOffset;
    }

    /// <summary>
    /// Enable toggling between showing and hiding the powerup's board.
    /// </summary>
    public void OnActivityBtnClicked()
    {
        if (m_activityTween != null)
        {
            m_activityTween.Kill();
        }

        if (m_isBoardActive)
        {
            m_activityTween =  m_boardParent.GetComponent<RectTransform>().DOAnchorPosX(-m_boardWidth - m_m_backgroundShadowOffset.x/2.0f, m_activityMovementDuration).SetEase(Ease.OutCubic);
        }
        else
        {
            m_activityTween = m_boardParent.GetComponent<RectTransform>().DOAnchorPosX(m_boardOffset, m_activityMovementDuration).SetEase(Ease.OutCubic);
        }
        m_isBoardActive = !m_isBoardActive;
    }

    /// <summary>
    /// Called every time a tile is hit on the game board. 
    /// If it was caused be a powerup then do nothing, otherwise we set the matching tile in the (row, column) of the tile that was hit as full in the pwoerup's board.
    /// </summary>
    private void OnTilesHit(string eventName, ActionParams _data)
    {
        bool isPowerupActive = _data.Get<bool>("isPowerupActive");
        if (isPowerupActive)
        {
            return;
        }

        List<(int, int)> tilesHit = _data.Get<List<(int, int)>>("tilesHit");
        for (int i = 0; i < tilesHit.Count; i++)
        {
            // Change tile from empty to full.
            ChangeTileType(tilesHit[i], m_emptyTileType, m_fullTileType);
        }

        CheckForSetComplete(tilesHit);
    }

    /// <summary>
    /// Change a tile located at the given tile indices from the <sourceType> type to the <targetType> type. currently only 2 types of empty/full.
    /// </summary>
    /// <param name="tileIndices"></param>
    /// <param name="sourceType"></param>
    /// <param name="targetType"></param>
    private void ChangeTileType((int, int) tileIndices, string sourceType, string targetType)
    {
        PowerupTileData powerupTileData = m_tiles[tileIndices.Item1, tileIndices.Item2];
        if (powerupTileData.type == sourceType)
        {
            Vector3 currentTilePosition = powerupTileData.tile.GetComponent<RectTransform>().localPosition;
            powerupTileData.tile.transform.SetParent(m_boardOriginalParent);
            ObjectPooler.Instance.ReturnToPool(powerupTileData.tile);
            m_tiles[tileIndices.Item1, tileIndices.Item2].type = targetType;
            GameObject tileGO = ObjectPooler.Instance.GetPooledObject(targetType);
            tileGO.SetActive(true);
            tileGO.transform.SetParent(m_boardParent.transform);
            tileGO.GetComponent<RectTransform>().localPosition = currentTilePosition;
            if (targetType == m_fullTileType)
            {
                tileGO.transform.DOPunchScale(m_turnFullTilePunchScale, 0.5f, 0, 0);
            }
            m_tiles[tileIndices.Item1, tileIndices.Item2].tile = tileGO;
        }
    }

    /// <summary>
    /// Called every time a tile is hit.
    /// Check if by hiting this tile we've completed hitting a set of tiles on the powerup's board.
    /// </summary>
    private void CheckForSetComplete(List<(int, int)> tilesHit)
    {
        List<(int, int)> setTiles = new List<(int, int)>();
        for (int i = 0; i < tilesHit.Count; i++)
        {
            if (IsCenterOfSquareSet(tilesHit[i])) setTiles.AddRange(GetCenterOfSquareSet(tilesHit[i]));
            if (IsLeftOfSquareSet(tilesHit[i])) setTiles.AddRange(GetLeftOfSquareSet(tilesHit[i]));
            if (IsLeftBottomCornerOfSquareSet(tilesHit[i])) setTiles.AddRange(GetLeftBottomCornerOfSquareSet(tilesHit[i]));
            if (IsLeftTopCornerOfSquareSet(tilesHit[i])) setTiles.AddRange(GetLeftTopCornerOfSquareSet(tilesHit[i]));
            if (IsTopOfSquareSet(tilesHit[i])) setTiles.AddRange(GetTopOfSquareSet(tilesHit[i]));
            if (IsBottomOfSquareSet(tilesHit[i])) setTiles.AddRange(GetBottomOfSquareSet(tilesHit[i]));
            if (IsRightTopCornerOfSquareSet(tilesHit[i])) setTiles.AddRange(GetRightTopCornerOfSquareSet(tilesHit[i]));
            if (IsRightOfSquareSet(tilesHit[i])) setTiles.AddRange(GetRightOfSquareSet(tilesHit[i]));
            if (IsRightBottomCornerOfSquareSet(tilesHit[i])) setTiles.AddRange(GetRightBottomCornerOfSquareSet(tilesHit[i]));
            if (IsRowLineSet(tilesHit[i])) setTiles.AddRange(GetRowLineSet(tilesHit[i]));
            if (IsColumnLineSet(tilesHit[i])) setTiles.AddRange(GetColumnLineSet(tilesHit[i]));
        }
        setTiles.Distinct().ToList();

        if (setTiles.Count > 0)
        {
            SetTilesComplete(setTiles);
            EventManager.TriggerEvent(EventNames.ON_ALL_POWERUPS_ENABLED);
        }
    }

    /// <summary>
    /// Animate the given tiles to their complet state and once it's complete, change the tiles from full type back to empty type.
    /// </summary>
    private void SetTilesComplete(List<(int, int)> setTiles)
    {
        for (int i = 0; i < setTiles.Count; i++)
        {
            Animator anim = m_tiles[setTiles[i].Item1, setTiles[i].Item2].tile.GetComponent<Animator>();
            m_tiles[setTiles[i].Item1, setTiles[i].Item2].tile.transform.DOKill();
            anim.SetBool("isComplete", true);
            StartCoroutine(OnTileCompleteAnimEnded(anim, setTiles[i]));
        }
    }

    IEnumerator OnTileCompleteAnimEnded(Animator anim, (int, int) tile)
    {
        // Animation clip is currently null, so we wait.
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }

        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

        RawImage rawImage = m_tiles[tile.Item1, tile.Item2].tile.GetComponent<RawImage>();
        Color color = rawImage.color;
        color.a = 1;
        rawImage.color = color;
        ChangeTileType(tile, m_fullTileType, m_emptyTileType);
    }

    /// <summary>
    /// Return true if the given tile is part of a set of only full tiles on its row.
    /// </summary>
    private bool IsRowLineSet((int, int) tile)
    {
        bool isPartOfFullLine = true;
        for (int row = 0; row < Board.Instance.COUNT_ROWS && isPartOfFullLine; row++)
        {
            if (Board.Instance.IsValidTileIndices((row, tile.Item2)))
            {
                if (m_tiles[row, tile.Item2].type == m_emptyTileType)
                {
                    isPartOfFullLine = false;
                }
            }
            else
            {
                isPartOfFullLine = false;
            }
        }
        return isPartOfFullLine;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of only full tiles on its column.
    /// </summary>
    private bool IsColumnLineSet((int, int) tile)
    {
        bool isPartOfFullLine = true;
        for (int col = 0; col < Board.Instance.COUNT_COLUMNS && isPartOfFullLine; col++)
        {
            if (Board.Instance.IsValidTileIndices((tile.Item1, col)))
            {
                if (m_tiles[tile.Item1, col].type == m_emptyTileType)
                {
                    isPartOfFullLine = false;
                }
            }
            else
            {
                isPartOfFullLine = false;
            }
        }
        return isPartOfFullLine;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its middle-left tile.
    /// </summary>
    private bool IsLeftOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = -1; row < 2 && isPartOfFullSquare; row++)
        {
            for (int col = 0; col < 3 && isPartOfFullSquare; col++)
            {

                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its bottom-left tile.
    /// </summary>
    private bool IsLeftBottomCornerOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = 0; row > -3 && isPartOfFullSquare; row--)
        {
            for (int col = 0; col < 3 && isPartOfFullSquare; col++)
            {

                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its top-left tile.
    /// </summary>
    private bool IsLeftTopCornerOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = 0; row < 3 && isPartOfFullSquare; row++)
        {
            for (int col = 0; col < 3 && isPartOfFullSquare; col++)
            {

                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its center tile.
    /// </summary>
    private bool IsCenterOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = -1; row < 2 && isPartOfFullSquare; row++)
        {
            for (int col = -1; col < 2 && isPartOfFullSquare; col++)
            {

                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its middle-top tile.
    /// </summary>
    private bool IsTopOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = 0; row < 3 && isPartOfFullSquare; row++)
        {
            for (int col = -1; col < 2 && isPartOfFullSquare; col++)
            {

                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its middle-bottom tile.
    /// </summary>
    private bool IsBottomOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = 0; row > -3 && isPartOfFullSquare; row--)
        {
            for (int col = -1; col < 2 && isPartOfFullSquare; col++)
            {

                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its top-right tile.
    /// </summary>
    private bool IsRightTopCornerOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = 0; row < 3 && isPartOfFullSquare; row++)
        {
            for (int col = 0; col > -3 && isPartOfFullSquare; col--)
            {
                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its middle-right tile.
    /// </summary>
    private bool IsRightOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = -1; row < 2 && isPartOfFullSquare; row++)
        {
            for (int col = 0; col > -3 && isPartOfFullSquare; col--)
            {
                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return true if the given tile is part of a set of a square of 3X3 only full tiles and the given tile is its bottom-right tile.
    /// </summary>
    private bool IsRightBottomCornerOfSquareSet((int, int) tile)
    {
        bool isPartOfFullSquare = true;
        for (int row = 0; row < 3 && isPartOfFullSquare; row--)
        {
            for (int col = 0; col > -3 && isPartOfFullSquare; col--)
            {
                if (Board.Instance.IsValidTileIndices((tile.Item1 + row, tile.Item2 + col)))
                {
                    if (m_tiles[tile.Item1 + row, tile.Item2 + col].type == m_emptyTileType)
                    {
                        isPartOfFullSquare = false;
                    }
                }
                else
                {
                    isPartOfFullSquare = false;
                }
            }
        }
        return isPartOfFullSquare;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's middle-left tile.
    /// </summary>
    private List<(int, int)> GetLeftOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = -1; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's bottom-left tile.
    /// </summary>
    private List<(int, int)> GetLeftBottomCornerOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = 0; row > -3; row--)
        {
            for (int col = 0; col < 3; col++)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's top-left tile.
    /// </summary>
    private List<(int, int)> GetLeftTopCornerOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's center tile.
    /// </summary>
    private List<(int, int)> GetCenterOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = -1; row < 2; row++)
        {
            for (int col = -1; col < 2; col++)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's top-middle tile.
    /// </summary>
    private List<(int, int)> GetTopOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = 0; row < 3; row++)
        {
            for (int col = -1; col < 2; col++)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's bottom-middle tile.
    /// </summary>
    private List<(int, int)> GetBottomOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = 0; row > -3; row--)
        {
            for (int col = -1; col < 2; col++)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's top-right tile.
    /// </summary>
    private List<(int, int)> GetRightTopCornerOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col > -3 ; col--)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's middle-right tile.
    /// </summary>
    private List<(int, int)> GetRightOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = -1; row < 2; row++)
        {
            for (int col = 0; col > -3; col--)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the 3X3 square that the given tile is it's bottom-right tile.
    /// </summary>
    private List<(int, int)> GetRightBottomCornerOfSquareSet((int, int) tile)
    {
        List<(int, int)> squareSet = new List<(int, int)>();
        for (int row = 0; row > -3; row--)
        {
            for (int col = 0; col > -3; col--)
            {
                squareSet.Add((tile.Item1 + row, tile.Item2 + col));
            }
        }
        return squareSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the row that the given tile is in.
    /// </summary>
    private List<(int, int)> GetRowLineSet((int, int) tile)
    {
        List<(int, int)> lineSet = new List<(int, int)>();
        for (int row = 0; row < Board.Instance.COUNT_ROWS; row++)
        {
            lineSet.Add((row, tile.Item2));

        }
        return lineSet;
    }

    /// <summary>
    /// Return a list of all the tiles in the column that the given tile is in.
    /// </summary>
    private List<(int, int)> GetColumnLineSet((int, int) tile)
    {
        List<(int, int)> lineSet = new List<(int, int)>();
        for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
        {
            lineSet.Add((tile.Item1, col));

        }
        return lineSet;
    }
}
