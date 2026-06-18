export interface Branch {
  id: string;
  name: string;
  address: string;
  phone: string;
  isActive?: boolean;
}

export interface Employee {
  id: string;
  name: string;
  positionId: string;
  phone: string;
  email: string;
  KPIRate: number;
  branchIds: string[];
  status: 'active' | 'inactive';
}

export interface Position {
  id: string;
  name: string;
}

export type Role = 'receptionist' | 'vet' | 'chief_vet' | 'director';

export interface User {
  id: string;
  username: string;
  employeeId?: string;
  roles: Role[];
  status: 'active' | 'inactive';
}

export interface Client {
  id: string;
  name: string;
  phone: string;
  email: string;
  consentSigned: boolean;
  createdAt: string;
}

export interface Patient {
  id: string;
  clientId: string;
  name: string;
  species: string;
  breed: string;
  gender: 'M' | 'F';
  birthDate: string;
  weight: number;
  color: string;
  chipNumber?: string;
}

export interface Service {
  id: string;
  category: string;
  name: string;
  defaultPrice: number;
}

export interface ServicePrice {
  id: string;
  serviceId: string;
  price: number;
  activeFrom: string;
}

export interface Material {
  id: string;
  name: string;
  sku: string;
  unit: string;
  category: string;
}

export interface Batch {
  id: string;
  materialId: string;
  supplier: string;
  purchasePrice: number;
  clientPrice: number;
  totalQuantity: number;
  remainingQuantity: number;
  expiryDate: string;
  receivedAt: string;
  branchId: string;
}

export interface Diagnosis {
  id: string;
  code: string;
  name: string;
  category: string;
}

export interface AppointmentService {
  serviceId: string;
  quantity: number;
  priceSnapshot: number;
}

export interface AppointmentMaterial {
  materialId: string;
  batchId: string;
  quantity: number;
  unitCostSnapshot: number;
  clientPriceSnapshot: number;
}

export interface AppointmentDiagnosis {
  diagnosisId: string;
  isPreliminary: boolean;
  doctorComment: string;
}

export type AppointmentStatus = 'Planned' | 'InProgress' | 'Closed' | 'Cancelled';

export interface Appointment {
  id: string;
  clientId: string;
  patientId: string;
  vetId: string;
  branchId: string;
  appointmentDate: string;
  timeSlot: string;
  status: AppointmentStatus;
  notes: string;
  services: AppointmentService[];
  materials: AppointmentMaterial[];
  diagnoses: AppointmentDiagnosis[];
  totalAmount: number;
  closedAt?: string;
  createdAt: string;
}

export interface Payment {
  id: string;
  appointmentId: string;
  paymentDate: string;
  amount: number;
  method: 'cash' | 'card' | 'transfer';
  notes?: string;
}

export interface AuditEntry {
  id: string;
  timestamp: string;
  username: string;
  action: string;
  details: string;
}
