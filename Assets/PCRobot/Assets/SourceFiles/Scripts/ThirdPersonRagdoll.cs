using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarterAssets
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonRagdoll : MonoBehaviour
    {
        [Tooltip("If true, the character is in ragdoll mode (Animator + CharacterController disabled, child rigidbodies enabled).")]
        [SerializeField] private bool ragdollActive;

        [Header("Optional")]
        [Tooltip("If assigned, this Animator is toggled when entering/exiting ragdoll. If not assigned, the first Animator in children is used.")]
        [SerializeField] private Animator animator;

        private ThirdPersonController _thirdPersonController;
        private CharacterController _characterController;

        private readonly List<Rigidbody> _ragdollBodies = new();
        private readonly List<Collider> _ragdollColliders = new();

        private bool _cached;
        private bool _originalThirdPersonEnabled;
        private bool _originalCharacterControllerEnabled;
        private bool _originalAnimatorEnabled;

        public bool RagdollActive
        {
            get => ragdollActive;
            set => SetRagdollActive(value);
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            _thirdPersonController = GetComponent<ThirdPersonController>();
            _characterController = GetComponent<CharacterController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            CollectRagdollParts();
            CacheOriginalStates();

            // Ensure we start in a consistent state.
            ApplyRagdollState(ragdollActive);
        }

        private void OnValidate()
        {
            // Avoid doing runtime state flips while editing, but keep the serialized value.
            if (!Application.isPlaying)
                return;

            if (_thirdPersonController == null) _thirdPersonController = GetComponent<ThirdPersonController>();
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);

            if (_ragdollBodies.Count == 0)
                CollectRagdollParts();

            ApplyRagdollState(ragdollActive);
        }

        public void SetRagdollActive(bool active)
        {
            if (ragdollActive == active)
                return;

            ragdollActive = active;

            if (!Application.isPlaying)
                return;

            ApplyRagdollState(active);
        }

        [ContextMenu("Enable Ragdoll")]
        private void ContextEnableRagdoll() => SetRagdollActive(true);

        [ContextMenu("Disable Ragdoll")]
        private void ContextDisableRagdoll() => SetRagdollActive(false);

        private void CacheOriginalStates()
        {
            if (_cached)
                return;

            _originalThirdPersonEnabled = _thirdPersonController != null && _thirdPersonController.enabled;
            _originalCharacterControllerEnabled = _characterController != null && _characterController.enabled;
            _originalAnimatorEnabled = animator != null && animator.enabled;

            _cached = true;
        }

        private void CollectRagdollParts()
        {
            _ragdollBodies.Clear();
            _ragdollColliders.Clear();

            var bodies = GetComponentsInChildren<Rigidbody>(true);
            foreach (var body in bodies)
            {
                if (body == null)
                    continue;

                // Ignore any Rigidbody on the root (and especially the one potentially co-located with the CharacterController).
                if (body.transform == transform)
                    continue;

                _ragdollBodies.Add(body);
            }

            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col == null)
                    continue;

                // Keep the CharacterController collider untouched.
                if (col is CharacterController)
                    continue;

                // Ignore colliders on the root object if present.
                if (col.transform == transform)
                    continue;

                _ragdollColliders.Add(col);
            }

            if (_ragdollBodies.Count == 0)
            {
                Debug.LogWarning("ThirdPersonRagdoll: No child Rigidbodies found. Ragdoll requires a rig with Rigidbodies + Colliders on bones.", this);
            }
        }

        private void ApplyRagdollState(bool active)
        {
            // Enter ragdoll: disable controller/animator, enable physics bodies.
            if (active)
            {
                CacheOriginalStates();

                if (_thirdPersonController != null)
                    _thirdPersonController.enabled = false;

                if (_characterController != null)
                    _characterController.enabled = false;

                if (animator != null)
                    animator.enabled = false;

                for (int i = 0; i < _ragdollColliders.Count; i++)
                {
                    if (_ragdollColliders[i] != null)
                        _ragdollColliders[i].enabled = true;
                }

                for (int i = 0; i < _ragdollBodies.Count; i++)
                {
                    var body = _ragdollBodies[i];
                    if (body == null) continue;

                    body.isKinematic = false;
                    body.useGravity = true;
                    body.detectCollisions = true;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                }

                return;
            }

            // Exit ragdoll: disable physics bodies, re-enable controller/animator.
            for (int i = 0; i < _ragdollBodies.Count; i++)
            {
                var body = _ragdollBodies[i];
                if (body == null) continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = true;
            }

            for (int i = 0; i < _ragdollColliders.Count; i++)
            {
                if (_ragdollColliders[i] != null)
                    _ragdollColliders[i].enabled = false;
            }

            if (animator != null)
                animator.enabled = _cached ? _originalAnimatorEnabled : true;

            if (_characterController != null)
                _characterController.enabled = _cached ? _originalCharacterControllerEnabled : true;

            if (_thirdPersonController != null)
                _thirdPersonController.enabled = _cached ? _originalThirdPersonEnabled : true;
        }
    }
}
