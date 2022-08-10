using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private float m_enterBarsUIDuration = 1.5f;
    [SerializeField] private float m_particlesDuration = 1.7f;
    [SerializeField] private Camera m_mainCamera;
    [SerializeField] private TMP_Text m_swapCountDownTxt;
    [SerializeField] private GameObject m_topBarUI;
    [SerializeField] private GameObject m_bottomBarUI;
    [SerializeField] private GameObject m_targetItemParent;
    [SerializeField] private Callout m_callout;
    [SerializeField] private Image[] m_availablePowerupRepresentation;
    [SerializeField] private int[] m_calloutCategoryAmounts;

    private float m_screenHeightInUnits;
    private float m_screenWidthInUnits;
    private string m_btnPrefix = "Button";
    private int m_swapTilesCounter;
    private int m_tilesHitCounter;
    private int m_totalTilesToHit;
    private int m_playerCurrentLevel;
    private int m_tilesHitPerMoveCounter;
    private bool m_isPowerupActive;
    private TMP_Text[] m_targetItemsTxt;
    private Animator[] m_targetItemsAnims;
    private List<int> m_targetAmountPerItem;
    private List<string> m_targetTypesPerItem;
    private Vector3 m_topLeftCorner;

    private void Awake()
    {
        AddListeners();
    }

    private void OnSceneLoaded(string eventName, ActionParams _data)
    {
        print("HUD: OnSceneLoaded");

        LevelData levelData = _data.Get<LevelData>("levelData");
        m_playerCurrentLevel = _data.Get<int>("playerCurrentLevel");
        InitData(levelData);
        InitPowerupsAvailable();
        InitTargetItems();
        EnterBarsUITween();
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    private void AddListeners()
    {
        EventManager.StartListening(EventNames.ON_SWAP_TILES, OnSwapTiles);
        EventManager.StartListening(EventNames.ON_BOARD_MOVE_ENDED, OnBoardMoveEnded);
        EventManager.StartListening(EventNames.ON_TILES_HIT, OnTilesHit);
        EventManager.StartListening(EventNames.ON_ALL_POWERUPS_ENABLED, OnAllPowerupsEnabled);
        EventManager.StartListening(EventNames.ON_SCENE_LOADED, OnSceneLoaded);
        EventManager.StartListening(EventNames.ON_DEACTIVATE_POWERUP, OnDeactivatePowerup);

    }

    private void RemoveListeners()
    {
        EventManager.StopListening(EventNames.ON_SWAP_TILES, OnSwapTiles);
        EventManager.StopListening(EventNames.ON_BOARD_MOVE_ENDED, OnBoardMoveEnded);
        EventManager.StopListening(EventNames.ON_TILES_HIT, OnTilesHit);
        EventManager.StopListening(EventNames.ON_ALL_POWERUPS_ENABLED, OnAllPowerupsEnabled);
        EventManager.StopListening(EventNames.ON_SCENE_LOADED, OnSceneLoaded);
        EventManager.StopListening(EventNames.ON_DEACTIVATE_POWERUP, OnDeactivatePowerup);
    }

    /// <summary>
    /// Init level-specific fields, UI and it's related fields.
    /// </summary>
    private void InitData(LevelData levelData)
    {
        m_tilesHitPerMoveCounter = 0;
        m_screenHeightInUnits = m_mainCamera.orthographicSize * 2;
        m_screenWidthInUnits = m_screenHeightInUnits * Screen.width / Screen.height;
        m_topLeftCorner = new Vector3(-m_screenWidthInUnits/2.0f, m_screenHeightInUnits / 2.0f);
        m_swapTilesCounter = levelData.totalSwaps;
        m_targetAmountPerItem = levelData.targetScores;
        m_targetTypesPerItem = levelData.targetTypes;
        m_swapCountDownTxt.text = m_swapTilesCounter.ToString();
        m_tilesHitCounter = 0;
        m_isPowerupActive = false;
    }

    /// <summary>
    /// Set the position and score needed for every targetItem.
    /// </summary>
    private void InitTargetItems()
    {
        m_totalTilesToHit = 0;
        m_targetItemsTxt = new TMP_Text[m_targetAmountPerItem.Count];
        m_targetItemsAnims = new Animator[m_targetAmountPerItem.Count];
        for (int i = 0; i < m_targetAmountPerItem.Count; i++)
        {
            GameObject tileGO = ObjectPooler.Instance.GetPooledObject(m_targetTypesPerItem[i]);
            Transform targetItem = m_targetItemParent.transform.GetChild(i);

            targetItem.GetComponentInChildren<RawImage>().texture = Utils.ConvertSpriteToTexture(tileGO.GetComponent<SpriteRenderer>().sprite);
            targetItem.GetComponentInChildren<RawImage>().color = tileGO.GetComponent<SpriteRenderer>().color;

            m_targetItemsAnims[i] = targetItem.GetComponentInChildren<Animator>();
            m_targetItemsTxt[i] = targetItem.GetComponentInChildren<TMP_Text>();
            m_targetItemsTxt[i].text = m_targetAmountPerItem[i].ToString();
            m_totalTilesToHit += m_targetAmountPerItem[i];
        }

        for (int i = m_targetAmountPerItem.Count; i < m_targetItemParent.transform.childCount; i++)
        {
            m_targetItemParent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Set the available powerup items of the current level on the bottom bar with the powerup usage bar.
    /// </summary>
    public void InitPowerupsAvailable()
    {
        Sprite[] powerupSprites = PowerupManager.Instance.GetAvailablePowerupsSpriteUI();
        for (int i = 0; i < powerupSprites.Length; i++)
        {
            m_availablePowerupRepresentation[i].transform.parent.gameObject.SetActive(true);
            m_availablePowerupRepresentation[i].sprite = powerupSprites[i];
        }
        if (m_availablePowerupRepresentation.Length > powerupSprites.Length)
        {
            for (int i = powerupSprites.Length; i < m_availablePowerupRepresentation.Length; i++)
            {
                m_availablePowerupRepresentation[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Tween in the top and bottom bars into view.
    /// </summary>
    private void EnterBarsUITween()
    {
        float targetY = m_topBarUI.transform.position.y;
        float height = m_topBarUI.GetComponent<RectTransform>().rect.height;
        Vector3 startPos = m_topBarUI.transform.position;
        startPos.y += height;

        m_topBarUI.transform.position = startPos;
        m_topBarUI.transform.DOMoveY(targetY, m_enterBarsUIDuration).SetEase(Ease.OutCubic);

        targetY = m_bottomBarUI.transform.position.y;
        height = m_bottomBarUI.GetComponent<RectTransform>().rect.height;
        startPos = m_bottomBarUI.transform.position;
        startPos.y -= height;

        m_bottomBarUI.transform.position = startPos;
        m_bottomBarUI.transform.DOMoveY(targetY, m_enterBarsUIDuration).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// Callback on tiles swap, update counter.
    /// </summary>
    private void OnSwapTiles(string eventName, ActionParams _data)
    {
        //print("OnSwapTiles m_swapTilesCounter = " + m_swapTilesCounter);
        m_swapTilesCounter--;
        m_swapCountDownTxt.text = m_swapTilesCounter.ToString();
    }

    private void OnBoardMoveEnded(string eventName, ActionParams _data)
    {
        m_tilesHitPerMoveCounter = 0;

        // Level success - show success popup.
        bool isCompletedAllTargets = true;
        for (int i = 0; i < m_targetAmountPerItem.Count; i++)
        {
            if (m_targetAmountPerItem[i] > 0)
            {
                isCompletedAllTargets = false;
                break;
            }
        }
        if (isCompletedAllTargets)
        {
            int levelScore = ScoreCalculator.Calc(m_swapTilesCounter, m_tilesHitCounter, m_totalTilesToHit);
            ActionParams data = new ActionParams();
            data.Put("movesLeft", m_swapTilesCounter);
            data.Put("tilesHitCounter", m_tilesHitCounter);
            data.Put("totalScore", levelScore);
            data.Put("playerCurrentLevel", m_playerCurrentLevel);
            EventManager.TriggerEvent(EventNames.SHOW_LEVEL_SUCCESS_POPUP, data);
        }
        // Level failed - show failed popup.
        else if (m_swapTilesCounter == 0)
        {
            EventManager.TriggerEvent(EventNames.SHOW_LEVEL_FAILED_POPUP);
        }
    }

    /// <summary>
    /// Callback on tiles hit, update the score slider and scoreItems.
    /// </summary>
    private void OnTilesHit(string eventName, ActionParams _data)
    {
        int tilesHit = _data.Get<List<(int, int)>>("tilesHit").Count;
        m_tilesHitCounter += tilesHit;
        HandleCallouts(tilesHit);
        string[] tileTypes = _data.Get<string[]>("tileTypes");
        Vector3[] tilePositions = _data.Get<Vector3[]>("tilePositions");
        Color[] tileColors = _data.Get<Color[]>("tileColors");
        UpdateTargetItems(tileTypes, tilePositions, tileColors);
    }

    private void HandleCallouts(int tilesHit)
    {
        m_tilesHitPerMoveCounter += tilesHit;
        print("HandleCallouts: m_tilesHitPerMoveCounter = " + m_tilesHitPerMoveCounter + " tilesHit = " + tilesHit);
        if (m_tilesHitPerMoveCounter > m_calloutCategoryAmounts[0] && (m_tilesHitPerMoveCounter - tilesHit) <= m_calloutCategoryAmounts[0])
        {
            m_callout.SetText("GREAT!");
            m_callout.gameObject.SetActive(true);
            StartCoroutine(OnCalloutActivated(m_callout.GetComponent<Animator>()));
        }

        else if (m_tilesHitPerMoveCounter > m_calloutCategoryAmounts[1] && (m_tilesHitPerMoveCounter - tilesHit) <= m_calloutCategoryAmounts[1])
        {
            m_callout.SetText("AWESOME!");
            m_callout.gameObject.SetActive(true);
            StartCoroutine(OnCalloutActivated(m_callout.GetComponent<Animator>()));
        }

        else if (m_tilesHitPerMoveCounter > m_calloutCategoryAmounts[2] && (m_tilesHitPerMoveCounter - tilesHit) <= m_calloutCategoryAmounts[2])
        {
            m_callout.SetText("PERFECT!");
            m_callout.gameObject.SetActive(true);
            StartCoroutine(OnCalloutActivated(m_callout.GetComponent<Animator>()));
        }
    }

    private IEnumerator OnCalloutActivated(Animator anim)
    {
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }
        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

        m_callout.gameObject.SetActive(false);
    }

    private void UpdateTargetItems(string[] tileTypes, Vector3[] tilePositions, Color[] tileColors)
    {
        for (int i = 0; i < tileTypes.Length; i++)
        {
            if (m_targetTypesPerItem.Contains(tileTypes[i]))
            {
                int itemIdx = m_targetTypesPerItem.IndexOf(tileTypes[i]);
                if (m_targetAmountPerItem[itemIdx] > 0)
                {
                    m_targetItemsAnims[itemIdx].SetTrigger("isHighlight");

                    // Show particles in the color of the tile hit and move it from the tile's hit position to the top lofet corner of the screen.
                    GameObject particleGO = ObjectPooler.Instance.GetPooledObject("MatchTargetParticle");
                    particleGO.SetActive(true);
                    ParticleSystem particleSystem = particleGO.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule settings = particleSystem.main;
                    settings.startColor = new ParticleSystem.MinMaxGradient(tileColors[i]);
                    particleSystem.Play();
                    particleGO.transform.position = tilePositions[i];

                    particleGO.transform.DOMove(m_topLeftCorner, m_particlesDuration).SetEase(Ease.InOutSine).OnComplete(() => { ObjectPooler.Instance.ReturnToPool(particleGO); }); ;
                    float value = 1;
                    DOTween.To(() => value, x => value = x, 0, m_particlesDuration - 0.1f).SetEase(Ease.InSine).OnUpdate(() =>
                    {
                        Color startColor = settings.startColor.color;
                        startColor.a = value;
                        settings.startColor = startColor;
                    });
                }
                m_targetAmountPerItem[itemIdx]--;
                if (m_targetAmountPerItem[itemIdx] < 0)
                    m_targetAmountPerItem[itemIdx] = 0;
                m_targetItemsTxt[itemIdx].text = m_targetAmountPerItem[itemIdx].ToString();
            }
        }
    }

    private void OnDeactivatePowerup(string eventName, ActionParams _data)
    {
        m_isPowerupActive = false;
    }

    private void OnAllPowerupsEnabled(string eventName, ActionParams _data)
    {
        for (int i = 0; i < m_availablePowerupRepresentation.Length; i++)
        {
            if (m_availablePowerupRepresentation[i] && m_availablePowerupRepresentation[i].gameObject.activeSelf)
            {
                m_availablePowerupRepresentation[i].transform.parent.GetComponent<Button>().interactable = true;
            }
        }
    }

    /// <summary>
    /// Callback on clicking a powerup, disable the button and update the powerupManager.
    /// </summary>
    public void OnPowerupClicked(Button clickedButton)
    {
        print("HUD: OnPowerupClicked");

        if (Board.Instance.IsBoardAvailable() && !m_isPowerupActive)
        {
            int selectedPowerupIdx = int.Parse(clickedButton.name.Substring(m_btnPrefix.Length));
            ActionParams data = new ActionParams();
            data.Put("selectedPowerupIdx", selectedPowerupIdx);
            EventManager.TriggerEvent(EventNames.ON_POWERUP_ACTIVATED, data);
            clickedButton.interactable = false;
            m_isPowerupActive = true;
        }
    }

    /// <summary>
    /// Callback on the settings button clicked, open the settings popup.
    /// </summary>
    public void OnSettingsClicked()
    {
        EventManager.TriggerEvent(EventNames.SHOW_SETTINGS_POPUP);
    }
}
