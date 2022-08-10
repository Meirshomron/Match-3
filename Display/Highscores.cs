using System.Collections.Generic;
using UnityEngine;

public class Highscores : MonoBehaviour
{
    [SerializeField] private GameObject m_rowPrefab;
    [SerializeField] private GameObject m_content;

    private HighscoreRow[] m_rows;

    void Start()
    {
        PlayerData.Instance.ValidateHighscoresDB();
        Dictionary<int, int> playerTopScores = PlayerData.Instance.GetAllPlayerTopScores();
        Dictionary<int, HighscoreData> highscores = PlayerData.Instance.GetAllHighscores();
        m_rows = new HighscoreRow[playerTopScores.Count];
        for (int i = 0; i < playerTopScores.Count; i++)
        {
            GameObject rowGO = Instantiate(m_rowPrefab);
            rowGO.transform.SetParent(m_content.transform);
            rowGO.transform.localScale = Vector3.one;
            m_rows[i] = rowGO.GetComponent<HighscoreRow>();
            int level = (i + 1);
            m_rows[i].Init(level, playerTopScores[level], highscores[level].score, highscores[level].username);
        }
    }

    public static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }

    public void OnBackClicked()
    {
        ActionParams data = new ActionParams();
        data.Put("sceneId", "MainMenu");
        EventManager.TriggerEvent(EventNames.ON_LOAD_SCENE, data);
    }
}
