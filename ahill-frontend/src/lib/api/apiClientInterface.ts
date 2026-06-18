import type {
  Branch, Employee, Position, User, Client, Patient, Service, ServicePrice,
  Material, Batch, Diagnosis, Appointment, Payment, AuditEntry, AppointmentStatus
} from '../types';

export interface ApiClient {
  auth: {
    login: (username: string, password: string) => Promise<{ user: User; employee?: Employee }>;
    logout: () => Promise<void>;
  };
  branches: {
    list: () => Promise<Branch[]>;
    save: (data: Omit<Branch, 'id'> & { id?: string }) => Promise<Branch>;
  };
  positions: {
    list: () => Promise<Position[]>;
  };
  employees: {
    list: () => Promise<Employee[]>;
    save: (data: Employee) => Promise<Employee>;
  };
  users: {
    list: () => Promise<User[]>;
    save: (data: Omit<User, 'id'> & { id?: string }) => Promise<User>;
  };
  clients: {
    list: () => Promise<Client[]>;
    get: (id: string) => Promise<Client | undefined>;
    save: (data: Omit<Client, 'id' | 'createdAt'> & { id?: string }) => Promise<Client>;
  };
  patients: {
    list: (clientId?: string) => Promise<Patient[]>;
    get: (id: string) => Promise<Patient | undefined>;
    save: (data: Omit<Patient, 'id'> & { id?: string }) => Promise<Patient>;
  };
  services: {
    list: () => Promise<Service[]>;
  };
  servicePrices: {
    list: () => Promise<ServicePrice[]>;
    addPrice: (serviceId: string, price: number) => Promise<ServicePrice>;
  };
  materials: {
    list: () => Promise<Material[]>;
    get: (id: string) => Promise<Material | undefined>;
  };
  batches: {
    list: () => Promise<Batch[]>;
    save: (data: Omit<Batch, 'id'> & { id?: string }) => Promise<Batch>;
  };
  diagnoses: {
    list: () => Promise<Diagnosis[]>;
    save: (data: Diagnosis) => Promise<Diagnosis>;
    delete: (id: string) => Promise<void>;
  };
  appointments: {
    list: () => Promise<Appointment[]>;
    get: (id: string) => Promise<Appointment | undefined>;
    save: (data: Omit<Appointment, 'id' | 'createdAt'> & { id?: string }) => Promise<Appointment>;
  };
  payments: {
    list: () => Promise<Payment[]>;
    forAppointment: (appointmentId: string) => Promise<Payment[]>;
    add: (data: Omit<Payment, 'id' | 'paymentDate'>) => Promise<Payment>;
  };
  audit: {
    list: () => Promise<AuditEntry[]>;
    log: (username: string, action: string, details: string) => Promise<void>;
  };
}
