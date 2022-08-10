using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MixBoard : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "MixBoard";
    private float m_mixDuration = 0.5f;
    private int m_totalSelectedTiles = 0;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected) { }

    public void OnPowerupActivated()
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        OnMixBoard();
    }

    private void OnMixBoard()
    {
        // 1. Create a list of ints, int value per tile position of all the tiles that have powerups enabled. 
        // 2. Convert the list to an array.
        // 3. Shuffle the array.
        // 4. Iterate the array in pairs and swap between them.
        List<int> randomPositions = new List<int>();
        for (int row = 0; row < Board.Instance.COUNT_ROWS; row++)
        {
            for (int col = 0; col < Board.Instance.COUNT_COLUMNS; col++)
            {
                if (TilesUtility.IsTilePowerupEnabled((row, col)))
                {
                    randomPositions.Add((row * Board.Instance.COUNT_COLUMNS) + col);
                }
            }
        }

        int[] randomPositionsArr = randomPositions.ToArray();
        Utils.Shuffle(randomPositionsArr);

        for (int pos = 0; pos < randomPositionsArr.Length - 1; pos+=2)
        {
            int row1 = randomPositionsArr[pos] / Board.Instance.COUNT_COLUMNS;
            int col1 = randomPositionsArr[pos] % Board.Instance.COUNT_COLUMNS;
            int row2 = randomPositionsArr[pos + 1] / Board.Instance.COUNT_COLUMNS;
            int col2 = randomPositionsArr[pos + 1] % Board.Instance.COUNT_COLUMNS;
            Board.Instance.SwapTiles((row1, col1), (row2, col2), m_mixDuration);
        }
        StartCoroutine(OnMixComplete());
    }

    IEnumerator OnMixComplete()
    {
        yield return new WaitForSeconds(m_mixDuration);
        PowerupManager.Instance.DeactivatePowerup();
        Board.Instance.OnBoardUpdated();
    }
}
