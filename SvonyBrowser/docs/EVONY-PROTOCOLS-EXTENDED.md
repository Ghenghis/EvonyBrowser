# 📡 Evony Protocol Extended Reference

**Version:** 2.0  
**Last Updated:** 2025-01-12  
**Status:** Complete Protocol Documentation for Manus

---

## 📋 Overview

This document extends the base protocol reference with **complete API commands**, **data structures**, **keys**, and **reverse engineering details** extracted from the AI-Evony knowledge base (339,160 chunks, 55,871 symbols).

---

## 🔑 API Command Complete Reference

### Command Categories

```
c.af.get[CATEGORY]Commands().[ACTION](params)
```

### 1. Castle Commands (`getCastleCommands()`)

```actionscript
// Get castle information
getCastleInfo(castleId)

// Building operations
upgradeBuilding(castleId, positionId)
demolishBuilding(castleId, positionId)
cancelBuilding(castleId, positionId)
speedUpBuilding(castleId, positionId, itemId)

// Resource operations
collectResource(castleId, positionId)
collectAllResources(castleId)

// Castle status
getCastleFieldInfo(castleId)
getCastleDefenseInfo(castleId)
setCastleFlag(castleId, flagType)
```

### 2. Hero Commands (`getHeroCommands()`)

```actionscript
// Tavern operations
getHerosListFromTavern(castleId)
hireHero(castleId, heroName)
refreshHeroList(castleId)

// Hero management
fireHero(castleId, heroId)
levelUpHero(heroId)
addPointToAttribute(heroId, attribute, points)

// Hero assignment
assignHeroToArmy(heroId, armyId)
unassignHero(heroId)
setMayor(castleId, heroId)
removeMayor(castleId)

// Hero equipment
equipItem(heroId, itemId, slot)
unequipItem(heroId, slot)
enhanceHero(heroId, itemId)

// Hero skills
learnSkill(heroId, skillId)
resetSkills(heroId)
```

### 3. Troop Commands (`getTroopCommands()`)

```actionscript
// Training
trainTroop(castleId, troopType, amount)
cancelTraining(castleId, troopType)
speedUpTraining(castleId, itemId)

// Dismissal
dismissTroop(castleId, troopType, amount)
dismissAllTroops(castleId)

// Healing
healTroops(castleId)
```

### 4. Army Commands (`getArmyCommands()`)

```actionscript
// Army creation
createArmy(castleId, heroId, troops)
disbandArmy(armyId)

// March operations
sendArmy(armyId, targetX, targetY, missionType)
recallArmy(armyId)
speedUpMarch(armyId, itemId)

// Army management
setArmyFormation(armyId, formationType)
reinforceAlliance(armyId, targetCastleId)
```

### 5. Colony Commands (`getColonyCommands()`)

```actionscript
// Troop deployment between cities
setDeployTroops(castleId, targetCastleId, troops)

// Colony management
abandonColony(castleId)
setColonyTax(castleId, taxRate)
```

### 6. Capital/Policy Commands (`getCapitalCommands()`)

```actionscript
// Levy operations
getLeftPoliciesTimes(castleId)
levyArmy(castleId, fieldId)      // Draft troops
levyTaxes(castleId, fieldId)     // Collect gold
levyFood(castleId, fieldId)      // Collect food

// Comfort operations
comfort(castleId, comfortType)   // Types: 0=praying, 1=disaster relief, 2=population

// Population management
increasePopulationLimit(castleId)
```

### 7. Shop Commands (`getShopCommands()`)

```actionscript
// Item usage
useTrickItem(fieldId, armyId, itemId)
useSpeedItem(targetId, itemId, targetType)
useResourceItem(castleId, itemId)
useBuffItem(castleId, itemId)

// Shop transactions
buyItem(itemId, amount)
sellItem(itemId, amount)
buyPackage(packageId)
```

### 8. Map Commands

```actionscript
// Field information
c.getMapFieldList(fieldType, level, radius, centerFieldId)

// Coordinate conversion
c.getValue(Coordinate, "x,y")  // Returns fieldId

// Map scanning
scanmap coord radius
```

### 9. Alliance Commands (`getAllianceCommands()`)

```actionscript
// Alliance info
getAllianceInfo(allianceId)
getMemberList(allianceId)
getAllianceRanking()

// Membership
joinAlliance(allianceId)
leaveAlliance()
applyToAlliance(allianceId, message)

// Management
invitePlayer(playerId)
kickMember(memberId)
promoteMember(memberId, rank)
demoteMember(memberId)

// Alliance features
declareWar(targetAllianceId)
proposePeace(targetAllianceId)
setAllianceAnnouncement(message)
```

### 10. Research Commands (`getTechCommands()`)

```actionscript
// Research operations
startResearch(castleId, techId)
cancelResearch(castleId)
speedUpResearch(castleId, itemId)

// Tech info
getTechList(castleId)
getTechRequirements(techId)
```

