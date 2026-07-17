import React, { createContext, useReducer, useEffect, ReactNode, useRef } from 'react';
import { SignalRService, HubEvents, ConnectionStatus, NotificationState } from '../services/signalr.service';

export interface Hospital {
  id: number;
  name: string;
  code: string;
}

export interface MonitoredServer {
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

export interface SecurityEvent {
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

export interface Alert {
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
  acknowledgedAt?: string | null;
  acknowledgedBy?: string | null;
  telegramSent: boolean;
  incidentId?: number | null;
}

export interface OverviewData {
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

export interface IncidentDto {
  id: number;
  title: string;
  description: string;
  severity: string; // Low, Medium, High, Critical
  status: string; // New, Assigned, Investigating, Resolved, FalsePositive, Closed
  assignedUserId?: string | null;
  assignedUserName?: string | null;
  assignedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  alertCount: number;
  createdBy?: string | null;
}

export interface DashboardState {
  servers: MonitoredServer[];
  alerts: Alert[];
  logsList: SecurityEvent[];
  connectionState: NotificationState;
  overview: OverviewData | null;
  incidents: IncidentDto[];
  dashboardOverview: any | null;
}

const initialState: DashboardState = {
  servers: [],
  alerts: [],
  logsList: [],
  connectionState: {
    connectionStatus: 'disconnected',
    reconnectAttempts: 0
  },
  overview: null,
  incidents: [],
  dashboardOverview: null
};

type ReducerAction =
  | { type: 'INITIALIZE'; payload: { servers: MonitoredServer[]; alerts: Alert[]; logsList: SecurityEvent[]; overview: OverviewData; incidents: IncidentDto[] } }
  | { type: 'SYNC_AFTER_RECONNECT'; payload: { servers: MonitoredServer[]; alerts: Alert[]; logsList: SecurityEvent[]; overview: OverviewData; incidents: IncidentDto[] } }
  | { type: 'SIGNALR_CONNECTING' }
  | { type: 'SIGNALR_CONNECTED'; payload: { lastConnectedAt: Date } }
  | { type: 'SIGNALR_DISCONNECTED' }
  | { type: 'SIGNALR_RECONNECTING'; payload: { attempts: number } }
  | { type: 'ALERT_CREATED'; payload: any }
  | { type: 'SECURITY_EVENT_CREATED'; payload: any }
  | { type: 'HEARTBEAT_UPDATED'; payload: any }
  | { type: 'METRIC_UPDATED'; payload: any }
  | { type: 'AGENT_STATUS_CHANGED'; payload: any }
  | { type: 'ACKNOWLEDGE_ALERT'; payload: { alertId: number; acknowledgedBy: string; acknowledgedAt: string } }
  | { type: 'INCIDENT_CREATED'; payload: IncidentDto }
  | { type: 'INCIDENT_UPDATED'; payload: IncidentDto }
  | { type: 'INCIDENT_ASSIGNED'; payload: IncidentDto }
  | { type: 'INCIDENT_STATUS_CHANGED'; payload: IncidentDto }
  | { type: 'INCIDENT_CLOSED'; payload: IncidentDto }
  | { type: 'DASHBOARD_OVERVIEW_UPDATED'; payload: any };

const notificationReducer = (state: DashboardState, action: ReducerAction): DashboardState => {
  switch (action.type) {
    case 'INITIALIZE':
    case 'SYNC_AFTER_RECONNECT':
      return {
        ...state,
        servers: action.payload.servers,
        alerts: action.payload.alerts,
        logsList: action.payload.logsList,
        overview: action.payload.overview,
        incidents: action.payload.incidents,
        connectionState: {
          ...state.connectionState,
          connectionStatus: 'connected',
          lastConnectedAt: action.type === 'SYNC_AFTER_RECONNECT' ? new Date() : state.connectionState.lastConnectedAt,
          reconnectAttempts: 0
        }
      };

    case 'SIGNALR_CONNECTING':
      return {
        ...state,
        connectionState: {
          connectionStatus: 'loading',
          reconnectAttempts: state.connectionState.reconnectAttempts
        }
      };

    case 'SIGNALR_CONNECTED':
      return {
        ...state,
        connectionState: {
          connectionStatus: 'connected',
          lastConnectedAt: action.payload.lastConnectedAt,
          reconnectAttempts: 0
        }
      };

    case 'SIGNALR_DISCONNECTED':
      return {
        ...state,
        connectionState: {
          ...state.connectionState,
          connectionStatus: 'disconnected'
        }
      };

    case 'SIGNALR_RECONNECTING':
      return {
        ...state,
        connectionState: {
          connectionStatus: 'reconnecting',
          reconnectAttempts: action.payload.attempts
        }
      };

    case 'DASHBOARD_OVERVIEW_UPDATED':
      return {
        ...state,
        dashboardOverview: action.payload,
        overview: state.overview ? {
          ...state.overview,
          onlineCount: action.payload.onlineAgents,
          offlineCount: action.payload.offlineAgents,
          serverCount: action.payload.onlineAgents + action.payload.offlineAgents,
          criticalAlertCount: action.payload.criticalIncidents,
          eventCount24H: action.payload.alertsToday
        } : null
      };

    case 'ALERT_CREATED': {
      const payload = action.payload;
      if (state.alerts.some(a => a.id === payload.id)) {
        return state;
      }

      const newAlert: Alert = {
        id: payload.id,
        serverId: state.servers.find(s => s.agentId === payload.agentId)?.id ?? 1,

        serverHostname: payload.agentHostname,
        hospitalName: 'Hospital A',
        ruleName: payload.ruleName,
        severity: (payload.severity === 'high' || payload.severity === 'critical') ? 'critical' : 'warning',
        title: payload.title,
        message: payload.message,
        category: payload.category,
        createdAt: payload.createdAt,
        isAcknowledged: false,
        telegramSent: false,
        incidentId: payload.incidentId
      };

      const updatedAlerts = [newAlert, ...state.alerts].slice(0, 100);

      const updatedOverview = state.overview ? {
        ...state.overview,
        criticalAlertCount: newAlert.severity === 'critical' ? state.overview.criticalAlertCount + 1 : state.overview.criticalAlertCount,
        warningAlertCount: newAlert.severity === 'warning' ? state.overview.warningAlertCount + 1 : state.overview.warningAlertCount,
        recentAlerts: [newAlert, ...state.overview.recentAlerts.slice(0, 4)]
      } : null;

      return {
        ...state,
        alerts: updatedAlerts,
        overview: updatedOverview
      };
    }

    case 'SECURITY_EVENT_CREATED': {
      const payload = action.payload;
      if (state.logsList.some(e => e.eventId === payload.eventId)) {
        return state;
      }

      const newEvent: SecurityEvent = {
        id: payload.id,
        eventId: payload.eventId,
        serverId: state.servers.find(s => s.agentId === payload.agentId)?.id ?? 1,
        serverHostname: payload.agentHostname,
        hospitalName: 'Hospital A',
        timestamp: payload.timestamp,
        category: payload.category,
        severity: (payload.severity === 'high' || payload.severity === 'critical') ? 'critical' : (payload.severity === 'info' ? 'info' : 'warning'),
        source: payload.source,
        title: payload.title,
        details: payload.details,
        rawData: '{}',
        receivedAt: payload.receivedAt
      };

      const updatedLogs = [newEvent, ...state.logsList].slice(0, 200);

      const updatedOverview = state.overview ? {
        ...state.overview,
        eventCount24H: state.overview.eventCount24H + 1,
        timeline: state.overview.timeline.map((t, idx, arr) => {
          if (idx === arr.length - 1) {
            return { ...t, events: t.events + 1 };
          }
          return t;
        })
      } : null;

      return {
        ...state,
        logsList: updatedLogs,
        overview: updatedOverview
      };
    }

    case 'HEARTBEAT_UPDATED': {
      const payload = action.payload;
      const agentExists = state.servers.some(s => s.agentId === payload.agentId);

      if (!agentExists) {
        // Agent mới chưa có trong list → thêm vào
        const newServer: MonitoredServer = {
          id: Date.now(),
          agentId: payload.agentId,
          hostname: payload.agentHostname || payload.hostname || payload.agentId,
          ipAddress: payload.ipAddress || 'Unknown',
          osType: 'Windows',
          osVersion: '10/11',
          registeredAt: new Date().toISOString(),
          lastHeartbeat: payload.lastSeenAt || new Date().toISOString(),
          status: payload.status?.toLowerCase() === 'online' ? 'online' : 'offline',
          cpuPercent: 0,
          ramPercent: 0,
          diskPercent: 0,
          uptime: 0,
          hospitalId: payload.hospitalId || 1
        };
        return {
          ...state,
          servers: [...state.servers, newServer]
        };
      }

      const updatedServers = state.servers.map(s => {
        if (s.agentId === payload.agentId) {
          const newStatus: 'online' | 'offline' | 'warning' = payload.status?.toLowerCase() === 'online' ? 'online' : (payload.status?.toLowerCase() === 'warning' ? 'warning' : 'offline');
          return {
            ...s,
            status: newStatus,
            lastHeartbeat: payload.lastSeenAt
          };
        }
        return s;
      });

      return {
        ...state,
        servers: updatedServers
      };
    }

    case 'METRIC_UPDATED': {
      const payload = action.payload;
      const updatedServers = state.servers.map(s => {
        if (s.agentId === payload.agentId) {
          const incomingTime = new Date(payload.timestamp).getTime();
          const localTime = new Date(s.lastHeartbeat).getTime();
          if (incomingTime >= localTime) {
            return {
              ...s,
              cpuPercent: Math.round(payload.cpuUsagePercent),
              ramPercent: Math.round(payload.ramUsagePercent),
              diskPercent: Math.round(payload.diskUsagePercent),
              lastHeartbeat: payload.timestamp
            };
          }
        }
        return s;
      });

      return {
        ...state,
        servers: updatedServers
      };
    }

    case 'AGENT_STATUS_CHANGED': {
      const payload = action.payload;
      const existingAgent = state.servers.find(s => s.agentId === payload.agentId);

      const newStatus: 'online' | 'offline' | 'warning' = payload.newStatus?.toLowerCase() === 'online' ? 'online' : (payload.newStatus?.toLowerCase() === 'warning' ? 'warning' : 'offline');

      let finalServers: MonitoredServer[];
      if (!existingAgent) {
        // Agent hoàn toàn mới → thêm vào list ngay
        const newServer: MonitoredServer = {
          id: Date.now(),
          agentId: payload.agentId,
          hostname: payload.hostname || payload.agentId,
          ipAddress: payload.ipAddress || 'Unknown',
          osType: 'Windows',
          osVersion: '10/11',
          registeredAt: new Date().toISOString(),
          lastHeartbeat: new Date().toISOString(),
          status: newStatus,
          cpuPercent: 0,
          ramPercent: 0,
          diskPercent: 0,
          uptime: 0,
          hospitalId: 1
        };
        finalServers = [...state.servers, newServer];
      } else {
        if (existingAgent.status === newStatus) return state;
        finalServers = state.servers.map(s => {
          if (s.agentId === payload.agentId) return { ...s, status: newStatus };
          return s;
        });
      }

      let onlineDiff = 0;
      let offlineDiff = 0;
      if (existingAgent) {
        if (existingAgent.status === 'online') onlineDiff -= 1;
        if (existingAgent.status === 'offline') offlineDiff -= 1;
      } else {
        // Brand new agent: count it
        offlineDiff += 1;
      }
      if (newStatus === 'online') onlineDiff += 1;
      if (newStatus === 'offline') offlineDiff += 1;

      const updatedOverview = state.overview ? {
        ...state.overview,
        serverCount: finalServers.length,
        onlineCount: Math.max(0, state.overview.onlineCount + onlineDiff),
        offlineCount: Math.max(0, state.overview.offlineCount + offlineDiff)
      } : null;

      return {
        ...state,
        servers: finalServers,
        overview: updatedOverview
      };
    }

    case 'ACKNOWLEDGE_ALERT': {
      const { alertId, acknowledgedBy, acknowledgedAt } = action.payload;
      const updatedAlerts = state.alerts.map(a => {
        if (a.id === alertId) {
          return {
            ...a,
            isAcknowledged: true,
            acknowledgedAt,
            acknowledgedBy
          };
        }
        return a;
      });

      const alert = state.alerts.find(a => a.id === alertId);
      const isCritical = alert?.severity === 'critical';
      const isWarning = alert?.severity === 'warning';

      const updatedOverview = state.overview ? {
        ...state.overview,
        criticalAlertCount: isCritical ? Math.max(0, state.overview.criticalAlertCount - 1) : state.overview.criticalAlertCount,
        warningAlertCount: isWarning ? Math.max(0, state.overview.warningAlertCount - 1) : state.overview.warningAlertCount,
        recentAlerts: state.overview.recentAlerts.map(a => a.id === alertId ? { ...a, isAcknowledged: true, acknowledgedBy, acknowledgedAt } : a)
      } : null;

      return {
        ...state,
        alerts: updatedAlerts,
        overview: updatedOverview
      };
    }

    case 'INCIDENT_CREATED': {
      const payload = action.payload;
      if (state.incidents.some(i => i.id === payload.id)) {
        return state;
      }
      return {
        ...state,
        incidents: [payload, ...state.incidents]
      };
    }

    case 'INCIDENT_UPDATED':
    case 'INCIDENT_ASSIGNED':
    case 'INCIDENT_STATUS_CHANGED':
    case 'INCIDENT_CLOSED': {
      const payload = action.payload;
      return {
        ...state,
        incidents: state.incidents.map(i => i.id === payload.id ? payload : i)
      };
    }

    default:
      return state;
  }
};

export interface NotificationContextType {
  state: DashboardState;
  showToast: (message: string, severity: 'critical' | 'warning' | 'success') => void;
  toast: { message: string; severity: 'critical' | 'warning' | 'success' } | null;
  loadInitialData: () => Promise<void>;
  acknowledgeAlert: (alertId: number, username: string) => Promise<void>;
  retryConnection: () => void;
}

export const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

interface NotificationProviderProps {
  token: string | null;
  children: ReactNode;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ token, children }) => {
  const [state, dispatch] = useReducer(notificationReducer, initialState);
  const [toast, setToast] = React.useState<{ message: string; severity: 'critical' | 'warning' | 'success' } | null>(null);
  const signalrServiceRef = useRef<SignalRService | null>(null);

  const showToast = (message: string, severity: 'critical' | 'warning' | 'success') => {
    setToast({ message, severity });
    setTimeout(() => setToast(null), 5000);
  };

  const authFetch = async (url: string, options: RequestInit = {}) => {
    const headers = new Headers(options.headers || {});
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    
    // BỔ SUNG: Truyền ID bệnh viện giả lập từ LocalStorage lên Header cho mọi request của Context
    const selectedHospitalId = localStorage.getItem('selectedHospitalId') || '';
    if (selectedHospitalId) {
      headers.set('X-Hospital-Id', selectedHospitalId);
    }
    
    const res = await fetch(url, { ...options, headers });
    return res;
  };

  const loadInitialData = async (isReconnect = false) => {
    if (!token) return;
    try {
      console.log(isReconnect ? 'Syncing data after reconnect...' : 'Loading initial dashboard data...');
      
      const [summaryRes, alertsRes, serversRes, eventsRes, incidentsRes] = await Promise.all([
        authFetch('/api/v1/dashboard/summary'),
        authFetch('/api/v1/dashboard/recent-alerts'),
        authFetch('/api/v1/dashboard/agent-status'),
        authFetch('/api/v1/dashboard/recent-events'),
        authFetch('/api/v1/incidents?page=1&pageSize=100'),
        authFetch('/api/v1/hospitals')
      ]);

      if (summaryRes.ok && alertsRes.ok && serversRes.ok && eventsRes.ok && incidentsRes.ok) {
        const summaryData = await summaryRes.json();
        const alertsData = await alertsRes.json();
        const serversData = await serversRes.json();
        const eventsData = await eventsRes.json();
        const incidentsData = await incidentsRes.json();

        // Map database agents
        const mappedServers: MonitoredServer[] = serversData.map((s: any, idx: number) => ({
          id: idx + 1,
          agentId: s.agentId,
          hostname: s.hostname,
          ipAddress: s.ipAddress,
          osType: 'Windows',
          osVersion: '10/11',
          registeredAt: new Date().toISOString(),
          lastHeartbeat: s.lastSeenAt,
          status: s.status?.toLowerCase() === 'online' ? 'online' : (s.status?.toLowerCase() === 'warning' ? 'warning' : 'offline'),
          cpuPercent: 0,
          ramPercent: 0,
          diskPercent: 0,
          uptime: 0,
          hospitalId: 1
        }));

        // Map database alerts
        const mappedAlerts: Alert[] = alertsData.map((a: any) => ({
          id: a.id,
          serverId: mappedServers.find(s => s.agentId === a.agentId)?.id ?? 1,
          serverHostname: a.agentHostname || 'Unknown',
          hospitalName: 'Hospital A',
          ruleName: a.ruleName,
          severity: (a.severity === 'high' || a.severity === 'critical') ? 'critical' : 'warning',
          title: a.title,
          message: a.message,
          category: a.category,
          createdAt: a.createdAt,
          isAcknowledged: a.isAcknowledged,
          telegramSent: false,
          incidentId: a.incidentId
        }));

        // Map database events
        const mappedEvents: SecurityEvent[] = eventsData.map((e: any, idx: number) => ({
          id: idx + 1,
          eventId: e.eventId,
          serverId: mappedServers.find(s => s.agentId === e.agentId)?.id ?? 1,

          serverHostname: e.agentHostname || 'Unknown',
          hospitalName: 'Hospital A',
          timestamp: e.timestamp,
          category: e.category,
          severity: (e.severity === 'high' || e.severity === 'critical') ? 'critical' : (e.severity === 'info' ? 'info' : 'warning'),
          source: e.source,
          title: e.title,
          details: e.details,
          rawData: '{}',
          receivedAt: e.receivedAt
        }));

        const overview: OverviewData = {
          serverCount: summaryData.totalAgents,
          onlineCount: summaryData.onlineAgents,
          warningCount: 0,
          offlineCount: summaryData.offlineAgents,
          criticalAlertCount: summaryData.unresolvedAlerts,
          warningAlertCount: 0,
          eventCount24H: summaryData.totalEvents,
          recentAlerts: mappedAlerts.slice(0, 5),
          timeline: []
        };

        const payload = {
          servers: mappedServers,
          alerts: mappedAlerts,
          logsList: mappedEvents,
          overview,
          incidents: incidentsData.items || []
        };

        if (isReconnect) {
          dispatch({ type: 'SYNC_AFTER_RECONNECT', payload });
          showToast('Data synchronized successfully.', 'success');
        } else {
          dispatch({ type: 'INITIALIZE', payload });
        }
      }
    } catch (err) {
      console.error('Failed to load initial data:', err);
      showToast('Error loading dashboard data.', 'critical');
    }
  };

  const acknowledgeAlert = async (alertId: number, username: string) => {
    try {
      const res = await authFetch(`/api/v1/alerts/${alertId}/acknowledge`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' }
      });
      if (res.ok) {
        dispatch({
          type: 'ACKNOWLEDGE_ALERT',
          payload: {
            alertId,
            acknowledgedBy: username,
            acknowledgedAt: new Date().toISOString()
          }
        });
        showToast('Alert acknowledged successfully.', 'success');
      } else {
        showToast('Failed to acknowledge alert.', 'warning');
      }
    } catch (err) {
      console.error(err);
      showToast('Connection error acknowledging alert.', 'critical');
    }
  };

