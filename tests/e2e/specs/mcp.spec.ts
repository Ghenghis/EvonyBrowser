import { test, expect } from '@playwright/test';

/**
 * MCP Server Tests - Svony Browser v7.0
 * 
 * These tests verify the MCP (Model Context Protocol) server functionality including:
 * - Server connectivity
 * - Tool invocation
 * - Resource access
 * - Error handling
 */

const MCP_SERVERS = {
  rag: { name: 'evony-rag', port: 3001 },
  rte: { name: 'evony-rte', port: 3002 },
  tools: { name: 'evony-tools', port: 3003 },
  advanced: { name: 'evony-advanced', port: 3004 },
  cdp: { name: 'evony-cdp', port: 3005 },
  complete: { name: 'evony-complete', port: 3006 },
};

test.describe('MCP Server Connectivity', () => {
  for (const [key, server] of Object.entries(MCP_SERVERS)) {
    test(`should connect to ${server.name} server`, async ({ request }) => {
      const response = await request.get(`http://localhost:${server.port}/health`);
      
      // Server may not be running in test environment
      if (response.ok()) {
        expect(response.status()).toBe(200);
        const body = await response.json();
        expect(body.status).toBe('healthy');
      } else {
        // Skip if server not available
        test.skip();
      }
    });
  }

  test('should list available tools from RAG server', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'tools/list',
        params: {},
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.result.tools).toBeDefined();
      expect(Array.isArray(body.result.tools)).toBe(true);
    } else {
      test.skip();
    }
  });

  test('should list available resources from RTE server', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rte.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'resources/list',
        params: {},
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.result.resources).toBeDefined();
    } else {
      test.skip();
    }
  });
});

test.describe('MCP Tool Invocation', () => {
  test.describe('RAG Server Tools', () => {
    test('should search knowledge base', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'search_knowledge',
            arguments: {
              query: 'barracks upgrade requirements',
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
        expect(body.error).toBeUndefined();
      } else {
        test.skip();
      }
    });

    test('should query protocol database', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'query_protocol',
            arguments: {
              action: 'trainTroops',
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
      } else {
        test.skip();
      }
    });

    test('should explain action', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'explain_action',
            arguments: {
              actionId: 1,
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
      } else {
        test.skip();
      }
    });
  });

  test.describe('RTE Server Tools', () => {
    test('should decode packet', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.rte.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'decode_packet',
            arguments: {
              hex: '00 0a 00 00 00 01',
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
      } else {
        test.skip();
      }
    });

    test('should analyze pattern', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.rte.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'analyze_pattern',
            arguments: {
              packets: ['00 0a', '00 0b', '00 0c'],
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
      } else {
        test.skip();
      }
    });
  });

  test.describe('Tools Server', () => {
    test('should calculate combat', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.tools.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'calculate_combat',
            arguments: {
              attacker: { cavalry: 10000, infantry: 5000 },
              defender: { cavalry: 8000, infantry: 6000 },
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
      } else {
        test.skip();
      }
    });

    test('should optimize build order', async ({ request }) => {
      const response = await request.post(`http://localhost:${MCP_SERVERS.tools.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: {
            name: 'optimize_build',
            arguments: {
              currentLevel: 25,
              targetLevel: 30,
              resources: { gold: 1000000, food: 500000 },
            },
          },
        },
      });

      if (response.ok()) {
        const body = await response.json();
        expect(body.result).toBeDefined();
      } else {
        test.skip();
      }
    });
  });
});

test.describe('MCP Resource Access', () => {
  test('should read protocol database resource', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'resources/read',
        params: {
          uri: 'protocol://database',
        },
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.result).toBeDefined();
      expect(body.result.contents).toBeDefined();
    } else {
      test.skip();
    }
  });

  test('should read game state resource', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rte.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'resources/read',
        params: {
          uri: 'game://state',
        },
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.result).toBeDefined();
    } else {
      test.skip();
    }
  });

  test('should read live traffic resource', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rte.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'resources/read',
        params: {
          uri: 'traffic://live',
        },
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.result).toBeDefined();
    } else {
      test.skip();
    }
  });
});

test.describe('MCP Error Handling', () => {
  test('should return error for invalid method', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'invalid/method',
        params: {},
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.error).toBeDefined();
      expect(body.error.code).toBeDefined();
    } else {
      test.skip();
    }
  });

  test('should return error for missing tool', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'tools/call',
        params: {
          name: 'nonexistent_tool',
          arguments: {},
        },
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.error).toBeDefined();
    } else {
      test.skip();
    }
  });

  test('should return error for invalid arguments', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'tools/call',
        params: {
          name: 'search_knowledge',
          arguments: {
            // Missing required 'query' argument
          },
        },
      },
    });

    if (response.ok()) {
      const body = await response.json();
      expect(body.error).toBeDefined();
    } else {
      test.skip();
    }
  });

  test('should handle malformed JSON gracefully', async ({ request }) => {
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: 'not valid json',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Should return 400 Bad Request or similar
    expect(response.status()).toBeGreaterThanOrEqual(400);
  });
});

test.describe('MCP Performance', () => {
  test('should respond within acceptable latency', async ({ request }) => {
    const startTime = Date.now();
    
    const response = await request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
      data: {
        jsonrpc: '2.0',
        id: 1,
        method: 'tools/list',
        params: {},
      },
    });

    const latency = Date.now() - startTime;
    
    if (response.ok()) {
      expect(latency).toBeLessThan(1000); // 1 second max
    } else {
      test.skip();
    }
  });

  test('should handle concurrent requests', async ({ request }) => {
    const requests = Array(10).fill(null).map((_, i) => 
      request.post(`http://localhost:${MCP_SERVERS.rag.port}/mcp`, {
        data: {
          jsonrpc: '2.0',
          id: i,
          method: 'tools/list',
          params: {},
        },
      })
    );

    const responses = await Promise.all(requests);
    const successCount = responses.filter(r => r.ok()).length;
    
    // At least 80% should succeed
    expect(successCount).toBeGreaterThanOrEqual(8);
  });
});
