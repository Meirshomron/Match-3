using System.Collections.Generic;
using UnityEngine;

public class XShape : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "XShape";
    private string m_newEffectID = "Particle";
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

        int currentColumn = tileSource.Item2 + 1;
        for (int row = (tileSource.Item1 + 1); row < Board.Instance.COUNT_ROWS; row++)
        {
            if (Board.Instance.IsValidTileIndices((row, currentColumn)) && TilesUtility.IsTilePowerupEnabled((row, currentColumn)))
            {
                tiles.Add((row, currentColumn));
                currentColumn++;
            }
            else
                break;
        }

        currentColumn = tileSource.Item2 - 1;
        for (int row = (tileSource.Item1 + 1); row < Board.Instance.COUNT_ROWS; row++)
        {
            if (Board.Instance.IsValidTileIndices((row, currentColumn)) && TilesUtility.IsTilePowerupEnabled((row, currentColumn)))
            {
                tiles.Add((row, currentColumn));
                currentColumn--;
            }
            else
                break;
        }

        currentColumn = tileSource.Item2 - 1;
        for (int row = (tileSource.Item1 - 1); row < Board.Instance.COUNT_ROWS; row--)
        {
            if (Board.Instance.IsValidTileIndices((row, currentColumn)) && TilesUtility.IsTilePowerupEnabled((row, currentColumn)))
            {
                tiles.Add((row, currentColumn));
                currentColumn--;
            }
            else
                break;
        }

        currentColumn = tileSource.Item2 + 1;
        for (int row = (tileSource.Item1 - 1); row < Board.Instance.COUNT_ROWS; row--)
        {
            if (Board.Instance.IsValidTileIndices((row, currentColumn)) && TilesUtility.IsTilePowerupEnabled((row, currentColumn)))
            {
                tiles.Add((row, currentColumn));
                currentColumn++;
            }
            else
                break;
        }

        List<GameObject> particles = PowerupsUtility.CreateExtraListFromPool(tiles, m_newEffectID);
        foreach(GameObject particle in particles)
        {
            StartCoroutine(PowerupsUtility.ReturnOnAnimationComplete(particle.GetComponent<Animator>(), particle));
        }
        PowerupManager.Instance.SetTileHit(tiles);
    }
}
