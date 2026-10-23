using EFT.Animations;
using UnityEngine;

namespace BreathFX.Breathing
{
    internal static class CustomBreathProcessor
    {
        private const float HoldSwayScale = 0.15f;
        private const float GaspSwayMultiplier = 1.2f;
        private const float GaspFrequencyMultiplier = 0.75f;

        internal static bool TryProcess(BreathEffector breath, BreathingController controller, float deltaTime)
        {
            if (breath == null || controller == null || !controller.IsActive)
            {
                return false;
            }

            if (!Settings.ReplaceWeaponSway.Value)
            {
                return false;
            }

            if (breath._processors == null || breath._processors.Length < 2)
            {
                return false;
            }

            if (breath.BreathParams == null
                || breath.BreathParams.AmplitudeCurve == null
                || breath.BreathParams.Delay == null
                || breath.BreathParams.Hardness == null
                || breath._handsRotationSpring == null
                || breath._shotEffector == null
                || breath._shotEffector.CurrentRecoilEffect == null
                || breath.StaminaLevel == null
                || breath.YRandom == null
                || breath.XRandom == null
                || breath.TremorXRandom == null
                || breath.TremorYRandom == null
                || breath.TremorZRandom == null
                || breath.HipXRandom == null
                || breath.HipZRandom == null)
            {
                return false;
            }

            float staminaLevel = breath.StaminaLevel.Value;
            if (breath.IsInMounting && staminaLevel > breath.EnergyLowerLimit && !breath.Fracture)
            {
                return true;
            }

            float amplitude = Mathf.Sqrt(breath.AmplitudeGain.Value);
            breath.HipXRandom.Amplitude = Mathf.Clamp(breath._baseHipRandomAmplitudes.x + amplitude, 0f, 3f);
            breath.HipZRandom.Amplitude = Mathf.Clamp(breath._baseHipRandomAmplitudes.y + amplitude, 0f, 3f);
            breath.HipXRandom.Hardness = (breath.HipZRandom.Hardness = breath.Hardness.Value);

            float mountingMultiplier;
            if (breath.IsInMounting)
            {
                if (breath.IsVerticalMounting)
                {
                    mountingMultiplier = breath.MountingVerticalOutOfBreathMultiplier;
                }
                else
                {
                    mountingMultiplier = breath.IsBipodUsed
                        ? breath.BipodOutOfBreathMultiplier
                        : breath.MountingHorizontalOutOfBreathMultiplier;
                }
            }
            else
            {
                mountingMultiplier = 1f;
            }

            float intensityMultiplier = breath.Intensity * mountingMultiplier;
            breath._shakeIntensity = 1f;
            bool tremorEnabled = breath.TremorOn || breath.Fracture;
            float tremorScale = 1f;
            breath._breathFrequency = deltaTime;

            if (Time.time < breath.StiffUntill)
            {
                float stiff = Mathf.Clamp(0f - breath.StiffUntill + Time.time + 1f, tremorEnabled ? 0.5f : 0.3f, 1f);
                breath._breathIntensity = stiff * intensityMultiplier;
                breath._shakeIntensity = stiff;
                tremorScale = stiff;
            }
            else if (controller.IsHolding)
            {
                breath._breathIntensity = HoldSwayScale;
                breath._shakeIntensity = HoldSwayScale;
            }
            else
            {
                float breathLevel = Mathf.Clamp01(controller.Breath);
                float curveValue = breath._lackOfOxygenStrength != null
                    ? Mathf.Clamp01(breath._lackOfOxygenStrength.Evaluate(breathLevel))
                    : breathLevel;

                float aimLimit = breath.IsAiming ? 0.75f : 1f;
                breath._breathIntensity = Mathf.Clamp(Mathf.Lerp(4f, aimLimit, curveValue), 1f, 1.5f) * intensityMultiplier;
                breath._breathFrequency = Mathf.Clamp(Mathf.Lerp(4f, 1f, curveValue), 1f, 2.5f) * deltaTime;
                breath._cameraSensetivity = Mathf.Lerp(2f, 0f, curveValue) * intensityMultiplier;

                if (controller.IsGasping)
                {
                    breath._breathIntensity *= GaspSwayMultiplier;
                    breath._breathFrequency *= GaspFrequencyMultiplier;
                    breath._cameraSensetivity *= GaspSwayMultiplier;
                }
            }

            breath.YRandom.Amplitude = breath.BreathParams.AmplitudeCurve.Evaluate(staminaLevel);
            float delay = breath.BreathParams.Delay.Evaluate(staminaLevel);
            breath.XRandom.MinMaxDelay = (breath.YRandom.MinMaxDelay = new Vector2(delay / 2f, delay));
            breath.YRandom.Hardness = breath.BreathParams.Hardness.Evaluate(staminaLevel);

            float yValue = breath.YRandom.GetValue(deltaTime);
            float xValue = breath.XRandom.GetValue(deltaTime);
            breath._handsRotationSpring.AddAcceleration(
                new Vector3(
                    Mathf.Max(0f, 0f - yValue) * (1f - staminaLevel) * 2f,
                    yValue,
                    xValue) * (breath._shakeIntensity * intensityMultiplier));

            Vector3 recoilOffset;
            if (!tremorEnabled)
            {
                recoilOffset = breath.IsAiming
                    ? Vector3.zero
                    : new Vector3(
                        breath.HipXRandom.GetValue(deltaTime),
                        0f,
                        breath.HipZRandom.GetValue(deltaTime)) * (intensityMultiplier * breath.HipPenalty);
            }
            else
            {
                float tremorDelta = (breath.TremorOn ? deltaTime : deltaTime / 2f) * tremorScale;
                float tremorX = breath.TremorXRandom.GetValue(tremorDelta);
                float tremorY = breath.TremorYRandom.GetValue(tremorDelta);
                float tremorZ = breath.TremorZRandom.GetValue(tremorDelta);
                if (breath.Fracture && !breath.IsAiming)
                {
                    tremorX += Mathf.Max(0f, yValue) * Mathf.Lerp(1f, 100f / breath.EnergyFractureLimit, staminaLevel);
                }

                recoilOffset = new Vector3(tremorX, tremorY, tremorZ) * intensityMultiplier;
            }

            Vector3 currentOffset = breath._shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset;
            breath._shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset =
                Vector3.SqrMagnitude(recoilOffset - currentOffset) > 0.01f
                    ? Vector3.Lerp(currentOffset, recoilOffset, 0.1f)
                    : recoilOffset;

            breath._processors[0].ProcessRaw(breath._breathFrequency, breath._breathIntensity);
            breath._processors[1].ProcessRaw(breath._breathFrequency, breath._breathIntensity * breath._cameraSensetivity);

            return true;
        }
    }
}

