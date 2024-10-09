using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;
using WitchDoctor.Utils;
using WitchDoctor.CoreResources.Utils.Singleton;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Managers.InputManagement;
using UnityEngine.InputSystem;

namespace WitchDoctor.CoreResources.Managers.GeneralUtils
{
    public enum OneShotSounds
    {
        PositiveButtonClick = 0,
        NegativeButtonClick = 1,
        GameWin = 2,
        GameLoss = 3,
        Timer = 4,
    }

    public enum BackgroundSounds
    {
        None = 0,
        MainMenu = 1,
        BGM1 = 2,
    }

    public enum AmbientSounds
    {
        None = 0,
        Cave = 1,
        Rain = 2,
    }

    public class SoundManager : MonoSingleton<SoundManager>
    {
        [Header("Audio List"), SerializeField]
        private AudioList _audioList;

        [Header("Mixers"), SerializeField]
        private AudioMixer _primaryMixer;
        // [SerializeField]
        // private AudioMixer _bgmMixer;
        

        [Header("Audio Sources"), SerializeField]
        private AudioSource[] _oneShotSources = new AudioSource[2];
        [SerializeField]
        private AudioSource[] _bgmSources = new AudioSource[2];
        [SerializeField]
        private AudioSource[] _ambientSources = new AudioSource[2];

        private Dictionary<OneShotSounds, AudioClip> _oneShotSoundMap;
        private Dictionary<BackgroundSounds, AudioClip> _BGMSoundMap;
        private Dictionary<AmbientSounds, AudioClip> _AmbientSoundsMap;
        private BackgroundSounds _currentlyPlayingBgm = BackgroundSounds.None;
        private AudioSource _currentlyPlayingBGMSource = null;

        private float _currSfxVolume, _currBGVolume, _currMasterVolume, _currentAmbienceVolume;
        private float _oldSfxVolume, _oldBGVolume, _oldMasterVolume, _oldAmbienceVolume;
        private bool _audioInitialized = false;

        public float CurrentSfxVolume { get { return _currSfxVolume; } }
        public float CurrentBgVolume { get { return _currBGVolume; } }
        public float CurrentMasterVolume { get { return _currMasterVolume; } }
        public float CurrentAmbienceVolume { get { return _currentAmbienceVolume; } }

        //Default volume levels
        public const float DEFAULT_MASTER_VOLUME = 10f;
        public const float DEFAULT_SFX_VOLUME = 5f;
        public const float DEFAULT_BG_VOLUME = 5f;
        public const float DEFAULT_AMBIENCE_VOLUME = 5f;

        private const string BG_VOLUME_STR = "BgVolume";
        private const string SFX_VOLUME_STR = "SfxVolume";
        private const string MASTER_VOLUME_STR = "MasterVolume";
        private const string AMBIENCE_VOLUME_STR = "AmbienceVolume";

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            _audioInitialized = false;
            _currentlyPlayingBgm = BackgroundSounds.None;
            _currentlyPlayingBGMSource = null;

            _oneShotSoundMap = new Dictionary<OneShotSounds, AudioClip>()
            {
                { OneShotSounds.PositiveButtonClick, _audioList.PositiveButtonClick },
                { OneShotSounds.NegativeButtonClick, _audioList.NegativeButtonClick },
                { OneShotSounds.GameWin, _audioList.GameWin },
                { OneShotSounds.GameLoss, _audioList.GameLoss },
                { OneShotSounds.Timer, _audioList.Timer }
            };

            _BGMSoundMap = new Dictionary<BackgroundSounds, AudioClip>()
            {
                { BackgroundSounds.MainMenu, _audioList.MainMenu },
                { BackgroundSounds.BGM1, _audioList.BGM1 },
            };
        }

        public override void CleanSingleton()
        {
            StopBG();
            StopAllOneShots();

            base.CleanSingleton();
        }

        private void Start()
        {
            InitializeSoundSystems();
        }
        #endregion

        #region Update Functions
        public void UpdateBG(float newBGVolume)
        {
            var currentValue = _currBGVolume = newBGVolume;
            currentValue /= 10;
            if (currentValue <= 0)
                currentValue = 0.0001f;
            _primaryMixer.SetFloat("BGM", ConversionUtils.ConvertFloatToLog(currentValue));
        }

