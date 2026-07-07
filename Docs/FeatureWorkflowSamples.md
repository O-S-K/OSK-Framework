# OSK Create Feature - Workflow Samples

This document is a prototype for the future Create Feature tool.
It is not generated runtime code yet. Use it to review feature structure, module choices, and gameplay flow before building the editor generator.

Core rule:

```csharp
// UI
Main.UI.Open<TView>(model);

// Event
Main.Event.Subscribe<TEvent>(OnEvent);
Main.Event.Unsubscribe<TEvent>(OnEvent);
Main.Event.Publish(new TEvent(...));

// Pool
Main.Pool.Build(prefab, groupName).SetPosition(pos).AutoDespawn(delay).Spawn();
Main.Pool.BuildByKey<GameObject>(key).SetPosition(pos).AutoDespawn(delay).Spawn();

// Sound
Main.Sound.Play(soundId);
Main.Sound.Stop(soundId);
Main.Sound.StopWithFade(soundId, fadeDuration);

// Data
Main.Data.Load(SaveType.Json, fileName, defaultData);
Main.Data.Save(SaveType.Json, fileName, data);

// Config
// Feature-specific ScriptableObject, assigned by inspector or loaded by project convention.
```

## Suggested Folder Convention

```text
Assets/_Game/Features/{FeatureName}/
  {FeatureName}Feature.cs
  {FeatureName}Service.cs
  {FeatureName}Events.cs
  {FeatureName}Model.cs
  UI/
    {FeatureName}View.cs
  Data/
    {FeatureName}Save.cs
    {FeatureName}Config.cs
  Pool/
    {FeatureName}PoolKeys.cs
  Audio/
    {FeatureName}SoundKeys.cs
  Examples/
    {FeatureName}Example.cs
```

Checkbox rule:

```text
UI      -> generate View + Model + Main.UI.Open flow
Event   -> generate GameEvent classes + Subscribe/Publish sample
Pool    -> generate PoolKeys + spawn/auto-despawn sample
Sound   -> generate SoundKeys + Main.Sound.Play/Stop sample
Data    -> generate Save class + Load/Save service flow
Config  -> generate ScriptableObject config + service reference
Command -> optional action wrapper later
Localize-> optional keys later
Debug   -> optional debug panel later
```

---

## 1. Daily Reward

Modules:

```text
[x] UI  [x] Event  [ ] Pool  [x] Sound  [x] Data  [x] Config
```

Use case:

```text
Open reward popup -> check if claimable -> claim -> save next claim time -> publish event -> play sound -> close UI
```

Generated files:

```text
DailyRewardFeature.cs
DailyRewardService.cs
DailyRewardEvents.cs
DailyRewardModel.cs
UI/DailyRewardView.cs
Data/DailyRewardSave.cs
Data/DailyRewardConfig.cs
Audio/DailyRewardSoundKeys.cs
Examples/DailyRewardExample.cs
```

Core flow:

```csharp
public static class DailyRewardFeature
{
    public static void Open()
    {
        DailyRewardModel model = DailyRewardService.BuildModel();
        Main.UI.Open<DailyRewardView>(model);
    }

    public static bool TryClaim()
    {
        if (!DailyRewardService.CanClaim()) return false;

        DailyRewardClaimResult result = DailyRewardService.Claim();
        Main.Data.Save(SaveType.Json, DailyRewardSave.FileName, DailyRewardService.Save);
        Main.Event.Publish(new DailyRewardClaimedEvent(result.Gold, result.Gem));
        Main.Sound.Play(DailyRewardSoundKeys.Claim);
        return true;
    }
}

public sealed class DailyRewardClaimedEvent : GameEvent
{
    public int Gold { get; }
    public int Gem { get; }

    public DailyRewardClaimedEvent(int gold, int gem)
    {
        Gold = gold;
        Gem = gem;
    }
}
```

Review focus:

```text
Good for teaching UI + Data + Event + Sound.
No Pool needed unless claim FX is requested.
```

---

## 2. Shop Purchase

Modules:

```text
[x] UI  [x] Event  [ ] Pool  [x] Sound  [x] Data  [x] Config
```

Use case:

