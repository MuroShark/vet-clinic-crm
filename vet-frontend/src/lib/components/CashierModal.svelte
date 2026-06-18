<script lang="ts">
  import { X, Check, DollarSign, CreditCard, Send, Printer, Receipt } from 'lucide-svelte';
  import { apiClient } from '$lib/api/client';
  import type { Appointment, Payment, Client, Patient, Employee, Service, Material, Branch } from '$lib/types';

  let { appointmentId, onClose, onSuccess }: {
    appointmentId: string;
    onClose: () => void;
    onSuccess: () => void;
  } = $props();

  let appointment = $state<Appointment | null>(null);
  let payments = $state<Payment[]>([]);
  let client = $state<Client | null>(null);
  let patient = $state<Patient | null>(null);
  let vet = $state<Employee | null>(null);
  let services = $state<Service[]>([]);
  let materials = $state<Material[]>([]);

  let payAmount = $state<number>(0);
  let payMethod = $state<'cash' | 'card' | 'transfer'>('card');
  let payNotes = $state<string>('');
  let errorMsg = $state<string | null>(null);
  let successPaid = $state<boolean>(false);
  let showPrintReceipt = $state<boolean>(false);
  let branches = $state<Branch[]>([]);

  async function loadData() {
    const appt = await apiClient.appointments.get(appointmentId);
    if (!appt) return;
    appointment = appt;
    payments = await apiClient.payments.forAppointment(appointmentId);
    client = (await apiClient.clients.get(appt.clientId)) || null;
    patient = (await apiClient.patients.get(appt.patientId)) || null;
    const list = await apiClient.employees.list();
    vet = list.find((e) => e.id === appt.vetId) || null;
    services = await apiClient.services.list();
    materials = await apiClient.materials.list();
    branches = await apiClient.branches.list();
    const totalPaid = payments.reduce((acc, p) => acc + p.amount, 0);
    payAmount = Math.max(0, appt.totalAmount - totalPaid);
  }

  $effect(() => {
    loadData();
  });

  const totalPaidSum = $derived(payments.reduce((acc, p) => acc + p.amount, 0));
  const balanceRemaining = $derived(appointment ? appointment.totalAmount - totalPaidSum : 0);

  async function handleProcessPayment(e: SubmitEvent) {
    e.preventDefault();
    errorMsg = null;
    if (payAmount <= 0) { errorMsg = 'Сумма платежа должна быть больше нуля.'; return; }
    if (payAmount > balanceRemaining) { errorMsg = `Сумма платежа (${payAmount} руб) превышает долг (${balanceRemaining} руб).`; return; }
    try {
      await apiClient.payments.add({ appointmentId, amount: payAmount, method: payMethod, notes: payNotes || 'Оплата приёма' });
      payments = await apiClient.payments.forAppointment(appointmentId);
      const updatedPaid = payments.reduce((acc, p) => acc + p.amount, 0);
      payAmount = Math.max(0, (appointment?.totalAmount || 0) - updatedPaid);
      successPaid = true;
      setTimeout(() => (successPaid = false), 2000);
    } catch (err: any) {
      errorMsg = err.message;
    }
  }

  const branchAddress = $derived(appointment ? branches.find((b) => b.id === appointment!.branchId)?.address : '');
</script>

