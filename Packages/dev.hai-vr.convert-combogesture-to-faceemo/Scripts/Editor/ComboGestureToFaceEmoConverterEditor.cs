using System;
using System.Linq;
using System.Reflection;
using ConvertComboGestureToFaceEmo.Runtime;
using Hai.ComboGesture.Scripts.Components;
using Suzuryg.FaceEmo.AppMain;
using Suzuryg.FaceEmo.Detail.View;
using Suzuryg.FaceEmo.Domain;
using UniRx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ConvertComboGestureToFaceEmo.Editor
{
    [CustomEditor(typeof(ComboGestureToFaceEmoConverter))]
    public class ComboGestureToFaceEmoConverterEditor : UnityEditor.Editor
    {
        private const int ItemLimit = 8;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Convert"))
            {
                var my = (ComboGestureToFaceEmoConverter)target;
                Convert(my);
            }
        }

        private void Convert(ComboGestureToFaceEmoConverter my)
        {
            var installer = new FaceEmoInstaller(my.faceEmoLauncher.gameObject);
            var menuRepository = installer.Container.Resolve<IMenuRepository>();
            var menu = menuRepository.Load(string.Empty);

            var cgeGroup = menu.AddGroup(Suzuryg.FaceEmo.Domain.Menu.RegisteredId);
            menu.ModifyGroupProperties(cgeGroup, displayName: "CGE");
            
            var total = my.comboGestureCompiler.comboLayers.Count;
            for (var index = 0; index < total; index++)
            {
                var mapper = my.comboGestureCompiler.comboLayers[index];
                if (mapper.kind == GestureComboStageKind.Activity)
                {
                    if (mapper.activity == null) continue;

                    var modeId = menu.AddMode(cgeGroup);
                    menu.ModifyModeProperties(modeId, displayName: mapper.activity.name);
                    Debug.Log($"ModifyModeProperties: {mapper.activity.name}");
                }
                // TODO: Puppets

                if (index % ItemLimit == ItemLimit - 1 && index != total - 1)
                {
                    var newSubGroup = menu.AddGroup(cgeGroup);
                    menu.ModifyGroupProperties(newSubGroup, displayName: "...");
                    cgeGroup = newSubGroup;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            menuRepository.Save(string.Empty, menu, "Convert ComboGesture to FaceEmo");

            // var inspectorView = installer.Container.Resolve<InspectorView>();
            // var onMenuUpdatedObservable = inspectorView.OnMenuUpdated;
            // __BreachObservable(onMenuUpdatedObservable).OnNext((menu, isModified: true));
            
            var existingWindows = Resources.FindObjectsOfTypeAll<MainWindow>();
            if (existingWindows.Any())
            {
                existingWindows.First().UpdateMenu(menu, true);
            }
        }

        private static Subject<(IMenu menu, bool isModified)> __BreachObservable(IObservable<(IMenu menu, bool isModified)> onMenuUpdatedObservable)
        {
            var sourceField = onMenuUpdatedObservable.GetType()
                .GetField("source", BindingFlags.Instance | BindingFlags.NonPublic);
            var __breachObservable__OnMenuUpdated =
                sourceField.GetValue(onMenuUpdatedObservable) as Subject<(IMenu menu, bool isModified)>;
            return __breachObservable__OnMenuUpdated;
        }
    }
}