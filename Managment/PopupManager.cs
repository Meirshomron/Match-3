using System;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField] private GameObject m_popupsParent;
    [SerializeField] private GameObject m_settingsPopupPrefab;
    [SerializeField] private GameObject m_levelFailedPopupPrefab;
    [SerializeField] private GameObject m_levelSuccessPopupPrefab;
    [SerializeField] private GameObject m_previewLevelPopupPrefab;
    [SerializeField] private GameObject m_exitGamePopupPrefab;

    private GameObject m_settingsPopup;
    private GameObject m_levelFailedPopup;
    private GameObject m_levelSuccessPopup;
    private GameObject m_previewLevelPopup;
    private GameObject m_exitGamePopup;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        AddListeners();
    }

    private void AddListeners()
    {
        EventManager.StartListening(EventNames.SHOW_LEVEL_SUCCESS_POPUP, ShowLevelSuccessPopup);
        EventManager.StartListening(EventNames.SHOW_LEVEL_FAILED_POPUP, ShowLevelFailedPopup);
        EventManager.StartListening(EventNames.SHOW_SETTINGS_POPUP, ShowSettingsPopup);
        EventManager.StartListening(EventNames.SHOW_PREVIEW_LEVEL_POPUP, ShowPreviewLevelPopup);
        EventManager.StartListening(EventNames.SHOW_EXIT_GAME_POPUP, ShowExitGamePopup);
    }

    private void ShowExitGamePopup(string eventName, ActionParams _data)
    {
        if (!m_exitGamePopup)
        {
            m_exitGamePopup = Instantiate(m_exitGamePopupPrefab.gameObject);
            m_exitGamePopup.transform.SetParent(m_popupsParent.transform);
        }
        else
        {
            if (m_exitGamePopup.activeSelf)
            {
                return;
            }
        }
        m_exitGamePopup.GetComponent<ExitGamePopup>().Init();
    }

    public void ShowLevelSuccessPopup(string eventName, ActionParams _data)
    {
        if (!m_levelSuccessPopup)
        {
            m_levelSuccessPopup = Instantiate(m_levelSuccessPopupPrefab.gameObject);
            m_levelSuccessPopup.transform.SetParent(m_popupsParent.transform);
        }
        m_levelSuccessPopup.GetComponent<LevelSuccessPopup>().Init(_data);
    }

    public void ShowLevelFailedPopup(string eventName, ActionParams _data)
    {
        if (!m_levelFailedPopup)
        {
            m_levelFailedPopup = Instantiate(m_levelFailedPopupPrefab.gameObject);
            m_levelFailedPopup.transform.SetParent(m_popupsParent.transform);
        }
        m_levelFailedPopup.GetComponent<LevelFailedPopup>().Init();
    }

    public void ShowSettingsPopup(string eventName, ActionParams _data)
    {
        if (!m_settingsPopup)
        {
            m_settingsPopup = Instantiate(m_settingsPopupPrefab.gameObject);
            m_settingsPopup.transform.SetParent(m_popupsParent.transform);
        }
        m_settingsPopup.GetComponent<SettingsPopup>().Init();
    }

    public void ShowPreviewLevelPopup(string eventName, ActionParams _data)
    {
        LevelData levelData = _data.Get<LevelData>("levelData");
        bool isInGame = _data.Get<bool>("isInGame");
        if (!m_previewLevelPopup)
        {
            m_previewLevelPopup = Instantiate(m_previewLevelPopupPrefab.gameObject);
            m_previewLevelPopup.transform.SetParent(m_popupsParent.transform);
        }

        m_previewLevelPopup.GetComponent<PreviewLevelPopup>().Init(levelData, !isInGame);
    }
}

