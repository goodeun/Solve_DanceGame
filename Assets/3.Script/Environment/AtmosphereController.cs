using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AtmosphereController : MonoBehaviour
{
    [FormerlySerializedAs("AtmosphereScore")]
    [SerializeField, Range(0f, 100f)] private float moodScore = 0f;

    [SerializeField] private int heating = 30;
    [SerializeField] private int burning = 70;

    public int Heating => heating;
    public int Burning => burning;
    public float MoodScore => moodScore;

    private readonly List<AudienceController> audiences = new();

    public void Register(AudienceController audience)
    {
        if (audience != null && !audiences.Contains(audience))
        {
            audiences.Add(audience);
            audience.SetMoodScore(moodScore);
        }
    }

    public void Unregister(AudienceController audience)
    {
        audiences.Remove(audience);
    }

    public void AddMood(float amount)
    {
        moodScore = Mathf.Clamp(moodScore + amount, 0f, 100f);
        BroadcastMoodScore();
    }

    public void SetMoodScore(float value)
    {
        moodScore = Mathf.Clamp(value, 0f, 100f);
        BroadcastMoodScore();
    }

    private void BroadcastMoodScore()
    {
        for (int i = audiences.Count - 1; i >= 0; i--)
        {
            if (audiences[i] == null)
                audiences.RemoveAt(i);
            else
                audiences[i].SetMoodScore(moodScore);
        }
    }
}
