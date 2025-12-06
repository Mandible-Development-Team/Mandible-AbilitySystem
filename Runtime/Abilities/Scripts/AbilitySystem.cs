using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Mandible.Systems;
using Mandible.PlayerController;

using Sirenix.OdinInspector;

namespace Mandible.AbilitySystem
{
    [ExecuteAlways]
    public class AbilitySystem : MonoBehaviour
    {
        [SerializeField] private Transform playerObject;
        public IPlayer player;
        public IAgent agent;

        [Header("Abilities")]
        [SerializeField] private ObservedList<Ability> baseAbilities = new();
        [SerializeField] private ObservedList<Ability> swappableAbilities = new();
        public readonly Dictionary<Ability, AbilityInstance> activeAbilities = new();
        public event Action<Ability> OnAbilityRun;
        private Dictionary<Ability, int> abilityRunFrame = new();

        [Header("Slots")]
        [SerializeField] private Transform baseAbilityParent;
        [SerializeField] private Transform swappableAbilityParent;
        [HideInInspector] private List<AbilitySlot> baseAbilitySlots = new();
        [HideInInspector] private List<AbilitySlot> swappableAbilitySlots = new();

        [Header("Override")]
        [SerializeField] public AbilityOverrideMode currentOverride;

        [Header("Input")]
        [SerializeField] public float inputBufferTime = 0.08f;
        [HideInInspector] public IInputSystem inputSystem;
        private List<QueuedInput> inputQueue = new();

        [Header("Extension")]
        [SerializeReference]
        [OdinSerialize][SubclassSelector]
        List<IAbilitySystemExtension> extensions = new()
        {
            new UltimateAbilitySystem()
        };

        [Header("Debug")]
        [SerializeField] private float baseAbilitySlotCount;
        [SerializeField] private float swappableAbilitySlotCount;
        [SerializeField] private bool debug = false;

        //Events
        private ObservedList<Ability>.ChangedDelegate onSwappableChanged;
        private ObservedList<Ability>.ChangedDelegate onBaseChanged;
        private Action onSwappableUpdated;
        private Action onBaseUpdated;

        private void Awake()
        {
            SetEventDelegates();
        }

        #if MANDIBLE_PLAYER_CONTROLLER
        private void Start()
        {
            //Start/
            InitializePlayer();
            ApplySwappableAbilities(swappableAbilities);

            //Slots
            GetSlotReferences();
            InitializeSlots();
            UpdateSlots();
            
            //Extensions
            InitializeExtensions();
        }  
        #else
        private void Start()
        {
            Debug.LogWarning("AbilitySystem requires Mandible PlayerController dependency to function. Please install from https://sampleurl.com");
            return;
        }
        #endif

        private void Update()
        {
            UpdateDebugInfo();
            if(!Application.isPlaying) return;

            CleanupExpiredCooldowns();
            ProcessInputQueue();

            HandleExtensions();
        }

        //Lifetime

        private void OnEnable()
        {
            ShowAbilitySlots(true);

            SubscribeEvents();
        }

        private void OnDisable()
        {
            ShowAbilitySlots(false);

            UnsubscribeEvents();
        }

        private void OnValidate()
        {
            if(Application.isPlaying) return;

            InitializeSlots();
            UpdateSlots();
        }

        private void SetEventDelegates()
        {
            onSwappableChanged = OnAbilityChanged;
            onBaseChanged = OnAbilityChanged;

            onSwappableUpdated = UpdateSlots;
            onBaseUpdated = UpdateSlots;
        }

        private void SubscribeEvents()
        {
            swappableAbilities.Changed += onSwappableChanged;
            swappableAbilities.Updated += onSwappableUpdated;

            baseAbilities.Changed += onBaseChanged;
            baseAbilities.Updated += onBaseUpdated;
        }

        private void UnsubscribeEvents()
        {
            swappableAbilities.Changed -= onSwappableChanged;
            swappableAbilities.Updated -= onSwappableUpdated;

            baseAbilities.Changed -= onBaseChanged;
            baseAbilities.Updated -= onBaseUpdated;
        }

        #if MANDIBLE_PLAYER_CONTROLLER
        void InitializePlayer()
        {
            if(playerObject == null)
                playerObject = this.transform;

            player = playerObject.GetComponent<IPlayer>();

            if(player == null)
                Debug.LogError("No IPlayer found on object. Please add a MonoBehaviour that implements IPlayer.");

            agent = player.Controller as IAgent;
            
            if(agent == null)
                Debug.LogError("No IAgent found on PlayerController. Please ensure PlayerController implements IAgent.");

            //Input
            inputSystem = player.Input;
        }
        #endif

        private void InitializeExtensions()
        {
            foreach (var ext in extensions)
                ext.Initialize(this);
        }

        public void HandleExtensions()
        {
            foreach (var ext in extensions)
                ext.Handle();
        }

        //Abilities

        public void OnAbilityChanged(int index, Ability oldValue, Ability newValue)
        {
            UpdateSlots();
        }

        public class AbilityInstance
        {
            public Coroutine Coroutine;
            public float CooldownEndTime;
            public RuntimeState data;
        }

