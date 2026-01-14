using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FootstepSound : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Footsteps Source")]
    [SerializeField] private AudioClip[] FootstepS;

    [Header("PUBG Realism Settings")]
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.1f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private AudioClip GetaudioFootsteps()
    {
        // Safety check to prevent crashing if you forgot to add sounds
        if (FootstepS.Length == 0) return null;
        return FootstepS[UnityEngine.Random.Range(0, FootstepS.Length)];
    }

    // This function is called by the Animation Event
    private void Step()
    {
        AudioClip clip = GetaudioFootsteps();
        if (clip != null)
        {
            // Randomize Pitch slightly (Makes it sound natural)
            audioSource.pitch = Random.Range(minPitch, maxPitch);

            // Randomize Volume slightly
            audioSource.volume = Random.Range(0.85f, 1.0f);

            audioSource.PlayOneShot(clip);
        }
    }
}