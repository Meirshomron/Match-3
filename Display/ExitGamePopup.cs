using UnityEngine;
using DG.Tweening;

public class ExitGamePopup : MonoBehaviour
{
    [SerializeField] private GameObject m_mainAsset;

    private float m_enterPopupDuration = 1f;

    public void Init()
    {
        gameObject.SetActive(true);
        EnterPopupTween();
        Board.Instance.IsActive = false;
    }

    /// <summary>
    /// Tween the popup into view.
    /// </summary>
    private void EnterPopupTween()
    {
        Vector3 startPos = m_mainAsset.transform.position;
        float targetX = startPos.x;
        startPos.x -= Screen.width/2 + m_mainAsset.GetComponent<RectTransform>().rect.width;
        m_mainAsset.transform.position = startPos;
        m_mainAsset.transform.DOMoveX(targetX, m_enterPopupDuration).SetEase(Ease.OutCubic);
    }

    public void OnExitClicked()
    {
        print("OnExitClicked");
        Application.Quit();

    }

    public void OnCloseClicked()
    {
        print("OnCloseClicked");
        gameObject.SetActive(false);
    }
}
