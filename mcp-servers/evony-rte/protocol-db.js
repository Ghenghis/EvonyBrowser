/**
 * Protocol Database for Evony
 * Contains all known protocol actions, their parameters, and documentation
 */

// Complete protocol action definitions
const PROTOCOL_ACTIONS = {
  // Castle Commands
  'castle.getCastleInfo': {
    category: 'castle',
    description: 'Get detailed castle information',
    request: { castleId: 'int' },
    response: { castle: 'CastleObject' }
  },
  'castle.upgradeBuilding': {
    category: 'castle',
    description: 'Upgrade a building in the castle',
    request: { castleId: 'int', positionId: 'int' },
    response: { success: 'boolean', endTime: 'timestamp' }
  },
  'castle.demolishBuilding': {
    category: 'castle',
    description: 'Demolish a building',
    request: { castleId: 'int', positionId: 'int' },
    response: { success: 'boolean' }
  },
  'castle.cancelBuilding': {
    category: 'castle',
    description: 'Cancel building construction/upgrade',
    request: { castleId: 'int', positionId: 'int' },
    response: { success: 'boolean', refund: 'ResourceObject' }
  },
  'castle.speedUpBuilding': {
    category: 'castle',
    description: 'Use speed up item on building',
    request: { castleId: 'int', positionId: 'int', itemId: 'int' },
    response: { success: 'boolean', newEndTime: 'timestamp' }
  },
  'castle.collectResource': {
    category: 'castle',
    description: 'Collect resources from a field',
    request: { castleId: 'int', positionId: 'int' },
    response: { collected: 'ResourceObject' }
  },
  'castle.collectAllResources': {
    category: 'castle',
    description: 'Collect all available resources',
    request: { castleId: 'int' },
    response: { total: 'ResourceObject' }
  },
  
  // Hero Commands
  'hero.getHerosListFromTavern': {
    category: 'hero',
    description: 'Get list of heroes available in tavern',
    request: { castleId: 'int' },
    response: { heroes: 'Array<HeroObject>' }
  },
  'hero.hireHero': {
    category: 'hero',
    description: 'Hire a hero from the tavern',
    request: { castleId: 'int', heroName: 'string' },
    response: { success: 'boolean', hero: 'HeroObject' }
  },
  'hero.refreshHeroList': {
    category: 'hero',
    description: 'Refresh tavern hero list',
    request: { castleId: 'int' },
    response: { heroes: 'Array<HeroObject>', cost: 'int' }
  },
  'hero.fireHero': {
    category: 'hero',
    description: 'Dismiss a hero',
    request: { castleId: 'int', heroId: 'int' },
    response: { success: 'boolean' }
  },
  'hero.levelUpHero': {
    category: 'hero',
    description: 'Level up a hero using experience',
    request: { heroId: 'int' },
    response: { success: 'boolean', newLevel: 'int' }
  },
  'hero.addPointToAttribute': {
    category: 'hero',
    description: 'Add attribute points to hero',
    request: { heroId: 'int', attribute: 'string', points: 'int' },
    response: { success: 'boolean', newValue: 'int' }
  },
  'hero.assignHeroToArmy': {
    category: 'hero',
    description: 'Assign hero to lead an army',
    request: { heroId: 'int', armyId: 'int' },
    response: { success: 'boolean' }
  },
  'hero.unassignHero': {
    category: 'hero',
    description: 'Remove hero from army assignment',
    request: { heroId: 'int' },
    response: { success: 'boolean' }
  },
  'hero.setMayor': {
    category: 'hero',
    description: 'Set hero as city mayor',
    request: { castleId: 'int', heroId: 'int' },
    response: { success: 'boolean' }
  },
  'hero.removeMayor': {
    category: 'hero',
    description: 'Remove mayor from city',
    request: { castleId: 'int' },
    response: { success: 'boolean' }
  },
  'hero.equipItem': {
    category: 'hero',
    description: 'Equip item to hero',
    request: { heroId: 'int', itemId: 'int', slot: 'int' },
    response: { success: 'boolean' }
  },
  'hero.unequipItem': {
    category: 'hero',
    description: 'Unequip item from hero',
    request: { heroId: 'int', slot: 'int' },
    response: { success: 'boolean', item: 'ItemObject' }
  },
  'hero.getAllHeroLevel': {
    category: 'hero',
    description: 'Get all hero levels and stats',
    request: { heroId: 'int', includeItems: 'boolean' },
    response: { level: 'int', exp: 'long', skills: 'Array<Skill>' }
  },
  
  // Troop Commands
  'troop.trainTroop': {
    category: 'troop',
    description: 'Train troops in barracks/stable',
    request: { castleId: 'int', troopType: 'string', amount: 'int' },
    response: { success: 'boolean', endTime: 'timestamp' }
  },
  'troop.cancelTraining': {
    category: 'troop',
    description: 'Cancel troop training',
    request: { castleId: 'int', troopType: 'string' },
    response: { success: 'boolean', refund: 'ResourceObject' }
  },
  'troop.speedUpTraining': {
    category: 'troop',
    description: 'Speed up troop training',
    request: { castleId: 'int', itemId: 'int' },
    response: { success: 'boolean', newEndTime: 'timestamp' }
  },
  'troop.dismissTroop': {
    category: 'troop',
    description: 'Dismiss troops',
    request: { castleId: 'int', troopType: 'string', amount: 'int' },
    response: { success: 'boolean' }
  },
  'troop.healTroops': {
    category: 'troop',
    description: 'Heal wounded troops',
    request: { castleId: 'int' },
    response: { healed: 'TroopObject', cost: 'ResourceObject' }
  },
  
  // Army Commands
  'army.createArmy': {
    category: 'army',
    description: 'Create a new army',
    request: { castleId: 'int', heroId: 'int', troops: 'TroopObject' },
    response: { armyId: 'int' }
  },
  'army.disbandArmy': {
    category: 'army',
    description: 'Disband an army',
    request: { armyId: 'int' },
    response: { success: 'boolean' }
  },
  'army.sendArmy': {
    category: 'army',
    description: 'Send army on a mission',
    request: { armyId: 'int', targetX: 'int', targetY: 'int', missionType: 'int' },
    response: { success: 'boolean', arriveTime: 'timestamp' }
  },
  'army.recallArmy': {
    category: 'army',
    description: 'Recall army from mission',
    request: { armyId: 'int' },
    response: { success: 'boolean', returnTime: 'timestamp' }
  },
  'army.speedUpMarch': {
    category: 'army',
    description: 'Speed up army march',
    request: { armyId: 'int', itemId: 'int' },
    response: { success: 'boolean', newArriveTime: 'timestamp' }
  },
  'army.setArmyFormation': {
    category: 'army',
    description: 'Set army formation',
    request: { armyId: 'int', formationType: 'int' },
    response: { success: 'boolean' }
  },
  
  // Alliance Commands
  'alliance.getAllianceInfo': {
    category: 'alliance',
    description: 'Get alliance information',
    request: { allianceId: 'int' },
    response: { alliance: 'AllianceObject' }
  },
  'alliance.getMemberList': {
    category: 'alliance',
    description: 'Get alliance member list',
    request: { allianceId: 'int' },
    response: { members: 'Array<MemberObject>' }
  },
  'alliance.joinAlliance': {
    category: 'alliance',
    description: 'Join an alliance',
    request: { allianceId: 'int' },
    response: { success: 'boolean' }
  },
  'alliance.leaveAlliance': {
    category: 'alliance',
    description: 'Leave current alliance',
    request: {},
    response: { success: 'boolean' }
  },
  'alliance.invitePlayer': {
    category: 'alliance',
    description: 'Invite player to alliance',
    request: { playerId: 'int' },
    response: { success: 'boolean' }
  },
  'alliance.kickMember': {
    category: 'alliance',
    description: 'Kick member from alliance',
    request: { memberId: 'int' },
    response: { success: 'boolean' }
  },
  
  // Research Commands
  'tech.startResearch': {
    category: 'tech',
    description: 'Start researching a technology',
    request: { castleId: 'int', techId: 'int' },
    response: { success: 'boolean', endTime: 'timestamp' }
  },
  'tech.cancelResearch': {
    category: 'tech',
    description: 'Cancel ongoing research',
    request: { castleId: 'int' },
    response: { success: 'boolean', refund: 'ResourceObject' }
  },
  'tech.speedUpResearch': {
    category: 'tech',
    description: 'Speed up research',
    request: { castleId: 'int', itemId: 'int' },
    response: { success: 'boolean', newEndTime: 'timestamp' }
  },
  'tech.getTechList': {
    category: 'tech',
    description: 'Get list of all technologies',
    request: { castleId: 'int' },
    response: { techs: 'Array<TechObject>' }
  },
  
  // Quest Commands
  'quest.getQuestList': {
    category: 'quest',
    description: 'Get list of available quests',
    request: { castleId: 'int' },
    response: { quests: 'Array<QuestObject>' }
  },
  'quest.completeQuest': {
    category: 'quest',
    description: 'Complete a quest',
    request: { questId: 'int' },
    response: { success: 'boolean', rewards: 'RewardObject' }
  },
  'quest.claimQuestReward': {
    category: 'quest',
    description: 'Claim quest reward',
    request: { questId: 'int' },
    response: { rewards: 'RewardObject' }
  },
  
  // Mail Commands
  'mail.sendMail': {
    category: 'mail',
    description: 'Send mail to player',
    request: { playerId: 'int', subject: 'string', content: 'string' },
    response: { success: 'boolean', mailId: 'int' }
  },
  'mail.readMail': {
    category: 'mail',
    description: 'Read a mail message',
    request: { mailId: 'int' },
    response: { mail: 'MailObject' }
  },
  'mail.deleteMail': {
    category: 'mail',
    description: 'Delete a mail message',
    request: { mailId: 'int' },
    response: { success: 'boolean' }
  },
  'mail.getMailList': {
    category: 'mail',
    description: 'Get mail list',
    request: { mailType: 'int', page: 'int' },
    response: { mails: 'Array<MailObject>', total: 'int' }
  },
  
  // Shop Commands
  'shop.useTrickItem': {
    category: 'shop',
    description: 'Use a trick/special item',
    request: { fieldId: 'int', armyId: 'int', itemId: 'int' },
    response: { success: 'boolean' }
  },
  'shop.useSpeedItem': {
    category: 'shop',
    description: 'Use a speed up item',
    request: { targetId: 'int', itemId: 'int', targetType: 'string' },
    response: { success: 'boolean', newEndTime: 'timestamp' }
  },
  'shop.useResourceItem': {
    category: 'shop',
    description: 'Use a resource item',
    request: { castleId: 'int', itemId: 'int' },
    response: { success: 'boolean', gained: 'ResourceObject' }
  },
  'shop.buyItem': {
    category: 'shop',
    description: 'Buy item from shop',
    request: { itemId: 'int', amount: 'int' },
    response: { success: 'boolean', cost: 'int' }
  },
  
  // System Commands
  'system.heartbeat': {
    category: 'system',
    description: 'Keep session alive',
    request: {},
    response: { timestamp: 'long' }
  },
  'system.getServerTime': {
    category: 'system',
    description: 'Get server time',
    request: {},
    response: { serverTime: 'timestamp' }
  },
  
  // Player Commands
  'player.getPlayerInfo': {
    category: 'player',
    description: 'Get player information',
    request: { playerId: 'int' },
    response: { player: 'PlayerObject' }
  },
  'player.getSelfInfo': {
    category: 'player',
    description: 'Get own player information',
    request: {},
    response: { player: 'PlayerObject' }
  },
  
  // Capital/Policy Commands
  'capital.getLeftPoliciesTimes': {
    category: 'capital',
    description: 'Get remaining policy times',
    request: { castleId: 'int' },
    response: { levy: 'int', comfort: 'int' }
  },
  'capital.levyArmy': {
    category: 'capital',
    description: 'Levy troops from population',
    request: { castleId: 'int', fieldId: 'int' },
    response: { troops: 'TroopObject' }
  },
  'capital.levyTaxes': {
    category: 'capital',
    description: 'Levy taxes from population',
    request: { castleId: 'int', fieldId: 'int' },
    response: { gold: 'int' }
  },
  'capital.levyFood': {
    category: 'capital',
    description: 'Levy food from population',
    request: { castleId: 'int', fieldId: 'int' },
    response: { food: 'int' }
  },
  'capital.comfort': {
    category: 'capital',
    description: 'Comfort population',
    request: { castleId: 'int', comfortType: 'int' },
    response: { success: 'boolean', loyaltyGain: 'int' }
  }
};