        public bool RunAbility(Ability ability)
        {
            if (ability == null) return false;

            if (IsAbilityInUse(ability))
            {
                if (debug) 
                    Debug.LogWarning($"{ability.name} is already running!");
                return false;
            }

            if (IsOnCooldown(ability))
            {
                if (debug)
                    Debug.Log($"{ability.name} is on cooldown ({GetCooldownRemaining(ability):0.00}s left)");
                return false;
            }

            var data = new RuntimeState();

            var instance = new AbilityInstance
            {
                data = data,
                CooldownEndTime = 0f
            };

            instance.Coroutine = StartCoroutine(WatchAbility(ability, data));
            activeAbilities[ability] = instance;

            ability.OnAbilityStart(agent, data);

            NotifyAbilityRun(ability);
            return true;
        }

        public void RequestAbility(Ability ability, int weight = 0)
        {
            if (ability == null)
                return;

            // Is already running
            if (IsAbilityRunning(ability))
            {
                CancelAbility(ability);
                return;
            }

            // Cooling down
            if (IsOnCooldown(ability))
            {
                if (debug)
                    Debug.Log($"{ability.name} is cooling down.");
                return;
            }

            // Conditions
            if (!ability.DoesSatisfyCondition())
                return;

            // Run
            QueueAbility(ability, weight);
        }

        private IEnumerator WatchAbility(Ability ability, RuntimeState data)
        {
            yield return ability.Activate(agent, data);
            StopAbility(ability, completed: true);
        }

        public void StopAbility(Ability ability, bool completed = true)
        {
            if (ability == null || !activeAbilities.ContainsKey(ability)) return;

            var instance = activeAbilities[ability];

            if (instance.Coroutine != null)
            {
                StopCoroutine(instance.Coroutine);
                instance.Coroutine = null;
            }

            if (!completed){
                ability.OnAbilityCanceled(agent, instance.data);
                ResetCooldown(ability);
            }
            else
                StartCooldown(ability);
        }

        private void StopAbilityRaw(Ability ability)
        {
            if (ability == null || !activeAbilities.ContainsKey(ability)) return;

            var instance = activeAbilities[ability];

            if (instance.Coroutine != null)
            {
                StopCoroutine(instance.Coroutine);
                instance.Coroutine = null;
            }

            ResetCooldown(ability, false);
        }

        public void StopAllAbilities(bool onAbilityEnd = true)
        {
            var abilitiesCopy = activeAbilities.Keys.ToList();

            foreach (var ability in abilitiesCopy)
            {
                StopAbility(ability, completed: !onAbilityEnd);
            }
        }

        public void CancelAbility(Ability ability)
        {
            StopAbility(ability, completed: false);
        }

        private void NotifyAbilityRun(Ability ability)
        {
            OnAbilityRun?.Invoke(ability);
            abilityRunFrame[ability] = Time.frameCount;
        }

        public bool OnAbilityCall(Ability ability)
        {
            return abilityRunFrame.TryGetValue(ability, out int frame)
                && frame == Time.frameCount;
        }

        //Cooldowns

        private void StartCooldown(Ability ability)
        {
            ability.OnAbilityEnd(agent, activeAbilities[ability].data);

            if (activeAbilities.TryGetValue(ability, out var instance))
            {
                instance.Coroutine = null;
                instance.CooldownEndTime = Time.time + ability.GetCooldown();
                activeAbilities[ability] = instance;
            }
        }

        public void ResetCooldown(Ability ability, bool triggerOnEnd = true)
        {
            SetCooldownToZero(ability);

            if(triggerOnEnd)
                ability.OnAbilityEnd(agent, activeAbilities[ability].data);
        }

        public void SetCooldownToZero(Ability ability)
        {
            var instance = activeAbilities[ability];

            instance.CooldownEndTime = 0f;
            activeAbilities[ability] = instance;
        }

