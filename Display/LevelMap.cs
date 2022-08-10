using UnityEngine;
using DG.Tweening;

public class LevelMap : MonoBehaviour
{
    [SerializeField] private Camera m_mainCamera;
    [SerializeField] private Shader m_additiveShader;

    private string m_levelItemID = "LevelItem";
    private float m_currentDragAmount = 0;
    private float m_startWidth = 0.2f;
    private float m_endWidth = 0.2f;
    private float m_screenHeight;
    private float m_screenWidth;
    private float m_screenEdgeOffset = 2;
    private float m_stepDistance = 2f;
    private float m_minDragThreshold = 0.5f;
    private float m_dragStep;
    private float m_dragDuration;
    private float[] m_startingMapPositions;
    private float m_totalSize;
    private int m_levelItemsCount = 0;
    private bool m_isDragging = false;
    private bool m_isDraggingStarted = false;
    private bool m_isLevelItemsReady = false;
    private Color m_startColor = Color.yellow;
    private Color m_endColor = Color.red;
    private Vector3[] m_allPosition;
    private Vector3 m_startPosition;
    private Vector3 m_startDragPos;
    private Vector3 m_endDragPos;
    private LevelItem[] m_allLevelItems;
    private LineRenderer m_lineRenderer;

    public const int NUM_OF_LEVELS = 50;

    private void Start()
    {
        CreateLineRenderer();
        SetupLineRendererPositions();
        AddListeners();
    }

    private void Update()
    {
        if (!m_isLevelItemsReady && ObjectPooler.Instance.IsPoolReady)
        {
            m_isLevelItemsReady = true;
            CreateLevelItems();
            ScrollToCurrentLevel();
        }

        HandleMouseEvents();
    }

    private void HandleMouseEvents()
    {
        // Start Pressing.
        if (Input.GetMouseButtonDown(0))
        {
            m_startDragPos = m_mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if (!m_isDragging)
            {
                m_isDragging = true;
                m_isDraggingStarted = true;
            }
        }

        // Release press - perform dragging if not already dragging.
        if (Input.GetMouseButtonUp(0))
        {
            if (m_isDraggingStarted)
            {
                m_isDraggingStarted = false;
                m_endDragPos = m_mainCamera.ScreenToWorldPoint(Input.mousePosition);
                if (Mathf.Abs(m_endDragPos.y - m_startDragPos.y) > m_minDragThreshold)
                {
                    PerfromDragging();
                }
                else
                {
                    m_isDragging = false;
                }
            }
        }
    }

