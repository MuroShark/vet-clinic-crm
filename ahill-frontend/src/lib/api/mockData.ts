import type { Branch, Position, Role, User, Employee, Client, Patient, Service, Material, Batch, Diagnosis, Appointment, Payment } from '../types';

export const mockData = {
  branches: [
    { id: 'br-1', name: 'Центральный филиал', address: 'г. Москва, ул. Дубнинская, д. 32', phone: '+7 (495) 555-0121', isActive: true },
    { id: 'br-2', name: 'Северный филиал', address: 'г. Москва, ул. Белозерская, д. 17Г', phone: '+7 (495) 555-0122', isActive: true }
  ] as Branch[],

  positions: [
    { id: 'pos-1', name: 'Главный врач' },
    { id: 'pos-2', name: 'Директор клиники' },
    { id: 'pos-3', name: 'Врач-терапевт' },
    { id: 'pos-4', name: 'Администратор регистратуры' },
    { id: 'pos-5', name: 'Врач-хирург' },
    { id: 'pos-6', name: 'Онколог' },
    { id: 'pos-7', name: 'Ратолог' }
  ] as Position[],

  employees: [
    { id: 'emp-1', name: 'Соколов Андрей Петрович', phone: '+7 (495) 111-2201', email: 'sokolovap@ahill.ru', branchIds: ['br-1'], positionId: 'pos-1', KPIRate: 0.35, status: 'active' },
    { id: 'emp-2', name: 'Абрамов Дмитрий Александрович', phone: '+7 (495) 111-2202', email: 'abramovda@ahill.ru', branchIds: ['br-1'], positionId: 'pos-2', KPIRate: 0.30, status: 'active' },
    { id: 'emp-3', name: 'Карабанова Елена Александровна', phone: '+7 (495) 111-2203', email: 'karabanovaea@ahill.ru', branchIds: ['br-1'], positionId: 'pos-3', KPIRate: 0.25, status: 'active' },
    { id: 'emp-4', name: 'Бенидзе Елизавета Игоревна', phone: '+7 (495) 111-2204', email: 'benidzeei@ahill.ru', branchIds: ['br-1'], positionId: 'pos-4', KPIRate: 0.00, status: 'active' },
    { id: 'emp-5', name: 'Фридман Артём Игоревич', phone: '+7 (495) 111-2205', email: 'fridmanai@ahill.ru', branchIds: ['br-1'], positionId: 'pos-5', KPIRate: 0.30, status: 'active' },
    { id: 'emp-6', name: 'Юпатова Екатерина Владимировна', phone: '+7 (495) 111-2206', email: 'yupatovaev@ahill.ru', branchIds: ['br-1'], positionId: 'pos-6', KPIRate: 0.28, status: 'active' },
    { id: 'emp-7', name: 'Брюхно Виктор Васильевич', phone: '+7 (495) 111-2207', email: 'bryuhnovv@ahill.ru', branchIds: ['br-2'], positionId: 'pos-7', KPIRate: 0.27, status: 'active' }
  ] as Employee[],

  users: [
    { id: 'usr-1', username: 'abramovda', employeeId: 'emp-2', roles: ['director', 'chief_vet', 'vet', 'receptionist'], status: 'active' },
    { id: 'usr-2', username: 'benidzeei', employeeId: 'emp-4', roles: ['receptionist'], status: 'active' },
    { id: 'usr-3', username: 'karabanovaea', employeeId: 'emp-3', roles: ['vet'], status: 'active' },
    { id: 'usr-4', username: 'sokolovap', employeeId: 'emp-1', roles: ['vet', 'chief_vet'], status: 'active' },
    { id: 'usr-5', username: 'bryuhnovv', employeeId: 'emp-7', roles: ['vet'], status: 'active' }
  ] as User[],

  services: [
    { id: 'srv-1', category: 'Терапия', name: 'Первичный приём', defaultPrice: 950 },
    { id: 'srv-2', category: 'Терапия', name: 'Повторный приём', defaultPrice: 600 },
    { id: 'srv-3', category: 'Терапия', name: 'Вакцинация', defaultPrice: 1200 },
    { id: 'srv-4', category: 'Диагностика', name: 'УЗИ органов брюшной полости', defaultPrice: 2000 },
    { id: 'srv-5', category: 'Диагностика', name: 'Общий анализ крови', defaultPrice: 1100 },
    { id: 'srv-6', category: 'Стоматология', name: 'Чистка зубов (ультразвук)', defaultPrice: 4500 },
    { id: 'srv-7', category: 'Хирургия', name: 'Кастрация (кот)', defaultPrice: 2400 },
    { id: 'srv-8', category: 'Хирургия', name: 'Стерилизация (кошка)', defaultPrice: 5200 }
  ] as Service[],

  diagnoses: [
    { id: 'dg-1', code: 'ДЫХ', name: 'Респираторные заболевания', category: 'Терапия' },
    { id: 'dg-2', code: 'ЖКТ', name: 'Заболевания ЖКТ', category: 'Гастроэнтерология' },
    { id: 'dg-3', code: 'ДЕРМ', name: 'Кожные заболевания', category: 'Дерматология' },
    { id: 'dg-4', code: 'ОРТО', name: 'Ортопедические заболевания', category: 'Ортопедия' },
    { id: 'dg-5', code: 'КАРД', name: 'Кардиомиопатия', category: 'Кардиология' },
    { id: 'dg-6', code: 'ПРОФ', name: 'Профилактика (здоров)', category: 'Терапия' }
  ] as Diagnosis[],

  materials: [
    { id: 'mat-1', name: 'Шприц 2мл', sku: 'MAT-001', unit: 'шт', category: 'Расходные материалы' },
    { id: 'mat-2', name: 'Шприц 5мл', sku: 'MAT-002', unit: 'шт', category: 'Расходные материалы' },
    { id: 'mat-3', name: 'Перчатки нитриловые M', sku: 'MAT-003', unit: 'пара', category: 'Расходные материалы' },
    { id: 'mat-4', name: 'Нобивак DHPPi', sku: 'MAT-004', unit: 'доза', category: 'Препараты' },
    { id: 'mat-5', name: 'Нобивак Rabies', sku: 'MAT-005', unit: 'доза', category: 'Препараты' },
    { id: 'mat-6', name: 'Физраствор NaCl 0.9% 200мл', sku: 'MAT-006', unit: 'фл', category: 'Препараты' }
  ] as Material[],

  batches: [
    { id: 'bat-1', materialId: 'mat-1', supplier: 'ЗооМедСнаб', purchasePrice: 5, clientPrice: 15, totalQuantity: 1000, remainingQuantity: 820, expiryDate: '2026-12-01', receivedAt: '2024-05-01', branchId: 'br-1' },
    { id: 'bat-2', materialId: 'mat-4', supplier: 'ВетТоргИнтер', purchasePrice: 450, clientPrice: 950, totalQuantity: 100, remainingQuantity: 60, expiryDate: '2025-06-01', receivedAt: '2024-04-15', branchId: 'br-1' },
    { id: 'bat-3', materialId: 'mat-1', supplier: 'ЗооМедСнаб', purchasePrice: 5, clientPrice: 15, totalQuantity: 600, remainingQuantity: 480, expiryDate: '2024-10-01', receivedAt: '2024-04-15', branchId: 'br-2' },
    { id: 'bat-4', materialId: 'mat-5', supplier: 'ВетМаркет', purchasePrice: 400, clientPrice: 850, totalQuantity: 80, remainingQuantity: 50, expiryDate: '2025-11-01', receivedAt: '2024-04-10', branchId: 'br-2' },
    { id: 'bat-5', materialId: 'mat-6', supplier: 'МедПрибор', purchasePrice: 120, clientPrice: 300, totalQuantity: 200, remainingQuantity: 150, expiryDate: '2027-01-01', receivedAt: '2024-05-10', branchId: 'br-2' }
  ] as Batch[],

  clients: [
    { id: 'cl-1', name: 'Кузнецов Дмитрий Сергеевич', phone: '+7 (921) 123-4567', email: 'dmitry.kuznetsov@mail.ru', consentSigned: true, createdAt: '2024-04-20' },
    { id: 'cl-2', name: 'Васильева Елена Игоревна', phone: '+7 (952) 987-6543', email: 'elena.vas@gmail.com', consentSigned: true, createdAt: '2024-04-25' },
    { id: 'cl-3', name: 'Петров Сергей Иванович', phone: '+7 (916) 234-5678', email: 'sergey.petrov@mail.ru', consentSigned: true, createdAt: '2024-04-30' }
  ] as Client[],

  patients: [
    { id: 'pt-1', clientId: 'cl-1', name: 'Барсик', species: 'Кошка', breed: 'Британская короткошёрстная', gender: 'M', birthDate: '2022-04-12', weight: 5.4, color: 'Голубой окрас' },
    { id: 'pt-2', clientId: 'cl-1', name: 'Альфа', species: 'Собака', breed: 'Немецкая овчарка', gender: 'F', birthDate: '2020-09-24', weight: 32.1, color: 'Чепрачный окрас' },
    { id: 'pt-3', clientId: 'cl-2', name: 'Соня', species: 'Кошка', breed: 'Сибирская', gender: 'F', birthDate: '2022-12-05', weight: 4.2, color: 'Серая полосатая' },
    { id: 'pt-4', clientId: 'cl-3', name: 'Рекс', species: 'Собака', breed: 'Лабрадор-ретривер', gender: 'M', birthDate: '2021-06-30', weight: 28.5, color: 'Палевый' }
  ] as Patient[],

  appointments: [
    {
      id: 'apt-1', clientId: 'cl-1', patientId: 'pt-1', vetId: 'emp-3', branchId: 'br-1',
      appointmentDate: new Date(Date.now() - 3 * 86400000).toISOString().split('T')[0], timeSlot: '11:00', status: 'Closed', notes: 'Жалобы на снижение аппетита. Поставлен предварительный гастрит.', totalAmount: 2065, createdAt: '2024-05-15', closedAt: '2024-05-15T11:45:00',
      services: [
        { serviceId: 'srv-1', quantity: 1, priceSnapshot: 950 },
        { serviceId: 'srv-5', quantity: 1, priceSnapshot: 1100 }
      ],
      materials: [
        { materialId: 'mat-1', batchId: 'bat-1', quantity: 1, unitCostSnapshot: 5, clientPriceSnapshot: 15 }
      ],
      diagnoses: [
        { diagnosisId: 'dg-2', isPreliminary: true, doctorComment: 'Исключить погрешности диеты' }
      ]
    },
    {
      id: 'apt-2', clientId: 'cl-1', patientId: 'pt-2', vetId: 'emp-5', branchId: 'br-1',
      appointmentDate: new Date().toISOString().split('T')[0], timeSlot: '15:00', status: 'Planned', notes: 'Плановый осмотр перед вакцинацией.', totalAmount: 0, createdAt: '2024-05-18',
      services: [], materials: [], diagnoses: []
    },
    {
      id: 'apt-3', clientId: 'cl-3', patientId: 'pt-4', vetId: 'emp-7', branchId: 'br-2',
      appointmentDate: new Date(Date.now() - 2 * 86400000).toISOString().split('T')[0], timeSlot: '09:30', status: 'Closed', notes: 'Одышка при нагрузке, проведены исследования.', totalAmount: 3800, createdAt: '2024-05-16', closedAt: '2024-05-16T10:30:00',
      services: [
        { serviceId: 'srv-4', quantity: 1, priceSnapshot: 2000 },
        { serviceId: 'srv-1', quantity: 1, priceSnapshot: 950 }
      ],
      materials: [
        { materialId: 'mat-5', batchId: 'bat-4', quantity: 1, unitCostSnapshot: 400, clientPriceSnapshot: 850 }
      ],
      diagnoses: [
        { diagnosisId: 'dg-5', isPreliminary: false, doctorComment: 'Кардиомиопатия, контроль через месяц' }
      ]
    },
    {
      id: 'apt-4', clientId: 'cl-3', patientId: 'pt-4', vetId: 'emp-7', branchId: 'br-2',
      appointmentDate: new Date().toISOString().split('T')[0], timeSlot: '11:00', status: 'Planned', notes: 'Повторный приём, контроль динамики.', totalAmount: 0, createdAt: '2024-05-18',
      services: [], materials: [], diagnoses: []
    }
  ] as Appointment[],

  payments: [
    { id: 'pay-1', appointmentId: 'apt-1', paymentDate: new Date(Date.now() - 3 * 86400000).toISOString().substring(0, 16), amount: 2065, method: 'cash', notes: '' },
    { id: 'pay-2', appointmentId: 'apt-3', paymentDate: new Date(Date.now() - 2 * 86400000).toISOString().substring(0, 16), amount: 3800, method: 'card', notes: '' }
  ] as Payment[]
};
