using UnityEngine;
using UnityEngine.Assertions;

namespace KartGame.KartSystems
{
    public class KartPlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _playerAnimator;
        [SerializeField] private KartMovement _kartMovement;

        private readonly string _groundedParam = "Grounded";
        private readonly string _steeringParam = "Steering";
        private int _steerHash;
        private int _groundHash;
        private float _steeringSmoother;

        private void Awake()
        {
            Assert.IsNotNull(_kartMovement, "No ArcadeKart found!");
            Assert.IsNotNull(_playerAnimator, "No PlayerAnimator found!");
            _steerHash = Animator.StringToHash(_steeringParam);
            _groundHash = Animator.StringToHash(_groundedParam);
        }

        private void Update()
        {
            _steeringSmoother = Mathf.Lerp(_steeringSmoother, _kartMovement.InputVector.x, Time.deltaTime * 5f);
            _playerAnimator.SetFloat(_steerHash, _steeringSmoother);
            _playerAnimator.SetBool(_groundHash, _kartMovement.GroundPercent >= 0.5f);
        }
    }
}