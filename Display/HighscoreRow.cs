using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HighscoreRow : MonoBehaviour
{
    private int m_level;
    private int m_myScore;
    private int m_highscore;
    private string m_username;

    [SerializeField] private TMP_Text m_levelTxt;
    [SerializeField] private TMP_Text m_myScoreTxt;
    [SerializeField] private TMP_Text m_highscoreTxt;
    [SerializeField] private TMP_Text m_usernameTxt;

    public void Init(int level, int myScore, int highscore, string username)
    {
        print("HighscoreRow: Init level = " + level + " myScore = " + myScore + " highscore = " + highscore + " username = " + username);
        this.m_level = level;
        this.m_myScore = myScore;
        this.m_highscore = highscore;
        this.m_username = username;

        m_levelTxt.text = m_level.ToString();
        m_myScoreTxt.text = m_myScore.ToString();
        m_highscoreTxt.text = m_highscore.ToString();
        m_usernameTxt.text = m_username;
    }
}
