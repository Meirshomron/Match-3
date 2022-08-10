using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelItem : MonoBehaviour
{
    [SerializeField] private TMP_Text m_levelTxt;
    [SerializeField] private Button m_levelBtn;
    [SerializeField] private GameObject m_lock;
    [SerializeField] private Material m_originalMaterial;

    private float m_minThickness = 4.0f;
    private float m_maxThickness = 9.0f;
    private float m_effectDuration = .6f;
    private int m_level;
    private bool m_isLocked;
    private bool m_isCurrentLevel;
    private bool m_isActive;

    public void Init(int level, bool isLocked, bool isCurrentLevel)
    {
        m_level = level;
        m_levelTxt.text = level.ToString();
        m_isLocked = isLocked;
        m_isCurrentLevel = isCurrentLevel;
        m_levelBtn.interactable = !isLocked;
        m_lock.SetActive(isLocked);
        m_isActive = true;

        if (m_isCurrentLevel)
        {
            SetCurrentLevelEffect();
        }
    }

    private void SetCurrentLevelEffect()
    {
        Image image = m_levelBtn.GetComponent<Image>();
        if (image.material != null)
        {
            m_originalMaterial = m_levelBtn.GetComponent<Image>().material;
            image.material = Instantiate(image.material);
            LoopEffect(image, m_maxThickness);
        }
    }

    private void LoopEffect(Image image, float thickness)
    {
        image.material.DOFloat(thickness, "_Thickness", m_effectDuration).OnComplete(() => { 
            if (thickness == m_minThickness)
                LoopEffect(image, m_maxThickness); 
            else
                LoopEffect(image, m_minThickness);
        }).SetEase(Ease.InOutBounce);
    }

    public void OnLevelClicked()
    {
        if (m_isActive)
        {
            AudioManager.Instance.PlaySound("Click");

            print("OnLevelClicked " + m_level);

            ActionParams data = new ActionParams();
            data.Put("level", m_level);
            EventManager.TriggerEvent(EventNames.ON_LEVEL_CLICKED, data);
        }
    }

    public void Disable()
    {
        m_isActive = false;
    }

    public void Enable()
    {
        m_isActive = true;
    }

    public void OnRemoved()
    {
        if (m_originalMaterial)
        {
            Image image = m_levelBtn.GetComponent<Image>();
            image.material.DOKill();

            image.material = m_originalMaterial;
            image.material.SetFloat("_Thickness", 0f);
        }
    }
}
