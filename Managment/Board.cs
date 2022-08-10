using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private Camera m_mainCamera;
    [SerializeField] private float m_paddingBetweenTiles = 0.1f;
    [SerializeField] private float m_swapTilesDuration = 0.2f;
    [SerializeField] private float m_dropTilesDuration = 0.85f;
    [SerializeField] private float m_enterBoardDuration = 1.5f;
    [SerializeField] private float m_minDragThreshold = 0.3f;
    [SerializeField] private RuntimeAnimatorController m_defaultMatchAnimation;
    [SerializeField] private GameObject m_boardParent;

    private float m_startingX;
    private float m_startingY;
    private float m_boardHeight;
    private float m_boardWidth;
    private float m_screenWidthInUnits;
    private float m_screenHeightInUnits;
    private float m_matchedTileAnimLength;
    private int m_powerupTilesToSelect = 0;
    private int m_powerupTilesSelectCounter = 0;
    private bool m_isActive = false;
    private bool m_isPowerupActive = false;
    private bool m_isInMatching = false;
    private (int, int)[] m_powerupTilesSelected;
    private (int, int)[] m_blankTiles;
    private (int, int)[] m_blockTiles;
    private (int, int) m_selectedTileIndices;
    private Tile[,] m_tiles;
    private List<(int, int)> m_allTileIndices;
    private static Board _instance;
    private Dictionary<int, Transform> m_tileToOriginalParent;

    public int COUNT_ROWS;
    public int COUNT_COLUMNS;
    public bool IsActive { get { return m_isActive; } set { m_isActive = value; } }
    public Tile[,] Tiles => m_tiles;
    public Camera MainCamera => m_mainCamera;
    public static Board Instance { get { return _instance; }}

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        AddListeners();
    }

    private void OnSceneLoaded(string eventName, ActionParams _data)
    {
        print("Board: OnSceneLoaded");
        m_tileToOriginalParent = new Dictionary<int, Transform>();
        LevelData levelData = _data.Get<LevelData>("levelData");
        InitLevelData(levelData);
        CreateBoard();
        StartCoroutine(EnterBoardTween());
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    private void AddListeners()
    {
        EventManager.StartListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StartListening(EventNames.ON_SCENE_LOADED, OnSceneLoaded);
        EventManager.StartListening(EventNames.ON_DEACTIVATE_POWERUP, OnDeactivatePowerup);
    }

    private void RemoveListeners()
    {
        EventManager.StopListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StopListening(EventNames.ON_SCENE_LOADED, OnSceneLoaded);
        EventManager.StopListening(EventNames.ON_DEACTIVATE_POWERUP, OnDeactivatePowerup);
    }

    private void OnLoadScene(string eventName, ActionParams _data)
    {
        RemoveAllTiles();
    }

    public void OnCheatClicked()
    {
        List<(int, int)> tilesHit = new List<(int, int)>();
        for (int i = 0; i < COUNT_ROWS; i++)
        {
            (int, int) tileIndices = (i, 3);
            if (!Utils.IsTupleEmpty(tileIndices) && TilesUtility.IsTilePowerupEnabled(tileIndices))
            {
                tilesHit.Add(tileIndices);
            }
        }
        
        HandleTilesHit(tilesHit);
    }

    private void InitLevelData(LevelData levelData)
    {
        COUNT_COLUMNS = levelData.numOfColumns;
        COUNT_ROWS = levelData.numOfRows;

        if (levelData.blankTiles.Count > 0)
        {
            int currentIdx = 0;
            m_blankTiles = new (int, int)[levelData.blankTiles.Count / 2];
            for (int i = 0; i < levelData.blankTiles.Count - 1; i += 2)
            {
                m_blankTiles[currentIdx] = (levelData.blankTiles[i], levelData.blankTiles[i + 1]);
                currentIdx++;
            }
        }

        if (levelData.blockTiles.Count > 0)
        {
            int currentIdx = 0;
            m_blockTiles = new (int, int)[levelData.blockTiles.Count / 2];
            for (int i = 0; i < levelData.blockTiles.Count - 1; i += 2)
            {
                m_blockTiles[currentIdx] = (levelData.blockTiles[i], levelData.blockTiles[i + 1]);
                currentIdx++;
            }
        }

        TilesUtility.SetTileTypesAvailable(levelData.tileTypesAvailable);  
    }

    private void Update()
    {
        if (!m_isInMatching && m_isActive)
            HandleMouseEvents();
    }

    private void HandleMouseEvents()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckSelectedTile();
        }

        // If we're releasing after selecting a tile:
        if (Input.GetMouseButtonUp(0) && !Utils.IsTupleEmpty(m_selectedTileIndices))
        {
            if (m_isPowerupActive)
            {
                if (m_powerupTilesToSelect > 0)
                {
                    if (!m_powerupTilesSelected.Contains(m_selectedTileIndices))
                    {
                        Tile selectedTile = m_tiles[m_selectedTileIndices.Item1, m_selectedTileIndices.Item2];
                        Animator anim = selectedTile.GetComponent<Animator>();

                        anim.SetBool("isSelect", true);
                        m_powerupTilesSelected[m_powerupTilesSelectCounter] = m_selectedTileIndices;
                        m_powerupTilesSelectCounter++;
                        m_powerupTilesToSelect--;
                        if (m_powerupTilesToSelect == 0)
                        {
                            PowerupManager.Instance.OnPowerupTilesSelected(m_powerupTilesSelected);
                        }
                    }
                }
            }
            else
            {
                HandleSwap();
            }
        }
    }

    /// <summary>
    /// Check if we've clicked and selected a tile.
    /// </summary>
    private void CheckSelectedTile()
    {
        // Casts the ray and get the first gameObject hit.
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Tile"))
            {
                (int, int) tileIndices = GetTileIndices(hit.transform.gameObject);
                if (TilesUtility.IsTilePowerupEnabled(tileIndices))
                {
                    m_selectedTileIndices = tileIndices;
                }
            }
        }
    }

    /// <summary>
    /// Check for a valid swap and execute it.
    /// </summary>
    private void HandleSwap()
    {
        //print("Board: HandleSwap");
        Vector3 startPosition = m_tiles[m_selectedTileIndices.Item1, m_selectedTileIndices.Item2].transform.position;
        Vector3 endPosition = m_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1));

        if (Vector2.Distance(startPosition, endPosition) < m_minDragThreshold)
        {
            m_selectedTileIndices = Utils.GetTupleResetValues();
            return;
        }

        (int, int) direction = TilesUtility.GetDirection(startPosition, endPosition);
        (int, int) endTileIndices = (m_selectedTileIndices.Item1 + direction.Item1, m_selectedTileIndices.Item2 + direction.Item2);

        // If we've got a tile to swap with.
        if (IsValidTileIndices(endTileIndices) && TilesUtility.IsTilesMatchEnabled(endTileIndices, m_selectedTileIndices))
        {
            // Swap the tiles and once the swapping animation completed - check for matches and handle if any exist.
            SwapTiles(m_selectedTileIndices, endTileIndices, m_swapTilesDuration);
            List<(int, int)> tileIndices = new List<(int, int)>();
            tileIndices.Add(endTileIndices);
            tileIndices.Add(m_selectedTileIndices);
            StartCoroutine(CheckMatchesPostAnimation(tileIndices, m_swapTilesDuration));
            EventManager.TriggerEvent(EventNames.ON_SWAP_TILES);
        }
        else
        {
            m_selectedTileIndices = Utils.GetTupleResetValues();
        }
    }

    /// <summary>
    /// Coroutine to check for matches on the given tileIndices and handles the tiles that are part of the match.
    /// </summary>
    IEnumerator CheckMatchesPostAnimation(List<(int, int)> tileIndices, float waitDuration)
    {
        yield return new WaitForSeconds(waitDuration);

        // Find all the tiles from the given tileIndices that are a part of a match.
        List<(int, int)> matchedTiles = new List<(int, int)>();
        for (int i = 0; i < tileIndices.Count; i++)
        {
            if (TilesUtility.IsTileMatchEnabled(tileIndices[i]) && MatchesUtility.IsPartOfMatch(tileIndices[i].Item1, tileIndices[i].Item2, m_tiles[tileIndices[i].Item1, tileIndices[i].Item2].TileType))
            {
                matchedTiles.AddRange(MatchesUtility.GetMatchIndices(tileIndices[i].Item1, tileIndices[i].Item2, m_tiles[tileIndices[i].Item1, tileIndices[i].Item2].TileType));
            }
        }
        matchedTiles = matchedTiles.Distinct().ToList();

        // If we've got a match - handle the tiles in it.
        if (matchedTiles.Count > 0)
        {
            HandleTilesHit(matchedTiles);
        }
        else
        {
            EventManager.TriggerEvent(EventNames.ON_BOARD_MOVE_ENDED);
            m_isInMatching = false;
        }

        m_selectedTileIndices = Utils.GetTupleResetValues();
    }


    public void HandleTilesHit(List<(int, int)> tilesHit)
    {
        if (tilesHit == null || tilesHit.Count == 0)
            return;

        m_isInMatching = true;
        HandleTilesComplete(tilesHit);
    }

    /// <summary>
    /// Given tiles that are part of a match and now complete, we iterate them by cloumn. 
    /// Per column we remove the tiles complete, move down the tiles above them and create new tiles to fill the gap the tiles hit left.
    /// </summary>
    /// <param name="tilesComplete"> Tiles that are hit and complete as part of a match or from a special powerup. </param>
    private void HandleTilesComplete(List<(int, int)> tilesComplete)
    {
        DispatchTilesHit(tilesComplete);

        tilesComplete.Sort(TilesUtility.SortLowestColumnHighestRow);
        int currentColumn = tilesComplete[0].Item2;
        int startColumnIdx = 0;
        int endColumnIdx = 1;

        for (int idx = 0; idx < tilesComplete.Count; idx++)
        {
            if (currentColumn != tilesComplete[idx].Item2)
            {
                endColumnIdx = idx - 1;
                HandleTilesHitInColumn(tilesComplete.GetRange(startColumnIdx, (endColumnIdx - startColumnIdx + 1)));
                currentColumn = tilesComplete[idx].Item2;
                startColumnIdx = idx;
            }

            if (idx == (tilesComplete.Count - 1))
            {
                endColumnIdx = idx;
                HandleTilesHitInColumn(tilesComplete.GetRange(startColumnIdx, (endColumnIdx - startColumnIdx + 1)));
            }
        }

        // After handling the tiles complete and adding new tiles to fill the gap they left, check for any new matches on the board.
        if (m_isPowerupActive)
        {
            PowerupManager.Instance.DeactivatePowerup();
        }
        StartCoroutine(CheckMatchesPostAnimation(m_allTileIndices, m_dropTilesDuration + m_matchedTileAnimLength + 0.5f));
    }

    private void DispatchTilesHit(List<(int, int)> tilesHit)
    {
        string[] tileTypes = new string[tilesHit.Count];
        Vector3[] tilePositions = new Vector3[tilesHit.Count];
        Color[] tileColors = new Color[tilesHit.Count];
        for (int i = 0; i < tilesHit.Count; i++)
        {
            Tile tile = m_tiles[tilesHit[i].Item1, tilesHit[i].Item2];
            tileTypes[i] = tile.TileType;
            tilePositions[i] = tile.transform.position;
            tileColors[i] = tile.TileColor;
        }
        ActionParams data = new ActionParams();
        data.Put("tileColors", tileColors);
        data.Put("tileTypes", tileTypes);
        data.Put("tilePositions", tilePositions);
        data.Put("tilesHit", tilesHit);
        data.Put("isPowerupActive", m_isPowerupActive);
        EventManager.TriggerEvent(EventNames.ON_TILES_HIT, data);
    }

    /// <summary>
    /// Given the tiles hit in a column, set their hit animation (match or the current powerup's animation).
    /// </summary>
    private void HandleTilesHitInColumn(List<(int, int)> matchedIndicesInColumn)
    {
        // Iterate from the matched tiles till row 0, remove the matched tiles and move down the tiles above them.
        for(int i = 0; i < matchedIndicesInColumn.Count; i++)
        {
            Tile currentTile = m_tiles[matchedIndicesInColumn[i].Item1, matchedIndicesInColumn[i].Item2];
            Animator anim = currentTile.GetComponent<Animator>();

            // Set the current special effect's animation on the affected tiles.
            if (m_isPowerupActive && PowerupManager.Instance.UsePowerupMatchAnimation())
            {
                anim.runtimeAnimatorController = PowerupManager.Instance.GetCurrentPowerupAnimController();
            }
            else
            {
                // Set "Match" animation.
                anim.SetBool("isMatch", true);
            }

            StartCoroutine(OnTilesHitInColumnAnimEnded(i==0, anim, currentTile, matchedIndicesInColumn));
        }
    }

    /// <summary>
    /// Once the hit animation of the tiles hit in a column is complete, remove the tiles and drop exisiting and new tiles down.
    /// </summary>
    IEnumerator OnTilesHitInColumnAnimEnded(bool isFirstTile, Animator anim, Tile tile, List<(int, int)> matchedIndicesInColumn)
    {
        // Animation clip is currently null, so we wait.
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }

        m_matchedTileAnimLength = anim.GetCurrentAnimatorClipInfo(0)[0].clip.length;
        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

        // Reset the animated matched tile and return it to the pool.
        Vector3 smallestTileOnColumnPos = m_tiles[matchedIndicesInColumn[0].Item1, matchedIndicesInColumn[0].Item2].transform.position;
        RemoveTile(tile, anim);

        // Assuming all tile match animation duration's the same.
        // Only wait for the first tile's match animation to end before droping in the remaining and new tiles in the column. 
        if (isFirstTile)
            DropDownTilesInColumn(smallestTileOnColumnPos, matchedIndicesInColumn);
    }

    /// <summary>
    /// Move down all the tiles above the removed tiles and create new tiles to fill the gap left.
    /// </summary>
    private void DropDownTilesInColumn(Vector3 smallestTileOnColumnPos, List<(int, int)> matchedIndicesInColumn)
    {
        float offsetY = m_startingY + TilesUtility.GetTileHeight() + m_paddingBetweenTiles;
        int currentColumn = matchedIndicesInColumn[0].Item2;
        int totalMatchedInColumn = matchedIndicesInColumn.Count;
        int matchedInGroup = 0;
        int targetRowIdx = 0;

        // Create a list of the rows in the current column that need to have tiles in them and populate these rows from bottom to top.
        List<int> targetRowIndices = new List<int>();
        for (int row = matchedIndicesInColumn[0].Item1; row >= 0; row--)
        {
            Tile currentTile = m_tiles[row, currentColumn];
            if (currentTile.IsMovable)
            {
                targetRowIndices.Add(row);
            }
        }

        // Drop down exiting non-matched movable tiles in this column.
        for (int row = matchedIndicesInColumn[0].Item1; row >= 0; row--)
        {
            // Skip the matched tiles.
            if (matchedInGroup < totalMatchedInColumn && row == matchedIndicesInColumn[matchedInGroup].Item1)
                matchedInGroup++;
            else
            {
                Tile currentTile = m_tiles[row, currentColumn];
                if (currentTile.IsMovable)
                {
                    int currentTargetRow = targetRowIndices[targetRowIdx];
                    m_tiles[currentTargetRow, currentColumn] = currentTile;
                    Vector3 targetTilePos = currentTile.transform.position + new Vector3(0, -((TilesUtility.GetTileHeight() + m_paddingBetweenTiles) * (currentTargetRow - row)));
                    currentTile.transform.DOMove(targetTilePos, m_dropTilesDuration).SetEase(Ease.OutBounce);
                    targetRowIdx++;
                }
            }
        }

        // Create new tiles to fill the gap left by the matched rows and slide them into the board from the top of the board.
        for (int i = 0; i < totalMatchedInColumn; i++)
        {
            int currentTargetRow = targetRowIndices[targetRowIdx];
            int tileType = int.Parse(TilesUtility.GetRandomTileType(false));
            Vector3 position = new Vector3(smallestTileOnColumnPos.x, offsetY + ((TilesUtility.GetTileHeight() + m_paddingBetweenTiles) * i));
            Vector3 targetTilePos = new Vector3(smallestTileOnColumnPos.x, offsetY - ((TilesUtility.GetTileHeight() + m_paddingBetweenTiles) * (currentTargetRow + 1)));
            (int, int) targetIndices = GetTileAtPosition(targetTilePos);
            GameObject tileGO = CreateTile(tileType, position, targetIndices);
            tileGO.transform.DOMove(targetTilePos, m_dropTilesDuration).SetEase(Ease.OutBounce);
            targetRowIdx++;
        }
    }

    /// <summary>
    /// Swap the Tiles at the given source and target indices in the tiles 2D array.
    /// </summary>
    /// <param name="sourceIndices"> Tuple of (row, column) in the tiles 2D array of the tile swapping from. </param>
    /// <param name="targetIndices"> Tuple of (row, column) in the tiles 2D array of the tile swapping with. </param>
    public void SwapTiles((int, int) sourceIndices, (int, int) targetIndices, float duration)
    {
        //print("SwapTiles: (" + sourceIndices.Item1 + ", " + sourceIndices.Item2 + ") With (" + targetIndices.Item1 + ", " + targetIndices.Item2 + ")");
        Tile sourceTile = m_tiles[sourceIndices.Item1, sourceIndices.Item2];
        Tile targetTile = m_tiles[targetIndices.Item1, targetIndices.Item2];
        Vector3 targetTileInitialPos = targetTile.transform.position;
        m_tiles[sourceIndices.Item1, sourceIndices.Item2] = targetTile;
        m_tiles[targetIndices.Item1, targetIndices.Item2] = sourceTile;
        targetTile.transform.DOMove(sourceTile.transform.position, duration);
        sourceTile.transform.DOMove(targetTileInitialPos, duration);
    }

    /// <summary>
    /// Create a random board of tiles in the size of COUNT_ROWS * COUNT_COLUMNS at the center of the screen. Save the tiles created in the tiles 2D array.
    /// </summary>
    private void CreateBoard()
    {
        m_tiles = new Tile[COUNT_ROWS, COUNT_COLUMNS];
        m_allTileIndices = new List<(int, int)>();
        m_selectedTileIndices = Utils.GetTupleResetValues();

        // Assuming the camera's position is at the origin.
        m_screenHeightInUnits = m_mainCamera.orthographicSize * 2;
        m_screenWidthInUnits = m_screenHeightInUnits * Screen.width / Screen.height;

        m_boardWidth = COUNT_COLUMNS * TilesUtility.GetTileWidth() + (COUNT_COLUMNS - 1) * m_paddingBetweenTiles;
        m_boardHeight = COUNT_ROWS * TilesUtility.GetTileHeight() + (COUNT_ROWS - 1) * m_paddingBetweenTiles;

        // Start creating the board from the top left corner.
        m_startingX = -m_screenWidthInUnits / 2.0f + ((m_screenWidthInUnits - m_boardWidth) / 2.0f) + TilesUtility.GetTileWidth() / 2;
        m_startingY = m_screenHeightInUnits / 2.0f - ((m_screenHeightInUnits - m_boardHeight) / 2.0f) - TilesUtility.GetTileHeight() / 2;

        for (int row = 0; row < COUNT_ROWS; row++)
        {
            for (int col = 0; col < COUNT_COLUMNS; col++)
            {
                // Validate we dont have any matches yet.
                Vector3 targetPosition = new Vector3(m_startingX + col * (TilesUtility.GetTileWidth() + m_paddingBetweenTiles), m_startingY - row * (TilesUtility.GetTileHeight() + m_paddingBetweenTiles), 1);
                int tileType;
                string fullTileType;
                if (m_blockTiles != null && Array.Exists(m_blockTiles, element => element == (row, col)))
                {
                    fullTileType = TilesUtility.BLOCK_TILE_TYPE;
                }
                else if (m_blankTiles != null && Array.Exists(m_blankTiles, element => element == (row, col)))
                {
                    fullTileType = TilesUtility.BLANK_TILE_TYPE;
                }
                else
                {
                    tileType = int.Parse(TilesUtility.GetRandomTileType(false));
                    while (MatchesUtility.IsPartOfMatch(row, col, TilesUtility.TILE_TYPE_PREFIX + tileType))
                    {
                        tileType = int.Parse(TilesUtility.GetRandomTileType(false));
                    }
                    fullTileType = TilesUtility.TILE_TYPE_PREFIX + tileType;
                }

                
                (int, int) tileIndices = (row, col);
                CreateTile(fullTileType, targetPosition, tileIndices);
                m_allTileIndices.Add(tileIndices);
            }
        }
    }

    public GameObject CreateTile(int tileType, Vector3 position, (int, int) tileIndices)
    {
        return CreateTile(TilesUtility.TILE_TYPE_PREFIX + tileType, position, tileIndices);
    }

    public GameObject CreateTile(string fullTileType, Vector3 position, (int, int) tileIndices)
    {
        GameObject tileGO = CreateTile(fullTileType);
        tileGO.transform.position = position;
        Tile tile = tileGO.GetComponent<Tile>();
        m_tiles[tileIndices.Item1, tileIndices.Item2] = tile;
        return tileGO;
    }

    public GameObject CreateTile(string fullTileType)
    {
        GameObject tileGO = ObjectPooler.Instance.GetPooledObject(fullTileType);
        int tileInstanceID = tileGO.GetComponent<Tile>().GetInstanceID();
        if (m_tileToOriginalParent.ContainsKey(tileInstanceID))
        {
            m_tileToOriginalParent[tileInstanceID] = tileGO.transform.parent;
        }
        else
        {
            m_tileToOriginalParent.Add(tileInstanceID, tileGO.transform.parent);
        }
        tileGO.transform.SetParent(m_boardParent.transform);
        tileGO.transform.localScale = Vector3.one;
        tileGO.SetActive(true);
        return tileGO;
    }

    public void RemoveAllTiles()
    {
        for (int row = 0; row < COUNT_ROWS; row++)
        {
            for (int col = 0; col < COUNT_COLUMNS; col++)
            {
                Tile tile = m_tiles[row, col];
                RemoveTile(tile, tile.GetComponent<Animator>());
            }
        }
    }

    public void RemoveTiles(List<(int, int)> tileIndices)
    {
        for (int i = 0; i < tileIndices.Count; i++)
        {
            Tile tile = m_tiles[tileIndices[i].Item1, tileIndices[i].Item2];
            RemoveTile(tile, tile.GetComponent<Animator>());
        }
    }

    /// <summary>
    /// Reset and return the given tile back to the pool.
    /// </summary>
    public void RemoveTile(Tile tile, Animator anim = null)
    {
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        Color color = sr.color;
        color.a = 1;
        sr.color = color;

        if (anim != null)
        {
            anim.runtimeAnimatorController = m_defaultMatchAnimation;
        }

        Transform tileOriginalParent;
        int tileInstanceID = tile.GetInstanceID();
        m_tileToOriginalParent.TryGetValue(tileInstanceID, out tileOriginalParent);
        tile.transform.SetParent(tileOriginalParent);

        ObjectPooler.Instance.ReturnToPool(tile.gameObject);
    }

    /// <summary>
    /// Tweening the board into view on start.
    /// </summary>
    IEnumerator EnterBoardTween()
    {
        Vector3 startPos = new Vector3();
        for (int row = 0; row < COUNT_ROWS; row++)
        {
            for (int col = 0; col < COUNT_COLUMNS; col++)
            {
                float targetX = m_tiles[row, col].transform.position.x;
                startPos = m_tiles[row, col].transform.position;
                startPos.x -= m_screenWidthInUnits / 2 + m_boardWidth;
                m_tiles[row, col].transform.position = startPos;
                m_tiles[row, col].transform.DOMoveX(targetX, m_enterBoardDuration).SetEase(Ease.OutCubic);
            }
        }

        yield return new WaitForSeconds(m_enterBoardDuration);
        m_isActive = true;
    }

    /// <summary>
    /// Result of the powerup completing its actions.
    /// </summary>
    private void OnDeactivatePowerup(string eventName, ActionParams _data)
    {
        print("Board: OnDeactivatePowerup");
        m_isPowerupActive = false;
        if (m_powerupTilesSelected != null)
        {
            for (int i = 0; i < m_powerupTilesSelected.Length; i++)
            {
                if (!Utils.IsTupleEmpty(m_powerupTilesSelected[i]))
                {
                    Tile selectedTile = m_tiles[m_powerupTilesSelected[i].Item1, m_powerupTilesSelected[i].Item2];
                    Animator anim = selectedTile.GetComponent<Animator>();

                    if (anim.GetBool("isSelect"))
                    {
                        anim.SetBool("isSelect", false);
                    }
                }
            }
        }
        m_powerupTilesSelected = null;
    }

    /// <summary>
    /// Powerup is activated, update fields to handle it.
    /// </summary>
    /// <param name="totalSelectedTiles"> The amount of tiles that need to be selected in the board to perform the powerup. </param>
    public void SetPowerupActive(int totalSelectedTiles)
    {
        //print("Board: SetPowerupActive");
        m_powerupTilesToSelect = totalSelectedTiles;
        m_powerupTilesSelectCounter = 0;
        m_isPowerupActive = true;
        m_powerupTilesSelected = new (int, int)[totalSelectedTiles];
        for (int i = 0; i < totalSelectedTiles; i++)
        {
            m_powerupTilesSelected[i] = Utils.GetTupleResetValues();
        }
    }

    /// <summary>
    /// Board was updated externally, check for matches.
    /// </summary>
    public void OnBoardUpdated(float delayTime = 0.5f)
    {
        StartCoroutine(CheckMatchesPostAnimation(m_allTileIndices, delayTime));
    }

    /// <summary>
    /// Return if the give (row, column) Tuple indices in the tiles 2D array are valid indices.
    /// </summary>
    public bool IsValidTileIndices((int, int) tileIndices)
    {
        return tileIndices.Item1 >= 0 && tileIndices.Item1 < COUNT_ROWS && tileIndices.Item2 >= 0 && tileIndices.Item2 < COUNT_COLUMNS;
    }

    /// <summary>
    /// Return the position of the tile at the given indices.
    /// </summary>
    public Vector3 GetTilePosition((int, int) tileIndices)
    {
        return m_tiles[tileIndices.Item1, tileIndices.Item2].transform.position;
    }

    /// <summary>
    /// Return the Given's Tile [row, col] indices in the tiles 2D array.
    /// </summary>
    public (int, int) GetTileIndices(GameObject tile)
    {
        Vector3 tilePosition = tile.transform.position;
        return GetTileAtPosition(tilePosition);
    }

    public (int, int) GetTileAtPosition(Vector3 position)
    {
        int row = Mathf.RoundToInt((m_startingY - position.y) / (TilesUtility.GetTileHeight() + m_paddingBetweenTiles));
        int col = Mathf.RoundToInt((position.x - m_startingX) / (TilesUtility.GetTileWidth() + m_paddingBetweenTiles));
        if (IsValidTileIndices((row, col)))
            return (row, col);

        return Utils.GetTupleResetValues();
    }

    public bool IsBoardAvailable()
    {
        return !m_isInMatching && m_isActive;
    }

    public bool IsBlankTile((int, int) tileIndices)
    {
        return m_blankTiles.Contains(tileIndices);
    }
}
