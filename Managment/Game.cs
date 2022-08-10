using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;

public class Game : MonoBehaviour
{

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            List<string> consentIdentifiers = await Events.CheckForRequiredConsents();
        }
        catch (ConsentCheckException e)
        {
            // Something went wrong when checking the GeoIP, check the e.Reason and handle appropriately
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EventManager.TriggerEvent(EventNames.SHOW_EXIT_GAME_POPUP);
        }
    }
}