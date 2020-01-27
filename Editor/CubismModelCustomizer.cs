using System;
using System.IO;
using System.Linq;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.LookAt;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Framework.Raycasting;
using Live2D.Cubism.Rendering;
using Ortega.Enums;
using Ortega.Live2DVerification.Component;
using Ortega.Share.Enums;
using UnityEditor;
using UnityEngine;

namespace Ortega.Live2DVerification.Editor
{
    /// <summary>
    /// Customizes materials of Cubism models from code.
    /// </summary>
    public static class CubismModelCustomizer
    {
        private static readonly string[] CustomizeMaterialSplit =
        {
            "FlowTexture", "Particle"
        };

        private static string ModelPlacePath { get; set; }

        public static void CustomizeCubismModel(CubismModelCustomizerParam param)
        {
            //内部的なシーンにプレハブをロードする
            var model = PrefabUtility.LoadPrefabContents(param.AssetPath);

            //var model = AssetDatabase.LoadAssetAtPath<GameObject>(param.AssetPath);

            //live2dモデルであるか？
            if (!model.GetComponent<CubismModel>())
            {
                return;
            }

            var cubismModel = model.GetComponent<CubismModel>();

            var renderController = model.GetComponent<CubismRenderController>();
            renderController.SortingLayer = "UI_Window";
            renderController.SortingOrder = -1;

            //イベントコンポーネントを設定する
            GetComponent<CubismEventExpansion>(model);

            //当たり判定使う場合
            if (param.IsHitJudge)
            {
                GetComponent<CubismRaycaster>(model);

                //「hitArea_アニメーション名_ボイス名_部位名」の形を成しているものに関してCubismRaycastableExpansionを追加する
                foreach (var modelDrawable in cubismModel.Drawables)
                {
                    var isHitJudgeTarget = false;
                    var split = modelDrawable.Id.Split('_');
                    if (split.Length >= 4)
                    {
                        isHitJudgeTarget = split[0] == "HitArea";
                    }

                    if (isHitJudgeTarget)
                    {
                        var raycastableExpansion = GetComponent<CubismRaycastableExpansion>(modelDrawable.gameObject);
                        //アニメーションタイプの設定
                        var characterAnimationType =
                            (CharacterAnimationType) Enum.Parse(typeof(CharacterAnimationType), split[1]);
                        raycastableExpansion._animationType = characterAnimationType;
                        //ボイスタイプの設定
                        var characterVoiceType = (CharacterVoiceType) Enum.Parse(typeof(CharacterVoiceType), split[2]);
                        raycastableExpansion._voiceType = characterVoiceType;
                    }
                }
            }

            //自動瞬きを使う場合
            if (param.IsAutoBlinkActive)
            {
                var eyeBlinkController = GetComponent<CubismEyeBlinkController>(model);

                eyeBlinkController.BlendMode = CubismParameterBlendMode.Override;

                var autoEyeBlinkInput = GetComponent<CubismAutoEyeBlinkInput>(model);

                //平均的な瞬きのタイミングを入れておく
                autoEyeBlinkInput.MaximumDeviation = 1.86f;
                autoEyeBlinkInput.Mean = 4.62f;
                autoEyeBlinkInput.Timescale = 3.66f;

                //パラメーターに関しては［左眼　開閉（ParamEyeLOpen）］、［右目　開閉（ParamEyeROpen）］を設定している場合
                //インポート時に自動追加してくれる??

                //「eyeBlink_パラメーター名」の形を成しているものに関してCubismEyeBlinkParameterを追加する
                foreach (var modelParameter in cubismModel.Parameters)
                {
                    var isEyeBlinkAddTarget = false;
                    var split = modelParameter.Id.Split('_');
                    if (split.Length > 0)
                    {
                        isEyeBlinkAddTarget = split[0] == "EyeBlink";
                    }

                    if (isEyeBlinkAddTarget)
                    {
                        GetComponent<CubismEyeBlinkParameter>(modelParameter.gameObject);
                    }
                }
            }

            //リップシンクを使う場合
            if (param.IsLipSyncActive)
            {
                var mouthController = GetComponent<CubismMouthController>(model);
                mouthController.BlendMode = CubismParameterBlendMode.Override;

                var lipSyncAudio = GetComponent<AudioSource>(model);
                var audioMouthInput = GetComponent<CubismAudioMouthInput>(model);

                audioMouthInput.AudioInput = lipSyncAudio;

                //平均的に口が綺麗に開くと思われる値を入れておく。
                audioMouthInput.Gain = 3.24f;
                audioMouthInput.Smoothing = 0.044f;

                //「lipSync_パラメーター名」の形を成しているものに関してCubismMouthParameterを追加する
                foreach (var modelParameter in cubismModel.Parameters)
                {
                    var isLipSyncAddTarget = false;
                    var split = modelParameter.Id.Split('_');
                    if (split.Length > 0)
                    {
                        isLipSyncAddTarget = split[0] == "LipSync";
                    }

                    if (isLipSyncAddTarget)
                    {
                        GetComponent<CubismMouthParameter>(modelParameter.gameObject);
                    }
                }
            }

            //視線追従を使う場合
            if (param.IsEyeTrackingActive)
            {
                var lookController = GetComponent<CubismLookController>(model);
                var targetObjectTransform = model.transform.Find("Target");
                GameObject targetObject = null;
                if (targetObjectTransform == null)
                {
                    targetObject = new GameObject("Target");
                    targetObject.transform.SetParent(model.transform, false);
                }
                else
                {
                    targetObject = targetObjectTransform.gameObject;
                }

                var lookTarget = GetComponent<CubismLookTargetExpansion>(targetObject);

                //BlendModeの設定
                lookController.BlendMode = CubismParameterBlendMode.Override;
                //Targetの設定
                lookController.Target = lookTarget;
                //「eyeTracking_パラメータ名＋(XYZ)」の形を成しているものに関しCubismLookParameterを追加する
                foreach (var modelParameter in cubismModel.Parameters)
                {
                    var isEyeTrackingAddTarget = false;
                    var split = modelParameter.Id.Split('_');
                    if (split.Length > 0)
                    {
                        isEyeTrackingAddTarget = split[0] == "EyeTracking";
                    }

                    if (isEyeTrackingAddTarget)
                    {
                        GetComponent<CubismLookParameter>(modelParameter.gameObject);
                    }
                }
            }

            //Particleの物理演算を使用する場合
            if (param.IsPhysicalOperationParticle)
            {
                foreach (var drawable in cubismModel.Drawables)
                {
                    var split = drawable.Id.Split('_');
                    if (split.Length >= 2)
                    {
                        var originalName = split[0] + split[1];
                        switch (split[0])
                        {
                            //「PhysicalOperationParticle_部位名」
                            case "PhysicalOperationParticle":

                                if (model.transform.Find(originalName) == null)
                                {
                                    var physicalOperationParticleObj = new GameObject(originalName);
                                    physicalOperationParticleObj.transform.SetParent(model.transform, false);

                                    var physicalOperationParticleFollow =
                                        GetComponent<CubismFollowObjectExpansion>(physicalOperationParticleObj);
                                    //追従元のMeshFilterを設定。
                                    var drawableMeshFilter = drawable.gameObject.GetComponent<MeshFilter>();
                                    physicalOperationParticleFollow.TargetMeshFilter = drawableMeshFilter;

                                    //Z軸で一番手前に置く(xとyはdrawableのmeshFilter参照)
                                    var posX = physicalOperationParticleObj.transform.position.x;
                                    var posY = physicalOperationParticleObj.transform.position.y;
                                    if (drawableMeshFilter.mesh.vertices.Length > 0)
                                    {
                                        posX = drawableMeshFilter.mesh.vertices[0].x;
                                        posY = drawableMeshFilter.mesh.vertices[0].y;
                                    }

                                    physicalOperationParticleObj.transform.position
                                        = new Vector3(posX, posY, -10);

                                    var physicalOperationParticle =
                                        GetComponent<ParticleSystem>(physicalOperationParticleObj);
                                    var particleRenderer = physicalOperationParticle.GetComponent<Renderer>();

                                    particleRenderer.sortingOrder = -1;
                                    particleRenderer.sortingLayerName = "UI_Window";
                                }

                                break;
                        }
                    }
                }
            }

            //TODO マテリアルのフォルダパスを作成する
            var assetPathSplit = param.AssetPath.Split('/');
            if (assetPathSplit.Length > 0)
            {
                ModelPlacePath = param.AssetPath.Replace(assetPathSplit[assetPathSplit.Length - 1], "");
            }

            //アニメーション設定を自動作成する場合
            if (param.IsAutoAnimationSetting)
            {
                /*
                var animatorControllerPath = $"{ModelPlacePath}/live2d_data/animation/{model.name}.controller";

                var runTimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);
                if (runTimeAnimatorController == null)
                {
                    runTimeAnimatorController = AnimatorController.CreateAnimatorControllerAtPath(animatorControllerPath);
                }

                var animatorController = runTimeAnimatorController as AnimatorController;

                //コントローラーに対してパラメーターを割り当てる
                foreach (CharacterAnimationType animationType in Enum.GetValues(typeof(CharacterAnimationType)))
                {
                    animatorController.AddParameter(animationType.ToString(), AnimatorControllerParameterType.Trigger);
                }

                //.animのものに関して全件検索
                var clipsPath = AssetDatabase.FindAssets("t:AnimationClip", new[] { $"{ModelPlacePath}/live2d_data/animation/" });
                foreach (var clipPath in clipsPath)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    AssetDatabase.AddObjectToAsset(clip, animatorController);
                    var state = animatorController.AddMotion(clip);
                }

                //コントローラーをモデルのAnimatorにアタッチする
                var modelAnimator = GetComponent<Animator>(model);

                //runtimeanimatorcontrollerが必要？？
                modelAnimator.runtimeAnimatorController = animatorController;

                AssetDatabase.SaveAssets();
                //コントローラーの上書き?
                FileUtil.ReplaceFile(animatorControllerPath, animatorControllerPath);
                AssetDatabase.Refresh();

                OrtegaLogger.Log("CubismModel Animation Setting Complete");
                */
                OrtegaLogger.Log("自動アニメーションの設定は未実装です");
            }

            //TODO マテリアルの割り当て
            //CubismImporter.OnPickMaterial = CustomizeMaterial;
            foreach (var drawable in cubismModel.Drawables)
            {
                var split = drawable.Id.Split('_');
                if (split.Length > 0 && CustomizeMaterialSplit.Contains(split[0]))
                {
                    var material = CustomizeMaterial(drawable);
                    var cubismRenderer = drawable.gameObject.GetComponent<CubismRenderer>();
                    cubismRenderer.Material = material;
                }
            }

            SetLayerRecursively(model, 5);

            //独自テクスチャ追加により必要になったオブジェクトを作成する
            foreach (var drawable in cubismModel.Drawables)
            {
                var split = drawable.Id.Split('_');
                if (split.Length >= 2)
                {
                    var originalName = split[0] + split[1];
                    switch (split[0])
                    {
                        //レンダーテクスチャ(パーティクル)
                        //particle_部位名(_(スキニングしている場合記号))
                        case "Particle":
                            var particleCameraObjTransform = model.transform.Find(originalName + "_Camera");
                            GameObject particleCameraObj = null;
                            Camera particleCamera = null;
                            if (particleCameraObjTransform == null)
                            {
                                //カメラの生成
                                particleCameraObj = new GameObject(originalName + "_Camera");
                                particleCameraObj.transform.SetParent(model.transform, false);
                                particleCamera = GetComponent<Camera>(particleCameraObj);

                                var renderTextureAssetPath = GetMaterialFolder() + "/" + originalName + "CustomRenderTexture.asset";
                                var targetTexture = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>(renderTextureAssetPath);
                                particleCamera.targetTexture = targetTexture;
                                OrtegaLogger.Log($"Setting complete.create:{particleCameraObj.name}");
                            }
                            else
                            {
                                particleCameraObj = particleCameraObjTransform.gameObject;
                                particleCamera = GetComponent<Camera>(particleCameraObj);
                            }

                            particleCamera.backgroundColor = new Color(0, 0, 0, 0);

                            particleCamera.orthographic = true;

                            //描画するレイヤーの設定(エフェクト専用のレイヤーCharacterEffect)

                            particleCamera.cullingMask = 1 << LayerMask.NameToLayer("CharacterEffect");

                            particleCamera.clearFlags = CameraClearFlags.SolidColor;

                            particleCamera.orthographicSize = 1;
                            //カメラのレイヤー設定(CharacterEffect)
                            particleCameraObj.layer = 9;
                            //パーティクルの生成(デフォのものも自動でやってしまうとよいかも)(カメラの子にする
                            var particleObjTransform = particleCameraObj.transform.Find(originalName + "_Particle");
                            GameObject particleObj = null;
                            if (particleObjTransform == null)
                            {
                                particleObj = new GameObject(originalName + "_Particle");
                                particleObj.transform.SetParent(particleCameraObj.transform, false);

                                var particle = GetComponent<ParticleSystem>(particleObj);
                                var particleRenderer = particle.GetComponent<Renderer>();
                                particleRenderer.sortingOrder = -1;
                                particleRenderer.sortingLayerName = "UI_Window";

                                OrtegaLogger.Log($"Setting complete.create:{particleObj.name}");
                            }
                            else
                            {
                                particleObj = particleObjTransform.gameObject;
                            }

                            particleObj.layer = 9;
                            break;
                        //流れるテクスチャ
                        //flowTexture_部位名(_(スキニングしている場合記号))
                        case "FlowTexture":
                            //プレハブ内では何も生成しない
                            break;
                    }
                }
            }

            //プレハブの変更を保存する
            PrefabUtility.SaveAsPrefabAsset(model, param.AssetPath);
            PrefabUtility.UnloadPrefabContents(model);

            OrtegaLogger.Log("Setting All Complete");
        }