// Troop type codes
const TROOP_CODES = {
  'wo': { id: 1, name: 'Worker', category: 'Support' },
  'w': { id: 2, name: 'Warrior', category: 'Infantry' },
  's': { id: 3, name: 'Scout', category: 'Cavalry' },
  'p': { id: 4, name: 'Pikeman', category: 'Infantry' },
  'sw': { id: 5, name: 'Swordsman', category: 'Infantry' },
  'a': { id: 6, name: 'Archer', category: 'Ranged' },
  'c': { id: 7, name: 'Cavalry', category: 'Cavalry' },
  'cata': { id: 8, name: 'Cataphract', category: 'Cavalry' },
  't': { id: 9, name: 'Transporter', category: 'Support' },
  'b': { id: 10, name: 'Ballista', category: 'Siege' },
  'r': { id: 11, name: 'Ram', category: 'Siege' },
  'cp': { id: 12, name: 'Catapult', category: 'Siege' }
};

// Building type IDs
const BUILDING_TYPES = {
  1: 'Town Hall',
  2: 'Cottage',
  3: 'Warehouse',
  4: 'Barracks',
  5: 'Academy',
  6: 'Forge',
  7: 'Workshop',
  8: 'Stable',
  9: 'Relief Station',
  10: 'Embassy',
  11: 'Marketplace',
  12: 'Inn',
  13: 'Feasting Hall',
  14: 'Rally Spot',
  15: 'Beacon Tower',
  16: 'Walls',
  20: 'Farm',
  21: 'Sawmill',
  22: 'Quarry',
  23: 'Ironmine'
};

