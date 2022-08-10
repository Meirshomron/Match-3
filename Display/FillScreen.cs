using UnityEngine;

public class FillScreen : MonoBehaviour
{
    [SerializeField] private Camera m_mainCamera;

    float m_screenWidth;
    float m_screenHeight;
    Vector3 m_bounds;

    void Start()
    {
        m_bounds = GetComponent<SpriteRenderer>().bounds.size;
        m_screenWidth = Screen.width;
        m_screenHeight = Screen.height;
        SpriteFillScreen();
    }

    void Update()
    {
        if (m_screenWidth != Screen.width || m_screenHeight != Screen.height)
        {
            m_screenWidth = Screen.width;
            m_screenHeight = Screen.height;
            SpriteFillScreen();
        }
    }

    private void SpriteFillScreen()
    {
        float height = m_mainCamera.orthographicSize * 2;
        float width = height * m_screenWidth / m_screenHeight;

        float scaleAmount = (width / m_bounds.x < height / m_bounds.y) ? height / m_bounds.y : width / m_bounds.x;
        transform.localScale = new Vector3(scaleAmount, scaleAmount);
    }
}
