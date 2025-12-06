using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Mandible.AbilitySystem
{
    [ExecuteAlways]
    public abstract class AbilitySlot : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] AbilitySystem owner;
        [SerializeField] protected Ability ability;
        [HideInInspector] protected string inputAction;

        [Header("Update")]
        [SerializeField] protected bool updateText = true;

        [Header("UI")]
        [SerializeField] protected Transform icon;
        [SerializeField] protected Transform cooldown;
        [SerializeField] protected Transform availability;
        [SerializeField] protected Transform count;

        [Header("Debug")]
        [SerializeField] protected bool debug = false;

        protected Image iconImage;
        protected Image availabilityImage;
        protected Image cooldownImage;
        protected TMP_Text cooldownText;
        protected IAgent agent;

        protected virtual void Awake()
        {
            //Images
            if (iconImage == null)
            {
                iconImage = icon.GetComponent<Image>();
            }
            if (availabilityImage == null)
            {
                availabilityImage = availability.GetComponent<Image>();
            }
            if (cooldownImage == null)
            {
                cooldownImage = cooldown.GetComponent<Image>();
            }

            //Text
            if (cooldownText == null)
            {
                cooldownText = count.GetComponent<TMP_Text>();
            }
        }

        protected virtual void Start()
        {
            SetAgent();
        }

        protected virtual void Update()
        {
            if (Application.isPlaying == false) return;
            if (ability == null || owner == null) return;

            UpdateUI();

            //Poll Input
            inputAction = ResolveInputAction();
            var signal = owner.inputSystem.GetSignal(inputAction);

            if (signal.Pressed)
            {
                if(debug) Debug.Log($"Ability Slot '{gameObject.name}' detected input action '{inputAction}' pressed.");
                TryActivateAbility();
            }
                
        }

        public void UpdateUI()
        {
            //UpdateIcon
            UpdateIcon();

            //Update Text
            float remaining = owner.GetCooldownRemaining(ability);
            if (cooldownText && updateText)
            {
                if (remaining > 0)
                    cooldownText.text = Mathf.CeilToInt(remaining).ToString();
                else
                    cooldownText.text = "";
            }        

            //Update Availability
            availabilityImage.enabled = !owner.IsAbilityAvailable(ability);

            //Update Cooldown
            if (cooldownImage){
                float percent = owner.GetCooldownPercent(ability);
                SetPercentage(1f - percent);

                cooldownImage.enabled = percent > 0 && percent < 1;
            }
        }

        public void UpdateIcon()
        {
            //Update Image
            try
            {
                if (iconImage) iconImage.sprite = ability.icon;
            }
            catch{}
        }

        public void Initialize(AbilitySystem owner)
        {
            this.owner = owner;

            SetAgent();
        }

        public void SetAbility(Ability ability)
        {
            this.ability = ability;
            UpdateIcon();
        }

        public void ClearAbility()
        {
            this.ability = null;
            UpdateIcon();
        }

        protected abstract string ResolveInputAction();

        public void SetPercentage(float value)
        {
            cooldownImage.fillAmount = value;
        }

        private void TryActivateAbility()
        {
            if (ability == null) return;

            owner.RequestAbility(ability);
        }

        public void SetAgent()
        {
            if(owner == null) return;
            
            agent = owner?.agent;
        }

    }
}
