using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PreviewLevelPopup : MonoBehaviour
{
    [SerializeField] private GameObject m_mainAsset;
    [SerializeField] private GameObject m_closeBtn;
    [SerializeField] private GameObject m_powerupsParent;
    [SerializeField] private Material m_selectedPowerupMatrial;
    [SerializeField] private Material m_defaultPowerupMatrial;

    private string m_btnPrefix = "Button";
    private float m_enterPopupDuration = 1f;
    private int m_amountOfPowerupsInLevel;
    private Image[] m_powerupChildren;
    private List<string> m_powerupsAvailable;
    private Queue<int> m_selectedPowerupIndices;
    private bool m_isCloseEnabled;
    public void Init(LevelData levelData, bool isCloseEnabled = true)
    {
        gameObject.SetActive(true);
        m_isCloseEnabled = isCloseEnabled;
        m_closeBtn.SetActive(m_isCloseEnabled);
        AddPowerups(levelData.powerupsAvailable, levelData.amountOfPowerupsInLevel);
        EnterPopupTween();
    }

    /// <summary>
    /// Tween the popup into view.
    /// </summary>
    private void EnterPopupTween()
    {
        Vector3 startPos = m_mainAsset.transform.position;
        float targetX = startPos.x;
        startPos.x -= Screen.width/2 + m_mainAsset.GetComponent<RectTransform>().rect.width;
        m_mainAsset.transform.position = startPos;
        m_mainAsset.transform.DOMoveX(targetX, m_enterPopupDuration).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// Add the sprites of the available powerups for the current level and enable selection of <amountOfPowerupsInLevel> pwoerups.
    /// </summary>
    public void AddPowerups(List<string> powerupsAvailable, int amountOfPowerupsInLevel)
    {
        m_selectedPowerupIndices = new Queue<int>();
        m_amountOfPowerupsInLevel = amountOfPowerupsInLevel;
        m_powerupChildren = new Image[powerupsAvailable.Count];
        m_powerupsAvailable = powerupsAvailable;
        for (int i = 0; i < powerupsAvailable.Count; i++)
        {
            Sprite powerupSprite = PowerupManager.Instance.GetPowerupSpriteUI(powerupsAvailable[i]);
            GameObject powerupChildGO = m_powerupsParent.transform.GetChild(i).gameObject;
            powerupChildGO.SetActive(true);
            m_powerupChildren[i] = powerupChildGO.GetComponent<Image>();
            m_powerupChildren[i].sprite = powerupSprite;

            // Set the first to be the default selected powerup.
            if (i < amountOfPowerupsInLevel)
            {
                m_powerupChildren[i].material = m_selectedPowerupMatrial;
                m_selectedPowerupIndices.Enqueue(i);
            }
            else
            {
                m_powerupChildren[i].material = m_defaultPowerupMatrial;
            }
        }

        if (powerupsAvailable.Count < m_powerupsParent.transform.childCount)
        {
            for (int i = powerupsAvailable.Count; i < m_powerupsParent.transform.childCount; i++)
            {
                GameObject powerupChildGO = m_powerupsParent.transform.GetChild(i).gameObject;
                powerupChildGO.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Callback on selecting a powerup sprite.
    /// </summary>
    public void OnPowerupSelected(Button powerupBtn)
    {
        int selectedPowerupIdx = int.Parse(powerupBtn.name.Substring(m_btnPrefix.Length));
        if (!m_selectedPowerupIndices.Contains(selectedPowerupIdx))
        {
            int lastSelectedPowerupIdx = m_selectedPowerupIndices.Dequeue();
            m_powerupChildren[lastSelectedPowerupIdx].GetComponent<Image>().material = m_defaultPowerupMatrial;
            m_powerupChildren[selectedPowerupIdx].GetComponent<Image>().material = m_selectedPowerupMatrial;
            m_selectedPowerupIndices.Enqueue(selectedPowerupIdx);
        }
    }

    public void OnCloseClicked()
    {
        if (m_isCloseEnabled)
        {
            EventManager.TriggerEvent(EventNames.ON_PREVIEW_LEVEL_POPUP_CLOSED);
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Callback on clicked to start playing the current level with the currently selected powerups. The levelManager actually loads the level.
    /// </summary>
    public void OnPlayClicked()
    {
        string[] selectedPowerupIds = new string[m_selectedPowerupIndices.Count];
        for (int i = 0; i < m_amountOfPowerupsInLevel; i++)
        {
            int selectedIdx = m_selectedPowerupIndices.Dequeue();
            selectedPowerupIds[i] = m_powerupsAvailable[selectedIdx];
        }

        ActionParams data = new ActionParams();
        data.Put("selectedPowerupIds", selectedPowerupIds);
        EventManager.TriggerEvent(EventNames.ON_PLAY_LEVEL_CLICKED, data);
        gameObject.SetActive(false);
    }
}
