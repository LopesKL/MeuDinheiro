import Api from './api';

/**
 * Desempacota ApiResponse do backend: { success, data, message, errors }
 */
/** Alinhado com ExpenseCreationSource no backend (.NET). */
export const EXPENSE_CREATION_SOURCE = {
  UNSPECIFIED: 0,
  QUICK_LAUNCH: 1,
  UPLOAD_RECEIPT: 2,
};

/**
 * Monta o body do POST /api/CreditCards a partir do formulário (tipo crédito vs vale alimentação).
 * @param {object} values - inclui cardKind: 'credit' | 'meal'
 */
export function normalizeCreditCardPayload(values) {
  const kind = values.cardKind === 'meal' || values.isMealVoucher === true ? 'meal' : 'credit';
  const isMeal = kind === 'meal';
  return {
    id: values.id,
    name: values.name,
    themeColor: values.themeColor,
    isMealVoucher: isMeal,
    closingDay: isMeal ? 1 : values.closingDay,
    dueDay: isMeal ? 1 : values.dueDay,
    mealVoucherDailyAmount: isMeal ? values.mealVoucherDailyAmount : null,
    mealVoucherCreditDay: isMeal ? values.mealVoucherCreditDay : null,
  };
}

export async function unwrap(request) {
  const { data } = await request;
  if (data && data.success === false) {
    const msg = (data.errors && data.errors.length ? data.errors.join(', ') : null) || data.message || 'Erro na API';
    const err = new Error(msg);
    err.api = data;
    throw err;
  }
  return data?.data;
}

export const financeApi = {
  categories: () => unwrap(Api.get('/api/Categories')),
  categoryCreate: (body) => unwrap(Api.post('/api/Categories', body)),
  categoryUpdate: (id, body) => unwrap(Api.put(`/api/Categories/${id}`, body)),
  categoryDelete: (id) => unwrap(Api.delete(`/api/Categories/${id}`)),

  incomes: () => unwrap(Api.get('/api/Incomes')),
  incomeHistory: (months = 24) => unwrap(Api.get('/api/Incomes/history', { params: { months } })),
  incomeUpsert: (body) => unwrap(Api.post('/api/Incomes', body)),
  incomeDelete: (id) => unwrap(Api.delete(`/api/Incomes/${id}`)),

  expenses: (year, month) =>
    unwrap(
      Api.get('/api/Expenses', {
        params: year != null && month != null ? { year, month } : {},
      })
    ),
  expenseParse: (text) => unwrap(Api.post('/api/Expenses/parse', { text })),
  expenseUpsert: (body) => unwrap(Api.post('/api/Expenses', body)),
  expenseDelete: (id) => unwrap(Api.delete(`/api/Expenses/${id}`)),
  uploadReceipt: (formData) =>
    unwrap(
      Api.post('/api/Expenses/upload-receipt', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
    ),

  creditCards: () => unwrap(Api.get('/api/CreditCards')),
  creditCardUpsert: (body) => unwrap(Api.post('/api/CreditCards', body)),
  creditCardDelete: (id) => unwrap(Api.delete(`/api/CreditCards/${id}`)),
  invoice: (cardId, year, month) =>
    unwrap(Api.get(`/api/CreditCards/${cardId}/invoice`, { params: { year, month } })),

  installmentPlans: () => unwrap(Api.get('/api/InstallmentPlans')),
  installmentPlanCreate: (body) => unwrap(Api.post('/api/InstallmentPlans', body)),
  installmentPlanDelete: (id) => unwrap(Api.delete(`/api/InstallmentPlans/${id}`)),
  payInstallment: (installmentId) =>
    unwrap(Api.post(`/api/InstallmentPlans/pay-installment/${installmentId}`)),

  installmentUpdate: (id, body) => unwrap(Api.put(`/api/Installments/${id}`, body)),
  installmentDelete: (id) => unwrap(Api.delete(`/api/Installments/${id}`)),

  debts: () => unwrap(Api.get('/api/Debts')),
  debtUpsert: (body) => unwrap(Api.post('/api/Debts', body)),
  debtDelete: (id) => unwrap(Api.delete(`/api/Debts/${id}`)),

  recurring: () => unwrap(Api.get('/api/RecurringExpenses')),
  recurringUpsert: (body) => unwrap(Api.post('/api/RecurringExpenses', body)),
  recurringDelete: (id) => unwrap(Api.delete(`/api/RecurringExpenses/${id}`)),
  recurringAmountSchedule: (recurringId, body) =>
    unwrap(Api.post(`/api/RecurringExpenses/${recurringId}/amount-schedule`, body)),
  recurringAmountScheduleDelete: (recurringId, scheduleId) =>
    unwrap(Api.delete(`/api/RecurringExpenses/${recurringId}/amount-schedule/${scheduleId}`)),

  accounts: () => unwrap(Api.get('/api/Accounts')),
  accountsTotal: () => unwrap(Api.get('/api/Accounts/total')),
  patrimonyHistory: (months = 12) =>
    unwrap(Api.get('/api/Accounts/patrimony-history', { params: { months } })),
  accountUpsert: (body) => unwrap(Api.post('/api/Accounts', body)),
  accountDelete: (id) => unwrap(Api.delete(`/api/Accounts/${id}`)),

  dashboard: (year, month) =>
    unwrap(
      Api.get('/api/Dashboard/summary', {
        params:
          year != null && month != null
            ? { year, month }
            : {},
      })
    ),
  projectionMe: (monthsAhead = 12) =>
    unwrap(Api.get('/api/Projections/me', { params: { monthsAhead } })),
  projectionSandbox: (body) =>
    Api.post('/api/Projections/sandbox', body).then((r) => {
      const { data } = r;
      if (data && data.success === false) {
        const msg = (data.errors && data.errors.join(', ')) || data.message;
        throw new Error(msg);
      }
      return data?.data;
    }),

  ocrAnalyze: (formData) =>
    unwrap(
      Api.post('/api/Ocr/analyze', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
    ),
};
