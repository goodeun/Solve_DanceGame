using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueEntry
{
    public string id;
    public string scene;
    public string state;
    public string text;

    public int minRhythm;
    public int minGroove;
    public int minPower;

    public int minStyle;
    public int minCondition;

    public BubbleState ParseState(string _state)
    {
        if (Enum.TryParse<BubbleState>(_state, out BubbleState state))
        {
            return state;
        }

        return BubbleState.positive;
    }
}

[System.Serializable]
public class DialogueDatabase
{
    public List<DialogueEntry> dialogues;
}
