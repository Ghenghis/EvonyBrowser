# 🔑 Evony Keys, Constants & Data Reference

**Version:** 1.0  
**Last Updated:** 2025-01-12  
**Status:** Complete Data Reference for Manus

---

## 📋 Overview

This document contains all extracted **keys**, **constants**, **IDs**, and **reference data** from the Evony protocol and knowledge base for use in reverse engineering and automation.

---

## 🏰 Building Type IDs

### Inside City Buildings (typeId)
| ID  | Name           | Max Lvl | Notes                   |
| --- | -------------- | ------- | ----------------------- |
| 1   | Town Hall      | 10      | Main building, required |
| 2   | Cottage        | 10      | Population capacity     |
| 3   | Warehouse      | 10      | Resource protection     |
| 4   | Barracks       | 10      | Infantry training       |
| 5   | Academy        | 10      | Research center         |
| 6   | Forge          | 10      | Gear crafting           |
| 7   | Workshop       | 10      | Siege equipment         |
| 8   | Stable         | 10      | Cavalry training        |
| 9   | Relief Station | 10      | Troop healing           |
| 10  | Embassy        | 10      | Reinforcement hosting   |
| 11  | Marketplace    | 10      | Trading                 |
| 12  | Inn            | 10      | Hero recruitment        |
| 13  | Feasting Hall  | 10      | Hero capacity           |
| 14  | Rally Spot     | 10      | March coordination      |
| 15  | Beacon Tower   | 10      | Intelligence reports    |
| 16  | Walls          | 10      | Defense fortification   |

### Outside City Buildings (Resource Fields)
| ID  | Name     | Max Lvl | Resource |
| --- | -------- | ------- | -------- |
| 20  | Farm     | 10      | Food     |
| 21  | Sawmill  | 10      | Lumber   |
| 22  | Quarry   | 10      | Stone    |
| 23  | Ironmine | 10      | Iron     |

---

## 📍 Position ID Mapping

### Inside City Grid (1001-1040)
```
Town Hall:      1001 (center, always exists)
Top Row:        1002, 1003, 1004, 1005
Upper Row:      1006, 1007, 1008, 1009, 1010
Middle Row:     1011, 1012, 1013, 1014, 1015
Lower Row:      1016, 1017, 1018, 1019, 1020
Bottom Row:     1021, 1022, 1023, 1024, 1025
Extra slots:    1026-1040
```

### Outside City Grid (2001-2040)
```
Resource fields arranged in rings around city:
Inner Ring:   2001-2010
Outer Ring:   2011-2020
Extended:     2021-2040
```

---

## ⚔️ Troop Type Codes

### Code Mapping
| Code | Name        | ID  | Category |
| ---- | ----------- | --- | -------- |
| wo   | Worker      | 1   | Support  |
| w    | Warrior     | 2   | Infantry |
| s    | Scout       | 3   | Cavalry  |
| p    | Pikeman     | 4   | Infantry |
| sw   | Swordsman   | 5   | Infantry |
| a    | Archer      | 6   | Ranged   |
| c    | Cavalry     | 7   | Cavalry  |
| cata | Cataphract  | 8   | Cavalry  |
| t    | Transporter | 9   | Support  |
| b    | Ballista    | 10  | Siege    |
| r    | Ram         | 11  | Siege    |
| cp   | Catapult    | 12  | Siege    |

### Troop Notation Format
```actionscript
// Standard format
c.getValue(Troop, "wo:100,w:500,s:50,p:0,sw:0,a:1000,c:200,cata:50,t:100,b:10,r:5,cp:2")

// Simplified (zeros omitted)
c.getValue(Troop, "a:1000,c:200,s:50")
```

---

## 🎯 Mission Type Codes

| Code | Name      | Description            | Returns |
| ---- | --------- | ---------------------- | ------- |
| 1    | Attack    | Attack target location | Yes     |
| 2    | Reinforce | Send troops to ally    | No*     |
| 3    | Scout     | Gather intelligence    | Yes     |
| 4    | Transport | Send resources         | Yes     |
| 5    | Occupy    | Capture NPC/valley     | Yes     |
| 6    | Colonize  | Build new city         | No      |
| 7    | Rally     | Move to rally point    | Yes     |

*Reinforcements stay until recalled

---

## 🔬 Research Technology IDs

