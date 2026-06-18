<script lang="ts">
  import { Calendar, Edit, Trash2, X } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type { Service, ServicePrice, Diagnosis, Employee, Appointment, Branch, Position } from '$lib/types';
  import DataTable from './DataTable.svelte';

  const VET_DEPARTMENTS = [
    'Терапия', 'Хирургия', 'Дерматология', 'Кардиология', 'Офтальмология', 'Онкология', 'Стоматология',
    'Неврология', 'Ортопедия', 'Гастроэнтерология', 'Травматология', 'Анестезиология',
    'Акушерство и Гинекология', 'Урология', 'Эндокринология', 'Инфекционные болезни',
    'УЗИ-Диагностика', 'Рентгенология', 'Лабораторная диагностика'
  ];

  let services = $state<Service[]>([]);
  let servicePrices = $state<ServicePrice[]>([]);
  let diagnoses = $state<Diagnosis[]>([]);
  let vets = $state<Employee[]>([]);
  let appointments = $state<Appointment[]>([]);
  let branches = $state<Branch[]>([]);
  let positions = $state<Position[]>([]);

  let showDiagnosesModal = $state(false);
  let editingDiagnosis = $state<Diagnosis | null>(null);
  let dgFormData = $state({ code: '', name: '', category: 'Новое' });

  let showPriceModal = $state(false);
  let priceFormData = $state({ serviceId: '', price: 1000 });

  async function loadAll() {
    services = await apiClient.services.list();
    servicePrices = await apiClient.servicePrices.list();
    diagnoses = await apiClient.diagnoses.list();
    const employees = await apiClient.employees.list();
    vets = employees.filter((e) => e.status === 'active' && e.positionId !== 'pos-4');
    appointments = await apiClient.appointments.list();
    branches = await apiClient.branches.list();
    positions = await apiClient.positions.list();
  }

  $effect(() => {
    app.activeView;
    app.reloadToken;
    loadAll();
  });

  async function handleSaveDiagnosis(e: SubmitEvent) {
    e.preventDefault();
    if (!dgFormData.code || !dgFormData.name) return;
    const payload: Diagnosis = {
      id: editingDiagnosis ? editingDiagnosis.id : 'dg-' + Math.random().toString(36).substring(2, 11),
      code: dgFormData.code, name: dgFormData.name, category: dgFormData.category
    };
    await apiClient.diagnoses.save(payload);
    app.audit(editingDiagnosis ? 'Изменение диагноза' : 'Добавление диагноза', `${payload.code} — ${payload.name}`);
    showDiagnosesModal = false;
    editingDiagnosis = null;
    dgFormData = { code: '', name: '', category: 'Новое' };
    loadAll();
  }

  async function handleDeleteDiagnosis(id: string) {
    if (confirm('Вы уверены, что хотите удалить данный диагноз из справочника?')) {
      await apiClient.diagnoses.delete(id);
      app.audit('Удаление диагноза', `Диагноз ${id}`);
      loadAll();
    }
  }

  async function handleSavePrice(e: SubmitEvent) {
    e.preventDefault();
    if (!priceFormData.serviceId || priceFormData.price <= 0) return;
    await apiClient.servicePrices.addPrice(priceFormData.serviceId, priceFormData.price);
    app.audit('Изменение цены услуги', `${services.find((s) => s.id === priceFormData.serviceId)?.name ?? priceFormData.serviceId} → ${priceFormData.price} руб.`);
    showPriceModal = false;
    loadAll();
  }

  const today = new Date().toISOString().split('T')[0];
  const defaultSlots = ['09:00', '09:30', '10:00', '10:30', '11:00', '11:30', '12:00', '12:30', '13:00', '13:30', '14:00', '14:30', '15:00', '15:30', '16:00', '16:30', '17:00', '17:30'];
  const clinicBranchVets = $derived(vets.filter((v) => v.branchIds.includes(app.activeBranchId)));

  // Дата просматриваемого расписания (главврач может листать дни)
  let scheduleDate = $state(today);
  function shiftScheduleDate(delta: number) {
    const d = new Date(scheduleDate);
    d.setDate(d.getDate() + delta);
    scheduleDate = d.toISOString().split('T')[0];
  }
  function openBooking(id: string) {
    app.schedulerApptId = id; // открыть карточку приёма (просмотр/оформление)
  }

  const serviceColumns = [
    { header: 'Категория услуг', sortKey: 'category' },
    { header: 'ID' },
    { header: 'Наименование медицинской услуги', sortKey: 'name' },
    { header: 'Текущая цена', sortKey: 'defaultPrice' }
  ];

  const diagnosisColumns = [
    { header: 'Код диагноза', sortKey: 'code' },
    { header: 'Наименование диагноза', sortKey: 'name' },
    { header: 'Раздел заболеваемости', sortKey: 'category' }
  ];

  let priceServiceName = $state('');
  function openPriceForService(s: Service) {
    priceFormData = { serviceId: s.id, price: s.defaultPrice };
    priceServiceName = `[${s.category}] ${s.name}`;
    showPriceModal = true;
  }
  // История последних цен по услуге (для финмодели ServicePrices)
  function priceHistoryFor(serviceId: string): ServicePrice[] {
    return servicePrices
      .filter((p) => p.serviceId === serviceId)
      .sort((a, b) => b.activeFrom.localeCompare(a.activeFrom))
      .slice(0, 3);
  }
  function openNewDiagnosis() {
    editingDiagnosis = null;
    dgFormData = { code: '', name: '', category: 'Общее' };
    showDiagnosesModal = true;
  }
  function openEditDiagnosis(d: Diagnosis) {
    editingDiagnosis = d;
    dgFormData = { code: d.code, name: d.name, category: d.category };
    showDiagnosesModal = true;
  }
