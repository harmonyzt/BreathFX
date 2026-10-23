using BreathFX.Audio;
using BreathFX.Breathing;
using BreathFX.Hud;
using UnityEngine;
using EFT;
using EFT.InventoryLogic;

namespace BreathFX
{
    internal sealed class BreathingController : MonoBehaviour
    {
        private const float MinStartBreath = 0.15f;
        private const float LowStaminaDrainPenalty = 0.75f;
        private const float LowStaminaRecoveryPenalty = 0.65f;
        private const float AudibleBreathThreshold = 0.35f;

        private Physical _physical;
        private Player _player;
        private BreathingAudio _audio;
        private BreathingVignette _vignette;
        private bool _holdRequested;
        private bool _autoHoldActive;
        private bool _autoHoldSuppressed;
        private bool _lastAudible;
        private bool _wasHolding;
        private bool _depletedHold;
        private float _recoveryLockUntil;
        private float _gaspUntil;
        private float? _originalOxygenMultiplier;
        private bool _oxygenFrozen;

        internal float Breath { get; private set; } = 1f;
        internal bool IsHolding { get; private set; }
        internal bool IsGasping { get; private set; }
        internal bool IsActive => Settings.Enabled.Value && isActiveAndEnabled && _physical != null && _player != null;
        internal bool IsAudible => IsActive && !IsHolding && (IsGasping || Breath <= AudibleBreathThreshold);

        internal void Initialize(Physical physical, Player player)
        {
            if (_physical == physical && _player == player)
            {
                EnsureAudio();
                EnsureVignette();
                FreezeOxygen();
                return;
            }

            RestoreOxygen();

            _physical = physical;
            _player = player;
            Breath = 1f;
            _holdRequested = false;
            _autoHoldActive = false;
            _autoHoldSuppressed = false;
            IsHolding = false;
            IsGasping = false;
            _lastAudible = false;
            _wasHolding = false;
            _depletedHold = false;
            _recoveryLockUntil = 0f;
            _gaspUntil = 0f;
            _oxygenFrozen = false;
            _originalOxygenMultiplier = null;

            EnsureAudio();
            EnsureVignette();
            FreezeOxygen();
        }

        internal void SetHoldRequested(bool enable)
        {
            if (!Settings.Enabled.Value)
            {
                return;
            }

            if (!enable)
            {
                bool manualRelease = !_autoHoldActive && _holdRequested;
                if (manualRelease && _physical != null && !_physical.Sprinting)
                {
                    _autoHoldSuppressed = true;
                }

                _autoHoldActive = false;
                _holdRequested = false;
                IsHolding = false;
                return;
            }

            _autoHoldSuppressed = false;

            if (IsHolding)
            {
                _autoHoldActive = false;
                return;
            }

            if (Breath < MinStartBreath || Time.time < _recoveryLockUntil)
            {
                return;
            }

            _autoHoldActive = false;
            _holdRequested = true;
            RefreshHolding();
        }

        private void EnsureAudio()
        {
            if (_player == null)
            {
                return;
            }

            if (_audio == null || _audio.gameObject != _player.gameObject)
            {
                _audio = _player.gameObject.GetComponent<BreathingAudio>();
                if (_audio == null)
                {
                    _audio = _player.gameObject.AddComponent<BreathingAudio>();
                }
            }

            _audio.Initialize();
        }

        private void EnsureVignette()
        {
            if (_player == null)
            {
                return;
            }

            if (_vignette == null || _vignette.gameObject != _player.gameObject)
            {
                _vignette = _player.gameObject.GetComponent<BreathingVignette>();
                if (_vignette == null)
                {
                    _vignette = _player.gameObject.AddComponent<BreathingVignette>();
                }
            }

            _vignette.Initialize();
        }

