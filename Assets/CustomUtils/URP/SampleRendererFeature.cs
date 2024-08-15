using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SampleRendererFeature : ScriptableRendererFeature {

    public Config config = new();
    private SampleRendererPass _pass;
    
    public override void Create() {
        _pass = new SampleRendererPass(config);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(_pass);
    }
}

public class SampleRendererPass : ScriptableRenderPass {

    private Shader _shader;

    internal SampleRendererPass(Config config) {
        _shader = config.shader;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        
    }
}

[Serializable]
public class Config {
    public int testValue;
    public Shader shader;
}