        private void CleanupExpiredCooldowns()
        {
            if (activeAbilities.Count == 0) return;

            float now = Time.time;
            ObservedList<Ability> toRemove = null;

            foreach (var kvp in activeAbilities)
            {
                if (kvp.Value.Coroutine == null && now >= kvp.Value.CooldownEndTime)
                {
                    toRemove ??= new ObservedList<Ability>();
                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
                foreach (var ab in toRemove)
                    activeAbilities.Remove(ab);
        }

        //Queries

        public bool IsAbilityInUse(Ability ability)
        {
            if (ability == null) return false;

            if (activeAbilities.TryGetValue(ability, out var instance))
            {
                bool coroutineActive = instance.Coroutine != null;
                bool cooldownNotStarted = Time.time >= instance.CooldownEndTime || instance.CooldownEndTime == 0f;
                return coroutineActive && cooldownNotStarted;
            }

            return false;
        }

        public bool IsAbilityRunning(Ability ability)
        {
            if (ability == null) return false;

            return activeAbilities.TryGetValue(ability, out var instance) && instance.Coroutine != null;
        }

        public bool IsAbilityAvailable(Ability ability)
        {
            if (ability == null) return false;

            if (activeAbilities.TryGetValue(ability, out var instance))
            {
                bool isRunning = instance.Coroutine != null;
                bool isCoolingDown = Time.time < instance.CooldownEndTime && instance.CooldownEndTime > 0f;
                return !isRunning && !isCoolingDown;
            }

            return true;
        }

        public bool IsOnCooldown(Ability ability)
        {
            if (ability == null) return false;
            return activeAbilities.TryGetValue(ability, out var instance) &&
                Time.time < instance.CooldownEndTime;
        }

        public float GetCooldownRemaining(Ability ability)
        {
            if (activeAbilities.TryGetValue(ability, out var instance))
                return Mathf.Max(0f, instance.CooldownEndTime - Time.time);
            return 0f;
        }

        public float GetCooldownPercent(Ability ability)
        {
            if (ability == null) return 0f;
            if (activeAbilities.TryGetValue(ability, out var instance))
            {
                float remaining = instance.CooldownEndTime - Time.time;
                float total = ability.GetCooldown();
                return Mathf.Clamp01(remaining / total);
            }
            return 0f;
        }

        //Overrides

        public void EnterOverrideMode(AbilityOverrideMode mode)
        {
            if (mode == null || mode.abilities.Count == 0) return;

            ApplyResetMask(mode.resetMask);

            currentOverride = mode;
            ApplySwappableAbilities(mode.abilities);
        }

        public void ExitOverrideMode()
        {
            if (currentOverride == null) return;

            ApplyResetMask(currentOverride.exitResetMask);

            currentOverride = null;
            ApplySwappableAbilities(swappableAbilities);
        }

        private void ApplySwappableAbilities(ObservedList<Ability> abilities)
        {
            for (int i = 0; i < swappableAbilitySlots.Count; i++)
            {
                if (i < abilities.Count)
                    swappableAbilitySlots[i].SetAbility(abilities[i]);
                else
                    swappableAbilitySlots[i].ClearAbility();
            }
        }

        private void ApplyBaseAbilities()
        {
            for (int i = 0; i < baseAbilitySlots.Count; i++)
            {
                if (i < baseAbilities.Count)
                    baseAbilitySlots[i].SetAbility(baseAbilities[i]);
                else
                    baseAbilitySlots[i].ClearAbility();
            }
        }

        private void ApplyResetMask(AbilityResetMask mask)
        {
            if (mask.HasFlag(AbilityResetMask.Swappable))
            {
                foreach (var ability in swappableAbilities)
                    if (activeAbilities.ContainsKey(ability))
                        StopAbilityRaw(ability);
            }

            if (mask.HasFlag(AbilityResetMask.Base))
            {
                foreach (var ability in baseAbilities)
                    if (activeAbilities.ContainsKey(ability))
                        StopAbilityRaw(ability);
            }
        }

        //Slots

        private void InitializeSlots()
        {
            foreach (var slot in baseAbilitySlots)
                slot.Initialize(this);

            foreach (var slot in swappableAbilitySlots)
                slot.Initialize(this);
        }

        private void UpdateSlots()
        {
            ApplyBaseAbilities();

            if (currentOverride != null)
                ApplySwappableAbilities(currentOverride.abilities);
            else
                ApplySwappableAbilities(swappableAbilities);

            if(debug) Debug.Log("Updating slots");
        }

        private void ShowAbilitySlots(bool show)
        {
            foreach (var slot in baseAbilitySlots)
                slot.gameObject.SetActive(show);
            
            foreach (var slot in swappableAbilitySlots)
                slot.gameObject.SetActive(show);
        }

        private void GetSlotReferences()
        {
            if(baseAbilitySlots != null){
                baseAbilitySlots.Clear();
                baseAbilitySlots.AddRange(baseAbilityParent.GetComponentsInChildren<AbilitySlot>());
            }

            if(swappableAbilitySlots != null){
                swappableAbilitySlots.Clear();
                swappableAbilitySlots.AddRange(swappableAbilityParent.GetComponentsInChildren<AbilitySlot>());
            }
        }

        //Ability Queue

        private struct QueuedInput
        {
            public float time;
            public Ability ability;
            public int priority;
        }

        public void QueueAbility(Ability ability, int priority)
        {
            if (ability == null) return;

            inputQueue.Add(new QueuedInput
            {
                time = Time.time,
                ability = ability,
                priority = priority
            });
        }

        private void ProcessInputQueue()
        {
            if (inputQueue.Count == 0) return;

            inputQueue = inputQueue.Where(i => Time.time - i.time <= inputBufferTime).ToList();
            if (inputQueue.Count == 0) return;

            var nextInput = inputQueue.OrderByDescending(i => i.priority).First();

            if (IsAbilityAvailable(nextInput.ability)){
                RunAbility(nextInput.ability);
            }

            inputQueue.Clear();
        }

        //Debug

        private void UpdateDebugInfo()
        {
            baseAbilitySlotCount = baseAbilitySlots.Count;
            swappableAbilitySlotCount = swappableAbilitySlots.Count;
        }
    }
}
