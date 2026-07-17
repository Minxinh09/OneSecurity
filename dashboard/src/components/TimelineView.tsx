import React, { useEffect, useState } from 'react';
import { 
  Clock, 
  Terminal, 
  ShieldAlert, 
  User, 
  Activity, 
  RefreshCw, 
  AlertOctagon, 
  Filter,
  CheckCircle,
  HelpCircle
} from 'lucide-react';

interface TimelineItem {
  id: string;
  type: string; // "SecurityEvent", "Alert", "Audit"
  title: string;
  description: string;
  timestamp: string;
  userName: string | null;
  severity: string;
}

export const TimelineView: React.FC = () => {
  const [items, setItems] = useState<TimelineItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterType, setFilterType] = useState<string>('all');

  const fetchTimeline = async () => {
    try {
      setLoading(true);
      const token = localStorage.getItem('token');
      const res = await fetch('/api/v1/dashboard/timeline', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!res.ok) {
        throw new Error(`Failed to fetch timeline: ${res.status}`);
      }
      const data = await res.json();
      setItems(data || []);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Error occurred while loading unified timeline.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTimeline();
    
    // Auto-refresh every 20s
    const interval = setInterval(fetchTimeline, 20000);
    return () => clearInterval(interval);
  }, []);

  const getIcon = (type: string, title: string) => {
    const t = title.toLowerCase();
    if (type === 'SecurityEvent') {
      return <Terminal size={16} />;
    }
    if (type === 'Alert') {
      return <ShieldAlert size={16} />;
    }
    // Audit actions
    if (t.includes('create')) return <Activity size={16} />;
    if (t.includes('assign')) return <User size={16} />;
    if (t.includes('resolve') || t.includes('close')) return <CheckCircle size={16} />;
    return <Clock size={16} />;
  };

  const getBorderColor = (type: string) => {
    switch (type) {
      case 'SecurityEvent': return 'var(--accent-color, #3b82f6)';
      case 'Alert': return 'var(--text-warning, #f97316)';
      case 'Audit': return '#a855f7';
      default: return 'var(--border-color)';
    }
  };

  const getBgColor = (type: string) => {
    switch (type) {
      case 'SecurityEvent': return 'rgba(59, 130, 246, 0.1)';
      case 'Alert': return 'rgba(249, 115, 22, 0.1)';
      case 'Audit': return 'rgba(168, 85, 247, 0.1)';
      default: return 'rgba(255, 255, 255, 0.02)';
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

  const filteredItems = items.filter(item => {
    if (filterType === 'all') return true;
    return item.type === filterType;
  });

  return (
    <div style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '24px', maxWidth: '900px', margin: '0 auto' }}>
      
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>Unified Activity Timeline</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '4px' }}>Chronological feed correlating raw security events, fired alerts, and SOC analyst activities</p>
        </div>
        <button 
          onClick={fetchTimeline}
          style={{ 
            display: 'flex', 
            alignItems: 'center', 
            gap: '8px', 
            background: 'rgba(255,255,255,0.03)', 
            border: '1px solid rgba(255,255,255,0.08)', 
            padding: '8px 16px', 
            borderRadius: '6px', 
            color: 'var(--text-primary)',
            cursor: 'pointer'
          }}
        >
          <RefreshCw size={14} style={{ animation: loading ? 'spin 1.5s linear infinite' : 'none' }} /> Refresh
        </button>
      </div>

      {/* Filter Row */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'var(--bg-card)', padding: '12px 16px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
          <Filter size={16} /> Filter by type:
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          {['all', 'SecurityEvent', 'Alert', 'Audit'].map((t) => (
            <button
              key={t}
              onClick={() => setFilterType(t)}
              style={{
                padding: '6px 12px',
                borderRadius: '4px',
                fontSize: '0.75rem',
                fontWeight: 600,
                border: '1px solid',
                cursor: 'pointer',
                background: filterType === t ? 'var(--text-danger)' : 'rgba(255,255,255,0.02)',
                borderColor: filterType === t ? 'var(--text-danger)' : 'var(--border-color)',
                color: filterType === t ? '#fff' : 'var(--text-secondary)',
                transition: 'all 0.2s'
              }}
            >
              {t === 'all' ? 'All Activities' : t === 'SecurityEvent' ? 'Events' : t === 'Alert' ? 'Alerts' : 'SOC Activities'}
            </button>
          ))}
        </div>
      </div>

      {/* Timeline List */}
      {error && (
        <div style={{ padding: '12px', background: 'rgba(239, 68, 68, 0.08)', border: '1px solid rgba(239, 68, 68, 0.2)', borderRadius: '6px', color: 'var(--text-danger)', fontSize: '0.875rem' }}>
          {error}
        </div>
      )}

      {loading && items.length === 0 ? (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '60px 0', color: 'var(--text-secondary)' }}>
          <RefreshCw size={24} style={{ animation: 'spin 1.5s linear infinite', marginRight: '10px' }} />
          Retrieving unified feed...
        </div>
      ) : filteredItems.length > 0 ? (
        <div style={{ position: 'relative', paddingLeft: '32px', margin: '8px 0' }}>
          
          {/* Vertical Connecting Line */}
          <div style={{ 
            position: 'absolute', 
            left: '11px', 
            top: '24px', 
            bottom: '24px', 
            width: '2px', 
            background: 'rgba(255,255,255,0.05)' 
          }} />

          {/* Timeline Nodes */}
          {filteredItems.map((item, idx) => {
            const borderColor = getBorderColor(item.type);
            const iconBg = getBgColor(item.type);
            const isFirst = idx === 0;

            return (
              <div key={`${item.type}-${item.id}`} style={{ position: 'relative', marginBottom: '24px' }}>
                
                {/* Glowing Dot Icon Node */}
                <div style={{ 
                  position: 'absolute', 
                  left: '-32px', 
                  top: '4px', 
                  width: '24px', 
                  height: '24px', 
                  borderRadius: '50%', 
                  background: 'var(--bg-app)', 
                  border: `2px solid ${borderColor}`,
                  display: 'flex', 
                  alignItems: 'center', 
                  justifyContent: 'center', 
                  color: borderColor,
                  boxShadow: isFirst ? `0 0 10px ${borderColor}40` : 'none',
                  zIndex: 2
                }}>
                  {getIcon(item.type, item.title)}
                </div>

                {/* Event Card */}
                <div style={{ 
                  background: 'var(--bg-card)', 
                  border: '1px solid var(--border-color)', 
                  borderRadius: '12px', 
                  padding: '16px',
                  boxShadow: isFirst ? '0 4px 12px rgba(0,0,0,0.1)' : 'none',
                  borderLeft: `4px solid ${borderColor}`
                }}>
                  
                  {/* Top Row: Type & Date & Severity */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '8px', marginBottom: '8px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{ fontSize: '0.675rem', fontWeight: 700, textTransform: 'uppercase', color: borderColor, letterSpacing: '0.05em' }}>
                        {item.type === 'SecurityEvent' ? 'Security Event' : item.type === 'Alert' ? 'Alert Fired' : 'SOC Action'}
                      </span>
                      {item.userName && (
                        <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                          • <User size={12} /> {item.userName}
                        </span>
                      )}
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      {item.severity && (
                        <span style={{ 
                          fontSize: '0.675rem', 
                          padding: '1px 6px', 
                          borderRadius: '8px', 
                          fontWeight: 600,
                          background: `${getSeverityColor(item.severity)}12`, 
                          color: getSeverityColor(item.severity) 
                        }}>
                          {item.severity}
                        </span>
                      )}
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                        {new Date(item.timestamp).toLocaleString()}
                      </span>
                    </div>
                  </div>

                  {/* Body Content */}
                  <div style={{ fontWeight: 600, fontSize: '0.925rem', color: 'var(--text-primary)', marginBottom: '4px' }}>
                    {item.title}
                  </div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', lineHeight: 1.4 }}>
                    {item.description}
                  </div>

                </div>

              </div>
            );
          })}

        </div>
      ) : (
        <div style={{ padding: '60px 0', textAlign: 'center', background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', color: 'var(--text-muted)' }}>
          <AlertOctagon size={36} style={{ marginBottom: '10px', color: 'var(--text-muted)' }} />
          <div>No timeline events found matching the selected filter</div>
        </div>
      )}

    </div>
  );
};
