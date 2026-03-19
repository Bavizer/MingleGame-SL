using MEC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MingleGame.Tools;

public static class LocationTools
{
    public static void Rotate(Transform transform, Vector3 delta, float duration = 0f)
    {
        if (duration <= 0f)
            transform.Rotate(delta);
        else
            Timing.RunCoroutine(Rotate_Coroutine(transform, delta, duration));
    }

    public static void SetLightIntensity(LightSourceToy light, float intensity, float switchingDuration = 0f)
    {
        if (switchingDuration <= 0f)
            light.NetworkLightIntensity = intensity;
        else
            Timing.RunCoroutine(SetLightIntensity_Coroutine(light, intensity, switchingDuration));
    }

    public static void SwitchLightColors(LightSourceToy light, IEnumerable<Color> colors, float delta, float duration = 0, bool mustBeRandom = true)
    {
        if (duration > 0f)
            Timing.RunCoroutine(SwitchLightColors_Coroutine(light, colors, delta, duration, mustBeRandom));
        else
        {
            var colors1 = colors.ToArray();
            light.NetworkLightColor = mustBeRandom 
                                        ? colors1[Random.Range(0, colors1.Length - 1)] 
                                        : colors1.Last();
        }
    }

    private static IEnumerator<float> Rotate_Coroutine(Transform transform, Vector3 delta, float duration)
    {
        var elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            yield return Timing.WaitForOneFrame;
            transform.Rotate(delta * Time.deltaTime);
            elapsedTime += Time.deltaTime;
        }
    }

    private static IEnumerator<float> SetLightIntensity_Coroutine(LightSourceToy light, float intensity, float switchingDuration)
    {
        var elapsedTime = 0f;
        var initialIntensity = light.NetworkLightIntensity;

        while (elapsedTime < switchingDuration)
        {
            yield return Timing.WaitForOneFrame;
            light.NetworkLightIntensity = Mathf.Lerp(initialIntensity, intensity, elapsedTime / switchingDuration);
            elapsedTime += Time.deltaTime;
        }
        light.NetworkLightIntensity = intensity;
    }

    private static IEnumerator<float> SwitchLightColors_Coroutine(LightSourceToy light, IEnumerable<Color> colors, float delta, float duration, bool mustBeRandom)
    {
        var startTime = Time.time;
        var elapsedTime = 0f;

        var colors1 = colors.ToArray();
        while (elapsedTime < duration)
        {
            if (mustBeRandom)
            {
                light.NetworkLightColor = colors1[Random.Range(0, colors1.Length - 1)];
                yield return Timing.WaitForSeconds(delta);
                elapsedTime = Time.time - startTime;
            }
            else
            {
                foreach (var color in colors1)
                {
                    light.NetworkLightColor = color;
                    yield return Timing.WaitForSeconds(delta);
                    elapsedTime = Time.time - startTime;
                }
            }
        }
    }
}
