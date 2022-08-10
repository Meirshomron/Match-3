using UnityEngine;

public class About : MonoBehaviour
{
    public void OnBackClicked()
    {
        ActionParams data = new ActionParams();
        data.Put("sceneId", "MainMenu");
        EventManager.TriggerEvent(EventNames.ON_LOAD_SCENE, data);
    }
}
