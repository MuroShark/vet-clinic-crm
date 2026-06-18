<script lang="ts">
  import { X, Search, Plus, Trash2, Calendar, AlertCircle, Layers } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type {
    Client, Patient, Employee, Branch, Service, Material, Batch, Diagnosis,
    Appointment, AppointmentStatus, AppointmentService, AppointmentMaterial, AppointmentDiagnosis
  } from '$lib/types';

  let { editingAppointmentId, onClose, onSuccess }: {
    editingAppointmentId?: string;
    onClose: () => void;
    onSuccess: (appointment: Appointment) => void;
  } = $props();

  const activeBranchId = app.activeBranchId;

  // Инициализация коллекций справочников
  let dbClients = $state<Client[]>([]);
  let dbPatients = $state<Patient[]>([]);
  let vets = $state<Employee[]>([]);
  let services = $state<Service[]>([]);
  let materials = $state<Material[]>([]);
  let batches = $state<Batch[]>([]);
  let allBatches = $state<Batch[]>([]);
  let allAppointments = $state<Appointment[]>([]);
  let diagnosesEntries = $state<Diagnosis[]>([]);
  let branches = $state<Branch[]>([]);

  const totalSteps = 7;
  let step = $state(1);

  // Выборки мастера оформления
  let selectedClient = $state<Client | null>(null);
  let selectedPatient = $state<Patient | null>(null);
  let selectedVet = $state<Employee | null>(null);
  let selectedDate = $state<string>(new Date().toISOString().split('T')[0]);
  let selectedSlot = $state<string>('');

  let apptServices = $state<{ serviceId: string; quantity: number; price: number }[]>([]);
  let apptMaterials = $state<{ materialId: string; batchId: string; quantity: number; purchasePrice: number; clientPrice: number; maxQty: number }[]>([]);
  let apptDiagnoses = $state<{ diagnosisId: string; isPreliminary: boolean; doctorComment: string }[]>([]);

  let comments = $state('');

  // Состояние подформ и поиска
  let clientSearch = $state('');
  let showNewClientForm = $state(false);
  let newClientData = $state({ name: '', phone: '', email: '', consentSigned: false });
  let showNewPatientForm = $state(false);
  let newPatientData = $state({ name: '', species: 'Кот', breed: '', gender: 'M' as 'M' | 'F', birthDate: '', weight: 0.1, color: '' });

  let validationError = $state<string | null>(null);

  const defaultSlots = [
    '09:00', '09:30', '10:00', '10:30', '11:00', '11:30', '12:00', '12:30',
    '13:00', '13:30', '14:00', '14:30', '15:00', '15:30', '16:00', '16:30', '17:00', '17:30'
  ];

  async function loadData() {
    const [clientsData, patientsData, employeesData, servicesData, materialsData, batchesData, diagnosesData, branchesData] =
      await Promise.all([
        apiClient.clients.list(), apiClient.patients.list(), apiClient.employees.list(),
        apiClient.services.list(), apiClient.materials.list(), apiClient.batches.list(),
        apiClient.diagnoses.list(), apiClient.branches.list()
      ]);
    dbClients = clientsData;
    dbPatients = patientsData;
    vets = employeesData.filter((e) => e.status === 'active' && e.branchIds.includes(activeBranchId) && e.positionId !== 'pos-4');
    services = servicesData;
    materials = materialsData;
    allBatches = batchesData;
    batches = batchesData.filter((b) => b.branchId === activeBranchId && b.remainingQuantity > 0);
    diagnosesEntries = diagnosesData;
    branches = branchesData;
    allAppointments = await apiClient.appointments.list();
  }

  async function loadDraft() {
    if (!editingAppointmentId) return;
    const appt = await apiClient.appointments.get(editingAppointmentId);
    if (!appt) return;

    selectedClient = (await apiClient.clients.get(appt.clientId)) || null;
    selectedPatient = (await apiClient.patients.get(appt.patientId)) || null;
    const list = await apiClient.employees.list();
    selectedVet = list.find((e) => e.id === appt.vetId) || null;
    selectedDate = appt.appointmentDate;
    selectedSlot = appt.timeSlot;
    comments = appt.notes;

    apptServices = appt.services.map((s) => ({ serviceId: s.serviceId, quantity: s.quantity, price: s.priceSnapshot }));
    apptMaterials = appt.materials.map((m) => {
      const bat = allBatches.find((b) => b.id === m.batchId);
      return {
        materialId: m.materialId, batchId: m.batchId, quantity: m.quantity,
        purchasePrice: m.unitCostSnapshot, clientPrice: m.clientPriceSnapshot,
        maxQty: bat ? bat.remainingQuantity + m.quantity : m.quantity
      };
    });
    apptDiagnoses = appt.diagnoses.map((d) => ({ diagnosisId: d.diagnosisId, isPreliminary: d.isPreliminary, doctorComment: d.doctorComment }));

    if (appt.status === 'InProgress') step = 4;
  }

  $effect(() => {
    loadData().then(loadDraft);
  });

  const filteredClients = $derived.by(() => {
    if (!clientSearch) return [];
    const q = clientSearch.toLowerCase();
    return dbClients.filter((c) => c.name.toLowerCase().includes(q) || c.phone.includes(q));
  });

  const clientPatients = $derived(selectedClient ? dbPatients.filter((p) => p.clientId === selectedClient!.id) : []);

  const availableSlots = $derived.by(() => {
    if (!selectedVet) return defaultSlots.map((slot) => ({ time: slot, isBusy: false }));
    const appts = allAppointments.filter(
      (a) => a.vetId === selectedVet!.id && a.appointmentDate === selectedDate && a.id !== editingAppointmentId && a.status !== 'Cancelled'
    );
    const busySlots = appts.map((a) => a.timeSlot);
    return defaultSlots.map((slot) => ({ time: slot, isBusy: busySlots.includes(slot) }));
  });

  const rawServicesTotal = $derived(apptServices.reduce((acc, row) => acc + row.price * row.quantity, 0));
  const rawMaterialsTotal = $derived(apptMaterials.reduce((acc, row) => acc + row.clientPrice * row.quantity, 0));
  const rawTotalPayable = $derived(rawServicesTotal + rawMaterialsTotal);

  // Обработчики услуг
  function handleAddServiceRow() {
    const firstSrv = services[0];
    if (!firstSrv) return;
    apptServices = [...apptServices, { serviceId: firstSrv.id, quantity: 1, price: firstSrv.defaultPrice }];
  }
  function handleServiceChange(index: number, serviceId: string) {
    const found = services.find((s) => s.id === serviceId);
    if (!found) return;
    apptServices[index] = { ...apptServices[index], serviceId, price: found.defaultPrice };
  }
  function handleRemoveServiceRow(index: number) {
    apptServices = apptServices.filter((_, i) => i !== index);
  }

  // Поиск партий (FIFO)
  function getFifoBatchForMaterial(materialId: string): Batch | null {
    const matching = batches.filter((b) => b.materialId === materialId && b.remainingQuantity > 0)
      .sort((a, b) => a.expiryDate.localeCompare(b.expiryDate));
    return matching[0] || null;
  }
  function handleAddMaterialRow() {
    const firstMat = materials[0];
    if (!firstMat) return;
    const sb = getFifoBatchForMaterial(firstMat.id);
    apptMaterials = [...apptMaterials, {
      materialId: firstMat.id, batchId: sb ? sb.id : '', quantity: 1,
      purchasePrice: sb ? sb.purchasePrice : 0, clientPrice: sb ? sb.clientPrice : 0, maxQty: sb ? sb.remainingQuantity : 0
    }];
  }
  function handleMaterialChange(index: number, materialId: string) {
    const sb = getFifoBatchForMaterial(materialId);
    apptMaterials[index] = {
      materialId, batchId: sb ? sb.id : '', quantity: 1,
      purchasePrice: sb ? sb.purchasePrice : 0, clientPrice: sb ? sb.clientPrice : 0, maxQty: sb ? sb.remainingQuantity : 0
    };
  }
  function handleBatchChange(index: number, batchId: string) {
    const b = batches.find((item) => item.id === batchId);
    if (!b) return;
    apptMaterials[index] = { ...apptMaterials[index], batchId, purchasePrice: b.purchasePrice, clientPrice: b.clientPrice, maxQty: b.remainingQuantity };
  }
  function handleRemoveMaterialRow(index: number) {
    apptMaterials = apptMaterials.filter((_, i) => i !== index);
  }

  // Обработчики диагнозов
  function handleAddDiagnosisRow() {
    const firstDg = diagnosesEntries[0];
    if (!firstDg) return;
    apptDiagnoses = [...apptDiagnoses, { diagnosisId: firstDg.id, isPreliminary: true, doctorComment: '' }];
  }
  function handleRemoveDiagnosisRow(index: number) {
    apptDiagnoses = apptDiagnoses.filter((_, i) => i !== index);
  }

  async function handleCreateClient(e: SubmitEvent) {
    e.preventDefault();
    if (!newClientData.name || !newClientData.phone) {
      validationError = 'Заполните обязательные поля: ФИО и Телефон';
      return;
    }
    try {
      const created = await apiClient.clients.save({ ...newClientData });
      dbClients = [...dbClients, created];
      selectedClient = created;
      showNewClientForm = false;
      clientSearch = '';
      validationError = null;
      step = 2;
    } catch (err: any) {
      validationError = err.message;
    }
  }

  async function handleCreatePatient(e: SubmitEvent) {
    e.preventDefault();
    if (!selectedClient) return;
    if (!newPatientData.name || !newPatientData.birthDate) {
      validationError = 'Укажите кличку и дату рождения питомца';
      return;
    }
    try {
      const created = await apiClient.patients.save({ ...newPatientData, clientId: selectedClient.id });
      dbPatients = [...dbPatients, created];
      selectedPatient = created;
      showNewPatientForm = false;
      validationError = null;
      step = 3;
    } catch (err: any) {
      validationError = err.message;
    }
  }

  function handleNextStep() {
    validationError = null;
    if (step === 1 && !selectedClient) { validationError = 'Пожалуйста, выберите клиента или создайте нового.'; return; }
    if (step === 2 && !selectedPatient) { validationError = 'Пожалуйста, выберите пациента (питомца) клиента.'; return; }
    if (step === 3) {
      if (!selectedVet) { validationError = 'Регистрация требует лечащего врача.'; return; }
      if (!selectedSlot) { validationError = 'Укажите свободное время записи из сетки календаря.'; return; }
    }
    if (step === 5) {
      for (const m of apptMaterials) {
        if (!m.batchId) { validationError = `В партии для материала #${m.materialId} нет доступных остатков.`; return; }
        if (m.quantity > m.maxQty) { validationError = `Запрашиваемое количество превышает складской остаток партии (${m.maxQty}).`; return; }
      }
    }
    step = Math.min(totalSteps, step + 1);
  }

  function handlePrevStep() {
    validationError = null;
    step = Math.max(1, step - 1);
  }

  async function handleSaveAppointment(finalStatus: AppointmentStatus) {
    validationError = null;
    if (!selectedClient || !selectedPatient || !selectedVet) {
      validationError = 'Заполните информацию о клиенте, пациенте и лечащем враче';
      return;
    }
    const srvPayload: AppointmentService[] = apptServices.map((s) => ({ serviceId: s.serviceId, quantity: s.quantity, priceSnapshot: s.price }));
    const matPayload: AppointmentMaterial[] = apptMaterials.map((m) => ({
      materialId: m.materialId, batchId: m.batchId, quantity: m.quantity,
      unitCostSnapshot: m.purchasePrice, clientPriceSnapshot: m.clientPrice
    }));
    const dgPayload: AppointmentDiagnosis[] = apptDiagnoses.map((d) => ({ diagnosisId: d.diagnosisId, isPreliminary: d.isPreliminary, doctorComment: d.doctorComment }));

    const payload: any = {
      clientId: selectedClient.id, patientId: selectedPatient.id, vetId: selectedVet.id, branchId: activeBranchId,
      appointmentDate: selectedDate, timeSlot: selectedSlot, status: finalStatus, notes: comments,
      services: srvPayload, materials: matPayload, diagnoses: dgPayload, totalAmount: rawTotalPayable
    };
    if (editingAppointmentId) payload.id = editingAppointmentId;

    try {
      const saved = await apiClient.appointments.save(payload);
      onSuccess(saved);
    } catch (err: any) {
      validationError = err.message;
    }
  }

  const stepNames = ['Клиент', 'Питомец', 'Расписание', 'Услуги', 'Расходники', 'Диагнозы', 'Сводка'];
  const branchName = $derived(branches.find((b) => b.id === activeBranchId)?.name ?? '');