</script>

{#if app.activeView === 'clinic-schedule'}
  <div class="space-y-6">
    <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
      <div class="flex items-center justify-between gap-3 flex-wrap">
        <div>
          <div class="flex items-center gap-1.5 mb-2">
            <Calendar class="w-5 h-5 text-teal-700" />
            <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Генеральный диспетчерский календарь всех врачей</h3>
          </div>
          <p class="text-[11px] text-slate-400 font-normal">Загрузка кабинетов клиники на выбранный день. Кликните по слоту, чтобы открыть карточку приёма.</p>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-[10px] font-bold text-slate-400 uppercase">Расписание на:</span>
          <button onclick={() => shiftScheduleDate(-1)} title="Предыдущий день" class="px-2 py-1 border border-slate-200 rounded text-slate-600 hover:bg-slate-50 cursor-pointer text-xs font-bold">‹</button>
          <input type="date" bind:value={scheduleDate} class="p-1.5 px-2 border border-slate-200 rounded bg-white text-xs font-mono focus:outline-none focus:border-teal-700" />
          <button onclick={() => shiftScheduleDate(1)} title="Следующий день" class="px-2 py-1 border border-slate-200 rounded text-slate-600 hover:bg-slate-50 cursor-pointer text-xs font-bold">›</button>
          <button onclick={() => (scheduleDate = today)} class="px-2.5 py-1 border border-slate-200 rounded text-teal-700 hover:bg-teal-50 cursor-pointer text-[11px] font-semibold">Сегодня</button>
        </div>
      </div>
    </div>

    <div class="bg-white border border-slate-200 rounded-lg overflow-x-auto shadow-sm">
      <table class="w-full text-left border-collapse min-w-[700px]">
        <thead>
          <tr class="bg-slate-50 border-b border-slate-200 text-[10px] text-slate-400 uppercase font-bold tracking-wider">
            <th class="p-3 pl-4">Врач (Ветеринар)</th>
            {#each defaultSlots as slot (slot)}<th class="p-3 text-center text-[10px] border-l border-slate-100 font-mono">{slot}</th>{/each}
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 text-xs">
          {#each clinicBranchVets as v (v.id)}
            <tr class="hover:bg-slate-50/30">
              <td class="p-3 pl-4 font-semibold text-slate-800 min-w-[160px]">{v.name}<span class="block text-[9px] text-slate-500 font-normal">{positions.find((p) => p.id === v.positionId)?.name ?? 'Врач'}</span></td>
              {#each defaultSlots as slot (slot)}
                {@const booking = appointments.find((a) => a.vetId === v.id && a.branchId === app.activeBranchId && a.appointmentDate === scheduleDate && a.timeSlot === slot && a.status !== 'Cancelled')}
                <td class="p-2 border-l border-slate-100 text-center min-w-[50px]">
                  {#if booking}
                    <button type="button" onclick={() => openBooking(booking.id)} class="w-full p-1 py-1 px-1.5 rounded text-[9px] text-center font-bold tracking-tight leading-tight cursor-pointer hover:ring-2 hover:ring-teal-300 transition {booking.status === 'Closed' ? 'bg-emerald-100 text-emerald-800' : booking.status === 'InProgress' ? 'bg-amber-100 text-amber-800' : 'bg-blue-100 text-blue-800'}" title="Открыть карточку приёма {booking.id}">{booking.status === 'Closed' ? 'ЗАКРЫТ' : booking.status === 'InProgress' ? 'ПРИЁМ' : 'ЗАПИСЬ'}</button>
                  {:else}
                    <span class="text-slate-200 text-[10px] select-none">-</span>
                  {/if}
                </td>
              {/each}
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>
{:else if app.activeView === 'clinic-services'}
  <div class="space-y-4">
    <div class="bg-white p-3 border border-slate-200 rounded-lg shadow-sm">
      <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Управление Прейскурантом Услуг</h3>
      <p class="text-[10px] text-slate-400 font-normal">Изменение цены — кнопкой в строке услуги. Каждое изменение фиксируется в истории цен (ServicePrices).</p>
    </div>

    <DataTable columns={serviceColumns} data={services} searchPlaceholder="Введите услугу или раздел прайса для поиска..." searchFilter={(s, q) => s.name.toLowerCase().includes(q.toLowerCase()) || s.category.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(s)}
        <td class="px-4 py-3 text-slate-700">{s.category}</td>
        <td class="px-4 py-3"><span class="font-mono text-slate-400">{s.id}</span></td>
        <td class="px-4 py-3"><span class="font-semibold text-slate-800">{s.name}</span></td>
        <td class="px-4 py-3"><span class="font-bold text-slate-900 font-mono">{s.defaultPrice} руб.</span></td>
      {/snippet}
      {#snippet actions(s)}
        <button onclick={() => openPriceForService(s)} class="inline-flex items-center gap-1 px-2.5 py-1 bg-teal-50 hover:bg-teal-100 text-teal-800 rounded font-bold text-[10px] transition cursor-pointer"><Edit class="w-3 h-3" /> Изменить цену</button>
      {/snippet}
    </DataTable>

    {#if showPriceModal}
      {@const history = priceHistoryFor(priceFormData.serviceId)}
      <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs">
        <form onsubmit={handleSavePrice} class="bg-white w-full max-w-md rounded-xl shadow-xl overflow-hidden border border-slate-200">
          <div class="p-4 bg-teal-700 text-white flex justify-between items-center">
            <span class="font-semibold text-xs text-white">Ввести новое ценообразование</span>
            <button type="button" onclick={() => (showPriceModal = false)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
          </div>
          <div class="p-5 space-y-4 bg-slate-50">
            <div class="space-y-3">
              <div>
                <span class="block text-slate-500 mb-1">Услуга:</span>
                <div class="w-full p-2 border border-slate-200 rounded bg-slate-100 text-slate-800 font-semibold" title={priceServiceName}>{priceServiceName}</div>
              </div>
              <div>
                <label class="block text-slate-500 mb-1">Новая устанавливаемая цена (руб) *</label>
                <input type="number" required min="1" bind:value={priceFormData.price} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
              </div>

              {#if history.length > 0}
                <div class="pt-1">
                  <span class="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">История последних цен</span>
                  <div class="border border-slate-100 rounded divide-y divide-slate-100 bg-white">
                    {#each history as h, i (h.id)}
                      <div class="flex items-center justify-between px-2.5 py-1.5 text-[11px]">
                        <span class="text-slate-500">с {h.activeFrom}{#if i === 0} <span class="text-emerald-700 font-semibold">(текущая)</span>{/if}</span>
                        <span class="font-mono font-semibold text-slate-800">{h.price} руб.</span>
                      </div>
                    {/each}
                  </div>
                </div>
              {/if}
            </div>
          </div>
          <div class="p-4 bg-slate-100 flex justify-end gap-2 shrink-0 border-t border-slate-100">
            <button type="button" onclick={() => (showPriceModal = false)} class="px-3 py-1.5 border border-slate-200 rounded text-slate-500 cursor-pointer">Отмена</button>
            <button type="submit" class="px-4 py-1.5 bg-teal-700 hover:bg-teal-800 font-bold text-white rounded cursor-pointer">Утвердить тариф</button>
          </div>
        </form>
      </div>
    {/if}
  </div>
{:else if app.activeView === 'clinic-diagnoses'}
  <div class="space-y-4">
    <div class="flex justify-between items-center bg-white p-3 border border-slate-200 rounded-lg shadow-sm">
      <div>
        <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Паспортный Справочник Болезней</h3>
        <p class="text-[10px] text-slate-400 font-normal">Синхронизированный каталог заболеваний животных по клиническим ветеринарным протоколам.</p>
      </div>
      <button onclick={openNewDiagnosis} class="px-3 py-1 bg-teal-700 text-white hover:bg-teal-800 transition rounded text-xs font-semibold cursor-pointer">+ Внести новый диагноз</button>
    </div>

    <DataTable columns={diagnosisColumns} data={diagnoses} searchPlaceholder="Быстрый поиск болезни по коду или названию..." searchFilter={(d, q) => d.name.toLowerCase().includes(q.toLowerCase()) || d.code.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(d)}
        <td class="px-4 py-3"><span class="font-bold font-mono text-xs text-teal-800 uppercase">{d.code}</span></td>
        <td class="px-4 py-3"><span class="font-semibold text-slate-800">{d.name}</span></td>
        <td class="px-4 py-3 text-slate-700">{d.category}</td>
      {/snippet}
      {#snippet actions(d)}
        <button onclick={() => openEditDiagnosis(d)} class="p-1 px-2 border border-slate-200 bg-white text-slate-600 rounded hover:bg-slate-50 transition cursor-pointer" title="Редактировать"><Edit class="w-3.5 h-3.5" /></button>
        <button onclick={() => handleDeleteDiagnosis(d.id)} class="p-1 px-2 border border-slate-200 bg-white text-red-600 rounded hover:bg-red-50 transition cursor-pointer" title="Удалить"><Trash2 class="w-3.5 h-3.5" /></button>
      {/snippet}
    </DataTable>

    {#if showDiagnosesModal}
      <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs">
        <form onsubmit={handleSaveDiagnosis} class="bg-white w-full max-w-md rounded-xl shadow-xl overflow-hidden border border-slate-200">
          <div class="p-4 bg-teal-700 text-white flex justify-between items-center">
            <span class="font-semibold text-xs text-white">{editingDiagnosis ? 'Редактировать диагноз' : 'Зарегистрировать диагноз'}</span>
            <button type="button" onclick={() => { showDiagnosesModal = false; editingDiagnosis = null; }} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
          </div>
          <div class="p-5 space-y-4 bg-slate-50">
            <div class="space-y-3">
              <div>
                <label class="block text-slate-500 mb-1">Код заболевания *</label>
                <input type="text" required placeholder="Например, K29.7" bind:value={dgFormData.code} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
              </div>
              <div>
                <label class="block text-slate-500 mb-1">Название болезни *</label>
                <input type="text" required placeholder="Острый гастрит неуточненный" bind:value={dgFormData.name} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
              </div>
              <div>
                <label class="block text-slate-500 mb-1 font-semibold">Подраздел ветеринарии *</label>
                <input type="text" required list="vet-departments" placeholder="Выберите или введите подраздел" bind:value={dgFormData.category} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                <datalist id="vet-departments">
                  {#each VET_DEPARTMENTS as dept (dept)}<option value={dept}></option>{/each}
                </datalist>
                <div class="mt-2">
                  <span class="block text-[10px] text-slate-400 mb-1.5 uppercase tracking-wider font-semibold">Быстрый выбор:</span>
                  <div class="flex flex-wrap gap-1">
                    {#each VET_DEPARTMENTS.slice(0, 10) as dept (dept)}
                      <button type="button" onclick={() => (dgFormData.category = dept)} class="px-2 py-1 rounded text-[10px] font-medium border transition cursor-pointer {dgFormData.category === dept ? 'bg-teal-50 border-teal-200 text-teal-800 font-semibold' : 'bg-white border-slate-200 hover:border-slate-300 text-slate-500 hover:text-slate-700'}">{dept}</button>
                    {/each}
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="p-4 bg-slate-100 flex justify-end gap-2 shrink-0 border-t border-slate-100">
            <button type="button" onclick={() => { showDiagnosesModal = false; editingDiagnosis = null; }} class="px-3 py-1.5 border border-slate-200 rounded text-slate-500 cursor-pointer">Отмена</button>
            <button type="submit" class="px-4 py-1.5 bg-teal-700 hover:bg-teal-800 font-bold text-white rounded cursor-pointer">Сохранить запись</button>
          </div>
        </form>
      </div>
    {/if}
  </div>
{/if}