### Military Tech
| ID  | Name               | Max | Effect                  |
| --- | ------------------ | --- | ----------------------- |
| 1   | Military Science   | 10  | +5% troop attack/level  |
| 2   | Military Tradition | 10  | +5% troop defense/level |
| 3   | Iron Working       | 10  | +5% cavalry attack      |
| 4   | Logistics          | 10  | +10% march speed        |
| 5   | Compass            | 10  | +5% troop load capacity |
| 6   | Horseback Riding   | 10  | +10% cavalry speed      |
| 7   | Archery            | 10  | +5% archer attack       |
| 8   | Machinery          | 10  | +5% siege attack        |
| 9   | Medicine           | 10  | +10% healing speed      |
| 10  | Construction       | 10  | +10% building speed     |

### Economy Tech
| ID  | Name          | Max | Effect                  |
| --- | ------------- | --- | ----------------------- |
| 11  | Agriculture   | 10  | +10% food production    |
| 12  | Lumbering     | 10  | +10% lumber production  |
| 13  | Masonry       | 10  | +10% stone production   |
| 14  | Mining        | 10  | +10% iron production    |
| 15  | Metal Casting | 10  | +10% all production     |
| 16  | Informatics   | 10  | +10% research speed     |
| 17  | Stockpile     | 10  | +10% warehouse capacity |
| 18  | Privateering  | 10  | +10% plunder capacity   |

---

## 🦸 Hero Attributes

### Attribute Types
| Attribute    | Code | Effect                           |
| ------------ | ---- | -------------------------------- |
| Politics     | pol  | Resource production, build speed |
| Attack       | att  | Troop attack power               |
| Intelligence | int  | Research speed, skill effects    |

### Hero Grades
| Grade     | Base Stats | Color  |
| --------- | ---------- | ------ |
| Common    | 20-40      | White  |
| Uncommon  | 40-60      | Green  |
| Rare      | 60-75      | Blue   |
| Epic      | 75-90      | Purple |
| Legendary | 90-100+    | Orange |

### Historic Heroes (from RAG)
```python
historic_heroes = [
    # ID, Name, Base Attack, Base Intelligence, Base Politics
    (1, "Julius Caesar", 95, 90, 98),
    (2, "Alexander the Great", 100, 85, 80),
    (3, "Napoleon", 98, 92, 88),
    (4, "Genghis Khan", 100, 75, 70),
    (5, "Sun Tzu", 80, 100, 85),
    (6, "Cleopatra", 60, 90, 100),
    (7, "Joan of Arc", 90, 85, 75),
    (8, "Hannibal", 95, 88, 82),
    # ... (692 total heroes in database)
]
```

---

## 🎒 Item IDs

### Speed Up Items
| ID  | Name               | Duration | Type     |
| --- | ------------------ | -------- | -------- |
| 101 | Beginner Speed     | 15 min   | Building |
| 102 | Intermediate Speed | 60 min   | Building |
| 103 | Advanced Speed     | 8 hours  | Building |
| 104 | Elite Speed        | 24 hours | Building |
| 201 | March Speed        | -20%     | March    |
| 202 | Urgent March       | -50%     | March    |
| 301 | Research Speed     | 15 min   | Research |
| 302 | Tech Boost         | 60 min   | Research |

### Resource Items
| ID   | Name          | Amount    |
| ---- | ------------- | --------- |
| 1001 | Food Pack S   | 10,000    |
| 1002 | Food Pack M   | 50,000    |
| 1003 | Food Pack L   | 200,000   |
| 1004 | Food Pack XL  | 1,000,000 |
| 1011 | Lumber Pack S | 10,000    |
| 1021 | Stone Pack S  | 10,000    |
| 1031 | Iron Pack S   | 10,000    |

### Combat Items
| ID   | Name              | Effect              |
| ---- | ----------------- | ------------------- |
| 2001 | Attack Buff       | +25% attack 1 hour  |
| 2002 | Defense Buff      | +25% defense 1 hour |
| 2003 | War Horn          | +50% attack 10 min  |
| 2004 | Smoke Bomb        | Hide march          |
| 2005 | Random Teleport   | Random relocation   |
| 2006 | Targeted Teleport | Specific relocation |

---

## 🌐 Server Configuration

### Server Domains
```python
servers = {
    "cc1": {"domain": "cc1.evony.com", "port": 443, "region": "US"},
    "cc2": {"domain": "cc2.evony.com", "port": 443, "region": "US"},
    "cc3": {"domain": "cc3.evony.com", "port": 443, "region": "EU"},
    "cc4": {"domain": "cc4.evony.com", "port": 443, "region": "EU"},
    "cc5": {"domain": "cc5.evony.com", "port": 443, "region": "Asia"},
}
```

