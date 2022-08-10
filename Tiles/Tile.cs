using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private string m_tileType;
    [SerializeField] private bool m_isPowerupEnabled;
    [SerializeField] private bool m_isMatchEnabled;
    [SerializeField] private bool m_isMovable;
    [SerializeField] private Color m_tileColor;

    public string TileType => m_tileType;
    public bool IsPowerupEnabled => m_isPowerupEnabled;
    public bool IsMatchEnabled => m_isMatchEnabled;
    public bool IsMovable => m_isMovable;
    public Color TileColor => m_tileColor;
}