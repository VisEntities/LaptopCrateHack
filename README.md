This plugin requires players to use targeting computers to hack into locked crates. Once a crate is unlocked, you can configure it so that only the person who hacked it, along with their teammates, friends, and clanmates, can open it. Additionally, there's a cooldown period players must wait before they can attempt to hack another crate.

[Demonstration](https://youtu.be/TfwtpbyXm5M)

-------------------

## Hacking
Players need to hold a certain number of targeting computers in their hand before they begin hacking. Once the hacking is successful, you can choose whether or not the computers are consumed.
 
 -------------------------

## Configuration
```json
{
  "Version": "2.0.0",
  "Required Targeting Computers For Hack": 1,
  "Consume Targeting Computer On Hack": true,
  "Crate Unlock Time Seconds": 900.0,
  "Cooldown Between Hacks Seconds": 300.0,
  "Crate Lootable By Hacker Only": true,
  "Can Be Looted By Hacker Teammates": true,
  "Can Be Looted By Hacker Friends": false,
  "Can Be Looted By Hacker Clanmates": false
}
```

-----------------

## Localization
```json
{
  "NeedTargetingComputer": "You need to hold <color=#FABE28>{0}</color> targeting computers in your hand to hack this crate.",
  "CooldownBeforeNextHack": "You must wait <color=#FABE28>{0:N0}</color> more seconds before hacking another crate.",
  "CrateLootDenied": "This crate is reserved for the original hacker and cannot be looted."
}
```

------------------

## Credits
 * Rewritten from scratch and maintained to present by **VisEntities**
 * Previous maintenance and contributions by **Arainrr**
 * Originally created by **TheSurgeon**