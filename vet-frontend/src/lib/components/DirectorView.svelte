<script lang="ts">
  import { FileSpreadsheet, CircleDot, Users, UserPlus, X, KeyRound, ScrollText, Plus, GitBranch } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import { excelExporter } from '$lib/excelExporter';
  import { app } from '$lib/stores/app.svelte';
  import type { Appointment, Employee, Payment, Client, Branch, Position, User, Role, AuditEntry, Service, Material, Batch } from '$lib/types';
  import DataTable from './DataTable.svelte';

  let appointments = $state<Appointment[]>([]);
  let employees = $state<Employee[]>([]);
  let payments = $state<Payment[]>([]);
  let clients = $state<Client[]>([]);
  let branches = $state<Branch[]>([]);
  let positions = $state<Position[]>([]);
  let usersList = $state<User[]>([]);
  let auditEntries = $state<AuditEntry[]>([]);
  let servicesData = $state<Service[]>([]);
  let materialsData = $state<Material[]>([]);
  let batchesData = $state<Batch[]>([]);

  function getFirstDayOfCurrentMonth() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`;
  }
  function getLastDayOfCurrentMonth() {
    const d = new Date();
    const year = d.getFullYear();
    const month = d.getMonth() + 1;
    const lastDay = new Date(year, month, 0).getDate();
    return `${year}-${String(month).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`;
  }

  let startDate = $state(getFirstDayOfCurrentMonth());
  let endDate = $state(getLastDayOfCurrentMonth());

  async function loadData() {
    appointments = await apiClient.appointments.list();
    employees = await apiClient.employees.list();
    payments = await apiClient.payments.list();
    clients = await apiClient.clients.list();
    branches = await apiClient.branches.list();
    positions = await apiClient.positions.list();
    usersList = await apiClient.users.list();
    auditEntries = await apiClient.audit.list();
    servicesData = await apiClient.services.list();
    materialsData = await apiClient.materials.list();
    batchesData = await apiClient.batches.list();
  }

  $effect(() => {
    app.activeView;
    app.activeBranchId;
    app.reloadToken;
    loadData();
  });

  const activeBranchAppts = $derived(appointments.filter((a) => a.branchId === app.activeBranchId && a.status === 'Closed'));

  const activeBranchPays = $derived.by(() => {
    const apptIds = new Set(appointments.filter((a) => a.branchId === app.activeBranchId).map((a) => a.id));
    return payments.filter((p) => apptIds.has(p.appointmentId));
  });

  const financialKPIs = $derived.by(() => {
    const totalRev = activeBranchPays.reduce((acc, p) => acc + p.amount, 0);
    const apptsQty = activeBranchAppts.length;
    const avgTicket = apptsQty > 0 ? Math.round(totalRev / apptsQty) : 0;
    let directMatCost = 0;
    activeBranchAppts.forEach((a) => a.materials.forEach((m) => (directMatCost += m.unitCostSnapshot * m.quantity)));
    const grossMargin = totalRev - directMatCost;
    return {
      totalRevenue: totalRev, averageTicket: avgTicket, materialsCost: directMatCost,
      grossProfit: grossMargin, grossMarginPercent: totalRev > 0 ? Math.round((grossMargin / totalRev) * 100) : 0
    };
  });

  // Оценка себестоимости материалов по закрытым приёмам за сегодня (метод FIFO)
  const todayStr = new Date().toISOString().split('T')[0];
  const materialsCostToday = $derived(
    activeBranchAppts
      .filter((a) => a.appointmentDate === todayStr)
      .reduce((acc, a) => acc + a.materials.reduce((s, m) => s + m.unitCostSnapshot * m.quantity, 0), 0)
  );

  const doctorKPIs = $derived.by(() => {
    const doctors = employees.filter((e) => e.status === 'active' && e.branchIds.includes(app.activeBranchId) && e.positionId !== 'pos-4');
    return doctors.map((doc) => {
      const docAppts = activeBranchAppts.filter((a) => a.vetId === doc.id);
      let servicesValue = 0;
      docAppts.forEach((a) => a.services.forEach((s) => (servicesValue += s.priceSnapshot * s.quantity)));
      return { id: doc.id, name: doc.name, appointmentsCount: docAppts.length, servicesValue, earnedKPI: Math.round(servicesValue * doc.KPIRate) };
    });
  });

  // Управление жизненным циклом сотрудников (CRUD)
  let editingEmployee = $state<Employee | null>(null);
  let showAddEmployeeModal = $state(false);
  let empName = $state('');
  let empPositionId = $state('');
  let empPhone = $state('');
  let empEmail = $state('');
  let empBranchIds = $state<string[]>([]);
  let empKPIRate = $state(0.3);
  let empStatus = $state<'active' | 'inactive'>('active');

  function handleStartEditEmployee(emp: Employee) {
    editingEmployee = emp;
    empName = emp.name;
    empPositionId = emp.positionId;
    empPhone = emp.phone || '';
    empEmail = emp.email || '';
    empBranchIds = [...emp.branchIds];
    empKPIRate = emp.KPIRate;
    empStatus = emp.status;
    showAddEmployeeModal = true;
  }

  function handleStartAddEmployee() {
    editingEmployee = null;
    empName = '';
    empPositionId = positions[0]?.id || 'pos-1';
    empPhone = '';
    empEmail = '';
    empBranchIds = branches.length > 0 ? [branches[0].id] : ['br-1'];
    empKPIRate = 0.3;
    empStatus = 'active';
    showAddEmployeeModal = true;
  }

  async function handleSaveEmployee(e: SubmitEvent) {
    e.preventDefault();
    try {
      const targetId = editingEmployee ? editingEmployee.id : 'emp-' + Math.random().toString(36).substring(2, 11);
      const updatedEmp: Employee = {
        id: targetId,
        name: empName,
        positionId: empPositionId,
        phone: empPhone,
        email: empEmail,
        branchIds: empBranchIds,
        KPIRate: Number(empKPIRate),
        status: empStatus
      };
      await apiClient.employees.save(updatedEmp);
      app.audit(editingEmployee ? 'Изменение сотрудника' : 'Приём сотрудника', `${updatedEmp.name} (${updatedEmp.status})`);
      showAddEmployeeModal = false;
      editingEmployee = null;
      await loadData();
    } catch (err: any) {
      alert('Ошибка сохранения сотрудника: ' + err.message);
    }
  }

  function toggleEmpBranch(id: string, isChecked: boolean) {
    if (isChecked) {
      if (empBranchIds.length > 1) empBranchIds = empBranchIds.filter((b) => b !== id);
    } else {
      empBranchIds = [...empBranchIds, id];
    }
  }

  // Управление учетными записями и доступом (CRUD)
  const ALL_ROLES: { value: Role; label: string }[] = [
    { value: 'receptionist', label: 'Регистратор' },
    { value: 'vet', label: 'Врач' },
    { value: 'chief_vet', label: 'Главврач' },
    { value: 'director', label: 'Директор' }
  ];
  let editingUser = $state<User | null>(null);
  let showAccountModal = $state(false);
  let accUsername = $state('');
  let accEmployeeId = $state('');
  let accRoles = $state<Role[]>([]);
  let accStatus = $state<'active' | 'inactive'>('active');

  function handleStartEditUser(u: User) {
    editingUser = u;
    accUsername = u.username;
    accEmployeeId = u.employeeId || '';
    accRoles = [...u.roles];
    accStatus = u.status;
    showAccountModal = true;
  }
  function handleStartAddUser() {
    editingUser = null;
    accUsername = '';
    accEmployeeId = employees[0]?.id || '';
    accRoles = ['receptionist'];
    accStatus = 'active';
    showAccountModal = true;
  }
  function toggleAccRole(r: Role) {
    if (accRoles.includes(r)) accRoles = accRoles.filter((x) => x !== r);
    else accRoles = [...accRoles, r];
  }
  async function handleSaveUser(e: SubmitEvent) {
    e.preventDefault();
    if (!accUsername.trim()) { alert('Укажите логин учётной записи.'); return; }
    if (accRoles.length === 0) { alert('Назначьте хотя бы одну роль.'); return; }
    try {
      const saved = await apiClient.users.save({
        id: editingUser?.id,
        username: accUsername.trim(),
        employeeId: accEmployeeId || undefined,
        roles: accRoles,
        status: accStatus
      });
      app.audit(editingUser ? 'Изменение учётной записи' : 'Создание учётной записи', `Логин ${saved.username}, роли: ${saved.roles.join(', ')}`);
      showAccountModal = false;
      editingUser = null;
      await loadData();
    } catch (err: any) {
      alert('Ошибка сохранения учётной записи: ' + err.message);
    }
  }
  const roleLabel = (r: string) => ALL_ROLES.find((x) => x.value === r)?.label ?? r;

  // Управление справочником филиалов (CRUD)
  let editingBranch = $state<Branch | null>(null);
  let showBranchModal = $state(false);
  let brName = $state('');
  let brAddress = $state('');
  let brPhone = $state('');
  let brActive = $state(true);

  function handleStartEditBranch(b: Branch) {
    editingBranch = b;
    brName = b.name;
    brAddress = b.address;
    brPhone = b.phone;
    brActive = b.isActive !== false;
    showBranchModal = true;
  }
  function handleStartAddBranch() {
    editingBranch = null;
    brName = '';
    brAddress = 'г. Москва, ';
    brPhone = '+7 (495) ';
    brActive = true;
    showBranchModal = true;
  }
  async function handleSaveBranch(e: SubmitEvent) {
    e.preventDefault();
    if (!brName.trim() || !brAddress.trim()) { alert('Укажите название и адрес филиала.'); return; }
    const saved = await apiClient.branches.save({
      id: editingBranch?.id, name: brName.trim(), address: brAddress.trim(), phone: brPhone.trim(), isActive: brActive
    });
    app.audit(editingBranch ? 'Изменение филиала' : 'Создание филиала', `${saved.name} (${saved.address})`);
    showBranchModal = false;
    editingBranch = null;
    app.triggerReload();
    await loadData();
  }
  async function handleToggleBranch(b: Branch) {
    const next = b.isActive === false;
    const word = next ? 'активировать' : 'деактивировать';
    if (!confirm(`Вы уверены, что хотите ${word} филиал «${b.name}»?`)) return;
    await apiClient.branches.save({ ...b, isActive: next });
    app.audit(next ? 'Активация филиала' : 'Деактивация филиала', b.name);
    app.triggerReload();
    await loadData();
  }

  function handleExportPayroll() {
    excelExporter.exportPayroll(employees, appointments, positions, startDate, endDate, servicesData, materialsData);
  }
  function handleExportFinancial() {
    excelExporter.exportFinancial(appointments, branches, servicesData, materialsData, startDate, endDate);
  }
  function handleExportWarehouse() {
    excelExporter.exportWarehouse(batchesData, materialsData, branches);
  }
  function handleExportPayments() {
    excelExporter.exportPayments(payments, appointments, clients, startDate, endDate);
  }

  const paymentColumns = [
    { header: 'ID Платежа', sortKey: 'id' },
    { header: 'Дата / Время', sortKey: 'paymentDate' },
    { header: 'ID Приёма', sortKey: 'appointmentId' },
    { header: 'ФИО владельца', sortKey: 'clientName', sortValue: (p: Payment) => { const a = appointments.find((x) => x.id === p.appointmentId); return clients.find((c) => c.id === a?.clientId)?.name || 'N/A'; } },
    { header: 'Тип платежа', sortKey: 'method' },
    { header: 'Зафиксировано', sortKey: 'notes' },
    { header: 'Зачислено в кассу', sortKey: 'amount' }
  ];

  const employeeColumns = [
    { header: 'ID Специалиста' },
    { header: 'ФИО Кадра', sortKey: 'name' },
    { header: 'Штатная должность' },
    { header: 'Контакты' },
    { header: 'Действующие филиалы' },
    { header: 'Ставка KPI %', sortKey: 'KPIRate' },
    { header: 'Статус штата' }
  ];

  const branchColumns = [
    { header: 'ID филиала' },
    { header: 'Название подразделения', sortKey: 'name' },
    { header: 'Фактический адрес' },
    { header: 'Телефон' },
    { header: 'Статус' }
  ];

  const accountColumns = [
    { header: 'Логин', sortKey: 'username' },
    { header: 'Привязанный сотрудник' },
    { header: 'Роли доступа' },
    { header: 'Статус' }
  ];

  const auditColumns = [
    { header: 'Дата / Время', sortKey: 'timestamp' },
    { header: 'Пользователь', sortKey: 'username' },
    { header: 'Действие', sortKey: 'action' },
    { header: 'Детали' }
  ];

  // Клиентская фильтрация записей журнала аудита
  const isoDaysAgo = (n: number) => {
    const d = new Date();
    d.setDate(d.getDate() - n);
    return d.toISOString().split('T')[0];
  };
  let auditAction = $state('');
  let auditUser = $state('');
  let auditFrom = $state(isoDaysAgo(7));
  let auditTo = $state(isoDaysAgo(0));

  const auditActionOptions = $derived([...new Set(auditEntries.map((a) => a.action))].sort());
  const auditUserOptions = $derived([...new Set(auditEntries.map((a) => a.username))].sort());
  const filteredAudit = $derived(
    auditEntries.filter((a) => {
      if (auditAction && a.action !== auditAction) return false;
      if (auditUser && a.username !== auditUser) return false;
      const d = a.timestamp.split('T')[0];
      if (auditFrom && d < auditFrom) return false;
      if (auditTo && d > auditTo) return false;
      return true;
    })
  );
  function resetAuditFilters() {
    auditAction = '';
    auditUser = '';
    auditFrom = isoDaysAgo(7);
    auditTo = isoDaysAgo(0);
  }

  const exporters = [
    { iconCls: 'bg-indigo-50 text-indigo-700', btnCls: 'bg-indigo-700 hover:bg-indigo-800', title: '1. Зарплатная ведомость по KPI', desc: 'Двухстраничный Excel документ. Первая страница: сводная таблица выплат врачам, количество приемов. Вторая: полная расшифровка услуг с расчетом KPI взноса.', btn: 'Сформировать зарплатную ведомость (*.XLSX)', fn: handleExportPayroll },
    { iconCls: 'bg-blue-50 text-blue-700', btnCls: 'bg-blue-700 hover:bg-blue-800', title: '2. Финансовый отчёт по клинике', desc: 'Выгрузка финансового баланса. Отображает число приёмов, выручку по прайсированным работам, затраты на FIFO материалы и валовую прибыль каждого филиала.', btn: 'Выгрузить финансовый баланс (*.XLSX)', fn: handleExportFinancial },
    { iconCls: 'bg-emerald-50 text-emerald-700', btnCls: 'bg-emerald-700 hover:bg-emerald-800', title: '3. Хроника Складских Списков', desc: 'Аптечный и складской реестр. Экспортирует складские остатки по партиям, себестоимость и цену реализации, а также предупреждения по срокам годности.', btn: 'Экспортировать складской остаток (*.XLSX)', fn: handleExportWarehouse },
    { iconCls: 'bg-teal-50 text-teal-700', btnCls: 'bg-teal-700 hover:bg-teal-800', title: '4. Реестр полученных оплат', desc: 'Реестр проведённых по кассе платежей. Кассовая ведомость с группировкой по датам, владельцам, суммам и видам оплаты.', btn: 'Скачать реестр фискальных оплат (*.XLSX)', fn: handleExportPayments }
  ];
</script>

{#if app.activeView === 'director-dashboard'}
  <div class="space-y-6">
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
        <span class="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">Выручка филиала</span>
        <div class="text-2xl font-bold text-slate-900 mt-1 font-mono">{financialKPIs.totalRevenue} руб.</div>
        <span class="text-[10px] text-emerald-600 font-semibold block mt-1">Оплаты по кассе</span>
      </div>
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
        <span class="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">Средний чек приёма</span>
        <div class="text-2xl font-bold text-slate-900 mt-1 font-mono">{financialKPIs.averageTicket} руб.</div>
        <span class="text-[10px] text-slate-400 block mt-1">Закрытые визиты</span>
      </div>
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
        <span class="text-[10px] uppercase font-bold text-slate-400 tracking-wider block">Списания расходников</span>
        <div class="text-2xl font-bold text-slate-900 mt-1 font-mono">{financialKPIs.materialsCost} руб.</div>
        <span class="text-[10px] text-red-600 block mt-1 font-semibold">FIFO себестоимость (всего)</span>
        <span class="text-[10px] text-slate-500 block mt-0.5">Расход за сегодня: <span class="font-mono font-semibold text-slate-700">{materialsCostToday} руб.</span></span>
      </div>
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm border-l-4 border-l-teal-600">
        <span class="text-[10px] uppercase font-bold text-teal-700 tracking-wider block">Валовая прибыль маржа</span>
        <div class="text-2xl font-bold text-teal-800 mt-1 font-mono">{financialKPIs.grossProfit} руб.</div>
        <span class="text-[10px] text-teal-600 font-bold block mt-1">{financialKPIs.grossMarginPercent}% маржинальности</span>
      </div>
    </div>

    <div class="p-5 bg-white border border-slate-200 rounded-lg shadow-sm">
      <div class="flex items-center justify-between mb-4">
        <div>
          <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-widest">Анализ врачебной выручки в филиале</h3>
          <p class="text-[10px] text-slate-400 font-normal mt-0.5">Соотношение выручки от оказанных услуг и KPI-начислений врачам в текущем месяце.</p>
        </div>
        <div class="flex items-center gap-4 text-[10px]">
          <span class="flex items-center gap-1"><CircleDot class="w-3 h-3 text-teal-600" /> Выручка за услуги</span>
          <span class="flex items-center gap-1"><CircleDot class="w-3 h-3 text-indigo-500" /> Зарплата по KPI</span>
        </div>
      </div>

      {#if doctorKPIs.length > 0}
        <div class="w-full pt-2">
          <svg viewBox="0 0 600 240" class="w-full h-auto bg-slate-50 border border-slate-100 rounded-md">
            <line x1="50" y1="40" x2="560" y2="40" stroke="#E2E8F0" stroke-dasharray="3,3" />
            <line x1="50" y1="100" x2="560" y2="100" stroke="#E2E8F0" stroke-dasharray="3,3" />
            <line x1="50" y1="160" x2="560" y2="160" stroke="#E2E8F0" stroke-dasharray="3,3" />
            <line x1="50" y1="200" x2="560" y2="200" stroke="#E2E8F0" />
            <text x="40" y="44" class="fill-slate-400" font-size="9" font-family="monospace" text-anchor="end">15 000</text>
            <text x="40" y="104" class="fill-slate-400" font-size="9" font-family="monospace" text-anchor="end">10 000</text>
            <text x="40" y="164" class="fill-slate-400" font-size="9" font-family="monospace" text-anchor="end">5 000</text>
            <text x="40" y="204" class="fill-slate-400" font-size="9" font-family="monospace" text-anchor="end">0</text>
            {#each doctorKPIs as doc, idx (doc.id)}
              {@const spacing = 450 / doctorKPIs.length}
              {@const xBase = 80 + idx * spacing}
              {@const hServices = Math.min(160, (doc.servicesValue / 15000) * 160)}
              {@const hKPI = Math.min(160, (doc.earnedKPI / 15000) * 160)}
              <rect x={xBase} y={200 - hServices} width="24" height={hServices} fill="#0F766E" rx="2" />
              <rect x={xBase + 28} y={200 - hKPI} width="18" height={hKPI} fill="#6366F1" rx="1.5" />
              <text x={xBase + 20} y="218" class="fill-slate-600" font-size="9" font-weight="600" text-anchor="middle">{doc.name.split(' ')[0]}</text>
            {/each}
          </svg>
        </div>
      {:else}
        <p class="py-6 text-center text-slate-400 italic">Нет показателей врачебных закрытий в филиале.</p>
      {/if}
    </div>

    <div class="bg-white border border-slate-200 rounded-lg overflow-hidden shadow-sm">
      <div class="p-4 border-b border-slate-200 bg-slate-50 font-bold text-slate-700 text-xs">Трудовое участие врачей-терапевтов (KPI Монитор)</div>
      <table class="w-full text-left border-collapse text-xs">
        <thead>
          <tr class="bg-slate-50 border-b border-slate-200 text-[10px] font-bold text-slate-400 uppercase tracking-widest">
            <th class="p-3 pl-4">Сотрудник клиники</th><th class="p-3">Закрытых приёмов (Qty)</th><th class="p-3">Начисленная выручка за услуги</th><th class="p-3">Действующий KPI % тариф</th><th class="p-3 text-right pr-4">KPI Начислено к выплате</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 text-slate-700">
          {#each doctorKPIs as doc (doc.id)}
            {@const empObj = employees.find((e) => e.id === doc.id)}
            <tr class="hover:bg-slate-50/50">
              <td class="p-3 pl-4 font-bold text-slate-800">{doc.name}</td>
              <td class="p-3 font-mono">{doc.appointmentsCount} шт.</td>
              <td class="p-3 font-mono">{doc.servicesValue} руб.</td>
              <td class="p-3 text-slate-500 font-semibold">{((empObj?.KPIRate || 0) * 100).toFixed(0)}%</td>
              <td class="p-3 text-right pr-4 font-bold text-emerald-700 font-mono">+{doc.earnedKPI} руб.</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{:else if app.activeView === 'director-payments'}
  <div class="space-y-4">
    <DataTable columns={paymentColumns} data={payments} searchPlaceholder="Введите ID приёма или платежа для сверки..." searchFilter={(p, q) => p.id.toLowerCase().includes(q.toLowerCase()) || p.appointmentId.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(p)}
        {@const appt = appointments.find((a) => a.id === p.appointmentId)}
        <td class="px-4 py-3"><span class="font-mono text-[10px] text-slate-400 uppercase">{p.id}</span></td>
        <td class="px-4 py-3"><span class="font-mono text-slate-800">{p.paymentDate.replace('T', ' ').substring(0, 16)}</span></td>
        <td class="px-4 py-3"><span class="font-mono text-slate-500 font-bold uppercase text-[10px] bg-slate-100 p-0.5 px-1.5 rounded">{p.appointmentId.toUpperCase()}</span></td>
        <td class="px-4 py-3 text-slate-700">{clients.find((c) => c.id === appt?.clientId)?.name || 'N/A'}</td>
        <td class="px-4 py-3"><span class="font-semibold text-slate-600 text-[10px] uppercase bg-slate-200 px-1.5 rounded">{p.method === 'cash' ? 'Наличные' : p.method === 'card' ? 'Безнал / Карта' : 'СБП / Перевод'}</span></td>
        <td class="px-4 py-3 text-slate-700">{p.notes || ''}</td>
        <td class="px-4 py-3"><span class="font-bold text-emerald-800 font-mono">+{p.amount} руб.</span></td>
      {/snippet}
    </DataTable>
  </div>
{:else if app.activeView === 'director-reports'}
  <div class="space-y-6">
    <div class="p-5 bg-white border border-slate-200 rounded-lg shadow-sm">
      <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-widest mb-4">Параметры формирования отчётности</h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-slate-700">
        <div><label class="block text-slate-400 font-semibold mb-1">Дата начала периода *</label><input type="date" required bind:value={startDate} class="w-full p-2 border border-slate-200 rounded bg-white font-mono focus:border-teal-700" /></div>
        <div><label class="block text-slate-400 font-semibold mb-1">Дата окончания периода *</label><input type="date" required bind:value={endDate} class="w-full p-2 border border-slate-200 rounded bg-white font-mono focus:border-teal-700" /></div>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      {#each exporters as ex (ex.title)}
        <div class="p-5 bg-white border border-slate-200 rounded-lg shadow-sm flex flex-col justify-between space-y-4">
          <div>
            <span class="p-2 {ex.iconCls} rounded-lg inline-block mb-3"><FileSpreadsheet class="w-5 h-5" /></span>
            <h4 class="text-xs font-bold text-slate-800 block uppercase tracking-wide">{ex.title}</h4>
            <p class="text-[11px] text-slate-400 font-normal mt-1 leading-relaxed">{ex.desc}</p>
          </div>
          <button onclick={ex.fn} class="w-full py-2 {ex.btnCls} font-semibold text-white text-xs rounded transition cursor-pointer select-none">{ex.btn}</button>
        </div>
      {/each}
    </div>
  </div>
{:else if app.activeView === 'admin-users'}
  <div class="space-y-4 text-xs relative">
    <div class="flex justify-between items-center bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
      <div>
        <h3 class="text-xs font-bold text-slate-800 uppercase tracking-widest flex items-center gap-2"><Users class="w-4 h-4 text-teal-700" /><span>Штатное расписание и права доступа</span></h3>
        <p class="text-[11px] text-slate-400 font-normal mt-0.5">Управление карточками сотрудников клиники, начислением процентов KPI и определением филиалов присутствия.</p>
      </div>
      <button onclick={handleStartAddEmployee} class="flex items-center gap-1.5 px-3 py-1.5 bg-[#0F766E] hover:bg-teal-800 text-white font-bold rounded shadow transition-colors cursor-pointer select-none"><UserPlus class="w-3.5 h-3.5" /><span>Принять сотрудника</span></button>
    </div>

    <DataTable columns={employeeColumns} data={employees} searchPlaceholder="Поиск кадров по ФИО..." searchFilter={(e, q) => e.name.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(e)}
        <td class="px-4 py-3"><span class="font-mono text-slate-400">{e.id}</span></td>
        <td class="px-4 py-3"><span class="font-bold text-slate-800">{e.name}</span></td>
        <td class="px-4 py-3 text-slate-700">{positions.find((p) => p.id === e.positionId)?.name || 'Сотрудник'}</td>
        <td class="px-4 py-3"><span class="text-slate-500 font-mono text-[10px] block">{e.phone || 'Н/Д'}<br />{e.email || 'Н/Д'}</span></td>
        <td class="px-4 py-3 text-slate-700">{e.branchIds.map((bid) => branches.find((b) => b.id === bid)?.name).join(', ')}</td>
        <td class="px-4 py-3"><span class="font-mono font-semibold text-teal-800">{(e.KPIRate * 100).toFixed(0)}%</span></td>
        <td class="px-4 py-3">{#if e.status === 'active'}<span class="text-emerald-700 font-semibold bg-emerald-50 px-2 py-0.5 rounded">В штате</span>{:else}<span class="text-slate-400 bg-slate-100 px-2 py-0.5 rounded">Уволен</span>{/if}</td>
      {/snippet}
      {#snippet actions(e)}
        <button onclick={() => handleStartEditEmployee(e)} class="px-2.5 py-1 bg-teal-50 hover:bg-teal-100 text-teal-800 rounded font-bold text-[10px] transition cursor-pointer">Редактировать</button>
      {/snippet}
    </DataTable>

    {#if showAddEmployeeModal}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
        <div class="bg-white border border-slate-200 w-full max-w-md rounded-lg shadow-2xl overflow-hidden flex flex-col">
          <div class="p-4 bg-slate-50 border-b border-slate-100 flex justify-between items-center">
            <span class="font-bold text-slate-800 text-xs uppercase tracking-wider">{editingEmployee ? 'Редактирование сотрудника' : 'Прием нового сотрудника'}</span>
            <button type="button" onclick={() => (showAddEmployeeModal = false)} class="p-1 hover:bg-slate-200 text-slate-400 hover:text-slate-600 rounded transition cursor-pointer"><X class="w-4 h-4" /></button>
          </div>
          <form onsubmit={handleSaveEmployee} class="p-5 space-y-4">
            <div class="space-y-1">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">ФИО Сотрудника *</label>
              <input type="text" required placeholder="Иванов Иван Иванович" bind:value={empName} class="w-full p-2 border border-slate-200 rounded text-xs focus:outline-none focus:border-teal-700 focus:ring-1 focus:ring-teal-700/20 bg-slate-50 focus:bg-white transition" />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div class="space-y-1">
                <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Телефон *</label>
                <input type="text" required placeholder="+7 (999) 000-0000" bind:value={empPhone} class="w-full p-2 border border-slate-200 rounded text-xs focus:outline-none focus:border-teal-700 bg-slate-50 focus:bg-white transition" />
              </div>
              <div class="space-y-1">
                <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Электронная почта *</label>
                <input type="email" required placeholder="employee@achilles-vet.ru" bind:value={empEmail} class="w-full p-2 border border-slate-200 rounded text-xs focus:outline-none focus:border-teal-700 bg-slate-50 focus:bg-white transition" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div class="space-y-1">
                <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Штатная должность *</label>
                <select bind:value={empPositionId} class="w-full p-2 border border-slate-200 rounded text-xs bg-white focus:outline-none focus:border-teal-700">
                  {#each positions as p (p.id)}<option value={p.id}>{p.name}</option>{/each}
                </select>
              </div>
              <div class="space-y-1">
                <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Статус штата *</label>
                <select bind:value={empStatus} class="w-full p-2 border border-slate-200 rounded text-xs bg-white focus:outline-none focus:border-teal-700">
                  <option value="active">Активен (В штате)</option>
                  <option value="inactive">Уволен / Отстранен</option>
                </select>
              </div>
            </div>
            <div class="space-y-1">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Ставка KPI комиссии (%) *</label>
              <div class="flex items-center gap-3">
                <input type="range" min="0" max="1" step="0.05" bind:value={empKPIRate} class="flex-1 accent-teal-700" />
                <span class="font-mono font-bold text-xs text-teal-800 bg-teal-50 px-2 py-1 rounded border border-teal-100 min-w-[50px] text-center">{(empKPIRate * 100).toFixed(0)}%</span>
              </div>
              <p class="text-[10px] text-slate-400">Процент выплат от выручки оказанных услуг при заполнении медицинских протоколов.</p>
            </div>
            <div class="space-y-1.5">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Привязать к филиалам (минимум один) *</label>
              <div class="bg-slate-50 p-2.5 rounded border border-slate-100 space-y-1 max-h-32 overflow-y-auto">
                {#each branches as b (b.id)}
                  {@const isChecked = empBranchIds.includes(b.id)}
                  <label class="flex items-center gap-2 text-[11px] font-medium text-slate-700 cursor-pointer select-none">
                    <input type="checkbox" checked={isChecked} onchange={() => toggleEmpBranch(b.id, isChecked)} class="rounded border-slate-300 accent-teal-700" />
                    <span>{b.name}</span>
                  </label>
                {/each}
              </div>
            </div>
            <div class="pt-3 border-t border-slate-100 flex justify-end gap-2 text-xs">
              <button type="button" onclick={() => (showAddEmployeeModal = false)} class="px-3 py-1.5 border border-slate-200 text-slate-600 hover:bg-slate-50 rounded font-semibold transition cursor-pointer">Отмена</button>
              <button type="submit" class="px-4 py-1.5 bg-[#0F766E] hover:bg-teal-800 text-white rounded font-bold shadow transition cursor-pointer">{editingEmployee ? 'Сохранить изменения' : 'Принять в штат'}</button>
            </div>
          </form>
        </div>
      </div>
    {/if}
  </div>
{:else if app.activeView === 'admin-accounts'}
  <div class="space-y-4 text-xs relative">
    <div class="flex justify-between items-center bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
      <div>
        <h3 class="text-xs font-bold text-slate-800 uppercase tracking-widest flex items-center gap-2"><KeyRound class="w-4 h-4 text-teal-700" /><span>Учётные записи и роли доступа</span></h3>
        <p class="text-[11px] text-slate-400 font-normal mt-0.5">Управление логинами сотрудников и назначением ролей (RBAC). Один пользователь может совмещать несколько ролей.</p>
      </div>
      <button onclick={handleStartAddUser} class="flex items-center gap-1.5 px-3 py-1.5 bg-[#0F766E] hover:bg-teal-800 text-white font-bold rounded shadow transition-colors cursor-pointer select-none"><UserPlus class="w-3.5 h-3.5" /><span>Создать учётку</span></button>
    </div>

    <DataTable columns={accountColumns} data={usersList} searchPlaceholder="Поиск по логину..." searchFilter={(u, q) => u.username.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(u)}
        <td class="px-4 py-3"><span class="font-mono font-bold text-slate-800">{u.username}</span></td>
        <td class="px-4 py-3 text-slate-700">{employees.find((e) => e.id === u.employeeId)?.name || '— не привязан —'}</td>
        <td class="px-4 py-3">
          <div class="flex flex-wrap gap-1">
            {#each u.roles as r (r)}<span class="px-1.5 py-0.5 rounded text-[9px] font-bold bg-teal-50 text-teal-800 border border-teal-100">{roleLabel(r)}</span>{/each}
          </div>
        </td>
        <td class="px-4 py-3">{#if u.status === 'active'}<span class="text-emerald-700 font-semibold bg-emerald-50 px-2 py-0.5 rounded">Активна</span>{:else}<span class="text-slate-400 bg-slate-100 px-2 py-0.5 rounded">Заблокирована</span>{/if}</td>
      {/snippet}
      {#snippet actions(u)}
        <button onclick={() => handleStartEditUser(u)} class="px-2.5 py-1 bg-teal-50 hover:bg-teal-100 text-teal-800 rounded font-bold text-[10px] transition cursor-pointer">Редактировать</button>
      {/snippet}
    </DataTable>

    {#if showAccountModal}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
        <div class="bg-white border border-slate-200 w-full max-w-md rounded-lg shadow-2xl overflow-hidden flex flex-col">
          <div class="p-4 bg-slate-50 border-b border-slate-100 flex justify-between items-center">
            <span class="font-bold text-slate-800 text-xs uppercase tracking-wider">{editingUser ? 'Редактирование учётной записи' : 'Новая учётная запись'}</span>
            <button type="button" onclick={() => (showAccountModal = false)} class="p-1 hover:bg-slate-200 text-slate-400 hover:text-slate-600 rounded transition cursor-pointer"><X class="w-4 h-4" /></button>
          </div>
          <form onsubmit={handleSaveUser} class="p-5 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div class="space-y-1">
                <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Логин *</label>
                <input type="text" required placeholder="ivanovii" bind:value={accUsername} class="w-full p-2 border border-slate-200 rounded text-xs font-mono focus:outline-none focus:border-teal-700 bg-slate-50 focus:bg-white transition" />
              </div>
              <div class="space-y-1">
                <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Статус *</label>
                <select bind:value={accStatus} class="w-full p-2 border border-slate-200 rounded text-xs bg-white focus:outline-none focus:border-teal-700">
                  <option value="active">Активна</option>
                  <option value="inactive">Заблокирована</option>
                </select>
              </div>
            </div>
            <div class="space-y-1">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Привязанный сотрудник</label>
              <select bind:value={accEmployeeId} class="w-full p-2 border border-slate-200 rounded text-xs bg-white focus:outline-none focus:border-teal-700">
                <option value="">— не привязан —</option>
                {#each employees as e (e.id)}<option value={e.id}>{e.name}</option>{/each}
              </select>
            </div>
            <div class="space-y-1.5">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Роли доступа (минимум одна) *</label>
              <div class="bg-slate-50 p-2.5 rounded border border-slate-100 grid grid-cols-2 gap-1.5">
                {#each ALL_ROLES as r (r.value)}
                  <label class="flex items-center gap-2 text-[11px] font-medium text-slate-700 cursor-pointer select-none">
                    <input type="checkbox" checked={accRoles.includes(r.value)} onchange={() => toggleAccRole(r.value)} class="rounded border-slate-300 accent-teal-700" />
                    <span>{r.label}</span>
                  </label>
                {/each}
              </div>
            </div>
            <div class="pt-3 border-t border-slate-100 flex justify-end gap-2 text-xs">
              <button type="button" onclick={() => (showAccountModal = false)} class="px-3 py-1.5 border border-slate-200 text-slate-600 hover:bg-slate-50 rounded font-semibold transition cursor-pointer">Отмена</button>
              <button type="submit" class="px-4 py-1.5 bg-[#0F766E] hover:bg-teal-800 text-white rounded font-bold shadow transition cursor-pointer">{editingUser ? 'Сохранить' : 'Создать учётку'}</button>
            </div>
          </form>
        </div>
      </div>
    {/if}
  </div>
{:else if app.activeView === 'admin-audit'}
  <div class="space-y-4 text-xs">
    <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
      <h3 class="text-xs font-bold text-slate-800 uppercase tracking-widest flex items-center gap-2"><ScrollText class="w-4 h-4 text-teal-700" /><span>Журнал действий пользователей</span></h3>
      <p class="text-[11px] text-slate-400 font-normal mt-0.5">Хронология ключевых операций в системе: входы, оформление приёмов, кассовые операции, изменения кадров и учётных записей.</p>
    </div>

    <div class="bg-white p-3 border border-slate-200 rounded-lg shadow-sm flex flex-wrap items-end gap-3">
      <div>
        <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">Тип действия</label>
        <select bind:value={auditAction} class="p-1.5 px-2 border border-slate-200 rounded bg-white text-xs focus:outline-none focus:border-teal-700 min-w-[180px]">
          <option value="">Все действия</option>
          {#each auditActionOptions as a (a)}<option value={a}>{a}</option>{/each}
        </select>
      </div>
      <div>
        <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">Пользователь</label>
        <select bind:value={auditUser} class="p-1.5 px-2 border border-slate-200 rounded bg-white text-xs focus:outline-none focus:border-teal-700 min-w-[150px]">
          <option value="">Все пользователи</option>
          {#each auditUserOptions as u (u)}<option value={u}>{u}</option>{/each}
        </select>
      </div>
      <div>
        <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">С даты</label>
        <input type="date" bind:value={auditFrom} class="p-1.5 px-2 border border-slate-200 rounded bg-white text-xs font-mono focus:outline-none focus:border-teal-700" />
      </div>
      <div>
        <label class="block text-[10px] font-bold text-slate-400 uppercase mb-1">По дату</label>
        <input type="date" bind:value={auditTo} class="p-1.5 px-2 border border-slate-200 rounded bg-white text-xs font-mono focus:outline-none focus:border-teal-700" />
      </div>
      <button onclick={resetAuditFilters} class="px-3 py-1.5 border border-slate-200 text-slate-500 hover:bg-slate-50 rounded font-semibold text-xs cursor-pointer transition">Сбросить</button>
      <div class="ml-auto text-[11px] text-slate-400 self-center">Записей в выборке: <span class="font-bold text-slate-700">{filteredAudit.length}</span></div>
    </div>

    <DataTable columns={auditColumns} data={filteredAudit} searchPlaceholder="Поиск по пользователю или действию..." searchFilter={(a, q) => a.username.toLowerCase().includes(q.toLowerCase()) || a.action.toLowerCase().includes(q.toLowerCase())} emptyMessage="Нет записей по выбранным фильтрам.">
      {#snippet row(a)}
        <td class="px-4 py-3"><span class="font-mono text-slate-800">{a.timestamp.replace('T', ' ').substring(0, 19)}</span></td>
        <td class="px-4 py-3"><span class="font-mono font-semibold text-slate-700">{a.username}</span></td>
        <td class="px-4 py-3"><span class="font-semibold text-teal-800 text-[10px] uppercase bg-teal-50 px-1.5 py-0.5 rounded border border-teal-100">{a.action}</span></td>
        <td class="px-4 py-3 text-slate-600">{a.details}</td>
      {/snippet}
    </DataTable>
  </div>
{:else if app.activeView === 'admin-branches'}
  <div class="space-y-4 text-xs relative">
    <div class="flex justify-between items-center bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
      <div>
        <h3 class="text-xs font-bold text-slate-800 uppercase tracking-widest flex items-center gap-2"><GitBranch class="w-4 h-4 text-teal-700" /><span>Филиалы клиники</span></h3>
        <p class="text-[11px] text-slate-400 font-normal mt-0.5">Управление подразделениями сети. Деактивация скрывает филиал из выбора, но сохраняет связанные данные (приёмы, склад).</p>
      </div>
      <button onclick={handleStartAddBranch} class="flex items-center gap-1.5 px-3 py-1.5 bg-[#0F766E] hover:bg-teal-800 text-white font-bold rounded shadow transition-colors cursor-pointer select-none"><Plus class="w-3.5 h-3.5" /><span>Новый филиал</span></button>
    </div>

    <DataTable columns={branchColumns} data={branches} searchPlaceholder="Введите название подразделения..." searchFilter={(b, q) => b.name.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(b)}
        <td class="px-4 py-3"><span class="font-mono text-slate-400">{b.id}</span></td>
        <td class="px-4 py-3"><span class="font-semibold text-teal-800">{b.name}</span></td>
        <td class="px-4 py-3 text-slate-700">{b.address}</td>
        <td class="px-4 py-3 font-mono text-slate-600">{b.phone}</td>
        <td class="px-4 py-3">{#if b.isActive === false}<span class="text-slate-400 bg-slate-100 px-2 py-0.5 rounded">Отключён</span>{:else}<span class="text-emerald-700 font-semibold bg-emerald-50 px-2 py-0.5 rounded">Действует</span>{/if}</td>
      {/snippet}
      {#snippet actions(b)}
        <button onclick={() => handleStartEditBranch(b)} class="px-2.5 py-1 bg-teal-50 hover:bg-teal-100 text-teal-800 rounded font-bold text-[10px] transition cursor-pointer">Изменить</button>
        <button onclick={() => handleToggleBranch(b)} class="px-2.5 py-1 rounded font-bold text-[10px] transition cursor-pointer {b.isActive === false ? 'bg-emerald-50 hover:bg-emerald-100 text-emerald-800' : 'bg-red-50 hover:bg-red-100 text-red-700'}">{b.isActive === false ? 'Включить' : 'Отключить'}</button>
      {/snippet}
    </DataTable>

    {#if showBranchModal}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
        <div class="bg-white border border-slate-200 w-full max-w-md rounded-lg shadow-2xl overflow-hidden flex flex-col">
          <div class="p-4 bg-slate-50 border-b border-slate-100 flex justify-between items-center">
            <span class="font-bold text-slate-800 text-xs uppercase tracking-wider">{editingBranch ? 'Редактирование филиала' : 'Новый филиал'}</span>
            <button type="button" onclick={() => (showBranchModal = false)} class="p-1 hover:bg-slate-200 text-slate-400 hover:text-slate-600 rounded transition cursor-pointer"><X class="w-4 h-4" /></button>
          </div>
          <form onsubmit={handleSaveBranch} class="p-5 space-y-4">
            <div class="space-y-1">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Название подразделения *</label>
              <input type="text" required placeholder="Восточный филиал" bind:value={brName} class="w-full p-2 border border-slate-200 rounded text-xs focus:outline-none focus:border-teal-700 bg-slate-50 focus:bg-white transition" />
            </div>
            <div class="space-y-1">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Фактический адрес *</label>
              <input type="text" required placeholder="г. Москва, ул. ..." bind:value={brAddress} class="w-full p-2 border border-slate-200 rounded text-xs focus:outline-none focus:border-teal-700 bg-slate-50 focus:bg-white transition" />
            </div>
            <div class="space-y-1">
              <label class="block font-bold text-slate-500 uppercase tracking-tight text-[10px]">Телефон</label>
              <input type="text" placeholder="+7 (495) 000-0000" bind:value={brPhone} class="w-full p-2 border border-slate-200 rounded text-xs font-mono focus:outline-none focus:border-teal-700 bg-slate-50 focus:bg-white transition" />
            </div>
            <label class="flex items-center gap-2 text-[11px] font-medium text-slate-700 cursor-pointer select-none">
              <input type="checkbox" bind:checked={brActive} class="rounded border-slate-300 accent-teal-700" />
              <span>Филиал действует (доступен для выбора и записи)</span>
            </label>
            <div class="pt-3 border-t border-slate-100 flex justify-end gap-2 text-xs">
              <button type="button" onclick={() => (showBranchModal = false)} class="px-3 py-1.5 border border-slate-200 text-slate-600 hover:bg-slate-50 rounded font-semibold transition cursor-pointer">Отмена</button>
              <button type="submit" class="px-4 py-1.5 bg-[#0F766E] hover:bg-teal-800 text-white rounded font-bold shadow transition cursor-pointer">{editingBranch ? 'Сохранить' : 'Создать филиал'}</button>
            </div>
          </form>
        </div>
      </div>
    {/if}
  </div>
{/if}