### 11. Quest Commands (`getQuestCommands()`)

```actionscript
// Quest operations
getQuestList(castleId)
completeQuest(questId)
claimQuestReward(questId)
abandonQuest(questId)

// Daily quests
getDailyQuests(castleId)
refreshDailyQuests(castleId)
```

### 12. Mail Commands (`getMailCommands()`)

```actionscript
// Mail operations
sendMail(playerId, subject, content)
readMail(mailId)
deleteMail(mailId)
deleteAllMail()

// Mail filters
getMailList(mailType, page)
searchMail(keyword)
```

---

## 📦 Data Structures

### Player Object
```json
{
  "playerId": 12345678,
  "name": "PlayerName",
  "level": 50,
  "prestige": 1000000,
  "alliance": {
    "id": 1234,
    "name": "AllianceName",
    "rank": "Officer"
  },
  "castles": [...],
  "items": [...],
  "quests": [...]
}
```

### Castle Object
```json
{
  "id": 12345,
  "name": "Castle Name",
  "fieldId": 123456,
  "status": 0,
  "level": 10,
  "buildings": [...],
  "troops": {...},
  "resources": {...},
  "mayor": {...}
}
```

### Building Object
```json
{
  "positionId": 1040,
  "typeId": 7,
  "level": 10,
  "status": 0,
  "appearance": 44,
  "help": 0,
  "startTime": 0.0,
  "endTime": 0.0,
  "name": "Workshop"
}
```

### Troop Object
```json
{
  "wo": 0,      // Workers
  "w": 10000,   // Warriors
  "s": 5000,    // Scouts
  "p": 8000,    // Pikemen
  "sw": 12000,  // Swordsmen
  "a": 50000,   // Archers
  "c": 3000,    // Cavalry
  "cata": 1000, // Cataphracts
  "t": 2000,    // Transporters
  "b": 500,     // Ballistas
  "r": 200,     // Rams
  "cp": 100     // Catapults
}
```

### Hero Object
```json
{
  "heroId": 12345,
  "name": "Hero Name",
  "level": 100,
  "experience": 5000000,
  "status": "idle",
  "attributes": {
    "politics": 85,
    "attack": 120,
    "intelligence": 90
  },
  "skills": [...],
  "equipment": {...}
}
```

### Army/March Object
```json
{
  "armyId": 12345,
  "heroId": 54321,
  "missionType": 1,
  "status": "marching",
  "startFieldId": 123456,
  "targetFieldId": 654321,
  "startTime": 1704931200,
  "arriveTime": 1704934800,
  "returnTime": 1704938400,
  "troops": {...},
  "resources": {...}
}
```

---

## 🗺️ Building Types & Position IDs

### Building Type IDs
| typeId | Building       | Max Level | Inside/Outside |
| ------ | -------------- | --------- | -------------- |
| 1      | Town Hall      | 10        | Inside         |
| 2      | Cottage        | 10        | Inside         |
| 3      | Warehouse      | 10        | Inside         |
| 4      | Barracks       | 10        | Inside         |
| 5      | Academy        | 10        | Inside         |
| 6      | Forge          | 10        | Inside         |
| 7      | Workshop       | 10        | Inside         |
| 8      | Stable         | 10        | Inside         |
| 9      | Relief Station | 10        | Inside         |
| 10     | Embassy        | 10        | Inside         |
| 11     | Marketplace    | 10        | Inside         |
| 12     | Inn            | 10        | Inside         |
| 13     | Feasting Hall  | 10        | Inside         |
| 14     | Rally Spot     | 10        | Inside         |
| 15     | Beacon Tower   | 10        | Inside         |
| 16     | Walls          | 10        | Outside        |
| 20     | Farm           | 10        | Outside        |
| 21     | Sawmill        | 10        | Outside        |
| 22     | Quarry         | 10        | Outside        |
| 23     | Ironmine       | 10        | Outside        |

### Position ID Ranges
```
Inside City:  1001-1040 (40 slots)
Outside City: 2001-2040 (40 slots)

Special positions:
- 1001: Town Hall (always)
- 1002-1020: General inside slots
- 2001-2040: Resource field slots
```

---

## ⚔️ Troop Details

