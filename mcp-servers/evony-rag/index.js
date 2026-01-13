#!/usr/bin/env node
/**
 * Evony RAG MCP Server
 * Provides knowledge retrieval and semantic search capabilities for Evony game data
 */

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { ChromaClient } from 'chromadb';
import { z } from 'zod';
import fs from 'fs/promises';
import path from 'path';

// Configuration
const CONFIG = {
  chromaDbPath: process.env.CHROMA_DB_PATH || './data/chroma-db',
  knowledgePath: process.env.KNOWLEDGE_PATH || './knowledge-base',
  collectionName: 'evony_knowledge',
  embeddingModel: process.env.EMBEDDING_MODEL || 'all-MiniLM-L6-v2'
};

// Initialize MCP Server
const server = new McpServer({
  name: 'evony-rag',
  version: '1.0.0',
  description: 'Evony knowledge base with semantic search and Q&A capabilities'
});

// ChromaDB client (lazy initialization)
let chromaClient = null;
let collection = null;

/**
 * Initialize ChromaDB connection
 */
async function initChroma() {
  if (collection) return collection;
  
  try {
    chromaClient = new ChromaClient({
      path: CONFIG.chromaDbPath
    });
    
    collection = await chromaClient.getOrCreateCollection({
      name: CONFIG.collectionName,
      metadata: { 
        description: 'Evony game knowledge base',
        created: new Date().toISOString()
      }
    });
    
    console.error(`[evony-rag] ChromaDB initialized with collection: ${CONFIG.collectionName}`);
    return collection;
  } catch (error) {
    console.error(`[evony-rag] ChromaDB initialization failed: ${error.message}`);
    // Return mock collection for testing without ChromaDB
    return createMockCollection();
  }
}

/**
 * Create mock collection for environments without ChromaDB
 */
function createMockCollection() {
  const documents = [];
  
  return {
    add: async ({ ids, documents: docs, metadatas }) => {
      for (let i = 0; i < ids.length; i++) {
        documents.push({
          id: ids[i],
          document: docs[i],
          metadata: metadatas?.[i] || {}
        });
      }
      return { success: true };
    },
    query: async ({ queryTexts, nResults, where }) => {
      // Simple text matching for mock
      const results = documents
        .filter(doc => {
          const matchesQuery = queryTexts.some(q => 
            doc.document.toLowerCase().includes(q.toLowerCase())
          );
          const matchesWhere = !where || Object.entries(where).every(([k, v]) => 
            doc.metadata[k] === v
          );
          return matchesQuery && matchesWhere;
        })
        .slice(0, nResults || 10);
      
      return {
        ids: [results.map(r => r.id)],
        documents: [results.map(r => r.document)],
        metadatas: [results.map(r => r.metadata)],
        distances: [results.map(() => 0.5)]
      };
    },
    count: async () => documents.length,
    get: async ({ ids }) => {
      const found = documents.filter(d => ids.includes(d.id));
      return {
        ids: found.map(f => f.id),
        documents: found.map(f => f.document),
        metadatas: found.map(f => f.metadata)
      };
    }
  };
}

/**
 * Load knowledge base documents
 */
async function loadKnowledgeBase() {
  const coll = await initChroma();
  const categories = ['heroes', 'buildings', 'combat', 'strategies'];
  let totalIndexed = 0;
  
  for (const category of categories) {
    const categoryPath = path.join(CONFIG.knowledgePath, category);
    
    try {
      const files = await fs.readdir(categoryPath);
      
      for (const file of files) {
        if (!file.endsWith('.md') && !file.endsWith('.json') && !file.endsWith('.txt')) continue;
        
        const filePath = path.join(categoryPath, file);
        const content = await fs.readFile(filePath, 'utf-8');
        const docId = `${category}_${file.replace(/\.[^.]+$/, '')}`;
        
        await coll.add({
          ids: [docId],
          documents: [content],
          metadatas: [{
            category,
            filename: file,
            source: filePath,
            indexed_at: new Date().toISOString()
          }]
        });
        
        totalIndexed++;
      }
    } catch (error) {
      // Directory may not exist yet
      console.error(`[evony-rag] Could not load ${category}: ${error.message}`);
    }
  }
  
  console.error(`[evony-rag] Indexed ${totalIndexed} documents`);
  return totalIndexed;
}

// ============================================================================
// MCP Tools
// ============================================================================

/**
 * Tool: evony_search - Semantic search across knowledge base
 */
