using UnityEngine;

namespace WitchDoctor.GameResources.Utils.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Audio List", menuName = "Scriptable Objects/Audio List", order = 1)]
    public class AudioList : ScriptableObject
    {
        [Header("One Shot Sounds")]
        public AudioClip PositiveButtonClick;
        public AudioClip NegativeButtonClick,
            GameWin,
            GameLoss,
            Timer,
            SwordSlash,
            SwordSlashMagic,
            WalkWetStone,
            WalkGravel,
            WalkGrass;

        [Header("Background Sounds")]
        public AudioClip MainMenu;
        public AudioClip BGM1;

        [Header("Ambient Sounds")]
        public AudioClip None;
        public AudioClip Cave;
        public AudioClip Rain;
    }
}