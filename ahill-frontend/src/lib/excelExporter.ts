import * as XLSX from 'xlsx';
import type { Appointment, Employee, Batch, Payment, Client, Service, Material, Branch } from '$lib/types';

export const excelExporter = {
  // 1. Зарплатная ведомость по KPI
  exportPayroll: (
    employees: Employee[],
    appointments: Appointment[],
    positions: { id: string; name: string }[],
    startDate: string,
    endDate: string,
    services?: Service[],
    materials?: Material[]
  ) => {
    const vets = employees.filter((e) => e.status === 'active');

    const closedAppointsInPeriod = appointments.filter((a) => {
      if (a.status !== 'Closed' || !a.closedAt) return false;
      const date = a.closedAt.split('T')[0];
      return date >= startDate && date <= endDate;
    });

    const summaryRows = vets.map((vet) => {
      const vetPos = positions.find((p) => p.id === vet.positionId)?.name || 'Врач';
      const vetAppts = closedAppointsInPeriod.filter((a) => a.vetId === vet.id);

      let totalServicesRevenue = 0;
      vetAppts.forEach((a) => {
        a.services.forEach((s) => {
          totalServicesRevenue += s.priceSnapshot * s.quantity;
        });
      });

      let totalMaterialsCost = 0;
      vetAppts.forEach((a) => {
        a.materials.forEach((m) => {
          totalMaterialsCost += m.unitCostSnapshot * m.quantity;
        });
      });

      const kpiRate = vet.KPIRate;
      const servicesKpiBase = totalServicesRevenue;
      const kpiEarnings = servicesKpiBase * kpiRate;

      return {
        name: vet.name,
        position: vetPos,
        apptsCount: vetAppts.length,
        servicesRevenue: totalServicesRevenue,
        materialsCost: totalMaterialsCost,
        kpiBase: servicesKpiBase,
        kpiRate,
        kpiEarnings,
        totalPayout: kpiEarnings
      };
    });

    const detailedRows: any[] = [];
    closedAppointsInPeriod.forEach((appt) => {
      const vet = vets.find((e) => e.id === appt.vetId);
      if (!vet) return;

      appt.services.forEach((s) => {
        const serviceName = services?.find((srv) => srv.id === s.serviceId)?.name || 'Услуга #' + s.serviceId;
        detailedRows.push({
          date: appt.closedAt?.split('T')[0] || appt.appointmentDate,
          apptId: appt.id,
          vetName: vet.name,
          itemName: serviceName,
          type: 'Услуга',
          quantity: s.quantity,
          price: s.priceSnapshot,
          total: s.priceSnapshot * s.quantity,
          kpiAmount: s.priceSnapshot * s.quantity * vet.KPIRate
        });
      });

      appt.materials.forEach((m) => {
        const materialName = materials?.find((mat) => mat.id === m.materialId)?.name || 'Материал #' + m.materialId;
        detailedRows.push({
          date: appt.closedAt?.split('T')[0] || appt.appointmentDate,
          apptId: appt.id,
          vetName: vet.name,
          itemName: materialName,
          type: 'Расходник',
          quantity: m.quantity,
          price: m.clientPriceSnapshot,
          total: m.clientPriceSnapshot * m.quantity,
          kpiAmount: 0
        });
      });
    });

    const summaryData: any[][] = [];
    summaryData.push(['Зарплатная ведомость по KPI']);
    summaryData.push([`Период: с ${startDate} по ${endDate}`]);
    summaryData.push([]);
    summaryData.push([
      'Сотрудник', 'Должность', 'Приёмы (шт)', 'Выручка по услугам (руб)',
      'Себестоимость мат. (руб)', 'KPI база (руб)', 'KPI %', 'KPI начислено (руб)', 'Итог к выплате (руб)'
    ]);

    summaryRows.forEach((row) => {
      summaryData.push([
        row.name, row.position, row.apptsCount, row.servicesRevenue,
        row.materialsCost, row.kpiBase, row.kpiRate * 100, row.kpiEarnings, row.totalPayout
      ]);
    });

    summaryData.push([
      'Итого по клинике', '',
      summaryRows.reduce((a, r) => a + r.apptsCount, 0),
      summaryRows.reduce((a, r) => a + r.servicesRevenue, 0),
      summaryRows.reduce((a, r) => a + r.materialsCost, 0),
      summaryRows.reduce((a, r) => a + r.kpiBase, 0),
      '',
      summaryRows.reduce((a, r) => a + r.kpiEarnings, 0),
      summaryRows.reduce((a, r) => a + r.totalPayout, 0)
    ]);

    const wsSummary = XLSX.utils.aoa_to_sheet(summaryData);
    wsSummary['!cols'] = [
      { wch: 26 }, { wch: 18 }, { wch: 12 }, { wch: 24 }, { wch: 24 }, { wch: 16 }, { wch: 10 }, { wch: 18 }, { wch: 20 }
    ];

    const detailsData: any[][] = [];
    detailsData.push(['Построчная расшифровка оказания услуг и списания материалов']);
    detailsData.push([]);
    detailsData.push(['Дата', 'ID Приёма', 'Врач', 'Тип', 'Наименование', 'Кол-во', 'Цена (руб)', 'Сумма (руб)', 'Начислено KPI (руб)']);

    detailedRows.forEach((row) => {
      detailsData.push([row.date, row.apptId, row.vetName, row.type, row.itemName, row.quantity, row.price, row.total, row.kpiAmount]);
    });

    const wsDetails = XLSX.utils.aoa_to_sheet(detailsData);
    wsDetails['!cols'] = [
      { wch: 12 }, { wch: 14 }, { wch: 26 }, { wch: 12 }, { wch: 25 }, { wch: 10 }, { wch: 12 }, { wch: 12 }, { wch: 18 }
    ];

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, wsSummary, 'Сводка');
    XLSX.utils.book_append_sheet(wb, wsDetails, 'Расшифровка');
    XLSX.writeFile(wb, `payroll_${startDate}_${endDate}.xlsx`);
  },

  // 2. Финансовый отчёт за период
  exportFinancial: (
    appointments: Appointment[],
    branches: Branch[],
    _services: Service[],
    _materials: Material[],
    startDate: string,
    endDate: string
  ) => {
    const closed = appointments.filter((a) => {
      if (a.status !== 'Closed' || !a.closedAt) return false;
      const date = a.closedAt.split('T')[0];
      return date >= startDate && date <= endDate;
    });

    const branchSummary = branches.map((br) => {
      const appts = closed.filter((a) => a.branchId === br.id);
      let revenue = 0;
      let costMaterials = 0;
      appts.forEach((a) => {
        a.services.forEach((s) => {
          revenue += s.priceSnapshot * s.quantity;
        });
        a.materials.forEach((m) => {
          revenue += m.clientPriceSnapshot * m.quantity;
          costMaterials += m.unitCostSnapshot * m.quantity;
        });
      });
      const grossProfit = revenue - costMaterials;
      const marginPercent = revenue > 0 ? (grossProfit / revenue) * 100 : 0;
      return { name: br.name, apptsQty: appts.length, revenue, costMaterials, grossProfit, marginPercent };
    });

    const reportData: any[][] = [];
    reportData.push(['Финансовый отчёт по филиалам']);
    reportData.push([`Период анализа: с ${startDate} по ${endDate}`]);
    reportData.push([]);
    reportData.push(['Филиал', 'Кол-во приёмов', 'Выручка (руб)', 'Затраты на материалы (руб)', 'Валовая прибыль (руб)', 'Маржинальность (%)']);

    branchSummary.forEach((row) => {
      reportData.push([row.name, row.apptsQty, row.revenue, row.costMaterials, row.grossProfit, parseFloat(row.marginPercent.toFixed(1))]);
    });

    const totRev = branchSummary.reduce((a, r) => a + r.revenue, 0);
    const totCost = branchSummary.reduce((a, r) => a + r.costMaterials, 0);
    const totProfit = totRev - totCost;
    reportData.push([
      'Итого по компании',
      branchSummary.reduce((a, r) => a + r.apptsQty, 0),
      totRev, totCost, totProfit,
      parseFloat((totRev > 0 ? (totProfit / totRev) * 100 : 0).toFixed(1))
    ]);

    const ws = XLSX.utils.aoa_to_sheet(reportData);
    ws['!cols'] = [{ wch: 25 }, { wch: 15 }, { wch: 18 }, { wch: 24 }, { wch: 22 }, { wch: 18 }];
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Финансовая сводка');
    XLSX.writeFile(wb, `financial_report_${startDate}_${endDate}.xlsx`);
  },

  // 3. Складской отчёт
  exportWarehouse: (batches: Batch[], materials: Material[], branches: Branch[]) => {
    const today = new Date();
    const formattedRows = batches.map((b) => {
      const mat = materials.find((m) => m.id === b.materialId);
      const br = branches.find((branch) => branch.id === b.branchId);
      const diffDays = Math.ceil((new Date(b.expiryDate).getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
      return {
        sku: mat?.sku || 'N/A', name: mat?.name || 'Unknown', category: mat?.category || 'Common',
        supplier: b.supplier, branch: br?.name || 'All', remQty: b.remainingQuantity,
        totalQty: b.totalQuantity, cost: b.purchasePrice, price: b.clientPrice,
        expiryDate: b.expiryDate, daysLeft: diffDays
      };
    });

    const warehouseData: any[][] = [];
    warehouseData.push(['Складской остаток партий и сроки годности']);
    warehouseData.push([`Выгружено: ${new Date().toISOString().split('T')[0]}`]);
    warehouseData.push([]);
    warehouseData.push(['Артикул', 'Материал', 'Категория', 'Поставщик', 'Филиал', 'Остаток', 'Закупка (всего)', 'Себестоимость (руб)', 'Цена продажи (руб)', 'Срок годности', 'Осталось дней']);

    formattedRows.forEach((row) => {
      warehouseData.push([row.sku, row.name, row.category, row.supplier, row.branch, row.remQty, row.totalQty, row.cost, row.price, row.expiryDate, row.daysLeft]);
    });

    const ws = XLSX.utils.aoa_to_sheet(warehouseData);
    ws['!cols'] = [{ wch: 14 }, { wch: 25 }, { wch: 15 }, { wch: 20 }, { wch: 18 }, { wch: 10 }, { wch: 14 }, { wch: 18 }, { wch: 18 }, { wch: 15 }, { wch: 15 }];
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Складской учёт');
    XLSX.writeFile(wb, 'warehouse_expiry_report.xlsx');
  },

  // 4. Реестр оплат за период
  exportPayments: (payments: Payment[], appointments: Appointment[], clients: Client[], startDate: string, endDate: string) => {
    const list = payments.filter((p) => {
      const date = p.paymentDate.split('T')[0];
      return date >= startDate && date <= endDate;
    });

    const rows = list.map((p) => {
      const appt = appointments.find((a) => a.id === p.appointmentId);
      const client = appt ? clients.find((c) => c.id === appt.clientId) : null;
      let methodText = 'Наличные';
      if (p.method === 'card') methodText = 'Карта';
      if (p.method === 'transfer') methodText = 'Перевод';
      return {
        id: p.id, date: p.paymentDate.split('T')[0], time: p.paymentDate.split('T')[1]?.substring(0, 5) || '',
        apptId: p.appointmentId, clientName: client?.name || 'N/A', clientPhone: client?.phone || '',
        amount: p.amount, method: methodText
      };
    });

    const paymentsData: any[][] = [];
    paymentsData.push(['Реестр полученных оплат (кассовая ведомость)']);
    paymentsData.push([`Выгрузка за период с ${startDate} по ${endDate}`]);
    paymentsData.push([]);
    paymentsData.push(['ID Платежа', 'Дата', 'Время', 'ID Приёма', 'ФИО Клиента', 'Телефон', 'Сумма (руб)', 'Способ оплаты']);

    rows.forEach((row) => {
      paymentsData.push([row.id, row.date, row.time, row.apptId, row.clientName, row.clientPhone, row.amount, row.method]);
    });

    paymentsData.push(['ИТОГО выручка', '', '', '', '', '', rows.reduce((a, r) => a + r.amount, 0), '']);

    const ws = XLSX.utils.aoa_to_sheet(paymentsData);
    ws['!cols'] = [{ wch: 14 }, { wch: 12 }, { wch: 10 }, { wch: 12 }, { wch: 25 }, { wch: 15 }, { wch: 15 }, { wch: 15 }];
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Реестр оплат');
    XLSX.writeFile(wb, `payments_registry_${startDate}_${endDate}.xlsx`);
  }
};
