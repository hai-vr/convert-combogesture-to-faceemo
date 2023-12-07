using System;
using System.Linq;
using ConvertComboGestureToFaceEmo.Runtime;
using Hai.ComboGesture.Scripts.Components;
using Suzuryg.FaceEmo.AppMain;
using Suzuryg.FaceEmo.Detail.View;
using Suzuryg.FaceEmo.Domain;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Animation = Suzuryg.FaceEmo.Domain.Animation;
using Menu = Suzuryg.FaceEmo.Domain.Menu;

namespace ConvertComboGestureToFaceEmo.Editor
{
    [CustomEditor(typeof(ComboGestureToFaceEmoConverter))]
    public class ComboGestureToFaceEmoConverterEditor : UnityEditor.Editor
    {
        private const int ItemLimit = 7;

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

                    ConvertActivity(menu, modeId, mapper.activity, my.ignoreFistTriggers);
                }

                if (mapper.kind == GestureComboStageKind.Puppet)
                {
                    if (mapper.puppet == null) continue;

                    var modeId = menu.AddMode(cgeGroup);
                    menu.ModifyModeProperties(modeId, displayName: mapper.puppet.name);

                    ConvertPuppet(menu, modeId, mapper.puppet);
                }

                if (index % ItemLimit == ItemLimit - 1 && index != total - 1
                                                       && index != total - 2 // If the last sub-group would only contain one item, don't create it
                                                       )
                {
                    var newSubGroup = menu.AddGroup(cgeGroup);
                    menu.ModifyGroupProperties(newSubGroup, displayName: "...");
                    cgeGroup = newSubGroup;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            menuRepository.Save(string.Empty, menu, "Convert ComboGesture to FaceEmo");
            
            var existingWindows = Resources.FindObjectsOfTypeAll<MainWindow>();
            if (existingWindows.Any())
            {
                existingWindows.First().UpdateMenu(menu, true);
            }
        }

