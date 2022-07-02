using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class Interactable : MonoBehaviour
{
    FirstPersonController firstPersonController; 
    public static Action<string> OnInteractTooltipTextChange;
    public virtual void Awake()
    {
        firstPersonController = GameObject.FindGameObjectWithTag("Player").GetComponent<FirstPersonController>();
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
    public abstract string actionWord { get; }
    public virtual string interactPrompt()
    {
        var _actionWord = string.IsNullOrWhiteSpace(actionWord) ? "interact with" : actionWord;
        return ($"press {firstPersonController.interactKey.ToString().ToLower()} to {_actionWord} {gameObject.name}");
    }
    public abstract void OnInteract();
    public virtual void OnFocus()
    {
        Debug.Log("Focused on " + gameObject.name);
        OnInteractTooltipTextChange?.Invoke(interactPrompt());
    }
    public virtual void OnUnfocus()
    {
        Debug.Log("Unfocused on " + gameObject.name);
        OnInteractTooltipTextChange?.Invoke(null);
    }
}