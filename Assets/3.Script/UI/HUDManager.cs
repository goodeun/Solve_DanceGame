using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    // 게임 씬 HUD와 일상 씬 HUD는 각기 다른 HUD이고, 상시 HUDManager가 현재 UI를 변경.

    public static HUDManager Instance = null;

    private CanvasGroup pauseCanvasGroup;
    private bool isHiddenForPause;
    private bool isHiddenForFlow;
    private bool hasStoredState;
    private float alphaBeforePause;
    private bool interactableBeforePause;
    private bool blocksRaycastsBeforePause;

    public bool IsHiddenForFlow => isHiddenForFlow;

    /// <summary>Pause 메뉴용 숨김. 루트 CanvasGroup만 사용하고 Fever의 하위 CanvasGroup은 건드리지 않는다.</summary>
    public void SetPauseHidden(bool hidden)
    {
        if (isHiddenForPause == hidden) return;
        isHiddenForPause = hidden;
        ApplyHiddenState();
    }

    /// <summary>씬 흐름용 숨김(곡 시작 전 대사·보스 등장 연출 등). Pause 숨김과 독립적으로 유지된다.</summary>
    public void SetFlowHidden(bool hidden)
    {
        if (isHiddenForFlow == hidden) return;
        isHiddenForFlow = hidden;
        ApplyHiddenState();
    }

    private void ApplyHiddenState()
    {
        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = GetComponent<CanvasGroup>();
            if (pauseCanvasGroup == null)
                pauseCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        bool hidden = isHiddenForPause || isHiddenForFlow;

        if (hidden)
        {
            if (!hasStoredState)
            {
                alphaBeforePause = pauseCanvasGroup.alpha;
                interactableBeforePause = pauseCanvasGroup.interactable;
                blocksRaycastsBeforePause = pauseCanvasGroup.blocksRaycasts;
                hasStoredState = true;
            }

            // Hide the root without changing Fever's child CanvasGroup or stopping scripts.
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        else if (hasStoredState)
        {
            pauseCanvasGroup.alpha = alphaBeforePause;
            pauseCanvasGroup.interactable = interactableBeforePause;
            pauseCanvasGroup.blocksRaycasts = blocksRaycastsBeforePause;
            hasStoredState = false;
        }
    }

    /// <summary>
    /// 씬 전환 전에 호출. 각 댄스 씬이 자기 MasterCanvas를 가지므로 이전 씬의 HUD를 해제해
    /// 다음 씬의 HUDManager가 Instance가 되도록 한다.
    /// </summary>
    public static void ReleaseForSceneChange()
    {
        if (Instance == null) return;

        GameObject go = Instance.gameObject;
        Instance = null;
        Destroy(go);
    }

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
