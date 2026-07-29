using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class UrpSetup
{
    [InitializeOnLoadMethod]
    private static void SetupUrpOnLoad()
    {
        if (GraphicsSettings.defaultRenderPipeline != null) return;

        string path = "Assets/_Project/Settings/URP2DRenderer.asset";
        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (existing != null)
        {
            GraphicsSettings.defaultRenderPipeline = existing;
            QualitySettings.renderPipeline = existing;
            return;
        }

        var pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        pipelineAsset.supportsCameraDepthTexture = false;
        pipelineAsset.supportsCameraOpaqueTexture = false;
        pipelineAsset.msaaSampleCount = 1;
        pipelineAsset.shadowDistance = 0f;
        pipelineAsset.shadowCascadeCount = 1;

        AssetDatabase.CreateAsset(pipelineAsset, path);

        var rendererPath = "Assets/_Project/Settings/URP2DRendererData.asset";
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererData.opaqueLayerMask = 0;
        rendererData.transparentLayerMask = ~0;
        rendererData.postProcessData = null;

        AssetDatabase.CreateAsset(rendererData, rendererPath);

        SerializedObject so = new SerializedObject(pipelineAsset);
        SerializedProperty rendererList = so.FindProperty("m_RendererDataList");
        if (rendererList != null && rendererList.arraySize == 0)
        {
            rendererList.InsertArrayElementAtIndex(0);
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            so.ApplyModifiedProperties();
        }

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;

        AssetDatabase.SaveAssets();
        Debug.Log("[UrpSetup] URP 2D Renderer configured.");
    }
}
