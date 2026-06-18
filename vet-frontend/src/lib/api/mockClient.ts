import { browser } from '$app/environment';
import { mockData } from './mockData';
import type { ApiClient } from './apiClientInterface';
import type { 
  User, Employee, Client, Patient, Service, Material, Batch, Diagnosis, 
  Appointment, Payment, Branch, Position, AuditEntry, ServicePrice
} from '../types';

const STORAGE_KEY = 'vetcrm_mock_db';

function getDb() {
  if (!browser) return mockData;
  const raw = localStorage.getItem(STORAGE_KEY);
  if (raw) return JSON.parse(raw);
  
  const initData = { ...mockData, audit: [] as AuditEntry[] };
  localStorage.setItem(STORAGE_KEY, JSON.stringify(initData));
  return initData;
}

function saveDb(data: any) {
  if (browser) localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}

const generateId = (prefix: string) => prefix + '-mock-' + Math.random().toString(36).substr(2, 6);

// Simulate network delay
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

export const mockApiClient: ApiClient = {
  auth: {
    login: async (username: string, password: string): Promise<{ user: User; employee?: Employee }> => {
      await delay(300);
      if (password !== 'demo123') throw new Error('Неверный пароль. Попробуйте demo123');
      const db = getDb();
      const user = db.users.find((u: User) => u.username === username);
      if (!user) throw new Error('Пользователь не найден');
      const employee = user.employeeId ? db.employees.find((e: Employee) => e.id === user.employeeId) : undefined;
      return { user, employee };
    },
    logout: async (): Promise<void> => {
      await delay(100);
    }
  },
  branches: {
    list: async (): Promise<Branch[]> => getDb().branches,
    save: async (data: any): Promise<Branch> => {
      await delay(200);
      const db = getDb();
      let item = db.branches.find((b: Branch) => b.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('br') }; db.branches.push(item); }
      saveDb(db); return item;
    }
  },
  positions: { list: async (): Promise<Position[]> => getDb().positions },
  employees: {
    list: async (): Promise<Employee[]> => getDb().employees,
    save: async (data: any): Promise<Employee> => {
      await delay(200);
      const db = getDb();
      let item = db.employees.find((e: Employee) => e.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('emp') }; db.employees.push(item); }
      saveDb(db); return item;
    }
  },
  users: {
    list: async (): Promise<User[]> => getDb().users,
    save: async (data: any): Promise<User> => {
      await delay(200);
      const db = getDb();
      let item = db.users.find((u: User) => u.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('usr') }; db.users.push(item); }
      saveDb(db); return item;
    }
  },
  clients: {
    list: async (): Promise<Client[]> => getDb().clients,
    get: async (id: string): Promise<Client | undefined> => getDb().clients.find((c: Client) => c.id === id),
    save: async (data: any): Promise<Client> => {
      await delay(200);
      const db = getDb();
      let item = db.clients.find((c: Client) => c.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('cl'), createdAt: new Date().toISOString().split('T')[0] }; db.clients.push(item); }
      saveDb(db); return item;
    }
  },
  patients: {
    list: async (clientId?: string): Promise<Patient[]> => {
      const all = getDb().patients;
      return clientId ? all.filter((p: Patient) => p.clientId === clientId) : all;
    },
    get: async (id: string): Promise<Patient | undefined> => getDb().patients.find((p: Patient) => p.id === id),
    save: async (data: any): Promise<Patient> => {
      await delay(200);
      const db = getDb();
      let item = db.patients.find((p: Patient) => p.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('pt') }; db.patients.push(item); }
      saveDb(db); return item;
    }
  },
  services: { list: async (): Promise<Service[]> => getDb().services },
  servicePrices: {
    list: async (): Promise<ServicePrice[]> => [],
    addPrice: async (serviceId: string, price: number): Promise<ServicePrice> => {
      await delay(200);
      return { id: generateId('spr'), serviceId, price, activeFrom: new Date().toISOString().split('T')[0] };
    }
  },
  materials: { 
    list: async (): Promise<Material[]> => getDb().materials,
    get: async (id: string): Promise<Material | undefined> => getDb().materials.find((m: Material) => m.id === id)
  },
  batches: { 
    list: async (): Promise<Batch[]> => getDb().batches,
    save: async (data: any): Promise<Batch> => {
      await delay(200);
      const db = getDb();
      let item = db.batches.find((b: Batch) => b.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('bat') }; db.batches.push(item); }
      saveDb(db); return item;
    }
  },
  diagnoses: { 
    list: async (): Promise<Diagnosis[]> => getDb().diagnoses,
    save: async (data: Diagnosis): Promise<Diagnosis> => {
      await delay(200);
      const db = getDb();
      let item = db.diagnoses.find((d: Diagnosis) => d.id === data.id);
      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('dg') }; db.diagnoses.push(item); }
      saveDb(db); return item;
    },
    delete: async (id: string): Promise<void> => {
      await delay(200);
      const db = getDb();
      db.diagnoses = db.diagnoses.filter((d: Diagnosis) => d.id !== id);
      saveDb(db);
    }
  },
  appointments: {
    list: async (): Promise<Appointment[]> => getDb().appointments,
    get: async (id: string): Promise<Appointment | undefined> => getDb().appointments.find((a: Appointment) => a.id === id),
    save: async (data: any): Promise<Appointment> => {
      await delay(300);
      const db = getDb();
      let item = db.appointments.find((a: Appointment) => a.id === data.id);
      
      if (data.status === 'Closed' && (!item || item.status !== 'Closed')) {
        for (const mat of data.materials) {
          const batch = db.batches.find((b: Batch) => b.id === mat.batchId);
          if (batch) batch.remainingQuantity = Math.max(0, batch.remainingQuantity - mat.quantity);
        }
      }

      if (item) Object.assign(item, data);
      else { item = { ...data, id: data.id || generateId('apt'), createdAt: new Date().toISOString().split('T')[0] }; db.appointments.push(item); }
      saveDb(db); return item;
    }
  },
  payments: {
    list: async (): Promise<Payment[]> => getDb().payments,
    forAppointment: async (appointmentId: string): Promise<Payment[]> => getDb().payments.filter((p: Payment) => p.appointmentId === appointmentId),
    add: async (data: any): Promise<Payment> => {
      await delay(200);
      const db = getDb();
      const item = { ...data, id: generateId('pay'), paymentDate: new Date().toISOString().substring(0, 16).replace('T', ' ') };
      db.payments.push(item);
      saveDb(db); return item;
    },
  },
  audit: {
    list: async (): Promise<AuditEntry[]> => getDb().audit || [],
    log: async (username: string, action: string, details: string): Promise<void> => {
      const db = getDb();
      db.audit = db.audit || [];
      db.audit.push({
        id: generateId('aud'),
        timestamp: new Date().toISOString(),
        username,
        action,
        details
      });
      saveDb(db);
    }
  }
};