// Mission types
const MISSION_TYPES = {
  1: 'Attack',
  2: 'Reinforce',
  3: 'Scout',
  4: 'Transport',
  5: 'Occupy',
  6: 'Colonize',
  7: 'Rally'
};

/**
 * Protocol Database Class
 */
export class ProtocolDatabase {
  constructor(dbPath = null) {
    this.actions = PROTOCOL_ACTIONS;
    this.troopCodes = TROOP_CODES;
    this.buildingTypes = BUILDING_TYPES;
    this.missionTypes = MISSION_TYPES;
  }

  /**
   * Get action definition
   */
  getAction(actionName) {
    return this.actions[actionName] || null;
  }

  /**
   * Get all actions
   */
  getAllActions() {
    return Object.entries(this.actions).map(([name, def]) => ({
      name,
      ...def
    }));
  }

  /**
   * Get actions by category
   */
  getActionsByCategory(category) {
    return Object.entries(this.actions)
      .filter(([_, def]) => def.category === category)
      .map(([name, def]) => ({ name, ...def }));
  }

  /**
   * Search actions by name pattern
   */
  searchActions(pattern) {
    const lowerPattern = pattern.toLowerCase();
    return Object.entries(this.actions)
      .filter(([name, _]) => name.toLowerCase().includes(lowerPattern))
      .map(([name, def]) => ({ name, ...def }));
  }

