import React, { useState, useEffect } from 'react';
import { NotificationProvider } from './contexts/NotificationContext';
import { useNotifications } from './hooks/useNotifications';
import { SOCDashboard } from './components/SOCDashboard';
import { ThreatHunting } from './components/ThreatHunting';
import { TimelineView } from './components/TimelineView';
import { RulesManagement } from './components/RulesManagement';
import { ResponseCenter } from './components/ResponseCenter';
import { Infrastructure } from './components/Infrastructure';
import {
  Shield, Server, AlertTriangle, List, Settings, Check, 
  Activity, ArrowRight, RefreshCw, Cpu, Database, HardDrive, 
  Clock, CheckCircle, Terminal, Search, Filter, AlertOctagon,
  Lock, User as UserIcon, LogOut, Building, Sliders, Zap
} from 'lucide-react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, AreaChart, Area } from 'recharts';

// Define Interfaces
interface Hospital {
  id: number;
  name: string;
  code: string;
}

interface MonitoredServer {
  id: number;
  agentId: string;
  hostname: string;
  osType: string;
  osVersion: string;
  ipAddress: string;
  registeredAt: string;
  lastHeartbeat: string;
  status: 'online' | 'offline' | 'warning';
  cpuPercent: number;
  ramPercent: number;
  diskPercent: number;
  uptime: number;
  hospitalId: number;
  hospital?: Hospital;
}

interface SecurityEvent {
  id: number;
  eventId: string;
  serverId: number;
  serverHostname: string;
  hospitalName?: string;
  server?: MonitoredServer;
  timestamp: string;
  category: string;
  severity: 'critical' | 'warning' | 'info';
  source: string;
  title: string;
  details: string;
  rawData: string;
  receivedAt: string;
}

interface Alert {
  id: number;
  serverId: number;
  serverHostname: string;
  hospitalName?: string;
  server?: MonitoredServer;
  ruleName: string;
  severity: 'critical' | 'warning';
  title: string;
  message: string;
  category: string;
  createdAt: string;
  isAcknowledged: boolean;
  acknowledgedAt: string | null;
  acknowledgedBy: string | null;
  telegramSent: boolean;
}

interface OverviewData {
  serverCount: number;
  onlineCount: number;
  warningCount: number;
  offlineCount: number;
  criticalAlertCount: number;
  warningAlertCount: number;
  eventCount24H: number;
  recentAlerts: Alert[];
  timeline: { time: string; events: number; alerts: number }[];
}

interface SystemConfig {
  telegramBotToken: string;
  telegramChatId: string;
  apiKey: string;
  bruteForceThreshold: number;
  bruteForceWindowMinutes: number;
  agentOfflineTimeoutSeconds: number;
}

interface Hospital {
  id: number;
  name: string;
  code: string;
}

interface UserSession {
  username: string;
  role: string;
  hospitalId: number | null;
  hospitalName: string | null;
}

export default function App() {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'));
  const [user, setUser] = useState<UserSession | null>(() => {
    const savedUser = localStorage.getItem('user');
    return savedUser ? JSON.parse(savedUser) : null;
  });

  return (
    <NotificationProvider token={token}>
      <DashboardMain
        token={token}
        setToken={setToken}
        user={user}
        setUser={setUser}
      />
    </NotificationProvider>
  );
}

