using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject m_bottomBarUI;
    [SerializeField] private Image m_sfxButton;
    [SerializeField] private Sprite m_sfxOn;
    [SerializeField] private Sprite m_sfxOff;
    [SerializeField] private Image m_musicButton;
    [SerializeField] private Sprite m_musicOn;
    [SerializeField] private Sprite m_musicOff;

    private float m_enterBarsUIDuration = 1.5f;
    private bool m_musicMute = false;
    private bool m_sfxMute = false;

    private void Start()
    {
        if (AudioManager.Instance.IsMusicMute())
        {
            ToggleMusic();
        }
        if (AudioManager.Instance.IsSFXMute())
        {
            ToggleSFX();
        }
        EnterBarUITween();
    }

    public void OnMusicToggle()
    {
        ToggleMusic();
        EventManager.TriggerEvent(EventNames.ON_TOGGLE_MUSIC);
    }

    public void OnSFXToggle()
    {
        ToggleSFX();
        EventManager.TriggerEvent(EventNames.ON_TOGGLE_SFX);
    }

    private void ToggleMusic()
    {
        m_musicMute = !m_musicMute;
        m_musicButton.sprite = !m_musicMute ? m_musicOn : m_musicOff;
    }

    private void ToggleSFX()
    {
        m_sfxMute = !m_sfxMute;
        m_sfxButton.sprite = !m_sfxMute ? m_sfxOn : m_sfxOff;
    }

    /// <summary>
    /// Tween in the top and bottom bars into view.
    /// </summary>
    private void EnterBarUITween()
    {
        float targetY = m_bottomBarUI.transform.position.y;
        float height = m_bottomBarUI.GetComponent<RectTransform>().rect.height;
        Vector3 startPos = m_bottomBarUI.transform.position;
        startPos.y -= height;

        m_bottomBarUI.transform.position = startPos;
        m_bottomBarUI.transform.DOMoveY(targetY, m_enterBarsUIDuration).SetEase(Ease.OutCubic);
    }

    public void OnHighscoresClicked()
    {
        ActionParams data = new ActionParams();
        data.Put("sceneId", "Highscores");
        EventManager.TriggerEvent(EventNames.ON_LOAD_SCENE, data);
    }

    public void OnAboutClicked()
    {
        ActionParams data = new ActionParams();
        data.Put("sceneId", "About");
        EventManager.TriggerEvent(EventNames.ON_LOAD_SCENE, data);
    }
}
