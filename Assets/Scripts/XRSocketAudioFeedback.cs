using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;


[RequireComponent(typeof(XRSocketInteractor))]
public class XRSocketAudioFeedback : MonoBehaviour
{
  
    [Header("Clips")]
    [Tooltip("Sound to play when an object starts hovering over the socket.")]
    [SerializeField] private AudioClip hoverEnterClip;

    [Tooltip("Sound to play when an object stops hovering over the socket.")]
    [SerializeField] private AudioClip hoverExitClip;

    [Tooltip("Sound to play when an object is successfully socketed.")]
    [SerializeField] private AudioClip selectClip;

    [Header("3D Audio Settings")]
    [Tooltip("Enable this if you want the audio to be processed spatially (e.g., directional in VR).")]
    [SerializeField] private bool spatialize = true;

    [Tooltip("0 = fully 2D sound (always the same volume), 1 = fully 3D sound (directional and distance-based).")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 1f;

    private XRSocketInteractor socket;
    private AudioSource audioSource;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();    
        audioSource = gameObject.AddComponent<AudioSource>();

        // Basic setup
        audioSource.playOnAwake = false;
        audioSource.spatialize = spatialize;   // Enable spatial audio for XR headsets
        audioSource.spatialBlend = spatialBlend; // Adjust between 2D and 3D sound
    }

    private void OnEnable()
    {
        // Subscribe to socket interaction events
        socket.hoverEntered.AddListener(OnHoverEntered);
        socket.hoverExited.AddListener(OnHoverExited);
        socket.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        socket.hoverEntered.RemoveListener(OnHoverEntered);
        socket.hoverExited.RemoveListener(OnHoverExited);
        socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Play(hoverEnterClip);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        Play(hoverExitClip);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Play(selectClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
