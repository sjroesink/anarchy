using UnityEngine;

namespace Reborn
{
    // Inspector provenance for the reconstruction scene, not gameplay authority.
    public sealed class SourcePlacement:MonoBehaviour
    {
        public string clientVersion,sourceIdentity,templateName;
        public int templateId;
        public Vector3 sourcePosition,coordinateOrigin;
        public Vector4 rawHeadingWZYX;
        public string headingStatus="Preserved only; rotation conversion not verified";
        void OnDrawGizmos()
        {
            Gizmos.color=templateId==95350?Color.cyan:templateId==96272?new Color(1,.6f,.2f):Color.gray;
            Gizmos.DrawWireSphere(transform.position,.7f);
        }
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.Label(transform.position+Vector3.up,$"{templateName} [{templateId}]\nAO {sourcePosition}");
        }
#endif
    }
}
