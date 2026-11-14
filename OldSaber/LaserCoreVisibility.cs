using UnityEngine;

namespace OldSaber
{
    [DefaultExecutionOrder(11000)]
    public sealed class LaserCoreVisibility : MonoBehaviour
    {
        private TubeBloomPrePassLight tube;
        private MeshRenderer core;

        internal void Initialize(TubeBloomPrePassLight light, MeshRenderer renderer)
        {
            tube = light;
            core = renderer;
        }

        private void OnEnable()
        {
            if (tube) tube.didRefreshEvent += Apply;
            Apply();
        }

        private void Apply()
        {
            if (!core) return;
            if (!tube || !tube.isActiveAndEnabled) { core.enabled = false; return; }
            var c = tube.color;
            core.enabled = BeamVisibility.ShouldDraw(c.r, c.g, c.b, c.a);
        }

        // ParametricBoxController can re-enable the renderer independently.
        // Reconcile after its lifecycle/update work, before cameras render.
        private void LateUpdate() { Apply(); }

        private void OnDisable()
        {
            if (tube) tube.didRefreshEvent -= Apply;
            if (core) core.enabled = false;
        }
    }
}
