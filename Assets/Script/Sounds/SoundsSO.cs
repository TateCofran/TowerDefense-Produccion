using UnityEngine;

namespace Assets.Script.Sounds
{
    [CreateAssetMenu(menuName = "Sounds/Sounds SO", fileName = "Sounds SO")]
    public class SoundsSO : ScriptableObject
    {
        public SoundList[] sounds;
    }
}
