<script lang="ts">
  import { ClipboardCheck, Database, X } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import type { Appointment, Patient, Client, Diagnosis } from '$lib/types';
  import DataTable from './DataTable.svelte';
  import StatusBadge from './StatusBadge.svelte';

  let appointments = $state<Appointment[]>([]);
  let patients = $state<Patient[]>([]);
  let clients = $state<Client[]>([]);
  let diagnoses = $state<Diagnosis[]>([]);
  let detailedPatient = $state<Patient | null>(null);

  const vetEmployeeId = $derived(app.currentUser?.employeeId);

  async function loadData() {
    const branchId = app.activeBranchId;
    const list = await apiClient.appointments.list();
    appointments = vetEmployeeId ? list.filter((a) => a.vetId === vetEmployeeId && a.branchId === branchId) : [];
    patients = await apiClient.patients.list();
    clients = await apiClient.clients.list();
    diagnoses = await apiClient.diagnoses.list();
  }

  $effect(() => {
    app.activeView;
    app.activeBranchId;
    app.reloadToken;
    vetEmployeeId;
    loadData();
  });

  function launchScheduler(id: string) {
    app.schedulerApptId = id;
  }

  const today = new Date().toISOString().split('T')[0];
  const todaysVisits = $derived(appointments.filter((a) => a.appointmentDate === today).sort((a, b) => a.timeSlot.localeCompare(b.timeSlot)));

  const patientColumns = [
    { header: 'Питомец', sortKey: 'name' },
    { header: 'Вид / Семейство', sortKey: 'species' },
    { header: 'Порода', sortKey: 'breed' },
    { header: 'Окрас' },
    { header: 'Вес (кг)', sortKey: 'weight' },
    { header: 'Владелец' }
  ];

  const diagnosisColumns = [
    { header: 'Код диагноза', sortKey: 'code' },
    { header: 'Наименование диагноза', sortKey: 'name' },
    { header: 'Раздел ветеринарии', sortKey: 'category' }
  ];
</script>

