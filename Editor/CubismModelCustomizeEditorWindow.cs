using UnityEditor;
using UnityEngine;

namespace Ortega.Live2DVerification.Editor
{
    public class CubismModelCustomizeEditorWindow : EditorWindow
    {
        private static bool isAutoAnimationSetting = true;
        private static bool isAutoBlinkActive = true;
        private static bool isEyeTrackingActive = true;
        private static bool isHitJudge = true;
        private static bool isLipSyncActive = true;
        private static bool isPhysicalOperationParticleActive = true;
        private static string prefabPath = "Assets/AddressableAssets/Characters/1/character_live2d_1.prefab";

        private void OnGUI()
        {
            prefabPath = EditorGUILayout.TextField("PrefabPath", prefabPath);
            isHitJudge = GUILayout.Toggle(isHitJudge, "当たり判定を有効にする");

            isLipSyncActive = GUILayout.Toggle(isLipSyncActive, "リップシンクを有効にする");

            isEyeTrackingActive = GUILayout.Toggle(isEyeTrackingActive, "視線追従を有効にする");

            isAutoBlinkActive = GUILayout.Toggle(isAutoBlinkActive, "自動瞬きを有効にする");

            isPhysicalOperationParticleActive = GUILayout.Toggle(isPhysicalOperationParticleActive, "物理演算パーティクルを有効にする");
            isAutoAnimationSetting = GUILayout.Toggle(isAutoAnimationSetting, "自動アニメーション設定を有効にする");

            if (GUILayout.Button("Live2Dのカスタマイズを実行する"))
            {
                //Live2dに設定を反映する
                CubismModelCustomizer.CustomizeCubismModel(new CubismModelCustomizerParam
                {
                    AssetPath = prefabPath,
                    IsAutoBlinkActive = isAutoBlinkActive,
                    IsEyeTrackingActive = isEyeTrackingActive,
                    IsHitJudge = isHitJudge,
                    IsLipSyncActive = isLipSyncActive,
                    IsPhysicalOperationParticle = isPhysicalOperationParticleActive,
                    IsAutoAnimationSetting = isAutoAnimationSetting
                });
            }
        }

        [MenuItem("Live2D/CubismModelCustomizeEditorWindow")]
        private static void Open()
        {
            GetWindow<CubismModelCustomizeEditorWindow>("CubismModelCustomizeEditorWindow");
        }
    }
}