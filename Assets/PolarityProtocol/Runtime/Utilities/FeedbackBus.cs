using System.Collections;
using UnityEngine;

namespace PolarityProtocol.Utilities
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FeedbackBus : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private static readonly float[] ToneFrequencies =
        {
            75f, 90f, 110f, 150f, 190f, 210f, 240f, 260f, 320f, 330f, 410f, 440f, 520f, 620f, 680f
        };

        private AudioSource source;
        private AudioClip[] toneClips;
        private static FeedbackBus active;

        private void Awake()
        {
            active = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.65f;
            toneClips = new AudioClip[ToneFrequencies.Length];
            for (int i = 0; i < ToneFrequencies.Length; i++)
            {
                toneClips[i] = CreateTone(ToneFrequencies[i]);
            }
        }

        private void OnDestroy()
        {
            if (active == this)
            {
                active = null;
            }

            if (toneClips == null)
            {
                return;
            }

            for (int i = 0; i < toneClips.Length; i++)
            {
                if (toneClips[i] != null)
                {
                    Destroy(toneClips[i]);
                }
            }
        }

        public static void Pulse(float frequency, float duration, float volume)
        {
            if (active == null || duration <= 0f || volume <= 0f)
            {
                return;
            }

            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < ToneFrequencies.Length; i++)
            {
                float distance = Mathf.Abs(frequency - ToneFrequencies[i]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            active.source.PlayOneShot(active.toneClips[nearestIndex], Mathf.Clamp01(volume * 5f));
        }

        public static void HitStop(float duration)
        {
            if (active != null)
            {
                active.StartCoroutine(active.HitStopRoutine(duration));
            }
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float previousScale = Time.timeScale;
            Time.timeScale = 0.06f;
            yield return new WaitForSecondsRealtime(duration);

            if (Time.timeScale < 0.1f)
            {
                Time.timeScale = previousScale;
            }
        }

        private static AudioClip CreateTone(float frequency)
        {
            const float duration = 0.14f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float envelope = Mathf.Pow(1f - progress, 2f);
                samples[i] = Mathf.Sin(i * Mathf.PI * 2f * frequency / SampleRate) * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create($"Tone {frequency:0}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