```text
Open shop -> select item -> validate price -> deduct currency -> save inventory/currency -> publish purchase event -> play success/fail sound
```

Core flow:

```csharp
public static class ShopFeature
{
    public static void Open()
    {
        Main.UI.Open<ShopView>(ShopService.BuildModel());
    }

    public static bool TryBuy(string itemId)
    {
        ShopPurchaseResult result = ShopService.TryBuy(itemId);

        if (!result.Success)
        {
            Main.Sound.Play(ShopSoundKeys.PurchaseFail);
            Main.Event.Publish(new ShopPurchaseFailedEvent(itemId, result.Reason));
            return false;
        }

        Main.Data.Save(SaveType.Json, ShopSave.FileName, ShopService.Save);
        Main.Event.Publish(new ShopItemPurchasedEvent(itemId, result.Price));
        Main.Sound.Play(ShopSoundKeys.PurchaseSuccess);
        return true;
    }
}

public sealed class ShopItemPurchasedEvent : GameEvent
{
    public string ItemId { get; }
    public int Price { get; }

    public ShopItemPurchasedEvent(string itemId, int price)
    {
        ItemId = itemId;
        Price = price;
    }
}
```

Review focus:

```text
Good for teaching validation before side effects.
Config should store item price and item data.
Data should store owned items/currency, not static item definitions.
```

---

## 3. Inventory Item Detail

Modules:

```text
[x] UI  [x] Event  [ ] Pool  [x] Sound  [x] Data  [x] Config
```

Use case:

```text
Open inventory -> select item -> equip/use item -> update save -> publish inventory changed -> refresh UI
```

Core flow:

```csharp
public static class InventoryFeature
{
    public static void Open()
    {
        Main.UI.Open<InventoryView>(InventoryService.BuildModel());
    }

    public static bool TryEquip(string itemId)
    {
        if (!InventoryService.HasItem(itemId)) return false;

        InventoryService.Equip(itemId);
        Main.Data.Save(SaveType.Json, InventorySave.FileName, InventoryService.Save);
        Main.Event.Publish(new InventoryItemEquippedEvent(itemId));
        Main.Sound.Play(InventorySoundKeys.Equip);
        return true;
    }
}

public sealed class InventoryItemEquippedEvent : GameEvent
{
    public string ItemId { get; }

    public InventoryItemEquippedEvent(string itemId)
    {
        ItemId = itemId;
    }
}
```

Review focus:

```text
Useful for UI + Data + Event.
Pool not useful unless item icons or drag previews are pooled.
```

---

## 4. Enemy Spawner

Modules:

```text
[ ] UI  [x] Event  [x] Pool  [x] Sound  [ ] Data  [x] Config
```

Use case:

```text
Start wave -> read spawn config -> spawn enemies from pool -> publish spawned/dead events -> play wave sound
```

Core flow:

```csharp
public sealed class EnemySpawnerFeature : MonoBehaviour
{
    [SerializeField] private EnemySpawnerConfig config;

    public void StartWave(int waveIndex)
    {
        EnemyWave wave = config.GetWave(waveIndex);
        Main.Sound.Play(EnemySpawnerSoundKeys.WaveStart);

        foreach (EnemySpawnEntry entry in wave.Enemies)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                GameObject enemy = Main.Pool
                    .BuildByKey<GameObject>(entry.PoolKey)
                    .SetPosition(config.GetSpawnPosition())
                    .Spawn();

                Main.Event.Publish(new EnemySpawnedEvent(entry.EnemyId, enemy));
            }
        }
    }
}

public sealed class EnemySpawnedEvent : GameEvent
{
    public string EnemyId { get; }
    public GameObject Instance { get; }

    public EnemySpawnedEvent(string enemyId, GameObject instance)
    {
        EnemyId = enemyId;
        Instance = instance;
    }
}
```

Review focus:

```text
Good for teaching Pool + Config + Event.
No Data unless wave progress must persist.
```

---

## 5. Skill Cast

Modules:

```text
[ ] UI  [x] Event  [x] Pool  [x] Sound  [ ] Data  [x] Config
```

Use case:

```text
Request cast -> validate cooldown/mana -> spawn VFX/projectile -> play sound -> publish cast event
```

Core flow:

