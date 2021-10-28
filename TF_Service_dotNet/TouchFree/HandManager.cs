﻿using Ultraleap.TouchFree.Library.Configuration;
using System;
using Leap;


namespace Ultraleap.TouchFree.Library
{
    public class HandManager
    {
        public long Timestamp { get; private set; }

        // The PrimaryHand is the hand that appeared first. It does not change until tracking on it is lost.
        public Hand PrimaryHand;
        public HandChirality primaryChirality;

        // The SecondaryHand is the second hand that appears. It may be promoted to the PrimaryHand if the
        // PrimaryHand is lost.
        public Hand SecondaryHand;
        public HandChirality secondaryChirality;

        public event Action HandFound;
        public event Action HandsLost;
        public delegate void HandUpdate(Hand primary, Hand secondary);
        public event HandUpdate HandsUpdated;

        bool PrimaryIsLeft => PrimaryHand != null && PrimaryHand.IsLeft;
        bool PrimaryIsRight => PrimaryHand != null && !PrimaryHand.IsLeft;
        bool SecondaryIsLeft => SecondaryHand != null && SecondaryHand.IsLeft;
        bool SecondaryIsRight => SecondaryHand != null && !SecondaryHand.IsLeft;

        public Hand LeftHand
        {
            get
            {
                if (PrimaryIsLeft)
                {
                    return PrimaryHand;
                }
                else if (SecondaryIsLeft)
                {
                    return SecondaryHand;
                }
                else
                {
                    return null;
                }
            }
        }

        public Hand RightHand
        {
            get
            {
                if (PrimaryIsRight)
                {
                    return PrimaryHand;
                }
                else if (SecondaryIsRight)
                {
                    return SecondaryHand;
                }
                else
                {
                    return null;
                }
            }
        }

        private LeapTransform trackingTransform;

        private TrackingConnectionManager trackingProvider;

        private int handsLastFrame;


        public HandManager(TrackingConnectionManager _trackingManager)
        {
            handsLastFrame = 0;

            trackingProvider = _trackingManager;
            if (trackingProvider != null)
            {
                trackingProvider.controller.FrameReady += Update;
            }
            
            ConfigManager.PhysicalConfig.OnConfigUpdated += UpdateTrackingTransform;
            UpdateTrackingTransform(ConfigManager.PhysicalConfig);
        }

        public void UpdateTrackingTransform(BaseConfig _config)
        {
            PhysicalConfig config = _config as PhysicalConfig;

            // To simplify the configuration values, positive X angles tilt the Leap towards the screen no matter how its mounted.
            // Therefore, we must convert to the real values before using them.
            // If bottom mounted, the X rotation should be negative if tilted towards the screen so we must negate the X rotation in this instance.
            var isTopMounted = ((config.LeapRotationD.Z > 179.9f) && (config.LeapRotationD.Z < 180.1f));
            float xAngleDegree = isTopMounted ? config.LeapRotationD.X : -config.LeapRotationD.X;

            System.Numerics.Quaternion quaternion = System.Numerics.Quaternion.CreateFromYawPitchRoll(VirtualScreen.DegreesToRadians(config.LeapRotationD.Y),
                VirtualScreen.DegreesToRadians(xAngleDegree),
                VirtualScreen.DegreesToRadians(config.LeapRotationD.Z));

            trackingTransform = new LeapTransform(new Vector(config.LeapPositionRelativeToScreenBottomM.X,
                config.LeapPositionRelativeToScreenBottomM.Y,
                -config.LeapPositionRelativeToScreenBottomM.Z) * 1000, new
                LeapQuaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W));
        }

        public void Update(object sender, FrameEventArgs e)
        {
            var currentFrame = e.frame;
            var handCount = currentFrame.Hands.Count;

            if (handCount == 0 && handsLastFrame > 0)
            {
                HandsLost?.Invoke();
            }
            else if (handCount > 0 && handsLastFrame == 0)
            {
                HandFound?.Invoke();
            }

            handsLastFrame = handCount;

            currentFrame.Transform(trackingTransform);

            Timestamp = currentFrame.Timestamp;

            Hand leftHand = null;
            Hand rightHand = null;

            foreach (Hand hand in currentFrame.Hands)
            {
                if (hand.IsLeft)
                    leftHand = hand;
                else
                    rightHand = hand;
            }

            UpdateHandStatus(ref PrimaryHand, leftHand, rightHand, HandType.PRIMARY);
            UpdateHandStatus(ref SecondaryHand, leftHand, rightHand, HandType.SECONDARY);

            HandsUpdated?.Invoke(PrimaryHand, SecondaryHand);
        }

        void UpdateHandStatus(ref Hand _hand, Hand _left, Hand _right, HandType _handType)
        {
            // We must use the cached HandChirality to ensure persistence
            HandChirality handChirality;

            if (_handType == HandType.PRIMARY)
            {
                handChirality = primaryChirality;
            }
            else
            {
                handChirality = secondaryChirality;
            }

            if (_hand == null)
            {
                // Look for a new hand

                if (_handType == HandType.PRIMARY)
                {
                    AssignNewPrimary(_left, _right);
                }
                else
                {
                    AssignNewSecondary(_left, _right);
                }
            }
            else
            {
                // Check hand is still active

                if (handChirality == HandChirality.LEFT && _left != null)
                {
                    // Hand is still left
                    _hand = _left;
                    return;
                }
                else if (handChirality == HandChirality.RIGHT && _right != null)
                {
                    // Hand is still right
                    _hand = _right;
                    return;
                }

                // If we are here, the Hand has been lost. Assign a new Hand.
                if (_handType == HandType.PRIMARY)
                {
                    AssignNewPrimary(_left, _right);
                }
                else
                {
                    AssignNewSecondary(_left, _right);
                }
            }
        }

        void AssignNewPrimary(Hand _left, Hand _right)
        {
            // When assigning a new primary, we should force Secondary to be re-assigned too
            PrimaryHand = null;
            SecondaryHand = null;

            if (_right != null)
            {
                PrimaryHand = _right;
                primaryChirality = HandChirality.RIGHT;
            }
            else if (_left != null)
            {
                PrimaryHand = _left;
                primaryChirality = HandChirality.LEFT;
            }
        }

        void AssignNewSecondary(Hand _left, Hand _right)
        {
            SecondaryHand = null;

            if (_right != null && primaryChirality != HandChirality.RIGHT)
            {
                SecondaryHand = _right;
                secondaryChirality = HandChirality.RIGHT;
            }
            else if (_left != null && primaryChirality != HandChirality.LEFT)
            {
                SecondaryHand = _left;
                secondaryChirality = HandChirality.LEFT;
            }
        }

        public LeapTransform TrackingTransform()
        {
            return trackingTransform;
        }
    }
}