<script lang="ts">
  import { Calendar, Plus, Filter, CheckCircle, X } from 'lucide-svelte';
  import { apiClient, ApiDb } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type { Appointment, Client, Patient, Employee, AppointmentStatus } from '$lib/types';
  import DataTable from './DataTable.svelte';
  import StatusBadge from './StatusBadge.svelte';

  let appointments = $state<Appointment[]>([]);
  let clients = $state<Client[]>([]);
  let patients = $state<Patient[]>([]);
  let vets = $state<Employee[]>([]);

  let detailedClient = $state<Client | null>(null);
  let detailedPatient = $state<Patient | null>(null);
  let showNewPetModal = $state(false);
  let showNewClientModal = $state(false);

  // Состояние модального окна добавления питомца
  let isNewClient = $state(true);
  let modalSelectedClientId = $state('');
  let modalClient = $state({ name: '', phone: '', email: '', consent: true });
  let modalPet = $state({ name: '', species: 'Кот', breed: '', gender: 'M' as 'M' | 'F', weight: '', color: '', birthDate: '', chipNumber: '' });
  let modalError = $state<string | null>(null);
  let modalSuccess = $state<string | null>(null);

  // Состояние модального окна добавления клиента
  let registryClient = $state({ name: '', phone: '', email: '', consent: true });
  let registryError = $state<string | null>(null);
  let registrySuccess = $state<string | null>(null);

  // Фильтры
  let selectedDoctor = $state('');
  let selectedStatus = $state('');
  let selectedDate = $state('');

  async function loadData() {
    const branchId = app.activeBranchId;
    const appts = await apiClient.appointments.list();
    appointments = appts.filter((a) => a.branchId === branchId);
    clients = await apiClient.clients.list();
    patients = await apiClient.patients.list();
    const employees = await apiClient.employees.list();
    vets = employees.filter((e) => e.status === 'active' && e.branchIds.includes(branchId) && e.positionId !== 'pos-4');
  }

  $effect(() => {
    // перезапуск при изменении subview, филиала или перезагрузке из модалки
    app.activeView;
    app.activeBranchId;
    app.reloadToken;
    loadData();
  });

  const today = new Date().toISOString().split('T')[0];

  const queueStats = $derived.by(() => {
    const todayAppts = appointments.filter((a) => a.appointmentDate === today);
    return {
      totalToday: todayAppts.length,
      planned: todayAppts.filter((a) => a.status === 'Planned').length,
      inProgress: todayAppts.filter((a) => a.status === 'InProgress').length,
      closed: todayAppts.filter((a) => a.status === 'Closed').length
    };
  });

  async function handleChangeStatus(apptId: string, newStatus: AppointmentStatus) {
    const original = await apiClient.appointments.get(apptId);
    if (original) {
      await apiClient.appointments.save({ ...original, status: newStatus });
      loadData();
    }
  }

  function launchScheduler(id?: string) {
    if (id) app.schedulerApptId = id;
    else app.showSchedulerNew = true;
  }
  function launchCashier(id: string) {
    app.cashierApptId = id;
  }

  async function handleSaveNewPet(e: SubmitEvent) {
    e.preventDefault();
    modalError = null;
    if (!modalPet.name.trim()) { modalError = 'Пожалуйста, введите кличку питомца.'; return; }
    let targetClientId = '';
    if (isNewClient) {
      if (!modalClient.name.trim() || !modalClient.phone.trim()) { modalError = 'Пожалуйста, заполните ФИО владельца и телефон.'; return; }
      try {
        const saved = await apiClient.clients.save({ name: modalClient.name.trim(), phone: modalClient.phone.trim(), email: modalClient.email.trim(), consentSigned: modalClient.consent });
        targetClientId = saved.id;
      } catch (err: any) { modalError = err.message || 'Ошибка создания клиента.'; return; }
    } else {
      if (!modalSelectedClientId) { modalError = 'Пожалуйста, выберите владельца из списка.'; return; }
      targetClientId = modalSelectedClientId;
    }
    try {
      await apiClient.patients.save({
        clientId: targetClientId, name: modalPet.name.trim(), species: modalPet.species,
        breed: modalPet.breed.trim() || 'Метис', gender: modalPet.gender, weight: parseFloat(modalPet.weight) || 0,
        color: modalPet.color.trim() || 'не указан', birthDate: modalPet.birthDate || today,
        chipNumber: modalPet.chipNumber.trim() || undefined
      });
      modalSuccess = 'Карта питомца успешно добавлена!';
      setTimeout(() => {
        showNewPetModal = false;
        modalSuccess = null;
        modalClient = { name: '', phone: '', email: '', consent: true };
        modalPet = { name: '', species: 'Кот', breed: '', gender: 'M', weight: '', color: '', birthDate: '', chipNumber: '' };
        loadData();
      }, 1500);
    } catch (err: any) { modalError = err.message || 'Ошибка создания питомца.'; }
  }

  async function handleSaveDirectClient(e: SubmitEvent) {
    e.preventDefault();
    registryError = null;
    if (!registryClient.name.trim() || !registryClient.phone.trim()) { registryError = 'Пожалуйста, заполните ФИО владельца и телефон.'; return; }
    try {
      await apiClient.clients.save({ name: registryClient.name.trim(), phone: registryClient.phone.trim(), email: registryClient.email.trim(), consentSigned: registryClient.consent });
      registrySuccess = 'Клиент успешно добавлен в систему!';
      setTimeout(() => {
        showNewClientModal = false;
        registrySuccess = null;
        registryClient = { name: '', phone: '', email: '', consent: true };
        loadData();
      }, 1500);
    } catch (err: any) { registryError = err.message || 'Ошибка создания клиента.'; }
  }

  const todaysUpcoming = $derived(
    appointments.filter((a) => a.appointmentDate === today && a.status !== 'Closed' && a.status !== 'Cancelled').sort((a, b) => a.timeSlot.localeCompare(b.timeSlot))
  );

  const filteredAppts = $derived(
    appointments.filter((a) => {
      if (selectedDoctor && a.vetId !== selectedDoctor) return false;
      if (selectedStatus && a.status !== selectedStatus) return false;
      if (selectedDate && a.appointmentDate !== selectedDate) return false;
      return true;
    }).sort((a, b) => b.appointmentDate.localeCompare(a.appointmentDate) || b.timeSlot.localeCompare(a.timeSlot))
  );

  const kanbanColumns: { title: string; status: AppointmentStatus; bg: string; border: string }[] = [
    { title: 'Ожидают приёма', status: 'Planned', bg: 'bg-blue-50/50', border: 'border-t-blue-500' },
    { title: 'В кабинете врача', status: 'InProgress', bg: 'bg-amber-50/50', border: 'border-t-amber-500' },
    { title: 'Приём завершён', status: 'Closed', bg: 'bg-emerald-50/50', border: 'border-t-emerald-500' }
  ];

  const apptColumns = [
    { header: 'ID', sortKey: 'id' },
    { header: 'Дата / Время', sortKey: 'appointmentDate' },
    { header: 'Пациент' },
    { header: 'Владелец' },
    { header: 'Врач' },
    { header: 'Сумма (руб)', sortKey: 'totalAmount' },
    { header: 'Статус', sortKey: 'status' }
  ];

  const clientColumns = [
    { header: 'ФИО Клиента / Владельца', sortKey: 'name' },
    { header: 'Семейный телефон', sortKey: 'phone' },
    { header: 'Электронная почта' },
    { header: 'Согласие ПД' },
    { header: 'Зарегистрирован', sortKey: 'createdAt' }
  ];

  const patientColumns = [
    { header: 'Кличка питомца', sortKey: 'name' },
    { header: 'Вид', sortKey: 'species' },
    { header: 'Порода', sortKey: 'breed' },
    { header: 'Пол' },
    { header: 'Вес (кг)', sortKey: 'weight' },
    { header: 'Окрас' },
    { header: 'Владелец' }
  ];
