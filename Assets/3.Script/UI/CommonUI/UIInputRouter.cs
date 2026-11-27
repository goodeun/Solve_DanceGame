using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputRouter : MonoBehaviour
{
    [SerializeField] private InputActionReference cancelActionRef;
    [SerializeField] private PauseManager pauseManager;

    private readonly Stack<ICancelableUI> uiStack = new Stack<ICancelableUI>();

    private void OnEnable()
    {
        if (cancelActionRef == null || cancelActionRef.action == null) return;
        cancelActionRef.action.performed += HandleCancelInput;
        cancelActionRef.action.Enable();
    }

    private void OnDisable()
    {
        if (cancelActionRef == null || cancelActionRef.action == null) return;
        cancelActionRef.action.performed -= HandleCancelInput;
        cancelActionRef.action.Disable();
    }

    public void PushUI(ICancelableUI ui)
    {
        if (ui == null) return;

        uiStack.Push(ui);
    }

    public void PopUI()
    {
        if (uiStack.Count <=0) return;
        ICancelableUI curUI = uiStack.Pop();
        return;
    }

    private void HandleCancelInput(InputAction.CallbackContext context)
    {
        if (pauseManager != null && pauseManager.isPauseActive == true)
        {
            pauseManager.HandlePauseInput();
            return;
        }
        else if (uiStack.Count >0)
        {
            uiStack.Pop().OnCancel();
            return;
        }
        else if (pauseManager != null)
        {
            pauseManager.HandlePauseInput();
        }
    }

}
