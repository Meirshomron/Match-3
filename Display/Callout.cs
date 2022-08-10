
using TMPro;
using UnityEngine;

public class Callout : MonoBehaviour
{
    [SerializeField] private TMP_Text m_text;

    public void SetText(string text)
    {
        m_text.text = text;
    }   

}