    /// <summary>
    /// According to drag direction, calculate the amount to drag and drag.
    /// </summary>
    private void PerfromDragging()
    {
        float targetValue;
        m_currentDragAmount = 0;
        if (m_endDragPos.y > m_startDragPos.y)
        {
            if (m_lineRenderer.GetPosition(0).y < m_startPosition.y)
            {
                targetValue = Mathf.Min(m_dragStep, m_startPosition.y - m_lineRenderer.GetPosition(0).y);
            }
            else
            {
                m_isDragging = false;
                return;
            }
        }
        else
        {
            if (m_lineRenderer.GetPosition(0).y + m_totalSize > m_startPosition.y + m_screenHeight - m_screenEdgeOffset*2)
            {
                targetValue = Mathf.Max(-m_dragStep, m_startPosition.y + m_screenHeight - m_screenEdgeOffset*2 - (m_lineRenderer.GetPosition(0).y + m_totalSize));
            }
            else
            {
                m_isDragging = false;
                return;
            }
        }
        DOTween.To(() => m_currentDragAmount, y => m_currentDragAmount = y, targetValue, m_dragDuration).OnUpdate(OnDrag).OnComplete(OnDragComplete).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// Called every Update of dragging, update the position of the lineRenderer and all the level items.
    /// </summary>
    private void OnDrag()
    {
        for (int i = 0; i < m_levelItemsCount; i++)
        {
            Vector3 currentPos = m_lineRenderer.GetPosition(i);
            currentPos.y = m_startingMapPositions[i] + m_currentDragAmount;
            m_lineRenderer.SetPosition(i, currentPos);
            m_allLevelItems[i].transform.position = currentPos;
        }
    }

    /// <summary>
    /// Called on dragging complete. Update new starting position and enable new dragging.
    /// </summary>
    private void OnDragComplete()
    {
        for (int i = 0; i < m_levelItemsCount; i++)
        {
            m_startingMapPositions[i] = m_lineRenderer.GetPosition(i).y;
        }
        print("LevelMap: Dragging complete");
        m_isDragging = false;
    }

    /// <summary>
    /// Set the current player's max level in screen view.
    /// </summary>
    private void ScrollToCurrentLevel()
    {
        // Count the amount of drag steps needed to set the current player's level in screen view.
        float currentLevelPosition = m_startingMapPositions[PlayerData.Instance.PlayerMaxLevel - 1];
        int amountOfDragSteps = 0;
        while (currentLevelPosition > (m_screenHeight / 2.0f))
        {
            amountOfDragSteps++;
            currentLevelPosition -= m_dragStep;
        }
        m_isDragging = true;
        DOTween.To(() => m_currentDragAmount, y => m_currentDragAmount = y, -amountOfDragSteps * m_dragStep, m_dragDuration).OnUpdate(OnDrag).OnComplete(OnDragComplete).SetEase(Ease.OutCubic);
    }

    private void CreateLineRenderer()
    {
        // Setup the line renderer.
        m_lineRenderer = gameObject.AddComponent<LineRenderer>();
        m_lineRenderer.material = new Material(m_additiveShader);
        m_lineRenderer.startColor = m_startColor;
        m_lineRenderer.endColor = m_endColor;
        m_lineRenderer.startWidth = m_startWidth;
        m_lineRenderer.endWidth = m_endWidth;
    }

    /// <summary>
    /// Setup the line renderer according to the screen size and the number of levels.
    /// </summary>
    private void SetupLineRendererPositions()
    {
        m_allPosition = new Vector3[NUM_OF_LEVELS];
        m_startingMapPositions = new float[NUM_OF_LEVELS];

        m_screenHeight = m_mainCamera.orthographicSize * 2;
        m_screenWidth = m_screenHeight * Screen.width / Screen.height;

        m_dragStep = m_screenHeight * (3 / 4.0f);
        m_dragDuration = m_dragStep / 15.0f;
        m_startPosition = new Vector3(0, -m_mainCamera.orthographicSize + m_screenEdgeOffset);
        AddPosition(m_startPosition);
        m_startingMapPositions[0] = m_startPosition.y;
        for (int level = 0; level < NUM_OF_LEVELS - 1; level++)
        {
            Vector3 nextPosition = CalcNextPosition();
            AddPosition(nextPosition);
            m_startingMapPositions[level + 1] = nextPosition.y;
        }
        m_totalSize = m_lineRenderer.GetPosition(m_levelItemsCount - 1).y - m_startPosition.y;
    }

    /// <summary>
    /// Calculate the next position of the level item to be made.
    /// </summary>
    private Vector3 CalcNextPosition()
    {
        Vector3 res = m_allPosition[m_levelItemsCount - 1];

        float angle = UnityEngine.Random.Range(75, 105);
        var x = m_stepDistance * Mathf.Cos(angle * Mathf.Deg2Rad);
        var y = m_stepDistance * Mathf.Sin(angle * Mathf.Deg2Rad);
        res.x += x;
        res.x = Mathf.Clamp(res.x, -m_screenWidth / 4.0f, m_screenWidth / 4.0f);
        res.y += y;
        return res;
    }
    
    private void AddPosition(Vector3 newPosition)
    {
        m_allPosition[m_levelItemsCount] = newPosition;
        m_levelItemsCount++;
        m_lineRenderer.positionCount = m_levelItemsCount;
        m_lineRenderer.SetPosition(m_levelItemsCount - 1, newPosition);
    }

    /// <summary>
    /// Get the level items from the Pool, position them on the positions calculated for the line renderer and initiate them.
    /// </summary>
    private void CreateLevelItems()
    {
        m_allLevelItems = new LevelItem[NUM_OF_LEVELS];
        for (int level = 0; level < NUM_OF_LEVELS; level++)
        {
            GameObject levelItem = ObjectPooler.Instance.GetPooledObject(m_levelItemID);
            levelItem.SetActive(true);
            levelItem.transform.position = m_allPosition[level];

            bool isLevelLocked = (level < PlayerData.Instance.PlayerMaxLevel) ? false : true;
            bool isCurrentLevel = level + 1 == PlayerData.Instance.PlayerMaxLevel;
            m_allLevelItems[level] = levelItem.GetComponent<LevelItem>(); 
            m_allLevelItems[level].Init(level + 1, isLevelLocked, isCurrentLevel);
        }
    }

    public void DisableAllLevels()
    {
        for (int level = 0; level < NUM_OF_LEVELS; level++)
        {
            m_allLevelItems[level].Disable();
        }
    }

    public void EnableAllLevels()
    {
        for (int level = 0; level < NUM_OF_LEVELS; level++)
        {
            m_allLevelItems[level].Enable();
        }
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    private void AddListeners()
    {
        EventManager.StartListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StartListening(EventNames.ON_LEVEL_CLICKED, OnLevelClicked);
        EventManager.StartListening(EventNames.ON_PREVIEW_LEVEL_POPUP_CLOSED, OnPreviewLevelClosed);
    }

    private void RemoveListeners()
    {
        EventManager.StopListening(EventNames.ON_LOAD_SCENE, OnLoadScene);
        EventManager.StopListening(EventNames.ON_LEVEL_CLICKED, OnLevelClicked);
        EventManager.StopListening(EventNames.ON_PREVIEW_LEVEL_POPUP_CLOSED, OnPreviewLevelClosed);
    }

    private void OnPreviewLevelClosed(string eventName, ActionParams _data)
    {
        EnableAllLevels();
    }

    private void OnLevelClicked(string eventName, ActionParams _data)
    {
        DisableAllLevels();
    }
    
    private void OnLoadScene(string eventName, ActionParams _data)
    {
        ReturnAllLevels();
        m_lineRenderer.gameObject.SetActive(false);
    }

    public void ReturnAllLevels()
    {
        for (int level = 0; level < NUM_OF_LEVELS; level++)
        {
            m_allLevelItems[level].OnRemoved();
            ObjectPooler.Instance.ReturnToPool(m_allLevelItems[level].gameObject);
        }
    }
}
