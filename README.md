# Attribute Modifier System (AMS)

A hierarchical attribute management system for calculating game stats like HP, damage, speed, and resources.

## Core Concepts

### Formula

All attribute calculations follow this formula:

```
finalValue = (sum(absolute) + weightedAvg(avgAbsolute)) 
           × (1 + (sum(percentage) + weightedAvg(avgPercentage)) 
           × 0.01)
```

### Class Overview

| Class | Purpose |
|-------|---------|
| `AmsProxy` | A node in the attribute hierarchy. Holds containers and calculates values. |
| `AmsContainer` | A named bundle of attribute values (equipment, buff, skill). |
| `AmsValues` | Calculated results with permanent, modifier, and current values. |
| `ValueSet` | The four value maps: absolute, percentage, avgAbsolute, avgPercentage. |
| `AmsTicker` | Manages automatic recalculation via Unity's PlayerLoop. |

### Calculator Classes

| Class | Purpose |
|-------|---------|
| `DefaultCalculator` | Simple double arithmetic. Default implementation. |
| `InstrumentedCalculator` | Logs all operations for debugging. |

## Complete Example

Here's a full example showing how to set up AMS for a game with a player that has base stats, equipment, and buffs.

### Step 1: Define Your Attributes

```csharp
using TeaSpoons.AMS;

// Define all game attributes as an enum
public enum GameAttribute
{
    MaxHp = 1,
    MaxEnergy = 2,
    AttackDamage = 3,
    Defense = 4,
    MovementSpeed = 5,
    CriticalChance = 6
}

// Create a wrapper that implements IAmsAttribute
public readonly struct Attr : IAmsAttribute
{
    private readonly GameAttribute _attr;
    
    public Attr(GameAttribute attr) => _attr = attr;
    
    public int Id => (int)_attr;
    public string Name => _attr.ToString();
    
    // Convenience: implicit conversion from enum
    public static implicit operator Attr(GameAttribute attr) => new Attr(attr);
}
```

### Step 2: Create the Player Proxy

```csharp
public class PlayerStatsManager : MonoBehaviour
{
    // The root proxy for the player
    private AmsProxy _playerProxy;
    
    // Child proxies for different stat sources
    private AmsProxy _equipmentProxy;
    private AmsProxy _buffsProxy;
    
    // Base stats container
    private AmsContainer _baseStats;
    
    void Awake()
    {
        InitializeAms();
    }
    
    void OnDestroy()
    {
        // Clean up when destroyed
        AmsTickManager.Instance.Unregister(_playerProxy);
    }
    
    private void InitializeAms()
    {
        // Create the hierarchy: Player -> Equipment, Buffs
        _playerProxy = new AmsProxy();
        _equipmentProxy = new AmsProxy();
        _buffsProxy = new AmsProxy();
        
        _playerProxy.AddChild(_equipmentProxy);
        _playerProxy.AddChild(_buffsProxy);
        
        // Create base stats
        _baseStats = new AmsContainer("BaseStats", "Core");
        _baseStats.ValueSet
            .AddAbsolute(GameAttribute.MaxHp, 100)
            .AddAbsolute(GameAttribute.MaxEnergy, 50)
            .AddAbsolute(GameAttribute.AttackDamage, 10)
            .AddAbsolute(GameAttribute.Defense, 5)
            .AddAbsolute(GameAttribute.MovementSpeed, 100)
            .AddAbsolute(GameAttribute.CriticalChance, 5);
        
        _playerProxy.AddContainer(_baseStats);
        
        // Register for automatic ticking
        AmsTickManager.Instance.TickMode = AmsTickMode.LateUpdate;
        AmsTickManager.Instance.Register(_playerProxy);
    }
    
    // Equipment management
    public void EquipItem(AmsContainer equipment)
    {
        _equipmentProxy.AddContainer(equipment);
    }
    
    public void UnequipItem(AmsContainer equipment)
    {
        _equipmentProxy.RemoveContainer(equipment);
    }
    
    // Buff management
    public void ApplyBuff(AmsContainer buff)
    {
        _buffsProxy.AddContainer(buff);
    }
    
    public void RemoveBuff(AmsContainer buff)
    {
        _buffsProxy.RemoveContainer(buff);
    }
    
    // Read final calculated values
    public double GetMaxHp() => _playerProxy.Values.GetPermanent(GameAttribute.MaxHp);
    public double GetAttackDamage() => _playerProxy.Values.GetPermanent(GameAttribute.AttackDamage);
    public double GetDefense() => _playerProxy.Values.GetPermanent(GameAttribute.Defense);
    
    // Get values including all children (equipment + buffs)
    public double GetTotalMaxHp() => _playerProxy.ValuesWithChildren.GetPermanent(GameAttribute.MaxHp);
}
```

