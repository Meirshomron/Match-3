using System.Collections.Generic;
using UnityEngine;

public class ChangeRandom : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;

    private string m_powerupID = "ChangeRandom";
    private int m_totalSelectedTiles = 0;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected) { }

    public void OnPowerupActivated() 
    {
        List<(int, int)> tilesHit = GetTilesAffected();
        ChangeTiles();
        PowerupManager.Instance.SetTileHit(tilesHit);
    }

    public List<(int, int)> GetTilesAffected()
    {
        List<(int, int)> tiles = new List<(int, int)>();

        for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
        {
            int amountOnColumn = UnityEngine.Random.Range(0, 3);
            HashSet<int> selectedRows = new HashSet<int>();
            for (int i = 0; i < amountOnColumn; i++)
            {
                int row = UnityEngine.Random.Range(0, Board.Instance.COUNT_ROWS);
                while (selectedRows.Contains(row))
                    row = UnityEngine.Random.Range(0, Board.Instance.COUNT_ROWS);
                selectedRows.Add(row);
                tiles.Add((row, col));
            }
        }
        return tiles;
    }

    public void ChangeTiles()
    {
        string tileType = TilesUtility.GetRandomTileType();
    }
}
