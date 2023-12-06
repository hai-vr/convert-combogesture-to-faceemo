using Hai.ComboGesture.Scripts.Components;
using Suzuryg.FaceEmo.Components;
using UnityEngine;

namespace ConvertComboGestureToFaceEmo.Runtime
{
    public class ComboGestureToFaceEmoConverter : MonoBehaviour
    {
        public ComboGestureCompiler comboGestureCompiler;
        public FaceEmoLauncherComponent faceEmoLauncher;
        public bool ignoreFistTriggers;
    }
}