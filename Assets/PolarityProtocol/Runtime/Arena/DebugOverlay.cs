using PolarityProtocol.Utilities;
using UnityEngine;

namespace PolarityProtocol.Arena
{
    public sealed class DebugOverlay : MonoBehaviour
    {
        public static bool Enabled { get; private set; }

        private void Update()
        {
            if (LegacyInput.DebugPressed)
            {
                Enabled = !Enabled;
                FeedbackBus.Pulse(Enabled ? 620f : 190f, 0.06f, 0.05f);
            }
        }

        private void OnDestroy()
        {
            Enabled = false;
        }
    }
}