```csharp
public static class SkillFeature
{
    public static bool TryCast(SkillCastContext context)
    {
        SkillConfigItem skill = SkillService.GetConfig(context.SkillId);
        if (!SkillService.CanCast(context, skill)) return false;

        Main.Pool
            .BuildByKey<GameObject>(skill.VfxPoolKey)
            .SetPosition(context.CastPosition)
            .SetRotation(context.CastRotation)
            .AutoDespawn(skill.VfxLifetime)
            .Spawn();

        Main.Sound.Play(skill.SoundId);
        Main.Event.Publish(new SkillCastedEvent(context.SkillId, context.Caster));
        return true;
    }
}

public sealed class SkillCastedEvent : GameEvent
{
    public string SkillId { get; }
    public GameObject Caster { get; }

    public SkillCastedEvent(string skillId, GameObject caster)
    {
        SkillId = skillId;
        Caster = caster;
    }
}
```

Review focus:

```text
Great Pool + Sound + Config sample.
Data should stay off unless player skill level/cooldown persistence is needed.
```

---

## 6. Level Complete

Modules:

```text
[x] UI  [x] Event  [ ] Pool  [x] Sound  [x] Data  [x] Config
```

Use case:

```text
Level ends -> calculate stars/rewards -> save best result -> publish event -> open result UI -> play music/sfx
```

Core flow:

```csharp
public static class LevelCompleteFeature
{
    public static void Complete(LevelCompleteContext context)
    {
        LevelCompleteResult result = LevelCompleteService.Calculate(context);
        LevelCompleteService.SaveBestResult(result);

        Main.Data.Save(SaveType.Json, LevelProgressSave.FileName, LevelCompleteService.Save);
        Main.Event.Publish(new LevelCompletedEvent(result.LevelId, result.Stars));
        Main.Sound.Play(LevelCompleteSoundKeys.Win);
        Main.UI.Open<LevelCompleteView>(result, hidePrev: true);
    }
}

public sealed class LevelCompletedEvent : GameEvent
{
    public string LevelId { get; }
    public int Stars { get; }

    public LevelCompletedEvent(string levelId, int stars)
    {
        LevelId = levelId;
        Stars = stars;
    }
}
```

Review focus:

```text
Good full gameplay-to-UI bridge.
Pool optional for reward FX.
```

---

## 7. Chest Open

Modules:

```text
[x] UI  [x] Event  [x] Pool  [x] Sound  [x] Data  [x] Config
```

Use case:

```text
Open chest popup -> roll rewards from config -> spawn reward FX -> save rewards -> publish event -> show result UI
```

Core flow:

```csharp
public static class ChestFeature
{
    public static void Open(string chestId)
    {
        Main.UI.Open<ChestView>(ChestService.BuildModel(chestId));
    }

    public static ChestOpenResult OpenChest(string chestId, Vector3 fxPosition)
    {
        ChestOpenResult result = ChestService.Roll(chestId);
        ChestService.ApplyRewards(result);

        Main.Pool
            .BuildByKey<GameObject>(ChestPoolKeys.OpenFx)
            .SetPosition(fxPosition)
            .AutoDespawn(1.5f)
            .Spawn();

        Main.Sound.Play(ChestSoundKeys.Open);
        Main.Data.Save(SaveType.Json, ChestSave.FileName, ChestService.Save);
        Main.Event.Publish(new ChestOpenedEvent(chestId, result.Rewards.Count));
        return result;
    }
}
```

Review focus:

```text
This is a strong sample because every checked module has a real reason.
```

---

## 8. Settings Panel

Modules:

```text
[x] UI  [ ] Event  [ ] Pool  [x] Sound  [x] Data  [ ] Config
```

Use case:

```text
Open settings -> change volume/toggle -> update SoundManager -> save settings
```

Core flow:

```csharp
public static class SettingsFeature
{
    public static void Open()
    {
        Main.UI.Open<SettingsView>(SettingsService.BuildModel());
    }

    public static void SetSfxEnabled(bool enabled)
    {
        Main.Sound.SetSoundTypeEnabled(SoundType.SFX, enabled);
        Main.Sound.SaveSettings();
        Main.Sound.Play(SettingsSoundKeys.Toggle);
    }

    public static void SetMusicVolume(float volume)
    {
        Main.Sound.SetVolumeForType(SoundType.MUSIC, volume);
        Main.Sound.SaveSettings();
    }
}
```

