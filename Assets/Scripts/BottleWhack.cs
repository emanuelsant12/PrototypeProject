using System.Collections;
using UnityEngine;

public class BottleWhack : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;
    public string upTrigger = "BottleUp";
    public string downTrigger = "BottleDown";

    [Header("Timing")]
    public Vector2 idleWaitRange = new Vector2(1.5f, 4f);
    public bool loop = true;

    [Header("Audio")]
    public AudioClip hitClip;
    [Range(0, 1)] public float hitVolume = 1f;

    bool isUp;

    void OnEnable() { StartCoroutine(Run()); }
    void OnDisable() { StopAllCoroutines(); isUp = false; }

    IEnumerator Run()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(idleWaitRange.x, idleWaitRange.y));
            isUp = true;
            if (animator) animator.SetTrigger(upTrigger);

            // wait until ForceDown() is called
            yield return new WaitUntil(() => !isUp);

            if (!loop) yield break; // stop after the first hit if loop==false
        }
    }

    // Call this from your hammer on a valid hit
    public void ForceDown()
    {
        if (!isUp) return;
        isUp = false;
        Debug.Log($"Bottle {gameObject.name} hit down at {Time.time:F2}");
        if (animator) animator.SetTrigger(downTrigger);
        if (hitClip) AudioSource.PlayClipAtPoint(hitClip, transform.position, hitVolume);
    }
}
