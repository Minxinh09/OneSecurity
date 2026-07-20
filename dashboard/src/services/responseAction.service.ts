export interface CreateResponseActionRequest {
  incidentId?: number;
  agentId: string;
  actionType: string;
  parameters?: string;
  metadata?: string;
}

export interface ResponseActionDto {
  id: number;
  incidentId?: number;
  agentId: string;
  agentHostname?: string;
  actionType: string;
  status: string;
  requestedByUserId: string;
  requestedByUserName: string;
  requestedAt: string;
  startedAt?: string;
  completedAt?: string;
  correlationId: string;
  parameters?: string;
  output?: string;
  errorMessage?: string;
  createdBy?: string;
}

const getHeaders = () => {
  const token = localStorage.getItem('token');
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  };
};

export const ResponseActionApi = {
  // Tạo lệnh ứng phó mới
  create: async (request: CreateResponseActionRequest): Promise<ResponseActionDto> => {
    const res = await fetch('/api/v1/response-actions', {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request)
    });
    
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || `Failed to create response action. Status: ${res.status}`);
    }
    return res.json();
  },

  // Lấy danh sách lịch sử lệnh phân trang của Agent
  getPaged: async (params: {
    page?: number;
    pageSize?: number;
    agentId?: string;
    status?: string;
  } = {}): Promise<{ items: ResponseActionDto[]; totalCount: number }> => {
    const query = new URLSearchParams();
    if (params.page) query.append('page', params.page.toString());
    if (params.pageSize) query.append('pageSize', params.pageSize.toString());
    if (params.agentId) query.append('agentId', params.agentId);
    if (params.status) query.append('status', params.status);

    const res = await fetch(`/api/v1/response-actions?${query.toString()}`, {
      headers: getHeaders()
    });
    if (!res.ok) {
      throw new Error(`Failed to fetch response actions list. Status: ${res.status}`);
    }
    return res.json();
  },

  // Hủy lệnh Pending
  cancel: async (id: number): Promise<ResponseActionDto> => {
    const res = await fetch(`/api/v1/response-actions/${id}/cancel`, {
      method: 'POST',
      headers: getHeaders()
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || `Failed to cancel response action. Status: ${res.status}`);
    }
    return res.json();
  }
};