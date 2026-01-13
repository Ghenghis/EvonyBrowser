# Evony Protocol Reference

## Overview

Evony Age I uses a custom binary protocol over TCP/HTTP with AMF3 (Action Message Format) encoding. This document covers the protocol structure, message types, and API commands extracted from the AI-Evony knowledge base.

---

## Protocol Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    PROTOCOL STACK                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              APPLICATION LAYER                          │ │
│  │  • Game Commands (hero, castle, troop, etc.)           │ │
│  │  • Response Handlers                                    │ │
│  └────────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              MESSAGE LAYER                              │ │
│  │  • Message Type                                         │ │
│  │  • Sequence ID                                          │ │
│  │  • Payload                                              │ │
│  └────────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              ENCODING LAYER                             │ │
│  │  • AMF3 Serialization                                   │ │
│  │  • ZLIB Compression (optional)                          │ │
│  └────────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              SECURITY LAYER                             │ │
│  │  • AES-256 Encryption                                   │ │
│  │  • MD5/SHA1 Authentication Hashing                      │ │
│  │  • Session Token Management                             │ │
│  └────────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              TRANSPORT LAYER                            │ │
│  │  • TCP Socket (primary)                                 │ │
│  │  • HTTP/HTTPS (fallback)                                │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## AMF3 Message Format

### Header Structure
```
┌──────────────┬──────────────┬──────────────┬──────────────┐
│  Length (4)  │  Type (1)    │  SeqID (4)   │  Flags (1)   │
├──────────────┴──────────────┴──────────────┴──────────────┤
│                     Payload (variable)                     │
└───────────────────────────────────────────────────────────┘
```

| Field   | Size     | Description                               |
| ------- | -------- | ----------------------------------------- |
| Length  | 4 bytes  | Total message length (big-endian)         |
| Type    | 1 byte   | Message type identifier                   |
| SeqID   | 4 bytes  | Sequence ID for request/response matching |
| Flags   | 1 byte   | Compression, encryption flags             |
| Payload | Variable | AMF3-encoded data                         |

### AMF3 Detection
From Fiddler CustomRules:
```csharp
// AMF magic bytes: 0x00 0x03
// Content-Type: application/x-amf
```

---

## Login Response Structure

From knowledge base account data:

```json
{
  "msg": "login success",
  "player": {
    "castles": [
      {
        "status": 0,
        "buildings": [
          {
            "positionId": 1040,
            "status": 0,
            "help": 0,
            "level": 10,
            "appearance": 44,
            "typeId": 7,
            "startTime": 0.0,
            "endTime": 0.0,
            "name": "..."
          }
        ]
      }
    ]
  }
}
```

### Building Type IDs
| typeId | Building       |
| ------ | -------------- |
| 1      | Town Hall      |
| 2      | Cottage        |
| 3      | Warehouse      |
| 4      | Barracks       |
| 5      | Academy        |
| 6      | Forge          |
| 7      | Workshop       |
| 8      | Stable         |
| 9      | Relief Station |
| 10     | Embassy        |
| 11     | Marketplace    |
| 12     | Inn            |
| 13     | Feasting Hall  |
| 14     | Rally Spot     |
| 15     | Beacon Tower   |
| 16     | Walls          |

### Position IDs
- **1001-1040**: Inside city buildings
- **2001-2040**: Outside city (resource fields)

---

## API Command Categories

### Castle Commands
```actionscript
// Get castle information
c.af.getCastleCommands().getCastleInfo(castleId)

// Upgrade building
c.af.getCastleCommands().upgradeBuilding(castleId, positionId)

// Demolish building
c.af.getCastleCommands().demolishBuilding(castleId, positionId)
```

### Hero Commands
```actionscript
// Get hero list from tavern
c.af.getHeroCommands().getHerosListFromTavern(castleId)

// Hire hero
c.af.getHeroCommands().hireHero(castleId, heroName)

// Fire hero
c.af.getHeroCommands().fireHero(castleId, heroId)

// Level up hero
c.af.getHeroCommands().levelUpHero(heroId)

// Assign hero to army
c.af.getHeroCommands().assignHeroToArmy(heroId, armyId)
```

