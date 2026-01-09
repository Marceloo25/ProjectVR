using UnityEngine;

public class PhysicRig : MonoBehaviour
{
    public Transform playerHead;
    public Transform leftController;
    public Transform rightController;


    public ConfigurableJoint headJoint;
    public ConfigurableJoint leftHandJoint;
    public ConfigurableJoint rightHandJoint;

    public Transform bodyCollider;
    public float bodyHeightMin = 0.5f;
    public float bodyHeightMax = 2.0f;

    //VR
    private CapsuleCollider capsuleCollider;

    //PC
    //private CharacterController characterController;

    void Start()
    {
        if (bodyCollider != null)
        {
            capsuleCollider = bodyCollider.GetComponent<CapsuleCollider>();

            //PC
            //if (capsuleCollider == null)
            //{
                //characterController = bodyCollider.GetComponent<CharacterController>();
            //}
        }
    }

    void FixedUpdate()
    {
        if (playerHead == null) return;

        float targetHeight = Mathf.Clamp(playerHead.localPosition.y, bodyHeightMin, bodyHeightMax);
        Vector3 targetCenter = new Vector3(playerHead.localPosition.x, targetHeight / 2, playerHead.localPosition.z);

        //VR
        if (capsuleCollider != null)
        {
            capsuleCollider.height = targetHeight;
            capsuleCollider.center = targetCenter;

            // Update joints
            if (headJoint != null)
            {
                headJoint.targetPosition = playerHead.localPosition;
            }
            if (leftHandJoint != null && leftController != null)
            {
                leftHandJoint.targetPosition = leftController.localPosition;
                leftHandJoint.targetRotation = leftController.localRotation;
            }
            if (rightHandJoint != null && rightController != null)
            {
                rightHandJoint.targetPosition = rightController.localPosition;
                rightHandJoint.targetRotation = rightController.localRotation;
            }
        }

        // PC
        //else if (characterController != null)
        //{
        //    characterController.height = targetHeight;
        //    characterController.center = targetCenter;
        //}
    }
}
