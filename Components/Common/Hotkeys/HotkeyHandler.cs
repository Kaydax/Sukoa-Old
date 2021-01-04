using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Sukoa.Components.Common.Hotkeys
{
  public struct HotkeyTriggerData
  {
    public HotkeyTriggerData(HotkeyTrigger trigger)
    {
      Key = trigger.Key;
      Shift = trigger.Shift;
      Ctrl = trigger.Ctrl;
      Alt = trigger.Alt;
    }

    public Key Key { get; }

    public bool Shift { get; }
    public bool Ctrl { get; }
    public bool Alt { get; }

    public override bool Equals(object? obj)
    {
      return obj is HotkeyTriggerData data &&
             Key == data.Key &&
             Shift == data.Shift &&
             Ctrl == data.Ctrl &&
             Alt == data.Alt;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Key, Shift, Ctrl, Alt);
    }
  }

  class HotkeyLock
  {
    public HotkeyTriggerData? LockedHotkey { get; private set; }
    public bool Locked => LockedHotkey != null;

    public void Lock(HotkeyTriggerData hotkey)
    {
      LockedHotkey = hotkey;
    }

    public void Unlock()
    {
      LockedHotkey = null;
    }
  }

  public class HotkeyData
  {
    public HotkeyData(string name, object value, HotkeyTriggerData[] triggers)
    {
      Name = name;
      Value = value;
      Triggers = triggers;
    }

    public string Name { get; }
    public object Value { get; }
    public HotkeyTriggerData[] Triggers { get; }
  }

  public class HotkeyHandler
  {
    public HotkeyHandler(HotkeyHandler parent, IReadOnlyDictionary<HotkeyTriggerData, object> registeredHotkeys)
    {
      Lock = parent.Lock;
      Parent = parent;
      RegisteredHotkeys = registeredHotkeys;
    }

    public HotkeyHandler(IReadOnlyDictionary<HotkeyTriggerData, object> registeredHotkeys)
    {
      Lock = new HotkeyLock();
      RegisteredHotkeys = registeredHotkeys;
    }

    HotkeyLock Lock { get; }
    public HotkeyHandler? Parent { get; }

    public void Update(bool enabled)
    {
      if(Lock.Locked && !IsLocking)
      {
        return;
      }

      var shift = ImGui.IsKeyDown((int)Key.ShiftLeft) || ImGui.IsKeyDown((int)Key.ShiftRight);
      var ctrl = ImGui.IsKeyDown((int)Key.ControlLeft) || ImGui.IsKeyDown((int)Key.ControlRight);
      var alt = ImGui.IsKeyDown((int)Key.AltLeft) || ImGui.IsKeyDown((int)Key.AltRight);

      bool modifiersMatch(HotkeyTriggerData trigger)
      {
        return shift == trigger.Shift && ctrl == trigger.Ctrl && alt == trigger.Alt;
      }

      if(Lock.Locked && IsLocking)
      {
        if(!ImGui.IsKeyDown((int)Lock.LockedHotkey!.Value.Key))
        {
          Lock.Unlock();
          IsLocking = false;
          SelectedHotkey = null;
        }
        else
        {
          if(modifiersMatch(Lock.LockedHotkey.Value) && ImGui.IsKeyPressed((int)Lock.LockedHotkey.Value.Key))
          {
            SelectedHotkey = Lock.LockedHotkey.Value;
          }
          else
          {
            SelectedHotkey = null;
          }
        }
      }
      else
      {
        if(enabled)
        {
          foreach(var h in RegisteredHotkeys)
          {
            if(modifiersMatch(h.Key) && ImGui.IsKeyPressed((int)h.Key.Key))
            {
              IsLocking = true;
              Lock.Lock(h.Key);
              SelectedHotkey = h.Key;
              break;
            }
          }
        }
      }
    }

    protected bool IsLocking { get; set; }
    public IReadOnlyDictionary<HotkeyTriggerData, object> RegisteredHotkeys { get; }
    protected HotkeyTriggerData? SelectedHotkey { get; set; }
  }

  public class HotkeyHandler<T> : HotkeyHandler where T : struct
  {
    public T? CurrentHotkey => SelectedHotkey != null ? (T?)RegisteredHotkeys[SelectedHotkey.Value] : null;

    static IReadOnlyDictionary<HotkeyTriggerData, object> ConvertEnumHotkeys(Type enumType)
    {
      if(!enumType.IsEnum)
      {
        throw new Exception("Generic type of HotkeyHandler must be a hotkey enum");
      }

      var values = new List<object>();
      foreach(var val in enumType.GetEnumValues()) values.Add(val);

      var hotkeyData = values.Select(value =>
      {
        MemberInfo memberInfo =
            enumType.GetMember(value.ToString()!).First();
        string name = memberInfo.GetCustomAttributes<HotkeyName>().FirstOrDefault()?.Name ?? memberInfo.Name;
        var triggers = memberInfo.GetCustomAttributes<HotkeyTrigger>().Select(trigger => new HotkeyTriggerData(trigger)).ToArray();
        return new HotkeyData(name, value, triggers);
      }).ToArray();

      var dict = new Dictionary<HotkeyTriggerData, object>();
      foreach(var hotkey in hotkeyData)
      {
        foreach(var trigger in hotkey.Triggers)
        {
          dict.Add(trigger, hotkey.Value);
        }
      }

      return dict;
    }

    public HotkeyHandler() : base(ConvertEnumHotkeys(typeof(T)))
    {

    }
  }
}
