import React, { useState, useEffect, useContext } from 'react';
import { 
  Shield, 
  ShieldAlert, 
  CheckCircle, 
  XCircle, 
  Clock, 
  User, 
  Server, 
  RefreshCw, 
  Search,
  Terminal,
  Activity,
  AlertTriangle,
  Play,
  RotateCcw
} from 'lucide-react';
import { useNotifications } from '../hooks/useNotifications';

interface ResponseActionDto {
  id: number;
  incidentId: number;
  agentId: string;
  agentHostname: string;
  actionType: string;
  status: string;
  requestedByUserId: string;
  requestedByUserName: string;
  approvedByUserId: string | null;
  approvedByUserName: string | null;
  requestedAt: string;
  startedAt: string | null;
  completedAt: string | null;
  resultMessage: string | null;
  correlationId: string;
  metadata: string | null;
}

export const ResponseCenter: React.FC = () => {
  const { showToast } = useNotifications();
  const [activeTab, setActiveTab] = useState<'pending' | 'history'>('pending');
  const [loading, setLoading] = useState(false);
  const [actions, setActions] = useState<ResponseActionDto[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  
  // Filtering & Pagination
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [statusFilter, setStatusFilter] = useState('');
  const [actionTypeFilter, setActionTypeFilter] = useState('');
  const [agentIdFilter, setAgentIdFilter] = useState('');

  // Current logged in user info for role checks
  const currentUser = (() => {
    try {
      const saved = localStorage.getItem('user');
      return saved ? JSON.parse(saved) : null;
    } catch {
      return null;
    }
  })();

  const isAdmin = currentUser?.role === 'Administrator';

  const fetchActions = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      let url = `/api/v1/responses?page=${page}&pageSize=${pageSize}`;
      
      if (activeTab === 'pending') {
        url += '&status=Pending';
      } else {
        if (statusFilter) url += `&status=${statusFilter}`;
        if (actionTypeFilter) url += `&actionType=${actionTypeFilter}`;
      }
      if (agentIdFilter) url += `&agentId=${agentIdFilter}`;

      const res = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (res.ok) {
        const data = await res.json();
        setActions(data.items || []);
        setTotalItems(data.totalItems || 0);
      } else {
        showToast('Failed to fetch response actions history.', 'critical');
      }
    } catch (err) {
      console.error(err);
      showToast('Connection error while fetching response actions.', 'critical');
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (id: number) => {
    if (!isAdmin) {
      showToast('Only Administrators can approve response actions.', 'critical');
      return;
    }
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`/api/v1/responses/${id}/approve`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (res.ok) {
        showToast('Response action approved and queued for dispatch.', 'success');
        fetchActions();
      } else {
        const errorData = await res.json();
        showToast(`Approval failed: ${errorData.message || 'Error'}`, 'critical');
      }
    } catch (err) {
      console.error(err);
      showToast('Error approving response action.', 'critical');
    }
  };

  const handleCancel = async (id: number) => {
    if (!isAdmin) {
      showToast('Only Administrators can cancel response actions.', 'critical');
      return;
    }
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`/api/v1/responses/${id}/cancel`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (res.ok) {
        showToast('Response action cancelled successfully.', 'success');
        fetchActions();
      } else {
        const errorData = await res.json();
        showToast(`Cancellation failed: ${errorData.message || 'Error'}`, 'critical');
      }
    } catch (err) {
      console.error(err);
      showToast('Error cancelling response action.', 'critical');
    }
  };

  // Real-time integration via custom event
  useEffect(() => {
    const handleUpdate = () => {
      fetchActions();
    };

    window.addEventListener('onesecurity-response-updated', handleUpdate);
    return () => {
      window.removeEventListener('onesecurity-response-updated', handleUpdate);
    };
  }, [page, activeTab, statusFilter, actionTypeFilter, agentIdFilter]);

  // Fetch actions when tab or pagination filters change
  useEffect(() => {
    setPage(1);
    fetchActions();
  }, [activeTab]);

  useEffect(() => {
    fetchActions();
  }, [page, statusFilter, actionTypeFilter, agentIdFilter]);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Pending':
        return (
          <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/20 text-amber-300 border border-amber-500/30">
            <Clock className="w-3.5 h-3.5" /> Pending Approval
          </span>
        );
      case 'Queued':
        return (
          <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-500/20 text-blue-300 border border-blue-500/30">
            <Activity className="w-3.5 h-3.5" /> Queued
          </span>
        );
      case 'Executing':
        return (
          <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 animate-pulse">
            <RefreshCw className="w-3.5 h-3.5 animate-spin" /> Executing
          </span>
        );
      case 'Succeeded':
        return (
          <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
            <CheckCircle className="w-3.5 h-3.5" /> Succeeded
          </span>
        );
      case 'Failed':
        return (
          <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-500/20 text-rose-300 border border-rose-500/30">
            <XCircle className="w-3.5 h-3.5" /> Failed
          </span>
        );
      case 'Cancelled':
        return (
          <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-500/20 text-zinc-400 border border-zinc-500/30">
            <XCircle className="w-3.5 h-3.5" /> Cancelled
          </span>
        );
      default:
        return (
          <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-500/10 text-zinc-400">
            {status}
          </span>
        );
    }
  };

  const getActionBadge = (action: string) => {
    const isDangerous = ['Shutdown', 'Restart', 'IsolateHost', 'RunScript'].includes(action);
    if (isDangerous) {
      return (
        <span className="flex items-center gap-1 px-2 py-0.5 rounded text-xs font-semibold bg-rose-500/10 text-rose-300 border border-rose-500/20">
          <ShieldAlert className="w-3.5 h-3.5" /> {action}
        </span>
      );
    }
    return (
      <span className="flex items-center gap-1 px-2 py-0.5 rounded text-xs font-semibold bg-sky-500/10 text-sky-300 border border-sky-500/20">
        <Terminal className="w-3.5 h-3.5" /> {action}
      </span>
    );
  };

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 border-b border-white/10 pb-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Shield className="w-7 h-7 text-indigo-400" /> Active Response Center
          </h1>
          <p className="text-sm text-zinc-400 mt-1">
            Audit, authorize, and trigger remote remediation actions across monitored servers.
          </p>
        </div>
        <button 
          onClick={fetchActions}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2 bg-white/5 hover:bg-white/10 text-white rounded-lg border border-white/10 transition duration-200 text-sm font-medium"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>

      {/* Tabs Selector */}
      <div className="flex border-b border-white/10">
        <button
          onClick={() => setActiveTab('pending')}
          className={`px-5 py-3 text-sm font-medium transition relative -bottom-[1px] ${
            activeTab === 'pending'
              ? 'text-indigo-400 border-b-2 border-indigo-400'
              : 'text-zinc-400 hover:text-zinc-300'
          }`}
        >
          Pending Approvals
          {activeTab !== 'pending' && actions.length > 0 && (
            <span className="ml-2 px-1.5 py-0.2 bg-amber-500 text-black text-[10px] font-bold rounded-full">
              Pending
            </span>
          )}
        </button>
        <button
          onClick={() => setActiveTab('history')}
          className={`px-5 py-3 text-sm font-medium transition relative -bottom-[1px] ${
            activeTab === 'history'
              ? 'text-indigo-400 border-b-2 border-indigo-400'
              : 'text-zinc-400 hover:text-zinc-300'
          }`}
        >
          Execution History
        </button>
      </div>

      {/* Filters (only for History) */}
      {activeTab === 'history' && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 p-4 rounded-xl bg-white/5 border border-white/10">
          <div>
            <label className="block text-xs font-semibold text-zinc-400 uppercase tracking-wider mb-2">Status</label>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="w-full bg-zinc-900 border border-white/10 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500"
            >
              <option value="">All Statuses</option>
              <option value="Queued">Queued</option>
              <option value="Executing">Executing</option>
              <option value="Succeeded">Succeeded</option>
              <option value="Failed">Failed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-semibold text-zinc-400 uppercase tracking-wider mb-2">Action Type</label>
            <select
              value={actionTypeFilter}
              onChange={(e) => setActionTypeFilter(e.target.value)}
              className="w-full bg-zinc-900 border border-white/10 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500"
            >
              <option value="">All Actions</option>
              <option value="Shutdown">Shutdown</option>
              <option value="Restart">Restart</option>
              <option value="CollectDiagnostics">Collect Diagnostics</option>
              <option value="IsolateHost">Isolate Host</option>
              <option value="BlockIPAddress">Block IP Address</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-semibold text-zinc-400 uppercase tracking-wider mb-2">Agent ID</label>
            <div className="relative">
              <input
                type="text"
                placeholder="Search Agent ID..."
                value={agentIdFilter}
                onChange={(e) => setAgentIdFilter(e.target.value)}
                className="w-full bg-zinc-900 border border-white/10 rounded-lg pl-9 pr-3 py-2 text-white text-sm focus:outline-none focus:border-indigo-500"
              />
              <Search className="w-4 h-4 text-zinc-500 absolute left-3 top-3" />
            </div>
          </div>
          <div className="flex items-end">
            <button
              onClick={() => {
                setStatusFilter('');
                setActionTypeFilter('');
                setAgentIdFilter('');
              }}
              className="w-full py-2 bg-white/5 hover:bg-white/10 text-zinc-300 rounded-lg text-sm transition font-medium flex items-center justify-center gap-2 border border-white/10"
            >
              <RotateCcw className="w-4 h-4" /> Reset Filters
            </button>
          </div>
        </div>
      )}

      {/* Main Table Content */}
      <div className="rounded-xl border border-white/10 bg-black/30 backdrop-blur-md overflow-hidden">
        {loading ? (
          <div className="py-20 flex flex-col items-center justify-center gap-3">
            <RefreshCw className="w-8 h-8 text-indigo-400 animate-spin" />
            <span className="text-zinc-400 text-sm">Loading actions...</span>
          </div>
        ) : actions.length === 0 ? (
          <div className="py-20 flex flex-col items-center justify-center text-center gap-3 p-4">
            <Shield className="w-12 h-12 text-zinc-600" />
            <span className="text-zinc-400 font-medium">No actions found.</span>
            <p className="text-xs text-zinc-500 max-w-sm">
              {activeTab === 'pending' 
                ? 'No pending response requests await Administrator approval.'
                : 'No execution history records match your filter criteria.'}
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-white/10 bg-white/5 text-zinc-400 text-xs font-semibold uppercase tracking-wider">
                  <th className="px-6 py-4">Target Host</th>
                  <th className="px-6 py-4">Action</th>
                  <th className="px-6 py-4">Status</th>
                  <th className="px-6 py-4">Requester</th>
                  <th className="px-6 py-4">Requested At</th>
                  {activeTab === 'history' ? (
                    <>
                      <th className="px-6 py-4">Result / Message</th>
                      <th className="px-6 py-4">Correlation ID</th>
                    </>
                  ) : (
                    <th className="px-6 py-4 text-right">Actions</th>
                  )}
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5 text-sm text-white">
                {actions.map((act) => (
                  <tr key={act.id} className="hover:bg-white/[0.02] transition">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <Server className="w-4 h-4 text-indigo-400" />
                        <span className="font-medium">{act.agentHostname}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4">{getActionBadge(act.actionType)}</td>
                    <td className="px-6 py-4">{getStatusBadge(act.status)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-1.5">
                        <User className="w-3.5 h-3.5 text-zinc-500" />
                        <span>{act.requestedByUserName}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-zinc-400">
                      {new Date(act.requestedAt).toLocaleString()}
                    </td>
                    {activeTab === 'history' ? (
                      <>
                        <td className="px-6 py-4 max-w-xs truncate">
                          {act.resultMessage ? (
                            <span 
                              title={act.resultMessage}
                              className={act.status === 'Failed' ? 'text-rose-400' : 'text-emerald-400'}
                            >
                              {act.resultMessage}
                            </span>
                          ) : (
                            <span className="text-zinc-500 italic">No output yet</span>
                          )}
                        </td>
                        <td className="px-6 py-4 text-xs font-mono text-zinc-500">
                          {act.correlationId}
                        </td>
                      </>
                    ) : (
                      <td className="px-6 py-4 text-right">
                        {isAdmin ? (
                          <div className="flex justify-end gap-2">
                            <button
                              onClick={() => handleApprove(act.id)}
                              className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-emerald-500 hover:bg-emerald-600 text-black transition flex items-center gap-1"
                            >
                              <Play className="w-3.5 h-3.5" /> Approve
                            </button>
                            <button
                              onClick={() => handleCancel(act.id)}
                              className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-rose-500/20 hover:bg-rose-500/30 text-rose-300 border border-rose-500/30 transition flex items-center gap-1"
                            >
                              <XCircle className="w-3.5 h-3.5" /> Cancel
                            </button>
                          </div>
                        ) : (
                          <span className="text-xs text-amber-400 flex items-center justify-end gap-1 font-semibold">
                            <AlertTriangle className="w-3.5 h-3.5" /> Awaiting Admin Approval
                          </span>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pagination Footer */}
      {activeTab === 'history' && totalItems > pageSize && (
        <div className="flex justify-between items-center text-sm text-zinc-400 px-2">
          <span>
            Showing <span className="text-white font-medium">{(page - 1) * pageSize + 1}</span> to{' '}
            <span className="text-white font-medium">
              {Math.min(page * pageSize, totalItems)}
            </span>{' '}
            of <span className="text-white font-medium">{totalItems}</span> actions
          </span>
          <div className="flex gap-2">
            <button
              onClick={() => setPage((p) => Math.max(p - 1, 1))}
              disabled={page === 1}
              className="px-3 py-1.5 rounded bg-white/5 hover:bg-white/10 text-white disabled:opacity-30 disabled:hover:bg-white/5 transition"
            >
              Previous
            </button>
            <button
              onClick={() => setPage((p) => (p * pageSize < totalItems ? p + 1 : p))}
              disabled={page * pageSize >= totalItems}
              className="px-3 py-1.5 rounded bg-white/5 hover:bg-white/10 text-white disabled:opacity-30 disabled:hover:bg-white/5 transition"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
