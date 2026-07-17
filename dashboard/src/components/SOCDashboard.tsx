import React, { useEffect, useState, useContext } from 'react';
import { 
  ResponsiveContainer, 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  Tooltip, 
  BarChart, 
  Bar, 
  Cell, 
  PieChart, 
  Pie, 
  Legend 
} from 'recharts';
import { 
  ShieldAlert, 
  Octagon, 
  Server, 
  Activity, 
  Users, 
  RefreshCw, 
  Clock, 
  FileText 
} from 'lucide-react';
import { NotificationContext } from '../contexts/NotificationContext';

interface TrendItem {
  date: string;
  count: number;
}

interface KeyValueItem {
  key: string;
  value: number;
}

interface RecentActivity {
  timestamp: string;
  title: string;
  message: string;
  severity: string;
}

interface DashboardOverview {
  openIncidents: number;
  criticalIncidents: number;
  onlineAgents: number;
  offlineAgents: number;
  alertsToday: number;
  resolvedToday: number;
  assignedToMe: number;
  alertTrend: TrendItem[];
  incidentTrend: TrendItem[];
  alertSeverityDistribution: KeyValueItem[];
  topAlertRules: KeyValueItem[];
  topAffectedHosts: KeyValueItem[];
  recentActivities: RecentActivity[];
}