  /**
   * Get all categories
   */
  getCategories() {
    const categories = new Set();
    Object.values(this.actions).forEach(def => categories.add(def.category));
    return Array.from(categories);
  }

  /**
   * Get troop info by code
   */
  getTroopInfo(code) {
    return this.troopCodes[code] || null;
  }

  /**
   * Get building name by ID
   */
  getBuildingName(typeId) {
    return this.buildingTypes[typeId] || `Unknown (${typeId})`;
  }

  /**
   * Get mission name by type
   */
  getMissionName(missionType) {
    return this.missionTypes[missionType] || `Unknown (${missionType})`;
  }

  /**
   * Parse troop string notation
   */
  parseTroopString(troopStr) {
    const result = {};
    const parts = troopStr.split(',');
    
    for (const part of parts) {
      const [code, count] = part.split(':');
      if (code && count) {
        const troopInfo = this.getTroopInfo(code.trim());
        if (troopInfo) {
          result[code.trim()] = {
            ...troopInfo,
            count: parseInt(count, 10)
          };
        }
      }
    }
    
    return result;
  }

  /**
   * Generate documentation for an action
   */
  generateActionDoc(actionName) {
    const action = this.getAction(actionName);
    if (!action) return null;
    
    let doc = `## Action: ${actionName}\n\n`;
    doc += `**Category:** ${action.category}\n`;
    doc += `**Description:** ${action.description}\n\n`;
    
    doc += `### Request Parameters\n`;
    doc += `| Field | Type | Description |\n`;
    doc += `|-------|------|-------------|\n`;
    for (const [field, type] of Object.entries(action.request)) {
      doc += `| ${field} | ${type} | - |\n`;
    }
    
    doc += `\n### Response\n`;
    doc += `| Field | Type | Description |\n`;
    doc += `|-------|------|-------------|\n`;
    for (const [field, type] of Object.entries(action.response)) {
      doc += `| ${field} | ${type} | - |\n`;
    }
    
    return doc;
  }
}

export default ProtocolDatabase;
