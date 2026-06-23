using UnityEngine;
using TMPro;

public class ParticipantIdManager : MonoBehaviour
{
    [Header("ID Settings")]
    public string prefix = "P";
    public int digits = 3;

    [Header("Optional UI")]
    public TMP_Text participantIdText;

    public string CurrentParticipantId { get; private set; }

    private const string CounterKey = "ParticipantIdCounter";

    private void Awake()
    {
        GenerateNewParticipantId();
    }

    private void GenerateNewParticipantId()
    {
        int nextNumber = PlayerPrefs.GetInt(CounterKey, 1);

        CurrentParticipantId = prefix + nextNumber.ToString("D" + digits);

        PlayerPrefs.SetInt(CounterKey, nextNumber + 1);
        PlayerPrefs.Save();

        if (participantIdText != null)
            participantIdText.text = $"Participant ID: {CurrentParticipantId}";

        Debug.Log($"[ParticipantIdManager] Generated participant ID: {CurrentParticipantId}");
    }

    [ContextMenu("Reset Participant Counter")]
    public void ResetCounter()
    {
        PlayerPrefs.DeleteKey(CounterKey);
        PlayerPrefs.Save();

        Debug.Log("[ParticipantIdManager] Participant counter reset.");
    }
}