using System.Collections.Generic;
using UnityEngine;

public class Hammer : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "Hammer";
    private int m_totalSelectedTiles = 1;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnPowerupActivated() { }

    public void OnTilesSelected((int, int)[] m_powerupTilesSelected)
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        List<(int, int)> tiles = new List<(int, int)>();
        tiles.Add(m_powerupTilesSelected[0]);
        PowerupManager.Instance.SetTileHit(tiles);
    }
}
