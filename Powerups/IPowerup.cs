using UnityEngine;

public interface IPowerup
{
    public string ID { get;}
    public int TotalSelectedTiles { get; }
    public bool UsePowerupMatchAnimation { get; }
    public RuntimeAnimatorController AnimController { get; }
    public Sprite SpriteRepresentation { get; }
    public void OnPowerupActivated();
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected);
}