### Troop Commands
```actionscript
// Train troops
c.af.getTroopCommands().trainTroop(castleId, troopType, amount)

// Get troop count
c.castle.troop
c.cm.localArmies[0].armyId

// All troops across cities
c.cityManager.allTroop
```

### Army/March Commands
```actionscript
// Create army
c.af.getArmyCommands().createArmy(castleId, heroId, troops)

// Send march
c.af.getArmyCommands().sendArmy(armyId, targetX, targetY, missionType)

// Recall army
c.af.getArmyCommands().recallArmy(armyId)

// Get march time
c.cm.getBaseTravelTime(fromFieldId, toFieldId, troops)
```

### Capital/Policy Commands
```actionscript
// Get policy times remaining
c.af.getCapitalCommands().getLeftPoliciesTimes(castleId)

// Levy army (draft troops)
c.af.getCapitalCommands().levyArmy(castleId, fieldId)

// Levy taxes (gold)
c.af.getCapitalCommands().levyTaxes(castleId, fieldId)

// Levy food
c.af.getCapitalCommands().levyFood(castleId, fieldId)
```

### Shop Commands
```actionscript
// Use item
c.af.getShopCommands().useTrickItem(fieldId, armyId, itemId)

// Buy item
c.af.getShopCommands().buyItem(itemId, amount)
```

### Map Commands
```actionscript
// Get map fields
c.getMapFieldList(fieldType, level, radius, centerFieldId)

// Field types: Castle, npc, resource, valley
// Example: Get all level 10 NPCs within radius
c.getMapFieldList(npc, 10, radius, c.castle.fieldId)

// Scan map area
scanmap coord radius
```

### Alliance Commands
```actionscript
// Get alliance info
c.af.getAllianceCommands().getAllianceInfo(allianceId)

// Join alliance
c.af.getAllianceCommands().joinAlliance(allianceId)

// Leave alliance
c.af.getAllianceCommands().leaveAlliance()
```

---

## Troop Types

| Code | Name        | Type     |
| ---- | ----------- | -------- |
| wo   | Worker      | Support  |
| w    | Warrior     | Infantry |
| s    | Scout       | Cavalry  |
| p    | Pikeman     | Infantry |
| sw   | Swordsman   | Infantry |
| a    | Archer      | Ranged   |
| c    | Cavalry     | Cavalry  |
| cata | Cataphract  | Cavalry  |
| t    | Transporter | Support  |
| b    | Ballista    | Siege    |
| r    | Ram         | Siege    |
| cp   | Catapult    | Siege    |

### Troop Food Consumption
```
militia (wo): 1 food/hour
warrior (w): 2 food/hour
scout (s): 3 food/hour
pikeman (p): 4 food/hour
swordsman (sw): 5 food/hour
archer (a): 6 food/hour
cavalry (c): 8 food/hour
cataphract (cata): 10 food/hour
transporter (t): 5 food/hour
ballista (b): 10 food/hour
ram (r): 15 food/hour
catapult (cp): 20 food/hour
```

---

## Response Handler Pattern

From script analysis:
```actionscript
// Send message and wait for response
set response $c.af.getHeroCommands().getHerosListFromTavern(castleId)$
gosub waitResp %response% 10

// Wait subroutine
label waitResp
sleep 1
iferror $%_1%.ok$ goto waitResp
if {%_2%<0} gosubreturn  // Timeout
set _2 {%_2%-1}
goto waitResp
```

### Response Structure
```json
{
  "ok": true,           // Success flag
  "errorMsg": null,     // Error message if failed
  "data": {             // Response payload
    // Varies by command
  }
}
```

---

## Packet Classification (Fiddler)

From `EvonyRE-CustomRules.cs`:

| Category | Keywords                           | Priority |
| -------- | ---------------------------------- | -------- |
| LOGIN    | login, auth, session               | High     |
| MARCH    | march, troop, army, move           | High     |
| BATTLE   | battle, attack, combat, fight      | Critical |
| RESOURCE | resource, gold, food, lumber, iron | Medium   |
| CHAT     | chat, message, mail                | Low      |
| BUILD    | build, upgrade, construct          | Medium   |
| RESEARCH | research, tech, academy            | Medium   |
| ALLIANCE | alliance, guild, member            | Medium   |
| HERO     | hero, tavern, hire, fire           | Medium   |
| QUEST    | quest, mission, reward             | Low      |