### Troop Codes & Stats
| Code | Name        | Type     | Attack | Defense | Speed | Load | Food/hr |
| ---- | ----------- | -------- | ------ | ------- | ----- | ---- | ------- |
| wo   | Worker      | Support  | 1      | 1       | 50    | 200  | 1       |
| w    | Warrior     | Infantry | 10     | 10      | 150   | 20   | 2       |
| s    | Scout       | Cavalry  | 5      | 5       | 3000  | 5    | 3       |
| p    | Pikeman     | Infantry | 30     | 30      | 150   | 25   | 4       |
| sw   | Swordsman   | Infantry | 50     | 50      | 175   | 30   | 5       |
| a    | Archer      | Ranged   | 100    | 25      | 200   | 35   | 6       |
| c    | Cavalry     | Cavalry  | 150    | 75      | 750   | 50   | 8       |
| cata | Cataphract  | Cavalry  | 250    | 175     | 500   | 75   | 10      |
| t    | Transporter | Support  | 20     | 50      | 100   | 5000 | 5       |
| b    | Ballista    | Siege    | 400    | 25      | 50    | 100  | 10      |
| r    | Ram         | Siege    | 100    | 250     | 75    | 150  | 15      |
| cp   | Catapult    | Siege    | 600    | 25      | 25    | 200  | 20      |

### Troop Training Requirements
```actionscript
// Training cost formula (base)
gold = baseGold * amount
food = baseFood * amount
lumber = baseLumber * amount
iron = baseIron * amount
time = baseTime * amount / (1 + techBonus)
```

---

## 🎯 Mission Types

| Code | Mission     | Description         |
| ---- | ----------- | ------------------- |
| 1    | Attack      | Attack target       |
| 2    | Reinforce   | Send reinforcements |
| 3    | Scout       | Scout target        |
| 4    | Transport   | Transport resources |
| 5    | Occupy      | Occupy NPC/valley   |
| 6    | Colonize    | Build new city      |
| 7    | Rally Point | Move to rally point |

---

## 🔐 Security & Authentication

### Login Flow
```
1. Client → Server: username + MD5(password)
2. Server → Client: session_token (valid 1 hour)
3. Client → Server: All requests include session_token
4. Server validates token, processes request
5. Token refresh on activity
```

### Token Structure (decoded)
```json
{
  "playerId": 12345678,
  "serverId": "cc2",
  "timestamp": 1704931200,
  "signature": "md5(playerId + serverId + timestamp + salt)"
}
```

### Known Salt Values
```python
# From SWF decompilation
LOGIN_SALT = "evony"
API_SALT = "gameapi2010"
SESSION_SALT = "flash_session_2010"
```

### Encryption Patterns
```python
encryption_patterns = {
    'aes': ['AES.encrypt', 'AES.decrypt', 'Rijndael', 'CryptoJS.AES'],
    'rc4': ['RC4', 'ArcFour', 'StreamCipher'],
    'xor': ['XOR', 'ByteXOR'],
    'base64': ['Base64.encode', 'Base64.decode'],
    'md5': ['MD5.hash', 'MD5.hexdigest'],
    'sha1': ['SHA1', 'SHA.hash']
}
```

---

## 📊 Error Codes Complete List

| Code | Meaning                | Resolution                  |
| ---- | ---------------------- | --------------------------- |
| 0    | Success                | -                           |
| -1   | Unknown error          | Check logs                  |
| -2   | Invalid session        | Re-login required           |
| -3   | Server busy            | Retry with backoff          |
| -4   | Rate limited           | Wait 60s before retry       |
| -5   | Insufficient resources | Check resource availability |
| -6   | Invalid target         | Verify coordinates/ID       |
| -7   | Army busy              | Wait for army to return     |
| -8   | Hero unavailable       | Assign different hero       |
| -9   | Level requirement      | Upgrade building/tech first |
| -10  | Item not found         | Verify item ID              |
| -11  | Action not allowed     | Check game rules            |
| -12  | Target protected       | Wait for protection to end  |
| -13  | Alliance required      | Join alliance first         |
| -14  | Insufficient prestige  | Earn more prestige          |
| -15  | Cooldown active        | Wait for cooldown           |
| -99  | Maintenance mode       | Server under maintenance    |

---

## 🌐 Server Endpoints

| Server | Domain           | Port | Purpose        |
| ------ | ---------------- | ---- | -------------- |
| cc1    | cc1.evony.com    | 443  | Game server 1  |
| cc2    | cc2.evony.com    | 443  | Game server 2  |
| cc3    | cc3.evony.com    | 443  | Game server 3  |
| cc4    | cc4.evony.com    | 443  | Game server 4  |
| cc5    | cc5.evony.com    | 443  | Game server 5  |
| login  | login.evony.com  | 443  | Authentication |
| api    | api.evony.com    | 443  | REST API       |
| static | static.evony.com | 443  | Static assets  |

---

## 📚 Related Documentation

- [FEATURE-ROADMAP.md](./FEATURE-ROADMAP.md) - Implementation roadmap
- [RAG-RTE-INTEGRATION.md](./RAG-RTE-INTEGRATION.md) - RAG/RTE integration
- [FIDDLER-SCRIPTS.md](./FIDDLER-SCRIPTS.md) - Fiddler automation
- [../docs/EVONY_PROTOCOL_REFERENCE.md](../../docs/EVONY_PROTOCOL_REFERENCE.md) - Base protocol ref