        private void Update()
        {
            if (_physical == null || _player == null)
            {
                return;
            }

            if (!Settings.Enabled.Value)
            {
                RestoreOxygen();
                _audio?.StopAll();
                _vignette?.Hide();
                return;
            }

            FreezeOxygen();

            bool aiming = _player.HandsController != null && _player.HandsController.IsAiming;
            UpdateAutoHold(aiming);

            if (_holdRequested && !aiming)
            {
                _holdRequested = false;
                _autoHoldActive = false;
            }

            RefreshHolding();

            if (IsHolding != _wasHolding)
            {
                if (IsHolding)
                {
                    _audio?.OnHoldStarted();
                }
                else
                {
                    if (!_depletedHold)
                    {
                        _audio?.OnHoldEnded();
                    }

                    _depletedHold = false;
                }

                _wasHolding = IsHolding;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            float stamina = Mathf.Clamp01(_physical.Stamina.NormalValue);

            if (IsHolding)
            {
                float drainMultiplier = 1f + LowStaminaDrainPenalty * (1f - stamina);
                float drainRate = 1f / Mathf.Max(0.1f, Settings.MaxHoldSeconds.Value);
                Breath = Mathf.Max(0f, Breath - drainRate * drainMultiplier * dt);

                if (Breath <= 0f)
                {
                    Deplete();
                }
            }
            else if (Time.time >= _recoveryLockUntil)
            {
                float recoveryMultiplier = Mathf.Lerp(1f - LowStaminaRecoveryPenalty, 1f, stamina);
                if (_physical.Sprinting)
                {
                    recoveryMultiplier *= 0.5f;
                }

                recoveryMultiplier = Mathf.Max(0.1f, recoveryMultiplier);
                float recoveryRate = 1f / Mathf.Max(0.1f, Settings.RecoverySeconds.Value);
                Breath = Mathf.Min(1f, Breath + recoveryRate * recoveryMultiplier * dt);
            }

            IsGasping = Time.time < _gaspUntil;
            _audio?.Tick(IsHolding, Breath);
            _vignette?.Tick(IsHolding, Breath);

            bool audible = IsAudible;
            if (audible != _lastAudible)
            {
                _lastAudible = audible;
                _physical.InvokeHoldBreathChanged();
            }
        }

        private void RefreshHolding()
        {
            bool aiming = _player != null && _player.HandsController != null && _player.HandsController.IsAiming;
            bool sprinting = _physical != null && _physical.Sprinting;
            IsHolding = _holdRequested && aiming && !(Settings.ReleaseOnSprint.Value && sprinting);
        }

        private void UpdateAutoHold(bool aiming)
        {
            if (!aiming)
            {
                _autoHoldSuppressed = false;
            }

            bool eligible = Settings.AutoHoldOnScope.Value
                            && aiming
                            && !_autoHoldSuppressed
                            && IsAutoHoldWeapon();
            if (eligible)
            {
                if (_autoHoldActive || _holdRequested)
                {
                    return;
                }

                if (Breath < MinStartBreath || Time.time < _recoveryLockUntil)
                {
                    return;
                }

                _autoHoldActive = true;
                _holdRequested = true;
                RefreshHolding();
                return;
            }

            if (_autoHoldActive)
            {
                _autoHoldActive = false;
                _holdRequested = false;
                RefreshHolding();
            }
        }

        private bool IsAutoHoldWeapon()
        {
            Player.FirearmController firearm = _player != null
                ? _player.HandsController as Player.FirearmController
                : null;

            if (firearm == null)
            {
                return false;
            }

            Weapon weapon = firearm.Item;
            if (weapon == null)
            {
                return false;
            }

            if (weapon is BaseAssault)
            {
                return Settings.AutoHoldAssaultRifles.Value;
            }

            if (weapon is MarksmanRifle)
            {
                return Settings.AutoHoldDmrs.Value;
            }

            if (weapon is SniperRifle)
            {
                return Settings.AutoHoldBoltActions.Value;
            }

            return false;
        }

        private void Deplete()
        {
            Breath = 0f;
            _holdRequested = false;
            _autoHoldActive = false;
            IsHolding = false;
            IsGasping = true;
            _depletedHold = true;
            _recoveryLockUntil = Time.time + Settings.DepletedLockSeconds.Value;
            _gaspUntil = Time.time + Settings.GaspSeconds.Value;
        }

        private void FreezeOxygen()
        {
            Stamina oxygen = _physical.Oxygen;
            if (oxygen == null)
            {
                return;
            }

            if (!_oxygenFrozen)
            {
                _originalOxygenMultiplier = oxygen.Multiplier;
                _oxygenFrozen = true;
            }

            if (oxygen.Multiplier != 0f)
            {
                oxygen.Multiplier = 0f;
            }

            float capacity = oxygen.TotalCapacity;
            if (Mathf.Abs(oxygen.Current - capacity) > 0.01f)
            {
                oxygen.Current = capacity;
            }
        }

        private void RestoreOxygen()
        {
            if (!_oxygenFrozen || _physical == null || _physical.Oxygen == null)
            {
                _oxygenFrozen = false;
                _originalOxygenMultiplier = null;
                return;
            }

            if (_originalOxygenMultiplier.HasValue && _physical.Oxygen.Multiplier == 0f)
            {
                _physical.Oxygen.Multiplier = _originalOxygenMultiplier.Value;
            }

            _oxygenFrozen = false;
            _originalOxygenMultiplier = null;
        }

        private void OnDisable()
        {
            _vignette?.Hide();
        }

        private void OnDestroy()
        {
            _audio?.StopAll();
            _vignette?.Hide();
            RestoreOxygen();
            BreathingManager.Unregister(_physical, this);
        }
    }
}