---

## Security Details

### Authentication Flow
```
1. Client sends: username + MD5(password)
2. Server validates credentials
3. Server returns session token
4. Client includes token in subsequent requests
5. Token expires after inactivity (typically 1 hour)
```

### Encryption Patterns (from evony_master_analyzer.py)
```python
encryption_patterns = {
    'aes': [
        'AES.encrypt', 'AES.decrypt',
        'Rijndael', 'CryptoJS.AES',
        'SecurityManager.encrypt',
        'EncryptionManager'
    ],
    'rc4': ['RC4', 'ArcFour', 'StreamCipher'],
    'xor': ['XOR', 'ByteXOR'],
    'base64': ['Base64.encode', 'Base64.decode']
}
```

### Known Vulnerabilities (from analysis)
- MD5/SHA1 used for password hashing (weak)
- Static salt values in token generation
- Hardcoded API keys in client code
- Predictable action verification

---

## Network Configuration

From `NETWORK_PROTOCOL_SYSTEM.md`:

```actionscript
// Connection parameters
MAX_CONNECTIONS: 100
CONNECTION_TIMEOUT: 30000 // 30 seconds
RECONNECT_ATTEMPTS: 3

// Queue parameters
MAX_QUEUE_SIZE: 1000
QUEUE_TIMEOUT: 5000 // 5 seconds
MAX_RETRIES: 3

// Protocol parameters
MAX_MESSAGE_SIZE: 1048576 // 1MB
PROTOCOL_VERSION: "1.0"
CHECKSUM_ALGORITHM: "SHA-256"

// Security parameters
ENCRYPTION_ALGORITHM: "AES-256"
KEY_SIZE: 256
SESSION_TIMEOUT: 3600 // 1 hour
```

---

## Server Endpoints

| Server | Domain          | Purpose                 |
| ------ | --------------- | ----------------------- |
| cc1    | cc1.evony.com   | Game server 1           |
| cc2    | cc2.evony.com   | Game server 2 (default) |
| cc3    | cc3.evony.com   | Game server 3           |
| login  | login.evony.com | Authentication          |
| api    | api.evony.com   | REST API                |

---

## SOL (Local Shared Object) Data

Flash stores session data in .sol files:

**Location**: `%APPDATA%\CefFlashBrowser\...`

**Common SOL Files**:
- `evony.sol` - Main game data
- `settings.sol` - User preferences
- `session.sol` - Auth tokens

**SOL Structure**:
```
{
  "username": "player123",
  "server": "cc2",
  "sessionToken": "abc123...",
  "lastLogin": 1704931200,
  "settings": {
    "sound": true,
    "music": false,
    "quality": "high"
  }
}
```

---

## Useful Script Variables

From AutoEvony scripts:

```actionscript
// Player info
c.player                    // Current player object
c.player.name               // Player name
c.player.level              // Player level

// Castle info
c.castle                    // Current castle
c.castle.id                 // Castle ID
c.castle.fieldId            // Map field ID
c.coord                     // Castle coordinates

// City manager
c.cm                        // City manager
c.cm.localArmies            // Local armies
m_city.allCities            // All cities array
m_city.allCities.length     // Number of cities

// Map
c.getValue(Coordinate, "x,y")  // Convert coords to fieldId
c.getValue(Troop, "s:1")       // Get troop value
```

---

## Error Codes

| Code | Meaning                |
| ---- | ---------------------- |
| 0    | Success                |
| -1   | Unknown error          |
| -2   | Invalid session        |
| -3   | Server busy            |
| -4   | Rate limited           |
| -5   | Insufficient resources |
| -6   | Invalid target         |
| -7   | Army busy              |
| -8   | Hero unavailable       |
| -99  | Maintenance mode       |

---

## References

- `g:\AI-Evony\RoboEvony\docs\05_Network\NETWORK_PROTOCOL_SYSTEM.md`
- `g:\AI-Evony\RoboTool\evony_unpack_build_tools\archived_tools\evony_master_analyzer.py`
- `D:\Fiddler-FlashBrowser\config\EvonyRE-CustomRules.cs`
- AI-Evony knowledge base (339,160 chunks)