{#if !appointment}
  <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 text-xs">
    <div class="bg-white p-6 rounded-lg text-slate-400">Загрузка данных кассы...</div>
  </div>
{:else}
  <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50">
    <div class="bg-white w-full max-w-2xl rounded-xl shadow-xl border border-slate-200 overflow-hidden flex flex-col max-h-[90vh]">
      <div class="p-4 bg-teal-700 text-white flex items-center justify-between shrink-0">
        <div class="flex items-center gap-2">
          <Receipt class="w-5 h-5" />
          <div>
            <h2 class="text-sm font-semibold text-white">Касса: Приём оплаты</h2>
            <p class="text-[10px] opacity-90">Регистрация ККТ • Приём {appointment.id}</p>
          </div>
        </div>
        <button onclick={onClose} class="p-1 hover:bg-white/10 rounded text-white cursor-pointer transition"><X class="w-4 h-4" /></button>
      </div>

      <div class="flex-1 overflow-y-auto p-5 grid grid-cols-1 md:grid-cols-2 gap-5 bg-slate-50">
        <!-- Column 1 -->
        <div class="space-y-4">
          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <span class="text-[10px] font-bold text-slate-400 uppercase block mb-1">Сводный баланс приёма</span>
            <div class="space-y-1 bg-slate-50 p-3 rounded-md mb-2">
              <div class="flex items-center justify-between text-xs"><span class="text-slate-500">Начислено по прайсу:</span><span class="font-semibold text-slate-800">{appointment.totalAmount} руб.</span></div>
              <div class="flex items-center justify-between text-xs text-emerald-700 font-medium"><span>Внесено платежей:</span><span>{totalPaidSum} руб.</span></div>
              <div class="flex items-center justify-between font-bold border-t border-slate-200 pt-1.5 mt-1 text-xs"><span class="text-slate-800">Остаток к оплате:</span><span class="{balanceRemaining > 0 ? 'text-red-600' : 'text-emerald-700'} font-mono">{balanceRemaining} руб.</span></div>
            </div>
            {#if balanceRemaining === 0}
              <div class="p-2 py-1 bg-emerald-50 border border-emerald-200 rounded text-[11px] text-emerald-800 font-medium flex items-center gap-1.5 justify-center"><Check class="w-3.5 h-3.5" /> Счёт полностью оплачен</div>
            {/if}
          </div>

          <div class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <span class="text-[10px] font-bold text-slate-400 uppercase block mb-2">История платежей по чеку</span>
            {#if payments.length > 0}
              <div class="space-y-2 max-h-40 overflow-y-auto">
                {#each payments as p (p.id)}
                  <div class="p-2 bg-slate-50 border border-slate-100 rounded text-[11px] flex items-center justify-between">
                    <div>
                      <span class="font-semibold text-slate-700 font-mono uppercase text-[9px] bg-slate-200 px-1.5 rounded mr-1">{p.method === 'cash' ? 'Нал' : p.method === 'card' ? 'Безнал' : 'Перевод'}</span>
                      <span class="text-slate-500">{p.paymentDate.split('T')[0]} {p.paymentDate.split('T')[1]?.substring(0, 5)}</span>
                    </div>
                    <span class="font-bold text-slate-800">+{p.amount} руб.</span>
                  </div>
                {/each}
              </div>
            {:else}
              <p class="text-slate-400 font-normal italic text-[11px]">Платежи по данному приёму ещё не поступали.</p>
            {/if}
          </div>

          <div class="p-3 bg-white border border-slate-200 rounded-lg">
            <button type="button" onclick={() => (showPrintReceipt = !showPrintReceipt)} class="w-full flex items-center justify-center gap-1.5 p-2 bg-slate-100 text-slate-700 rounded-md font-medium text-xs hover:bg-slate-200 transition cursor-pointer select-none"><Printer class="w-4 h-4" /> {showPrintReceipt ? 'Скрыть печатный вид' : 'Показать чек ККТ'}</button>
          </div>
        </div>

        <!-- Column 2 -->
        <div>
          {#if !showPrintReceipt}
            <form onsubmit={handleProcessPayment} class="p-4 bg-white border border-slate-200 rounded-lg shadow-sm h-full flex flex-col justify-between">
              <div>
                <h3 class="text-xs font-semibold text-slate-800 uppercase tracking-wide mb-3">Внести новый платёж</h3>
                {#if successPaid}<div class="mb-3 p-2 bg-emerald-50 border border-emerald-200 rounded text-[11px] text-emerald-800 font-medium">Платёж успешно сохранён в кассе!</div>{/if}
                {#if errorMsg}<div class="mb-3 p-2 bg-red-50 border border-red-200 rounded text-[11px] text-red-800">{errorMsg}</div>{/if}
                <div class="space-y-3 text-xs">
                  <div>
                    <label class="block text-slate-500 mb-1">Сумма к внесению (руб) *</label>
                    <input type="number" min="1" max={balanceRemaining} required disabled={balanceRemaining <= 0} bind:value={payAmount} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                  </div>
                  <div>
                    <label class="block text-slate-500 mb-1">Метод оплаты *</label>
                    <div class="grid grid-cols-3 gap-2">
                      <button type="button" onclick={() => (payMethod = 'cash')} class="p-2 rounded border text-center font-medium cursor-pointer transition {payMethod === 'cash' ? 'bg-teal-700 text-white border-teal-700' : 'bg-white text-slate-600 border-slate-200'}"><DollarSign class="w-3.5 h-3.5 inline mr-1" /> Наличные</button>
                      <button type="button" onclick={() => (payMethod = 'card')} class="p-2 rounded border text-center font-medium cursor-pointer transition {payMethod === 'card' ? 'bg-teal-700 text-white border-teal-700' : 'bg-white text-slate-600 border-slate-200'}"><CreditCard class="w-3.5 h-3.5 inline mr-1" /> Картой</button>
                      <button type="button" onclick={() => (payMethod = 'transfer')} class="p-2 rounded border text-center font-medium cursor-pointer transition {payMethod === 'transfer' ? 'bg-teal-700 text-white border-teal-700' : 'bg-white text-slate-600 border-slate-200'}"><Send class="w-3.5 h-3.5 inline mr-1" /> Перевод</button>
                    </div>
                  </div>
                  <div>
                    <label class="block text-slate-500 mb-1">Служебный комментарий кассира</label>
                    <input type="text" placeholder="Например, оплата по СБП..." disabled={balanceRemaining <= 0} bind:value={payNotes} class="w-full p-2 border border-slate-200 rounded focus:outline-none focus:border-teal-700 bg-white" />
                  </div>
                </div>
              </div>
              <button type="submit" disabled={balanceRemaining <= 0} class="w-full mt-6 py-2 bg-teal-700 text-white rounded font-semibold text-xs hover:bg-teal-800 disabled:opacity-50 cursor-pointer select-none transition">Внести платёж в кассу</button>
            </form>
          {:else}
            <div class="p-4 bg-white border border-slate-300 rounded-lg shadow-inner flex flex-col justify-between">
              <div id="thermal-receipt-print" class="bg-white p-3 border border-slate-200 shadow-sm font-mono text-[10px] text-slate-800 rounded leading-relaxed">
                <div class="text-center font-bold text-xs uppercase border-b border-dashed border-slate-300 pb-2 mb-2">
                  ВЦ АХИЛЛ - ЧЕК ККТ
                  <span class="text-[9px] block font-normal normal-case text-slate-500 mt-1">Клиника заботы о животных</span>
                  <span class="text-[9px] block font-normal normal-case text-slate-500">Адрес: {branchAddress}</span>
                </div>
                <div class="space-y-1 border-b border-dashed border-slate-300 pb-2 mb-2">
                  <div>Дата: {new Date().toLocaleString()}</div>
                  <div>Приём #: {appointment.id}</div>
                  {#if client}<div>Владелец: {client.name}</div>{/if}
                  {#if patient}<div>Пациент: {patient.name} ({patient.species})</div>{/if}
                  {#if vet}<div>Лечащий врач: {vet.name}</div>{/if}
                </div>
                <div class="space-y-2 border-b border-dashed border-slate-300 pb-2 mb-2">
                  <div class="font-bold border-b border-slate-100 pb-0.5">ОКАЗАННЫЕ УСЛУГИ:</div>
                  {#each appointment.services as s, i (i)}
                    <div class="flex justify-between items-start text-[9px]"><span class="max-w-[70%]">{services.find((item) => item.id === s.serviceId)?.name} (x{s.quantity})</span><span>{s.priceSnapshot * s.quantity} руб.</span></div>
                  {/each}
                  <div class="font-bold border-b border-slate-100 pb-0.5 mt-2">СПИСАННЫЕ МАТЕРИАЛЫ:</div>
                  {#each appointment.materials as m, i (i)}
                    <div class="flex justify-between items-start text-[9px]"><span class="max-w-[70%]">{materials.find((item) => item.id === m.materialId)?.name} (x{m.quantity})</span><span>{m.clientPriceSnapshot * m.quantity} руб.</span></div>
                  {/each}
                </div>
                <div class="space-y-1 mb-3">
                  <div class="flex justify-between font-bold text-xs"><span>ИТОГО К НАЧИСЛЕНИЮ:</span><span>{appointment.totalAmount} руб.</span></div>
                  <div class="flex justify-between text-emerald-700"><span>ОПЛАЧЕНО ВСЕГО:</span><span>{totalPaidSum} руб.</span></div>
                  {#if balanceRemaining > 0}<div class="flex justify-between text-red-600 font-bold"><span>ДОЛГ ОСТАТОК:</span><span>{balanceRemaining} руб.</span></div>{/if}
                </div>
                <div class="text-center text-[9px] text-slate-500 border-t border-dashed border-slate-300 pt-2 uppercase font-bold">Спасибо за доверие здоровья питомца!</div>
              </div>
              <button type="button" onclick={() => window.print()} class="w-full mt-4 flex items-center justify-center gap-1.5 py-2 bg-teal-700 text-white rounded font-semibold text-xs hover:bg-teal-800 transition cursor-pointer select-none"><Printer class="w-4 h-4" /> Распечатать на термопринтере</button>
            </div>
          {/if}
        </div>
      </div>

      <div class="p-4 bg-slate-100 border-t border-slate-200 flex justify-end shrink-0">
        <button type="button" onclick={() => { onSuccess(); onClose(); }} class="px-4 py-1.5 bg-slate-800 text-white rounded font-semibold text-xs hover:bg-slate-900 cursor-pointer select-none transition">Готово</button>
      </div>
    </div>
  </div>
{/if}