Review focus:

```text
Data may be implicit because SoundManager already saves settings with Main.Data.
No Event unless other systems need settings change notifications.
```

---

## 9. Quest Progress

Modules:

```text
[x] UI  [x] Event  [ ] Pool  [x] Sound  [x] Data  [x] Config
```

Use case:

```text
Subscribe gameplay events -> update quest progress -> save -> publish quest updated/completed -> open quest UI
```

Core flow:

```csharp
public sealed class QuestFeature : MonoBehaviour
{
    private void OnEnable()
    {
        Main.Event.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
    }

    private void OnDisable()
    {
        Main.Event.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
    }

    public void Open()
    {
        Main.UI.Open<QuestView>(QuestService.BuildModel());
    }

    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        QuestUpdateResult result = QuestService.AddProgress("kill_enemy", evt.EnemyId, 1);
        Main.Data.Save(SaveType.Json, QuestSave.FileName, QuestService.Save);
        Main.Event.Publish(new QuestProgressChangedEvent(result.QuestId, result.Current, result.Target));

        if (result.Completed)
        {
            Main.Sound.Play(QuestSoundKeys.Completed);
            Main.Event.Publish(new QuestCompletedEvent(result.QuestId));
        }
    }
}
```

Review focus:

```text
Good sample for Subscribe/Unsubscribe discipline.
Pool not needed.
```

---

## 10. Floating Damage Popup

Modules:

```text
[ ] UI  [x] Event  [x] Pool  [x] Sound  [ ] Data  [x] Config
```

Use case:

```text
Damage event -> spawn pooled popup at world position -> configure text/color -> auto despawn -> optional hit sound
```

Core flow:

```csharp
public sealed class DamagePopupFeature : MonoBehaviour
{
    [SerializeField] private DamagePopupConfig config;

    private void OnEnable()
    {
        Main.Event.Subscribe<DamageDealtEvent>(OnDamageDealt);
    }

    private void OnDisable()
    {
        Main.Event.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
    }

    private void OnDamageDealt(DamageDealtEvent evt)
    {
        DamagePopup popup = Main.Pool
            .BuildByKey<DamagePopup>(DamagePopupPoolKeys.Popup)
            .SetPosition(evt.WorldPosition)
            .AutoDespawn(config.Lifetime)
            .Configure(x => x.SetValue(evt.Amount, config.GetColor(evt.Type)))
            .Spawn();

        if (evt.IsCritical)
        {
            Main.Sound.Play(DamagePopupSoundKeys.CriticalHit);
        }
    }
}
```

Review focus:

```text
Good non-UI Pool sample.
It teaches Configure(...) on PoolBuilder.
```

---

## What The Generator Should Ask

```text
Feature Name: DailyReward
Root Path: Assets/_Game/Features

Preset:
( ) Empty
( ) UI Feature
( ) Gameplay Feature
( ) Full Feature

Modules:
[ ] UI
[ ] Event
[ ] Pool
[ ] Sound
[ ] Data
[ ] Config
[ ] Command
[ ] Localization
[ ] RedDot
[ ] Debug

Options:
[ ] Generate Example MonoBehaviour
[ ] Generate README
[ ] Add TODO comments
[ ] Use async Data API
[ ] Use View<TModel>
[ ] Use Pool BuildByKey
```

## What The Generator Should Not Do Yet

```text
- Do not auto-add generated View prefab to ListViewSO until prefab generation is stable.
- Do not auto-add sound IDs to ListSoundSO until sound asset workflow is clear.
- Do not force Pool/Sound/Data if checkbox is off.
- Do not generate 15 empty files. Only generate files with real code paths.
```

## First Tool Scope

```text
1. Generate folders.
2. Generate C# files for selected modules.
3. Generate one Example file showing real feature flow.
4. Generate README for humans.
5. Log next manual steps:
   - Add View prefab to ListViewSO.
   - Add Sound IDs to ListSoundSO.
   - Add Pool keys/config to PoolManager.
   - Assign Config ScriptableObject.
```
