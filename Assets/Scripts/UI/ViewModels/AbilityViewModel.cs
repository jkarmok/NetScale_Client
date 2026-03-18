using UnityEngine;
using UnityEngine.UIElements;

namespace UI.ViewModels
{
    public class AbilityViewModel : UIScreen
    {
        // ─── Один слот способности ────────────────────────────────────────────

        private class AbilitySlot
        {
            private readonly VisualElement _cdOverlay;
            private readonly Label _cdLabel;
            private readonly Label _manaLabel;
            private readonly VisualElement _unavailable;
            private readonly VisualElement _castOverlay;
            private readonly Label         _castLabel;
            private readonly VisualElement _chargeRing;

            // Backing fields как nullable
            private float? _cooldownProgress;
            private float? _cooldownRemaining;
            private float? _manaCost;
            private bool?  _canActivate;
            private float? _castProgress;
            private float? _castTimeRemaining;
            private float? _chargeElapsed;
            private float? _chargeProgress;

            private const float MaxChargeBorderPx = 6f;

            public AbilitySlot(VisualElement root, int n)
            {
                _cdOverlay   = root.Q<VisualElement>($"ability-{n}-cd-overlay");
                _cdLabel     = root.Q<Label>($"ability-{n}-cd-label");
                _manaLabel   = root.Q<Label>($"ability-{n}-mana");
                _unavailable = root.Q<VisualElement>($"ability-{n}-unavailable");
                _castOverlay = root.Q<VisualElement>($"ability-{n}-cast-overlay");
                _castLabel   = root.Q<Label>($"ability-{n}-cast-label");
                _chargeRing  = root.Q<VisualElement>($"ability-{n}-charge-ring");
            }

            public float CooldownProgress
            {
                set
                {
                    if (_cooldownProgress.HasValue && Mathf.Approximately(_cooldownProgress.Value, value))
                        return;

                    _cooldownProgress = value;
                    var invertValue = 1f - value;
                    _cdOverlay.style.height = Length.Percent(Mathf.Clamp01(invertValue) * 100f);
                }
            }

            public float CooldownRemaining
            {
                set
                {
                    if (_cooldownRemaining.HasValue && Mathf.Approximately(_cooldownRemaining.Value, value))
                        return;

                    _cooldownRemaining = value;

                    bool onCD = value > 0.05f;
                    _cdLabel.style.display = onCD ? DisplayStyle.Flex : DisplayStyle.None;
                    if (onCD)
                        _cdLabel.text = value < 10f
                            ? value.ToString("F1")
                            : Mathf.CeilToInt(value).ToString();

                    RefreshUnavailable();
                }
            }

            public float ManaCost
            {
                set
                {
                    if (_manaCost.HasValue && Mathf.Approximately(_manaCost.Value, value))
                        return;

                    _manaCost = value;
                    _manaLabel.text = Mathf.RoundToInt(value).ToString();
                }
            }

            public bool CanActivate
            {
                set
                {
                    if (_canActivate.HasValue && _canActivate.Value == value)
                        return;

                    _canActivate = value;
                    RefreshUnavailable();
                }
            }

            public float CastProgress
            {
                set
                {
                    if (_castProgress.HasValue && Mathf.Approximately(_castProgress.Value, value))
                        return;

                    _castProgress = value;
                    bool casting = value > 0.001f;
                    _castOverlay.style.display = casting ? DisplayStyle.Flex : DisplayStyle.None;
                    if (casting)
                        _castOverlay.style.height = Length.Percent(Mathf.Clamp01(value) * 100f);
                }
            }

            public float CastTimeRemaining
            {
                set
                {
                    if (_castTimeRemaining.HasValue && Mathf.Approximately(_castTimeRemaining.Value, value))
                        return;

                    _castTimeRemaining = value;
                    bool casting = value > 0.05f;
                    _castLabel.style.display = casting ? DisplayStyle.Flex : DisplayStyle.None;
                    if (casting)
                        _castLabel.text = value < 10f
                            ? value.ToString("F1")
                            : Mathf.CeilToInt(value).ToString();
                }
            }

            public float ChargeElapsed
            {
                set
                {
                    if (_chargeElapsed.HasValue && Mathf.Approximately(_chargeElapsed.Value, value))
                        return;

                    _chargeElapsed = value;
                    bool charging = value > 0.001f;
                    _chargeRing.style.display = charging ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            public float ChargeProgress
            {
                set
                {
                    if (_chargeProgress.HasValue && Mathf.Approximately(_chargeProgress.Value, value))
                        return;

                    _chargeProgress = value;
                    var borderPx = Mathf.Clamp01(value) * MaxChargeBorderPx;
                    _chargeRing.style.borderTopWidth    = borderPx;
                    _chargeRing.style.borderBottomWidth = borderPx;
                    _chargeRing.style.borderLeftWidth   = borderPx;
                    _chargeRing.style.borderRightWidth  = borderPx;
                }
            }

            private void RefreshUnavailable()
            {
                bool unavailable = _canActivate.HasValue && !_canActivate.Value;
                _unavailable.style.display = unavailable ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // ─── HP / Mana ────────────────────────────────────────────────────────

        private class StatBar
        {
            private readonly VisualElement _fill;
            private readonly Label         _label;
            private int _current, _max;

            public StatBar(VisualElement root, string fillName, string labelName)
            {
                _fill  = root.Q<VisualElement>(fillName);
                _label = root.Q<Label>(labelName);
            }

            public void Set(int current, int max)
            {
                if (_current == current && _max == max) return;
                _current = current;
                _max     = Mathf.Max(1, max);
                _fill.style.width = Length.Percent(Mathf.Clamp01((float)_current / _max) * 100f);
                _label.text = $"{_current} / {_max}";
            }
        }

        // ─── ViewModel ────────────────────────────────────────────────────────

        private StatBar    _hp;
        private StatBar    _mana;
        private AbilitySlot[] _slots;

        public override void Initialize(VisualElement topElement, VisualElement root)
        {
            base.Initialize(topElement, root);

            _hp   = new StatBar(root, "hp-fill",   "hp-label");
            _mana = new StatBar(root, "mana-fill",  "mana-label");

            _slots = new AbilitySlot[4];
            for (int i = 0; i < 4; i++)
                _slots[i] = new AbilitySlot(root, i + 1);
        }
 
        public void SetHp(int current, int max)   => _hp.Set(current, max);
        public void SetMana(int current, int max)  => _mana.Set(current, max);

        public void SetAbility(
            int index,
            float cdRemaining,
            float cdProgress, 
            float manaCost,
            bool canActivate,
            float castProgress, 
            float castTimeRemaining,
            float chargeElapsed,
            float chargeProgress)
        {
            if ((uint)index >= (uint)_slots.Length) return;
            var slot = _slots[index];
            slot.ManaCost           = manaCost;
            slot.CanActivate        = canActivate;
            slot.CooldownProgress   = cdProgress;
            slot.CooldownRemaining  = cdRemaining;
            slot.CastProgress       = castProgress;
            slot.CastTimeRemaining  = castTimeRemaining;
            slot.ChargeElapsed      = chargeElapsed;
            slot.ChargeProgress     = chargeProgress;
        }

        protected override void SetVisualElements() => base.SetVisualElements();

 
    }
}