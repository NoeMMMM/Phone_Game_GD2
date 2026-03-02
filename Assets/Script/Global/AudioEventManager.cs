using UnityEngine;

public class AudioEventManager : MonoBehaviour
{
   [SerializeField] private AudioEventDispatcher audioEventDispatcher;
   [SerializeField] private AudioSource audioSource;


   private void OnEnable()
   {
      audioEventDispatcher.OnAudioEvent += PlayAudioFX;
   }

   private void OnDisable()
   {
      audioEventDispatcher.OnAudioEvent -= PlayAudioFX;
   }
   private void PlayAudioFX(AudioClip clip)
   {
      audioSource.Stop();
      audioSource.clip = clip;
      audioSource.Play();
   }
}
