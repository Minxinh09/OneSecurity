import React, { useState, useEffect } from 'react';
import { 
  Play, 
  Settings, 
  Activity, 
  CheckCircle, 
  XCircle, 
  BarChart2, 
  Terminal, 
  Cpu, 
  ShieldAlert, 
  AlertTriangle,
  RefreshCw,
  Sliders,
  Maximize2
} from 'lucide-react';

interface RuleStatItem {
  ruleId: number;
  ruleName: string;
  value: number;
}

interface RuleMatchStat {
  ruleId: number;
  ruleName: string;
  matchCount: number;
}

interface RuleStatistics {
  totalEvaluations: number;
  totalMatches: number;
  averageEvaluationTimeMs: number;
  currentActiveRulesCount: number;
  lastEvaluationTime: string | null;
  ruleHitRate: number;
  topSlowRules: RuleStatItem[];
  topFailedRules: RuleStatItem[];
  topTriggeredRules: RuleMatchStat[];
}

export const RulesManagement: React.FC = () => {
  const [activeSubTab, setActiveSubTab] = useState<'statistics' | 'tester' | 'pipeline'>('statistics');
  const [loadingStats, setLoadingStats] = useState(false);
  const [stats, setStats] = useState<RuleStatistics | null>(null);

  // Tester state
  const [testRuleName, setTestRuleName] = useState('Brute-force SSH Login');
  const [testRuleExpr, setTestRuleExpr] = useState(
    JSON.stringify({
      severity: "Warning",
      category: "auth",
      source: "ssh",
      keyword: "failed",
      threshold: 5,
      timeWindowSeconds: 60
    }, null, 2)
  );
  
  const [testEventData, setTestEventData] = useState(
    JSON.stringify({
      eventId: "evt-12345",
      agentId: "agent-01",
      category: "auth",
      severity: "warning",
      source: "ssh",
      title: "Logon Failure",
      details: "Failed logon attempt for user admin from IP 192.168.1.100",
      rawData: "{\"user\":\"admin\",\"ip\":\"192.168.1.100\"}"
    }, null, 2)
  );

  const [testingRule, setTestingRule] = useState(false);
  const [testResult, setTestResult] = useState<any | null>(null);
  const [testError, setTestError] = useState<string | null>(null);

  const fetchStatistics = async () => {
    setLoadingStats(true);
    try {
      const token = localStorage.getItem('token');
      const res = await fetch('/api/v1/rules/statistics', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (res.ok) {
        const data = await res.json();
        setStats(data);
      }
    } catch (err) {
      console.error("Failed to load rule stats:", err);
    } finally {
      setLoadingStats(false);
    }
  };

  useEffect(() => {
    if (activeSubTab === 'statistics') {
      fetchStatistics();
    }
  }, [activeSubTab]);

  const handleTestRule = async () => {
    setTestingRule(true);
    setTestResult(null);
    setTestError(null);
    try {
      // Validate JSON formats
      let ruleObj, eventObj;
      try {
        ruleObj = JSON.parse(testRuleExpr);
      } catch (e) {
        throw new Error("Invalid Rule Condition Expression JSON");
      }
      try {
        eventObj = JSON.parse(testEventData);
      } catch (e) {
        throw new Error("Invalid Security Event JSON");
      }

      const token = localStorage.getItem('token');
      const res = await fetch('/api/v1/rules/test', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          rule: {
            name: testRuleName,
            eventType: "security",
            conditionExpression: JSON.stringify(ruleObj),
            alertSeverity: "Warning",
            isEnabled: true
          },
          event: eventObj
        })
      });

      if (res.ok) {
        const data = await res.json();
        setTestResult(data);
      } else {
        const errObj = await res.json();
        setTestError(errObj.message || "Failed to execute rule simulation test.");
      }
    } catch (err: any) {
      setTestError(err.message || "An unexpected error occurred.");
    } finally {
      setTestingRule(false);
    }
  };

  const renderStats = () => {
    if (loadingStats && !stats) {
      return (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '300px', gap: '8px' }}>
          <RefreshCw size={20} className="spin-animation" style={{ animation: 'spin 1.5s linear infinite' }} />
          <span>Fetching analytics...</span>
        </div>
      );
    }

    const s = stats || {
      totalEvaluations: 0,
      totalMatches: 0,
      averageEvaluationTimeMs: 0,
      currentActiveRulesCount: 0,
      lastEvaluationTime: 'N/A',
      ruleHitRate: 0,
      topSlowRules: [],
      topFailedRules: [],
      topTriggeredRules: []
    };

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
        {/* Row 1: KPI Cards */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px' }}>
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Total Evaluations</span>
            <span style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--primary)' }}>{s.totalEvaluations.toLocaleString()}</span>
          </div>
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Total Matches</span>
            <span style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--success)' }}>{s.totalMatches.toLocaleString()}</span>
          </div>
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Average Latency</span>
            <span style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--accent)' }}>{s.averageEvaluationTimeMs.toFixed(3)} ms</span>
          </div>
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Hit Rate</span>
            <span style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--warning)' }}>{s.ruleHitRate.toFixed(2)}%</span>
          </div>
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Active Policies</span>
            <span style={{ fontSize: '1.75rem', fontWeight: 700, color: '#f8fafc' }}>{s.currentActiveRulesCount}</span>
          </div>
        </div>

        {/* Row 2: Tables / Analysis lists */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '20px' }}>
          
          {/* Top Triggered Rules */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', borderBottom: '1px solid var(--panel-border)', paddingBottom: '8px' }}>
              <BarChart2 size={16} color="var(--success)" />
              <h3 style={{ fontSize: '1rem', fontWeight: 600 }}>Top Triggered Policies</h3>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {s.topTriggeredRules.length === 0 ? (
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>No statistics recorded yet.</span>
              ) : s.topTriggeredRules.map((r, idx) => (
                <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.875rem' }}>
                  <span style={{ fontWeight: 500 }}>{r.ruleName}</span>
                  <span className="badge" style={{ background: 'var(--success-glow)', color: 'var(--success)', padding: '2px 8px', borderRadius: '4px', fontSize: '0.75rem' }}>
                    {r.matchCount} hits
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Top Slowest Rules */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', borderBottom: '1px solid var(--panel-border)', paddingBottom: '8px' }}>
              <Cpu size={16} color="var(--danger)" />
              <h3 style={{ fontSize: '1rem', fontWeight: 600 }}>Top Slowest Rules</h3>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {s.topSlowRules.length === 0 ? (
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>No statistics recorded yet.</span>
              ) : s.topSlowRules.map((r, idx) => (
                <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.875rem' }}>
                  <span style={{ fontWeight: 500 }}>{r.ruleName}</span>
                  <span className="badge" style={{ background: 'var(--danger-glow)', color: 'var(--danger)', padding: '2px 8px', borderRadius: '4px', fontSize: '0.75rem' }}>
                    {r.value.toFixed(3)} ms
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Top Failed Rules */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', borderBottom: '1px solid var(--panel-border)', paddingBottom: '8px' }}>
              <AlertTriangle size={16} color="var(--warning)" />
              <h3 style={{ fontSize: '1rem', fontWeight: 600 }}>Top Failed Rules (Errors)</h3>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {s.topFailedRules.length === 0 ? (
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>No failed/errored rule evaluations recorded.</span>
              ) : s.topFailedRules.map((r, idx) => (
                <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.875rem' }}>
                  <span style={{ fontWeight: 500 }}>{r.ruleName}</span>
                  <span className="badge" style={{ background: 'var(--warning-glow)', color: 'var(--warning)', padding: '2px 8px', borderRadius: '4px', fontSize: '0.75rem' }}>
                    {r.value} fails
                  </span>
                </div>
              ))}
            </div>
          </div>

        </div>

        {/* Footer info */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '0.75rem', color: 'var(--text-muted)', borderTop: '1px solid var(--panel-border)', paddingTop: '12px' }}>
          <span>Detection pipeline metrics are cached and calculated globally in memory.</span>
          <span>Last Evaluation: {s.lastEvaluationTime || 'None'}</span>
        </div>
      </div>
    );
  };

  const renderTester = () => {
    return (
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '24px' }}>
        {/* Rules Config Panels */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Settings size={16} color="var(--primary)" />
              1. Rule Config (Sandbox)
            </h3>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Rule Policy Name</label>
              <input 
                type="text" 
                value={testRuleName}
                onChange={e => setTestRuleName(e.target.value)}
                style={{
                  background: '#090d16',
                  border: '1px solid var(--panel-border)',
                  borderRadius: '6px',
                  padding: '8px 10px',
                  color: '#fff',
                  fontSize: '0.85rem',
                  outline: 'none'
                }}
              />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Condition Expression (JSON)</label>
              <textarea 
                value={testRuleExpr}
                onChange={e => setTestRuleExpr(e.target.value)}
                rows={8}
                style={{
                  background: '#090d16',
                  border: '1px solid var(--panel-border)',
                  borderRadius: '6px',
                  padding: '10px',
                  color: '#10b981',
                  fontFamily: 'monospace',
                  fontSize: '0.8rem',
                  outline: 'none',
                  resize: 'vertical'
                }}
              />
            </div>
          </div>

          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Terminal size={16} color="var(--accent)" />
              2. Security Event Payload
            </h3>
            <textarea 
              value={testEventData}
              onChange={e => setTestEventData(e.target.value)}
              rows={8}
              style={{
                background: '#090d16',
                border: '1px solid var(--panel-border)',
                borderRadius: '6px',
                padding: '10px',
                color: '#8b5cf6',
                fontFamily: 'monospace',
                fontSize: '0.8rem',
                outline: 'none',
                resize: 'vertical'
              }}
            />
          </div>

          <button 
            onClick={handleTestRule} 
            disabled={testingRule}
            className="primary" 
            style={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center', 
              gap: '8px', 
              padding: '12px',
              fontWeight: 600,
              background: 'linear-gradient(135deg, var(--primary) 0%, var(--accent) 100%)',
              borderColor: 'transparent',
              color: '#fff',
              boxShadow: '0 0 15px rgba(99, 102, 241, 0.4)'
            }}
          >
            {testingRule ? (
              <>
                <RefreshCw size={16} className="spin-animation" style={{ animation: 'spin 1.5s linear infinite' }} />
                <span>Simulating Rule Evaluation...</span>
              </>
            ) : (
              <>
                <Play size={16} />
                <span>Test Rule Evaluation (Simulate)</span>
              </>
            )}
          </button>

        </div>

        {/* Results Panel */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div className="glass-panel" style={{ flex: 1, minHeight: '350px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <h3 style={{ fontSize: '1rem', fontWeight: 600, borderBottom: '1px solid var(--panel-border)', paddingBottom: '8px' }}>
              Simulation Output & Trace
            </h3>

            {testError && (
              <div style={{ background: 'rgba(244, 63, 94, 0.15)', border: '1px solid var(--danger)', color: 'var(--danger)', padding: '12px', borderRadius: '6px', fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <XCircle size={16} />
                <span>{testError}</span>
              </div>
            )}

            {!testResult && !testError && (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', flex: 1, color: 'var(--text-muted)', gap: '8px' }}>
                <Sliders size={32} />
                <span style={{ fontSize: '0.85rem' }}>Configure rule criteria and event above, then click test.</span>
              </div>
            )}

            {testResult && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '20px', flex: 1 }}>
                {/* Result header */}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 16px', borderRadius: '8px', background: testResult.matched ? 'rgba(16, 185, 129, 0.12)' : 'rgba(244, 63, 94, 0.12)', border: `1px solid ${testResult.matched ? 'var(--success)' : 'var(--danger)'}` }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    {testResult.matched ? <CheckCircle size={18} color="var(--success)" /> : <XCircle size={18} color="var(--danger)" />}
                    <span style={{ fontWeight: 600, fontSize: '0.95rem', color: testResult.matched ? 'var(--success)' : 'var(--danger)' }}>
                      {testResult.matched ? "RULE MATCHED" : "RULE MISSED"}
                    </span>
                  </div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                    Latency: <strong>{testResult.executionTimeMs.toFixed(3)} ms</strong>
                  </span>
                </div>

                {/* Match reason */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Evaluation Reason</span>
                  <div style={{ background: '#090d16', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '12px', fontSize: '0.85rem', color: 'var(--text-primary)', fontFamily: 'monospace' }}>
                    {testResult.reason}
                  </div>
                </div>

                {/* Matched Conditions list */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Matched Conditions Trace</span>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                    {testResult.matchedConditions && testResult.matchedConditions.length > 0 ? (
                      testResult.matchedConditions.map((cond: string) => (
                        <span key={cond} style={{ fontSize: '0.7rem', fontWeight: 600, padding: '4px 10px', borderRadius: '12px', background: 'var(--primary-glow)', color: 'var(--primary)', border: '1px solid var(--primary)' }}>
                          {cond}
                        </span>
                      ))
                    ) : (
                      <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>No conditions were matched.</span>
                    )}
                  </div>
                </div>
              </div>
            )}

          </div>
        </div>

      </div>
    );
  };

  const renderPipeline = () => {
    const steps = [
      { id: 1, title: "1. Receive Event", desc: "SecurityEvent ingested by Server API via Agent batch upload.", active: true },
      { id: 2, title: "2. Normalization", desc: "Lowercases identifiers, trims text fields, formats metadata categories/severity tags.", active: true },
      { id: 3, title: "3. Pre Filter", desc: "Blocks invalid events, check minimum parameters before executing heavy evaluations.", active: true },
      { id: 4, title: "4. Rule Evaluation", desc: "Retrieves priority-sorted active policies from RuleCacheService. Evaluates Regex, Keyword, EventId.", active: true },
      { id: 5, title: "5. Threshold Evaluation", desc: "Evaluates rolling occurrences over window list via thread-safe Sliding Counter ConcurrentDictionary.", active: true },
      { id: 6, title: "6. Correlation", desc: "Assigns Alert Category, Alert Severity and correlation keys.", active: true },
      { id: 7, title: "7. Alert Creation", desc: "Alert instances populated, linked to trigger events and saved to SQLite Database (Unit of Work).", active: true },
      { id: 8, title: "8. Incident Correlation", desc: "Correlates alert with active Incidents (checking Rule, Agent, User, IP, Source, EventID) or spawns new cases.", active: true },
      { id: 9, title: "9. Notification", desc: "Fires realtime websocket notifications to SOC consoles (SignalR) and pushes Outbound Telegram Alerts.", active: true }
    ];

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
        <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <h3 style={{ fontSize: '1rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Activity size={16} color="var(--primary)" />
            Real-time Detection Pipeline v2 Flow
          </h3>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
            OneSecurity uses a high-performance, concurrent 9-stage pipeline to ingest, filter, correlate and alert on security logs forwarded by active Go Agents.
          </p>
        </div>

        {/* Visual pipeline representation */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', position: 'relative', paddingLeft: '24px' }}>
          {/* Vertical connecting line */}
          <div style={{ position: 'absolute', left: '11px', top: '10px', bottom: '10px', width: '2px', background: 'linear-gradient(to bottom, var(--primary) 0%, var(--accent) 50%, var(--success) 100%)' }} />

          {steps.map((s, idx) => (
            <div key={s.id} className="glass-panel" style={{ 
              position: 'relative', 
              display: 'flex', 
              gap: '16px', 
              alignItems: 'flex-start',
              padding: '16px',
              borderLeft: '4px solid var(--primary)',
              marginLeft: '12px'
            }}>
              {/* Circular node */}
              <div style={{ 
                position: 'absolute', 
                left: '-32px', 
                top: '16px', 
                width: '16px', 
                height: '16px', 
                borderRadius: '50%', 
                background: '#090d16',
                border: '3px solid var(--primary)',
                boxShadow: '0 0 10px var(--primary-glow)'
              }} />

              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', flex: 1 }}>
                <h4 style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-primary)' }}>{s.title}</h4>
                <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', lineHeight: 1.4 }}>{s.desc}</p>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '0.7rem', fontWeight: 600, color: 'var(--success)', background: 'var(--success-glow)', padding: '2px 8px', borderRadius: '4px' }}>
                <CheckCircle size={10} /> Active
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Selector Subtabs */}
      <div style={{ display: 'flex', gap: '12px', borderBottom: '1px solid var(--panel-border)', paddingBottom: '12px' }}>
        <button 
          onClick={() => setActiveSubTab('statistics')}
          style={{
            background: activeSubTab === 'statistics' ? 'var(--primary)' : 'transparent',
            borderColor: activeSubTab === 'statistics' ? 'transparent' : 'var(--panel-border)',
            color: activeSubTab === 'statistics' ? '#fff' : 'var(--text-secondary)',
            fontWeight: 600,
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}
        >
          <BarChart2 size={14} /> Analytics & Performance
        </button>
        <button 
          onClick={() => setActiveSubTab('tester')}
          style={{
            background: activeSubTab === 'tester' ? 'var(--primary)' : 'transparent',
            borderColor: activeSubTab === 'tester' ? 'transparent' : 'var(--panel-border)',
            color: activeSubTab === 'tester' ? '#fff' : 'var(--text-secondary)',
            fontWeight: 600,
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}
        >
          <Sliders size={14} /> Rule Tester (Sandbox)
        </button>
        <button 
          onClick={() => setActiveSubTab('pipeline')}
          style={{
            background: activeSubTab === 'pipeline' ? 'var(--primary)' : 'transparent',
            borderColor: activeSubTab === 'pipeline' ? 'transparent' : 'var(--panel-border)',
            color: activeSubTab === 'pipeline' ? '#fff' : 'var(--text-secondary)',
            fontWeight: 600,
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}
        >
          <Activity size={14} /> Ingestion Pipeline
        </button>
      </div>

      {/* Render subtab */}
      {activeSubTab === 'statistics' && renderStats()}
      {activeSubTab === 'tester' && renderTester()}
      {activeSubTab === 'pipeline' && renderPipeline()}
    </div>
  );
};
