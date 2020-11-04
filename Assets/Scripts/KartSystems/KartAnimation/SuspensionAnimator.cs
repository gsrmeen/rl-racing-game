using UnityEngine;

namespace KartGame.KartSystems
{
    public class SuspensionAnimator : MonoBehaviour
    {
        public void AnimateSuspension(Vector3 suspensionNeutralPos, KartStats stats, Transform suspensionBody)
        {
            var suspensionTargetPos = suspensionNeutralPos;
            var bodyRot = transform.rotation.eulerAngles;
            var maxXTilt = stats.Suspension * 45;
            var closestNeutralRot = Mathf.Abs(360 - bodyRot.x) < Mathf.Abs(bodyRot.x) ? 360 : 0;
            var xTilt = Mathf.DeltaAngle(closestNeutralRot, bodyRot.x);
            var suspensionT = Mathf.InverseLerp(0, maxXTilt, xTilt);
            suspensionT *= suspensionT;
            bodyRot.x = Mathf.Lerp(closestNeutralRot, bodyRot.x, suspensionT);
            var suspensionTargetRot =
                Quaternion.Inverse(suspensionBody.transform.rotation) * Quaternion.Euler(bodyRot);
            suspensionBody.transform.localPosition = Vector3.Lerp(suspensionBody.transform.localPosition,
                suspensionTargetPos, Time.deltaTime * 5f);
            suspensionBody.transform.localRotation = Quaternion.Slerp(suspensionBody.transform.localRotation,
                suspensionTargetRot, Time.deltaTime * 5f);
        }
    }
}