using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance = null;

    [SerializeField] private TextAsset dialogueJsonFile;
    private DialogueDatabase dialogueDB;

    private List<DialogueEntry> validEntries = new List<DialogueEntry>(50);

    private void Awake()
    {
        if(Instance ==null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        dialogueDB = JsonUtility.FromJson<DialogueDatabase>(dialogueJsonFile.text);
    }

    public string GetDialogue(string scene, string state, PlayerStats curStats)
    {
        validEntries.Clear();
        foreach (DialogueEntry entry in dialogueDB.dialogues)
        {
            if (entry.scene == scene && entry.state == state)
            {
                if (curStats.rhythm >= entry.minRhythm &&
                    curStats.groove >= entry.minGroove &&
                    curStats.power >= entry.minPower &&
                    curStats.style >= entry.minStyle &&
                    curStats.condition >= entry.minCondition)
                {
                    validEntries.Add(entry);
                }
            }
        }

        if (validEntries.Count > 0)
        {
            int randomIndex = Random.Range(0, validEntries.Count);
            return validEntries[randomIndex].text;
        }
        return "No data";
    }

}
