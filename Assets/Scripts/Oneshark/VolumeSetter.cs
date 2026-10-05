//using UnityEngine;
//using System.Collections;

//public class VolumeSetter : MonoBehaviour
//{
    
//    AudioSource[] audioSrc;
//    public bool isSoundEffect = true;
//    void Start()
//    {
//        audioSrc = GetComponents<AudioSource>();
//        //Invoke("setVolumes", .1f);
//            setVolumes();
//    }
    

//    void OnEnable()
//    {
//        EventManager.OnAudioLevelChanged += OnAudioLevelChanged;
//        setVolumes();
//    }

//    void OnDisable()
//    {
//        EventManager.OnAudioLevelChanged -= OnAudioLevelChanged;
//    }

//    void setVolumes()
//    {
//        if (audioSrc == null) audioSrc = GetComponents<AudioSource>();

//        if (isSoundEffect)
//        {
           
//                foreach (AudioSource _audio in audioSrc)
//                {
//                    _audio.volume = AudioManager.single.getSFXVolume();
//                }
//        }else
//        {
//            foreach (AudioSource _audio in audioSrc)
//            {
//                _audio.volume = AudioManager.single.getBGVolume();
//            }

//        }
      
//    }



//    public void OnAudioLevelChanged()
//    {
//       // Debug.Log("Audio Level Chagned, fix all volumes");
//        setVolumes();
//    }
//}
