using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class UrpSetup
{
    [InitializeOnLoadMethod]
    private static void SetupUrpOnLoad()
    {
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            RepairDefaultRenderer(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);
            return;
        }

        string path = "Assets/_Project/Settings/URP2DRenderer.asset";
        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (existing != null)
        {
            GraphicsSettings.defaultRenderPipeline = existing;
            QualitySettings.renderPipeline = existing;
            RepairDefaultRenderer(existing);
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
        RepairDefaultRenderer(pipelineAsset);
        Debug.Log("[UrpSetup] URP 2D Renderer configured.");
    }

    private static void RepairDefaultRenderer(UniversalRenderPipelineAsset asset)
    {
        if (asset == null) return;

        string rendererPath = "Assets/_Project/Settings/URP2DRendererData.asset";
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (rendererData == null) return;

        var so = new SerializedObject(asset);
        var list = so.FindProperty("m_RendererDataList");
        var index = so.FindProperty("m_DefaultRendererIndex");
        if (list == null || list.arraySize == 0 || index == null) return;

        int defaultIndex = index.intValue >= 0 && index.intValue < list.arraySize
            ? index.intValue
            : 0;
        var element = list.GetArrayElementAtIndex(defaultIndex);
        if (element.objectReferenceValue == null)
        {
            element.objectReferenceValue = rendererData;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[UrpSetup] Repaired missing default renderer.");
        }
    }
}
