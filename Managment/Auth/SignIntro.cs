using UnityEngine;

public class SignIntro : MonoBehaviour
{

    public void OnSignInClicked()
    {
        EventManager.TriggerEvent(EventNames.ON_SHOW_SIGN_IN_CLICKED);
    }

    public void OnCreateAccountClicked()
    {
        EventManager.TriggerEvent(EventNames.ON_SHOW_SIGN_UP_CLICKED);
    }
}