</script>

{#if app.activeView === 'reception-main'}
  <div class="space-y-6">
    <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm">
        <span class="text-[10px] uppercase font-bold text-slate-400 tracking-wider">Приёмов сегодня</span>
        <div class="text-2xl font-bold text-slate-900 mt-1">{queueStats.totalToday}</div>
      </div>
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm border-l-4 border-l-blue-600">
        <span class="text-[10px] uppercase font-bold text-blue-500 tracking-wider">Ожидают приёма</span>
        <div class="text-2xl font-bold text-blue-800 mt-1">{queueStats.planned}</div>
      </div>
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm border-l-4 border-l-yellow-600">
        <span class="text-[10px] uppercase font-bold text-yellow-500 tracking-wider">В кабинете у врача</span>
        <div class="text-2xl font-bold text-yellow-800 mt-1">{queueStats.inProgress}</div>
      </div>
      <div class="bg-white p-4 border border-slate-200 rounded-lg shadow-sm border-l-4 border-l-emerald-600">
        <span class="text-[10px] uppercase font-bold text-emerald-500 tracking-wider">Закрыто приёмов</span>
        <div class="text-2xl font-bold text-emerald-800 mt-1">{queueStats.closed}</div>
      </div>
    </div>

    <div class="p-5 bg-white border border-slate-200 rounded-lg shadow-sm">
      <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wider mb-4">Быстрые диспетчерские операции</h3>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <button onclick={() => launchScheduler()} class="p-4 bg-teal-50 border border-teal-200 hover:bg-teal-100/50 rounded-lg text-left transition cursor-pointer select-none group flex items-start gap-4">
          <span class="p-2.5 bg-teal-700 rounded text-white group-hover:scale-105 transition shrink-0"><Calendar class="w-5 h-5 text-white" /></span>
          <span>
            <span class="font-semibold text-teal-800 text-xs block">Мастер Регистрации Приёма</span>
            <span class="text-[11px] text-slate-500 mt-0.5 block font-normal">7-шаговый мастер записи владельца, питомца, выбора врача, услуг, резерва FIFO материалов и закрытия</span>
          </span>
        </button>
        <div class="p-4 bg-slate-50 border border-slate-200 rounded-lg flex items-center justify-between text-xs">
          <div>
            <span class="font-semibold text-slate-800 block mb-0.5">Карта клиентов</span>
            <p class="text-[10px] text-slate-400 font-normal">Поиск медицинской истории питомцев по ФИО владельца</p>
          </div>
          <button onclick={() => (showNewPetModal = true)} class="px-3.5 py-1.5 bg-slate-800 hover:bg-slate-900 border border-slate-800 font-bold text-white text-xs rounded transition flex items-center gap-1 cursor-pointer">Новый питомец <Plus class="w-3.5 h-3.5" /></button>
        </div>
      </div>
    </div>

    <div class="p-5 bg-white border border-slate-200 rounded-lg shadow-sm">
      <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wider mb-3">Ближайшая очередь сегодня</h3>
      {#if todaysUpcoming.length > 0}
        <div class="divide-y divide-slate-100 max-h-80 overflow-y-auto">
          {#each todaysUpcoming as a (a.id)}
            {@const c = clients.find((item) => item.id === a.clientId)}
            {@const p = patients.find((item) => item.id === a.patientId)}
            {@const v = vets.find((item) => item.id === a.vetId)}
            <div class="py-3 text-xs flex items-center justify-between hover:bg-slate-50/40 px-2 rounded">
              <div class="flex gap-4 items-center">
                <span class="font-mono bg-slate-100 p-1 px-2 font-bold text-slate-800 rounded">{a.timeSlot}</span>
                <div>
                  <span class="font-semibold text-slate-800">{p?.name || 'Питомец'}</span>
                  <span class="text-[10px] text-slate-400 block font-normal">Владелец: {c?.name} • Врач: {v?.name || 'Терапевт'}</span>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <StatusBadge status={a.status} />
                <button onclick={() => { if (a.status === 'Planned') handleChangeStatus(a.id, 'InProgress'); else if (a.status === 'InProgress') launchScheduler(a.id); }} class="p-1 px-2 border border-slate-200 bg-white text-slate-600 rounded text-[10px] hover:bg-slate-50 cursor-pointer select-none transition">{a.status === 'Planned' ? 'Направить к врачу ➜' : 'Закрыть приём ➜'}</button>
              </div>
            </div>
          {/each}
        </div>
      {:else}
        <p class="text-xs text-slate-400 font-normal italic">Свободная очередь. Нет запланированных приёмов на сегодня.</p>
      {/if}
    </div>
  </div>
{:else if app.activeView === 'reception-queue'}
  <div class="space-y-4">
    <div class="flex justify-between items-center bg-white p-3 border border-slate-200 rounded-lg shadow-sm">
      <div>
        <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Очередь приёмов на сегодня</h3>
        <p class="text-[10px] text-slate-400 font-normal">Приёмы на {today} распределены по этапам. Нажмите кнопку на карточке, чтобы перевести приём на следующий этап.</p>
      </div>
      <button onclick={() => launchScheduler()} class="px-3 py-1 bg-teal-700 text-white text-xs font-semibold rounded hover:bg-teal-800 transition cursor-pointer">+ Записать питомца</button>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      {#each kanbanColumns as col (col.status)}
        {@const list = appointments.filter((a) => a.appointmentDate === today && a.status === col.status).sort((a, b) => a.timeSlot.localeCompare(b.timeSlot))}
        <div class="border border-slate-200 border-t-4 {col.border} {col.bg} p-4 rounded-lg flex flex-col min-h-[300px] shadow-sm">
          <span class="text-sm font-bold text-slate-700 uppercase block mb-3 border-b border-slate-100 pb-2">{col.title} ({list.length})</span>
          <div class="space-y-3 flex-1 overflow-y-auto">
            {#each list as a (a.id)}
              {@const c = clients.find((item) => item.id === a.clientId)}
              {@const p = patients.find((item) => item.id === a.patientId)}
              {@const v = vets.find((item) => item.id === a.vetId)}
              <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-xs space-y-2.5 hover:shadow-md transition">
                <div class="flex justify-between items-center">
                  <span class="font-mono bg-slate-100 px-2 py-0.5 font-bold text-slate-800 text-sm rounded">{a.timeSlot}</span>
                  <span class="text-[10px] text-slate-400 font-mono">{a.id}</span>
                </div>
                <div>
                  <span class="font-bold text-slate-900 block text-base leading-tight">{p?.name}</span>
                  <span class="text-xs text-slate-500 font-normal">{p?.species} • {p?.breed || 'метис'}</span>
                </div>
                <div class="text-xs text-slate-500 font-normal py-2 border-t border-dashed border-slate-100 space-y-0.5">
                  <div>Владелец: <span class="text-slate-700">{c?.name}</span></div>
                  <div>Врач: <span class="text-slate-700">{v?.name}</span></div>
                </div>
                <div class="flex justify-end gap-1 pt-1 border-t border-slate-100">
                  {#if col.status === 'Planned'}
                    <button onclick={() => handleChangeStatus(a.id, 'InProgress')} class="w-full text-center py-2 bg-blue-50 text-blue-700 font-bold text-xs rounded hover:bg-blue-100 select-none cursor-pointer transition">Вызвать в кабинет ➜</button>
                  {:else if col.status === 'InProgress'}
                    <button onclick={() => launchScheduler(a.id)} class="w-full text-center py-2 bg-amber-50 text-amber-700 font-bold text-xs rounded hover:bg-amber-100 select-none cursor-pointer transition">Оформить итог & Закрыть ➜</button>
                  {:else}
                    <button onclick={() => launchCashier(a.id)} class="w-full text-center py-2 bg-emerald-50 text-emerald-700 font-bold text-xs rounded hover:bg-emerald-100 select-none cursor-pointer transition">Принять оплату / Касса ➜</button>
                  {/if}
                </div>
              </div>
            {/each}
            {#if list.length === 0}<div class="text-center py-12 text-xs font-normal text-slate-400 italic">Пусто в этой колонке.</div>{/if}
          </div>
        </div>
      {/each}
    </div>
  </div>
{:else if app.activeView === 'reception-appointments'}
  <div class="space-y-4">
    <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm flex flex-col md:flex-row items-center justify-between gap-3 text-xs">
      <div class="flex flex-wrap items-center gap-3">
        <span class="text-slate-400 font-semibold flex items-center gap-1 shrink-0"><Filter class="w-3.5 h-3.5" /> Фильтры:</span>
        <select bind:value={selectedDoctor} class="p-1 px-2 border border-slate-200 rounded bg-white text-xs focus:outline-none">
          <option value="">Все ветеринары</option>
          {#each vets as v (v.id)}<option value={v.id}>{v.name}</option>{/each}
        </select>
        <select bind:value={selectedStatus} class="p-1 px-2 border border-slate-200 rounded bg-white text-xs focus:outline-none">
          <option value="">Все статусы</option><option value="Planned">Запланирован</option><option value="InProgress">Приём врача</option><option value="Closed">Завершён</option><option value="Cancelled">Отмена</option>
        </select>
        <input type="date" bind:value={selectedDate} class="p-1 px-2 border border-slate-200 rounded bg-white text-xs focus:outline-none" />
      </div>
      {#if selectedDoctor || selectedStatus || selectedDate}
        <button onclick={() => { selectedDoctor = ''; selectedStatus = ''; selectedDate = ''; }} class="text-red-500 font-semibold hover:underline cursor-pointer">Сброс</button>
      {/if}
    </div>

    <DataTable columns={apptColumns} data={filteredAppts} searchPlaceholder="Быстрый поиск по ID приёма..." searchFilter={(a, q) => a.id.toLowerCase().includes(q.toLowerCase())} emptyMessage="Приёмов по выбранным критериям не зафиксировано.">
      {#snippet row(a)}
        {@const pt = patients.find((p) => p.id === a.patientId)}
        <td class="px-4 py-3"><span class="font-mono text-[10px] text-slate-400 uppercase">{a.id}</span></td>
        <td class="px-4 py-3"><span class="font-mono font-medium text-slate-800">{a.appointmentDate} в {a.timeSlot}</span></td>
        <td class="px-4 py-3"><span class="font-semibold text-slate-800">{pt?.name || 'Питомец'} ({pt?.species})</span></td>
        <td class="px-4 py-3 text-slate-700">{clients.find((c) => c.id === a.clientId)?.name || 'Владелец'}</td>
        <td class="px-4 py-3 text-slate-700">{vets.find((e) => e.id === a.vetId)?.name || 'Терапевт'}</td>
        <td class="px-4 py-3"><span class="font-mono font-semibold">{a.totalAmount}</span></td>
        <td class="px-4 py-3"><StatusBadge status={a.status} /></td>
      {/snippet}
      {#snippet actions(a)}
        {#if a.status === 'Closed'}
          <button onclick={() => launchCashier(a.id)} class="px-2.5 py-1 bg-emerald-50 text-emerald-800 font-bold hover:bg-emerald-100 rounded text-[10px] cursor-pointer">Оплата</button>
        {:else if a.status !== 'Cancelled'}
          <button onclick={() => launchScheduler(a.id)} class="px-2.5 py-1 bg-teal-50 text-teal-800 font-bold hover:bg-teal-100 rounded text-[10px] cursor-pointer">Завершить приём</button>
        {/if}
      {/snippet}
    </DataTable>
  </div>
{:else if app.activeView === 'reception-clients'}
  <div class="space-y-4">
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center bg-white p-4 border border-slate-200 rounded-lg shadow-sm gap-2">
      <div>
        <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Реестр клиентов</h3>
        <p class="text-[10px] text-slate-400 font-normal">База данных всех зарегистрированных владельцев и их контактные сведения.</p>
      </div>
      <button onclick={() => (showNewClientModal = true)} class="px-3.5 py-1.5 bg-teal-700 hover:bg-teal-800 text-white text-xs font-bold rounded transition flex items-center gap-1 cursor-pointer">Добавить клиента <Plus class="w-3.5 h-3.5" /></button>
    </div>

    <DataTable columns={clientColumns} data={clients} searchPlaceholder="Введите ФИО или телефон клиента для поиска..." searchFilter={(c, q) => c.name.toLowerCase().includes(q.toLowerCase()) || c.phone.includes(q)}>
      {#snippet row(c)}
        <td class="px-4 py-3"><span class="font-semibold text-slate-800 text-[13px]">{c.name}</span></td>
        <td class="px-4 py-3"><span class="font-mono">{c.phone}</span></td>
        <td class="px-4 py-3 text-slate-700">{#if c.email}{c.email}{:else}<span class="text-slate-400 italic font-normal">нет</span>{/if}</td>
        <td class="px-4 py-3">{#if c.consentSigned}<span class="text-emerald-700 font-semibold">Подписано</span>{:else}<span class="text-red-600 font-normal">Нет согласия</span>{/if}</td>
        <td class="px-4 py-3 text-slate-700">{c.createdAt}</td>
      {/snippet}
      {#snippet actions(c)}
        <button onclick={() => (detailedClient = c)} class="px-2.5 py-1 bg-teal-50 text-teal-800 font-bold hover:bg-teal-100 border border-teal-100 rounded text-[10px] cursor-pointer">Карточка клиента ➜</button>
      {/snippet}
    </DataTable>
  </div>
{:else if app.activeView === 'reception-patients'}
  <div class="space-y-4">
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center bg-white p-4 border border-slate-200 rounded-lg shadow-sm gap-2">
      <div>
        <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Реестр питомцев (Ветеринарные паспорта)</h3>
        <p class="text-[10px] text-slate-400 font-normal">Электронные медицинские паспорта, хронология диагнозов и сведения о лечении.</p>
      </div>
      <button onclick={() => (showNewPetModal = true)} class="px-3.5 py-1.5 bg-teal-700 hover:bg-teal-800 text-white text-xs font-bold rounded transition flex items-center gap-1 cursor-pointer">Добавить питомца <Plus class="w-3.5 h-3.5" /></button>
    </div>

    <DataTable columns={patientColumns} data={patients} searchPlaceholder="Поиск питомца по кличке или виду..." searchFilter={(p, q) => p.name.toLowerCase().includes(q.toLowerCase()) || p.species.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(p)}
        <td class="px-4 py-3"><span class="font-bold text-teal-800">{p.name}</span></td>
        <td class="px-4 py-3"><span class="font-medium">{p.species}</span></td>
        <td class="px-4 py-3 text-slate-700">{p.breed || 'Метис'}</td>
        <td class="px-4 py-3 text-slate-700">{p.gender === 'M' ? 'Самец' : 'Самка'}</td>
        <td class="px-4 py-3"><span class="font-mono">{p.weight}</span></td>
        <td class="px-4 py-3 text-slate-700">{p.color}</td>
        <td class="px-4 py-3 text-slate-700">{clients.find((c) => c.id === p.clientId)?.name || 'N/A'}</td>
      {/snippet}
      {#snippet actions(p)}
        <button onclick={() => (detailedPatient = p)} class="px-2.5 py-1 bg-teal-50 text-teal-800 font-bold hover:bg-teal-100 rounded text-[10px] cursor-pointer">История болезней ➜</button>
      {/snippet}
    </DataTable>
  </div>
{/if}

<!-- Глобальные модальные окна -->
{#if showNewPetModal}
  <div class="fixed inset-0 bg-slate-950/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 overflow-y-auto">
    <div class="bg-white rounded-xl shadow-2xl border border-slate-200 w-full max-w-2xl overflow-hidden my-8 max-h-[95vh] flex flex-col text-xs text-slate-700">
      <div class="p-4 bg-teal-700 text-white flex justify-between items-center shrink-0">
        <span class="font-semibold text-xs text-white">Регистрация нового пациента (питомца)</span>
        <button type="button" onclick={() => (showNewPetModal = false)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
      </div>
      <form onsubmit={handleSaveNewPet} class="p-6 overflow-y-auto space-y-4 bg-slate-50 flex-1 text-xs">
        {#if modalError}<div class="p-3 bg-red-50 border border-red-200 text-red-700 font-semibold rounded-lg">⚠️ {modalError}</div>{/if}
        {#if modalSuccess}<div class="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 font-semibold rounded-lg flex items-center gap-2"><CheckCircle class="w-4 h-4" /><span>{modalSuccess}</span></div>{/if}

        <div class="p-4 bg-white rounded-lg border border-slate-200 shadow-sm space-y-3">
          <h4 class="font-bold text-slate-800 text-xs uppercase tracking-wider">Владелец</h4>
          <div class="flex gap-4 border-b border-slate-100 pb-2">
            <label class="flex items-center gap-1.5 cursor-pointer font-medium select-none text-slate-700"><input type="radio" checked={isNewClient} onchange={() => { isNewClient = true; modalError = null; }} class="accent-teal-700" />Новый клиент</label>
            <label class="flex items-center gap-1.5 cursor-pointer font-medium select-none text-slate-700"><input type="radio" checked={!isNewClient} onchange={() => { isNewClient = false; modalError = null; }} class="accent-teal-700" />Существующий из базы</label>
          </div>
          {#if isNewClient}
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3 pt-1">
              <div><label class="block text-slate-500 mb-1 font-semibold">ФИО владельца *</label><input type="text" placeholder="Иванов Иван Иванович" bind:value={modalClient.name} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
              <div><label class="block text-slate-500 mb-1 font-semibold">Мобильный телефон *</label><input type="text" placeholder="+7 (911) 222-3344" bind:value={modalClient.phone} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
              <div class="md:col-span-2"><label class="block text-slate-500 mb-1 font-semibold">Электронная почта</label><input type="email" placeholder="owner@mail.ru" bind:value={modalClient.email} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
              <div class="md:col-span-2 flex items-center gap-2 pt-1"><input type="checkbox" id="modalClientConsent" bind:checked={modalClient.consent} class="accent-teal-700 w-4 h-4 cursor-pointer" /><label for="modalClientConsent" class="text-slate-500 font-normal cursor-pointer select-none">Получено согласие на обработку персональных данных и отправку СМС-уведомлений</label></div>
            </div>
          {:else}
            <div>
              <label class="block text-slate-500 mb-1 font-semibold">Выберите владельца из базы *</label>
              <select bind:value={modalSelectedClientId} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800">
                <option value="">-- выберите владельца --</option>
                {#each clients as c (c.id)}<option value={c.id}>{c.name} ({c.phone})</option>{/each}
              </select>
            </div>
          {/if}
        </div>

        <div class="p-4 bg-white rounded-lg border border-slate-200 shadow-sm space-y-3">
          <h4 class="font-bold text-slate-800 text-xs uppercase tracking-wider">Сведения о питомце</h4>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div><label class="block text-slate-500 mb-1 font-semibold">Кличка питомца *</label><input type="text" placeholder="Барсик" bind:value={modalPet.name} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
            <div>
              <label class="block text-slate-500 mb-1 font-semibold">Вид животного *</label>
              <select bind:value={modalPet.species} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800">
                <option value="Кот">Кот / Кошка</option><option value="Собака">Собака</option><option value="Птица">Птица</option><option value="Грызун">Грызун</option><option value="Рептилия">Рептилия</option><option value="Другое">Другое</option>
              </select>
            </div>
            <div><label class="block text-slate-500 mb-1 font-semibold">Порода</label><input type="text" placeholder="Британская короткошёрстная" bind:value={modalPet.breed} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
            <div>
              <label class="block text-slate-500 mb-1 font-semibold">Пол питомца *</label>
              <div class="flex gap-4 pt-1.5">
                <label class="flex items-center gap-1.5 cursor-pointer font-medium select-none text-slate-700"><input type="radio" name="modalPetGender" checked={modalPet.gender === 'M'} onchange={() => (modalPet.gender = 'M')} class="accent-teal-700" />Самец</label>
                <label class="flex items-center gap-1.5 cursor-pointer font-medium select-none text-slate-700"><input type="radio" name="modalPetGender" checked={modalPet.gender === 'F'} onchange={() => (modalPet.gender = 'F')} class="accent-teal-700" />Самка</label>
              </div>
            </div>
            <div><label class="block text-slate-500 mb-1 font-semibold">Текущий вес (кг)</label><input type="number" step="0.01" placeholder="4.25" bind:value={modalPet.weight} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
            <div><label class="block text-slate-500 mb-1 font-semibold">Окрас шерсти</label><input type="text" placeholder="Серый табби" bind:value={modalPet.color} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
            <div><label class="block text-slate-500 mb-1 font-semibold">Дата рождения питомца</label><input type="date" bind:value={modalPet.birthDate} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
            <div><label class="block text-slate-500 mb-1 font-semibold">Номер микрочипа (если есть)</label><input type="text" placeholder="643093400123456" bind:value={modalPet.chipNumber} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
          </div>
        </div>

        <div class="pt-2 flex justify-end gap-2 shrink-0">
          <button type="button" onclick={() => (showNewPetModal = false)} class="px-4 py-2 border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 font-bold rounded-lg transition cursor-pointer">Отмена</button>
          <button type="submit" class="px-4 py-2 bg-teal-700 hover:bg-teal-800 text-white font-bold rounded-lg shadow-sm transition cursor-pointer">Создать карту питомца</button>
        </div>
      </form>
    </div>
  </div>
{/if}

{#if showNewClientModal}
  <div class="fixed inset-0 bg-slate-950/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 overflow-y-auto">
    <div class="bg-white rounded-xl shadow-2xl border border-slate-200 w-full max-w-lg overflow-hidden my-8 flex flex-col text-xs text-slate-700">
      <div class="p-4 bg-teal-700 text-white flex justify-between items-center shrink-0">
        <span class="font-semibold text-xs text-white">Регистрация нового клиента</span>
        <button type="button" onclick={() => (showNewClientModal = false)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
      </div>
      <form onsubmit={handleSaveDirectClient} class="p-6 space-y-4 bg-slate-50">
        {#if registryError}<div class="p-3 bg-red-50 border border-red-200 text-red-700 font-semibold rounded-lg">⚠️ {registryError}</div>{/if}
        {#if registrySuccess}<div class="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 font-semibold rounded-lg flex items-center gap-2"><CheckCircle class="w-4 h-4" /><span>{registrySuccess}</span></div>{/if}
        <div class="p-4 bg-white rounded-lg border border-slate-200 shadow-sm space-y-3">
          <div><label class="block text-slate-500 mb-1 font-semibold">ФИО владельца *</label><input type="text" placeholder="Иванов Иван Иванович" bind:value={registryClient.name} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
          <div><label class="block text-slate-500 mb-1 font-semibold">Мобильный телефон *</label><input type="text" placeholder="+7 (911) 222-3344" bind:value={registryClient.phone} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
          <div><label class="block text-slate-500 mb-1 font-semibold">Электронная почта</label><input type="email" placeholder="owner@mail.ru" bind:value={registryClient.email} class="w-full p-2 border border-slate-200 rounded focus:border-teal-700 focus:outline-none text-slate-800" /></div>
          <div class="flex items-center gap-2 pt-1"><input type="checkbox" id="registryClientConsent" bind:checked={registryClient.consent} class="accent-teal-700 w-4 h-4 cursor-pointer" /><label for="registryClientConsent" class="text-slate-500 font-normal cursor-pointer select-none">Получено согласие на обработку персональных данных и отправку СМС-уведомлений</label></div>
        </div>
        <div class="pt-2 flex justify-end gap-2 shrink-0">
          <button type="button" onclick={() => (showNewClientModal = false)} class="px-4 py-2 border border-slate-200 bg-white hover:bg-slate-50 text-slate-600 font-bold rounded-lg transition cursor-pointer">Отмена</button>
          <button type="submit" class="px-4 py-2 bg-teal-700 hover:bg-teal-800 text-white font-bold rounded-lg shadow-sm transition cursor-pointer">Зарегистрировать</button>
        </div>
      </form>
    </div>
  </div>
{/if}

{#if detailedClient}
  {@const ownerPets = patients.filter((p) => p.clientId === detailedClient!.id)}
  {@const clientHistory = appointments.filter((a) => a.clientId === detailedClient!.id)}
  <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs text-slate-700">
    <div class="bg-white w-full max-w-2xl rounded-xl shadow-xl overflow-hidden border border-slate-200">
      <div class="p-4 bg-teal-700 text-white flex justify-between items-center">
        <span class="font-semibold text-xs text-white">Карточка владельца</span>
        <button onclick={() => (detailedClient = null)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
      </div>
      <div class="p-6 space-y-4 bg-slate-50 max-h-[80vh] overflow-y-auto">
        <div class="bg-white p-4 rounded-lg border border-slate-200 space-y-1.5 shadow-sm">
          <h3 class="font-bold text-slate-800 text-sm mb-2">{detailedClient.name}</h3>
          <p class="text-slate-500 font-normal">Телефон: <span class="text-slate-900 font-semibold">{detailedClient.phone}</span></p>
          <p class="text-slate-500 font-normal">Почта: <span class="text-slate-900 font-semibold">{detailedClient.email || 'не указана'}</span></p>
          <p class="text-slate-500 font-normal">Комплаенс ПД СМС: <span class="text-emerald-700 font-bold">{detailedClient.consentSigned ? 'Согласие передано' : 'Не подписано'}</span></p>
        </div>
        <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-widest block mb-1">Закрепленные питомцы ({ownerPets.length})</span>
          <div class="space-y-1 pt-1">
            {#each ownerPets as p (p.id)}
              <div class="p-2 bg-slate-50 border border-slate-100 rounded text-slate-700 flex justify-between items-center font-medium"><span>{p.name} ({p.species} • {p.breed || 'Метис'})</span><span class="text-[10px] text-slate-400 font-mono">{p.weight} кг</span></div>
            {/each}
            {#if ownerPets.length === 0}<span class="italic text-slate-400 font-normal block py-2">Зарегистрированные питомцы отсутствуют.</span>{/if}
          </div>
        </div>
        <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-widest block mb-2">История обращений в филиалы</span>
          <div class="space-y-1.5 max-h-36 overflow-y-auto">
            {#each clientHistory as a (a.id)}
              <div class="text-[11px] flex justify-between items-center text-slate-600 bg-slate-50 p-1.5 rounded"><span>{a.appointmentDate} в {a.timeSlot} (врач {vets.find((v) => v.id === a.vetId)?.name})</span><StatusBadge status={a.status} /></div>
            {/each}
          </div>
        </div>
      </div>
    </div>
  </div>
{/if}

{#if detailedPatient}
  {@const history = appointments.filter((a) => a.patientId === detailedPatient!.id && a.status === 'Closed')}
  <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs">
    <div class="bg-white w-full max-w-2xl rounded-xl shadow-xl overflow-hidden border border-slate-200 text-slate-700">
      <div class="p-4 bg-teal-700 text-white flex justify-between items-center">
        <span class="font-semibold text-xs text-white">Ветеринарный паспорт питомца</span>
        <button onclick={() => (detailedPatient = null)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
      </div>
      <div class="p-6 space-y-4 bg-slate-50 max-h-[80vh] overflow-y-auto">
        <div class="bg-white p-4 rounded-lg border border-slate-200 space-y-1.5 shadow-sm">
          <h3 class="font-bold text-slate-800 text-sm mb-1">{detailedPatient.name}</h3>
          <p class="text-slate-500 font-normal">Вид & Порода: <span class="text-slate-900 font-semibold">{detailedPatient.species} ({detailedPatient.breed || 'метис'})</span></p>
          <p class="text-slate-500 font-normal">Пол: <span class="text-slate-900 font-semibold">{detailedPatient.gender === 'M' ? 'Самец' : 'Самка'}</span></p>
          <p class="text-slate-500 font-normal">Дата рождения: <span class="text-slate-900 font-semibold">{detailedPatient.birthDate}</span></p>
          <p class="text-slate-500 font-normal">Физические параметры: <span class="text-slate-900 font-semibold">{detailedPatient.weight} кг ({detailedPatient.color})</span></p>
        </div>
        <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-widest block mb-1">Медико-клиническая хроника (Диагнозы)</span>
          <div class="space-y-2 mt-2">
            {#each history as a (a.id)}
              <div class="p-2.5 bg-slate-50 border border-slate-100 rounded text-[11px] space-y-1">
                <div class="flex justify-between font-bold"><span>Визит {a.appointmentDate} (Врач: {vets.find((v) => v.id === a.vetId)?.name})</span><span>{a.id.toUpperCase()}</span></div>
                <p class="text-slate-500 font-normal italic">{a.notes || 'Без общего анамнеза'}</p>
                {#if a.diagnoses.length > 0}
                  <div class="pt-1 mt-1 border-t border-slate-200 border-dashed">
                    <span class="text-[9px] font-bold text-slate-400 uppercase">Диагнозы:</span>
                    {#each a.diagnoses as d, i (i)}
                      <div class="text-[10px] text-slate-800">• <span class="font-bold font-mono">[{d.diagnosisId}]</span> {d.isPreliminary ? '(предварительный)' : '(окончательный)'} {d.doctorComment}</div>
                    {/each}
                  </div>
                {/if}
              </div>
            {/each}
            {#if history.length === 0}<p class="text-slate-400 font-normal italic text-[11px] py-2">Сведения о перенесенных заболеваниях отсутствуют.</p>{/if}
          </div>
        </div>
      </div>
    </div>
  </div>
{/if}
