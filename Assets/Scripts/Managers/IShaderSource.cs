using UnityEngine;

public interface IShaderSource
{
    float VignetteTotal {get; }
    float Chroma {get;}
    float ColorSaturation {get;}
    float VignetteSmooth {get;}
}
