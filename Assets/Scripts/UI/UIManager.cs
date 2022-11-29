using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] Animation screenFade;
    [SerializeField] List<UIScreenController> screenList;

    UIScreenController currentScreen;
    public UIScreenController CurrentScreen 
    {
        get => currentScreen;
        private set 
        {
            if (value != currentScreen)
                OnShowScreen(value);

            currentScreen = value;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        screenList.ForEach(x => x.gameObject.SetActive(false));
    }

    public UIScreenController GetScreen(string screen) 
    {
        UIScreenController selectedScreen = screenList.FirstOrDefault(x => x.screen == screen);

        if (selectedScreen != null)
        {
            return selectedScreen;
        }
        else
        {
            Debug.LogWarning($"Screen '{screen}' not found!");
            return null;
        }
    }

    private void OnShowScreen(UIScreenController screen) 
    {
        
    }

    public void ShowScreen(string screen)
    {
        if (CurrentScreen != null)
            CurrentScreen.gameObject.SetActive(false);

        UIScreenController selectedScreen = GetScreen(screen);

        if (selectedScreen != null)
        {
            screenFade?.Play();

            selectedScreen.gameObject.SetActive(true);
            CurrentScreen = selectedScreen;
            selectedScreen.OnShow();
        }
        else
        {
            Debug.LogWarning("Screen not found!");
        }
    }
}
