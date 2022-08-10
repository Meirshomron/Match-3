using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GatherTwoSidesCallout : MonoBehaviour
{
    [SerializeField] private Image m_goingLeftImg;
    [SerializeField] private Image m_goingRightImg;
    private Vector3 m_originalScale;

    void Awake()
    {
        m_originalScale = GetComponent<RectTransform>().localScale;
    }

    public void Init(string goingLeftTileType, string goingRightTileType)
    {
        SetImages(goingLeftTileType, goingRightTileType);
        StartCoroutine(DeactivateOnAnimationComplete(GetComponent<Animator>()));
    }

    private void SetImages(string goingLeftTileType, string goingRightTileType)
    {
        GameObject tileGO = Board.Instance.CreateTile(goingLeftTileType);
        SpriteRenderer tileSpriteRenderer = tileGO.GetComponent<SpriteRenderer>();
        m_goingLeftImg.sprite = tileSpriteRenderer.sprite;
        m_goingLeftImg.color = tileSpriteRenderer.color;
        Board.Instance.RemoveTile(tileGO.GetComponent<Tile>());

        tileGO = Board.Instance.CreateTile(goingRightTileType);
        tileSpriteRenderer = tileGO.GetComponent<SpriteRenderer>();
        m_goingRightImg.sprite = tileSpriteRenderer.sprite;
        m_goingRightImg.color = tileSpriteRenderer.color;
        Board.Instance.RemoveTile(tileGO.GetComponent<Tile>());
    }

    public IEnumerator DeactivateOnAnimationComplete(Animator anim)
    {
        while (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
        {
            yield return null;
        }
        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

        // Reset and deactivate the callout.
        GetComponent<CanvasGroup>().alpha = 1;
        GetComponent<RectTransform>().localScale = m_originalScale;
        gameObject.SetActive(false);
        GetComponent<RectTransform>().localScale = m_originalScale;
    }
}
