using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// TODO. 전체적인 코드 정리 및 구체적인 계획 수립
public class GraphicService : IService {

    private RenderPipelineAsset _renderPipeline;
    private UniversalRenderPipelineAsset _universalRenderPipeline;
    private ScriptableRenderer _renderer;

    private Enum _currentQualityTpe;
    
    private Dictionary<string, int> _allQualityLevelDic = new();
    private Dictionary<Enum, int> _implementQualityLevelDic = new();
    private Dictionary<Type, RendererFeatureHandler> _handlerDic = new();

    void IService.Init() {
        QualitySettings.names.IndexForEach((name, index) => {
            foreach (var nameCase in name.GetAllCaseList()) {
                _allQualityLevelDic.AutoAdd(nameCase, index);
            }
        });
        
        _renderPipeline = GraphicsSettings.currentRenderPipeline;
        _universalRenderPipeline = _renderPipeline as UniversalRenderPipelineAsset;
        
        if (_universalRenderPipeline != null) {
            foreach (var type in ReflectionProvider.GetSubClassTypes<ScriptableRendererFeature>()) {
                var feature = RendererFeatureHandler.GetRendererFeature(type);
                if (feature != null) {
                    var handler = RendererFeatureHandler.CreateHandler(feature);
                    if (handler != null) {
                        _handlerDic.AutoAdd(type, handler);
                    }
                }
            }
        }

        if (ReflectionProvider.TryGetAttributeEnumTypes<GraphicQualityEnumAttribute>(out var enumerable)) {
           foreach (var type in enumerable.SelectMany(x => Enum.GetValues(x).Cast<Enum>())) {
               if (_allQualityLevelDic.TryGetValue(type.ToString(), out var level)) {
                   _implementQualityLevelDic.AutoAdd(type, level);
               }
           }
        }
    }
    
    void IService.Start() {
        Logger.TraceLog(_allQualityLevelDic.ToStringCollection(", "));
        Logger.TraceLog(_implementQualityLevelDic.ToStringCollection(", "));
        
        if (_renderPipeline != null) {
            Logger.TraceLog(_renderPipeline.name);
        }
    }

    void IService.Stop() {

    }

    public void ChangeQualityLevel(Enum type) {
        if (_implementQualityLevelDic.TryGetValue(type, out var level)) {
            _currentQualityTpe = type;
            ChangeQualityLevel(level);
        }
    }

    public void ChangeQualityLevel(int level) => QualitySettings.SetQualityLevel(level);

    public Enum GetCurrentQualityType() {
        if (_currentQualityTpe == null) {
            var level = QualitySettings.GetQualityLevel();
            foreach (var pair in _implementQualityLevelDic) {
                if (pair.Value == level) {
                    return pair.Key;
                }
            }

            return null;
        }
        
        return _currentQualityTpe;
    }
    
    public int GetCurrentQualityLevel() => QualitySettings.GetQualityLevel();
    public int GetGraphicSize() => SystemInfo.graphicsMemorySize;
}

[AttributeUsage(AttributeTargets.Enum)]
public class GraphicQualityEnumAttribute : Attribute {
    
}

[GraphicQualityEnum]
public enum TEST_TYPE {
    VeryLow,
    Very_Low,
    Low,
}

[RendererFeatureHandler(typeof(SampleRendererFeature))]
public class SampleRendererFeatureHandler : RendererFeatureHandler {

    public SampleRendererFeatureHandler(ScriptableRendererFeature feature) : base(feature) { }

    public override T Get<T>() {
        if (feature is T rendererFeature) {
            return rendererFeature;
        }

        return null;
    }
}

public class RendererFeatureHandlerAttribute : Attribute {

    public Type featureType;

    public RendererFeatureHandlerAttribute(Type featureType) {
        if (featureType.DeclaringType == typeof(ScriptableRendererFeature)) {
            this.featureType = featureType;
        }
    } 
}

[RequiresAttributeImplementation(typeof(RendererFeatureHandlerAttribute))]
public abstract class RendererFeatureHandler {

    protected ScriptableRendererFeature feature;

    private const string TARGET_PROPERTY_NAME = "rendererFeatures";
    
    internal static ScriptableRendererFeature GetRendererFeature(Type type) {
        if (type.IsAbstract == false || type.DeclaringType != typeof(ScriptableRendererFeature)) {
            Logger.TraceError($"{type.Name} does not inherit from {nameof(ScriptableRendererFeature)}. Please check the implementation again.");
            return null;
        }
        
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset universalPipelineAsset) {
            var renderer = universalPipelineAsset.scriptableRenderer;
            if (renderer != null && renderer.GetType().TryGetPropertyValue(renderer, TARGET_PROPERTY_NAME, out List<ScriptableRendererFeature> list, BindingFlags.GetProperty | BindingFlags.NonPublic)) {
                foreach (var feature in list) {
                    if (feature.GetType() == type) {
                        return feature;
                    }
                }
            }
        }
        
        Logger.TraceError($"Could not find a {nameof(feature)} that matches the ${type.Name}.");
        return null;
    }

    internal static RendererFeatureHandler CreateHandler(ScriptableRendererFeature feature) {
        var type = feature.GetType();
        foreach (var handlerType in ReflectionProvider.GetSubClassTypes<RendererFeatureHandler>()) {
            if (handlerType.TryGetCustomAttribute<RendererFeatureHandlerAttribute>(out var attribute) && attribute.featureType == type) {
                return SystemUtil.SafeCreateInstance<RendererFeatureHandler>(type);
            }
        }

        return null;
    }

    public RendererFeatureHandler(ScriptableRendererFeature feature) => this.feature = feature;

    public abstract T Get<T>() where T : ScriptableRendererFeature;
    public virtual bool IsValid() => feature != null;
}