### Step 3: Create Equipment and Buffs

```csharp
public static class ItemFactory
{
    public static AmsContainer CreateIronSword()
    {
        var sword = new AmsContainer("IronSword", "Weapon");
        sword.ValueSet
            .AddAbsolute(GameAttribute.AttackDamage, 15)
            .AddPercentage(GameAttribute.CriticalChance, 5); // +5% crit
        return sword;
    }
    
    public static AmsContainer CreateSteelArmor()
    {
        var armor = new AmsContainer("SteelArmor", "Armor");
        armor.ValueSet
            .AddAbsolute(GameAttribute.Defense, 20)
            .AddAbsolute(GameAttribute.MaxHp, 50)
            .AddPercentage(GameAttribute.MovementSpeed, -10); // -10% speed
        return armor;
    }
    
    public static AmsContainer CreateHealthPotion()
    {
        // Stackable buff with limit
        var potion = new AmsContainer("HealthPotion", "Consumable", new ValueSet(), 3);
        potion.ValueSet.AddAbsolute(GameAttribute.MaxHp, 25);
        potion.StackCount = 1; // Start with 1 stack
        return potion;
    }
    
    public static AmsContainer CreateBerserkBuff()
    {
        var buff = new AmsContainer("Berserk", "Buff");
        buff.ValueSet
            .AddPercentage(GameAttribute.AttackDamage, 50)  // +50% damage
            .AddPercentage(GameAttribute.Defense, -25);      // -25% defense
        return buff;
    }
}
```

### Step 4: Using the System

```csharp
public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerStatsManager _player;
    
    void Start()
    {
        // Equip items
        _player.EquipItem(ItemFactory.CreateIronSword());
        _player.EquipItem(ItemFactory.CreateSteelArmor());
        
        // After next tick, values are calculated
        // Base: 100 HP + Armor: 50 HP = 150 HP
        Debug.Log($"Max HP: {_player.GetTotalMaxHp()}");
        
        // Base: 10 + Sword: 15 = 25 damage
        Debug.Log($"Attack: {_player.GetAttackDamage()}");
    }
    
    public void OnBerserkButtonPressed()
    {
        _player.ApplyBuff(ItemFactory.CreateBerserkBuff());
        // Attack becomes: 25 * (1 + 50 * 0.01) = 37.5
    }
    
    public void OnDrinkPotion()
    {
        var potion = ItemFactory.CreateHealthPotion();
        potion.StackCount = 2; // Drink 2 potions
        _player.ApplyBuff(potion);
        // HP increases by 25 * 2 = 50
    }
}
```

## Hierarchy and Value Types

AmsProxy provides four different value calculations:

| Property | Description |
|----------|-------------|
| `Values` | This proxy's containers only |
| `ValuesWithParents` | This proxy + all ancestors |
| `ValuesWithChildren` | This proxy + all descendants |
| `ValuesWithParentsAndChildren` | This proxy + all ancestors + all descendants |

### Example Hierarchy

```
Player (root)
├── Equipment
│   ├── Sword container (+15 damage)
│   └── Armor container (+50 HP, +20 defense)
└── Buffs
    └── Berserk container (+50% damage)

Player.Values                    → Base stats only
Player.ValuesWithChildren        → Base + Equipment + Buffs (most common)
Equipment.ValuesWithParents      → Base + Equipment
Equipment.Values                 → Sword + Armor only
```

## Tick Modes

AMS uses dirty flags for efficient recalculation. Values only update when `Tick()` is called on the root proxy.

```csharp
// Automatic ticking (recommended)
AmsTickManager.Instance.TickMode = AmsTickMode.LateUpdate;
AmsTickManager.Instance.Register(rootProxy);

// Manual ticking
AmsTickManager.Instance.TickMode = AmsTickMode.Manual;
rootProxy.Tick(); // Call when needed
```

| Mode | When | Use Case |
|------|------|----------|
| `Manual` | Never auto-ticks | Full control, turn-based games |
| `Update` | Every frame, early | Real-time games needing fresh values |
| `LateUpdate` | Every frame, late | After all game logic runs (recommended) |
| `FixedUpdate` | Physics tick | Physics-dependent attributes |

## Extending AmsProxy

Override virtual methods to react to changes:

