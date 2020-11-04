using System;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    public class KartMovement : MonoBehaviour
    {
        public Vector2 InputVector => _inputVector;
        public float GroundPercent { get; private set; }

        public KartStats baseStats = KartStats.GetDefaults();

        [SerializeField] private Transform _centerOfMass;
        [SerializeField] private Transform[] _wheels;
        [SerializeField] private Transform _suspensionBody;
        [SerializeField] private Rigidbody _rb;


        public float RaycastDist = 0.3f;
        public float MinHeightThreshold = 0.02f;

        private Vector2 _inputVector;
        private bool _canMove = true;
        private KartStats _finalStats;
        private IInput _input;
        private Vector3 _suspensionNeutralPos;
        private float _airPercent => 1f - GroundPercent;



        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _input = GetComponent<IInput>();
            var bodyTransform = _suspensionBody.transform;
            _suspensionNeutralPos = bodyTransform.localPosition;
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
            GroundPercent = (float) groundedCount / _wheels.Length;
            var accel = _inputVector.y;
            var turn = _inputVector.x;

            GroundVehicle(minHeight);
            if (_canMove) MoveVehicle(accel, turn);

            GroundAirbourne();
            AnimateSuspension();
        }

        private void GatherInputs()
        {
            _inputVector = Vector2.zero;
            var current = _input.GenerateInput();
            if (current.sqrMagnitude > 0) 
                _inputVector = current;
        }

        private void InitValues()
        {
            _finalStats = baseStats;
            _finalStats.Grip = Mathf.Clamp(_finalStats.Grip, 0, 1);
            _finalStats.Suspension = Mathf.Clamp(_finalStats.Suspension, 0, 1);
            _rb.centerOfMass = _rb.transform.InverseTransformPoint(_centerOfMass.position);
        }

        private void AnimateSuspension()
        {
            var suspensionTargetPos = _suspensionNeutralPos;
            var bodyRot = transform.rotation.eulerAngles;
            var maxXTilt = _finalStats.Suspension * 45;
            var closestNeutralRot = Mathf.Abs(360 - bodyRot.x) < Mathf.Abs(bodyRot.x) ? 360 : 0;
            var xTilt = Mathf.DeltaAngle(closestNeutralRot, bodyRot.x);
            var suspensionT = Mathf.InverseLerp(0, maxXTilt, xTilt);
            suspensionT *= suspensionT;
            bodyRot.x = Mathf.Lerp(closestNeutralRot, bodyRot.x, suspensionT);
            var suspensionTargetRot = Quaternion.Inverse(_suspensionBody.transform.rotation) * Quaternion.Euler(bodyRot);
            _suspensionBody.transform.localPosition = Vector3.Lerp(_suspensionBody.transform.localPosition,
                suspensionTargetPos, Time.deltaTime * 5f);
            _suspensionBody.transform.localRotation = Quaternion.Slerp(_suspensionBody.transform.localRotation,
                suspensionTargetRot, Time.deltaTime * 5f);
        }

        private int CountGroundedWheels(out float minHeight)
        {
            var groundedCount = 0;
            minHeight = float.MaxValue;

            for (var i = 0; i < _wheels.Length; i++)
            {
                var current = _wheels[i];
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
            if (_airPercent >= 1) _rb.velocity += Physics.gravity * Time.deltaTime * _finalStats.AddedGravity;
        }

        private void MoveVehicle(float accelInput, float turnInput)
        {
            // manual acceleration curve coefficient scalar
            float accelerationCurveCoeff = 5;
            var localVel = transform.InverseTransformVector(_rb.velocity);

            var accelDirectionIsFwd = accelInput >= 0;
            var localVelDirectionIsFwd = localVel.z >= 0;

            // use the max speed for the direction we are going--forward or reverse.
            var maxSpeed = accelDirectionIsFwd ? _finalStats.TopSpeed : _finalStats.ReverseSpeed;
            var accelPower = accelDirectionIsFwd ? _finalStats.Acceleration : _finalStats.ReverseAcceleration;

            var accelRampT = _rb.velocity.magnitude / maxSpeed;
            var multipliedAccelerationCurve = _finalStats.AccelerationCurve * accelerationCurveCoeff;
            var accelRamp = Mathf.Lerp(multipliedAccelerationCurve, 1, accelRampT * accelRampT);

            var isBraking = accelDirectionIsFwd != localVelDirectionIsFwd;

            var finalAccelPower = isBraking ? _finalStats.Braking : accelPower;
            var finalAcceleration = finalAccelPower * accelRamp;
            var turningPower = turnInput * _finalStats.Steer;
            var turnAngle = Quaternion.AngleAxis(turningPower, _rb.transform.up);
            var fwd = turnAngle * _rb.transform.forward;
            var movement = fwd * accelInput * finalAcceleration * GroundPercent;

            // simple suspension allows us to thrust forward even when on bumpy terrain
            fwd.y = Mathf.Lerp(fwd.y, 0, _finalStats.Suspension);

            var currentSpeed = _rb.velocity.magnitude;
            var wasOverMaxSpeed = currentSpeed >= maxSpeed;

            if (wasOverMaxSpeed && !isBraking) movement *= 0;
            var adjustedVelocity = _rb.velocity + movement * Time.deltaTime;

            adjustedVelocity.y = _rb.velocity.y;

            if (GroundPercent > 0)
                if (adjustedVelocity.magnitude > maxSpeed && !wasOverMaxSpeed)
                    adjustedVelocity = Vector3.ClampMagnitude(adjustedVelocity, maxSpeed);

            var isCoasting = Mathf.Abs(accelInput) < .01f;
            if (isCoasting)
            {
                var restVelocity = new Vector3(0, _rb.velocity.y, 0);
                adjustedVelocity = Vector3.MoveTowards(adjustedVelocity, restVelocity,
                    Time.deltaTime * _finalStats.CoastingDrag);
            }

            _rb.velocity = adjustedVelocity;

            ApplyAngularSuspension();

            if (GroundPercent > 0)
            {
                // manual angular velocity coefficient
                var angularVelocitySteering = .4f;
                var angularVelocitySmoothSpeed = 20f;

                // turning is reversed if we're going in reverse and pressing reverse
                if (!localVelDirectionIsFwd && !accelDirectionIsFwd) angularVelocitySteering *= -1;
                var angularVel = _rb.angularVelocity;

                // move the Y angular velocity towards our target
                angularVel.y = Mathf.MoveTowards(angularVel.y, turningPower * angularVelocitySteering,
                    Time.deltaTime * angularVelocitySmoothSpeed);

                // apply the angular velocity
                _rb.angularVelocity = angularVel;

                // rotate rigidbody's velocity as well to generate immediate velocity redirection
                // manual velocity steering coefficient
                var velocitySteering = 25f;
                // rotate our velocity based on current steer value
                _rb.velocity = Quaternion.Euler(0f, turningPower * velocitySteering * Time.deltaTime, 0f) *
                               _rb.velocity;
            }

            if (GroundPercent > 0f)
            {
                var gripCoeff = 30f;
                var latFrictionDirection = Vector3.Cross(fwd, transform.up);
                var latSpeed = Vector3.Dot(_rb.velocity, latFrictionDirection);
                var latFrictionDampedVelocity = _rb.velocity -
                                                latFrictionDirection * latSpeed * _finalStats.Grip * gripCoeff *
                                                Time.deltaTime;
                _rb.velocity = latFrictionDampedVelocity;
            }
        }

        private void ApplyAngularSuspension()
        {
            var suspendedX = transform.right;
            var suspendedZ = transform.forward;
            suspendedX.y *= 0f;
            suspendedZ.y *= 0f;
            var sX = Vector3.Dot(_rb.angularVelocity, suspendedX) * suspendedX;
            var sZ = Vector3.Dot(_rb.angularVelocity, suspendedZ) * suspendedZ;
            var sXZ = sX + sZ;
            var sCoeff = 10f;
            Vector3 suspensionRotation;
            var minimumSuspension = 0.5f;
            if (GroundPercent > 0.5f || _finalStats.Suspension < minimumSuspension)
                suspensionRotation = sXZ * _finalStats.Suspension * sCoeff * Time.deltaTime;
            else
                suspensionRotation = sXZ * minimumSuspension * sCoeff * Time.deltaTime;
            var suspendedAngular = _rb.angularVelocity - suspensionRotation;
            _rb.angularVelocity = suspendedAngular;
        }

        public float LocalSpeed()
        {
            if (_canMove)
            {
                var dot = Vector3.Dot(transform.forward, _rb.velocity);
                if (Mathf.Abs(dot) > 0.1f)
                {
                    var speed = _rb.velocity.magnitude;
                    return dot < 0 ? -(speed / _finalStats.ReverseSpeed) : speed / _finalStats.TopSpeed;
                }

                return 0f;
            }

            return _inputVector.y;
        }
    }
}