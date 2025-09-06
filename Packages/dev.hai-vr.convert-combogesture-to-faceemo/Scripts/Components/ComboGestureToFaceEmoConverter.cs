using Hai.ComboGesture.Scripts.Components;
using Suzuryg.FaceEmo.Components;
using UnityEngine;

namespace ConvertComboGestureToFaceEmo.Runtime
{
    [AddComponentMenu("Haï/ComboGesture To FaceEmo Converter")]
    [HelpURL("https://docs.hai-vr.dev/docs/products/combo-gesture-expressions/convert-to-faceemo")]
    public class ComboGestureToFaceEmoConverter : MonoBehaviour
    {
        public ComboGestureCompiler comboGestureCompiler;
        public FaceEmoLauncherComponent faceEmoLauncher;
        public bool ignoreFistTriggers;
    }
}