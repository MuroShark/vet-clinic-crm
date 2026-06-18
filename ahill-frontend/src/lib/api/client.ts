import { browser } from '$app/environment';
import { api, setToken } from './http';
import type {
  Branch, Employee, Position, User, Client, Patient, Service, ServicePrice,
  Material, Batch, Diagnosis, Appointment, Payment, AuditEntry
} from '../types';

const STORAGE_PREFIX = 'vetcrm_';

/** Управляет предпочтением активного филиала в localStorage (состояние UI, а не данные домена). */
export class ApiDb {
  static getActiveBranchId = (): string => {
    if (!browser) return 'br-1';
    return localStorage.getItem(STORAGE_PREFIX + 'active_branch_id') || 'br-1';
  };
  static setActiveBranchId = (branchId: string) => {
    if (browser) localStorage.setItem(STORAGE_PREFIX + 'active_branch_id', branchId);
  };
}

const post = (path: string, body: unknown) => ({ method: 'POST', body: JSON.stringify(body) });

/** Все доменные данные извлекаются из бэкенда ASP.NET Core через JWT (см. http.ts). */
export const apiClient = {
  auth: {
    login: async (username: string, password: string): Promise<{ user: User; employee?: Employee }> => {
      const data = await api<{ token: string; user: User; employee?: Employee }>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password })
      });
      setToken(data.token);
      if (browser) localStorage.setItem('vetcrm_auth', JSON.stringify({ user: data.user, employee: data.employee }));
      return { user: data.user, employee: data.employee };
    },
    logout: async (): Promise<void> => {
      setToken(null);
      if (browser) localStorage.removeItem('vetcrm_auth');
    }
  },

  clients: {
    list: (): Promise<Client[]> => api<Client[]>('/api/clients'),
    get: async (id: string): Promise<Client | undefined> => (await api<Client[]>('/api/clients')).find((c) => c.id === id),
    save: (c: Omit<Client, 'id' | 'createdAt'> & { id?: string }): Promise<Client> => api<Client>('/api/clients', post('/api/clients', c) as RequestInit)
  },

  patients: {
    list: async (clientId?: string): Promise<Patient[]> => {
      const all = await api<Patient[]>('/api/patients');
      return clientId ? all.filter((p) => p.clientId === clientId) : all;
    },
    get: async (id: string): Promise<Patient | undefined> => (await api<Patient[]>('/api/patients')).find((p) => p.id === id),
    save: (p: Omit<Patient, 'id'> & { id?: string }): Promise<Patient> => api<Patient>('/api/patients', post('/api/patients', p) as RequestInit)
  },

  employees: {
    list: (): Promise<Employee[]> => api<Employee[]>('/api/employees'),
    save: (e: Employee): Promise<Employee> => api<Employee>('/api/employees', post('/api/employees', e) as RequestInit)
  },

  users: {
    list: (): Promise<User[]> => api<User[]>('/api/users'),
    save: (u: Omit<User, 'id'> & { id?: string }): Promise<User> => api<User>('/api/users', post('/api/users', u) as RequestInit)
  },

  positions: {
    list: (): Promise<Position[]> => api<Position[]>('/api/positions')
  },

  branches: {
    list: (): Promise<Branch[]> => api<Branch[]>('/api/branches'),
    save: (b: Omit<Branch, 'id'> & { id?: string }): Promise<Branch> => api<Branch>('/api/branches', post('/api/branches', b) as RequestInit)
  },

  services: {
    list: (): Promise<Service[]> => api<Service[]>('/api/services')
  },

  servicePrices: {
    list: (): Promise<ServicePrice[]> => api<ServicePrice[]>('/api/servicePrices'),
    addPrice: (serviceId: string, price: number): Promise<ServicePrice> =>
      api<ServicePrice>('/api/servicePrices', post('/api/servicePrices', { serviceId, price }) as RequestInit)
  },

  materials: {
    list: (): Promise<Material[]> => api<Material[]>('/api/materials'),
    get: async (id: string): Promise<Material | undefined> => (await api<Material[]>('/api/materials')).find((m) => m.id === id)
  },

  batches: {
    list: (): Promise<Batch[]> => api<Batch[]>('/api/batches'),
    save: (b: Omit<Batch, 'id'> & { id?: string }): Promise<Batch> => api<Batch>('/api/batches', post('/api/batches', b) as RequestInit)
  },

  diagnoses: {
    list: (): Promise<Diagnosis[]> => api<Diagnosis[]>('/api/diagnoses'),
    save: (d: Diagnosis): Promise<Diagnosis> => api<Diagnosis>('/api/diagnoses', post('/api/diagnoses', d) as RequestInit),
    delete: (id: string): Promise<void> => api<void>(`/api/diagnoses/${id}`, { method: 'DELETE' })
  },

  appointments: {
    list: (): Promise<Appointment[]> => api<Appointment[]>('/api/appointments'),
    get: (id: string): Promise<Appointment | undefined> => api<Appointment>(`/api/appointments/${id}`),
    save: (a: Omit<Appointment, 'id' | 'createdAt'> & { id?: string }): Promise<Appointment> =>
      api<Appointment>('/api/appointments', post('/api/appointments', a) as RequestInit)
  },

  payments: {
    list: (): Promise<Payment[]> => api<Payment[]>('/api/payments'),
    forAppointment: (appointmentId: string): Promise<Payment[]> => api<Payment[]>(`/api/payments?appointmentId=${encodeURIComponent(appointmentId)}`),
    add: (p: Omit<Payment, 'id' | 'paymentDate'>): Promise<Payment> => api<Payment>('/api/payments', post('/api/payments', p) as RequestInit)
  },

  audit: {
    list: (): Promise<AuditEntry[]> => api<AuditEntry[]>('/api/audit'),
    log: (username: string, action: string, details: string): Promise<void> =>
      api<void>('/api/audit', post('/api/audit', { username, action, details }) as RequestInit)
  }
};
