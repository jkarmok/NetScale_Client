using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationSystem.Unity.Editor
{
    public sealed class AnimationSystemDebugWindow : EditorWindow
    {
        [MenuItem("AnimationSystem/Debug Window", priority = 10)]
        public static void Open() => GetWindow<AnimationSystemDebugWindow>("AS Debug");

        private AnimatorComponent _target;
        private Vector2 _scrollLayers;
        private Vector2 _scrollBones;
        private Vector2 _scrollMapping;
        private string _previewClipName = "";
        private double _lastRepaintTime;
        private const double RepaintInterval = 0.05;

        private enum Tab { Live, Mapping, Network }
        private Tab _tab = Tab.Live;

        private bool _showLayers = true;
        private bool _showIK = false;
        private bool _showBones = true;

        private void Update()
        {
            if (EditorApplication.isPlaying &&
                EditorApplication.timeSinceStartup - _lastRepaintTime > RepaintInterval)
            {
                _lastRepaintTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("AnimationSystem Debugger", EditorStyles.boldLabel);

            DrawTargetSelection();

            if (_target == null)
            {
                EditorGUILayout.HelpBox("Select GameObject with AnimatorComponent", MessageType.Info);
                return;
            }

            if (!_target.IsInitialized)
            {
                DrawInitializationSection();
                return;
            }

            DrawTabs();
        }

        private void DrawTargetSelection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _target = (AnimatorComponent)EditorGUILayout.ObjectField(
                    "Target", _target, typeof(AnimatorComponent), true);
                
                if (GUILayout.Button("Selection", GUILayout.Width(70)))
                {
                    var go = Selection.activeGameObject;
                    if (go) _target = go.GetComponent<AnimatorComponent>();
                }
            }
        }

        private void DrawInitializationSection()
        {
            EditorGUILayout.HelpBox(
                "AnimatorComponent not initialized. Enter Play Mode or initialize manually.",
                MessageType.Warning);
            
            if (GUILayout.Button("Initialize (Edit Mode)"))
                _target.Initialize();
            
            if (_target.IsInitialized)
                DrawMappingDiagnostic();
        }

        private void DrawTabs()
        {
            _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Live State", "Bone Mapping", "Network" });
            EditorGUILayout.Space(4);

            switch (_tab)
            {
                case Tab.Live: DrawLiveTab(); break;
                case Tab.Mapping: DrawMappingDiagnostic(); break;
                case Tab.Network: DrawNetworkTab(); break;
            }
        }

        private void DrawLiveTab()
        {
            DrawPreviewControls();
            DrawServerInfo();
            EditorGUILayout.Space(4);

            _showLayers = EditorGUILayout.BeginFoldoutHeaderGroup(_showLayers, 
                $"Layers ({_target.Controller.LayerCount})");
            if (_showLayers) DrawLayersSection();
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showIK = EditorGUILayout.BeginFoldoutHeaderGroup(_showIK, "IK Chains");
            if (_showIK) DrawIKSection();
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showBones = EditorGUILayout.BeginFoldoutHeaderGroup(_showBones, "Bone Pose (local)");
            if (_showBones) DrawBonesSection();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPreviewControls()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                _previewClipName = EditorGUILayout.TextField(_previewClipName, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("▶ Play", GUILayout.Width(55)) && !string.IsNullOrEmpty(_previewClipName))
                    _target.Play(_previewClipName);
                
                if (GUILayout.Button("Fade", GUILayout.Width(44)) && !string.IsNullOrEmpty(_previewClipName))
                    _target.Play(_previewClipName, 0.25f);
            }
        }

        private void DrawServerInfo()
        {
            var controller = _target.Controller;
            EditorGUILayout.LabelField(
                $"Server Time: {controller.ServerTime:F3}s",
                EditorStyles.miniLabel);
        }

        private void DrawLayersSection()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("(Play Mode only)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollLayers, GUILayout.Height(130));
            _scrollLayers = scroll.scrollPosition;

            for (int i = 0; i < _target.Controller.LayerCount; i++)
            {
                DrawLayer(i);
            }
        }

        private void DrawLayer(int index)
        {
            var layer = _target.Controller.GetLayer(index);
            var state = layer.CurrentState;

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Layer {index}", EditorStyles.boldLabel, GUILayout.Width(55));
                
                EditorGUI.BeginChangeCheck();
                float w = EditorGUILayout.Slider(layer.Weight, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    _target.Controller.SetLayerWeight(index, w);
            }

            if (state == null)
            {
                EditorGUILayout.LabelField("(no state)", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField(
                $"{state.Clip.Name}  id={state.Clip.Id}  loop={state.Clip.IsLooping}",
                EditorStyles.miniLabel);

            var r = EditorGUILayout.GetControlRect(false, 14f);
            EditorGUI.ProgressBar(r, state.NormalizedTime,
                $"{state.Time:F3}s / {state.Clip.Duration:F3}s");

            if (layer.IsInTransition)
            {
                EditorGUILayout.LabelField(
                    $"→ Transition {layer.TransitionProgress * 100f:F0}%",
                    EditorStyles.boldLabel);
            }
        }

        private void DrawIKSection()
        {
            var skeleton = _target.Controller?.Skeleton;
            if (skeleton == null) return;

            var ikChains = _target.SkeletonAsset?.ikChains;
            if (ikChains == null || ikChains.Count == 0)
            {
                EditorGUILayout.LabelField("No IK chains defined", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            foreach (var chain in ikChains)
            {
                using var row = new EditorGUILayout.HorizontalScope(EditorStyles.helpBox);
                EditorGUILayout.LabelField(chain.chainName, GUILayout.Width(110));
                EditorGUILayout.LabelField(chain.chainType.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(string.Join(" → ", chain.boneIndices));
            }
        }

        private void DrawBonesSection()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("(Play Mode only)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var pose = _target.Controller.GetCurrentPose();
            var skel = _target.Controller.Skeleton;

            DrawBoneHeader();

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollBones, GUILayout.Height(220));
            _scrollBones = scroll.scrollPosition;

            for (int i = 0; i < pose.BoneCount; i++)
            {
                DrawBoneRow(i, pose, skel);
            }
        }

        private void DrawBoneHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Idx", GUILayout.Width(28));
                GUILayout.Label("Bone", GUILayout.Width(120));
                GUILayout.Label("Position (local)", GUILayout.Width(185));
                GUILayout.Label("Rotation Euler", GUILayout.ExpandWidth(true));
            }
        }

        private void DrawBoneRow(int index,Skeleton.SkeletonPose pose, 
            Skeleton.SkeletonDefinition skel)
        {
            ref readonly var loc = ref pose.Local[index];
            var pos = SkeletonAsset.ToUnityVec3(loc.Position);
            var rot = SkeletonAsset.ToUnityQuat(loc.Rotation).eulerAngles;

            var bindPos = SkeletonAsset.ToUnityVec3(skel.GetBone(index).BindPose.Position);
            bool isAnimated = Vector3.Distance(pos, bindPos) > 0.0001f ||
                              SkeletonAsset.ToUnityQuat(loc.Rotation) !=
                              SkeletonAsset.ToUnityQuat(skel.GetBone(index).BindPose.Rotation);

            var style = isAnimated ? EditorStyles.boldLabel : EditorStyles.label;

            using var row = new EditorGUILayout.HorizontalScope(
                index % 2 == 0 ? GUI.skin.box : GUIStyle.none);

            EditorGUILayout.LabelField(index.ToString(), style, GUILayout.Width(28));
            EditorGUILayout.LabelField(skel.GetBone(index).Name, style, GUILayout.Width(120));
            EditorGUILayout.LabelField($"({pos.x:F3}, {pos.y:F3}, {pos.z:F3})", 
                style, GUILayout.Width(185));
            EditorGUILayout.LabelField($"({rot.x:F1}°, {rot.y:F1}°, {rot.z:F1}°)", style);
        }

        private void DrawMappingDiagnostic()
        {
            EditorGUILayout.LabelField("Bone → Transform Mapping", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Verify that each skeleton bone is correctly mapped to a Transform.\n" +
                "Bones with empty Path (root) are skipped - this is normal.\n" +
                "Bones with NULL Transform indicate mapping errors - check transformPath in SkeletonAsset.",
                MessageType.Info);

            if (_target.SkeletonAsset == null)
            {
                EditorGUILayout.HelpBox("SkeletonAsset not assigned!", MessageType.Error);
                return;
            }

            DrawMappingHeader();

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollMapping, GUILayout.Height(300));
            _scrollMapping = scroll.scrollPosition;

            for (int i = 0; i < _target.SkeletonAsset.bones.Count; i++)
            {
                DrawMappingRow(i);
            }
        }

        private void DrawMappingHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Idx", GUILayout.Width(32));
                GUILayout.Label("Bone Name", GUILayout.Width(130));
                GUILayout.Label("Transform Path", GUILayout.Width(180));
                GUILayout.Label("Resolved Transform", GUILayout.ExpandWidth(true));
                GUILayout.Label("Status", GUILayout.Width(80));
            }
        }

        private void DrawMappingRow(int index)
        {
            var entry = _target.SkeletonAsset.bones[index];
            bool isRoot = string.IsNullOrEmpty(entry.transformPath);

            var (resolved, status, statusColor) = ResolveBoneMapping(entry, isRoot);

            using var row = new EditorGUILayout.HorizontalScope(
                index % 2 == 0 ? GUI.skin.box : GUIStyle.none);

            EditorGUILayout.LabelField(entry.boneIndex.ToString(), GUILayout.Width(32));
            EditorGUILayout.LabelField(entry.boneName, GUILayout.Width(130));
            EditorGUILayout.LabelField(
                isRoot ? "(empty = root)" : entry.transformPath,
                EditorStyles.miniLabel, GUILayout.Width(180));

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(resolved, typeof(Transform), true, GUILayout.ExpandWidth(true));

            var oldColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            EditorGUILayout.LabelField(status, EditorStyles.boldLabel, GUILayout.Width(80));
            GUI.contentColor = oldColor;
        }

        private (Transform resolved, string status, Color color) ResolveBoneMapping(
            SkeletonAsset.BoneEntry entry, bool isRoot)
        {
            Transform resolved = null;
            string status = "—";
            Color statusColor = Color.grey;

            if (isRoot)
            {
                resolved = _target.transform;
                status = "ROOT (skip)";
                statusColor = Color.yellow;
            }
            else
            {
                resolved = _target.transform.Find(entry.transformPath);
                if (resolved != null)
                {
                    status = "OK";
                    statusColor = Color.green;
                }
                else
                {
                    status = "NOT FOUND";
                    statusColor = Color.red;
                }
            }

            return (resolved, status, statusColor);
        }

        private void DrawNetworkTab()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("(Play Mode only)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            try
            {
                var state = _target.Controller.GetNetworkState();
                byte[] packet = AnimationSystem.Serialization.NetworkStateSerializer.Serialize(state);
                int fullSize = _target.Controller.Skeleton.BoneCount * 40;

                DrawPacketStats(packet, fullSize, state);
                EditorGUILayout.Space(4);
                DrawLayerDetails(state);
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
            }
        }

        private void DrawPacketStats(byte[] packet, int fullSize, 
            AnimationSystem.Controller.AnimationNetworkState state)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Packet Stats", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Packet size:    {packet.Length} bytes");
                EditorGUILayout.LabelField($"Full pose size: {fullSize} bytes");
                EditorGUILayout.LabelField($"Compression:    {(float)fullSize / packet.Length:F1}×");
                EditorGUILayout.LabelField($"Layers in state: {state.Layers.Length}");
                EditorGUILayout.LabelField($"IK targets:     {state.IKTargets.Length}");
            }
        }

        private void DrawLayerDetails(AnimationSystem.Controller.AnimationNetworkState state)
        {
            EditorGUILayout.LabelField("Layer Details", EditorStyles.boldLabel);
            foreach (var lp in state.Layers)
            {
                EditorGUILayout.LabelField(
                    $"  Layer {lp.LayerIndex}: clip={lp.CurrentState.ClipId} " +
                    $"t={lp.CurrentState.Time:F3}s w={lp.LayerWeight:F2}",
                    EditorStyles.miniLabel);
            }
        }
    }
}