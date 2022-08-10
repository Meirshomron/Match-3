using System;
using System.Collections.Generic;
using UnityEngine;

public class PowerupManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> m_powerupsGO;
    [SerializeField] private GameObject m_powerupsParent;
    [SerializeField] private GameObject m_selectTilesCalloutPrefab;

    private string[] m_availablePowerups;
    private List<IPowerup> m_powerups;
    private IPowerup m_activePowerup;
    private SelectTilesCallout m_selectTilesCallout;
    private static PowerupManager _instance;

    public string[] AvailablePowerups { get { return m_availablePowerups; } set { m_availablePowerups = value; } }
    public static PowerupManager Instance { get { return _instance; } }

    public Transform PowerupsExtraParent { get { return m_powerupsParent.transform; } }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        InitPowerups();
        EventManager.StartListening("ON_POWERUP_ACTIVATED", OnPowerupActivated);
    }

    /// <summary>
    /// Create instances of all the powerups.
    /// </summary>
    private void InitPowerups()
    {
        m_powerups = new List<IPowerup>();
        foreach (GameObject powerupGO in m_powerupsGO)
        {
            GameObject powerupInstance = Instantiate(powerupGO);
            m_powerups.Add(powerupInstance.GetComponent<IPowerup>());
            powerupInstance.transform.SetParent(m_powerupsParent.transform);
        }
    }

    /// <summary>
    /// Callback on powerup clicked to be activated, update the board and activate it.
    /// </summary>
    private void OnPowerupActivated(string eventName, ActionParams _data)
    {
        int selectedPowerupIdx = _data.Get<int>("selectedPowerupIdx");
        string selectedPowerupId = m_availablePowerups[selectedPowerupIdx];
        foreach (IPowerup powerup in m_powerups)
        {
            if (powerup.ID == selectedPowerupId)
            {
                m_activePowerup = powerup;
                break;
            }
        }

        if (m_activePowerup.TotalSelectedTiles > 0)
        {
            ShowSelectTilesCallout();
        }
        // If the powerup is immidiate then dont wait for the player to select a tile or for the powerup do activate.
        Board.Instance.SetPowerupActive(m_activePowerup.TotalSelectedTiles);
        m_activePowerup.OnPowerupActivated();
    }

    private void ShowSelectTilesCallout()
    {
        if (m_selectTilesCallout == null)
        {
            m_selectTilesCallout = Instantiate(m_selectTilesCalloutPrefab).GetComponent<SelectTilesCallout>();
            m_selectTilesCallout.transform.SetParent(transform);
        }
        else
        {
            m_selectTilesCallout.gameObject.SetActive(true);
        }
        m_selectTilesCallout.Init(m_activePowerup.TotalSelectedTiles);
    }

    public void SetTileHit(List<(int, int)> tilesHit)
    {
        Board.Instance.HandleTilesHit(tilesHit);
    }

    public void DeactivatePowerup()
    {
        m_activePowerup = null;
        EventManager.TriggerEvent(EventNames.ON_DEACTIVATE_POWERUP);
    }

    public bool UsePowerupMatchAnimation()
    {
        return m_activePowerup.UsePowerupMatchAnimation;
    }

    public RuntimeAnimatorController GetCurrentPowerupAnimController()
    {
        return (m_activePowerup == null) ? null : m_activePowerup.AnimController;
    }

    internal void OnPowerupTilesSelected((int, int)[] m_powerupTilesSelected)
    {
        m_activePowerup.OnTilesSelected(m_powerupTilesSelected);
    }

    public Sprite GetPowerupSpriteUI(string powerupId)
    {
        foreach (IPowerup powerup in m_powerups)
        {
            if (powerupId == powerup.ID)
            {
                print("found sprite of " + powerupId);
                return powerup.SpriteRepresentation;
            }
        }
        return null;
    }

    /// <summary>
    /// Get the sprites of all the available powerups in this level.
    /// </summary>
    public Sprite[] GetAvailablePowerupsSpriteUI()
    {
        Sprite[] result = new Sprite[AvailablePowerups.Length];
        for (int i = 0; i < AvailablePowerups.Length; i++)
        {
            result[i] = GetPowerupSpriteUI(AvailablePowerups[i]);
        }

        return result;
    }
}
