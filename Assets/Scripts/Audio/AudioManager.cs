using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }
    private void Awake()
    {
        if(instance != null)
        {
            Debug.Log("There is more than one AudioManager.");
        }
        instance = this;
    }
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }
    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        return eventInstance;
    }
    public void PlayButtonClick()
    {
        if (FMODEvents.instance != null)
        {
            RuntimeManager.PlayOneShot(FMODEvents.instance.UIButtonClick, Vector3.zero);
        }
    }
}
