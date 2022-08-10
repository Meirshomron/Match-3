using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
	private static AudioManager _instance;

	public static AudioManager Instance
	{
		get { return _instance; }
	}
	public float delayInCrossfading = 0.3f;

	public List<MusicTrack> tracks = new List<MusicTrack>();
	public List<Sound> sounds = new List<Sound>();

	private bool sfxMute;
	private bool musicMute;
	private AudioSource music;
	private AudioSource sfx;

	private Sound GetSoundByName(string sName) => sounds.Find(x => x.name == sName);

	private static readonly List<string> mixBuffer = new List<string>();
	private const float mixBufferClearDelay = 0.05f;

	internal string currentTrack;

	public float MusicVolume => !PlayerPrefs.HasKey("Music Volume") ? 1f : PlayerPrefs.GetFloat("Music Volume");

	public float SfxVolume => !PlayerPrefs.HasKey("SFX Volume") ? 1f : PlayerPrefs.GetFloat("SFX Volume");

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(this.gameObject);
			return;
		}
		_instance = this;

		// Configuring Audio Source For Playing Music And SFX
		music = gameObject.AddComponent<AudioSource>();
		music.loop = true;
		sfx = gameObject.AddComponent<AudioSource>();

		sfxMute = false;
		musicMute = false;

		// Check If Sfx Volume Is Not 0
		if (Math.Abs(SfxVolume) > 0.05f)
		{
			// Set The Saved Value Of SFX Volume
			sfx.volume = SfxVolume;
		}
		// Set The Values To 0
		else
		{
			sfx.volume = 0;
		}

		// Check If Music Volume Is Not 0
		if (Math.Abs(MusicVolume) > 0.05f)
		{
			// Set The Saved Value Of Music Volume
			music.volume = MusicVolume;
		}
		// Set The Values To 0
		else
		{
			music.volume = 0;
		}

		// Checks If The sfxMute Is True Or Not
		if (PlayerPrefs.GetInt("sfxMute") == 1)
		{
			SfxToggle();
		}
		// Checks If The musicMute Is True Or Not
		if (PlayerPrefs.GetInt("musicMute") == 1)
		{
			MusicToggle();
		}

		StartCoroutine(MixBufferRoutine());
	}

    private void Start()
    {
		print("AudioManager: Start");
		EventManager.StartListening(EventNames.ON_TOGGLE_MUSIC, OnToggleMusicEvent);
		EventManager.StartListening(EventNames.ON_TOGGLE_SFX, OnToggleSFXEvent);
	}

    // Responsible for limiting the frequency of playing sounds
    private IEnumerator MixBufferRoutine()
	{
		float time = 0;

		while (true)
		{
			time += Time.unscaledDeltaTime;
			yield return 0;
			if (time >= mixBufferClearDelay)
			{
				mixBuffer.Clear();
				time = 0;
			}
		}
	}

	// Play a music track with Cross fading
	public void PlayMusic(string trackName)
	{
		if (trackName != "")
			currentTrack = trackName;
		AudioClip to = null;
		foreach (MusicTrack track in tracks)
			if (track.name == trackName)
				to = track.track;

		StartCoroutine(CrossFade(to));
	}

	// Cross fading - Smooth Transition When Track Is Switched
	private IEnumerator CrossFade(AudioClip to)
	{
		if (music.clip != null)
		{
			while (delayInCrossfading > 0)
			{
				music.volume = delayInCrossfading * MusicVolume;
				delayInCrossfading -= Time.unscaledDeltaTime;
				yield return 0;
			}
		}
		music.clip = to;
		if (to == null)
		{
			music.Stop();
			yield break;
		}
		delayInCrossfading = 0;

		if (!music.isPlaying)
			music.Play();

		while (delayInCrossfading < 1f)
		{
			music.volume = delayInCrossfading * MusicVolume;
			delayInCrossfading += Time.unscaledDeltaTime;
			yield return 0;
		}
		music.volume = MusicVolume;
	}

	public void StopSound()
	{
		sfx.Stop();
	}

	private void OnToggleSFXEvent(string eventName, ActionParams _data)
	{
		SfxToggle();
	}

	private void OnToggleMusicEvent(string eventName, ActionParams _data)
	{
		MusicToggle();
	}

	// Sfx Button On/Off
	public void SfxToggle()
	{
		sfxMute = !sfxMute;
		sfx.mute = sfxMute;

		PlayerPrefs.SetInt("sfxMute", Utils.BoolToBinary(sfxMute));
		PlayerPrefs.Save();
	}

	// Music Button On/Off
	public void MusicToggle()
	{
		musicMute = !musicMute;
		music.mute = musicMute;

		PlayerPrefs.SetInt("musicMute", Utils.BoolToBinary(musicMute));
		PlayerPrefs.Save();
	}

	// A single sound effect
	public void PlaySound(string clip)
	{
		Sound sound = GetSoundByName(clip);

		if (sound != null && !mixBuffer.Contains(clip))
		{
			mixBuffer.Add(clip);
			sfx.PlayOneShot(sound.clip);
		}
	}

	// A single sound effect
	public void PlaySound(string clipName, AudioClip clip)
	{
		Sound sound = GetSoundByName(clipName);
		if (sound == null)
        {
			sound = new Sound();
			sound.clip = clip;
			sound.name = clipName;
			sounds.Add(sound);
		}
		
		if (!mixBuffer.Contains(clipName))
		{
			mixBuffer.Add(clipName);
			sfx.PlayOneShot(sound.clip);
		}
	}

	public bool IsMusicMute()
    {
		return musicMute;
	}

	public bool IsSFXMute()
	{
		return sfxMute;
	}

	[Serializable]
	public class MusicTrack
	{
		public string name;
		public AudioClip track;
	}

	[Serializable]
	public class Sound
	{
		public string name;
		public AudioClip clip;
	}
}
