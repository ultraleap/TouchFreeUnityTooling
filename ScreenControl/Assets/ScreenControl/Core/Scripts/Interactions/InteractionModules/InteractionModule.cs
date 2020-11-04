﻿using UnityEngine;

namespace Ultraleap.ScreenControl.Core
{
    public class InteractionModule : MonoBehaviour
    {
        public virtual ScreenControlTypes.InteractionType InteractionType { get; } = ScreenControlTypes.InteractionType.Undefined;

        private ScreenControlTypes.HandChirality handChirality = ScreenControlTypes.HandChirality.UNKNOWN;
        public ScreenControlTypes.HandType handType;

        public bool ignoreDragging;
        public PositioningModule positioningModule;

        public delegate void InputAction(ScreenControlTypes.HandChirality _chirality, ScreenControlTypes.HandType _handType, ScreenControlTypes.InputActionData _inputData);
        public static event InputAction HandleInputAction;

        protected Positions positions;

        protected long latestTimestamp;

        void Update()
        {
            // Obtain the relevant Hand Data from the HandManager, and call the main UpdateData function
            latestTimestamp = HandManager.Instance.Timestamp;

            Leap.Hand hand = null;

            switch (handType)
            {
                case ScreenControlTypes.HandType.PRIMARY:

                    hand = HandManager.Instance.PrimaryHand;
                    break;
                case ScreenControlTypes.HandType.SECONDARY:
                    hand = HandManager.Instance.SecondaryHand;
                    break;
            }

            if (hand != null)
            {
                handChirality = hand.IsLeft ? ScreenControlTypes.HandChirality.LEFT : ScreenControlTypes.HandChirality.RIGHT;
            }

            UpdateData(hand);
        }

        // This is the main update loop of the interaction module
        protected virtual void UpdateData(Leap.Hand hand) { }

        protected void SendInputAction(ScreenControlTypes.InputType _inputType, Positions _positions, float _progressToClick)
        {
            ScreenControlTypes.InputActionData actionData = new ScreenControlTypes.InputActionData(latestTimestamp, InteractionType, handType, handChirality, _inputType, _positions, _progressToClick);
            HandleInputAction?.Invoke(handChirality, handType, actionData);
        }

        protected virtual void OnEnable()
        {
            SettingsConfig.OnConfigUpdated += OnSettingsUpdated;
            OnSettingsUpdated();
            PhysicalConfigurable.CreateVirtualScreen(PhysicalConfigurable.Config);
            positioningModule.Stabiliser.ResetValues();
        }

        protected virtual void OnDisable()
        {
            SettingsConfig.OnConfigUpdated -= OnSettingsUpdated;
        }

        protected virtual void OnSettingsUpdated()
        {
            ignoreDragging = !SettingsConfig.Config.UseScrollingOrDragging;
        }
    }
}