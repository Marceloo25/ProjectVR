using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Fishing : MonoBehaviour
{
    enum FishingState
    {
        Idle,
        Casting,
        BobberInFlight,
        BobberOnWaterWaiting,
        BobberBittenReadyToHook,
        Hooked,
    }

    [Header("References")]
    [Tooltip("XRGrabInteractable on the rod. If left empty, will try to find one on this object.")]
    [SerializeField] XRGrabInteractable m_RodGrab;

    [Tooltip("The bobber/sphere child object to throw.")]
    [SerializeField] Rigidbody m_BobberRigidbody;

    [Tooltip("Where the bobber sits when attached/reset.")]
    [SerializeField] Transform m_BobberRestTransform;

    [Tooltip("What transform to track while calculating throw velocity. Usually the bobber while held.")]
    [SerializeField] Transform m_VelocitySource;

    [Header("Cast")]
    [SerializeField, Min(1)] int m_VelocitySampleFrames = 6;
    [SerializeField] float m_MaxThrowSpeed = 12f;
    [Tooltip("Optional multiplier applied to the sampled velocity when throwing.")]
    [SerializeField] float m_ThrowSpeedMultiplier = 1.0f;

    [Header("Water / Bite")]
    [Tooltip("Exact name of the water object the bobber should stick to.")]
    [SerializeField] string m_WaterObjectName = "Water";
    [SerializeField, Min(0f)] float m_BiteDelaySeconds = 1.0f;
    [SerializeField, Min(0f)] float m_BiteDipMeters = 0.08f;

    FishingState m_State = FishingState.Idle;
    bool m_IsTriggerHeld;

    Vector3[] m_VelocitySamples;
    int m_SampleWriteIndex;
    int m_SampleCount;
    Vector3 m_LastSamplePosition;
    float m_LastSampleTime;

    Transform m_OriginalBobberParent;
    Coroutine m_BiteRoutine;
    Vector3 m_BobberWaterLockPosition;

    void Awake()
    {
        if (m_RodGrab == null)
            TryGetComponent(out m_RodGrab);

        if (m_BobberRigidbody == null)
        {
            // Common setup: bobber is a child with a Rigidbody.
            m_BobberRigidbody = GetComponentInChildren<Rigidbody>();
        }

        if (m_BobberRigidbody == null)
        {
            Debug.LogError("Fishing: Missing bobber Rigidbody reference.", this);
            enabled = false;
            return;
        }

        if (m_BobberRestTransform == null)
            m_BobberRestTransform = m_BobberRigidbody.transform;

        if (m_VelocitySource == null)
            m_VelocitySource = m_BobberRigidbody.transform;

        m_OriginalBobberParent = m_BobberRigidbody.transform.parent;

        EnsureBobberProxy();
        EnsureVelocityBuffer();
        ResetBobberToRest();
    }

    void OnEnable()
    {
        if (m_RodGrab != null)
        {
            m_RodGrab.activated.AddListener(OnTriggerPressed);
            m_RodGrab.deactivated.AddListener(OnTriggerReleased);
        }
    }

    void OnDisable()
    {
        if (m_RodGrab != null)
        {
            m_RodGrab.activated.RemoveListener(OnTriggerPressed);
            m_RodGrab.deactivated.RemoveListener(OnTriggerReleased);
        }
    }

    void Update()
    {
        if (m_State == FishingState.Casting && m_IsTriggerHeld)
        {
            SampleVelocity();
        }

        // If we're locking the bobber to the water, keep it fixed.
        if (m_State == FishingState.BobberOnWaterWaiting || m_State == FishingState.BobberBittenReadyToHook)
        {
            m_BobberRigidbody.transform.position = m_BobberWaterLockPosition;
        }
    }

    void OnTriggerPressed(ActivateEventArgs args)
    {
        if (m_RodGrab == null || !m_RodGrab.isSelected)
            return;

        // Hook if a bite has happened.
        if (m_State == FishingState.BobberBittenReadyToHook)
        {
            m_State = FishingState.Hooked;
            // Minimal hook feedback: pop the bobber back up a little.
            m_BobberWaterLockPosition += Vector3.up * (m_BiteDipMeters * 0.75f);
            return;
        }

        // Start cast only when bobber is at rest/idle.
        if (m_State != FishingState.Idle)
            return;

        m_IsTriggerHeld = true;
        m_State = FishingState.Casting;
        ResetVelocitySampling();
    }

    void OnTriggerReleased(DeactivateEventArgs args)
    {
        if (!m_IsTriggerHeld)
            return;

        m_IsTriggerHeld = false;

        if (m_State == FishingState.Casting)
        {
            ThrowBobber(GetSmoothedVelocity() * m_ThrowSpeedMultiplier);
        }
    }

    void ThrowBobber(Vector3 throwVelocity)
    {
        if (m_BiteRoutine != null)
        {
            StopCoroutine(m_BiteRoutine);
            m_BiteRoutine = null;
        }

        m_State = FishingState.BobberInFlight;

        // Detach from the rod, enable physics, apply velocity.
        m_BobberRigidbody.transform.parent = null;
        m_BobberRigidbody.isKinematic = false;
        m_BobberRigidbody.useGravity = true;
        m_BobberRigidbody.linearVelocity = throwVelocity;
        m_BobberRigidbody.angularVelocity = Vector3.zero;
    }

    void OnBobberHitWater(GameObject waterObject)
    {
        if (m_State != FishingState.BobberInFlight)
            return;

        if (waterObject == null || waterObject.name != m_WaterObjectName)
            return;

        // Lock in place.
        m_BobberRigidbody.linearVelocity = Vector3.zero;
        m_BobberRigidbody.angularVelocity = Vector3.zero;
        m_BobberRigidbody.isKinematic = true;
        m_BobberRigidbody.useGravity = false;

        m_BobberWaterLockPosition = m_BobberRigidbody.transform.position;
        m_State = FishingState.BobberOnWaterWaiting;

        m_BiteRoutine = StartCoroutine(BiteRoutine());
    }

    IEnumerator BiteRoutine()
    {
        yield return new WaitForSeconds(m_BiteDelaySeconds);

        if (m_State != FishingState.BobberOnWaterWaiting)
            yield break;

        // "Bite": dip down a little.
        m_BobberWaterLockPosition += Vector3.down * m_BiteDipMeters;
        m_State = FishingState.BobberBittenReadyToHook;
    }

    void ResetBobberToRest()
    {
        if (m_BiteRoutine != null)
        {
            StopCoroutine(m_BiteRoutine);
            m_BiteRoutine = null;
        }

        m_IsTriggerHeld = false;
        m_State = FishingState.Idle;

        m_BobberRigidbody.linearVelocity = Vector3.zero;
        m_BobberRigidbody.angularVelocity = Vector3.zero;
        m_BobberRigidbody.isKinematic = true;
        m_BobberRigidbody.useGravity = false;

        // Reattach to rod.
        if (m_OriginalBobberParent != null)
            m_BobberRigidbody.transform.parent = m_OriginalBobberParent;
        else
            m_BobberRigidbody.transform.parent = transform;

        m_BobberRigidbody.transform.SetPositionAndRotation(m_BobberRestTransform.position, m_BobberRestTransform.rotation);
    }

    void EnsureVelocityBuffer()
    {
        var size = Mathf.Max(1, m_VelocitySampleFrames);
        if (m_VelocitySamples == null || m_VelocitySamples.Length != size)
            m_VelocitySamples = new Vector3[size];
    }

    void ResetVelocitySampling()
    {
        EnsureVelocityBuffer();
        m_SampleWriteIndex = 0;
        m_SampleCount = 0;
        for (var i = 0; i < m_VelocitySamples.Length; i++)
            m_VelocitySamples[i] = Vector3.zero;

        m_LastSamplePosition = m_VelocitySource.position;
        m_LastSampleTime = Time.time;
    }

    void SampleVelocity()
    {
        var now = Time.time;
        var dt = now - m_LastSampleTime;
        if (dt <= Mathf.Epsilon)
            return;

        var current = m_VelocitySource.position;
        var frameVelocity = (current - m_LastSamplePosition) / dt;

        var clamped = Vector3.ClampMagnitude(frameVelocity, m_MaxThrowSpeed);
        m_VelocitySamples[m_SampleWriteIndex] = clamped;
        m_SampleWriteIndex = (m_SampleWriteIndex + 1) % m_VelocitySamples.Length;
        m_SampleCount = Mathf.Min(m_SampleCount + 1, m_VelocitySamples.Length);

        m_LastSamplePosition = current;
        m_LastSampleTime = now;
    }

    Vector3 GetSmoothedVelocity()
    {
        if (m_SampleCount <= 0)
            return Vector3.zero;

        var sum = Vector3.zero;
        for (var i = 0; i < m_SampleCount; i++)
            sum += m_VelocitySamples[i];

        return sum / m_SampleCount;
    }

    void EnsureBobberProxy()
    {
        var proxy = m_BobberRigidbody.GetComponent<FishingBobberProxy>();
        if (proxy == null)
            proxy = m_BobberRigidbody.gameObject.AddComponent<FishingBobberProxy>();

        proxy.Init(this);
    }

    // This lives on the bobber so we actually receive physics callbacks.
    class FishingBobberProxy : MonoBehaviour
    {
        Fishing m_Owner;

        public void Init(Fishing owner)
        {
            m_Owner = owner;
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Owner == null)
                return;
            m_Owner.OnBobberHitWater(other.gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (m_Owner == null)
                return;
            m_Owner.OnBobberHitWater(collision.gameObject);
        }
    }
}