        public void UpdateSfx(float newSfxVolume)
        {
            var currentValue = _currSfxVolume = newSfxVolume;
            currentValue /= 10;
            if (currentValue <= 0)
                currentValue = 0.0001f;
            _primaryMixer.SetFloat("SFX", ConversionUtils.ConvertFloatToLog(currentValue));
        }

        public void UpdateMaster(float newMasterVolume)
        {
            var currentValue = _currMasterVolume = newMasterVolume;
            currentValue /= 10;
            if (currentValue <= 0)
                currentValue = 0.0001f;
            _primaryMixer.SetFloat("Master", ConversionUtils.ConvertFloatToLog(currentValue));
        }

        public void UpdateAmbience(float newAmbienceVolume)
        {
            var currentValue = _currentAmbienceVolume = newAmbienceVolume;
            currentValue /= 10;
            if (currentValue <= 0)
                currentValue = 0.0001f;
            _primaryMixer.SetFloat("Ambient", ConversionUtils.ConvertFloatToLog(currentValue));
        }
        #endregion

        #region Play / Stop Functions
        public void PlayBG(BackgroundSounds soundType)
        {
            if (!_audioInitialized || soundType == _currentlyPlayingBgm)
                return;

            AudioClip selClip;
            if (!_BGMSoundMap.TryGetValue(soundType, out selClip) || selClip == null)
            {
                throw new MissingReferenceException($"{soundType} does not have an associated sound clip or the sound clip is not assigned");
            }

            // at a time only one music can be played as BG
            if (!_bgmSources.Any((x) => x.isPlaying))
            {
                _currentlyPlayingBgm = soundType;
                PlayBGInternal(_bgmSources[0], selClip);
            }
            else
            {
                // Crossfade needs to happen here
                TransitionToNextBG(soundType);
            }

        }

        public void StopBG()
        {
            _currentlyPlayingBgm = BackgroundSounds.None;
            _currentlyPlayingBGMSource = null;
            foreach (var source in _bgmSources)
            {
                StopBGInternal(source);
            }
        }

        public void PlayOneShot(OneShotSounds soundType, bool shouldLoop = false)
        {
            AudioClip selClip;
            if (!_oneShotSoundMap.TryGetValue(soundType, out selClip) || selClip == null)
            {
                throw new MissingReferenceException($"{soundType} does not have an associated sound clip or the sound clip is not assigned");
            }

            var source = _oneShotSources.First(x => !x.isPlaying);

            if (source == null)
            {
                _oneShotSources[0].Stop();
                _oneShotSources[0].clip = null;
                source = _oneShotSources[0];
            }

            source.clip = selClip;
            source.loop = shouldLoop;
            source.Play();
        }

        public void StopAllOneShots()
        {
            foreach (var source in _oneShotSources)
            {
                source.Stop();
                source.clip = null;
            }
        }

        // Need to get access to the ambience track first
        public void PlayAmbientSound(string Event)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Internal Methods
        private void PlayBGInternal(AudioSource source, AudioClip clip)
        {
            if (source == null) return;

            _currentlyPlayingBGMSource = source;
            source.clip = clip;
            source.loop = true;
            source.Play();
        }

        private void StopBGInternal(AudioSource source)
        {
            source.Stop();
            source.clip = null;
        }

        private void TransitionToNextBG(BackgroundSounds soundType)
        {
            AudioClip selClip;
            string exposedParamNew = "";
            string exposedParamPrev = "";

            if (!_BGMSoundMap.TryGetValue(soundType, out selClip) || selClip == null)
            {
                throw new MissingReferenceException($"{soundType} does not have an associated sound clip or the sound clip is not assigned");
            }


            for (int i = 0; i < _bgmSources.Length; i++)
            {
                if (_bgmSources[i].isPlaying)
                {
                    if (i == 0)
                    {
                        exposedParamPrev = "BG1";
                        exposedParamNew = "BG2";
                    }
                    else
                    {
                        exposedParamPrev = "BG2";
                        exposedParamNew = "BG1";
                    }
                    break;
                }

            }

            _currentlyPlayingBgm = soundType;
            var prevSource = _currentlyPlayingBGMSource;

            _primaryMixer.DOSetFloat(exposedParamPrev, -80, 3).onComplete += () =>
            {
                StopBGInternal(prevSource);
                _primaryMixer.SetFloat(exposedParamPrev, 0);
            };

            _primaryMixer.SetFloat(exposedParamNew, -80);

            try
            {
                PlayBGInternal(_bgmSources.First((x) => !x.isPlaying), selClip);
            }
            catch
            {
                foreach (var source in _bgmSources)
                {
                    source.Stop();
                }
                PlayBGInternal(_bgmSources[0], selClip);
            }

            _primaryMixer.DOSetFloat(exposedParamNew, 0, 3);
        }
        #endregion

