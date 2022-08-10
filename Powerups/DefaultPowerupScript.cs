using System.Collections.Generic;
using UnityEngine;

public class DefaultPowerupScript : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;

    private string m_powerupID = "DefaultPowerupScript";
    private int m_totalSelectedTiles = 1;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnPowerupActivated() { }

    public void OnTilesSelected((int, int)[] m_powerupTilesSelected){}
}