  const initSignalR = () => {
    if (!token) return;

    dispatch({ type: 'SIGNALR_CONNECTING' });

    const service = new SignalRService(token);

    service.onConnectedCallback = (date) => {
      dispatch({ type: 'SIGNALR_CONNECTED', payload: { lastConnectedAt: date } });
    };

    service.onDisconnectedCallback = () => {
      dispatch({ type: 'SIGNALR_DISCONNECTED' });
    };

    service.onReconnectingCallback = () => {
      dispatch({ type: 'SIGNALR_RECONNECTING', payload: { attempts: 1 } });
    };

    service.onReconnectedCallback = () => {
      loadInitialData(true);
    };

    service.onClosedCallback = () => {
      dispatch({ type: 'SIGNALR_DISCONNECTED' });
    };

    service.start();

    // Register all Hub events
   // Lắng nghe trạng thái Agent (Heartbeat)
    service.registerHandler(HubEvents.HeartbeatUpdated, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return; // Chặn tín hiệu khác chi nhánh
      
      dispatch({ type: 'HEARTBEAT_UPDATED', payload });
    });

    // Lắng nghe Metric CPU/RAM
    service.registerHandler(HubEvents.MetricUpdated, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;
      
      dispatch({ type: 'METRIC_UPDATED', payload });
    });

