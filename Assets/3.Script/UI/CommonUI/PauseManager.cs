using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private PausePanelView pausePanelView;
    [SerializeField] private MonoBehaviour sceneHandlerObject;

    private IPauseSceneHandler sceneHandler;

    private readonly Stack<IPauseSubPanel> subPanelStack = new Stack<IPauseSubPanel>();

    private PauseState curState = PauseState.Playing;
    public bool isPauseActive
    {
        get
        {
            return curState != PauseState.Playing;
        }
    }

    private void Awake()
    {
        if (sceneHandlerObject != null)
        {
            sceneHandler = sceneHandlerObject as IPauseSceneHandler;
        }

        if (sceneHandler == null)
        {
            Debug.LogError("sceneHandlerObject에 IPauseSceneHandler 구현체가 연결되지 않음");
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void EnterPause()
    {
        if (curState != PauseState.Playing) return;

        curState = PauseState.Transitioning;

        if (sceneHandler != null) sceneHandler.OnPauseEntered();

        pausePanelView.GameToPauseAnimation(() => { curState = PauseState.Paused; });
    }

    public void OpenSubPanel(IPauseSubPanel subPanel)
    {
        if (curState != PauseState.Paused || subPanel == null) return;
        curState = PauseState.Transitioning;

        pausePanelView.PauseToSubmenuAnimation(() =>
        {
            subPanel.PlayOpen(() =>
            {
                subPanelStack.Push(subPanel);
                curState = PauseState.Paused;
            });
        });
    }

    public void ReturnPause()
    {
        if (curState != PauseState.Paused || subPanelStack.Count == 0) return;
        curState = PauseState.Transitioning;

        IPauseSubPanel top = subPanelStack.Pop();

        top.PlayClose(() =>
        {
            if (subPanelStack.Count == 0) pausePanelView.SubMenuToPauseAnimation(() => { curState = PauseState.Paused; });
            else curState = PauseState.Paused;
        });

    }

    public void ReturnGame()
    {
        if (curState != PauseState.Paused) return;
        curState = PauseState.Transitioning;

        pausePanelView.PauseToGameAnimation(() =>
        {

            curState = PauseState.CountDowning;
            if (sceneHandler == null) curState = PauseState.Playing;
            else sceneHandler.OnResumeRequested(() =>
            {
                sceneHandler.OnResumeCompleted();
                curState = PauseState.Playing;
            });
        });
    }

    public void HandlePauseInput()
    {
        switch (curState)
        {
            case PauseState.Transitioning:
                return;

            case PauseState.CountDowning:
                return;

            case PauseState.Playing:
                EnterPause();
                break;

            case PauseState.Paused:
                if (subPanelStack.Count > 0) ReturnPause();
                else if (subPanelStack.Count == 0) ReturnGame();
                break;
        }
    }

}