server.tool(
  'evony_search',
  'Search the Evony knowledge base with semantic search',
  {
    query: z.string().describe('Search query for Evony knowledge'),
    limit: z.number().default(10).describe('Maximum results to return'),
    category: z.enum(['heroes', 'buildings', 'combat', 'strategies', 'all']).default('all').describe('Category filter')
  },
  async ({ query, limit, category }) => {
    try {
      const coll = await initChroma();
      
      const whereClause = category !== 'all' ? { category } : undefined;
      
      const results = await coll.query({
        queryTexts: [query],
        nResults: limit,
        where: whereClause
      });
      
      const formattedResults = [];
      if (results.ids[0]) {
        for (let i = 0; i < results.ids[0].length; i++) {
          formattedResults.push({
            id: results.ids[0][i],
            content: results.documents[0][i]?.substring(0, 500) + '...',
            metadata: results.metadatas[0][i],
            relevance: results.distances ? (1 - results.distances[0][i]).toFixed(3) : 'N/A'
          });
        }
      }
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            query,
            total_results: formattedResults.length,
            results: formattedResults
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: evony_query - Natural language Q&A
 */
server.tool(
  'evony_query',
  'Ask a question about Evony and get an AI-powered answer',
  {
    question: z.string().describe('Question about Evony gameplay, mechanics, or strategy'),
    include_sources: z.boolean().default(true).describe('Include source references')
  },
  async ({ question, include_sources }) => {
    try {
      const coll = await initChroma();
      
      // Get relevant context from knowledge base
      const context = await coll.query({
        queryTexts: [question],
        nResults: 5
      });
      
      // Build context string
      const contextDocs = context.documents[0] || [];
      const contextText = contextDocs.join('\n\n---\n\n');
      
      // Build response (in production, this would call an LLM)
      const response = {
        question,
        answer: buildAnswer(question, contextDocs),
        context_used: contextDocs.length,
        sources: include_sources ? context.metadatas[0] : undefined
      };
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify(response, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: evony_index - Add new document to knowledge base
 */
server.tool(
  'evony_index',
  'Add a new document to the Evony knowledge base',
  {
    content: z.string().describe('Document content to index'),
    title: z.string().describe('Document title'),
    category: z.enum(['heroes', 'buildings', 'combat', 'strategies', 'protocol', 'traffic']).describe('Category tag'),
    source: z.string().optional().describe('Source reference')
  },
  async ({ content, title, category, source }) => {
    try {
      const coll = await initChroma();
      const id = `doc_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
      
      await coll.add({
        ids: [id],
        documents: [content],
        metadatas: [{
          title,
          category,
          source: source || 'user_input',
          indexed_at: new Date().toISOString()
        }]
      });
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            success: true,
            id,
            message: `Document "${title}" indexed successfully`
          })
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

/**
 * Tool: evony_list_sources - List indexed sources
 */
server.tool(
  'evony_list_sources',
  'List all indexed sources in the knowledge base',
  {
    category: z.enum(['heroes', 'buildings', 'combat', 'strategies', 'all']).default('all').describe('Filter by category')
  },
  async ({ category }) => {
    try {
      const coll = await initChroma();
      const count = await coll.count();
      
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            total_documents: count,
            category_filter: category,
            categories: ['heroes', 'buildings', 'combat', 'strategies'],
            status: 'operational'
          }, null, 2)
        }]
      };
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({ error: error.message })
        }],
        isError: true
      };
    }
  }
);

// ============================================================================
// MCP Resources
// ============================================================================

server.resource(
  'evony://knowledge/stats',
  'Knowledge base statistics',
  async () => {
    const coll = await initChroma();
    const count = await coll.count();
    
    return {
      contents: [{
        uri: 'evony://knowledge/stats',
        mimeType: 'application/json',
        text: JSON.stringify({
          total_documents: count,
          categories: ['heroes', 'buildings', 'combat', 'strategies'],
          embedding_model: CONFIG.embeddingModel,
          last_updated: new Date().toISOString()
        })
      }]
    };
  }
);

server.resource(
  'evony://knowledge/heroes',
  'Hero database and statistics',
  async () => {
    return {
      contents: [{
        uri: 'evony://knowledge/heroes',
        mimeType: 'application/json',
        text: JSON.stringify({
          description: 'Evony hero database',
          total_heroes: 692,
          attributes: ['politics', 'attack', 'intelligence'],
          grades: ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary']
        })
      }]
    };
  }
);

server.resource(
  'evony://knowledge/buildings',
  'Building database and requirements',
  async () => {
    return {
      contents: [{
        uri: 'evony://knowledge/buildings',
        mimeType: 'application/json',
        text: JSON.stringify({
          inside_city: [
            { id: 1, name: 'Town Hall', max_level: 10 },
            { id: 2, name: 'Cottage', max_level: 10 },
            { id: 3, name: 'Warehouse', max_level: 10 },
            { id: 4, name: 'Barracks', max_level: 10 },
            { id: 5, name: 'Academy', max_level: 10 },
            { id: 6, name: 'Forge', max_level: 10 },
            { id: 7, name: 'Workshop', max_level: 10 },
            { id: 8, name: 'Stable', max_level: 10 },
            { id: 9, name: 'Relief Station', max_level: 10 },
            { id: 10, name: 'Embassy', max_level: 10 },
            { id: 11, name: 'Marketplace', max_level: 10 },
            { id: 12, name: 'Inn', max_level: 10 },
            { id: 13, name: 'Feasting Hall', max_level: 10 },
            { id: 14, name: 'Rally Spot', max_level: 10 },
            { id: 15, name: 'Beacon Tower', max_level: 10 },
            { id: 16, name: 'Walls', max_level: 10 }
          ],
          outside_city: [
            { id: 20, name: 'Farm', max_level: 10, resource: 'Food' },
            { id: 21, name: 'Sawmill', max_level: 10, resource: 'Lumber' },
            { id: 22, name: 'Quarry', max_level: 10, resource: 'Stone' },
            { id: 23, name: 'Ironmine', max_level: 10, resource: 'Iron' }
          ]
        })
      }]
    };
  }
);

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Build answer from context (simplified - in production use LLM)
 */
function buildAnswer(question, contextDocs) {
  if (!contextDocs || contextDocs.length === 0) {
    return 'I could not find relevant information in the knowledge base to answer your question.';
  }
  
  // Extract key information from context
  const combinedContext = contextDocs.join(' ').substring(0, 2000);
  
  return `Based on the Evony knowledge base:\n\n${combinedContext}\n\n(Note: This is a context-based response. For more detailed analysis, consider using an LLM integration.)`;
}

// ============================================================================
// Server Startup
// ============================================================================

async function main() {
  console.error('[evony-rag] Starting Evony RAG MCP Server...');
  
  // Initialize knowledge base
  await loadKnowledgeBase();
  
  // Start MCP server
  const transport = new StdioServerTransport();
  await server.connect(transport);
  
  console.error('[evony-rag] Server running on stdio transport');
}

main().catch(error => {
  console.error('[evony-rag] Fatal error:', error);
  process.exit(1);
});