        /// <summary>
        /// カスタマイズされたマテリアルを適用する
        /// </summary>
        /// <param name="drawable"></param>
        /// <returns></returns>
        private static Material CustomizeMaterial(CubismDrawable drawable)
        {
            Material material = null;

            var cubismRenderer = drawable.gameObject.GetComponent<CubismRenderer>();

            var split = drawable.Id.Split('_');
            if (split.Length >= 2)
            {
                var originalName = split[0] + split[1];
                var joinName = string.Empty;
                switch (split[0])
                {
                    //レンダーテクスチャ(パーティクル)
                    //particle_部位名(_(スキニングしている場合記号))
                    case "Particle":

                        var particleMaterialFolderPath = GetMaterialFolder();
                        joinName = particleMaterialFolderPath + "/" + originalName;
                        material = AssetDatabase.LoadAssetAtPath<Material>(joinName + ".mat");
                        CustomRenderTexture customRenderTexture = null;
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Live2DVerification/CubismModelRenderTexture"));

                            AssetDatabase.CreateAsset(material, joinName + ".mat");
                            //TODO 空のテクスチャを作成

                            customRenderTexture = new CustomRenderTexture(cubismRenderer.MainTexture.width, cubismRenderer.MainTexture.height);
                            customRenderTexture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
                            customRenderTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;
                            AssetDatabase.CreateAsset(customRenderTexture, joinName + "CustomRenderTexture.asset");

                            //TODO
                            material.SetTexture("RenderTex", customRenderTexture);

                            OrtegaLogger.Log("CustomMaterial setting complete." +
                                             $"setting target:{drawable.Id}" +
                                             $"create:{joinName}CustomRenderTexture.asset & {joinName}.mat");
                        }
                        else
                        {
                            customRenderTexture = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>(joinName + "CustomRenderTexture.asset");
                        }

                        //live2d専用のマテリアル設定
                        material.SetInt("_SrcColor", 1);
                        material.SetInt("_DstColor", 10);
                        material.SetInt("_SrcAlpha", 1);
                        material.SetInt("_DstAlpha", 10);
                        //独自のテクスチャ設定
                        material.SetTexture("_RenderTex", customRenderTexture);

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        break;
                    //流れるテクスチャ
                    //flowTexture_部位名(_(スキニングしている場合記号))
                    case "FlowTexture":

                        var flowTextureMaterialFolderPath = GetMaterialFolder();
                        joinName = flowTextureMaterialFolderPath + "/" + originalName;
                        material = AssetDatabase.LoadAssetAtPath<Material>(joinName + ".mat");
                        Texture2D subTexture = null;
                        Texture2D flowTexture = null;
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Live2DVerification/CubismModelFlowTexture"));
                            AssetDatabase.CreateAsset(material, joinName + ".mat");

                            //TODO 空のテクスチャを作成
                            subTexture = new Texture2D(cubismRenderer.MainTexture.width, cubismRenderer.MainTexture.height, TextureFormat.ARGB32, false, false);
                            var subTextureBytes = subTexture.EncodeToPNG();
                            File.WriteAllBytes(joinName + "SubTexture.png", subTextureBytes);

                            flowTexture = new Texture2D(cubismRenderer.MainTexture.width, cubismRenderer.MainTexture.height, TextureFormat.ARGB32, false, false);

                            var flowTextureBytes = flowTexture.EncodeToPNG();
                            File.WriteAllBytes(joinName + "FlowTexture.png", flowTextureBytes);

                            Debug.Log("CustomMaterial setting complete." +
                                      $"setting target:{drawable.Id}" +
                                      $"create:{joinName}SubTexture.png & {joinName}FlowTexture.png & {joinName}.mat");
                        }
                        else
                        {
                            subTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(joinName + "SubTexture.png");
                            flowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(joinName + "FlowTexture.png");
                        }

                        //live2d専用のマテリアル設定
                        material.SetInt("_SrcColor", 1);
                        material.SetInt("_DstColor", 10);
                        material.SetInt("_SrcAlpha", 1);
                        material.SetInt("_DstAlpha", 10);

                        //独自のテクスチャ設定
                        material.SetTexture("_SubTex", subTexture);
                        material.SetTexture("_FlowMap", flowTexture);

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        break;
                    //新しいマテリアルが追加されたらどんどん追加記述していく。
                    default:
                        OrtegaLogger.Log($"DefaultMaterial setting complete.setting target:{drawable.Id}");
                        break;
                }
            }

            return material;
        }

        private static T GetComponent<T>(GameObject obj) where T : UnityEngine.Component
        {
            if (obj.GetComponent<T>())
            {
                return obj.GetComponent<T>();
            }

            return obj.AddComponent<T>();
        }

        private static string GetMaterialFolder()
        {
            var materialFolderPath = ModelPlacePath + "live2d_data/materials";

            if (!AssetDatabase.IsValidFolder(materialFolderPath))
            {
                Directory.CreateDirectory(materialFolderPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return materialFolderPath;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (null == obj)
            {
                return;
            }

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                if (null == child)
                {
                    continue;
                }

                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    public class CubismModelCustomizerParam
    {
        public string AssetPath { get; set; }

        public bool IsAutoAnimationSetting { get; set; }
        public bool IsAutoBlinkActive { get; set; }
        public bool IsEyeTrackingActive { get; set; }
        public bool IsHitJudge { get; set; }
        public bool IsLipSyncActive { get; set; }
        public bool IsPhysicalOperationParticle { get; set; }
    }
}