```csharp
public class PlayerProxy : AmsProxy
{
    public event Action<double, double> OnHealthChanged;
    
    protected override void OnValuesSelfChanged(AmsValues oldValues, AmsValues newValues)
    {
        var oldHp = oldValues.GetPermanent(GameAttribute.MaxHp);
        var newHp = newValues.GetPermanent(GameAttribute.MaxHp);
        
        if (Math.Abs(oldHp - newHp) > 0.001)
        {
            OnHealthChanged?.Invoke(oldHp, newHp);
        }
    }
    
    protected override void OnContainerAdded(AmsContainer container)
    {
        Debug.Log($"Added: {container.Name}");
    }
    
    protected override void OnContainerRemoved(AmsContainer container)
    {
        Debug.Log($"Removed: {container.Name}");
    }
}
```

## Debugging with InstrumentedCalculator

Track all calculations for debugging server/client drift:

```csharp
// Enable instrumentation
var instrumented = AmsCalculatorProvider.CreateInstrumented();
AmsCalculatorProvider.SetCalculator(instrumented);

// Run some calculations
rootProxy.Tick();

// Export logs
string json = instrumented.ExportLogsJson();
Debug.Log(json);

// Reset to default
AmsCalculatorProvider.Reset();
```

## API Reference

### AmsContainer

```csharp
// Creation
var container = new AmsContainer("Name", "System");
var container = new AmsContainer("Name", "System", valueSet, stackCount, stackLimit);

// Stack management
container.StackCount = 3;      // Set stack count (min: 1, max: StackLimit)
container.StackLimit = 5;      // Set maximum stacks (null = unlimited)

// Value access
container.ValueSet.AddAbsolute(attr, 100);
container.ValueSet.AddPercentage(attr, 15);
container.GetValueAbsolute(attr);
container.IsEmpty;
```

### AmsProxy

```csharp
// Hierarchy
proxy.AddChild(childProxy);
proxy.RemoveChild(childProxy);
proxy.Parent;
proxy.Children;
proxy.IsRoot;
proxy.IsLeaf;
proxy.Root;
proxy.Flattened();

// Containers
proxy.AddContainer(container);
proxy.RemoveContainer(container);
proxy.Containers;

// Values (call after Tick)
proxy.Values.GetPermanent(attr);
proxy.ValuesWithChildren.GetPermanent(attr);
proxy.Tick();
```

### AmsValues

```csharp
// Permanent values (calculated)
values.GetPermanent(attr);      // Returns double
values.GetPermanentLong(attr);  // Returns long (truncated)
values.Permanents;              // All permanent values

// Modifiers
values.GetModifier(attr);       // Returns decimal (15% → 0.15)
values.GetModifierRaw(attr);    // Returns raw (15% → 15.0)
values.Modifiers;               // All modifiers

// Current values (mutable runtime state)
values.SetCurrent(attr, value);
values.GetCurrent(attr);
values.ClearCurrents();
```

### ValueSet

```csharp
// Add values
valueSet.AddAbsolute(attr, magnitude);
valueSet.AddPercentage(attr, magnitude);
valueSet.AddAbsoluteAverage(attr, magnitude, stackCount);
valueSet.AddPercentageAverage(attr, magnitude, stackCount);
valueSet.AddAmsValue(attr, new AmsValue("100"));   // Parses "100" or "15%"
valueSet.AddValueSet(otherValueSet, stackCount);

// Read values
valueSet.GetAbsoluteValue(attr);
valueSet.GetPercentageValue(attr);
valueSet.IsEmpty();
valueSet.Clear();

// Factory methods
ValueSet.FromAbsolute(attr, magnitude);
ValueSet.FromPercentage(attr, magnitude);
```

## Best Practices

1. **Register only root proxies** with `AmsTickManager`. Children are ticked automatically.

2. **Use `ValuesWithChildren`** for total player stats. Use `Values` for base stats only.

3. **Keep containers immutable** after creation when possible. Modify `StackCount` for stacking.

4. **Separate concerns** with child proxies: Equipment, Buffs, Passives, etc.

5. **Use `LateUpdate` tick mode** to ensure all game logic runs before stat calculation.

6. **Null attributes throw exceptions** - this is intentional to catch bugs early.
## Installation

In Unity: **Window > Package Manager > + > Add package from git URL**, then enter:

```
https://github.com/tea-spoons/ams.git
```

Pin a release by appending a tag, for example `#v0.0.3`.

### Dependencies

Unity cannot resolve git dependencies automatically, so add these to your project first:

- `com.tea-spoons.logging` 1.3.8

## License

Copyright (c) 2026 Bigpoint. Authored by Muhammad Tarek Abdou.

Available for research, education and other noncommercial use under the [PolyForm Noncommercial 1.0.0](LICENSE.md)
license. Commercial use is not permitted.