export const SOCDashboard: React.FC = () => {
  const [data, setData] = useState<DashboardOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const context = useContext(NotificationContext);

  const fetchOverview = async () => {
    try {
      setLoading(true);
      const token = localStorage.getItem('token');
      const res = await fetch('/api/v1/dashboard/overview', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!res.ok) {
        throw new Error(`Failed to fetch overview: ${res.status}`);
      }
      const overviewData = await res.json();
      setData(overviewData);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Error loading dashboard statistics.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOverview();
    
    // Set up auto-refresh timer every 30s as fallback, though SignalR updates it in realtime
    const interval = setInterval(fetchOverview, 30000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (context?.state.dashboardOverview) {
      const live = context.state.dashboardOverview;
      setData(prev => {
        if (!prev) return null;
        return {
          ...prev,
          onlineAgents: live.onlineAgents ?? prev.onlineAgents,
          offlineAgents: live.offlineAgents ?? prev.offlineAgents,
          openIncidents: live.openIncidents ?? prev.openIncidents,
          criticalIncidents: live.criticalIncidents ?? prev.criticalIncidents,
          alertsToday: live.alertsToday ?? prev.alertsToday,
          resolvedToday: live.resolvedToday ?? prev.resolvedToday,
          assignedToMe: live.assignedToMe ?? prev.assignedToMe
        };
      });
    }
  }, [context?.state.dashboardOverview]);

  if (loading && !data) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80vh', color: 'var(--text-secondary)' }}>
        <RefreshCw style={{ animation: 'spin 1.5s linear infinite', marginRight: '8px' }} />
        <span>Loading SOC metrics...</span>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div style={{ padding: '20px', color: 'var(--text-danger)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '60vh' }}>
        <ShieldAlert size={48} style={{ marginBottom: '10px' }} />
        <h3>Failed to load SOC dashboard statistics</h3>
        <p>{error}</p>
        <button 
          onClick={fetchOverview} 
          style={{ marginTop: '15px', padding: '8px 16px', background: 'var(--bg-card)', border: '1px solid var(--border-color)', color: 'var(--text-primary)', borderRadius: '4px', cursor: 'pointer' }}
        >
          Retry
        </button>
      </div>
    );
  }

  // Color mappings
  const COLORS = {
    critical: '#ef4444',
    high: '#f97316',
    warning: '#f97316',
    medium: '#eab308',
    low: '#3b82f6',
    info: '#3b82f6',
    success: '#10b981'
  };

  // Pie Chart formatting
  const pieData = data.alertSeverityDistribution.map(x => ({
    name: x.key,
    value: x.value
  }));

  const getSeverityColor = (sev: string) => {
    const s = sev.toLowerCase();
    if (s.includes('critical')) return COLORS.critical;
    if (s.includes('high') || s.includes('warning')) return COLORS.warning;
    if (s.includes('medium')) return COLORS.medium;
    if (s.includes('low') || s.includes('info')) return COLORS.info;
    return 'var(--text-muted)';
  };

  return (
    <div style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '24px' }}>
      
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>SOC Dashboard</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '4px' }}>Real-time Security Operations Center health and metrics</p>
        </div>
        <button 
          onClick={fetchOverview}
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
          <RefreshCw size={14} /> Refresh
        </button>
      </div>

      {/* Overview Cards Row */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: '16px' }}>
        
        {/* Open Incidents */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(239, 68, 68, 0.1)', padding: '10px', borderRadius: '8px', color: COLORS.critical }}>
            <Octagon size={24} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Open Incidents</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.openIncidents}</div>
          </div>
        </div>

        {/* Critical Incidents */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(249, 115, 22, 0.1)', padding: '10px', borderRadius: '8px', color: COLORS.warning }}>
            <ShieldAlert size={24} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Critical Incidents</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.criticalIncidents}</div>
          </div>
        </div>

        {/* Online Agents */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(16, 185, 129, 0.1)', padding: '10px', borderRadius: '8px', color: COLORS.success }}>
            <Server size={24} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Online Agents</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.onlineAgents}</div>
          </div>
        </div>

        {/* Offline Agents */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(255, 255, 255, 0.05)', padding: '10px', borderRadius: '8px', color: 'var(--text-muted)' }}>
            <Server size={24} style={{ opacity: 0.5 }} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Offline Agents</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.offlineAgents}</div>
          </div>
        </div>

        {/* Alerts Today */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(59, 130, 246, 0.1)', padding: '10px', borderRadius: '8px', color: COLORS.info }}>
            <Activity size={24} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Alerts Today</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.alertsToday}</div>
          </div>
        </div>

        {/* Resolved Today */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(16, 185, 129, 0.1)', padding: '10px', borderRadius: '8px', color: COLORS.success }}>
            <FileText size={24} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Resolved Today</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.resolvedToday}</div>
          </div>
        </div>

        {/* Assigned to Me */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '16px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ background: 'rgba(168, 85, 247, 0.1)', padding: '10px', borderRadius: '8px', color: '#a855f7' }}>
            <Users size={24} />
          </div>
          <div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Assigned to Me</div>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-primary)' }}>{data.assignedToMe}</div>
          </div>
        </div>

      </div>

      {/* Charts section */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(450px, 1fr))', gap: '24px' }}>
        
        {/* Trend chart */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px' }}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: '1rem', fontWeight: 600, color: 'var(--text-primary)' }}>Security Trend (7 Days)</h3>
          <div style={{ height: '300px' }}>
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={data.alertTrend}>
                <XAxis dataKey="date" stroke="var(--text-muted)" style={{ fontSize: '0.75rem' }} />
                <YAxis stroke="var(--text-muted)" style={{ fontSize: '0.75rem' }} />
                <Tooltip 
                  contentStyle={{ background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px' }}
                  labelStyle={{ color: 'var(--text-primary)' }}
                />
                <Legend style={{ fontSize: '0.75rem' }} />
                <Line type="monotone" name="Alerts" dataKey="count" stroke={COLORS.info} strokeWidth={2} dot={{ r: 4 }} activeDot={{ r: 6 }} />
                {/* Overlay incidents in same/scaled trend */}
                <Line type="monotone" name="Incidents" data={data.incidentTrend} dataKey="count" stroke={COLORS.critical} strokeWidth={2} dot={{ r: 4 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Severity distribution */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px', display: 'flex', flexDirection: 'column' }}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: '1rem', fontWeight: 600, color: 'var(--text-primary)' }}>Alert Severity Distribution</h3>
          <div style={{ height: '300px', display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
            {pieData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={pieData}
                    cx="50%"
                    cy="45%"
                    innerRadius={60}
                    outerRadius={90}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {pieData.map((entry, index) => {
                      const color = getSeverityColor(entry.name);
                      return <Cell key={`cell-${index}`} fill={color} />;
                    })}
                  </Pie>
                  <Tooltip 
                    contentStyle={{ background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px' }}
                    labelStyle={{ color: 'var(--text-primary)' }}
                  />
                  <Legend verticalAlign="bottom" height={36} />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div style={{ color: 'var(--text-muted)', fontSize: '0.875rem' }}>No alert data available</div>
            )}
          </div>
        </div>

      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(450px, 1fr))', gap: '24px' }}>
        
        {/* Top Alert Rules */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px' }}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: '1rem', fontWeight: 600, color: 'var(--text-primary)' }}>Top Alert Rules</h3>
          <div style={{ height: '260px' }}>
            {data.topAlertRules.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data.topAlertRules} layout="vertical" margin={{ left: 30, right: 10 }}>
                  <XAxis type="number" stroke="var(--text-muted)" style={{ fontSize: '0.75rem' }} />
                  <YAxis dataKey="key" type="category" stroke="var(--text-muted)" style={{ fontSize: '0.75rem' }} width={120} />
                  <Tooltip 
                    contentStyle={{ background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px' }}
                    labelStyle={{ color: 'var(--text-primary)' }}
                  />
                  <Bar dataKey="value" name="Alert Count" fill={COLORS.info} radius={[0, 4, 4, 0]}>
                    {data.topAlertRules.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={index === 0 ? COLORS.critical : index === 1 ? COLORS.warning : COLORS.info} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%', color: 'var(--text-muted)' }}>No rule statistics</div>
            )}
          </div>
        </div>

        {/* Top Affected Hosts */}
        <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px' }}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: '1rem', fontWeight: 600, color: 'var(--text-primary)' }}>Top Affected Hosts</h3>
          <div style={{ height: '260px' }}>
            {data.topAffectedHosts.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data.topAffectedHosts} margin={{ bottom: 20 }}>
                  <XAxis dataKey="key" stroke="var(--text-muted)" style={{ fontSize: '0.75rem' }} />
                  <YAxis stroke="var(--text-muted)" style={{ fontSize: '0.75rem' }} />
                  <Tooltip 
                    contentStyle={{ background: 'var(--bg-app)', border: '1px solid var(--border-color)', borderRadius: '6px' }}
                    labelStyle={{ color: 'var(--text-primary)' }}
                  />
                  <Bar dataKey="value" name="Alert Count" fill={COLORS.warning} radius={[4, 4, 0, 0]}>
                    {data.topAffectedHosts.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={index === 0 ? COLORS.critical : COLORS.warning} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%', color: 'var(--text-muted)' }}>No host statistics</div>
            )}
          </div>
        </div>

      </div>

      {/* Recent Security Activities */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '20px' }}>
        <h3 style={{ margin: '0 0 16px 0', fontSize: '1rem', fontWeight: 600, color: 'var(--text-primary)' }}>Recent Security Activities</h3>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
                <th style={{ padding: '12px 8px' }}>Timestamp</th>
                <th style={{ padding: '12px 8px' }}>Action</th>
                <th style={{ padding: '12px 8px' }}>Details</th>
                <th style={{ padding: '12px 8px' }}>Severity</th>
              </tr>
            </thead>
            <tbody>
              {data.recentActivities.length > 0 ? (
                data.recentActivities.map((act, idx) => (
                  <tr key={idx} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)', fontSize: '0.875rem' }}>
                    <td style={{ padding: '12px 8px', whiteSpace: 'nowrap', display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)' }}>
                      <Clock size={12} /> {new Date(act.timestamp).toLocaleString()}
                    </td>
                    <td style={{ padding: '12px 8px', fontWeight: 500, color: 'var(--text-primary)' }}>{act.title}</td>
                    <td style={{ padding: '12px 8px', color: 'var(--text-secondary)' }}>{act.message}</td>
                    <td style={{ padding: '12px 8px' }}>
                      <span style={{ 
                        fontSize: '0.75rem', 
                        padding: '2px 8px', 
                        borderRadius: '12px', 
                        fontWeight: 600,
                        background: `${getSeverityColor(act.severity)}15`, 
                        color: getSeverityColor(act.severity) 
                      }}>
                        {act.severity}
                      </span>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={4} style={{ padding: '20px', textAlign: 'center', color: 'var(--text-muted)' }}>No recent activities detected</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

    </div>
  );
};
