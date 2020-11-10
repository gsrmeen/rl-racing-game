using System;
using UnityEngine;

namespace KartGame.KartSystems
{
    public class KartMovement : MonoBehaviour
    {
        public Vector2 InputVector => _inputVector;
        public float GroundPercent { get; private set; }

        [SerializeField] private Transform _centerOfMass;
        [SerializeField] private Transform[] _wheels;
        [SerializeField] private Transform _suspensionBody;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private SuspensionAnimator _suspensionAnimator;

        private KartStats _stats;
        private Vector2 _inputVector;
        private bool _canMove = true;
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

        private void Start()
        {
            _stats = Registry.ProjectSettings.kartStats;
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

            _stats.Grip = Mathf.Clamp(_stats.Grip, 0, 1);
            _stats.Suspension = Mathf.Clamp(_stats.Suspension, 0, 1);
            _rb.centerOfMass = _rb.transform.InverseTransformPoint(_centerOfMass.position);

            var groundedCount = CountGroundedWheels(out var minHeight);
            GroundPercent = (float) groundedCount / _wheels.Length;
            var accel = _inputVector.y;
            var turn = _inputVector.x;

            GroundVehicle(minHeight);
            if (_canMove) MoveVehicle(accel, turn);

            GroundAirbourne();
            _suspensionAnimator.AnimateSuspension(_suspensionNeutralPos, _stats, _suspensionBody);
        }

        private void GatherInputs()
        {
            _inputVector = Vector2.zero;
            var current = _input.RetrieveMovementInput();
            if (current.sqrMagnitude > 0)
                _inputVector = current;
        }


        private int CountGroundedWheels(out float minHeight)
        {
            var groundedCount = 0;
            minHeight = float.MaxValue;

            for (var i = 0; i < _wheels.Length; i++)
            {
                var current = _wheels[i];
                var raycastHit = Physics.Raycast(current.position, Vector3.down, out var hit, _stats.RaycastDist);
                groundedCount += raycastHit ? 1 : 0;
                if (hit.distance > 0) minHeight = Mathf.Min(hit.distance, minHeight);
            }

            return groundedCount;
        }

        private void GroundVehicle(float minHeight)
        {
            if (GroundPercent < 1f || minHeight >= _stats.MinHeightThreshold) return;

            var diff = _stats.MinHeightThreshold - minHeight;
            transform.position += diff * transform.up;
        }

        private void GroundAirbourne()
        {
            if (_airPercent < 1f) return;

            _rb.velocity += Physics.gravity * (Time.deltaTime * _stats.AddedGravity);
        }

        private void MoveVehicle(float accelInput, float turnInput)
        {
            // manual acceleration curve coefficient scalar
            float accelerationCurveCoeff = 5;
            var localVel = transform.InverseTransformVector(_rb.velocity);

            var accelDirectionIsFwd = accelInput >= 0;
            var localVelDirectionIsFwd = localVel.z >= 0;

            // use the max speed for the direction we are going--forward or reverse.
            var maxSpeed = accelDirectionIsFwd ? _stats.TopSpeed : _stats.ReverseSpeed;
            var accelPower = accelDirectionIsFwd ? _stats.Acceleration : _stats.ReverseAcceleration;

            var accelRampT = _rb.velocity.magnitude / maxSpeed;
            var multipliedAccelerationCurve = _stats.AccelerationCurve * accelerationCurveCoeff;
            var accelRamp = Mathf.Lerp(multipliedAccelerationCurve, 1, accelRampT * accelRampT);

            var isBraking = accelDirectionIsFwd != localVelDirectionIsFwd;

            var finalAccelPower = isBraking ? _stats.Braking : accelPower;
            var finalAcceleration = finalAccelPower * accelRamp;
            var turningPower = turnInput * _stats.Steer;
            var turnAngle = Quaternion.AngleAxis(turningPower, _rb.transform.up);
            var fwd = turnAngle * _rb.transform.forward;
            var movement = fwd * accelInput * finalAcceleration * GroundPercent;

            // simple suspension allows us to thrust forward even when on bumpy terrain
            fwd.y = Mathf.Lerp(fwd.y, 0, _stats.Suspension);

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
                    Time.deltaTime * _stats.CoastingDrag);
            }

            _rb.velocity = adjustedVelocity;

            ApplyAngularSuspension();

            if (GroundPercent > 0)
            {
                var angularVelocitySteering = .4f;
                var angularVelocitySmoothSpeed = 20f;
                if (!localVelDirectionIsFwd && !accelDirectionIsFwd) angularVelocitySteering *= -1;
                var angularVel = _rb.angularVelocity;
                angularVel.y = Mathf.MoveTowards(angularVel.y, turningPower * angularVelocitySteering,
                    Time.deltaTime * angularVelocitySmoothSpeed);
                _rb.angularVelocity = angularVel;
                var velocitySteering = 25f;
                _rb.velocity = Quaternion.Euler(0f, turningPower * velocitySteering * Time.deltaTime, 0f) *
                               _rb.velocity;
            }

            if (GroundPercent <= 0f) return;

            var gripCoeff = 30f;
            var latFrictionDirection = Vector3.Cross(fwd, transform.up);
            var latSpeed = Vector3.Dot(_rb.velocity, latFrictionDirection);
            var dampened = latFrictionDirection * (latSpeed * _stats.Grip * gripCoeff * Time.deltaTime);
            var latFrictionDampedVelocity = _rb.velocity - dampened;
            _rb.velocity = latFrictionDampedVelocity;
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
            if (GroundPercent > 0.5f || _stats.Suspension < minimumSuspension)
                suspensionRotation = sXZ * _stats.Suspension * sCoeff * Time.deltaTime;
            else
                suspensionRotation = sXZ * minimumSuspension * sCoeff * Time.deltaTime;
            var suspendedAngular = _rb.angularVelocity - suspensionRotation;
            _rb.angularVelocity = suspendedAngular;
        }

        public float LocalSpeed()
        {
            if (_canMove == false) return _inputVector.y;

            var dot = Vector3.Dot(transform.forward, _rb.velocity);
            if (Mathf.Abs(dot) <= 0.1f) return 0f;

            var speed = _rb.velocity.magnitude;
            return dot < 0 ? -(speed / _stats.ReverseSpeed) : speed / _stats.TopSpeed;
        }
    }
}