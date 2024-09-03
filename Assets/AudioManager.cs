using UnityEngine;

public class AudioManager : MonoBehaviour
{
	[Header("--------- Audio Source ---------")]
	[SerializeField] AudioSource musicSource;
	[SerializeField] AudioSource SFXSource;
	
	
	[Header("--------- Audio Source ---------")]
	public AudioClip background;
	public AudioClip DampCave;
	
	private void Start()
	{
		musicSource.clip = background;
        musicSource.loop = true;
        musicSource.Play();
	}
}
