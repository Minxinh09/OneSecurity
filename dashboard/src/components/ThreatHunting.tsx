import React, { useState } from 'react';
import { 
  Search, 
  ShieldAlert, 
  Terminal, 
  User, 
  Calendar, 
  Info, 
  Loader2, 
  Cpu, 
  FileSpreadsheet, 
  Database,
  ArrowRight
} from 'lucide-react';

interface ThreatItem {
  type: string;
  id: string;
  title: string;
  description: string;
  severity: string;
  status: string;
  hostname: string;
  ipAddress: string;
  username: string;
  timestamp: string;
}

export const ThreatHunting: React.FC = () => {
  const [keyword, setKeyword] = useState('');
  const [hostname, setHostname] = useState('');
  const [ip, setIp] = useState('');
  const [username, setUsername] = useState('');
  const [severity, setSeverity] = useState('');
  const [status, setStatus] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const [results, setResults] = useState<ThreatItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSearched(true);

    try {
      const token = localStorage.getItem('token');
      
      // Build query string
      const params = new URLSearchParams();
      if (keyword) params.append('keyword', keyword);
      if (hostname) params.append('hostname', hostname);
      if (ip) params.append('ip', ip);
      if (username) params.append('username', username);
      if (severity) params.append('severity', severity);
      if (status) params.append('status', status);
      if (from) params.append('from', new Date(from).toISOString());
      if (to) params.append('to', new Date(to).toISOString());

      const res = await fetch(`/api/v1/hunting/search?${params.toString()}`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!res.ok) {
        throw new Error(`Search failed: ${res.status}`);
      }

      const searchData = await res.json();
      setResults(searchData.items || []);
    } catch (err: any) {
      setError(err.message || 'Error occurred during threat hunting search.');
    } finally {
      setLoading(false);
    }
  };

  const handleClear = () => {
    setKeyword('');
    setHostname('');
    setIp('');
    setUsername('');
    setSeverity('');
    setStatus('');
    setFrom('');
    setTo('');
    setResults([]);
    setSearched(false);
    setError(null);
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'Agent':
        return <Cpu size={16} style={{ color: '#10b981' }} />;
      case 'SecurityEvent':
        return <Terminal size={16} style={{ color: '#3b82f6' }} />;
      case 'Alert':
        return <ShieldAlert size={16} style={{ color: '#f97316' }} />;
      case 'Incident':
        return <Database size={16} style={{ color: '#ef4444' }} />;
      case 'AuditLog':
        return <FileSpreadsheet size={16} style={{ color: '#a855f7' }} />;
      default:
        return <Info size={16} />;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Agent': return '#10b981';
      case 'SecurityEvent': return '#3b82f6';
      case 'Alert': return '#f97316';
      case 'Incident': return '#ef4444';
      case 'AuditLog': return '#a855f7';
      default: return 'var(--text-muted)';
    }
  };

  const getSeverityColor = (sev: string) => {
    const s = sev.toLowerCase();
    if (s.includes('critical')) return '#ef4444';
    if (s.includes('high') || s.includes('warning')) return '#f97316';
    if (s.includes('medium')) return '#eab308';
    if (s.includes('low') || s.includes('info')) return '#3b82f6';
    return 'var(--text-muted)';
  };

  return (
    <div style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '24px' }}>
      
      {/* Title */}
      <div>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>Threat Hunting</h1>
        <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '4px' }}>Unified correlation and lookup portal across events, alerts, incidents, agents, and audit logs</p>
      </div>

      {/* Form Card */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px' }}>
        <form onSubmit={handleSearch} style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
          
          {/* Keyword */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Keyword</label>
            <div style={{ position: 'relative' }}>
              <input 
                type="text" 
                placeholder="Search message, description, title..." 
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                style={{ width: '100%', padding: '10px 10px 10px 36px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
              />
              <Search size={16} style={{ position: 'absolute', left: '12px', top: '12px', color: 'var(--text-muted)' }} />
            </div>
          </div>

          {/* Hostname */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Hostname</label>
            <input 
              type="text" 
              placeholder="e.g. win-hosp-a" 
              value={hostname}
              onChange={(e) => setHostname(e.target.value)}
              style={{ padding: '10px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
            />
          </div>

          {/* IP Address */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>IP Address</label>
            <input 
              type="text" 
              placeholder="e.g. 192.168.1.35" 
              value={ip}
              onChange={(e) => setIp(e.target.value)}
              style={{ padding: '10px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
            />
          </div>

          {/* Username */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Username</label>
            <div style={{ position: 'relative' }}>
              <input 
                type="text" 
                placeholder="e.g. admin" 
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                style={{ width: '100%', padding: '10px 10px 10px 36px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
              />
              <User size={16} style={{ position: 'absolute', left: '12px', top: '12px', color: 'var(--text-muted)' }} />
            </div>
          </div>

          {/* Severity */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Severity</label>
            <select 
              value={severity} 
              onChange={(e) => setSeverity(e.target.value)}
              style={{ padding: '10px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
            >
              <option value="">Any Severity</option>
              <option value="Critical">Critical</option>
              <option value="High">High / Warning</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
              <option value="Information">Information</option>
            </select>
          </div>

          {/* Status */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Status</label>
            <select 
              value={status} 
              onChange={(e) => setStatus(e.target.value)}
              style={{ padding: '10px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
            >
              <option value="">Any Status</option>
              <option value="New">New</option>
              <option value="Assigned">Assigned</option>
              <option value="Investigating">Investigating</option>
              <option value="Resolved">Resolved</option>
              <option value="Closed">Closed</option>
              <option value="online">Online (Agent)</option>
              <option value="offline">Offline (Agent)</option>
            </select>
          </div>

          {/* From Time */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>From Date</label>
            <input 
              type="datetime-local" 
              value={from} 
              onChange={(e) => setFrom(e.target.value)}
              style={{ padding: '10px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
            />
          </div>

          {/* To Time */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>To Date</label>
            <input 
              type="datetime-local" 
              value={to} 
              onChange={(e) => setTo(e.target.value)}
              style={{ padding: '10px', background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px', color: 'var(--text-primary)', fontSize: '0.875rem' }}
            />
          </div>

          {/* Form Actions */}
          <div style={{ gridColumn: '1 / -1', display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '8px' }}>
            <button 
              type="button" 
              onClick={handleClear}
              style={{ padding: '10px 20px', background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)', borderRadius: '6px', color: 'var(--text-secondary)', cursor: 'pointer', fontSize: '0.875rem' }}
            >
              Clear Filters
            </button>
            <button 
              type="submit" 
              disabled={loading}
              style={{ 
                display: 'flex', 
                alignItems: 'center', 
                gap: '8px', 
                padding: '10px 24px', 
                background: 'var(--text-danger)', 
                border: 'none', 
                borderRadius: '6px', 
                color: '#fff', 
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: '0.875rem',
                opacity: loading ? 0.7 : 1
              }}
            >
              {loading ? <Loader2 size={16} style={{ animation: 'spin 1.5s linear infinite' }} /> : <Search size={16} />}
              Search Threats
            </button>
          </div>

        </form>
      </div>

      {/* Results Section */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px' }}>
        <h3 style={{ margin: '0 0 16px 0', fontSize: '1rem', fontWeight: 600, color: 'var(--text-primary)', display: 'flex', alignItems: 'center', gap: '8px' }}>
          Search Results 
          {searched && (
            <span style={{ fontSize: '0.75rem', background: 'rgba(255,255,255,0.05)', padding: '2px 8px', borderRadius: '12px', color: 'var(--text-secondary)' }}>
              {results.length} records found
            </span>
          )}
        </h3>

        {error && (
          <div style={{ padding: '12px', background: 'rgba(239, 68, 68, 0.08)', border: '1px solid rgba(239, 68, 68, 0.2)', borderRadius: '6px', color: 'var(--text-danger)', fontSize: '0.875rem', marginBottom: '16px' }}>
            {error}
          </div>
        )}

        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                <th style={{ padding: '12px 8px' }}>Type</th>
                <th style={{ padding: '12px 8px' }}>Timestamp</th>
                <th style={{ padding: '12px 8px' }}>Title & Description</th>
                <th style={{ padding: '12px 8px' }}>Context Details</th>
                <th style={{ padding: '12px 8px' }}>Severity</th>
                <th style={{ padding: '12px 8px' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {results.length > 0 ? (
                results.map((item) => (
                  <tr key={`${item.type}-${item.id}`} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)', fontSize: '0.875rem' }}>
                    
                    {/* Type Badge */}
                    <td style={{ padding: '16px 8px', whiteSpace: 'nowrap' }}>
                      <span style={{ 
                        display: 'inline-flex', 
                        alignItems: 'center', 
                        gap: '6px', 
                        fontSize: '0.75rem', 
                        padding: '4px 10px', 
                        borderRadius: '6px',
                        background: `${getTypeColor(item.type)}12`,
                        color: getTypeColor(item.type),
                        fontWeight: 600
                      }}>
                        {getTypeIcon(item.type)}
                        {item.type}
                      </span>
                    </td>

                    {/* Timestamp */}
                    <td style={{ padding: '16px 8px', whiteSpace: 'nowrap', color: 'var(--text-secondary)' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <Calendar size={12} />
                        {new Date(item.timestamp).toLocaleString()}
                      </div>
                    </td>

                    {/* Title & Description */}
                    <td style={{ padding: '16px 8px', maxWidth: '350px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)', marginBottom: '4px' }}>{item.title}</div>
                      <div style={{ color: 'var(--text-secondary)', fontSize: '0.8125rem', overflow: 'hidden', textOverflow: 'ellipsis', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
                        {item.description}
                      </div>
                    </td>

                    {/* Host/IP/User Context */}
                    <td style={{ padding: '16px 8px', color: 'var(--text-secondary)', fontSize: '0.8125rem' }}>
                      {item.hostname && <div><strong>Host:</strong> {item.hostname}</div>}
                      {item.ipAddress && <div><strong>IP:</strong> {item.ipAddress}</div>}
                      {item.username && <div><strong>User:</strong> {item.username}</div>}
                      {!item.hostname && !item.ipAddress && !item.username && <span style={{ color: 'var(--text-muted)' }}>-</span>}
                    </td>

                    {/* Severity */}
                    <td style={{ padding: '16px 8px' }}>
                      {item.severity ? (
                        <span style={{ 
                          fontSize: '0.72rem', 
                          padding: '2px 8px', 
                          borderRadius: '12px', 
                          fontWeight: 600,
                          background: `${getSeverityColor(item.severity)}15`,
                          color: getSeverityColor(item.severity)
                        }}>
                          {item.severity}
                        </span>
                      ) : <span style={{ color: 'var(--text-muted)' }}>-</span>}
                    </td>

                    {/* Status */}
                    <td style={{ padding: '16px 8px', whiteSpace: 'nowrap' }}>
                      {item.status ? (
                        <span style={{ 
                          fontSize: '0.72rem', 
                          padding: '2px 8px', 
                          borderRadius: '4px',
                          background: 'rgba(255,255,255,0.03)',
                          border: '1px solid rgba(255,255,255,0.05)',
                          color: 'var(--text-primary)'
                        }}>
                          {item.status}
                        </span>
                      ) : <span style={{ color: 'var(--text-muted)' }}>-</span>}
                    </td>

                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6} style={{ padding: '40px 8px', textAlign: 'center', color: 'var(--text-muted)' }}>
                    {loading ? (
                      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '8px' }}>
                        <Loader2 size={16} style={{ animation: 'spin 1.5s linear infinite' }} />
                        Scanning all tables for threats...
                      </div>
                    ) : searched ? (
                      'No threats found matching the search criteria'
                    ) : (
                      'Enter query criteria above and click Search'
                    )}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

    </div>
  );
};