### API Endpoints
```python
endpoints = {
    "login": "/api/login",
    "game": "/api/game",
    "static": "/static",
    "swf": "/flash",
    "ws": "/ws"
}
```

---

## 🔐 Security Constants

### Known Salt Values
```python
# Extracted from SWF decompilation
SALTS = {
    "login": "evony",
    "api": "gameapi2010",
    "session": "flash_session_2010",
    "action": "action_verify_2010"
}
```

### Hash Algorithms
```python
# Hash function usage
HASH_CONFIG = {
    "password": "md5",
    "session_token": "sha1",
    "action_verify": "md5",
    "file_checksum": "sha256"
}
```

### Token Expiry
```python
TOKEN_CONFIG = {
    "session_timeout": 3600,      # 1 hour
    "refresh_interval": 300,      # 5 minutes
    "max_inactive": 1800,         # 30 minutes
    "reconnect_grace": 60         # 1 minute
}
```

---

## 📊 Rate Limits

### API Rate Limits
| Action Category | Limit     | Cooldown |
| --------------- | --------- | -------- |
| Login           | 5/minute  | 60s ban  |
| March           | 10/minute | 30s wait |
| Build           | 20/minute | 15s wait |
| Research        | 10/minute | 30s wait |
| Chat            | 30/minute | 5s wait  |
| API General     | 60/minute | 60s ban  |

### Bypass Detection
```python
# Patterns that trigger detection
DETECTION_PATTERNS = [
    "request_interval < 0.5s",
    "identical_requests > 5",
    "api_calls > 100/min",
    "missing_user_agent",
    "automated_header_patterns"
]
```

---

## 🗺️ Map Constants

### Field Types
| Type | Name        | ID Range   |
| ---- | ----------- | ---------- |
| 0    | Flat        | N/A        |
| 1    | Forest      | N/A        |
| 2    | Desert      | N/A        |
| 3    | Hill        | N/A        |
| 4    | Swamp       | N/A        |
| 5    | Lake        | N/A        |
| 10   | NPC City    | Level 1-10 |
| 20   | Valley      | Types vary |
| 30   | Player City | N/A        |

### Coordinate System
```python
# Field ID to coordinates
def field_to_coords(field_id, map_width=800):
    x = field_id % map_width
    y = field_id // map_width
    return (x, y)

# Coordinates to Field ID
def coords_to_field(x, y, map_width=800):
    return y * map_width + x

# Distance calculation
def distance(field1, field2, map_width=800):
    x1, y1 = field_to_coords(field1, map_width)
    x2, y2 = field_to_coords(field2, map_width)
    return ((x2-x1)**2 + (y2-y1)**2) ** 0.5
```

---

## 📁 File Paths

### Critical Paths
```python
PATHS = {
    "autoevony_swf": "D:/Fiddler-FlashBrowser/AutoEvony.swf",
    "evony_client_swf": "D:/Fiddler-FlashBrowser/EvonyClient1921.swf",
    "flash_plugin": "D:/Fiddler-FlashBrowser/FlashBrowser_x64/Assets/Plugins/pepflashplayer.dll",
    "fiddler_rules": "D:/Fiddler-FlashBrowser/config/EvonyRE-CustomRules.cs",
    "sol_cache": "%APPDATA%/CefFlashBrowser/Cache",
    "logs": "D:/Fiddler-FlashBrowser/logs",
    "captures": "D:/Fiddler-FlashBrowser/captures"
}
```

### SOL File Structure
```python
SOL_FILES = {
    "evony.sol": "Main game data",
    "settings.sol": "User preferences",
    "session.sol": "Auth tokens",
    "player.sol": "Player cache"
}
```

---

## 📚 RAG Knowledge Base Stats

```
Total Chunks: 339,160
Total Symbols: 55,871
Categories:
  - Scripts: ~200,000 chunks
  - Account Data: ~100,000 chunks
  - Documentation: ~30,000 chunks
  - Protocol: ~10,000 chunks

Query Modes:
  - research: General queries
  - forensics: Deep analysis
  - full_access: All data
```

---

## 📚 Related Documentation

- [EVONY-PROTOCOLS-EXTENDED.md](./EVONY-PROTOCOLS-EXTENDED.md) - Protocol reference
- [EXPLOITS-WORKAROUNDS.md](./EXPLOITS-WORKAROUNDS.md) - RE techniques
- [FIDDLER-SCRIPTS.md](./FIDDLER-SCRIPTS.md) - Automation scripts
