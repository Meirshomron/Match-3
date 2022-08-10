using System.Collections;
using UnityEngine;
using TMPro;

public class SelectTilesCallout : MonoBehaviour
{
    [SerializeField] private TMP_Text m_text;
    private const string m_multipleTilesTxt = "SELECT %s TILES";
    private const string m_singleTileTxt = "SELECT %s TILE";

    public void Init(int amountOfTiles)
    {
        SetText(amountOfTiles);
        StartCoroutine(DeactivateOnAnimationComplete(GetComponent<Animator>()));
    }

    private void SetText(int amountOfTiles)
    {
        string result;
        if (amountOfTiles == 1)
        {
            result = m_singleTileTxt;
        }
        else
        {
            result = m_multipleTilesTxt;
        }
        result = result.Replace("%s", amountOfTiles.ToString());
        m_text.text = result;
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
        gameObject.SetActive(false);
    }
}