    // Lắng nghe Cảnh báo mới (Alert)
    service.registerHandler(HubEvents.AlertCreated, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;
      
      dispatch({ type: 'ALERT_CREATED', payload });
    });

    // Lắng nghe Sự kiện bảo mật (Security Event)
    service.registerHandler(HubEvents.SecurityEventCreated, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;
      
      dispatch({ type: 'SECURITY_EVENT_CREATED', payload });
    });

    // Lắng nghe trạng thái Online/Offline
    service.registerHandler(HubEvents.AgentStatusChanged, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;

      dispatch({ type: 'AGENT_STATUS_CHANGED', payload });
    });

    // Lắng nghe các sự kiện Response Action gửi từ Server
    service.registerHandler(HubEvents.ResponseCreated, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;

      window.dispatchEvent(new CustomEvent('onesecurity-response-updated', { detail: payload }));
      showToast(`New response action ${payload.actionType} requested.`, 'warning');
    });

    service.registerHandler(HubEvents.ResponseStarted, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;

      window.dispatchEvent(new CustomEvent('onesecurity-response-updated', { detail: payload }));
    });

    service.registerHandler(HubEvents.ResponseUpdated, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;

      window.dispatchEvent(new CustomEvent('onesecurity-response-updated', { detail: payload }));
    });

    service.registerHandler(HubEvents.ResponseCompleted, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;

      window.dispatchEvent(new CustomEvent('onesecurity-response-updated', { detail: payload }));
      showToast(`Response action ${payload.actionType} succeeded.`, 'success');
    });

    service.registerHandler(HubEvents.ResponseFailed, (payload) => {
      const selected = localStorage.getItem('selectedHospitalId');
      if (selected && payload.hospitalId && payload.hospitalId.toString() !== selected) return;

      window.dispatchEvent(new CustomEvent('onesecurity-response-updated', { detail: payload }));
      showToast(`Response action ${payload.actionType} failed.`, 'critical');
    });

    signalrServiceRef.current = service;
  };

  const retryConnection = () => {
    if (signalrServiceRef.current) {
      signalrServiceRef.current.stop().then(() => {
        signalrServiceRef.current = null;
        initSignalR();
      });
    } else {
      initSignalR();
    }
  };

  useEffect(() => {
    if (!token) {
      // Clear data and stop SignalR on logout
      dispatch({ 
        type: 'INITIALIZE', 
        payload: { 
          servers: [], 
          alerts: [], 
          logsList: [], 
          overview: {
            serverCount: 0,
            onlineCount: 0,
            warningCount: 0,
            offlineCount: 0,
            criticalAlertCount: 0,
            warningAlertCount: 0,
            eventCount24H: 0,
            recentAlerts: [],
            timeline: []
          },
          incidents: []
        } 
      });
      if (signalrServiceRef.current) {
        signalrServiceRef.current.stop();
        signalrServiceRef.current = null;
      }
      return;
    }

    // Mounted initial sequencing: REST Fetch -> INITIALIZE -> SignalR Connect
    loadInitialData().then(() => {
      initSignalR();
    });

    return () => {
      if (signalrServiceRef.current) {
        signalrServiceRef.current.stop();
        signalrServiceRef.current = null;
      }
    };
  }, [token]);

  return (
    <NotificationContext.Provider value={{ state, showToast, toast, loadInitialData, acknowledgeAlert, retryConnection }}>
      {children}
    </NotificationContext.Provider>
  );
};
