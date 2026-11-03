using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    // 게임 씬 HUD와 일상 씬 HUD는 각기 다른 HUD이고, 상시 HUDManager가 현재 UI를 변경.

    public static HUDManager Instance = null;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private DanceSceneHUD danceHUD;
    private LifeSceneHUD lifeHUD;

    private void Start()
    {
        danceHUD = GetComponentInChildren<DanceSceneHUD>();
        lifeHUD = GetComponentInChildren<LifeSceneHUD>();

        danceHUD.gameObject.SetActive(false);
        lifeHUD.gameObject.SetActive(false);

        InGameEvent.OnLifeToDance += SwitchSceneToDanceScene;
        InGameEvent.OnDanceToLife += SwitchSceneToLifeScene;

        // TODO 아래는 임시 HUD 활성화
        // 나중에 씬 시작 이벤트로 불러올 것

        SwitchSceneToDanceScene();
    }

    public void SwitchSceneToDanceScene()
    {
        // Life UI를 종료하고 Dance UI를 킨다.
        Debug.Log("[HUD] 댄스 파트 UI 활성화");

        danceHUD.gameObject.SetActive(true);
        lifeHUD.gameObject.SetActive(false);
    }

    public void SwitchSceneToLifeScene()
    {
        // Dance UI 를 종료하고 Life UI를 킨다.
        Debug.Log("[HUD] 일상 파트 UI 활성화");

        danceHUD.gameObject.SetActive(false);
        lifeHUD.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        InGameEvent.OnLifeToDance -= SwitchSceneToDanceScene;
        InGameEvent.OnDanceToLife -= SwitchSceneToLifeScene;
    }


}
