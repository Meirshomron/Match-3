using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class LevelSuccessPopup : MonoBehaviour
{
    [SerializeField] private GameObject m_mainAsset;
    [SerializeField] private GameObject m_achivement;
    [SerializeField] private TMP_Text m_scoreTxt;
    [SerializeField] private TMP_Text m_movesTxt;
    [SerializeField] private TMP_Text m_movesTitleTxt;
    [SerializeField] private TMP_Text m_tilesHitTxt;
    [SerializeField] private TMP_Text m_tilesHitTitleTxt;
    [SerializeField] private Button m_nextLevelBtn;
    [SerializeField] private Button m_menuBtn1;
    [SerializeField] private Button m_menuBtn2;

    private float m_enterPopupDuration = 1f;
    private float m_tweenPopupDuration = 1f;
    private float m_delayAfterEnterDuration = 1f;

    private int m_targetScore;
    private int m_targetMovesLeft;
    private int m_targetTilesHit;
    private int m_currentScore;
    private int m_playerCurrentLevel;
    private int m_currentTilesHit;
    private int m_currentMovesLeft;
    private int m_achivementPunchAmount = 5;
    private Vector3 m_achivementPunchScale = new Vector3(.3f, .3f);
    public void Init(ActionParams data)
    {
        //print("LevelSuccessPopup Init");
        EventManager.StartListening(EventNames.ON_NEW_HIGHSCORE, OnNewHighscore);

        m_targetMovesLeft = data.Get<int>("movesLeft");
        m_targetTilesHit = data.Get<int>("tilesHitCounter");
        m_targetScore = data.Get<int>("totalScore");
        m_playerCurrentLevel = data.Get<int>("playerCurrentLevel");

        m_achivement.SetActive(false);
        gameObject.SetActive(true);
        m_nextLevelBtn.interactable = false;
        m_menuBtn1.interactable = false;
        m_menuBtn2.interactable = false;
        m_currentScore = 0;
        m_currentTilesHit = 0;
        m_currentMovesLeft = 0;

        m_scoreTxt.text = m_currentScore.ToString();
        m_tilesHitTxt.text = m_targetTilesHit.ToString();
        m_movesTxt.text = m_targetMovesLeft.ToString();
        EnterPopupTween();
        Board.Instance.IsActive = false;
        PlayerData.Instance.SetScoreAtLevel(m_playerCurrentLevel, m_targetScore);
    }

    private void OnNewHighscore(string eventName, ActionParams _data)
    {
        //print("OnNewHighscore");
        m_achivement.SetActive(true);
        m_achivement.GetComponent<RectTransform>().DOPunchScale(m_achivementPunchScale, 0.5f, 0, 0).SetLoops(m_achivementPunchAmount);
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
        m_mainAsset.transform.DOMoveX(targetX, m_enterPopupDuration).SetEase(Ease.OutCubic).OnComplete(() => StartCoroutine(OnEnterPopupCompelete()));
    }

    IEnumerator OnEnterPopupCompelete()
    {
        yield return new WaitForSeconds(m_delayAfterEnterDuration);

        DOTween.To(() => m_currentScore, x => m_currentScore = x, m_targetScore, m_tweenPopupDuration).OnUpdate(() => m_scoreTxt.text = m_currentScore.ToString()).OnComplete(OnTweenPopupScoreComplete);
        DOTween.To(() => m_currentTilesHit, x => m_currentTilesHit = x, m_targetTilesHit, m_tweenPopupDuration).OnUpdate(() => m_tilesHitTxt.text = (m_targetTilesHit - m_currentTilesHit).ToString());
        DOTween.To(() => m_currentMovesLeft, x => m_currentMovesLeft = x, m_targetMovesLeft, m_tweenPopupDuration).OnUpdate(() => m_movesTxt.text = (m_targetMovesLeft - m_currentMovesLeft).ToString());
        m_scoreTxt.GetComponent<RectTransform>().DOPunchScale(Vector3.one, m_tweenPopupDuration, 0, 0.05f);
        m_movesTxt.DOFade(0, m_tweenPopupDuration);
        m_movesTitleTxt.DOFade(0, m_tweenPopupDuration);
        m_tilesHitTxt.DOFade(0, m_tweenPopupDuration);
        m_tilesHitTitleTxt.DOFade(0, m_tweenPopupDuration);
    }

    private void OnTweenPopupScoreComplete()
    {
        m_nextLevelBtn.interactable = true;
        m_menuBtn1.interactable = true;
        m_menuBtn2.interactable = true;
    }

    public void OnNextLevelClicked()
    {
        print("OnNextLevelClicked");
        EventManager.TriggerEvent(EventNames.ON_PLAY_NEXT_LEVEL_CLICKED);
        Close();
    }

    public void OnMenuClicked()
    {
        print("OnMenuClicked");
        EventManager.TriggerEvent(EventNames.ON_MENU_CLICKED);
        Close();
    }

    private void ResetTweenTexts()
    {
        m_movesTxt.DOKill();
        m_movesTitleTxt.DOKill();
        m_tilesHitTxt.DOKill();
        m_tilesHitTitleTxt.DOKill();

        m_movesTxt.DOFade(1, 0);
        m_movesTitleTxt.DOFade(1, 0);
        m_tilesHitTxt.DOFade(1, 0);
        m_tilesHitTitleTxt.DOFade(1, 0);
    }

    private void Close()
    {
        EventManager.StopListening(EventNames.ON_NEW_HIGHSCORE, OnNewHighscore);
        ResetTweenTexts();
        gameObject.SetActive(false);

    }
}
