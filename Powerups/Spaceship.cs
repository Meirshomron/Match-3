using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spaceship : MonoBehaviour, IPowerup
{
    [SerializeField] private RuntimeAnimatorController m_animController;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Sprite m_spriteRepresentation;
    [SerializeField] private AudioClip m_powerupActiveAudio;

    private string m_powerupID = "Spaceship";
    private string m_newEffectID = "SpaceBuilding";
    private string m_explosionID = "Explosion";
    private float m_durationToColumn = 0.3f;
    private float m_durationToSpaceBuilding = 0.6f;
    private int m_totalSelectedTiles = 0;
    private List<(int, int)> m_spaceBuildingTiles;
    private HashSet<(int, int)> m_allTilesHit;
    private List<GameObject> m_spaceBuildingsGO;

    public string ID { get => m_powerupID; }
    public int TotalSelectedTiles { get => m_totalSelectedTiles; }
    public bool UsePowerupMatchAnimation { get => true; }
    public Sprite SpriteRepresentation => m_spriteRepresentation;
    public RuntimeAnimatorController AnimController => m_animController;
    public void OnTilesSelected((int, int)[] m_powerupTilesSelected) { }

    public void OnPowerupActivated() 
    {
        m_spaceBuildingTiles = PowerupsUtility.GetRandomTilesFullBoard(0, 3);
        m_spaceBuildingTiles.Sort(TilesUtility.SortLowestColumnHighestRow);
        TilesUtility.PlayTilesAnimations(m_spaceBuildingTiles, m_animController);
        m_spaceBuildingsGO = PowerupsUtility.CreateExtraListFromPool(m_spaceBuildingTiles, m_newEffectID);
        MoveSpaceship();
    }

    private void MoveSpaceship()
    {
        AudioManager.Instance.PlaySound(m_powerupID, m_powerupActiveAudio);

        m_allTilesHit = new HashSet<(int, int)>();
        m_spriteRenderer.enabled = true;
        transform.position = Board.Instance.GetTilePosition((Board.Instance.COUNT_ROWS - 1, 0)) - new Vector3(TilesUtility.GetTileWidth(), TilesUtility.GetTileHeight());
        Tween tween = transform.DOMove(Board.Instance.GetTilePosition((Board.Instance.COUNT_ROWS - 1, m_spaceBuildingTiles[0].Item2)), m_durationToColumn);
        StartCoroutine(OnCurrentSpaceshipTweenComplete(tween, 0));        
    }

    IEnumerator OnCurrentSpaceshipTweenComplete(Tween tween, int currentIndex)
    {
        yield return tween.WaitForCompletion();

        if (currentIndex < m_spaceBuildingTiles.Count)
        {
            Tween newTween;
            Vector3 nextSpaceBuilding = Board.Instance.GetTilePosition((m_spaceBuildingTiles[currentIndex].Item1, m_spaceBuildingTiles[currentIndex].Item2));
            if (Mathf.Abs(nextSpaceBuilding.x - transform.position.x) < PowerupsUtility.epsilon)
            {
                newTween = transform.DOMove(nextSpaceBuilding, m_durationToColumn).OnUpdate(() => CheckSelectedTile(transform.position));
                currentIndex++;
            }
            else
            {
                Vector3 nextSpaceBuildingColumn = new Vector3(nextSpaceBuilding.x, transform.position.y);
                newTween = transform.DOMove(nextSpaceBuildingColumn, m_durationToSpaceBuilding).OnUpdate(() =>CheckSelectedTile(transform.position));
            }
            StartCoroutine(OnCurrentSpaceshipTweenComplete(newTween, currentIndex));
        }
        // Completed movement.
        else
        {
            PowerupsUtility.ReturnExtraListToPool(this, m_spaceBuildingsGO, m_animController);
            m_spriteRenderer.DOFade(0, .5f).OnComplete(OnSpaceshipFadeComplete);
            PowerupManager.Instance.SetTileHit(m_allTilesHit.ToList());
        }
    }

    private void OnSpaceshipFadeComplete()
    {
        Color color = m_spriteRenderer.color;
        color.a = 1;
        m_spriteRenderer.color = color;
        m_spriteRenderer.enabled = false;
    }

    private void CheckSelectedTile(Vector3 position)
    {
        (int, int) tileIndices = Board.Instance.GetTileAtPosition(position);
        if (!Utils.IsTupleEmpty(tileIndices) && TilesUtility.IsTilePowerupEnabled(tileIndices))
        {
            if (!m_allTilesHit.Contains(tileIndices))
            {
                m_allTilesHit.Add(tileIndices);
                if (!m_spaceBuildingTiles.Contains(tileIndices))
                {
                    TilesUtility.PlayTileAnimation(tileIndices, m_animController);
                    GameObject explosion = PowerupsUtility.CreateExtraFromPool(tileIndices, m_explosionID);
                    StartCoroutine(PowerupsUtility.ReturnOnAnimationComplete(explosion.GetComponent<Animator>(), explosion));
                }
            }
        }
    }
}
