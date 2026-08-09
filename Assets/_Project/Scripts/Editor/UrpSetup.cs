using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class UrpSetup
{
    private const string PipelinePath = "Assets/_Project/Settings/URP2DRenderer.asset";
    private const string Renderer3DPath = "Assets/_Project/Settings/URP3DRendererData.asset";
    private const float ShadowDistance = 30f;

    [InitializeOnLoadMethod]
    private static void SetupUrpOnLoad()
    {
        var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            pipeline = CreatePipelineIfMissing();
            if (pipeline == null) return;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        Ensure3DRenderer(pipeline);
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
    }

    private static UniversalRenderPipelineAsset CreatePipelineIfMissing()
    {
        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (existing != null) return existing;

        var pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        pipelineAsset.supportsCameraDepthTexture = false;
        pipelineAsset.supportsCameraOpaqueTexture = false;
        pipelineAsset.msaaSampleCount = 1;
        pipelineAsset.shadowDistance = ShadowDistance;
        pipelineAsset.shadowCascadeCount = 1;

        AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
        AssetDatabase.SaveAssets();
        return pipelineAsset;
    }

    private static void Ensure3DRenderer(UniversalRenderPipelineAsset pipeline)
    {
        var renderer3d = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Renderer3DPath);
        if (renderer3d == null)
        {
            renderer3d = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer3d.opaqueLayerMask = ~0;
            renderer3d.transparentLayerMask = ~0;
            renderer3d.postProcessData = null;
            AssetDatabase.CreateAsset(renderer3d, Renderer3DPath);
        }

        SerializedObject so = new SerializedObject(pipeline);
        SerializedProperty rendererList = so.FindProperty("m_RendererDataList");
        if (rendererList != null)
        {
            rendererList.ClearArray();
            rendererList.InsertArrayElementAtIndex(0);
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer3d;
        }
        SerializedProperty defaultIndex = so.FindProperty("m_DefaultRendererIndex");
        if (defaultIndex != null) defaultIndex.intValue = 0;
        SerializedProperty shadowDistance = so.FindProperty("m_ShadowDistance");
        if (shadowDistance != null && shadowDistance.floatValue <= 0f)
            shadowDistance.floatValue = ShadowDistance;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}