        private void ConvertActivity(Menu menu, string modeId, ComboGestureActivity activity, bool ignoreFist)
        {
            switch (activity.activityMode)
            {
                case ComboGestureActivity.CgeActivityMode.Combos:
                    ConvertActivityCombos(menu, modeId, activity, ignoreFist);
                    break;
                case ComboGestureActivity.CgeActivityMode.Permutations:
                    ConvertActivityPermutations(menu, modeId, activity, ignoreFist);
                    break;
                case ComboGestureActivity.CgeActivityMode.LeftHandOnly:
                    ConvertActivitySpecificHand(menu, modeId, activity, Hand.Left, ignoreFist);
                    break;
                case ComboGestureActivity.CgeActivityMode.RightHandOnly:
                    ConvertActivitySpecificHand(menu, modeId, activity, Hand.Right, ignoreFist);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ConvertPuppet(Menu menu, string modeId, ComboGesturePuppet puppet)
        {
            // menu.AddBranch(modeId);

            var animation = Anim(puppet.mainTree);
            
            var bothEyesClosed = puppet.blinking.Contains(animation);
            // menu.ModifyBranchProperties(modeId, currentBranchIndex, blinkEnabled: bothEyesClosed);

            if (animation != null)
            {
                // menu.SetAnimation(AsDomainAnimation(animation), modeId, currentBranchIndex, BranchAnimationType.Base);
                menu.ModifyModeProperties(modeId, changeDefaultFace: true, blinkEnabled: !bothEyesClosed);
                menu.SetAnimation(AsDomainAnimation(animation), modeId);
            }
        }

        private void ConvertActivitySpecificHand(Menu menu, string modeId, ComboGestureActivity activity, Hand side, bool ignoreFist)
        {
            var branchIndex = 0;
            
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim00), 00, side);
            Specific(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim01 : activity.anim00), 01, side, universalTrigger: ignoreFist ? null : Anim(activity.anim01));
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim02), 02, side);
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim03), 03, side);
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim04), 04, side);
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim05), 05, side);
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim06), 06, side);
            Specific(menu, modeId, activity, ref branchIndex, Anim(activity.anim07), 07, side);
        }

        private void ConvertActivityCombos(Menu menu, string modeId, ComboGestureActivity activity, bool ignoreFist)
        {
            var branchIndex = 0;
            
            // Where we're going we don't need foreach
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim00), 00);
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim01 : activity.anim00), 01, universalTrigger: ignoreFist ? null : Anim(activity.anim01));
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim02), 02);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim03), 03);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim04), 04);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim05), 05);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim06), 06);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim07), 07);
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim11 : activity.anim00), 11, leftTrigger: ignoreFist ? null : Anim(activity.anim11_L), rightTriggerNullable: ignoreFist ? null : Anim(activity.anim11_R), universalTrigger: ignoreFist ? null : Anim(activity.anim11));
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim12 : activity.anim00), 12, universalTrigger: ignoreFist ? null : Anim(activity.anim12));
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim13 : activity.anim00), 13, universalTrigger: ignoreFist ? null : Anim(activity.anim13));
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim14 : activity.anim00), 14, universalTrigger: ignoreFist ? null : Anim(activity.anim14));
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim15 : activity.anim00), 15, universalTrigger: ignoreFist ? null : Anim(activity.anim15));
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim16 : activity.anim00), 16, universalTrigger: ignoreFist ? null : Anim(activity.anim16));
            Combo(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim17 : activity.anim00), 17, universalTrigger: ignoreFist ? null : Anim(activity.anim17));
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim22), 22);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim23), 23);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim24), 24);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim25), 25);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim26), 26);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim27), 27);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim33), 33);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim34), 34);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim35), 35);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim36), 36);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim37), 37);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim44), 44);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim45), 45);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim46), 46);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim47), 47);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim55), 55);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim56), 56);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim57), 57);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim66), 66);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim67), 67);
            Combo(menu, modeId, activity, ref branchIndex, Anim(activity.anim77), 77);
        }

        private void ConvertActivityPermutations(Menu menu, string modeId, ComboGestureActivity activity, bool ignoreFist)
        {
            var branchIndex = 0;
            
            // Where we're going we don't need foreach
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim00), 00);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim01 : activity.anim00), 01, rightTrigger: ignoreFist ? null : Anim(activity.anim01));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim02), 02);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim03), 03);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim04), 04);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim05), 05);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim06), 06);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim07), 07);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim11 : activity.anim00), 11, leftTrigger: ignoreFist ? null : Anim(activity.anim11_L), rightTrigger: ignoreFist ? null : Anim(activity.anim11_R), universalTrigger: ignoreFist ? null : Anim(activity.anim11));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim12 : activity.anim00), 12, leftTrigger: ignoreFist ? null : Anim(activity.anim12));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim13 : activity.anim00), 13, leftTrigger: ignoreFist ? null : Anim(activity.anim13));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim14 : activity.anim00), 14, leftTrigger: ignoreFist ? null : Anim(activity.anim14));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim15 : activity.anim00), 15, leftTrigger: ignoreFist ? null : Anim(activity.anim15));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim16 : activity.anim00), 16, leftTrigger: ignoreFist ? null : Anim(activity.anim16));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(ignoreFist ? activity.anim17 : activity.anim00), 17, leftTrigger: ignoreFist ? null : Anim(activity.anim17));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim22), 22);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim23), 23);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim24), 24);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim25), 25);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim26), 26);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim27), 27);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim33), 33);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim34), 34);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim35), 35);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim36), 36);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim37), 37);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim44), 44);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim45), 45);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim46), 46);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim47), 47);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim55), 55);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim56), 56);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim57), 57);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim66), 66);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim67), 67);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim77), 77);
            //
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim10) ?? Anim(activity.anim01) : Anim(activity.anim00), 10, leftTrigger: ignoreFist ? null : Anim(activity.anim10) ?? Anim(activity.anim01));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim20) ?? Anim(activity.anim02), 20);
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim21) ?? Anim(activity.anim12) : Anim(activity.anim00), 21, rightTrigger: ignoreFist ? null : Anim(activity.anim21) ?? Anim(activity.anim12));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim30) ?? Anim(activity.anim03), 30);
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim31) ?? Anim(activity.anim13) : Anim(activity.anim00), 31, rightTrigger: ignoreFist ? null : Anim(activity.anim31) ?? Anim(activity.anim13));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim32) ?? Anim(activity.anim23), 32);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim40) ?? Anim(activity.anim04), 40);
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim41) ?? Anim(activity.anim14) : Anim(activity.anim00), 41, rightTrigger: ignoreFist ? null : Anim(activity.anim41) ?? Anim(activity.anim14));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim42) ?? Anim(activity.anim24), 42);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim43) ?? Anim(activity.anim34), 43);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim50) ?? Anim(activity.anim05), 50);
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim51) ?? Anim(activity.anim15) : Anim(activity.anim00), 51, rightTrigger: ignoreFist ? null : Anim(activity.anim51) ?? Anim(activity.anim15));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim52) ?? Anim(activity.anim25), 52);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim53) ?? Anim(activity.anim35), 53);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim54) ?? Anim(activity.anim45), 54);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim60) ?? Anim(activity.anim06), 60);
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim61) ?? Anim(activity.anim16) : Anim(activity.anim00), 61, rightTrigger: ignoreFist ? null : Anim(activity.anim61) ?? Anim(activity.anim16));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim62) ?? Anim(activity.anim26), 62);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim63) ?? Anim(activity.anim36), 63);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim64) ?? Anim(activity.anim46), 64);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim65) ?? Anim(activity.anim56), 65);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim70) ?? Anim(activity.anim07), 70);
            Permutation(menu, modeId, activity, ref branchIndex, ignoreFist ? Anim(activity.anim71) ?? Anim(activity.anim17) : Anim(activity.anim00), 71, rightTrigger: ignoreFist ? null : Anim(activity.anim71) ?? Anim(activity.anim17));
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim72) ?? Anim(activity.anim27), 72);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim73) ?? Anim(activity.anim37), 73);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim74) ?? Anim(activity.anim47), 74);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim75) ?? Anim(activity.anim57), 75);
            Permutation(menu, modeId, activity, ref branchIndex, Anim(activity.anim76) ?? Anim(activity.anim67), 76);
        }

        private AnimationClip Anim(Motion motionNullable)
        {
            if (motionNullable == null) return null;
            if (motionNullable is AnimationClip anim)
            {
                return anim;
            }

            if (motionNullable is BlendTree bt)
            {
                if (bt.children.Length == 0) return null;
                
                // As far as I know, Puppets are not supported in FaceEmo. Try to get the most sensible animation from that blend tree.
                
                var is2d = bt.blendType != BlendTreeType.Direct && bt.blendType != BlendTreeType.Simple1D;
                if (is2d)
                {
                    // Get the anim at the origin of the blend tree.
                    var motionsAtZero = bt.children.Where(motion => motion.position == Vector2.zero).ToArray();
                    if (motionsAtZero.Length > 0) return Anim(motionsAtZero[0].motion);
                    
                    return Anim(bt.children[0].motion);
                }
                else
                {
                    // Get the anim at the rightmost of the blend tree.
                    return Anim(bt.children[bt.children.Length - 1].motion);
                }
            }

            return null;
        }

        private static void Combo(Menu menu, string modeId, ComboGestureActivity activity, ref int branchIndex,
            AnimationClip animation, int code, AnimationClip leftTrigger = null, AnimationClip rightTriggerNullable = null, AnimationClip universalTrigger = null)
        {
            if (animation == null && universalTrigger == null) return;

            var left = (HandGesture)(code / 10);
            var right = (HandGesture)(code % 10);
            if (universalTrigger != null && left != right)
            {
                var oppositeCode = OppositeCode(code);
                Permutation(menu, modeId, activity, ref branchIndex, animation, code < 10 ? oppositeCode : code, leftTrigger: universalTrigger);
                Permutation(menu, modeId, activity, ref branchIndex, animation, code < 10 ? code : oppositeCode, rightTrigger: universalTrigger);
                return;
            }
            
            Build(menu, modeId, activity, ref branchIndex, animation, leftTrigger, rightTriggerNullable, universalTrigger, left, right, true);
        }

        private static int OppositeCode(int code)
        {
            var l = code / 10;
            var r = code % 10;
            var oppositeCode = l + r * 10;
            return oppositeCode;
        }

        private static void Permutation(Menu menu, string modeId, ComboGestureActivity activity, ref int branchIndex,
            AnimationClip animation, int code, AnimationClip leftTrigger = null, AnimationClip rightTrigger = null, AnimationClip universalTrigger = null)
        {
            if (animation == null && universalTrigger == null) return;
            
            var left = (HandGesture)(code / 10);
            var right = (HandGesture)(code % 10);
            
            Build(menu, modeId, activity, ref branchIndex, animation, leftTrigger, rightTrigger, universalTrigger, left, right, false);
        }

        private static void Specific(Menu menu, string modeId, ComboGestureActivity activity, ref int branchIndex,
            AnimationClip animation, int code, Hand side, AnimationClip universalTrigger = null)
        {
            if (animation == null && universalTrigger == null) return;
            
            var universal = (HandGesture)(code % 10);
            
            Build(menu, modeId, activity, ref branchIndex, animation, side == Hand.Left ? universalTrigger : null, side == Hand.Right ? universalTrigger : null, null, side == Hand.Left ? universal : HandGesture.Neutral, side == Hand.Right ? universal : HandGesture.Neutral, false, true, side);
        }

        private static void Build(Menu menu, string modeId, ComboGestureActivity activity, ref int branchIndex,
            AnimationClip animation, AnimationClip leftTriggerNullable, AnimationClip rightTriggerNullable, AnimationClip universalTrigger,
            HandGesture left, HandGesture right, bool isNonSided, bool isSpecificHand = false, Hand specificSide = Hand.Either)
        {
            var currentBranchIndex = branchIndex;

            menu.AddBranch(modeId);
            branchIndex++;

            // As far as I know, in FaceEmo blinking, prevention is part of the branch, not the animation itself,
            // so when it comes to Analog Fist animations, ComboGesture's blinking metadata cannot be cleanly carried over to FaceEmo.
            var bothEyesClosed = activity.blinking.Contains(animation);
            menu.ModifyBranchProperties(modeId, currentBranchIndex, blinkEnabled: !bothEyesClosed,
                isLeftTriggerUsed: leftTriggerNullable != null || universalTrigger != null,
                isRightTriggerUsed: rightTriggerNullable != null || universalTrigger != null);

            if (animation != null)
            {
                menu.SetAnimation(AsDomainAnimation(animation), modeId, currentBranchIndex, BranchAnimationType.Base);
            }

            if (leftTriggerNullable != null)
            {
                menu.SetAnimation(AsDomainAnimation(leftTriggerNullable), modeId, currentBranchIndex, BranchAnimationType.Left);
            }

            if (rightTriggerNullable != null)
            {
                menu.SetAnimation(AsDomainAnimation(rightTriggerNullable), modeId, currentBranchIndex,
                    BranchAnimationType.Right);
            }

            if (universalTrigger != null)
            {
                menu.SetAnimation(AsDomainAnimation(universalTrigger), modeId, currentBranchIndex, BranchAnimationType.Both);
            }

            if (left == right)
            {
                var equalCondition = isSpecificHand ? specificSide : Hand.Both;
                menu.AddCondition(modeId, currentBranchIndex, new Condition(equalCondition, left, ComparisonOperator.Equals));
                if (left == 0 && animation != null)
                {
                    menu.ModifyModeProperties(modeId, changeDefaultFace: true, blinkEnabled: !bothEyesClosed);
                    menu.SetAnimation(AsDomainAnimation(animation), modeId);
                }
            }
            else
            {
                if (isSpecificHand)
                {
                    menu.AddCondition(modeId, currentBranchIndex, new Condition(specificSide, specificSide == Hand.Left ? left : right, ComparisonOperator.Equals));
                }
                else
                {
                    menu.AddCondition(modeId, currentBranchIndex, new Condition(isNonSided ? Hand.OneSide : Hand.Left, left, ComparisonOperator.Equals));
                    menu.AddCondition(modeId, currentBranchIndex, new Condition(isNonSided ? Hand.OneSide : Hand.Right, right, ComparisonOperator.Equals));
                }
            }
        }

        private static Animation AsDomainAnimation(Motion animation)
        {
            return new Animation(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(animation)));
        }
    }
}