using System;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    public class KartMovement : MonoBehaviour
    {
        public KartStats baseStats = KartStats.GetDefaults();

        [Header("Vehicle Physics")] public Transform CenterOfMass;

        [Tooltip("The physical representations of the Kart's wheels.")]
        public Transform[] Wheels;

        [Tooltip("Which layers the wheels will detect.")]
        public LayerMask GroundLayers = Physics.DefaultRaycastLayers;

        [Tooltip("How far to raycast when checking for ground.")]
        public float RaycastDist = 0.3f;

        [Tooltip("How high to keep the kart above the ground.")]
        public float MinHeightThreshold = 0.02f;

        public Transform SuspensionBody;

        private bool canMove = true;
        private KartStats finalStats;

        private IInput[] m_Inputs;

        private Vector3 suspensionNeutralPos;
        private Quaternion suspensionNeutralRot;

        public Rigidbody Rigidbody { get; private set; }
        public Vector2 Input { get; private set; }
        public float AirPercent { get; private set; }
        public float GroundPercent { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            m_Inputs = GetComponents<IInput>();
            var bodyTransform = SuspensionBody.transform;
            suspensionNeutralPos = bodyTransform.localPosition;
            suspensionNeutralRot = bodyTransform.localRotation;
        }

        public void Reset()
        {
            var euler = transform.rotation.eulerAngles;
            euler.x = euler.z = 0f;
            transform.rotation = Quaternion.Euler(euler);
        }

        private void FixedUpdate()
        {
            GatherInputs();
            InitValues();

            var groundedCount = CountGroundedWheels(out var minHeight);
            GroundPercent = (float) groundedCount / Wheels.Length;
            AirPercent = 1 - GroundPercent;

            var accel = Input.y;
            var turn = Input.x;

            GroundVehicle(minHeight);
            if (canMove) MoveVehicle(accel, turn);

            GroundAirbourne();
            AnimateSuspension();
        }

        private void GatherInputs()
        {
            // reset input
            Input = Vector2.zero;

            // gather nonzero input from our sources
            for (var i = 0; i < m_Inputs.Length; i++)
            {
                var inputSource = m_Inputs[i];
                var current = inputSource.GenerateInput();
                if (current.sqrMagnitude > 0) Input = current;
            }
        }

        private void InitValues()
        {
            finalStats = baseStats;
            finalStats.Grip = Mathf.Clamp(finalStats.Grip, 0, 1);
            finalStats.Suspension = Mathf.Clamp(finalStats.Suspension, 0, 1);
            Rigidbody.centerOfMass = Rigidbody.transform.InverseTransformPoint(CenterOfMass.position);
        }

        private void AnimateSuspension()
        {
            // simple suspension animation
            var suspensionTargetPos = suspensionNeutralPos;
            var suspensionTargetRot = suspensionNeutralRot;
            var bodyRot = transform.rotation.eulerAngles;

            var maxXTilt = finalStats.Suspension * 45;
            var closestNeutralRot = Mathf.Abs(360 - bodyRot.x) < Mathf.Abs(bodyRot.x) ? 360 : 0;
            var xTilt = Mathf.DeltaAngle(closestNeutralRot, bodyRot.x);

            var suspensionT = Mathf.InverseLerp(0, maxXTilt, xTilt);
            suspensionT = suspensionT * suspensionT;

            //Debug.Log("Suspension: " + suspensionT + " bodyRot: "  + bodyRot.x + " neutral: " + closestNeutralRot);
            bodyRot.x = Mathf.Lerp(closestNeutralRot, bodyRot.x, suspensionT);

            // transform bodyRot to suspension local space
            suspensionTargetRot = Quaternion.Inverse(SuspensionBody.transform.rotation) * Quaternion.Euler(bodyRot);

            // apply the new transforms
            SuspensionBody.transform.localPosition = Vector3.Lerp(SuspensionBody.transform.localPosition,
                suspensionTargetPos, Time.deltaTime * 5f);
            SuspensionBody.transform.localRotation = Quaternion.Slerp(SuspensionBody.transform.localRotation,
                suspensionTargetRot, Time.deltaTime * 5f);
        }

        private int CountGroundedWheels(out float minHeight)
        {
            var groundedCount = 0;
            minHeight = float.MaxValue;

            for (var i = 0; i < Wheels.Length; i++)
            {
                var current = Wheels[i];
                groundedCount += Physics.Raycast(current.position, Vector3.down, out var hit, RaycastDist)
                    ? 1
                    : 0;

                if (hit.distance > 0) minHeight = Mathf.Min(hit.distance, minHeight);
            }

            return groundedCount;
        }

        private void GroundVehicle(float minHeight)
        {
            if (GroundPercent >= 1f)
                if (minHeight < MinHeightThreshold)
                {
                    var diff = MinHeightThreshold - minHeight;
                    transform.position += diff * transform.up;
                }
        }

        private void GroundAirbourne()
        {
            // while in the air, fall faster
            if (AirPercent >= 1) Rigidbody.velocity += Physics.gravity * Time.deltaTime * finalStats.AddedGravity;
        }

        private void MoveVehicle(float accelInput, float turnInput)
        {
            // manual acceleration curve coefficient scalar
            float accelerationCurveCoeff = 5;
            var localVel = transform.InverseTransformVector(Rigidbody.velocity);

            var accelDirectionIsFwd = accelInput >= 0;
            var localVelDirectionIsFwd = localVel.z >= 0;

            // use the max speed for the direction we are going--forward or reverse.
            var maxSpeed = accelDirectionIsFwd ? finalStats.TopSpeed : finalStats.ReverseSpeed;
            var accelPower = accelDirectionIsFwd ? finalStats.Acceleration : finalStats.ReverseAcceleration;

            var accelRampT = Rigidbody.velocity.magnitude / maxSpeed;
            var multipliedAccelerationCurve = finalStats.AccelerationCurve * accelerationCurveCoeff;
            var accelRamp = Mathf.Lerp(multipliedAccelerationCurve, 1, accelRampT * accelRampT);

            var isBraking = accelDirectionIsFwd != localVelDirectionIsFwd;

            // if we are braking (moving reverse to where we are going)
            // use the braking accleration instead
            var finalAccelPower = isBraking ? finalStats.Braking : accelPower;

            var finalAcceleration = finalAccelPower * accelRamp;

            // apply inputs to forward/backward
            var turningPower = turnInput * finalStats.Steer;

            var turnAngle = Quaternion.AngleAxis(turningPower, Rigidbody.transform.up);
            var fwd = turnAngle * Rigidbody.transform.forward;

            var movement = fwd * accelInput * finalAcceleration * GroundPercent;

            // simple suspension allows us to thrust forward even when on bumpy terrain
            fwd.y = Mathf.Lerp(fwd.y, 0, finalStats.Suspension);

            // forward movement
            var currentSpeed = Rigidbody.velocity.magnitude;
            var wasOverMaxSpeed = currentSpeed >= maxSpeed;

            // if over max speed, cannot accelerate faster.
            if (wasOverMaxSpeed && !isBraking) movement *= 0;

            var adjustedVelocity = Rigidbody.velocity + movement * Time.deltaTime;

            adjustedVelocity.y = Rigidbody.velocity.y;

            //  clamp max speed if we are on ground
            if (GroundPercent > 0)
                if (adjustedVelocity.magnitude > maxSpeed && !wasOverMaxSpeed)
                    adjustedVelocity = Vector3.ClampMagnitude(adjustedVelocity, maxSpeed);

            // coasting is when we aren't touching accelerate
            var isCoasting = Mathf.Abs(accelInput) < .01f;

            if (isCoasting)
            {
                var restVelocity = new Vector3(0, Rigidbody.velocity.y, 0);
                adjustedVelocity = Vector3.MoveTowards(adjustedVelocity, restVelocity,
                    Time.deltaTime * finalStats.CoastingDrag);
            }

            Rigidbody.velocity = adjustedVelocity;

            ApplyAngularSuspension();

            if (GroundPercent > 0)
            {
                // manual angular velocity coefficient
                var angularVelocitySteering = .4f;
                var angularVelocitySmoothSpeed = 20f;

                // turning is reversed if we're going in reverse and pressing reverse
                if (!localVelDirectionIsFwd && !accelDirectionIsFwd) angularVelocitySteering *= -1;
                var angularVel = Rigidbody.angularVelocity;

                // move the Y angular velocity towards our target
                angularVel.y = Mathf.MoveTowards(angularVel.y, turningPower * angularVelocitySteering,
                    Time.deltaTime * angularVelocitySmoothSpeed);

                // apply the angular velocity
                Rigidbody.angularVelocity = angularVel;

                // rotate rigidbody's velocity as well to generate immediate velocity redirection
                // manual velocity steering coefficient
                var velocitySteering = 25f;
                // rotate our velocity based on current steer value
                Rigidbody.velocity = Quaternion.Euler(0f, turningPower * velocitySteering * Time.deltaTime, 0f) *
                                     Rigidbody.velocity;
            }

            // apply simplified lateral ground friction
            // only apply if we are on the ground at all
            if (GroundPercent > 0f)
            {
                // manual grip coefficient scalar
                var gripCoeff = 30f;
                // what direction is our lateral friction in?
                // it is the direction the wheels are turned, our forward
                var latFrictionDirection = Vector3.Cross(fwd, transform.up);
                // how fast are we currently moving in our friction direction?
                var latSpeed = Vector3.Dot(Rigidbody.velocity, latFrictionDirection);
                // apply the damping
                var latFrictionDampedVelocity = Rigidbody.velocity -
                                                latFrictionDirection * latSpeed * finalStats.Grip * gripCoeff *
                                                Time.deltaTime;

                // apply the damped velocity
                Rigidbody.velocity = latFrictionDampedVelocity;
            }
        }

        private void ApplyAngularSuspension()
        {
            // simple suspension dampens x and z angular velocity while on the ground
            var suspendedX = transform.right;
            var suspendedZ = transform.forward;
            suspendedX.y *= 0f;
            suspendedZ.y *= 0f;
            var sX = Vector3.Dot(Rigidbody.angularVelocity, suspendedX) * suspendedX;
            var sZ = Vector3.Dot(Rigidbody.angularVelocity, suspendedZ) * suspendedZ;
            var sXZ = sX + sZ;
            var sCoeff = 10f;

            Vector3 suspensionRotation;
            var minimumSuspension = 0.5f;
            if (GroundPercent > 0.5f || finalStats.Suspension < minimumSuspension)
                suspensionRotation = sXZ * finalStats.Suspension * sCoeff * Time.deltaTime;
            else
                suspensionRotation = sXZ * minimumSuspension * sCoeff * Time.deltaTime;

            var suspendedAngular = Rigidbody.angularVelocity - suspensionRotation;

            // apply the adjusted angularvelocity
            Rigidbody.angularVelocity = suspendedAngular;
        }

        private bool IsStuck()
        {
            var speed = Rigidbody.velocity.magnitude;
            if (GroundPercent <= 0 && speed < 0.01f && Mathf.Abs(Input.y) > 0)
                return true;

            return false;
        }

        public float LocalSpeed()
        {
            if (canMove)
            {
                var dot = Vector3.Dot(transform.forward, Rigidbody.velocity);
                if (Mathf.Abs(dot) > 0.1f)
                {
                    var speed = Rigidbody.velocity.magnitude;
                    return dot < 0 ? -(speed / finalStats.ReverseSpeed) : speed / finalStats.TopSpeed;
                }

                return 0f;
            }

            // use this value to play kart sound when it is waiting the race start countdown.
            return Input.y;
        }
    }
}