{#if app.activeView === 'vet-schedule'}
  <div class="space-y-6">
    {#if !vetEmployeeId}
      <div class="p-5 bg-amber-50 border border-amber-200 rounded-lg shadow-sm text-xs space-y-2">
        <h4 class="font-bold text-amber-900 text-sm flex items-center gap-1.5 uppercase tracking-wide"><span>●</span> Профиль не привязан к сотруднику</h4>
        <p class="text-amber-800 leading-relaxed font-normal">Пользователь <span class="font-semibold underline text-amber-950">{app.currentUser?.username}</span> не привязан ни к одному из сотрудников-ветеринаров в штатном расписании клиники. В связи с этим просмотр персонального ежедневного кабинета врача невозможен.</p>
        <p class="text-amber-700/80 leading-normal text-[11px] font-normal">Чтобы продемонстрировать сквозной процесс лечения пациента, пожалуйста, выйдите из системы и авторизуйтесь под тестовым профилем врача, например: <span class="font-semibold text-teal-800">kozlovps</span> (пароль 123).</p>
      </div>
    {:else}
      <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
        <div class="flex items-center gap-2 mb-2">
          <ClipboardCheck class="w-5 h-5 text-teal-700" />
          <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide">Моё расписание приёма на сегодня ({today})</h3>
        </div>
        <p class="text-[11px] text-slate-400 font-normal">Электронная медицинская карта (ЭМК) открывается по кнопке «Открыть протокол ЭМК». Заполните данные осмотра, используемые препараты, внесите диагноз и подпишите приём.</p>
      </div>

      <div class="bg-white border border-slate-200 rounded-lg shadow-sm overflow-hidden">
        <div class="p-4 border-b border-slate-100 font-bold text-slate-700 bg-slate-50/50 text-xs">Записи приёма на лечении</div>
        <div class="divide-y divide-slate-100">
          {#if todaysVisits.length > 0}
            {#each todaysVisits as a (a.id)}
              {@const p = patients.find((item) => item.id === a.patientId)}
              {@const c = clients.find((item) => item.id === a.clientId)}
              <div class="p-4 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 text-xs hover:bg-slate-50/50 transition">
                <div class="flex gap-4 items-center min-w-0">
                  <span class="font-mono bg-teal-50 text-teal-800 p-2 text-center rounded font-bold min-w-[65px]">{a.timeSlot}</span>
                  <div class="min-w-0">
                    <span class="font-bold text-slate-800 flex items-center gap-2">{p?.name} <span class="font-normal text-slate-400 text-[10px]">({p?.species} • {p?.breed || 'метис'})</span></span>
                    <span class="text-slate-500 block text-[10px] font-normal truncate mt-0.5">Владелец: {c?.name} • Вес: {p?.weight} кг</span>
                  </div>
                </div>
                <div class="flex items-center gap-3 self-end md:self-auto">
                  <StatusBadge status={a.status} variant="vet" />
                  {#if a.status !== 'Closed' && a.status !== 'Cancelled'}
                    <button onclick={() => launchScheduler(a.id)} class="px-3.5 py-1.5 bg-teal-700 text-white rounded font-bold text-[10px] hover:bg-teal-800 transition cursor-pointer">Открыть протокол ЭМК ➜</button>
                  {:else}
                    <span class="text-slate-400 font-mono text-[10px]">Карта заблокирована</span>
                  {/if}
                </div>
              </div>
            {/each}
          {:else}
            <p class="p-8 text-center text-slate-400 italic font-normal">У вас нет запланированных визитов к лечению на сегодня.</p>
          {/if}
        </div>
      </div>
    {/if}
  </div>
{:else if app.activeView === 'vet-patients'}
  <div class="space-y-4">
    <DataTable columns={patientColumns} data={patients} searchPlaceholder="Введите кличку или вид питомца..." searchFilter={(p, q) => p.name.toLowerCase().includes(q.toLowerCase()) || p.species.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(p)}
        <td class="px-4 py-3"><span class="font-bold text-teal-800">{p.name}</span></td>
        <td class="px-4 py-3 text-slate-700">{p.species}</td>
        <td class="px-4 py-3 text-slate-700">{p.breed || 'Метис'}</td>
        <td class="px-4 py-3 text-slate-700">{p.color}</td>
        <td class="px-4 py-3"><span class="font-mono">{p.weight}</span></td>
        <td class="px-4 py-3 text-slate-700">{clients.find((c) => c.id === p.clientId)?.name || 'N/A'}</td>
      {/snippet}
      {#snippet actions(p)}
        <button onclick={() => (detailedPatient = p)} class="px-3 py-1 bg-teal-50 text-teal-800 border border-teal-100 hover:bg-teal-100 rounded text-[10px] cursor-pointer">Медицинская карта ЭМК ➜</button>
      {/snippet}
    </DataTable>

    {#if detailedPatient}
      {@const history = appointments.filter((a) => a.patientId === detailedPatient!.id && a.status === 'Closed')}
      <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs">
        <div class="bg-white w-full max-w-2xl rounded-xl shadow-xl overflow-hidden border border-slate-200">
          <div class="p-4 bg-teal-700 text-white flex justify-between items-center">
            <span class="font-semibold text-xs text-white">Ветеринарная медкарта : {detailedPatient.name}</span>
            <button onclick={() => (detailedPatient = null)} class="text-white hover:bg-white/10 p-1 rounded cursor-pointer"><X class="w-4 h-4" /></button>
          </div>
          <div class="p-6 space-y-4 bg-slate-50 max-h-[85vh] overflow-y-auto">
            <div class="bg-white p-4 rounded-lg border border-slate-200 space-y-1 shadow-sm">
              <h3 class="font-bold text-slate-800 text-sm">{detailedPatient.name}</h3>
              <div class="grid grid-cols-2 gap-4 text-xs pt-2">
                <div><span class="text-slate-400">Вид животного:</span> <span class="text-slate-800 font-semibold">{detailedPatient.species}</span></div>
                <div><span class="text-slate-400">Порода:</span> <span class="text-slate-800 font-semibold">{detailedPatient.breed || 'метис'}</span></div>
                <div><span class="text-slate-400">Пол:</span> <span class="text-slate-800 font-semibold">{detailedPatient.gender === 'M' ? 'Самец' : 'Самка'}</span></div>
                <div><span class="text-slate-400">Дата рождения:</span> <span class="text-slate-800 font-semibold">{detailedPatient.birthDate}</span></div>
                <div><span class="text-slate-400">Окрас:</span> <span class="text-slate-800 font-semibold">{detailedPatient.color}</span></div>
                <div><span class="text-slate-400">Текущий параметр веса:</span> <span class="text-slate-800 font-semibold">{detailedPatient.weight} кг</span></div>
              </div>
            </div>
            <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-sm">
              <span class="text-[10px] font-bold text-slate-400 uppercase tracking-widest block mb-1">Анамнезы и Листы назначений по приёмам</span>
              <div class="space-y-3 mt-3">
                {#each history as a (a.id)}
                  <div class="p-3 bg-slate-50 border border-slate-100 rounded-lg text-xs space-y-1.5">
                    <div class="flex justify-between items-center font-semibold border-b border-slate-200 pb-1.5 mb-1.5">
                      <span>Протокол от {a.appointmentDate}</span>
                      <span class="text-[9px] uppercase font-mono tracking-wider bg-slate-200 p-0.5 px-2 rounded">{a.id}</span>
                    </div>
                    <p class="text-slate-600 font-normal"><span class="font-semibold block text-[10px] text-slate-400 uppercase mb-0.5">Клинические отметки (Общие симптомы):</span>{a.notes || 'Общее состояние стабильное'}</p>
                    {#if a.diagnoses.length > 0}
                      <div class="pt-2 border-t border-slate-200 border-dashed space-y-1">
                        <span class="text-[9px] font-bold text-slate-400 uppercase block">Установленные диагнозы:</span>
                        {#each a.diagnoses as d, i (i)}
                          {@const dgObj = diagnoses.find((dg) => dg.id === d.diagnosisId)}
                          <div class="text-[11px] text-slate-800 flex items-center justify-between">
                            <span>• <span class="font-bold underline">{dgObj?.code}</span> {dgObj?.name}</span>
                            <span class="text-[9px] px-1.5 py-0.5 rounded {d.isPreliminary ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'}">{d.isPreliminary ? 'Предварительный' : 'Окончательный'}</span>
                          </div>
                        {/each}
                      </div>
                    {/if}
                  </div>
                {/each}
                {#if history.length === 0}<p class="text-slate-400 font-normal italic text-[11px] py-1">Анамнеза по пациенту не обнаружено.</p>{/if}
              </div>
            </div>
          </div>
        </div>
      </div>
    {/if}
  </div>
{:else if app.activeView === 'vet-diagnoses'}
  <div class="space-y-4">
    <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
      <div class="flex items-center gap-1.5 mb-1">
        <Database class="w-4 h-4 text-teal-700" />
        <h4 class="text-xs font-semibold text-slate-700 uppercase tracking-wide">Поиск по справочнику диагнозов</h4>
      </div>
      <p class="text-[11px] text-slate-400 font-normal">Ветеринарный справочник диагнозов для быстрого ввода в ЭМК врача</p>
    </div>

    <DataTable columns={diagnosisColumns} data={diagnoses} searchPlaceholder="Введите код или наименование болезни для поиска..." searchFilter={(d, q) => d.name.toLowerCase().includes(q.toLowerCase()) || d.code.toLowerCase().includes(q.toLowerCase())}>
      {#snippet row(d)}
        <td class="px-4 py-3"><span class="font-bold font-mono text-xs text-teal-800 uppercase">{d.code}</span></td>
        <td class="px-4 py-3"><span class="font-medium text-slate-800">{d.name}</span></td>
        <td class="px-4 py-3 text-slate-700">{d.category}</td>
      {/snippet}
    </DataTable>
  </div>
{/if}
