using System.Collections;
using System.IO;
using UnityEngine;

namespace PolarityProtocol.Arena
{
    public sealed class VisualCaptureProbe : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(CaptureWhenReady());
        }

        private static IEnumerator CaptureWhenReady()
        {
            while (GameSession.Active == null)
            {
                yield return null;
            }

            GameSession.Active.BeginRun();
            yield return new WaitForSecondsRealtime(3f);
            yield return new WaitForEndOfFrame();

            string outputPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "PolarityProtocolCapture.png"));
            ScreenCapture.CaptureScreenshot(outputPath, 1);

            float timeout = Time.realtimeSinceStartup + 5f;
            while (!File.Exists(outputPath) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Debug.Log($"[PolarityCapture] Screenshot written to {outputPath}");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(File.Exists(outputPath) ? 0 : 1);
        }
    }
}