</script>

<div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50">
  <div class="bg-white w-full max-w-4xl h-[90vh] rounded-xl shadow-xl flex flex-col overflow-hidden border border-slate-200">
    <!-- Заголовок модального окна -->
    <div class="p-4 bg-teal-700 text-white flex items-center justify-between">
      <div class="flex items-center gap-2">
        <Layers class="w-5 h-5 opacity-90" />
        <div>
          <h2 class="text-sm font-semibold text-white">Регистрация и оформление приёма</h2>
          <p class="text-[10px] opacity-85">Пошаговый мастер записи • {branchName}</p>
        </div>
      </div>
      <button onclick={onClose} class="p-1 hover:bg-white/15 rounded text-white cursor-pointer transition"><X class="w-4 h-4" /></button>
    </div>

    <!-- Индикатор шагов -->
    <div class="bg-slate-50 border-b border-slate-100 p-3 px-6 flex items-center justify-between text-xs text-slate-500 font-medium select-none">
      {#each stepNames as name, idx (idx)}
        {@const stepNum = idx + 1}
        {@const isActive = stepNum === step}
        {@const isCompleted = stepNum < step}
        <div class="flex items-center gap-2 flex-1 last:flex-none">
          <span class="w-5 h-5 rounded-full flex items-center justify-center font-bold text-[10px] {isActive ? 'bg-teal-700 text-white' : isCompleted ? 'bg-teal-50 text-teal-700 border border-teal-700' : 'bg-slate-200 text-slate-400'}">{stepNum}</span>
          <span class="hidden md:inline {isActive ? 'text-teal-700 font-semibold' : ''}">{name}</span>
          {#if idx < totalSteps - 1}<span class="h-0.5 bg-slate-200 flex-1 mx-2"></span>{/if}
        </div>
      {/each}
    </div>

    <!-- Основной контент мастера -->
    <div class="flex-1 overflow-y-auto p-6 bg-slate-50/30">
      {#if validationError}
        <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-md text-xs text-red-700 flex items-start gap-2">
          <AlertCircle class="w-4 h-4 shrink-0 mt-0.5" />
          <span>{validationError}</span>
        </div>
      {/if}

      {#if step === 1}
        <!-- ШАГ 1: Поиск или регистрация владельца -->
        <div class="space-y-4">
          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide mb-3">Шаг 1: Поиск или регистрация владельца</h3>
            <div class="space-y-4">
              <div class="relative">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400 pointer-events-none"><Search class="w-4 h-4" /></span>
                <input
                  type="text"
                  placeholder="Введите фамилию клиента или номер телефона для поиска..."
                  bind:value={clientSearch}
                  oninput={() => { selectedClient = null; selectedPatient = null; }}
                  class="w-full pl-9 pr-4 py-2 border border-slate-200 rounded-md text-xs focus:outline-none focus:border-teal-700 bg-white"
                />
              </div>

              {#if filteredClients.length > 0}
                <div class="border border-slate-100 rounded-md divide-y divide-slate-100 max-h-40 overflow-y-auto">
                  {#each filteredClients as c (c.id)}
                    <button
                      type="button"
                      onclick={() => { selectedClient = c; clientSearch = ''; selectedPatient = null; }}
                      class="w-full text-left p-3 text-xs flex items-center justify-between cursor-pointer hover:bg-slate-50 transition {selectedClient?.id === c.id ? 'bg-teal-50/50 border-l-2 border-teal-700' : ''}"
                    >
                      <span>
                        <span class="font-medium text-slate-800 block">{c.name}</span>
                        <span class="text-[10px] text-slate-400">{c.phone} • {c.email || 'нет email'}</span>
                      </span>
                      <span class="text-[10px] bg-slate-100 text-slate-500 rounded px-2 py-0.5">Владелец</span>
                    </button>
                  {/each}
                </div>
              {/if}

              {#if selectedClient}
                <div class="p-3 bg-teal-50/40 border border-teal-200 rounded-md text-xs flex items-center justify-between">
                  <div>
                    <div class="font-semibold text-teal-800">{selectedClient.name}</div>
                    <div class="text-[10px] text-slate-500">Телефон: {selectedClient.phone} | Личный кабинет заведен</div>
                  </div>
                  <span class="text-[10px] font-bold text-teal-700 bg-white border border-teal-200 px-2.5 py-1 rounded">Выбран</span>
                </div>
              {/if}

              {#if !selectedClient && !showNewClientForm}
                <div class="text-center py-4">
                  <p class="text-xs text-slate-400 mb-2">Не нашли в реестре? Зарегистрируйте нового пациента</p>
                  <button type="button" onclick={() => (showNewClientForm = true)} class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border border-teal-700 text-teal-700 hover:bg-teal-50 text-xs rounded-md font-medium cursor-pointer transition">
                    <Plus class="w-3.5 h-3.5" /> Добавить в базу
                  </button>
                </div>
              {/if}
            </div>
          </div>

          {#if showNewClientForm}
            <form onsubmit={handleCreateClient} class="p-4 bg-white border border-teal-600/30 rounded-lg shadow-sm space-y-3">
              <div class="flex items-center justify-between pb-2 border-b border-slate-100">
                <span class="text-xs font-semibold text-slate-800">Карточка Нового Клиента</span>
                <button type="button" onclick={() => (showNewClientForm = false)} class="text-slate-500 hover:text-slate-600 text-xs">Отмена</button>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
                <div>
                  <label class="block text-slate-500 mb-1">ФИО Клиента *</label>
                  <input type="text" placeholder="Иванов Пётр Сидорович" required bind:value={newClientData.name} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Телефон *</label>
                  <input type="text" placeholder="+7(999)123-4567" required bind:value={newClientData.phone} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Эл. почта</label>
                  <input type="email" placeholder="client@mail.ru" bind:value={newClientData.email} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
              </div>
              <div class="flex items-center gap-2 pt-1">
                <input type="checkbox" id="consent" bind:checked={newClientData.consentSigned} class="w-4 h-4 rounded accent-teal-700" />
                <label for="consent" class="text-[11px] text-slate-500">Согласен на обработку персональных данных и коммуникацию по СМС/Email</label>
              </div>
              <div class="pt-2 flex justify-end">
                <button type="submit" disabled={!newClientData.consentSigned} class="px-4 py-1.5 bg-teal-700 text-white rounded-md text-xs font-semibold hover:bg-teal-800 disabled:opacity-50 cursor-pointer">Создать и выбрать</button>
              </div>
            </form>
          {/if}
        </div>
      {:else if step === 2 && selectedClient}
        <!-- ШАГ 2: Выбор или регистрация питомца -->
        <div class="space-y-4">
          <div class="p-3 bg-white border border-slate-200 rounded-lg flex items-center justify-between text-xs shadow-sm">
            <div>
              <span class="text-slate-400 block font-normal text-[10px]">Клиент / Владелец:</span>
              <span class="font-semibold text-slate-800">{selectedClient.name} • {selectedClient.phone}</span>
            </div>
            <button onclick={() => (step = 1)} class="text-teal-700 font-medium hover:underline text-xs cursor-pointer">Сменить</button>
          </div>

          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide mb-3">Шаг 2: Выберите питомца</h3>
            {#if clientPatients.length > 0}
              <div class="grid grid-cols-1 md:grid-cols-2 gap-3 mb-4">
                {#each clientPatients as p (p.id)}
                  <button type="button" onclick={() => (selectedPatient = p)} class="text-left p-3 border rounded-lg cursor-pointer transition flex items-start gap-3 hover:bg-slate-50 {selectedPatient?.id === p.id ? 'border-teal-700 bg-teal-50/30' : 'border-slate-200'}">
                    <span class="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-600 font-bold shrink-0 text-xs">{p.species.substring(0, 2)}</span>
                    <span class="text-xs min-w-0">
                      <span class="font-semibold text-slate-800 flex items-center gap-1.5">{p.name}<span class="text-[10px] text-slate-400">({p.gender === 'M' ? 'Самец' : 'Самка'})</span></span>
                      <span class="text-[10px] text-slate-500 truncate block">{p.species} • {p.breed || 'Метис'}</span>
                      <span class="text-[9px] text-slate-400 mt-1 block">Рожд: {p.birthDate} ({p.weight} кг)</span>
                    </span>
                  </button>
                {/each}
              </div>
            {:else}
              <p class="text-xs text-slate-400 font-normal mb-3">У выбранного клиента нет зарегистрированных питомцев.</p>
            {/if}

            {#if !showNewPatientForm}
              <div class="text-center py-2">
                <button type="button" onclick={() => (showNewPatientForm = true)} class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-teal-50 border border-teal-200 text-teal-700 text-xs rounded font-semibold cursor-pointer hover:bg-teal-100 transition">
                  <Plus class="w-3.5 h-3.5" /> Вписать питомца в медкарту
                </button>
              </div>
            {/if}
          </div>

          {#if showNewPatientForm}
            <form onsubmit={handleCreatePatient} class="p-4 bg-white border border-teal-600/30 rounded-lg shadow-sm space-y-3">
              <div class="flex items-center justify-between pb-2 border-b border-slate-100">
                <span class="text-xs font-semibold text-slate-800">Регистрация питомца</span>
                <button type="button" onclick={() => (showNewPatientForm = false)} class="text-slate-500 hover:text-slate-600 text-xs">Отмена</button>
              </div>
              <div class="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
                <div>
                  <label class="block text-slate-500 mb-1">Кличка *</label>
                  <input type="text" required placeholder="Шарик" bind:value={newPatientData.name} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Вид животного *</label>
                  <select bind:value={newPatientData.species} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white">
                    <option value="Кот">Кот</option><option value="Собака">Собака</option><option value="Кролик">Кролик</option>
                    <option value="Попугай">Попугай</option><option value="Хомяк">Хомяк / Грызун</option><option value="Другое">Другое</option>
                  </select>
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Порода</label>
                  <input type="text" placeholder="Например, Лабрадор" bind:value={newPatientData.breed} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Пол *</label>
                  <select bind:value={newPatientData.gender} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white">
                    <option value="M">Самец (М)</option><option value="F">Самка (Ж)</option>
                  </select>
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Дата рождения *</label>
                  <input type="date" required bind:value={newPatientData.birthDate} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
                <div>
                  <label class="block text-slate-500 mb-1">Текущий вес (кг)</label>
                  <input type="number" step="0.01" placeholder="1.50" bind:value={newPatientData.weight} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
                <div class="col-span-2">
                  <label class="block text-slate-500 mb-1">Окрас и приметы</label>
                  <input type="text" placeholder="Черный с белой грудкой, чипирован" bind:value={newPatientData.color} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                </div>
              </div>
              <div class="pt-2 flex justify-end">
                <button type="submit" class="px-4 py-1.5 bg-teal-700 text-white rounded-md text-xs font-semibold hover:bg-teal-800 cursor-pointer">Добавить и выбрать</button>
              </div>
            </form>
          {/if}
        </div>
      {:else if step === 3 && selectedPatient}
        <!-- ШАГ 3: Выбор врача и расписания -->
        <div class="space-y-4">
          <div class="p-3 bg-white border border-slate-200 rounded-lg flex items-center justify-between text-xs shadow-sm">
            <div class="flex gap-4">
              <div><span class="text-slate-400 block text-[10px]">Клиент:</span><span class="font-semibold text-slate-800">{selectedClient?.name}</span></div>
              <div><span class="text-slate-400 block text-[10px]">Пациент:</span><span class="font-semibold text-slate-800">{selectedPatient.name} ({selectedPatient.species})</span></div>
            </div>
            <button onclick={() => (step = 2)} class="text-teal-700 font-medium hover:underline text-xs cursor-pointer">Назад</button>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div class="md:col-span-1 p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
              <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide mb-3">1. Выбор лечащего врача</h3>
              <div class="space-y-2">
                {#each vets as v (v.id)}
                  <button type="button" onclick={() => { selectedVet = v; selectedSlot = ''; }} class="w-full text-left p-3 border rounded-lg cursor-pointer transition text-xs hover:bg-slate-50 {selectedVet?.id === v.id ? 'border-teal-700 bg-teal-50/20 font-medium' : 'border-slate-100'}">
                    <div class="text-slate-800">{v.name}</div>
                    <div class="text-[10px] text-slate-400 mt-1">Доля KPI: {(v.KPIRate * 100).toFixed(0)}%</div>
                  </button>
                {/each}
              </div>
            </div>

            <div class="md:col-span-2 p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
              <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide mb-3">2. Дата и Слоты времени приёма</h3>
              <div class="mb-4">
                <label class="block text-slate-500 mb-1 text-xs">Дата проведения приёма *</label>
                <div class="relative">
                  <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400"><Calendar class="w-4 h-4" /></span>
                  <input type="date" bind:value={selectedDate} onchange={() => (selectedSlot = '')} class="w-full max-w-xs pl-9 pr-4 py-1.5 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white text-xs" />
                </div>
              </div>

              {#if selectedVet}
                <p class="text-[10px] text-slate-400 mb-2">Доступные окна на <span class="font-medium text-slate-700">{selectedDate}</span> для {selectedVet.name}:</p>
                <div class="grid grid-cols-4 gap-2">
                  {#each availableSlots as slotObj (slotObj.time)}
                    <button
                      type="button"
                      disabled={slotObj.isBusy}
                      onclick={() => (selectedSlot = slotObj.time)}
                      class="p-2 rounded text-center font-mono text-[11px] border cursor-pointer transition {slotObj.isBusy ? 'bg-slate-100 text-slate-300 border-slate-200 cursor-not-allowed line-through' : selectedSlot === slotObj.time ? 'bg-teal-700 text-white border-teal-700 font-bold' : 'bg-white text-slate-700 border-slate-200 hover:bg-slate-50'}"
                    >{slotObj.time}</button>
                  {/each}
                </div>
              {:else}
                <div class="text-center py-8 text-slate-400 text-xs">Выберите врача слева, чтобы открыть расписание свободных часов</div>
              {/if}
            </div>
          </div>
        </div>
      {:else if step === 4}
        <!-- ШАГ 4: Назначение услуг -->
        <div class="space-y-4">
          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <div class="flex items-center justify-between mb-4">
              <span class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Шаг 4: Назначенные услуги</span>
              <button type="button" onclick={handleAddServiceRow} class="inline-flex items-center gap-1 px-3 py-1 bg-teal-700 text-white text-xs rounded font-medium hover:bg-teal-800 cursor-pointer"><Plus class="w-3.5 h-3.5" /> Вписать услугу</button>
            </div>

            {#if apptServices.length > 0}
              <div class="space-y-2 max-h-80 overflow-y-auto">
                <table class="w-full border-collapse">
                  <thead>
                    <tr class="text-left text-slate-400 text-[10px] uppercase font-bold border-b border-slate-100">
                      <th class="pb-2 w-[45%]">Услуга из каталога</th><th class="pb-2 w-[15%]">Кол-во</th>
                      <th class="pb-2 w-[20%]">Цена (руб)</th><th class="pb-2 w-[15%] text-right">Сумма</th><th class="pb-2 w-[5%]"></th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    {#each apptServices as row, index (index)}
                      <tr class="text-xs">
                        <td class="py-2.5 pr-2">
                          <select value={row.serviceId} onchange={(e) => handleServiceChange(index, e.currentTarget.value)} class="w-full p-1.5 border border-slate-200 rounded text-xs bg-white text-slate-800 focus:outline-none focus:border-teal-700">
                            {#each services as s (s.id)}<option value={s.id}>[{s.category}] {s.name} ({s.defaultPrice} руб.)</option>{/each}
                          </select>
                        </td>
                        <td class="py-2.5 pr-2"><input type="number" min="1" bind:value={row.quantity} class="w-full max-w-[60px] p-1.5 border border-slate-200 rounded text-xs bg-white focus:outline-none" /></td>
                        <td class="py-2.5 pr-2"><input type="number" min="0" bind:value={row.price} class="w-full max-w-[100px] p-1.5 border border-slate-200 rounded text-xs bg-white focus:outline-none" /></td>
                        <td class="py-2.5 text-right font-medium text-slate-800">{row.price * row.quantity} руб.</td>
                        <td class="py-2.5 text-center"><button type="button" onclick={() => handleRemoveServiceRow(index)} class="p-1 hover:bg-slate-100 rounded text-red-600 cursor-pointer"><Trash2 class="w-3.5 h-3.5" /></button></td>
                      </tr>
                    {/each}
                  </tbody>
                </table>
              </div>
            {:else}
              <div class="text-center py-8 text-slate-400 text-xs">Нет добавленных услуг. Добавьте хотя бы одну строку, нажав «вписать услугу».</div>
            {/if}

            <div class="mt-4 pt-4 border-t border-slate-100 flex justify-end text-xs font-semibold text-slate-900">Подитог по услугам: {rawServicesTotal} руб.</div>
          </div>
        </div>
      {:else if step === 5}
        <!-- ШАГ 5: Списание материалов -->
        <div class="space-y-4">
          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <div class="flex items-center justify-between mb-4">
              <div>
                <span class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Шаг 5: Списание расходников и препаратов</span>
                <p class="text-[10px] text-slate-400 mt-1">Остатки списываются по принципу FIFO (первым выбывает товар с ближайшим сроком годности)</p>
              </div>
              <button type="button" onclick={handleAddMaterialRow} class="inline-flex items-center gap-1 px-3 py-1 bg-teal-700 text-white text-xs rounded font-medium hover:bg-teal-800 cursor-pointer"><Plus class="w-3.5 h-3.5" /> Добавить материал</button>
            </div>

            {#if apptMaterials.length > 0}
              <div class="space-y-2 max-h-80 overflow-y-auto">
                <table class="w-full border-collapse">
                  <thead>
                    <tr class="text-left text-slate-400 text-[10px] uppercase font-bold border-b border-slate-100">
                      <th class="pb-2 w-[40%]">Расходный материал</th><th class="pb-2 w-[25%]">Рекомендуемая партия *</th>
                      <th class="pb-2 w-[10%]">Доступно</th><th class="pb-2 w-[10%]">Списано</th><th class="pb-2 w-[10%] text-right">Наценка</th><th class="pb-2 w-[5%]"></th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100">
                    {#each apptMaterials as row, index (index)}
                      {@const availBatches = batches.filter((b) => b.materialId === row.materialId && b.remainingQuantity > 0)}
                      <tr class="text-xs">
                        <td class="py-2.5 pr-2">
                          <select value={row.materialId} onchange={(e) => handleMaterialChange(index, e.currentTarget.value)} class="w-full p-1.5 border border-slate-200 rounded text-xs bg-white text-slate-800 focus:outline-none focus:border-teal-700">
                            {#each materials as m (m.id)}<option value={m.id}>[{m.category}] {m.name} ({m.sku})</option>{/each}
                          </select>
                        </td>
                        <td class="py-2.5 pr-2">
                          <select value={row.batchId} onchange={(e) => handleBatchChange(index, e.currentTarget.value)} required class="w-full p-1.5 border border-slate-200 rounded text-xs bg-white text-slate-800 focus:outline-none">
                            {#if availBatches.length > 0}
                              {#each availBatches as b (b.id)}<option value={b.id}>Партия {b.id.toUpperCase()} (до {b.expiryDate})</option>{/each}
                            {:else}
                              <option value="" disabled>НЕТ ОСТАТКОВ НА СКЛАДЕ!</option>
                            {/if}
                          </select>
                        </td>
                        <td class="py-2.5 pr-2 font-mono text-[11px] text-slate-500">{row.maxQty} шт.</td>
                        <td class="py-2.5 pr-2"><input type="number" min="1" max={row.maxQty} bind:value={row.quantity} class="w-full max-w-[60px] p-1.5 border border-slate-200 rounded text-xs bg-white focus:outline-none" /></td>
                        <td class="py-2.5 text-right font-medium text-slate-800">{row.clientPrice * row.quantity} руб.</td>
                        <td class="py-2.5 text-center"><button type="button" onclick={() => handleRemoveMaterialRow(index)} class="p-1 hover:bg-slate-100 rounded text-red-600 cursor-pointer"><Trash2 class="w-3.5 h-3.5" /></button></td>
                      </tr>
                    {/each}
                  </tbody>
                </table>
              </div>
            {:else}
              <div class="text-center py-8 text-slate-400 text-xs">Расходные материалы не вписывались в приём. Если использовались медикаменты, впишите их через кнопку выше.</div>
            {/if}

            <div class="mt-4 pt-4 border-t border-slate-100 flex justify-end text-xs font-semibold text-slate-900">Подитог по расходникам (для клиента): {rawMaterialsTotal} руб.</div>
          </div>
        </div>
      {:else if step === 6}
        <!-- ШАГ 6: Фиксация диагнозов -->
        <div class="space-y-4">
          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <div class="flex items-center justify-between mb-4">
              <div>
                <span class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Шаг 6: Фиксация диагнозов</span>
                <p class="text-[10px] text-slate-400 mt-1">Окончательные диагнозы запираются в паспорте пациента</p>
              </div>
              <button type="button" onclick={handleAddDiagnosisRow} class="inline-flex items-center gap-1 px-3 py-1 bg-teal-700 text-white text-xs rounded font-medium hover:bg-teal-800 cursor-pointer"><Plus class="w-3.5 h-3.5" /> Добавить диагноз</button>
            </div>

            {#if apptDiagnoses.length > 0}
              <div class="space-y-3 max-h-80 overflow-y-auto">
                {#each apptDiagnoses as row, index (index)}
                  <div class="p-3 border border-slate-100 rounded-lg bg-slate-50/50 flex flex-col md:flex-row items-start md:items-center gap-4 text-xs">
                    <div class="flex-1 min-w-0">
                      <label class="block text-[10px] text-slate-400 mb-1">Ветеринарный клинический диагноз</label>
                      <select bind:value={row.diagnosisId} class="w-full p-2 border border-slate-200 rounded text-xs bg-white">
                        {#each diagnosesEntries as dg (dg.id)}<option value={dg.id}>{dg.code} - {dg.name} ({dg.category})</option>{/each}
                      </select>
                    </div>
                    <div class="w-full md:w-auto mt-2 md:mt-0 flex items-center gap-2 pt-4">
                      <input type="checkbox" id="prelim-{index}" bind:checked={row.isPreliminary} class="w-4 h-4 rounded accent-teal-700" />
                      <label for="prelim-{index}" class="text-xs font-semibold text-slate-700">Предварительный</label>
                    </div>
                    <div class="flex-[1.5] w-full">
                      <label class="block text-[10px] text-slate-400 mb-1">Свободный комментарий / ход лечения</label>
                      <input type="text" placeholder="Опишите состояние животного, назначения лекарств или рекомендации..." bind:value={row.doctorComment} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                    </div>
                    <button type="button" onclick={() => handleRemoveDiagnosisRow(index)} class="pt-4 text-red-600 hover:bg-slate-100 p-1.5 rounded text-xs"><Trash2 class="w-4 h-4" /></button>
                  </div>
                {/each}
              </div>
            {:else}
              <div class="text-center py-8 text-slate-400 text-xs shadow-inner bg-slate-50/40 rounded">Диагнозы не внесены. Рекомендуется зафиксировать хотя бы предварительный симптом / комментарий врача.</div>
            {/if}
          </div>
        </div>
      {:else if step === 7 && selectedClient && selectedPatient && selectedVet}
        <!-- ШАГ 7: Итоговая сводка по приему -->
        <div class="space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm space-y-3 text-xs">
              <h4 class="text-slate-800 font-bold border-b border-slate-100 pb-1.5 uppercase tracking-wide text-[10px]">Паспорт приёма и Медкарта</h4>
              <div class="space-y-1.5">
                <div><span class="text-slate-400 font-medium">Владелец:</span> <span class="text-slate-800 font-semibold">{selectedClient.name} ({selectedClient.phone})</span></div>
                <div><span class="text-slate-400 font-medium">Пациент:</span> <span class="text-slate-800 font-semibold">{selectedPatient.name} ({selectedPatient.species} • {selectedPatient.breed || 'метис'})</span></div>
                <div><span class="text-slate-400 font-medium">Ведущий врач:</span> <span class="text-slate-800 font-semibold">{selectedVet.name}</span></div>
                <div><span class="text-slate-400 font-medium">Слот времени:</span> <span class="text-slate-800 font-semibold font-mono text-[11px]">{selectedDate} в {selectedSlot}</span></div>
              </div>
              <div class="pt-2">
                <label class="block text-slate-500 mb-1 font-semibold">Общие клинические заметки / Анамнез</label>
                <textarea placeholder="Опишите состояние зрачков, температура, жалобы владельца..." bind:value={comments} rows="3" class="w-full p-2 border border-slate-200 rounded text-xs focus:outline-none focus:border-teal-700 bg-white"></textarea>
              </div>
            </div>

            <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm flex flex-col justify-between text-xs">
              <div>
                <h4 class="text-slate-800 font-bold border-b border-slate-100 pb-1.5 uppercase tracking-wide text-[10px] mb-2">Финансовое начисление</h4>
                <div class="space-y-2 max-h-36 overflow-y-auto">
                  {#each apptServices as row, idx (idx)}
                    <div class="flex justify-between text-[11px] text-slate-600"><span>{services.find((s) => s.id === row.serviceId)?.name} (x{row.quantity})</span><span>{row.price * row.quantity} руб.</span></div>
                  {/each}
                  {#each apptMaterials as row, idx (idx)}
                    <div class="flex justify-between text-[11px] text-slate-600"><span>{materials.find((m) => m.id === row.materialId)?.name} (x{row.quantity})</span><span>{row.clientPrice * row.quantity} {row.clientPrice === 0 ? '(Расходник клиники)' : 'руб.'}</span></div>
                  {/each}
                  {#if apptServices.length === 0 && apptMaterials.length === 0}
                    <span class="text-slate-400 italic font-normal">Строк к оплате не начислено</span>
                  {/if}
                </div>
              </div>
              <div class="pt-4 border-t border-slate-100 mt-2 space-y-1">
                <div class="flex justify-between text-xs text-slate-500"><span>Назначенные услуги:</span><span>{rawServicesTotal} руб.</span></div>
                <div class="flex justify-between text-xs text-slate-500"><span>Лекарства и препараты:</span><span>{rawMaterialsTotal} руб.</span></div>
                <div class="flex justify-between text-sm font-bold text-slate-950 pt-2 border-t border-slate-200 border-dashed"><span>ИТОГО К ОПЛАТЕ:</span><span class="font-mono">{rawTotalPayable} руб.</span></div>
              </div>
            </div>
          </div>

          {#if apptDiagnoses.length > 0}
            <div class="p-3 bg-teal-50/20 border border-slate-200 rounded-lg text-xs space-y-1">
              <span class="text-[10px] font-bold text-slate-500 uppercase tracking-tight block">Зафиксированные диагнозы для карты питомца:</span>
              <div class="divide-y divide-slate-100">
                {#each apptDiagnoses as dRow, idx (idx)}
                  {@const dg = diagnosesEntries.find((i) => i.id === dRow.diagnosisId)}
                  <div class="py-1 text-[11px] flex items-center justify-between">
                    <span class="text-slate-800">
                      <span class="font-mono bg-teal-50 px-1 font-bold text-teal-800 rounded mr-1.5">{dg?.code}</span>{dg?.name}
                      {#if dRow.doctorComment}<span class="text-slate-400 text-[10px] block font-normal ml-[55px]">Комментарий: {dRow.doctorComment}</span>{/if}
                    </span>
                    <span class="text-[9px] px-1.5 py-0.5 rounded {dRow.isPreliminary ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800 font-bold'}">{dRow.isPreliminary ? 'Предварительный' : 'Окончательный'}</span>
                  </div>
                {/each}
              </div>
            </div>
          {/if}
        </div>
      {/if}
    </div>

    <!-- Панель управления шагами -->
    <div class="p-4 bg-slate-50 border-t border-slate-200 flex items-center justify-between shrink-0">
      <div>
        {#if step > 1}
          <button type="button" onclick={handlePrevStep} class="px-4 py-1.5 bg-white border border-slate-200 rounded text-slate-600 font-semibold hover:bg-slate-50 text-xs cursor-pointer select-none transition">Назад</button>
        {/if}
      </div>
      <div class="flex items-center gap-2">
        {#if step < totalSteps}
          <button type="button" onclick={handleNextStep} class="px-4 py-1.5 bg-teal-700 text-white rounded text-xs font-semibold hover:bg-teal-800 cursor-pointer select-none transition">Продолжить</button>
        {:else}
          <button type="button" onclick={() => handleSaveAppointment('Planned')} class="px-4 py-1.5 bg-white border border-teal-700 text-teal-700 rounded text-xs font-semibold hover:bg-teal-50 cursor-pointer select-none transition">Черновик записи (статус запланирован)</button>
          <button type="button" onclick={() => handleSaveAppointment('Closed')} class="px-4 py-1.5 bg-teal-700 text-white rounded text-xs font-semibold hover:bg-teal-800 cursor-pointer select-none transition">Провести транзакцию и закрыть приём</button>
        {/if}
      </div>
    </div>
  </div>
</div>