function DashboardMain({
  token,
  setToken,
  user,
  setUser
}: {
  token: string | null;
  setToken: React.Dispatch<React.SetStateAction<string | null>>;
  user: UserSession | null;
  setUser: React.Dispatch<React.SetStateAction<UserSession | null>>;
}) {
  // Consume Notification Context
  const { state, acknowledgeAlert, toast, showToast, loadInitialData: contextLoadInitialData, retryConnection } = useNotifications();

  // Map state to variables expected by legacy rendering logic
  const servers = state.servers;
  const alerts = state.alerts;
  const logsList = state.logsList;
  const overview = state.overview;
  const signalrConnected = state.connectionState.connectionStatus === 'connected';

  // Login form state
  const [loginUsername, setLoginUsername] = useState('');
  const [loginPassword, setLoginPassword] = useState('');
  const [loginError, setLoginError] = useState<string | null>(null);
  const [loginLoading, setLoginLoading] = useState(false);

  // App Navigation
  const [activeTab, setActiveTab] = useState<'overview' | 'soc-dashboard' | 'threat-hunting' | 'timeline' | 'servers' | 'infrastructure' | 'alerts' | 'logs' | 'settings' | 'incidents' | 'rules' | 'responses'>('overview');
  const [selectedServerId, setSelectedServerId] = useState<number | null>(null);
  
  // Incident State
  const [selectedIncidentId, setSelectedIncidentId] = useState<number | null>(null);
  const [incidentResponses, setIncidentResponses] = useState<any[]>([]);
  const [incidentStatusFilter, setIncidentStatusFilter] = useState<string>('');
  const [incidentSeverityFilter, setIncidentSeverityFilter] = useState<string>('');
  const [incidentAssigneeFilter, setIncidentAssigneeFilter] = useState<string>('');
  const [incidentSearchQuery, setIncidentSearchQuery] = useState<string>('');

  const fetchIncidentResponses = async (id: number) => {
    try {
      const res = await authFetch(`/api/v1/responses/incident/${id}`);
      if (res.ok) {
        const data = await res.json();
        setIncidentResponses(data);
      }
    } catch (err) {
      console.error('Failed to fetch incident responses:', err);
    }
  };

  const handleTriggerResponseAction = async (actionType: string) => {
    if (!selectedIncidentDetail) return;
    const targetAgentId = selectedIncidentDetail.alerts && selectedIncidentDetail.alerts.length > 0 
      ? selectedIncidentDetail.alerts[0].agentId 
      : null;
      
    if (!targetAgentId) {
      showToast('No agent linked to this incident to trigger actions.', 'warning');
      return;
    }

    try {
      const token = localStorage.getItem('token');
      const res = await fetch('/api/v1/responses/request', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          incidentId: selectedIncidentDetail.id,
          agentId: targetAgentId,
          actionType: actionType,
          metadata: JSON.stringify({})
        })
      });

      if (res.ok) {
        const data = await res.json();
        if (data.status === 'Pending') {
          showToast(`Response action ${actionType} requested. Awaiting administrator approval.`, 'warning');
        } else {
          showToast(`Response action ${actionType} successfully queued.`, 'success');
        }
        await fetchIncidentResponses(selectedIncidentDetail.id);
      } else {
        const errorData = await res.json();
        showToast(`Request failed: ${errorData.message || 'Error'}`, 'critical');
      }
    } catch (err) {
      console.error(err);
      showToast('Error requesting response action.', 'critical');
    }
  };

  useEffect(() => {
    const handleUpdate = () => {
      if (selectedIncidentId) {
        fetchIncidentResponses(selectedIncidentId);
      }
    };
    window.addEventListener('onesecurity-response-updated', handleUpdate);
    return () => {
      window.removeEventListener('onesecurity-response-updated', handleUpdate);
    };
  }, [selectedIncidentId]);

  const [onlyMyIncidents, setOnlyMyIncidents] = useState<boolean>(false);
  const [usersList, setUsersList] = useState<any[]>([]);

  // Detailed view states & modals
  const [selectedIncidentDetail, setSelectedIncidentDetail] = useState<any | null>(null);
  const [incidentTimeline, setIncidentTimeline] = useState<any[]>([]);
  const [showCreateIncidentModal, setShowCreateIncidentModal] = useState(false);
  const [newIncidentTitle, setNewIncidentTitle] = useState('');
  const [newIncidentDescription, setNewIncidentDescription] = useState('');
  const [newIncidentSelectedAlerts, setNewIncidentSelectedAlerts] = useState<number[]>([]);
  const [showLinkAlertsModal, setShowLinkAlertsModal] = useState(false);
  const [linkAlertsSelected, setLinkAlertsSelected] = useState<number[]>([]);
  
  // Multitenancy State
  const [hospitals, setHospitals] = useState<Hospital[]>([]);
  const [debugMsg, setDebugMsg] = useState<string>('');
  const [selectedHospitalId, setSelectedHospitalId] = useState<string>(() => localStorage.getItem('selectedHospitalId') || '');

  const [config, setConfig] = useState<SystemConfig | null>(null);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [adminName, setAdminName] = useState<string>(user?.username || 'Admin');
  
  // Filters & Pagination with localStorage preservation
  const [alertSeverityFilter, setAlertSeverityFilter] = useState<string>(() => localStorage.getItem('alertSeverityFilter') || '');
  const [alertAckFilter, setAlertAckFilter] = useState<string>(() => localStorage.getItem('alertAckFilter') || 'false');
  const [logSeverityFilter, setLogSeverityFilter] = useState<string>(() => localStorage.getItem('logSeverityFilter') || '');
  const [logCategoryFilter, setLogCategoryFilter] = useState<string>(() => localStorage.getItem('logCategoryFilter') || '');
  const [logSearchQuery, setLogSearchQuery] = useState<string>(() => localStorage.getItem('logSearchQuery') || '');
  const [logPage, setLogPage] = useState<number>(1);
  const [totalLogs, setTotalLogs] = useState<number>(0);

  // Sync filters to localStorage
  useEffect(() => {
    localStorage.setItem('alertSeverityFilter', alertSeverityFilter);
  }, [alertSeverityFilter]);

  useEffect(() => {
    localStorage.setItem('alertAckFilter', alertAckFilter);
  }, [alertAckFilter]);

  useEffect(() => {
    localStorage.setItem('logSeverityFilter', logSeverityFilter);
    localStorage.setItem('logCategoryFilter', logCategoryFilter);
    localStorage.setItem('logSearchQuery', logSearchQuery);
  }, [logSeverityFilter, logCategoryFilter, logSearchQuery]);

  useEffect(() => {
    localStorage.setItem('selectedHospitalId', selectedHospitalId);
  }, [selectedHospitalId]);

  // Auth fetch wrapper
  const authFetch = async (url: string, options: RequestInit = {}) => {
    if (!token) return new Response(null, { status: 401 });
    
    const headers = new Headers(options.headers || {});
    headers.set('Authorization', `Bearer ${token}`);
    
    // ======= BỔ SUNG: Gửi ID bệnh viện giả lập lên Server qua Header =======
    const selectedHospitalId = localStorage.getItem('selectedHospitalId') || '';
    if (selectedHospitalId) {
      headers.set('X-Hospital-Id', selectedHospitalId); //
    }
    // =====================================================================
    
    const res = await fetch(url, { ...options, headers });
    
    if (res.status === 401) {
      handleLogout();
    }
    return res;
  };
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoginError(null);
    setLoginLoading(true);

    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: loginUsername, password: loginPassword })
      });

      if (res.ok) {
        const data = await res.json();
        localStorage.setItem('token', data.token);
        localStorage.setItem('user', JSON.stringify({
          username: data.username,
          role: data.role,
          hospitalId: data.hospitalId,
          hospitalName: data.hospitalName
        }));
        
        setToken(data.token);
        setUser({
          username: data.username,
          role: data.role,
          hospitalId: data.hospitalId,
          hospitalName: data.hospitalName
        });
        setAdminName(data.username);
        showToast(`Welcome back, ${data.username}!`, 'success');
      } else {
        const err = await res.json();
        setLoginError(err.message || 'Invalid username or password');
      }
    } catch (err) {
      console.error(err);
      setLoginError('Could not connect to authentication service.');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('selectedHospitalId');
    setToken(null);
    setUser(null);
    setHospitals([]);
    setSelectedHospitalId('');
    setSelectedServerId(null);
  };

  // Fetch initial data
  const loadInitialData = async () => {
    if (!token) {
      setDebugMsg("No token");
      return;
    }
    setRefreshing(true);
    setDebugMsg("Start load");
    try {
      await contextLoadInitialData();
      setDebugMsg(prev => prev + " | Context OK");
      
      if (user && (user.role === 'Administrator' || user.role === 'Operator' || user.role === 'SecurityOperator')) {
        setDebugMsg(prev => prev + " | Check user role");
        try {
          const usersRes = await authFetch('/api/v1/users');
          setDebugMsg(prev => prev + ` | Users: ${usersRes.status}`);
          if (usersRes.ok) {
            const usersData = await usersRes.json();
            setUsersList(usersData);
          }
        } catch (uErr: any) {
          setDebugMsg(prev => prev + ` | Users Err: ${uErr.message}`);
        }
      }

      // ======= BỔ SUNG: Tải danh sách bệnh viện cho Dropdown nếu là Admin =======
      if (user && (user.role === 'SuperAdmin' || user.role === 'Administrator')) {
        setDebugMsg(prev => prev + " | Fetching hosp");
        try {
          const hospRes = await authFetch('/api/v1/hospitals'); //
          setDebugMsg(prev => prev + ` | Hosp: ${hospRes.status}`);
          if (hospRes.ok) {
            const hospData = await hospRes.json();
            setDebugMsg(prev => prev + ` | Hosp count: ${hospData.length}`);
            setHospitals(hospData); // Gán vào state hospitals có sẵn
          }
        } catch (hErr: any) {
          setDebugMsg(prev => prev + ` | Hosp Err: ${hErr.message}`);
        }
      } else {
        setDebugMsg(prev => prev + ` | Skip hosp (user role: ${user?.role || 'null'})`);
      }

      setConfig({
        telegramBotToken: "placeholder_token",
        telegramChatId: "placeholder_chat_id",
        apiKey: "placeholder_key",
        bruteForceThreshold: 5,
        bruteForceWindowMinutes: 1,
        agentOfflineTimeoutSeconds: 30
      });
    } catch (err: any) {
      setDebugMsg(prev => prev + ` | Gen Err: ${err.message}`);
      console.error('Failed to load dashboard data:', err);
    } finally {
      setRefreshing(false);
    }
  };

  useEffect(() => {
    if (token) {
      loadInitialData();
    }
  }, [token, selectedHospitalId]);

  const handleAcknowledgeAlert = async (alertId: number) => {
    await acknowledgeAlert(alertId, adminName);
  };

  const handleSaveConfig = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!config) return;

    try {
      const res = await authFetch('/api/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });
      
      if (res.ok) {
        showToast('Configuration updated successfully', 'success');
      } else {
        showToast('Failed to update configuration', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('API connection error', 'critical');
    }
  };

  // Render Login Panel
  const renderLogin = () => {
    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'var(--bg-gradient)'
      }}>
        <form onSubmit={handleLogin} className="glass-panel" style={{
          width: '400px',
          padding: '2.5rem',
          display: 'flex',
          flexDirection: 'column',
          gap: '1.25rem',
          boxShadow: '0 20px 50px rgba(0, 0, 0, 0.5)'
        }}>
          {/* Brand Logo */}
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
            <div style={{
              background: 'linear-gradient(135deg, var(--primary) 0%, var(--accent) 100%)',
              padding: '0.75rem',
              borderRadius: '12px',
              boxShadow: '0 0 20px rgba(99, 102, 241, 0.5)'
            }}>
              <Shield size={32} color="#ffffff" />
            </div>
            <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginTop: '0.5rem' }}>OneSecurity</h1>
            <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Medical Server Security Monitoring Console</span>
          </div>

          {loginError && (
            <div style={{
              background: 'rgba(244, 63, 94, 0.15)',
              border: '1px solid var(--danger)',
              color: 'var(--danger)',
              padding: '0.75rem',
              borderRadius: '6px',
              fontSize: '0.8rem',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem'
            }}>
              <AlertOctagon size={16} />
              <span>{loginError}</span>
            </div>
          )}

          {/* Username Input */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Username</label>
            <div style={{ position: 'relative' }}>
              <UserIcon size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
              <input
                type="text"
                placeholder="Enter username"
                value={loginUsername}
                onChange={e => setLoginUsername(e.target.value)}
                required
                style={{
                  width: '100%',
                  background: '#111827',
                  border: '1px solid var(--panel-border)',
                  borderRadius: '6px',
                  padding: '10px 12px 10px 38px',
                  fontSize: '0.875rem',
                  color: '#fff',
                  outline: 'none'
                }}
              />
            </div>
          </div>

          {/* Password Input */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
            <label style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', fontWeight: 600 }}>Password</label>
            <div style={{ position: 'relative' }}>
              <Lock size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
              <input
                type="password"
                placeholder="Enter password"
                value={loginPassword}
                onChange={e => setLoginPassword(e.target.value)}
                required
                style={{
                  width: '100%',
                  background: '#111827',
                  border: '1px solid var(--panel-border)',
                  borderRadius: '6px',
                  padding: '10px 12px 10px 38px',
                  fontSize: '0.875rem',
                  color: '#fff',
                  outline: 'none'
                }}
              />
            </div>
          </div>

          <button type="submit" disabled={loginLoading} className="primary" style={{ width: '100%', justifyContent: 'center', marginTop: '0.5rem' }}>
            {loginLoading ? 'Authenticating...' : 'Sign In'}
          </button>
        </form>
      </div>
    );
  };

  const renderSidebar = () => {
    const navItems = [
      { id: 'overview', name: 'Overview', icon: Shield },
      { id: 'soc-dashboard', name: 'SOC Dashboard', icon: Activity },
      { id: 'threat-hunting', name: 'Threat Hunting', icon: Search },
      { id: 'timeline', name: 'Unified Timeline', icon: Clock },
      { id: 'servers', name: 'Monitored Servers', icon: Server },
      { id: 'infrastructure', name: 'Infrastructure', icon: Cpu },
      { id: 'alerts', name: 'Security Alerts', icon: AlertTriangle },
      { id: 'incidents', name: 'Incidents', icon: AlertOctagon },
      { id: 'logs', name: 'Security Events', icon: List },
      { id: 'rules', name: 'Detection Rules', icon: Sliders },
      { id: 'responses', name: 'Response Center', icon: Zap },
      { id: 'settings', name: 'System Settings', icon: Settings },
    ];

    return (
      <aside style={{
        width: '260px',
        borderRight: '1px solid var(--panel-border)',
        height: '100vh',
        position: 'fixed',
        top: 0,
        left: 0,
        background: 'rgba(11, 16, 27, 0.95)',
        display: 'flex',
        flexDirection: 'column',
        zIndex: 10
      }}>
        {/* Sidebar Logo */}
        <div style={{
          padding: '1.5rem',
          display: 'flex',
          alignItems: 'center',
          gap: '0.75rem',
          borderBottom: '1px solid var(--panel-border)'
        }}>
          <div style={{
            background: 'linear-gradient(135deg, var(--primary) 0%, var(--accent) 100%)',
            padding: '0.5rem',
            borderRadius: '10px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 0 15px rgba(99, 102, 241, 0.4)'
          }}>
            <Shield size={20} color="#ffffff" />
          </div>
          <div>
            <h1 style={{ fontSize: '1rem', fontWeight: 700, letterSpacing: '0.5px' }}>OneSecurity</h1>
            <span style={{ fontSize: '0.65rem', color: 'var(--success)', display: 'flex', alignItems: 'center', gap: '4px' }}>
              <span className="glow-dot online" style={{ width: '6px', height: '6px' }}></span>
              LAN Monitor
            </span>
          </div>
        </div>

        {/* User context info */}
        <div style={{
          padding: '1rem 1.5rem',
          borderBottom: '1px solid var(--panel-border)',
          background: 'rgba(255,255,255,0.01)',
          display: 'flex',
          flexDirection: 'column',
          gap: '0.25rem'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.8rem', fontWeight: 600 }}>
            <Building size={14} color="var(--primary)" />
            <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {user?.role === 'SuperAdmin' || user?.role === 'Administrator' ? 'Headquarters' : user?.hospitalName || 'OneSecurity Central'}
            </span>
          </div>
          <span style={{ fontSize: '0.65rem', color: 'var(--text-secondary)' }}>
            Role: {user?.role || 'Viewer'}
          </span>
        </div>

        {/* Navigation Items */}
        <nav style={{ padding: '1.5rem 1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem', flex: 1 }}>
          {navItems
            .filter(item => {
              if (user?.role === 'Viewer') {
                return !['infrastructure', 'rules', 'responses', 'settings'].includes(item.id);
              }
              return true;
            })
            .map(item => {
              const Icon = item.icon;
              const isActive = activeTab === item.id;
              return (
              <button
                key={item.id}
                onClick={() => {
                  setActiveTab(item.id as any);
                  setSelectedServerId(null);
                }}
                style={{
                  width: '100%',
                  textAlign: 'left',
                  padding: '0.75rem 1rem',
                  borderRadius: '8px',
                  border: '1px solid transparent',
                  background: isActive ? 'rgba(99, 102, 241, 0.12)' : 'transparent',
                  color: isActive ? '#ffffff' : 'var(--text-secondary)',
                  borderLeft: isActive ? '3px solid var(--primary)' : '3px solid transparent',
                  fontWeight: isActive ? 600 : 500,
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.75rem',
                  cursor: 'pointer'
                }}
              >
                <Icon size={18} color={isActive ? 'var(--primary)' : 'var(--text-secondary)'} />
                {item.name}
                {item.id === 'alerts' && overview && overview.criticalAlertCount + overview.warningAlertCount > 0 && (
                  <span style={{
                    marginLeft: 'auto',
                    background: 'var(--danger)',
                    color: '#ffffff',
                    fontSize: '0.65rem',
                    fontWeight: 700,
                    padding: '2px 6px',
                    borderRadius: '10px',
                    boxShadow: '0 0 8px var(--danger-glow)'
                  }}>
                    {overview.criticalAlertCount + overview.warningAlertCount}
                  </span>
                )}
              </button>
            );
          })}
        </nav>

        {/* Connection status footer */}
        <div style={{
          padding: '1rem',
          borderTop: '1px solid var(--panel-border)',
          fontSize: '0.75rem',
          color: 'var(--text-secondary)',
          background: 'rgba(5, 8, 15, 0.5)'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <span 
              className={`glow-dot ${
                state.connectionState.connectionStatus === 'connected' ? 'online' :
                state.connectionState.connectionStatus === 'disconnected' ? 'offline' : 'warning'
              }`} 
              onClick={retryConnection} 
              style={{ cursor: 'pointer' }} 
              title="Click to manually reconnect"
            ></span>
            <span 
              onClick={retryConnection} 
              style={{ cursor: 'pointer' }} 
              title="Click to manually reconnect"
            >
              {state.connectionState.connectionStatus === 'connected' && 'SignalR Connected'}
              {state.connectionState.connectionStatus === 'loading' && 'Connecting to Server...'}
              {state.connectionState.connectionStatus === 'reconnecting' && `Reconnecting... (${state.connectionState.reconnectAttempts})`}
              {state.connectionState.connectionStatus === 'disconnected' && 'SignalR Disconnected'}
            </span>
          </div>
          <button onClick={handleLogout} className="danger" style={{ width: '100%', justifyContent: 'center', padding: '6px', fontSize: '0.75rem' }}>
            <LogOut size={12} /> Sign Out
          </button>
        </div>
      </aside>
    );
  };

  const renderHeader = () => {
    if (activeTab === 'soc-dashboard' || activeTab === 'threat-hunting' || activeTab === 'timeline') {
      return null;
    }
    return (
      <header style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: '1.5rem',
        borderBottom: '1px solid var(--panel-border)',
        paddingBottom: '1rem'
      }}>
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 700 }}>
            {activeTab === 'overview' && 'Security Dashboard'}
            {activeTab === 'servers' && 'Monitored Servers'}
            {activeTab === 'infrastructure' && 'Infrastructure & Asset Management'}
            {activeTab === 'alerts' && 'Security Alerts'}
            {activeTab === 'incidents' && 'SOC Incidents'}
            {activeTab === 'logs' && 'Security Event Logs'}
            {activeTab === 'rules' && 'Detection Rules & Pipeline'}
            {activeTab === 'settings' && 'System Settings'}
          </h2>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem' }}>
            {activeTab === 'overview' && 'Real-time medical server security monitoring overview'}
            {activeTab === 'servers' && 'Full inventory of servers with active Go security agents'}
            {activeTab === 'infrastructure' && 'Manage assets, collectors, agent policies, and enrollment keys'}
            {activeTab === 'alerts' && 'Triggered security rules and severity status logs'}
            {activeTab === 'incidents' && 'SOC Case Management, assignment, triage, response and closure'}
            {activeTab === 'logs' && 'Searchable database logs of all events forwarded by active agents'}
            {activeTab === 'rules' && 'Configure and test custom detection policies, monitor rule execution stats and track ingestion flow'}
            {activeTab === 'settings' && 'Manage alert thresholds and outbound notification configurations'}
          </p>
        </div>

        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
          {/* Debug message */}
          {debugMsg && (
            <span style={{ fontSize: '0.75rem', color: 'orange', padding: '2px 6px', background: '#221500', borderRadius: '4px', border: '1px solid #5a3000' }}>
              {debugMsg}
            </span>
          )}

          {/* Hospital selector if SuperAdmin or Administrator */}
          {/* Đảm bảo kiểm tra đúng điều kiện user.role đã được cấu hình */}
          {(user?.role === 'SuperAdmin' || user?.role === 'Administrator') && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.8rem' }}>
              <Building size={14} color="var(--text-secondary)" />
              <select
                value={selectedHospitalId}
                onChange={e => setSelectedHospitalId(e.target.value)}
                style={{
                  background: '#111827',
                  border: '1px solid var(--panel-border)',
                  padding: '6px 12px',
                  borderRadius: '6px',
                  fontSize: '0.8rem',
                  color: '#fff',
                  outline: 'none',
                  fontWeight: 500
                }}
              >
                <option value="">All Hospitals</option>
                {hospitals.map(h => (
                  <option key={h.id} value={h.id}>{h.name}</option>
                ))}
              </select>
            </div>
          )}

          <button onClick={loadInitialData} disabled={refreshing} className="btn">
            <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />
            Refresh
          </button>
        </div>
      </header>
    );
  };

  const renderOverview = () => {
    if (!overview) return <div style={{ padding: '2rem', textAlign: 'center' }}>Loading dashboard data...</div>;

    const statsCards = [
      { title: 'Total Servers', value: overview.serverCount, desc: `${overview.onlineCount} Online`, icon: Server, color: 'var(--primary)' },
      { title: 'Unack Alerts', value: overview.criticalAlertCount + overview.warningAlertCount, desc: `${overview.criticalAlertCount} Critical`, icon: AlertTriangle, color: 'var(--danger)' },
      { title: '24h Security Events', value: overview.eventCount24H, desc: 'Events collected', icon: Activity, color: 'var(--accent)' },
      { title: 'System Status', value: overview.criticalAlertCount > 0 ? 'WARNING' : 'HEALTHY', desc: 'Real-time LAN audit', icon: Shield, color: overview.criticalAlertCount > 0 ? 'var(--warning)' : 'var(--success)' },
    ];

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {/* Stats Grid */}
        <div className="dashboard-grid">
          {statsCards.map((card, i) => {
            const Icon = card.icon;
            return (
              <div key={i} className="glass-panel" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase', fontWeight: 600 }}>{card.title}</span>
                  <h3 style={{ fontSize: '1.875rem', fontWeight: 700, margin: '0.25rem 0' }}>{card.value}</h3>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{card.desc}</span>
                </div>
                <div style={{
                  background: `rgba(255, 255, 255, 0.03)`,
                  border: `1px solid rgba(255, 255, 255, 0.05)`,
                  padding: '0.75rem',
                  borderRadius: '10px',
                  color: card.color
                }}>
                  <Icon size={24} />
                </div>
              </div>
            );
          })}
        </div>

        {/* Incident Stats Grid */}
        <div className="dashboard-grid">
          {[
            { title: 'Open Incidents', value: state.incidents.filter(i => i.status !== 'Closed' && i.status !== 'Resolved' && i.status !== 'FalsePositive').length, desc: 'Active investigations', icon: AlertOctagon, color: 'var(--warning)' },
            { title: 'Critical Incidents', value: state.incidents.filter(i => i.severity === 'Critical' && i.status !== 'Closed').length, desc: 'Critical case load', icon: AlertTriangle, color: 'var(--danger)' },
            { title: 'Resolved Today', value: state.incidents.filter(i => i.status === 'Resolved' || i.status === 'FalsePositive').length, desc: 'Closed/Resolved cases', icon: CheckCircle, color: 'var(--success)' },
            { title: 'Assigned To Me', value: state.incidents.filter(i => i.assignedUserName === adminName && i.status !== 'Closed').length, desc: 'Your assigned cases', icon: UserIcon, color: 'var(--primary)' }
          ].map((card, i) => {
            const Icon = card.icon;
            return (
              <div key={i} className="glass-panel" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', textTransform: 'uppercase', fontWeight: 600 }}>{card.title}</span>
                  <h3 style={{ fontSize: '1.875rem', fontWeight: 700, margin: '0.25rem 0' }}>{card.value}</h3>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{card.desc}</span>
                </div>
                <div style={{
                  background: `rgba(255, 255, 255, 0.03)`,
                  border: `1px solid rgba(255, 255, 255, 0.05)`,
                  padding: '0.75rem',
                  borderRadius: '10px',
                  color: card.color
                }}>
                  <Icon size={24} />
                </div>
              </div>
            );
          })}
        </div>

        {/* Main Chart Section */}
        <div className="glass-panel">
          <div style={{ marginBottom: '1rem' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600 }}>Security Incident Timeline (Hourly)</h4>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.75rem' }}>Correlated event and alert activity levels over the monitored window</p>
          </div>
          <div style={{ width: '100%', height: 260 }}>
            <ResponsiveContainer>
              <AreaChart data={overview.timeline} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="colorEvents" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="var(--primary)" stopOpacity={0.2}/>
                    <stop offset="95%" stopColor="var(--primary)" stopOpacity={0}/>
                  </linearGradient>
                  <linearGradient id="colorAlerts" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="var(--danger)" stopOpacity={0.2}/>
                    <stop offset="95%" stopColor="var(--danger)" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255, 255, 255, 0.05)" />
                <XAxis dataKey="time" stroke="var(--text-muted)" fontSize={10} />
                <YAxis stroke="var(--text-muted)" fontSize={10} />
                <Tooltip contentStyle={{ background: '#111827', border: '1px solid rgba(255,255,255,0.1)' }} />
                <Area type="monotone" dataKey="events" name="Security Events" stroke="var(--primary)" fillOpacity={1} fill="url(#colorEvents)" />
                <Area type="monotone" dataKey="alerts" name="Triggered Alerts" stroke="var(--danger)" fillOpacity={1} fill="url(#colorAlerts)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Live Grid of Monitored Servers and Alerts */}
        <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr', gap: '1.5rem' }}>
          {/* Server Grid */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h4 style={{ fontSize: '1rem', fontWeight: 600 }}>Active Nodes</h4>
              <button onClick={() => setActiveTab('servers')} style={{ padding: '4px 8px', fontSize: '0.7rem' }}>View All</button>
            </div>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', flex: 1 }}>
              {servers.length === 0 ? (
                <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                  No agents connected yet. Start an agent to register.
                </div>
              ) : (
                servers.slice(0, 4).map(server => (
                  <div key={server.id} style={{
                    padding: '0.75rem',
                    border: '1px solid var(--panel-border)',
                    borderRadius: '8px',
                    background: 'rgba(255,255,255,0.01)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    cursor: 'pointer'
                  }} onClick={() => {
                    setSelectedServerId(server.id);
                    setActiveTab('servers');
                  }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                      <span className={`glow-dot ${server.status}`}></span>
                      <div>
                        <div style={{ fontWeight: 600, fontSize: '0.875rem' }}>{server.hostname}</div>
                        <div style={{ fontSize: '0.7rem', color: 'var(--text-secondary)' }}>{server.ipAddress} | {server.osType}</div>
                      </div>
                    </div>
                    {/* CPU/RAM bars */}
                    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                      <div style={{ textAlign: 'right' }}>
                        <div style={{ fontSize: '0.65rem', color: 'var(--text-secondary)' }}>CPU</div>
                        <div style={{ fontSize: '0.75rem', fontWeight: 600, color: server.cpuPercent > 80 ? 'var(--danger)' : 'var(--text-primary)' }}>
                          {server.cpuPercent.toFixed(0)}%
                        </div>
                      </div>
                      <div style={{ textAlign: 'right' }}>
                        <div style={{ fontSize: '0.65rem', color: 'var(--text-secondary)' }}>RAM</div>
                        <div style={{ fontSize: '0.75rem', fontWeight: 600, color: server.ramPercent > 85 ? 'var(--danger)' : 'var(--text-primary)' }}>
                          {server.ramPercent.toFixed(0)}%
                        </div>
                      </div>
                      <ArrowRight size={14} color="var(--text-muted)" />
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Recent Alerts */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h4 style={{ fontSize: '1rem', fontWeight: 600 }}>Unacknowledged Alerts</h4>
              <button onClick={() => setActiveTab('alerts')} style={{ padding: '4px 8px', fontSize: '0.7rem' }}>View All</button>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', flex: 1 }}>
              {alerts.filter(a => !a.isAcknowledged).length === 0 ? (
                <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--success)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.5rem', justifyContent: 'center', flex: 1 }}>
                  <CheckCircle size={28} />
                  <span style={{ fontSize: '0.85rem' }}>No active security alerts! System clean.</span>
                </div>
              ) : (
                alerts.filter(a => !a.isAcknowledged).slice(0, 3).map(alert => (
                  <div key={alert.id} style={{
                    padding: '0.75rem',
                    border: '1px solid var(--panel-border)',
                    borderRadius: '8px',
                    background: alert.severity === 'critical' ? 'rgba(244, 63, 94, 0.04)' : 'rgba(245, 158, 11, 0.04)',
                    borderLeft: alert.severity === 'critical' ? '4px solid var(--danger)' : '4px solid var(--warning)',
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <span className={`severity-badge ${alert.severity}`}>{alert.severity}</span>
                      <span style={{ fontSize: '0.65rem', color: 'var(--text-muted)' }}>
                        {new Date(alert.createdAt).toLocaleTimeString()}
                      </span>
                    </div>
                    <div style={{ fontWeight: 600, fontSize: '0.85rem', marginTop: '0.375rem' }}>{alert.ruleName}</div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.125rem' }}>
                      {alert.serverHostname || alert.server?.hostname || "Unknown"} ({alert.hospitalName || alert.server?.hospital?.name || "Hospital"}): {alert.title}
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
                      {user?.role !== 'Viewer' && (
                        <button onClick={() => handleAcknowledgeAlert(alert.id)} className="success" style={{ padding: '2px 8px', fontSize: '0.65rem' }}>
                          <Check size={10} />
                          Ack
                        </button>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    );
  };

  const renderServers = () => {
    if (selectedServerId !== null) {
      return renderServerDetail(selectedServerId);
    }

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        <div className="glass-panel" style={{ padding: 0 }}>
          <div className="custom-table-container">
            <table className="custom-table">
              <thead>
                <tr>
                  <th>Server</th>
                  <th>Status</th>
                  <th>IP Address</th>
                  <th>OS Info</th>
                  <th>Metrics (CPU/RAM/Disk)</th>
                  <th>Uptime</th>
                  <th>Last Connection</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {servers.length === 0 ? (
                  <tr>
                    <td colSpan={8} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
                      No servers registered. Make sure your Go agents are running and pointing to the server API URL.
                    </td>
                  </tr>
                ) : (
                  servers.map(server => (
                    <tr key={server.id}>
                      <td style={{ fontWeight: 600 }}>{server.hostname}</td>
                      <td>
                        <span className={`status-badge ${server.status}`}>
                          {server.status}
                        </span>
                      </td>
                      <td>{server.ipAddress}</td>
                      <td style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                        {server.osType} ({server.osVersion})
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: '0.75rem', fontSize: '0.75rem', fontFamily: 'monospace' }}>
                          <span style={{ color: server.cpuPercent > 80 ? 'var(--danger)' : 'var(--text-secondary)' }}>
                            C:{server.cpuPercent.toFixed(0)}%
                          </span>
                          <span style={{ color: server.ramPercent > 85 ? 'var(--danger)' : 'var(--text-secondary)' }}>
                            R:{server.ramPercent.toFixed(0)}%
                          </span>
                          <span>D:{server.diskPercent.toFixed(0)}%</span>
                        </div>
                      </td>
                      <td style={{ fontSize: '0.75rem' }}>
                        {formatDuration(server.uptime)}
                      </td>
                      <td style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                        {new Date(server.lastHeartbeat).toLocaleTimeString()}
                      </td>
                      <td>
                        <button onClick={() => setSelectedServerId(server.id)} style={{ padding: '4px 10px', fontSize: '0.75rem' }}>
                          Manage
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  };

  const renderServerDetail = (id: number) => {
    const server = servers.find(s => s.id === id);
    if (!server) return <div>Server details not found.</div>;

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {/* Back and Title */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <button onClick={() => setSelectedServerId(null)} className="btn">Back</button>
          <div>
            <h2 style={{ fontSize: '1.25rem', fontWeight: 700 }}>{server.hostname}</h2>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>Agent ID: {server.agentId} | Type: {server.osType}</p>
          </div>
        </div>

        {/* Server metrics and info */}
        <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: '1.5rem' }}>
          {/* Main Info */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600, borderBottom: '1px solid var(--panel-border)', paddingBottom: '0.5rem' }}>Resource Metrics</h4>
            
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem' }}>
              <div style={{ textAlign: 'center', padding: '1rem', background: 'rgba(255,255,255,0.01)', border: '1px solid var(--panel-border)', borderRadius: '8px' }}>
                <Cpu size={24} color="var(--primary)" style={{ margin: '0 auto 0.5rem' }} />
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>CPU Usage</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, margin: '0.25rem 0' }}>{server.cpuPercent.toFixed(1)}%</div>
                <div style={{ width: '100%', height: '4px', background: 'rgba(255,255,255,0.1)', borderRadius: '2px', overflow: 'hidden' }}>
                  <div style={{ width: `${server.cpuPercent}%`, height: '100%', background: 'var(--primary)' }}></div>
                </div>
              </div>
              <div style={{ textAlign: 'center', padding: '1rem', background: 'rgba(255,255,255,0.01)', border: '1px solid var(--panel-border)', borderRadius: '8px' }}>
                <Database size={24} color="var(--accent)" style={{ margin: '0 auto 0.5rem' }} />
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>RAM Usage</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, margin: '0.25rem 0' }}>{server.ramPercent.toFixed(1)}%</div>
                <div style={{ width: '100%', height: '4px', background: 'rgba(255,255,255,0.1)', borderRadius: '2px', overflow: 'hidden' }}>
                  <div style={{ width: `${server.ramPercent}%`, height: '100%', background: 'var(--accent)' }}></div>
                </div>
              </div>
              <div style={{ textAlign: 'center', padding: '1rem', background: 'rgba(255,255,255,0.01)', border: '1px solid var(--panel-border)', borderRadius: '8px' }}>
                <HardDrive size={24} color="var(--success)" style={{ margin: '0 auto 0.5rem' }} />
                <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Disk Space</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, margin: '0.25rem 0' }}>{server.diskPercent.toFixed(1)}%</div>
                <div style={{ width: '100%', height: '4px', background: 'rgba(255,255,255,0.1)', borderRadius: '2px', overflow: 'hidden' }}>
                  <div style={{ width: `${server.diskPercent}%`, height: '100%', background: 'var(--success)' }}></div>
                </div>
              </div>
            </div>

            {/* Event trigger simulator details helper */}
            <div style={{ background: 'rgba(99, 102, 241, 0.05)', padding: '1rem', borderRadius: '8px', border: '1px solid rgba(99, 102, 241, 0.15)', fontSize: '0.8rem' }}>
              <h5 style={{ fontWeight: 600, color: 'var(--primary)', marginBottom: '0.25rem', display: 'flex', alignItems: 'center', gap: '4px' }}>
                <Terminal size={14} /> Agent Diagnostics
              </h5>
              <p style={{ color: 'var(--text-secondary)' }}>
                This node is reporting metrics every 10 seconds. You can trigger simulated security events from this server by opening the Go Agent terminal window and using the interactive CLI menu options.
              </p>
            </div>
          </div>

          {/* Node Metadata Card */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600, borderBottom: '1px solid var(--panel-border)', paddingBottom: '0.5rem' }}>System Profile</h4>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', fontSize: '0.85rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>OS Type</span>
                <span style={{ fontWeight: 600 }}>{server.osType}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Kernel Version</span>
                <span style={{ fontWeight: 600, fontSize: '0.75rem' }}>{server.osVersion}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Host IP Address</span>
                <span style={{ fontWeight: 600 }}>{server.ipAddress}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Connection Status</span>
                <span className={`status-badge ${server.status}`} style={{ fontSize: '0.65rem' }}>{server.status}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Uptime</span>
                <span>{formatDuration(server.uptime)}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--text-secondary)' }}>Registered On</span>
                <span>{new Date(server.registeredAt).toLocaleDateString()}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Server specific events */}
        <div className="glass-panel">
          <h4 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem' }}>Server Event History</h4>
          <div className="custom-table-container">
            <table className="custom-table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Category</th>
                  <th>Severity</th>
                  <th>Source</th>
                  <th>Incident Name</th>
                  <th>Event Details</th>
                </tr>
              </thead>
              <tbody>
                {logsList.filter(l => l.serverId === id).length === 0 ? (
                  <tr>
                    <td colSpan={6} style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)' }}>
                      No events logged for this server yet.
                    </td>
                  </tr>
                ) : (
                  logsList.filter(l => l.serverId === id).map(log => (
                    <tr key={log.id}>
                      <td style={{ fontSize: '0.75rem' }}>{new Date(log.timestamp).toLocaleString()}</td>
                      <td>
                        <span style={{ fontSize: '0.75rem', fontFamily: 'monospace', background: 'rgba(255,255,255,0.05)', padding: '2px 6px', borderRadius: '4px' }}>
                          {log.category}
                        </span>
                      </td>
                      <td>
                        <span className={`severity-badge ${log.severity}`}>{log.severity}</span>
                      </td>
                      <td>{log.source}</td>
                      <td style={{ fontWeight: 600 }}>{log.title}</td>
                      <td style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', maxWidth: '280px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {log.details}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  };

  const renderAlerts = () => {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {/* Filter Toolbar */}
        <div className="glass-panel" style={{ display: 'flex', gap: '1.5rem', alignItems: 'center', padding: '0.75rem 1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.8rem' }}>
            <Filter size={14} color="var(--text-secondary)" />
            <span>Filters:</span>
          </div>

          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <select
              value={alertSeverityFilter}
              onChange={e => setAlertSeverityFilter(e.target.value)}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', padding: '4px 8px', borderRadius: '6px', fontSize: '0.75rem', color: '#fff' }}
            >
              <option value="">All Severities</option>
              <option value="critical">Critical</option>
              <option value="warning">Warning</option>
            </select>

            <select
              value={alertAckFilter}
              onChange={e => setAlertAckFilter(e.target.value)}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', padding: '4px 8px', borderRadius: '6px', fontSize: '0.75rem', color: '#fff' }}
            >
              <option value="">All Statuses</option>
              <option value="false">Unacknowledged</option>
              <option value="true">Acknowledged</option>
            </select>

            <button
              onClick={() => {
                setAlertSeverityFilter('');
                setAlertAckFilter('false');
              }}
              style={{ padding: '4px 10px', fontSize: '0.75rem' }}
            >
              Reset Filters
            </button>
          </div>
        </div>

        {/* Alerts Table */}
        <div className="glass-panel" style={{ padding: 0 }}>
          <div className="custom-table-container">
            <table className="custom-table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Server</th>
                  <th>Rule Name</th>
                  <th>Severity</th>
                  <th>Title</th>
                  <th>Alert Details</th>
                  <th>Telegram</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {alerts
                  .filter(a => !alertSeverityFilter || a.severity === alertSeverityFilter)
                  .filter(a => !alertAckFilter || a.isAcknowledged.toString() === alertAckFilter)
                  .length === 0 ? (
                  <tr>
                    <td colSpan={8} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
                      No alerts match the chosen filters.
                    </td>
                  </tr>
                ) : (
                  alerts
                    .filter(a => !alertSeverityFilter || a.severity === alertSeverityFilter)
                    .filter(a => !alertAckFilter || a.isAcknowledged.toString() === alertAckFilter)
                    .map(alert => (
                      <tr key={alert.id} style={{
                        background: !alert.isAcknowledged ? 
                          (alert.severity === 'critical' ? 'rgba(244, 63, 94, 0.01)' : 'rgba(245, 158, 11, 0.01)') : 'transparent'
                      }}>
                        <td style={{ fontSize: '0.75rem' }}>{new Date(alert.createdAt).toLocaleString()}</td>
                        <td style={{ fontWeight: 600 }}>
                          {alert.serverHostname || alert.server?.hostname || "Unknown"}
                          <span style={{ display: 'block', color: 'var(--text-muted)', fontSize: '0.7rem', fontWeight: 400, marginTop: '2px' }}>
                            {alert.hospitalName || alert.server?.hospital?.name || "Hospital"}
                          </span>
                        </td>
                        <td style={{ fontSize: '0.75rem', fontWeight: 600 }}>{alert.ruleName}</td>
                        <td>
                          <span className={`severity-badge ${alert.severity}`}>{alert.severity}</span>
                        </td>
                        <td style={{ fontWeight: 500 }}>{alert.title}</td>
                        <td style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', maxWidth: '250px' }}>
                          {alert.message}
                        </td>
                        <td style={{ textAlign: 'center' }}>
                          <span style={{ color: alert.telegramSent ? 'var(--success)' : 'var(--text-muted)', fontSize: '0.8rem' }}>
                            {alert.telegramSent ? 'Sent' : 'Offline'}
                          </span>
                        </td>
                        <td>
                          {alert.isAcknowledged ? (
                            <div style={{ fontSize: '0.7rem', color: 'var(--text-secondary)' }}>
                              <div style={{ display: 'flex', alignItems: 'center', gap: '2px', color: 'var(--success)' }}>
                                <CheckCircle size={10} /> Ack
                              </div>
                              <span style={{ fontSize: '0.65rem', color: 'var(--text-muted)' }}>by {alert.acknowledgedBy}</span>
                            </div>
                          ) : (
                            user?.role !== 'Viewer' ? (
                              <button onClick={() => handleAcknowledgeAlert(alert.id)} className="success" style={{ padding: '4px 10px', fontSize: '0.7rem' }}>
                                Acknowledge
                              </button>
                            ) : (
                              <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>Unacknowledged</span>
                            )
                          )}
                        </td>
                      </tr>
                    ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    );
  };

  const renderLogs = () => {
    const categories = ['login', 'rdp', 'user_mgmt', 'service', 'firewall', 'privilege', 'backup', 'crontab'];

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {/* Filter bar */}
        <div className="glass-panel" style={{ display: 'flex', flexWrap: 'wrap', gap: '1rem', alignItems: 'center', padding: '0.75rem 1rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '4px 10px', flex: 1, minWidth: '220px' }}>
            <Search size={14} color="var(--text-secondary)" />
            <input
              type="text"
              placeholder="Search events, servers or messages..."
              value={logSearchQuery}
              onChange={e => { setLogSearchQuery(e.target.value); setLogPage(1); }}
              style={{ background: 'transparent', border: 'none', color: '#fff', fontSize: '0.8rem', width: '100%', outline: 'none' }}
            />
          </div>

          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <select
              value={logSeverityFilter}
              onChange={e => { setLogSeverityFilter(e.target.value); setLogPage(1); }}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', padding: '6px 10px', borderRadius: '6px', fontSize: '0.75rem', color: '#fff' }}
            >
              <option value="">All Severities</option>
              <option value="critical">Critical</option>
              <option value="warning">Warning</option>
              <option value="info">Info</option>
            </select>

            <select
              value={logCategoryFilter}
              onChange={e => { setLogCategoryFilter(e.target.value); setLogPage(1); }}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', padding: '6px 10px', borderRadius: '6px', fontSize: '0.75rem', color: '#fff' }}
            >
              <option value="">All Categories</option>
              {categories.map(c => <option key={c} value={c}>{c}</option>)}
            </select>

            <button
              onClick={() => {
                setLogSeverityFilter('');
                setLogCategoryFilter('');
                setLogSearchQuery('');
                setLogPage(1);
              }}
              style={{ padding: '6px 12px', fontSize: '0.75rem' }}
            >
              Reset Filters
            </button>
          </div>
        </div>

        {/* Logs Table */}
        <div className="glass-panel" style={{ padding: 0 }}>
          <div className="custom-table-container">
            <table className="custom-table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Server</th>
                  <th>Category</th>
                  <th>Severity</th>
                  <th>Source</th>
                  <th>Event Name</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {logsList.length === 0 ? (
                  <tr>
                    <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
                      No events match search filters.
                    </td>
                  </tr>
                ) : (
                  logsList.map(log => (
                    <tr key={log.id}>
                      <td style={{ fontSize: '0.75rem' }}>{new Date(log.timestamp).toLocaleString()}</td>
                      <td style={{ fontWeight: 600 }}>
                        {log.serverHostname || log.server?.hostname || "Unknown"}
                        <span style={{ display: 'block', color: 'var(--text-muted)', fontSize: '0.7rem', fontWeight: 400, marginTop: '2px' }}>
                          {log.hospitalName || log.server?.hospital?.name || "Hospital"}
                        </span>
                      </td>
                      <td>
                        <span style={{ fontSize: '0.75rem', fontFamily: 'monospace', background: 'rgba(255,255,255,0.05)', padding: '2px 6px', borderRadius: '4px' }}>
                          {log.category}
                        </span>
                      </td>
                      <td>
                        <span className={`severity-badge ${log.severity}`}>{log.severity}</span>
                      </td>
                      <td>{log.source}</td>
                      <td style={{ fontWeight: 600 }}>{log.title}</td>
                      <td style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>{log.details}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination Footer */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem', borderTop: '1px solid var(--panel-border)' }}>
            <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
              Showing {logsList.length} of {totalLogs} events
            </span>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button 
                onClick={() => setLogPage(p => Math.max(1, p - 1))} 
                disabled={logPage === 1}
                style={{ padding: '4px 10px', fontSize: '0.75rem' }}
              >
                Previous
              </button>
              <button 
                onClick={() => setLogPage(p => p + 1)} 
                disabled={logsList.length < 20}
                style={{ padding: '4px 10px', fontSize: '0.75rem' }}
              >
                Next
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  };

  const renderSettings = () => {
    if (!config) return <div style={{ padding: '2rem', textAlign: 'center' }}>Loading configurations...</div>;

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: '1.5rem' }}>
          {/* Config Forms */}
          <form onSubmit={handleSaveConfig} className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600, borderBottom: '1px solid var(--panel-border)', paddingBottom: '0.5rem' }}>Integrations & Thresholds</h4>
            
            <fieldset disabled={user?.role !== 'Administrator'} style={{ border: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
              {/* API Key */}
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)' }}>Agent X-API-Key</label>
                <input
                  type="text"
                  value={config.apiKey}
                  onChange={e => setConfig({ ...config, apiKey: e.target.value })}
                  required
                  style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', fontSize: '0.85rem', color: '#fff' }}
                />
              </div>

              {/* Telegram config */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                  <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)' }}>Telegram Bot Token</label>
                  <input
                    type="password"
                    placeholder="e.g. 123456789:ABCDefGh..."
                    value={config.telegramBotToken || ''}
                    onChange={e => setConfig({ ...config, telegramBotToken: e.target.value })}
                    style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', fontSize: '0.85rem', color: '#fff' }}
                  />
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                  <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)' }}>Telegram Chat ID</label>
                  <input
                    type="text"
                    placeholder="e.g. -10012345678"
                    value={config.telegramChatId || ''}
                    onChange={e => setConfig({ ...config, telegramChatId: e.target.value })}
                    style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', fontSize: '0.85rem', color: '#fff' }}
                  />
                </div>
              </div>

              {/* Rules config */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginTop: '0.5rem' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                  <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)' }}>Brute-force Limit (Attempts)</label>
                  <input
                    type="number"
                    value={config.bruteForceThreshold}
                    onChange={e => setConfig({ ...config, bruteForceThreshold: parseInt(e.target.value) || 5 })}
                    min={2}
                    style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', fontSize: '0.85rem', color: '#fff' }}
                  />
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                  <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)' }}>Brute-force Window (Minutes)</label>
                  <input
                    type="number"
                    value={config.bruteForceWindowMinutes}
                    onChange={e => setConfig({ ...config, bruteForceWindowMinutes: parseInt(e.target.value) || 5 })}
                    min={1}
                    style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', fontSize: '0.85rem', color: '#fff' }}
                  />
                </div>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
                <label style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)' }}>Offline Timeout (Seconds)</label>
                <input
                  type="number"
                  value={config.agentOfflineTimeoutSeconds}
                  onChange={e => setConfig({ ...config, agentOfflineTimeoutSeconds: parseInt(e.target.value) || 90 })}
                  min={15}
                  style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', fontSize: '0.85rem', color: '#fff' }}
                />
              </div>

              {user?.role !== 'Administrator' && (
                <p style={{ fontSize: '0.75rem', color: 'var(--warning)', margin: 0, marginTop: '0.5rem' }}>
                  * Modifying system configurations is restricted to Administrators only.
                </p>
              )}

              <button type="submit" className="primary" style={{ width: '100%', justifyContent: 'center', marginTop: '1rem' }}>
                Save Configurations
              </button>
            </fieldset>
          </form>

          {/* Rules overview info card */}
          <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600, borderBottom: '1px solid var(--panel-border)', paddingBottom: '0.5rem' }}>Alert Rules Matrix</h4>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', fontSize: '0.75rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>Brute-force attempts</span>
                <span className="severity-badge critical">CRITICAL</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>After-hours success login</span>
                <span className="severity-badge warning">WARNING</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>SA/SYS database login</span>
                <span className="severity-badge critical">CRITICAL</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>IIS / MSSQL service down</span>
                <span className="severity-badge critical">CRITICAL</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>Agent offline &gt; 90s</span>
                <span className="severity-badge critical">CRITICAL</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>Database Backup failed</span>
                <span className="severity-badge critical">CRITICAL</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>User Account changes</span>
                <span className="severity-badge warning">WARNING</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>Firewall policy changes</span>
                <span className="severity-badge warning">WARNING</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingBottom: '4px', borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                <span>Direct Root SSH Session</span>
                <span className="severity-badge critical">CRITICAL</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };

  // Incident Management Handlers and Views
  const fetchIncidentTimeline = async (incidentId: number) => {
    try {
      const res = await authFetch(`/api/v1/audit?pageSize=100&entityId=${incidentId}`);
      if (res.ok) {
        const data = await res.json();
        const sorted = (data.items || []).sort((a: any, b: any) => new Date(a.timestampUtc).getTime() - new Date(b.timestampUtc).getTime());
        setIncidentTimeline(sorted);
      }
    } catch (err) {
      console.error('Failed to fetch incident timeline:', err);
    }
  };

  const handleSelectIncident = async (id: number) => {
    setSelectedIncidentId(id);
    setSelectedIncidentDetail(null);
    setIncidentTimeline([]);
    try {
      const res = await authFetch(`/api/v1/incidents/${id}`);
      if (res.ok) {
        const data = await res.json();
        setSelectedIncidentDetail(data);
      }
      await fetchIncidentTimeline(id);
      await fetchIncidentResponses(id);
    } catch (err) {
      console.error('Failed to load incident detail:', err);
      showToast('Error loading incident details.', 'critical');
    }
  };

  const handleCreateIncident = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newIncidentTitle.trim()) {
      showToast('Title is required', 'warning');
      return;
    }
    try {
      const res = await authFetch('/api/v1/incidents', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: newIncidentTitle,
          description: newIncidentDescription,
          alertIds: newIncidentSelectedAlerts
        })
      });

      if (res.ok) {
        showToast('Incident created successfully', 'success');
        setShowCreateIncidentModal(false);
        setNewIncidentTitle('');
        setNewIncidentDescription('');
        setNewIncidentSelectedAlerts([]);
        loadInitialData();
      } else {
        const err = await res.json();
        showToast(err.message || 'Failed to create incident', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('Network error creating incident', 'critical');
    }
  };

  const handleAssignIncident = async (userId: string | null) => {
    if (!selectedIncidentId) return;
    try {
      const res = await authFetch(`/api/v1/incidents/${selectedIncidentId}/assign`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ assignedUserId: userId })
      });

      if (res.ok) {
        showToast('Incident assignment updated', 'success');
        const data = await res.json();
        setSelectedIncidentDetail(data);
        loadInitialData();
        fetchIncidentTimeline(selectedIncidentId);
      } else {
        const err = await res.json();
        showToast(err.message || 'Failed to assign incident', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('Network error assigning incident', 'critical');
    }
  };

  const handleUpdateIncidentStatus = async (status: string) => {
    if (!selectedIncidentId) return;
    try {
      const res = await authFetch(`/api/v1/incidents/${selectedIncidentId}/status`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status })
      });

      if (res.ok) {
        showToast(`Incident status updated to ${status}`, 'success');
        const data = await res.json();
        setSelectedIncidentDetail(data);
        loadInitialData();
        fetchIncidentTimeline(selectedIncidentId);
      } else {
        const err = await res.json();
        showToast(err.message || 'Failed to update status', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('Network error updating status', 'critical');
    }
  };

  const handleLinkAlertsToIncident = async () => {
    if (!selectedIncidentId || linkAlertsSelected.length === 0) return;
    try {
      const res = await authFetch(`/api/v1/incidents/${selectedIncidentId}/alerts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ alertIds: linkAlertsSelected })
      });

      if (res.ok) {
        showToast('Alerts linked successfully', 'success');
        setShowLinkAlertsModal(false);
        setLinkAlertsSelected([]);
        const data = await res.json();
        setSelectedIncidentDetail(data);
        loadInitialData();
        fetchIncidentTimeline(selectedIncidentId);
      } else {
        const err = await res.json();
        showToast(err.message || 'Failed to link alerts', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('Network error linking alerts', 'critical');
    }
  };

  const handleUnlinkAlertFromIncident = async (alertId: number) => {
    if (!selectedIncidentId) return;
    try {
      const res = await authFetch(`/api/v1/incidents/${selectedIncidentId}/alerts/${alertId}`, {
        method: 'DELETE'
      });

      if (res.ok) {
        showToast('Alert unlinked successfully', 'success');
        const data = await res.json();
        setSelectedIncidentDetail(data);
        loadInitialData();
        fetchIncidentTimeline(selectedIncidentId);
      } else {
        const err = await res.json();
        showToast(err.message || 'Failed to unlink alert', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('Network error unlinking alert', 'critical');
    }
  };

  const renderIncidentDetail = () => {
    if (!selectedIncidentDetail) {
      return <div className="glass-panel" style={{ padding: '2rem', textAlign: 'center' }}>Loading incident details...</div>;
    }

    const detail = selectedIncidentDetail;
    const canManage = user?.role === 'Administrator' || user?.role === 'Operator' || user?.role === 'SecurityOperator';

    // State machine buttons list depending on current state
    const currentStatus = detail.status;
    const allowedTransitions: { label: string; status: string; variant: 'primary' | 'warning' | 'success' | 'info' }[] = [];

    if (currentStatus === 'New' && canManage) {
      allowedTransitions.push({ label: 'Assign', status: 'Assigned', variant: 'info' });
    } else if (currentStatus === 'Assigned' && canManage) {
      allowedTransitions.push({ label: 'Investigate', status: 'Investigating', variant: 'primary' });
    } else if (currentStatus === 'Investigating' && canManage) {
      allowedTransitions.push({ label: 'Mark as Resolved', status: 'Resolved', variant: 'success' });
      allowedTransitions.push({ label: 'Mark as False Positive', status: 'FalsePositive', variant: 'warning' });
    } else if ((currentStatus === 'Resolved' || currentStatus === 'FalsePositive') && canManage) {
      allowedTransitions.push({ label: 'Close Incident', status: 'Closed', variant: 'success' });
    }

    // Alerts in the global context that are NOT assigned to any incident
    const assignableAlerts = state.alerts.filter(a => !a.incidentId);

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {/* Back and Title Row */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <button 
            onClick={() => { setSelectedIncidentId(null); setSelectedIncidentDetail(null); }} 
            className="btn" 
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', background: 'rgba(255,255,255,0.05)' }}
          >
            &larr; Back to List
          </button>
          
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            {allowedTransitions.map((t, idx) => (
              <button 
                key={idx}
                onClick={() => handleUpdateIncidentStatus(t.status)}
                style={{
                  background: t.variant === 'success' ? 'var(--success)' : t.variant === 'warning' ? 'var(--warning)' : t.variant === 'info' ? 'var(--primary)' : 'var(--accent)',
                  color: '#fff',
                  border: 'none',
                  padding: '8px 16px',
                  borderRadius: '6px',
                  fontSize: '0.85rem',
                  fontWeight: 600,
                  cursor: 'pointer'
                }}
              >
                {t.label}
              </button>
            ))}
          </div>
        </div>

        {/* Detailed Layout */}
        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '1.5rem' }}>
          {/* Main Info */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            <div className="glass-panel" style={{ padding: '2rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', fontWeight: 600 }}>INCIDENT #{detail.id}</span>
                  <h3 style={{ fontSize: '1.5rem', fontWeight: 700, margin: '0.25rem 0' }}>{detail.title}</h3>
                  <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', whiteSpace: 'pre-wrap', marginTop: '0.5rem' }}>{detail.description}</p>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '0.5rem' }}>
                  <span className={`severity-badge ${detail.severity.toLowerCase()}`}>
                    {detail.severity}
                  </span>
                  <span className="status-badge" style={{
                    borderColor: detail.status === 'New' ? 'var(--danger)' : detail.status === 'Assigned' ? 'var(--accent)' : detail.status === 'Investigating' ? 'var(--warning)' : 'var(--success)',
                    color: '#fff',
                    borderWidth: '1px',
                    borderStyle: 'solid',
                    padding: '2px 8px',
                    borderRadius: '4px',
                    fontSize: '0.75rem'
                  }}>
                    {detail.status}
                  </span>
                </div>
              </div>

              {/* Metadata Details */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginTop: '1.5rem', borderTop: '1px solid rgba(255,255,255,0.05)', paddingTop: '1.5rem', fontSize: '0.85rem' }}>
                <div>
                  <div style={{ color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>Created By</div>
                  <div style={{ fontWeight: 600 }}>{detail.createdBy || 'System'}</div>
                </div>
                <div>
                  <div style={{ color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>Created At</div>
                  <div style={{ fontWeight: 600 }}>{new Date(detail.createdAt).toLocaleString()}</div>
                </div>
                <div>
                  <div style={{ color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>Assigned Analyst</div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.25rem' }}>
                    {canManage ? (
                      <select 
                        value={detail.assignedUserId || ''} 
                        onChange={(e) => handleAssignIncident(e.target.value || null)}
                        style={{
                          background: '#111827',
                          border: '1px solid var(--panel-border)',
                          color: '#fff',
                          padding: '4px 8px',
                          borderRadius: '4px',
                          fontSize: '0.8rem',
                          outline: 'none'
                        }}
                      >
                        <option value="">Unassigned</option>
                        {usersList.filter(u => u.role !== 'Viewer').map(u => (
                          <option key={u.id} value={u.id}>{u.username}</option>
                        ))}
                      </select>
                    ) : (
                      <div style={{ fontWeight: 600 }}>{detail.assignedUserName || 'Unassigned'}</div>
                    )}
                  </div>
                </div>
                <div>
                  <div style={{ color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>Last Updated</div>
                  <div style={{ fontWeight: 600 }}>{new Date(detail.updatedAt).toLocaleString()}</div>
                </div>
              </div>
            </div>

            {/* Linked Alerts */}
            <div className="glass-panel" style={{ padding: '1.5rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                <h4 style={{ fontSize: '1rem', fontWeight: 600 }}>Linked Alerts ({detail.alerts.length})</h4>
                {canManage && detail.status !== 'Closed' && (
                  <button 
                    onClick={() => setShowLinkAlertsModal(true)} 
                    className="btn" 
                    style={{ fontSize: '0.75rem', padding: '4px 8px', background: 'var(--primary)' }}
                  >
                    Link Alerts
                  </button>
                )}
              </div>

              {detail.alerts.length === 0 ? (
                <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                  No alerts linked to this incident.
                </div>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', textAlign: 'left', color: 'var(--text-secondary)' }}>
                        <th style={{ padding: '8px' }}>Rule</th>
                        <th style={{ padding: '8px' }}>Title</th>
                        <th style={{ padding: '8px' }}>Host</th>
                        <th style={{ padding: '8px' }}>Severity</th>
                        <th style={{ padding: '8px' }}>Triggered</th>
                        {canManage && detail.status !== 'Closed' && <th style={{ padding: '8px', textAlign: 'right' }}>Actions</th>}
                      </tr>
                    </thead>
                    <tbody>
                      {detail.alerts.map((alert: any) => (
                        <tr key={alert.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                          <td style={{ padding: '8px', fontWeight: 600 }}>{alert.ruleName}</td>
                          <td style={{ padding: '8px' }}>{alert.title}</td>
                          <td style={{ padding: '8px' }}>{alert.agentHostname}</td>
                          <td style={{ padding: '8px' }}>
                            <span className={`severity-badge ${alert.severity === 'critical' ? 'critical' : 'warning'}`}>
                              {alert.severity}
                            </span>
                          </td>
                          <td style={{ padding: '8px' }}>{new Date(alert.createdAt).toLocaleString()}</td>
                          {canManage && detail.status !== 'Closed' && (
                            <td style={{ padding: '8px', textAlign: 'right' }}>
                              <button 
                                onClick={() => handleUnlinkAlertFromIncident(alert.id)}
                                style={{
                                  background: 'rgba(244, 63, 94, 0.15)',
                                  border: '1px solid var(--danger)',
                                  color: 'var(--danger)',
                                  padding: '2px 6px',
                                  borderRadius: '4px',
                                  fontSize: '0.75rem',
                                  cursor: 'pointer'
                                }}
                              >
                                Unlink
                              </button>
                            </td>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {/* Remote Response Actions */}
            <div className="glass-panel" style={{ padding: '1.5rem', marginTop: '1.5rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                <h4 style={{ fontSize: '1rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <Zap size={18} className="text-indigo-400" /> Remote Response Actions
                </h4>
              </div>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginBottom: '1.25rem' }}>
                Issue remediation commands to the agent machine linked to alerts in this incident.
              </p>
              
              {/* Trigger Buttons */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.75rem', marginBottom: '1.5rem' }}>
                <button
                  disabled={!detail.alerts || detail.alerts.length === 0 || detail.status === 'Closed' || user?.role === 'Viewer'}
                  onClick={() => handleTriggerResponseAction('Restart')}
                  className="btn"
                  style={{
                    background: 'rgba(244, 63, 94, 0.1)',
                    border: '1px solid rgb(244, 63, 94)',
                    color: 'rgb(244, 63, 94)',
                    padding: '8px 16px',
                    borderRadius: '6px',
                    fontSize: '0.85rem',
                    fontWeight: 600,
                    cursor: 'pointer'
                  }}
                >
                  Restart Agent
                </button>

                <button
                  disabled={!detail.alerts || detail.alerts.length === 0 || detail.status === 'Closed' || user?.role === 'Viewer'}
                  onClick={() => handleTriggerResponseAction('CollectDiagnostics')}
                  className="btn"
                  style={{
                    background: 'rgba(56, 189, 248, 0.1)',
                    border: '1px solid rgb(56, 189, 248)',
                    color: 'rgb(56, 189, 248)',
                    padding: '8px 16px',
                    borderRadius: '6px',
                    fontSize: '0.85rem',
                    fontWeight: 600,
                    cursor: 'pointer'
                  }}
                >
                  Collect Logs
                </button>

                <button
                  disabled={!detail.alerts || detail.alerts.length === 0 || detail.status === 'Closed' || user?.role === 'Viewer'}
                  onClick={() => handleTriggerResponseAction('RunScan')}
                  className="btn"
                  style={{
                    background: 'rgba(16, 185, 129, 0.1)',
                    border: '1px solid rgb(16, 185, 129)',
                    color: 'rgb(16, 185, 129)',
                    padding: '8px 16px',
                    borderRadius: '6px',
                    fontSize: '0.85rem',
                    fontWeight: 600,
                    cursor: 'pointer'
                  }}
                >
                  Run Scan
                </button>

                <button
                  disabled={!detail.alerts || detail.alerts.length === 0 || detail.status === 'Closed' || user?.role === 'Viewer'}
                  onClick={() => handleTriggerResponseAction('SyncConfiguration')}
                  className="btn"
                  style={{
                    background: 'rgba(168, 85, 247, 0.1)',
                    border: '1px solid rgb(168, 85, 247)',
                    color: 'rgb(168, 85, 247)',
                    padding: '8px 16px',
                    borderRadius: '6px',
                    fontSize: '0.85rem',
                    fontWeight: 600,
                    cursor: 'pointer'
                  }}
                >
                  Sync Configuration
                </button>
              </div>
              
              {/* History Table */}
              <h5 style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '0.75rem' }}>
                Incident Action Logs
              </h5>
              
              {incidentResponses.length === 0 ? (
                <div style={{ textAlign: 'center', padding: '1.5rem', color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                  No actions have been executed for this incident yet.
                </div>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.8rem' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', textAlign: 'left', color: 'var(--text-muted)' }}>
                        <th style={{ padding: '6px' }}>Action</th>
                        <th style={{ padding: '6px' }}>Status</th>
                        <th style={{ padding: '6px' }}>Requester</th>
                        <th style={{ padding: '6px' }}>Execution Time</th>
                        <th style={{ padding: '6px' }}>Message / Output</th>
                      </tr>
                    </thead>
                    <tbody>
                      {incidentResponses.map((resItem: any) => (
                        <tr key={resItem.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                          <td style={{ padding: '6px', fontWeight: 600 }}>{resItem.actionType}</td>
                          <td style={{ padding: '6px' }}>
                            <span style={{
                              color: resItem.status === 'Succeeded' ? 'var(--success)' : resItem.status === 'Failed' ? 'var(--danger)' : resItem.status === 'Executing' ? '#818cf8' : 'var(--warning)',
                              fontWeight: 600
                            }}>
                              {resItem.status}
                            </span>
                          </td>
                          <td style={{ padding: '6px' }}>{resItem.requestedByUserName}</td>
                          <td style={{ padding: '6px', color: 'var(--text-muted)' }}>
                            {new Date(resItem.requestedAt).toLocaleString()}
                          </td>
                          <td style={{ padding: '6px', color: resItem.status === 'Failed' ? 'var(--danger)' : 'var(--text-primary)', whiteSpace: 'nowrap', textOverflow: 'ellipsis', overflow: 'hidden', maxWidth: '200px' }}>
                            {resItem.resultMessage || <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>No message</span>}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

          {/* Timeline and History from Audit Log */}
          <div className="glass-panel" style={{ padding: '1.5rem', maxHeight: '600px', overflowY: 'auto' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1.25rem' }}>Incident Lifecycle Timeline</h4>
            {incidentTimeline.length === 0 ? (
              <div style={{ color: 'var(--text-muted)', fontSize: '0.85rem', textAlign: 'center', padding: '2rem' }}>
                No status history found.
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column' }}>
                {incidentTimeline.map((item, idx) => (
                  <div key={idx} style={{ display: 'flex', gap: '1rem', position: 'relative', paddingBottom: '1.5rem' }}>
                    {idx < incidentTimeline.length - 1 && (
                      <div style={{ position: 'absolute', left: '16px', top: '24px', bottom: 0, width: '2px', background: 'rgba(255,255,255,0.05)' }} />
                    )}
                    <div style={{
                      width: '10px',
                      height: '10px',
                      borderRadius: '50%',
                      background: item.severity === 'Warning' ? 'var(--warning)' : item.severity === 'Critical' ? 'var(--danger)' : 'var(--primary)',
                      marginLeft: '12px',
                      marginTop: '6px',
                      zIndex: 2,
                      boxShadow: '0 0 10px currentColor'
                    }} />
                    <div>
                      <div style={{ fontSize: '0.72rem', color: 'var(--text-muted)' }}>
                        {new Date(item.timestampUtc).toLocaleString()} by <strong>{item.userName}</strong>
                      </div>
                      <div style={{ fontSize: '0.82rem', color: 'var(--text-primary)', marginTop: '0.25rem', fontWeight: 500 }}>
                        {item.description}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Link Alerts Modal */}
        {showLinkAlertsModal && (
          <div style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
          }}>
            <div className="glass-panel" style={{ width: '600px', padding: '2rem', display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Link Unassigned Alerts</h3>
                <button 
                  onClick={() => { setShowLinkAlertsModal(false); setLinkAlertsSelected([]); }}
                  style={{ background: 'none', border: 'none', color: '#fff', fontSize: '1.25rem', cursor: 'pointer' }}
                >
                  &times;
                </button>
              </div>

              <div style={{ maxHeight: '300px', overflowY: 'auto', border: '1px solid var(--panel-border)', borderRadius: '6px' }}>
                {assignableAlerts.length === 0 ? (
                  <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                    No unassigned alerts available.
                  </div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.8rem' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid var(--panel-border)', background: 'rgba(255,255,255,0.02)', textAlign: 'left' }}>
                        <th style={{ padding: '8px' }}>Select</th>
                        <th style={{ padding: '8px' }}>Rule</th>
                        <th style={{ padding: '8px' }}>Title</th>
                        <th style={{ padding: '8px' }}>Severity</th>
                      </tr>
                    </thead>
                    <tbody>
                      {assignableAlerts.map(alert => (
                        <tr key={alert.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                          <td style={{ padding: '8px' }}>
                            <input 
                              type="checkbox" 
                              checked={linkAlertsSelected.includes(alert.id)}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  setLinkAlertsSelected([...linkAlertsSelected, alert.id]);
                                } else {
                                  setLinkAlertsSelected(linkAlertsSelected.filter(id => id !== alert.id));
                                }
                              }}
                            />
                          </td>
                          <td style={{ padding: '8px', fontWeight: 600 }}>{alert.ruleName}</td>
                          <td style={{ padding: '8px' }}>{alert.title}</td>
                          <td style={{ padding: '8px' }}>
                            <span className={`severity-badge ${alert.severity === 'critical' ? 'critical' : 'warning'}`}>
                              {alert.severity}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '0.5rem' }}>
                <button 
                  onClick={() => { setShowLinkAlertsModal(false); setLinkAlertsSelected([]); }}
                  className="btn"
                  style={{ background: 'rgba(255,255,255,0.05)' }}
                >
                  Cancel
                </button>
                <button 
                  onClick={handleLinkAlertsToIncident}
                  className="btn"
                  disabled={linkAlertsSelected.length === 0}
                  style={{ background: 'var(--primary)' }}
                >
                  Link selected ({linkAlertsSelected.length})
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  };

  const renderIncidents = () => {
    if (selectedIncidentId) {
      return renderIncidentDetail();
    }

    const filteredIncidents = state.incidents.filter(i => {
      if (incidentStatusFilter && i.status !== incidentStatusFilter) return false;
      if (incidentSeverityFilter && i.severity !== incidentSeverityFilter) return false;
      if (incidentAssigneeFilter && i.assignedUserId !== incidentAssigneeFilter) return false;
      if (onlyMyIncidents && i.assignedUserName !== user?.username) return false;
      if (incidentSearchQuery) {
        const query = incidentSearchQuery.toLowerCase();
        return i.title.toLowerCase().includes(query) || i.description.toLowerCase().includes(query);
      }
      return true;
    });

    const isOperatorPlus = user?.role === 'Administrator' || user?.role === 'Operator' || user?.role === 'SecurityOperator';
    const assignableAlerts = state.alerts.filter(a => !a.incidentId);

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        {/* Filters and Search Row */}
        <div className="glass-panel" style={{ padding: '1.25rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h4 style={{ fontSize: '1rem', fontWeight: 600 }}>Filter Incidents ({filteredIncidents.length})</h4>
            {isOperatorPlus && (
              <button 
                onClick={() => setShowCreateIncidentModal(true)} 
                className="btn" 
                style={{ background: 'var(--primary)', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
              >
                Create Incident
              </button>
            )}
          </div>
          
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1rem', alignItems: 'center' }}>
            {/* Search */}
            <div style={{ flex: 1, minWidth: '200px', display: 'flex', alignItems: 'center', background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '4px 10px' }}>
              <Search size={14} color="var(--text-secondary)" />
              <input 
                type="text" 
                placeholder="Search Title or Description..."
                value={incidentSearchQuery}
                onChange={e => setIncidentSearchQuery(e.target.value)}
                style={{ background: 'none', border: 'none', color: '#fff', fontSize: '0.8rem', outline: 'none', width: '100%', marginLeft: '6px' }}
              />
            </div>

            {/* Status */}
            <select 
              value={incidentStatusFilter}
              onChange={e => setIncidentStatusFilter(e.target.value)}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', color: '#fff', padding: '6px 12px', borderRadius: '6px', fontSize: '0.8rem', outline: 'none' }}
            >
              <option value="">All Statuses</option>
              <option value="New">New</option>
              <option value="Assigned">Assigned</option>
              <option value="Investigating">Investigating</option>
              <option value="Resolved">Resolved</option>
              <option value="FalsePositive">False Positive</option>
              <option value="Closed">Closed</option>
            </select>

            {/* Severity */}
            <select 
              value={incidentSeverityFilter}
              onChange={e => setIncidentSeverityFilter(e.target.value)}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', color: '#fff', padding: '6px 12px', borderRadius: '6px', fontSize: '0.8rem', outline: 'none' }}
            >
              <option value="">All Severities</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>

            {/* Assignee */}
            <select 
              value={incidentAssigneeFilter}
              onChange={e => setIncidentAssigneeFilter(e.target.value)}
              style={{ background: '#111827', border: '1px solid var(--panel-border)', color: '#fff', padding: '6px 12px', borderRadius: '6px', fontSize: '0.8rem', outline: 'none' }}
            >
              <option value="">All Assignees</option>
              {usersList.filter(u => u.role !== 'Viewer').map(u => (
                <option key={u.id} value={u.id}>{u.username}</option>
              ))}
            </select>

            {/* Only My Incidents */}
            {isOperatorPlus && (
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.8rem' }}>
                <input 
                  type="checkbox" 
                  checked={onlyMyIncidents}
                  onChange={e => setOnlyMyIncidents(e.target.checked)}
                />
                Only My Incidents
              </label>
            )}

            {/* Reset */}
            {(incidentStatusFilter || incidentSeverityFilter || incidentAssigneeFilter || incidentSearchQuery || onlyMyIncidents) && (
              <button 
                onClick={() => {
                  setIncidentStatusFilter('');
                  setIncidentSeverityFilter('');
                  setIncidentAssigneeFilter('');
                  setIncidentSearchQuery('');
                  setOnlyMyIncidents(false);
                }}
                className="btn" 
                style={{ fontSize: '0.75rem', padding: '4px 8px', background: 'rgba(255,255,255,0.05)' }}
              >
                Reset Filters
              </button>
            )}
          </div>
        </div>

        {/* Incidents Table List */}
        <div className="glass-panel" style={{ padding: '0' }}>
          {filteredIncidents.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
              No incidents match the filters.
            </div>
          ) : (
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--panel-border)', background: 'rgba(255,255,255,0.02)', color: 'var(--text-secondary)', fontSize: '0.8rem' }}>
                    <th style={{ padding: '12px 16px' }}>ID</th>
                    <th style={{ padding: '12px 16px' }}>Title</th>
                    <th style={{ padding: '12px 16px' }}>Severity</th>
                    <th style={{ padding: '12px 16px' }}>Status</th>
                    <th style={{ padding: '12px 16px' }}>Assigned User</th>
                    <th style={{ padding: '12px 16px' }}>Alerts</th>
                    <th style={{ padding: '12px 16px' }}>Created</th>
                    <th style={{ padding: '12px 16px' }}>Updated</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredIncidents.map(i => (
                    <tr 
                      key={i.id}
                      onClick={() => handleSelectIncident(i.id)}
                      style={{ 
                        borderBottom: '1px solid rgba(255,255,255,0.02)', 
                        cursor: 'pointer', 
                        transition: 'background 0.2s'
                      }}
                      onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.02)'}
                      onMouseLeave={e => e.currentTarget.style.background = 'none'}
                    >
                      <td style={{ padding: '14px 16px', fontWeight: 600, color: 'var(--text-muted)' }}>#{i.id}</td>
                      <td style={{ padding: '14px 16px', fontWeight: 600 }}>{i.title}</td>
                      <td style={{ padding: '14px 16px' }}>
                        <span className={`severity-badge ${i.severity.toLowerCase()}`}>
                          {i.severity}
                        </span>
                      </td>
                      <td style={{ padding: '14px 16px' }}>
                        <span className="status-badge" style={{
                          borderColor: i.status === 'New' ? 'var(--danger)' : i.status === 'Assigned' ? 'var(--accent)' : i.status === 'Investigating' ? 'var(--warning)' : 'var(--success)',
                          color: '#fff',
                          borderWidth: '1px',
                          borderStyle: 'solid',
                          padding: '2px 8px',
                          borderRadius: '4px',
                          fontSize: '0.72rem'
                        }}>
                          {i.status}
                        </span>
                      </td>
                      <td style={{ padding: '14px 16px' }}>{i.assignedUserName || <span style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>Unassigned</span>}</td>
                      <td style={{ padding: '14px 16px', fontWeight: 600 }}>{i.alertCount}</td>
                      <td style={{ padding: '14px 16px', fontSize: '0.75rem', color: 'var(--text-muted)' }}>{new Date(i.createdAt).toLocaleString()}</td>
                      <td style={{ padding: '14px 16px', fontSize: '0.75rem', color: 'var(--text-muted)' }}>{new Date(i.updatedAt).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Create Incident Modal */}
        {showCreateIncidentModal && (
          <div style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
          }}>
            <form 
              onSubmit={handleCreateIncident}
              className="glass-panel" 
              style={{ width: '600px', padding: '2rem', display: 'flex', flexDirection: 'column', gap: '1.25rem' }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Create New SOC Incident</h3>
                <button 
                  type="button"
                  onClick={() => setShowCreateIncidentModal(false)}
                  style={{ background: 'none', border: 'none', color: '#fff', fontSize: '1.25rem', cursor: 'pointer' }}
                >
                  &times;
                </button>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <label style={{ fontSize: '0.85rem', fontWeight: 600 }}>Incident Title *</label>
                <input 
                  type="text" 
                  required
                  placeholder="e.g. Brute force database login attempt"
                  value={newIncidentTitle}
                  onChange={e => setNewIncidentTitle(e.target.value)}
                  style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', color: '#fff', outline: 'none' }}
                />
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <label style={{ fontSize: '0.85rem', fontWeight: 600 }}>Description *</label>
                <textarea 
                  required
                  placeholder="Provide incident context, investigation targets and recommendations..."
                  value={newIncidentDescription}
                  onChange={e => setNewIncidentDescription(e.target.value)}
                  style={{ background: '#111827', border: '1px solid var(--panel-border)', borderRadius: '6px', padding: '8px 12px', color: '#fff', outline: 'none', height: '80px', resize: 'vertical' }}
                />
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <label style={{ fontSize: '0.85rem', fontWeight: 600 }}>Link Trigger Alerts ({newIncidentSelectedAlerts.length})</label>
                <div style={{ maxHeight: '180px', overflowY: 'auto', border: '1px solid var(--panel-border)', borderRadius: '6px' }}>
                  {assignableAlerts.length === 0 ? (
                    <div style={{ padding: '1.5rem', textAlign: 'center', color: 'var(--text-secondary)', fontSize: '0.8rem' }}>
                      No unassigned alerts available to link.
                    </div>
                  ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.75rem' }}>
                      <thead>
                        <tr style={{ borderBottom: '1px solid var(--panel-border)', background: 'rgba(255,255,255,0.02)', textAlign: 'left' }}>
                          <th style={{ padding: '6px 8px' }}>Select</th>
                          <th style={{ padding: '6px 8px' }}>Rule</th>
                          <th style={{ padding: '6px 8px' }}>Title</th>
                          <th style={{ padding: '6px 8px' }}>Severity</th>
                        </tr>
                      </thead>
                      <tbody>
                        {assignableAlerts.map(alert => (
                          <tr key={alert.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.02)' }}>
                            <td style={{ padding: '6px 8px' }}>
                              <input 
                                type="checkbox" 
                                checked={newIncidentSelectedAlerts.includes(alert.id)}
                                onChange={(e) => {
                                  if (e.target.checked) {
                                    setNewIncidentSelectedAlerts([...newIncidentSelectedAlerts, alert.id]);
                                  } else {
                                    setNewIncidentSelectedAlerts(newIncidentSelectedAlerts.filter(id => id !== alert.id));
                                  }
                                }}
                              />
                            </td>
                            <td style={{ padding: '6px 8px', fontWeight: 600 }}>{alert.ruleName}</td>
                            <td style={{ padding: '6px 8px' }}>{alert.title}</td>
                            <td style={{ padding: '6px 8px' }}>
                              <span className={`severity-badge ${alert.severity === 'critical' ? 'critical' : 'warning'}`}>
                                {alert.severity}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '0.5rem' }}>
                <button 
                  type="button"
                  onClick={() => setShowCreateIncidentModal(false)}
                  className="btn"
                  style={{ background: 'rgba(255,255,255,0.05)' }}
                >
                  Cancel
                </button>
                <button 
                  type="submit"
                  className="btn"
                  style={{ background: 'var(--primary)' }}
                >
                  Create Incident
                </button>
              </div>
            </form>
          </div>
        )}
      </div>
    );
  };

  // Helper formats
  const formatDuration = (seconds: number) => {
    if (!seconds) return '0s';
    const d = Math.floor(seconds / (3600*24));
    const h = Math.floor((seconds % (3600*24)) / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    
    const parts = [];
    if (d > 0) parts.push(`${d}d`);
    if (h > 0) parts.push(`${h}h`);
    if (m > 0) parts.push(`${m}m`);
    if (s > 0 || parts.length === 0) parts.push(`${s}s`);
    return parts.join(' ');
  };

  // Main UI gating: if not logged in, render login panel
  if (!token) {
    return renderLogin();
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', background: 'var(--bg-gradient)' }}>
      {renderSidebar()}
      
      {/* Main Content Area */}
      <main style={{ marginLeft: '260px', flex: 1, padding: '2rem', minWidth: 0 }}>
        {renderHeader()}
        {activeTab === 'overview' && renderOverview()}
        {activeTab === 'soc-dashboard' && <SOCDashboard />}
        {activeTab === 'threat-hunting' && <ThreatHunting />}
        {activeTab === 'timeline' && <TimelineView />}
        {activeTab === 'servers' && renderServers()}
        {activeTab === 'infrastructure' && <Infrastructure user={user} authFetch={authFetch} />}
        {activeTab === 'alerts' && renderAlerts()}
        {activeTab === 'incidents' && renderIncidents()}
        {activeTab === 'logs' && renderLogs()}
        {activeTab === 'rules' && <RulesManagement />}
        {activeTab === 'responses' && <ResponseCenter />}
        {activeTab === 'settings' && renderSettings()}
      </main>

      {/* Real-time Web Toast Notification */}
      {toast && (
        <div style={{
          position: 'fixed',
          bottom: '24px',
          right: '24px',
          background: '#1f2937',
          border: '1px solid var(--panel-border)',
          borderLeft: `4px solid ${toast.severity === 'critical' ? 'var(--danger)' : toast.severity === 'warning' ? 'var(--warning)' : 'var(--success)'}`,
          padding: '1rem',
          borderRadius: '8px',
          boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.5)',
          display: 'flex',
          alignItems: 'center',
          gap: '0.75rem',
          zIndex: 100,
          animation: 'pulse-glow 5s infinite',
          maxWidth: '350px'
        }}>
          {toast.severity === 'critical' ? (
            <AlertOctagon color="var(--danger)" size={20} />
          ) : toast.severity === 'warning' ? (
            <AlertTriangle color="var(--warning)" size={20} />
          ) : (
            <CheckCircle color="var(--success)" size={20} />
          )}
          <span style={{ fontSize: '0.875rem', fontWeight: 500 }}>{toast.message}</span>
        </div>
      )}
    </div>
  );
}
