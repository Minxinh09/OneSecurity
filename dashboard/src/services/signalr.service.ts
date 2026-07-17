import * as signalR from '@microsoft/signalr';

export const HubEvents = {
  AlertCreated: 'AlertCreated',
  MetricUpdated: 'MetricUpdated',
  HeartbeatUpdated: 'HeartbeatUpdated',
  SecurityEventCreated: 'SecurityEventCreated',
  AgentStatusChanged: 'AgentStatusChanged',
  IncidentCreated: 'IncidentCreated',
  IncidentUpdated: 'IncidentUpdated',
  IncidentAssigned: 'IncidentAssigned',
  IncidentStatusChanged: 'IncidentStatusChanged',
  IncidentClosed: 'IncidentClosed',
  DashboardOverviewUpdated: 'DashboardOverviewUpdated',
  ResponseCreated: 'ResponseCreated',
  ResponseStarted: 'ResponseStarted',
  ResponseUpdated: 'ResponseUpdated',
  ResponseCompleted: 'ResponseCompleted',
  ResponseFailed: 'ResponseFailed'
} as const;

export type HubEventType = keyof typeof HubEvents;

export type ConnectionStatus = 'loading' | 'connected' | 'reconnecting' | 'disconnected';

export interface NotificationState {
  connectionStatus: ConnectionStatus;
  lastConnectedAt?: Date;
  reconnectAttempts: number;
}

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private token: string;
  private url: string;

  public onConnectedCallback?: (date: Date) => void;
  public onDisconnectedCallback?: () => void;
  public onReconnectingCallback?: (error?: Error) => void;
  public onReconnectedCallback?: (connectionId?: string) => void;
  public onClosedCallback?: (error?: Error) => void;

  constructor(token: string, url: string = '/hubs/security') {
    this.token = token;
    this.url = url;
  }

  public start(): void {
    if (this.connection) {
      console.warn('SignalR connection is already initialized.');
      return;
    }

    console.log(`Starting SignalR connection to ${this.url}...`);

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.url, {
        accessTokenFactory: () => this.token
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Retry delay: 2s, 5s, 10s, 30s thereafter
          if (retryContext.previousRetryCount === 0) return 2000;
          if (retryContext.previousRetryCount === 1) return 5000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set connection lifecycle callbacks
    this.connection.onreconnecting((error) => {
      console.warn('SignalR connection is reconnecting due to error:', error);
      if (this.onReconnectingCallback) {
        this.onReconnectingCallback(error);
      }
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR connection successfully reconnected. ConnectionId:', connectionId);
      if (this.onReconnectedCallback) {
        this.onReconnectedCallback(connectionId);
      }
    });

    this.connection.onclose((error) => {
      console.error('SignalR connection was closed. Error:', error);
      if (this.onClosedCallback) {
        this.onClosedCallback(error);
      }
    });

    this.connection.start()
      .then(() => {
        console.log('SignalR connection established successfully.');
        if (this.onConnectedCallback) {
          this.onConnectedCallback(new Date());
        }
        // Invoke JoinDashboard if required by legacy logic
        this.connection?.invoke('JoinDashboard')
          .catch(err => console.error('Failed to invoke JoinDashboard:', err));
      })
      .catch((error) => {
        console.error('SignalR connection failed to start:', error);
        if (this.onDisconnectedCallback) {
          this.onDisconnectedCallback();
        }
      });
  }

  public registerHandler(eventName: string, handler: (...args: any[]) => void): void {
    if (!this.connection) {
      console.error('Cannot register event handler before starting connection.');
      return;
    }
    this.connection.on(eventName, handler);
  }

  public unregisterHandler(eventName: string, handler: (...args: any[]) => void): void {
    if (this.connection) {
      this.connection.off(eventName, handler);
    }
  }

  public stop(): Promise<void> {
    if (!this.connection) {
      return Promise.resolve();
    }
    console.log('Stopping SignalR connection...');
    return this.connection.stop()
      .then(() => {
        this.connection = null;
        console.log('SignalR connection stopped.');
      })
      .catch((err) => {
        console.error('Error stopping SignalR connection:', err);
      });
  }
}
