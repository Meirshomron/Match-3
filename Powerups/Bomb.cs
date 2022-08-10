using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "Bomb";
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

        (int, int) tileSource = m_powerupTilesSelected[0];
        List<(int, int)> tiles = new List<(int, int)>();
        tiles.Add(tileSource);

        if (Board.Instance.IsValidTileIndices((tileSource.Item1 + 1, tileSource.Item2)) && TilesUtility.IsTilePowerupEnabled((tileSource.Item1 + 1, tileSource.Item2)))
            tiles.Add((tileSource.Item1 + 1, tileSource.Item2));

        if (Board.Instance.IsValidTileIndices((tileSource.Item1 - 1, tileSource.Item2)) && TilesUtility.IsTilePowerupEnabled((tileSource.Item1 - 1, tileSource.Item2)))
            tiles.Add((tileSource.Item1 - 1, tileSource.Item2));

        if (Board.Instance.IsValidTileIndices((tileSource.Item1, tileSource.Item2 + 1)) && TilesUtility.IsTilePowerupEnabled((tileSource.Item1, tileSource.Item2 + 1)))
            tiles.Add((tileSource.Item1, tileSource.Item2 + 1));

        if (Board.Instance.IsValidTileIndices((tileSource.Item1, tileSource.Item2 - 1)) && TilesUtility.IsTilePowerupEnabled((tileSource.Item1, tileSource.Item2 - 1)))
            tiles.Add((tileSource.Item1, tileSource.Item2 - 1));

        PowerupManager.Instance.SetTileHit(tiles);
    }
}
