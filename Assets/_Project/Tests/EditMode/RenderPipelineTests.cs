using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderPipelineTests
{
    [Test]
    public void DefaultPipeline_IsConfiguredAndSharedAcrossQualitySettings()
    {
        Assert.IsNotNull(GraphicsSettings.defaultRenderPipeline, "A default render pipeline is configured");
        Assert.AreEqual(GraphicsSettings.defaultRenderPipeline, QualitySettings.renderPipeline, "Quality settings share the same pipeline");
    }

    [Test]
    public void Pipeline_IsUniversal_WithOpaquePassAndShadows()
    {
        var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;

        Assert.IsNotNull(pipeline, "The default render pipeline is a URP Universal asset");
        Assert.That(pipeline.shadowDistance, Is.GreaterThan(0f), "The pipeline casts light shadows");

        var so = new SerializedObject(pipeline);
        var rendererList = so.FindProperty("m_RendererDataList");
        Assert.That(rendererList.arraySize, Is.GreaterThan(0), "The pipeline carries a renderer");

        var rendererData = rendererList.GetArrayElementAtIndex(0).objectReferenceValue as UniversalRendererData;
        Assert.IsNotNull(rendererData, "The default renderer is a UniversalRendererData");

        var rso = new SerializedObject(rendererData);
        var opaqueMask = rso.FindProperty("m_OpaqueLayerMask");
        Assert.AreEqual(~0, opaqueMask.intValue, "The opaque pass covers all layers so 3D content renders lit");
    }
}