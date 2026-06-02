using GorillaLocomotion;
using System.Collections.Generic;
using UnityEngine;

namespace DogesPullMod
{
    public class Mods
    {
        public static float pullPower = 0.9f;


        public static bool leftGrab = false;
        public static bool rightGrab = false;
        private static bool scaleWithPlayer = false;

        public static float leftGrabFloat = 0f;
        public static float rightGrabFloat = 0f;

        private static readonly Dictionary<bool, bool> previousTouchingGround = new Dictionary<bool, bool>()
        {
            { true, false },   
            { false, false }  
        };

        public static void PullModUpdate()
        {
            ProcessPullHand(false);
            ProcessPullHand(true); 
        }

        public static void ProcessPullHand(bool left)
        {
            float grabValue = left ? leftGrabFloat : rightGrabFloat;

       
            if (grabValue < 0.1f)
                return;

            bool touchingGround = GTPlayer.Instance.IsHandTouching(left);
            previousTouchingGround.TryGetValue(left, out bool wasTouchingGround);

            if (!touchingGround && wasTouchingGround)
            {
                Vector3 handPos = left
                    ? GTPlayer.Instance.LastLeftHandPosition
                    : GTPlayer.Instance.LastRightHandPosition;

                Vector3 normal = Vector3.up;

                Vector3 vel = GorillaTagger.Instance.rigidbody.linearVelocity;
                if (Physics.Raycast(handPos, -vel.normalized, out RaycastHit hit, 0.3f))
                    normal = hit.normal;

                Vector3 direction = GorillaTagger.Instance.rigidbody.linearVelocity.X_Z();
                Vector3 tangent = direction - normal * Vector3.Dot(direction, normal);

   
                float strength = grabValue * (pullPower * 5f);

                GTPlayer.Instance.transform.position +=
                    tangent.normalized *
                    (direction.magnitude / GTPlayer.Instance.maxJumpSpeed * strength) *
                    (scaleWithPlayer ? GTPlayer.Instance.scale : 1f);
            }

            previousTouchingGround[left] = touchingGround;
        }
    }
}
