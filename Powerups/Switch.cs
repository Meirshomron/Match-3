using UnityEngine;
using DG.Tweening;
using System.Collections;

public class Switch : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "Switch";
    private float m_switchDuration = 0.3f;
    private int m_totalSelectedTiles = 2;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnPowerupActivated() {}

    public void OnTilesSelected((int, int)[] m_powerupTilesSelected)
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        Tile tile1 = Board.Instance.Tiles[m_powerupTilesSelected[0].Item1, m_powerupTilesSelected[0].Item2];
        Tile tile2 = Board.Instance.Tiles[m_powerupTilesSelected[1].Item1, m_powerupTilesSelected[1].Item2];
        Vector3 tile2InitialPos = Board.Instance.GetTilePosition(m_powerupTilesSelected[1]);

        Board.Instance.Tiles[m_powerupTilesSelected[0].Item1, m_powerupTilesSelected[0].Item2] = tile2;
        Board.Instance.Tiles[m_powerupTilesSelected[1].Item1, m_powerupTilesSelected[1].Item2] = tile1;
        tile1.transform.DOMove(tile2InitialPos, m_switchDuration);
        tile2.transform.DOMove(tile1.transform.position, m_switchDuration);

        StartCoroutine(OnSwitchComplete());
    }

    IEnumerator OnSwitchComplete()
    {
        yield return new WaitForSeconds(m_switchDuration);
        PowerupManager.Instance.DeactivatePowerup();
        Board.Instance.OnBoardUpdated();
    }
}
