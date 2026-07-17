import React, { useState, useEffect } from 'react';
import { 
  Server, Shield, Cpu, Key, Plus, RefreshCw, Trash2, Edit2, 
  Check, Play, Pause, X, AlertTriangle, Info, Copy, CheckCircle 
} from 'lucide-react';

interface InfrastructureProps {
  user: any;
  authFetch: (url: string, options?: RequestInit) => Promise<Response>;
}

export const Infrastructure: React.FC<InfrastructureProps> = ({ user, authFetch }) => {
  const [activeTab, setActiveTab] = useState<'assets' | 'collectors' | 'policies' | 'enrollment'>('assets');
  const [loading, setLoading] = useState(false);
  
  // Data lists
  const [assets, setAssets] = useState<any[]>([]);
  const [collectors, setCollectors] = useState<any[]>([]);
  const [policies, setPolicies] = useState<any[]>([]);
  const [tokens, setTokens] = useState<any[]>([]);
  
  // Forms & Modals
  const [showAddAssetModal, setShowAddAssetModal] = useState(false);
  const [newAsset, setNewAsset] = useState({
    hostname: '', ipAddress: '', operatingSystem: 'Windows', operatingSystemVersion: 'Server 2022',
    domain: '', department: '', building: '', owner: '', description: '',
    criticality: 'Medium', assetType: 'Server', collectorId: 1, policyId: 1
  });

  const [showAddCollectorModal, setShowAddCollectorModal] = useState(false);
  const [newCollector, setNewCollector] = useState({
    name: '', ipAddress: '127.0.0.1', collectorKey: '', sharedSecret: '',
    location: '', description: '', version: '1.2.0'
  });

  const [showAddPolicyModal, setShowAddPolicyModal] = useState(false);
  const [newPolicy, setNewPolicy] = useState({
    name: '', heartbeatInterval: 10, metricsInterval: 10,
    enabledLogs: 'ProcessMonitor,FileIntegrity,NetworkMonitor',
    responseEnabled: true, description: ''
  });

  const [showGenerateTokenModal, setShowGenerateTokenModal] = useState(false);
  const [newTokenReq, setNewTokenReq] = useState({
    assetId: 0, policyId: 1, collectorId: 1, maxUses: 1, reason: ''
  });

  const [generatedTokenString, setGeneratedTokenString] = useState<string | null>(null);
  const [copiedToken, setCopiedToken] = useState(false);

  // Role check helpers
  const isViewer = user?.role === 'Viewer';
  const isOperator = user?.role === 'Operator' || user?.role === 'Administrator' || user?.role === 'SuperAdmin';
  const isAdmin = user?.role === 'Administrator' || user?.role === 'SuperAdmin';

  // Fetch functions
  const fetchData = async () => {
    setLoading(true);
    try {
      const resAssets = await authFetch('/api/v1/assets');
      if (resAssets.ok) setAssets(await resAssets.json());

      const resCollectors = await authFetch('/api/v1/collectors');
      if (resCollectors.ok) setCollectors(await resCollectors.json());

      const resPolicies = await authFetch('/api/v1/policies');
      if (resPolicies.ok) setPolicies(await resPolicies.json());

      const resTokens = await authFetch('/api/v1/enrollment/tokens');
      if (resTokens.ok) setTokens(await resTokens.json());
    } catch (err) {
      console.error('Error fetching infrastructure data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [activeTab]);

  // Copy helper
  const handleCopyToken = (tok: string) => {
    navigator.clipboard.writeText(tok);
    setCopiedToken(true);
    setTimeout(() => setCopiedToken(false), 2000);
  };

  // Submit Handlers
  const handleCreateAsset = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await authFetch('/api/v1/assets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newAsset)
      });
      if (res.ok) {
        setShowAddAssetModal(false);
        setNewAsset({
          hostname: '', ipAddress: '', operatingSystem: 'Windows', operatingSystemVersion: 'Server 2022',
          domain: '', department: '', building: '', owner: '', description: '',
          criticality: 'Medium', assetType: 'Server', collectorId: collectors[0]?.id || 1, policyId: policies[0]?.id || 1
        });
        fetchData();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleCreateCollector = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await authFetch('/api/v1/collectors', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newCollector)
      });
      if (res.ok) {
        setShowAddCollectorModal(false);
        setNewCollector({
          name: '', ipAddress: '127.0.0.1', collectorKey: '', sharedSecret: '',
          location: '', description: '', version: '1.2.0'
        });
        fetchData();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleCreatePolicy = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await authFetch('/api/v1/policies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newPolicy)
      });
      if (res.ok) {
        setShowAddPolicyModal(false);
        setNewPolicy({
          name: '', heartbeatInterval: 10, metricsInterval: 10,
          enabledLogs: 'ProcessMonitor,FileIntegrity,NetworkMonitor',
          responseEnabled: true, description: ''
        });
        fetchData();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleGenerateToken = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await authFetch('/api/v1/enrollment/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newTokenReq)
      });
      if (res.ok) {
        const tokenData = await res.json();
        setGeneratedTokenString(tokenData.token);
        setShowGenerateTokenModal(false);
        setNewTokenReq({
          assetId: 0, policyId: policies[0]?.id || 1, collectorId: collectors[0]?.id || 1, maxUses: 1, reason: ''
        });
        fetchData();
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleDeleteAsset = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this asset?')) return;
    try {
      const res = await authFetch(`/api/v1/assets/${id}`, { method: 'DELETE' });
      if (res.ok) fetchData();
    } catch (err) {
      console.error(err);
    }
  };

  const handleTransitionAssetStatus = async (id: number, targetStatus: string) => {
    try {
      const res = await authFetch(`/api/v1/assets/${id}/status`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(targetStatus)
      });
      if (res.ok) fetchData();
    } catch (err) {
      console.error(err);
    }
  };

  // Helper selectors
  const getCollectorName = (colId: number) => collectors.find(c => c.id === colId)?.name || 'Unknown';
  const getPolicyName = (polId: number) => policies.find(p => p.id === polId)?.name || 'Unknown';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem', width: '100%' }}>
      {/* Sub tabs navigation */}
      <div style={{
        display: 'flex',
        gap: '0.5rem',
        padding: '6px',
        background: 'var(--panel-bg-dark, rgba(17, 24, 39, 0.4))',
        border: '1px solid var(--panel-border)',
        borderRadius: '12px',
        alignSelf: 'flex-start'
      }}>
        <button
          onClick={() => setActiveTab('assets')}
          style={{
            display: 'flex', alignItems: 'center', gap: '8px', padding: '8px 16px', borderRadius: '8px', border: 'none',
            background: activeTab === 'assets' ? 'rgba(99, 102, 241, 0.15)' : 'transparent',
            color: activeTab === 'assets' ? '#ffffff' : 'var(--text-secondary)',
            fontWeight: activeTab === 'assets' ? 600 : 500, cursor: 'pointer', transition: 'all 0.2s'
          }}
        >
          <Server size={16} />
          Assets
        </button>
        <button
          onClick={() => setActiveTab('collectors')}
          style={{
            display: 'flex', alignItems: 'center', gap: '8px', padding: '8px 16px', borderRadius: '8px', border: 'none',
            background: activeTab === 'collectors' ? 'rgba(99, 102, 241, 0.15)' : 'transparent',
            color: activeTab === 'collectors' ? '#ffffff' : 'var(--text-secondary)',
            fontWeight: activeTab === 'collectors' ? 600 : 500, cursor: 'pointer', transition: 'all 0.2s'
          }}
        >
          <Cpu size={16} />
          Collectors
        </button>
        <button
          onClick={() => setActiveTab('policies')}
          style={{
            display: 'flex', alignItems: 'center', gap: '8px', padding: '8px 16px', borderRadius: '8px', border: 'none',
            background: activeTab === 'policies' ? 'rgba(99, 102, 241, 0.15)' : 'transparent',
            color: activeTab === 'policies' ? '#ffffff' : 'var(--text-secondary)',
            fontWeight: activeTab === 'policies' ? 600 : 500, cursor: 'pointer', transition: 'all 0.2s'
          }}
        >
          <Shield size={16} />
          Policies
        </button>
        <button
          onClick={() => setActiveTab('enrollment')}
          style={{
            display: 'flex', alignItems: 'center', gap: '8px', padding: '8px 16px', borderRadius: '8px', border: 'none',
            background: activeTab === 'enrollment' ? 'rgba(99, 102, 241, 0.15)' : 'transparent',
            color: activeTab === 'enrollment' ? '#ffffff' : 'var(--text-secondary)',
            fontWeight: activeTab === 'enrollment' ? 600 : 500, cursor: 'pointer', transition: 'all 0.2s'
          }}
        >
          <Key size={16} />
          Enrollment Keys
        </button>
      </div>

      {/* Copy notification popup */}
      {generatedTokenString && (
        <div style={{
          background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px',
          padding: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem',
          boxShadow: '0 8px 32px rgba(0, 0, 0, 0.4)', maxWidth: '600px', width: '100%',
          animation: 'fadeIn 0.3s'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', color: 'var(--success)' }}>
            <CheckCircle size={20} />
            <h4 style={{ margin: 0, fontWeight: 600 }}>Enrollment Key Generated Successfully</h4>
          </div>
          <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
            This token is uniquely bound to its designated Asset and Collector. Copy it to configure your local agent:
          </p>
          <div style={{
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            background: 'rgba(0,0,0,0.3)', border: '1px solid var(--panel-border)',
            padding: '10px 15px', borderRadius: '8px', fontFamily: 'monospace', fontSize: '0.9rem'
          }}>
            <span style={{ color: 'var(--primary)', overflow: 'hidden', textOverflow: 'ellipsis' }}>{generatedTokenString}</span>
            <button 
              onClick={() => handleCopyToken(generatedTokenString)}
              style={{
                background: 'transparent', border: 'none', color: copiedToken ? 'var(--success)' : 'var(--text-secondary)',
                cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '4px'
              }}
            >
              {copiedToken ? <Check size={16} /> : <Copy size={16} />}
              {copiedToken ? 'Copied' : 'Copy'}
            </button>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', fontSize: '0.75rem', color: 'var(--text-secondary)', borderTop: '1px solid var(--panel-border)', paddingTop: '1rem' }}>
            <span style={{ fontWeight: 600, color: '#ffffff' }}>Deployment Instructions:</span>
            <span>1. Extract the <strong style={{ color: 'var(--primary)' }}>agent-laptop-package.zip</strong> package on target machine.</span>
            <span>2. Open <strong style={{ color: 'var(--primary)' }}>config.json</strong> and set the parameters:</span>
            <pre style={{ margin: '5px 0', background: 'rgba(0,0,0,0.2)', padding: '6px', borderRadius: '4px', color: '#ffb300' }}>
{`{
  "ServerUrl": "http://localhost:5050",
  "CollectorId": ${newTokenReq.collectorId || 1},
  "EnrollmentToken": "${generatedTokenString}"
}`}
            </pre>
            <span>3. Execute <strong style={{ color: 'var(--primary)' }}>run_agent.bat</strong>. The agent status will automatically transition to Online.</span>
          </div>
          <button 
            onClick={() => setGeneratedTokenString(null)}
            style={{
              alignSelf: 'flex-end', background: 'rgba(255,255,255,0.08)', color: '#ffffff', border: 'none',
              padding: '6px 16px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600
            }}
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Main content table panels */}
      {loading ? (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '200px', color: 'var(--text-secondary)' }}>
          <RefreshCw size={24} style={{ animation: 'spin 1.5s linear infinite', marginRight: '8px' }} />
          <span>Synchronizing infrastructure state...</span>
        </div>
      ) : (
        <>
          {/* TAB: ASSETS */}
          {activeTab === 'assets' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ margin: 0, fontWeight: 600 }}>Infrastructure Assets</h3>
                {isOperator && (
                  <button 
                    onClick={() => setShowAddAssetModal(true)}
                    style={{
                      background: 'var(--primary)', color: '#ffffff', border: 'none', borderRadius: '8px',
                      padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer', fontWeight: 600
                    }}
                  >
                    <Plus size={16} /> Add Asset
                  </button>
                )}
              </div>

              <div style={{ background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px', overflow: 'hidden' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'rgba(255,255,255,0.02)', borderBottom: '1px solid var(--panel-border)' }}>
                      <th style={{ padding: '12px 16px' }}>Criticality</th>
                      <th style={{ padding: '12px 16px' }}>Hostname</th>
                      <th style={{ padding: '12px 16px' }}>IP Address</th>
                      <th style={{ padding: '12px 16px' }}>OS</th>
                      <th style={{ padding: '12px 16px' }}>Collector</th>
                      <th style={{ padding: '12px 16px' }}>Policy</th>
                      <th style={{ padding: '12px 16px' }}>Status</th>
                      <th style={{ padding: '12px 16px' }}>Last Seen</th>
                      <th style={{ padding: '12px 16px', textAlign: 'right' }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {assets.length === 0 ? (
                      <tr>
                        <td colSpan={9} style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>No assets defined.</td>
                      </tr>
                    ) : (
                      assets.map(asset => (
                        <tr key={asset.id} style={{ borderBottom: '1px solid var(--panel-border)' }}>
                          <td style={{ padding: '12px 16px' }}>
                            <span style={{
                              padding: '2px 8px', borderRadius: '10px', fontSize: '0.7rem', fontWeight: 700,
                              background: asset.criticality === 'Critical' ? 'rgba(239, 68, 68, 0.15)' : asset.criticality === 'High' ? 'rgba(245, 158, 11, 0.15)' : 'rgba(99, 102, 241, 0.15)',
                              color: asset.criticality === 'Critical' ? 'var(--danger)' : asset.criticality === 'High' ? 'var(--warning)' : 'var(--primary)'
                            }}>
                              {asset.criticality}
                            </span>
                          </td>
                          <td style={{ padding: '12px 16px', fontWeight: 600 }}>{asset.hostname}</td>
                          <td style={{ padding: '12px 16px', fontFamily: 'monospace' }}>{asset.ipAddress}</td>
                          <td style={{ padding: '12px 16px' }}>{asset.operatingSystem} ({asset.operatingSystemVersion})</td>
                          <td style={{ padding: '12px 16px' }}>{getCollectorName(asset.collectorId)}</td>
                          <td style={{ padding: '12px 16px' }}>{getPolicyName(asset.policyId)}</td>
                          <td style={{ padding: '12px 16px' }}>
                            <span style={{
                              padding: '2px 8px', borderRadius: '10px', fontSize: '0.7rem', fontWeight: 700,
                              background: asset.status === 'Managed' ? 'rgba(16, 185, 129, 0.15)' : asset.status === 'PendingEnrollment' ? 'rgba(245, 158, 11, 0.15)' : 'rgba(156, 163, 175, 0.15)',
                              color: asset.status === 'Managed' ? 'var(--success)' : asset.status === 'PendingEnrollment' ? 'var(--warning)' : 'var(--text-secondary)'
                            }}>
                              {asset.status}
                            </span>
                          </td>
                          <td style={{ padding: '12px 16px', color: 'var(--text-secondary)' }}>
                            {asset.lastSeen ? new Date(asset.lastSeen).toLocaleString() : 'Never'}
                          </td>
                          <td style={{ padding: '12px 16px', textAlign: 'right' }}>
                            <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                              {isOperator && asset.status === 'Managed' && (
                                <button
                                  onClick={() => handleTransitionAssetStatus(asset.id, 'Maintenance')}
                                  title="Enter Maintenance"
                                  style={{ background: 'transparent', border: 'none', color: 'var(--warning)', cursor: 'pointer' }}
                                >
                                  <Pause size={14} />
                                </button>
                              )}
                              {isOperator && asset.status === 'Maintenance' && (
                                <button
                                  onClick={() => handleTransitionAssetStatus(asset.id, 'Managed')}
                                  title="Resume Monitoring"
                                  style={{ background: 'transparent', border: 'none', color: 'var(--success)', cursor: 'pointer' }}
                                >
                                  <Play size={14} />
                                </button>
                              )}
                              {isAdmin && (
                                <button
                                  onClick={() => handleDeleteAsset(asset.id)}
                                  title="Delete Asset"
                                  style={{ background: 'transparent', border: 'none', color: 'var(--danger)', cursor: 'pointer' }}
                                >
                                  <Trash2 size={14} />
                                </button>
                              )}
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* TAB: COLLECTORS */}
          {activeTab === 'collectors' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ margin: 0, fontWeight: 600 }}>Collector Nodes</h3>
                {isAdmin && (
                  <button 
                    onClick={() => setShowAddCollectorModal(true)}
                    style={{
                      background: 'var(--primary)', color: '#ffffff', border: 'none', borderRadius: '8px',
                      padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer', fontWeight: 600
                    }}
                  >
                    <Plus size={16} /> Register Collector
                  </button>
                )}
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1rem' }}>
                {collectors.length === 0 ? (
                  <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)', gridColumn: '1/-1' }}>No collectors registered.</div>
                ) : (
                  collectors.map(col => (
                    <div 
                      key={col.id}
                      style={{
                        background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px',
                        padding: '1.25rem', display: 'flex', flexDirection: 'column', gap: '0.75rem', position: 'relative'
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontWeight: 600, fontSize: '1rem' }}>{col.name}</span>
                        <span style={{
                          padding: '2px 8px', borderRadius: '10px', fontSize: '0.65rem', fontWeight: 700,
                          background: col.status === 'Online' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
                          color: col.status === 'Online' ? 'var(--success)' : 'var(--danger)'
                        }}>
                          {col.status}
                        </span>
                      </div>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                        <div>IP Address: <span style={{ fontFamily: 'monospace', color: '#ffffff' }}>{col.ipAddress}</span></div>
                        <div>Collector Key: <span style={{ fontFamily: 'monospace', color: '#ffffff' }}>{col.collectorKey}</span></div>
                        <div>Location: <span style={{ color: '#ffffff' }}>{col.location || 'N/A'}</span></div>
                        <div>Version: <span style={{ color: '#ffffff' }}>{col.version}</span></div>
                        <div>Config Version: <span style={{ color: '#ffffff' }}>{col.configurationVersion}</span></div>
                        <div>Rules Version: <span style={{ color: '#ffffff' }}>{col.rulesVersion}</span></div>
                      </div>
                      <div style={{ borderTop: '1px solid var(--panel-border)', paddingTop: '0.5rem', fontSize: '0.7rem', color: 'var(--text-secondary)' }}>
                        Last Sync: {new Date(col.lastSync).toLocaleString()}
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}

          {/* TAB: POLICIES */}
          {activeTab === 'policies' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ margin: 0, fontWeight: 600 }}>Agent Monitoring Policies</h3>
                {isAdmin && (
                  <button 
                    onClick={() => setShowAddPolicyModal(true)}
                    style={{
                      background: 'var(--primary)', color: '#ffffff', border: 'none', borderRadius: '8px',
                      padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer', fontWeight: 600
                    }}
                  >
                    <Plus size={16} /> Create Policy
                  </button>
                )}
              </div>

              <div style={{ background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px', overflow: 'hidden' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'rgba(255,255,255,0.02)', borderBottom: '1px solid var(--panel-border)' }}>
                      <th style={{ padding: '12px 16px' }}>Policy Name</th>
                      <th style={{ padding: '12px 16px' }}>Heartbeat Interval (s)</th>
                      <th style={{ padding: '12px 16px' }}>Metrics Interval (s)</th>
                      <th style={{ padding: '12px 16px' }}>Logging Capabilities</th>
                      <th style={{ padding: '12px 16px' }}>Active Responses</th>
                      <th style={{ padding: '12px 16px' }}>Description</th>
                      <th style={{ padding: '12px 16px' }}>Version</th>
                    </tr>
                  </thead>
                  <tbody>
                    {policies.map(policy => (
                      <tr key={policy.id} style={{ borderBottom: '1px solid var(--panel-border)' }}>
                        <td style={{ padding: '12px 16px', fontWeight: 600 }}>{policy.name}</td>
                        <td style={{ padding: '12px 16px', fontFamily: 'monospace' }}>{policy.heartbeatInterval}</td>
                        <td style={{ padding: '12px 16px', fontFamily: 'monospace' }}>{policy.metricsInterval}</td>
                        <td style={{ padding: '12px 16px' }}>
                          <span style={{ fontSize: '0.75rem', color: 'var(--primary)', background: 'rgba(99, 102, 241, 0.1)', padding: '2px 6px', borderRadius: '4px' }}>
                            {policy.enabledLogs}
                          </span>
                        </td>
                        <td style={{ padding: '12px 16px' }}>
                          <span style={{ color: policy.responseEnabled ? 'var(--success)' : 'var(--danger)' }}>
                            {policy.responseEnabled ? 'Enabled' : 'Disabled'}
                          </span>
                        </td>
                        <td style={{ padding: '12px 16px', color: 'var(--text-secondary)' }}>{policy.description || 'N/A'}</td>
                        <td style={{ padding: '12px 16px', fontWeight: 600 }}>v{policy.version}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* TAB: ENROLLMENT */}
          {activeTab === 'enrollment' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ margin: 0, fontWeight: 600 }}>Enrollment Tokens</h3>
                {isOperator && (
                  <button 
                    onClick={() => setShowGenerateTokenModal(true)}
                    style={{
                      background: 'var(--primary)', color: '#ffffff', border: 'none', borderRadius: '8px',
                      padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '6px', cursor: 'pointer', fontWeight: 600
                    }}
                  >
                    <Key size={16} /> Generate Key
                  </button>
                )}
              </div>

              <div style={{ background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px', overflow: 'hidden' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'rgba(255,255,255,0.02)', borderBottom: '1px solid var(--panel-border)' }}>
                      <th style={{ padding: '12px 16px' }}>Key (Token)</th>
                      <th style={{ padding: '12px 16px' }}>Target Asset</th>
                      <th style={{ padding: '12px 16px' }}>Policy</th>
                      <th style={{ padding: '12px 16px' }}>Collector</th>
                      <th style={{ padding: '12px 16px' }}>Expiration</th>
                      <th style={{ padding: '12px 16px' }}>Usage</th>
                      <th style={{ padding: '12px 16px' }}>Generated By</th>
                      <th style={{ padding: '12px 16px', textAlign: 'right' }}>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tokens.length === 0 ? (
                      <tr>
                        <td colSpan={8} style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>No keys generated.</td>
                      </tr>
                    ) : (
                      tokens.map(tok => (
                        <tr key={tok.id} style={{ borderBottom: '1px solid var(--panel-border)' }}>
                          <td style={{ padding: '12px 16px', fontFamily: 'monospace', fontWeight: 600, color: 'var(--primary)' }}>
                            {tok.token}
                          </td>
                          <td style={{ padding: '12px 16px' }}>{tok.asset?.hostname || 'Unknown'}</td>
                          <td style={{ padding: '12px 16px' }}>{tok.policy?.name || 'Unknown'}</td>
                          <td style={{ padding: '12px 16px' }}>{tok.collector?.name || 'Unknown'}</td>
                          <td style={{ padding: '12px 16px', color: new Date(tok.expireAt).getTime() < Date.now() ? 'var(--danger)' : 'var(--text-secondary)' }}>
                            {new Date(tok.expireAt).toLocaleString()}
                          </td>
                          <td style={{ padding: '12px 16px' }}>
                            <span style={{
                              padding: '2px 8px', borderRadius: '10px', fontSize: '0.7rem', fontWeight: 700,
                              background: tok.used ? 'rgba(16, 185, 129, 0.15)' : 'rgba(245, 158, 11, 0.15)',
                              color: tok.used ? 'var(--success)' : 'var(--warning)'
                            }}>
                              {tok.usedCount} / {tok.maxUses} Uses
                            </span>
                          </td>
                          <td style={{ padding: '12px 16px', color: 'var(--text-secondary)' }}>{tok.createdBy}</td>
                          <td style={{ padding: '12px 16px', textAlign: 'right' }}>
                            <button
                              onClick={() => handleCopyToken(tok.token)}
                              style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}
                              title="Copy key"
                            >
                              <Copy size={14} />
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}

      {/* MODAL: ADD ASSET */}
      {showAddAssetModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.6)',
          display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <form 
            onSubmit={handleCreateAsset}
            style={{
              background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px',
              padding: '1.5rem', width: '100%', maxWidth: '500px', display: 'flex', flexDirection: 'column', gap: '1rem',
              boxShadow: '0 8px 32px rgba(0, 0, 0, 0.4)'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h4 style={{ margin: 0, fontWeight: 600 }}>Create New Infrastructure Asset</h4>
              <button type="button" onClick={() => setShowAddAssetModal(false)} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>
            
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Hostname *</label>
                <input 
                  type="text" required value={newAsset.hostname} onChange={e => setNewAsset({ ...newAsset, hostname: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>IP Address *</label>
                <input 
                  type="text" required value={newAsset.ipAddress} onChange={e => setNewAsset({ ...newAsset, ipAddress: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Operating System *</label>
                <select 
                  value={newAsset.operatingSystem} onChange={e => setNewAsset({ ...newAsset, operatingSystem: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                >
                  <option value="Windows">Windows</option>
                  <option value="Linux">Linux</option>
                  <option value="macOS">macOS</option>
                </select>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>OS Version</label>
                <input 
                  type="text" value={newAsset.operatingSystemVersion} onChange={e => setNewAsset({ ...newAsset, operatingSystemVersion: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Asset Type</label>
                <select 
                  value={newAsset.assetType} onChange={e => setNewAsset({ ...newAsset, assetType: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                >
                  <option value="Server">Server</option>
                  <option value="Workstation">Workstation</option>
                  <option value="Laptop">Laptop</option>
                </select>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Criticality</label>
                <select 
                  value={newAsset.criticality} onChange={e => setNewAsset({ ...newAsset, criticality: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                >
                  <option value="Critical">Critical</option>
                  <option value="High">High</option>
                  <option value="Medium">Medium</option>
                  <option value="Low">Low</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Collector Node</label>
                <select 
                  value={newAsset.collectorId} onChange={e => setNewAsset({ ...newAsset, collectorId: parseInt(e.target.value) })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                >
                  {collectors.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Policy</label>
                <select 
                  value={newAsset.policyId} onChange={e => setNewAsset({ ...newAsset, policyId: parseInt(e.target.value) })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                >
                  {policies.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Owner / Custodian</label>
              <input 
                type="text" value={newAsset.owner} onChange={e => setNewAsset({ ...newAsset, owner: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '1rem' }}>
              <button 
                type="button" onClick={() => setShowAddAssetModal(false)}
                style={{ background: 'rgba(255,255,255,0.08)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer' }}
              >
                Cancel
              </button>
              <button 
                type="submit"
                style={{ background: 'var(--primary)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 }}
              >
                Save Asset
              </button>
            </div>
          </form>
        </div>
      )}

      {/* MODAL: REGISTER COLLECTOR */}
      {showAddCollectorModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.6)',
          display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <form 
            onSubmit={handleCreateCollector}
            style={{
              background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px',
              padding: '1.5rem', width: '100%', maxWidth: '500px', display: 'flex', flexDirection: 'column', gap: '1rem',
              boxShadow: '0 8px 32px rgba(0, 0, 0, 0.4)'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h4 style={{ margin: 0, fontWeight: 600 }}>Register Collector Node</h4>
              <button type="button" onClick={() => setShowAddCollectorModal(false)} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Name *</label>
              <input 
                type="text" required value={newCollector.name} onChange={e => setNewCollector({ ...newCollector, name: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>IP Address *</label>
                <input 
                  type="text" required value={newCollector.ipAddress} onChange={e => setNewCollector({ ...newCollector, ipAddress: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Collector Key *</label>
                <input 
                  type="text" required placeholder="e.g. primary_key" value={newCollector.collectorKey} onChange={e => setNewCollector({ ...newCollector, collectorKey: e.target.value })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Shared Secret *</label>
              <input 
                type="password" required placeholder="For secure HMAC sync authorization" value={newCollector.sharedSecret} onChange={e => setNewCollector({ ...newCollector, sharedSecret: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Location / Context</label>
              <input 
                type="text" placeholder="e.g. Cloud Host, On-Premise Clinic" value={newCollector.location} onChange={e => setNewCollector({ ...newCollector, location: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '1rem' }}>
              <button 
                type="button" onClick={() => setShowAddCollectorModal(false)}
                style={{ background: 'rgba(255,255,255,0.08)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer' }}
              >
                Cancel
              </button>
              <button 
                type="submit"
                style={{ background: 'var(--primary)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 }}
              >
                Register
              </button>
            </div>
          </form>
        </div>
      )}

      {/* MODAL: ADD POLICY */}
      {showAddPolicyModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.6)',
          display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <form 
            onSubmit={handleCreatePolicy}
            style={{
              background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px',
              padding: '1.5rem', width: '100%', maxWidth: '500px', display: 'flex', flexDirection: 'column', gap: '1rem',
              boxShadow: '0 8px 32px rgba(0, 0, 0, 0.4)'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h4 style={{ margin: 0, fontWeight: 600 }}>Create Monitoring Policy</h4>
              <button type="button" onClick={() => setShowAddPolicyModal(false)} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Policy Name *</label>
              <input 
                type="text" required placeholder="e.g. High-Security Server" value={newPolicy.name} onChange={e => setNewPolicy({ ...newPolicy, name: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Heartbeat Interval (s)</label>
                <input 
                  type="number" required value={newPolicy.heartbeatInterval} onChange={e => setNewPolicy({ ...newPolicy, heartbeatInterval: parseInt(e.target.value) })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Metrics Interval (s)</label>
                <input 
                  type="number" required value={newPolicy.metricsInterval} onChange={e => setNewPolicy({ ...newPolicy, metricsInterval: parseInt(e.target.value) })}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
                />
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Enabled Capabilities (Comma-separated)</label>
              <input 
                type="text" required value={newPolicy.enabledLogs} onChange={e => setNewPolicy({ ...newPolicy, enabledLogs: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <input 
                type="checkbox" id="resEnabled" checked={newPolicy.responseEnabled} onChange={e => setNewPolicy({ ...newPolicy, responseEnabled: e.target.checked })}
                style={{ cursor: 'pointer' }}
              />
              <label htmlFor="resEnabled" style={{ fontSize: '0.85rem', color: '#ffffff', cursor: 'pointer' }}>Active Response Actions (Kill Process, Quarantine, etc.)</label>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Description</label>
              <textarea 
                placeholder="Details of what this policy enforces" value={newPolicy.description} onChange={e => setNewPolicy({ ...newPolicy, description: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff', minHeight: '80px' }}
              />
            </div>

            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '1rem' }}>
              <button 
                type="button" onClick={() => setShowAddPolicyModal(false)}
                style={{ background: 'rgba(255,255,255,0.08)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer' }}
              >
                Cancel
              </button>
              <button 
                type="submit"
                style={{ background: 'var(--primary)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 }}
              >
                Create
              </button>
            </div>
          </form>
        </div>
      )}

      {/* MODAL: GENERATE ENROLLMENT KEY */}
      {showGenerateTokenModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.6)',
          display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <form 
            onSubmit={handleGenerateToken}
            style={{
              background: 'var(--panel-bg)', border: '1px solid var(--panel-border)', borderRadius: '12px',
              padding: '1.5rem', width: '100%', maxWidth: '500px', display: 'flex', flexDirection: 'column', gap: '1rem',
              boxShadow: '0 8px 32px rgba(0, 0, 0, 0.4)'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h4 style={{ margin: 0, fontWeight: 600 }}>Generate Enrollment Key</h4>
              <button type="button" onClick={() => setShowGenerateTokenModal(false)} style={{ background: 'transparent', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Target Asset *</label>
              <select 
                required value={newTokenReq.assetId} onChange={e => {
                  const assetId = parseInt(e.target.value);
                  const selectedAsset = assets.find(a => a.id === assetId);
                  setNewTokenReq({
                    ...newTokenReq,
                    assetId,
                    collectorId: selectedAsset?.collectorId || collectors[0]?.id || 1,
                    policyId: selectedAsset?.policyId || policies[0]?.id || 1
                  });
                }}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              >
                <option value="">Select Asset...</option>
                {assets.filter(a => a.status === 'PendingEnrollment' || a.status === 'Discovered').map(a => (
                  <option key={a.id} value={a.id}>{a.hostname} ({a.ipAddress})</option>
                ))}
              </select>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Binding Collector</label>
                <select 
                  disabled
                  value={newTokenReq.collectorId}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(255,255,255,0.05)', border: '1px solid var(--panel-border)', color: 'var(--text-secondary)' }}
                >
                  {collectors.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Binding Policy</label>
                <select 
                  disabled
                  value={newTokenReq.policyId}
                  style={{ padding: '8px', borderRadius: '6px', background: 'rgba(255,255,255,0.05)', border: '1px solid var(--panel-border)', color: 'var(--text-secondary)' }}
                >
                  {policies.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Maximum Enrollment Uses</label>
              <input 
                type="number" required min={1} value={newTokenReq.maxUses} onChange={e => setNewTokenReq({ ...newTokenReq, maxUses: parseInt(e.target.value) })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Reason / Remarks</label>
              <input 
                type="text" placeholder="e.g. Deploying security agent on Laptop" value={newTokenReq.reason} onChange={e => setNewTokenReq({ ...newTokenReq, reason: e.target.value })}
                style={{ padding: '8px', borderRadius: '6px', background: 'rgba(0,0,0,0.2)', border: '1px solid var(--panel-border)', color: '#ffffff' }}
              />
            </div>

            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '1rem' }}>
              <button 
                type="button" onClick={() => setShowGenerateTokenModal(false)}
                style={{ background: 'rgba(255,255,255,0.08)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer' }}
              >
                Cancel
              </button>
              <button 
                type="submit"
                style={{ background: 'var(--primary)', color: '#ffffff', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 }}
              >
                Generate Key
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