        #region Settings Utils
        private void InitializeSoundSystems()
        {
            if (!LoadSoundSettingsFromPlayerPrefs())
            {
                DefaultSoundSettings();
            }

            _audioInitialized = true;
            // PlayBG(BackgroundSounds.MainMenu);

            // InputManager.Player.Jump.performed += SampTransitionFunction;
        }

        //private void SampTransitionFunction(InputAction.CallbackContext obj)
        //{
        //    if (obj.performed)
        //    {
        //        BackgroundSounds soundToPlay = _currentlyPlayingBgm == BackgroundSounds.MainMenu ? BackgroundSounds.BGM1 : BackgroundSounds.MainMenu;
        //        PlayBG(soundToPlay);
        //    }
        //}

        public void SaveSoundSettingsToPlayerPrefs()
        {
            _oldBGVolume = _currBGVolume;
            PlayerPrefs.SetFloat(BG_VOLUME_STR, _oldBGVolume);
            _oldSfxVolume = _currSfxVolume;
            PlayerPrefs.SetFloat(SFX_VOLUME_STR, _oldSfxVolume);
            _oldMasterVolume = _currMasterVolume;
            PlayerPrefs.SetFloat(MASTER_VOLUME_STR, _oldMasterVolume);
            _oldAmbienceVolume = _currentAmbienceVolume;
            PlayerPrefs.SetFloat(AMBIENCE_VOLUME_STR, _oldAmbienceVolume);
        }

        public void DefaultSoundSettings()
        {
            //Bg volume changes
            _currBGVolume = DEFAULT_BG_VOLUME;
            _oldBGVolume = _currBGVolume;
            PlayerPrefs.SetFloat(BG_VOLUME_STR, _oldBGVolume);

            //Sfx volume changes
            _currSfxVolume = DEFAULT_SFX_VOLUME;
            _oldSfxVolume = _currSfxVolume;
            PlayerPrefs.SetFloat(SFX_VOLUME_STR, _oldSfxVolume);

            //Master volume changes
            _currMasterVolume = DEFAULT_MASTER_VOLUME;
            _oldMasterVolume = _currMasterVolume;
            PlayerPrefs.SetFloat(MASTER_VOLUME_STR, _oldMasterVolume);

            //Ambience volume changes
            _currentAmbienceVolume = DEFAULT_AMBIENCE_VOLUME;
            _oldAmbienceVolume = _currentAmbienceVolume;
            PlayerPrefs.SetFloat(AMBIENCE_VOLUME_STR, _oldAmbienceVolume);

            ApplySoundSettings();
        }

        public void ApplySoundSettings()
        {
            UpdateBG(_currBGVolume);
            UpdateSfx(_currSfxVolume);
            UpdateMaster(_currMasterVolume);
            UpdateAmbience(_currentAmbienceVolume);
        }

        public void DiscardSoundChanges()
        {
            _currBGVolume = _oldBGVolume;
            _currSfxVolume = _oldSfxVolume;
            _currMasterVolume = _oldMasterVolume;
            _currentAmbienceVolume = _oldAmbienceVolume;
        }

        public bool LoadSoundSettingsFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(BG_VOLUME_STR) ||
                !PlayerPrefs.HasKey(SFX_VOLUME_STR) ||
                !PlayerPrefs.HasKey(MASTER_VOLUME_STR) ||
                !PlayerPrefs.HasKey(AMBIENCE_VOLUME_STR))
                return false;

            _oldBGVolume = PlayerPrefs.GetFloat(BG_VOLUME_STR);
            _currBGVolume = _oldBGVolume;
            _oldSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_STR);
            _currSfxVolume = _oldSfxVolume;

            // Since these aren't updated by the user, we'll set default values here
            // _oldMasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_STR);
            _oldMasterVolume = DEFAULT_MASTER_VOLUME;
            _currMasterVolume = _oldMasterVolume;
            // _oldAmbienceVolume = PlayerPrefs.GetFloat(AMBIENCE_VOLUME_STR);
            _oldAmbienceVolume = DEFAULT_AMBIENCE_VOLUME;
            _currentAmbienceVolume = _oldAmbienceVolume;

            ApplySoundSettings();

            return true;
        }
        #endregion
    }
}