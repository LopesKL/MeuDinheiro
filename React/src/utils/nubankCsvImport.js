import dayjs from 'dayjs';

/** Ex.: "Tv 65  - Parcela 9/12" → grupo 9/12 no final. */
export const PARCELA_REGEX = /\s*-\s*Parcela\s+(\d+)\/(\d+)\s*$/i;

export function parseCsvLineToFields(line) {
  const parts = line.split(',');
  if (parts.length < 3) return null;
  const dateStr = parts[0].trim();
  const amountStr = parts[parts.length - 1].trim();
  const title = parts.slice(1, -1).join(',').trim();
  return { dateStr, title, amountStr };
}

export function parseAmount(amountStr) {
  const n = parseFloat(String(amountStr).replace(/\s/g, '').replace(',', '.'));
  return Number.isFinite(n) ? n : NaN;
}

export function analyzeTitle(title) {
  const raw = String(title).trim();
  const m = raw.match(PARCELA_REGEX);
  if (!m) {
    return { cleanTitle: raw, installmentCurrent: null, installmentTotal: null };
  }
  const installmentCurrent = parseInt(m[1], 10);
  const installmentTotal = parseInt(m[2], 10);
  const cleanTitle = raw.replace(PARCELA_REGEX, '').trim();
  return { cleanTitle, installmentCurrent, installmentTotal };
}

function isValidParcel(current, total) {
  return (
    Number.isInteger(total) &&
    total > 0 &&
    Number.isInteger(current) &&
    current >= 1 &&
    current <= total
  );
}

/** Comparação estável para deduplicação (nome original do CSV). */
export function normalizeImportDedupText(s) {
  return String(s || '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, ' ');
}

/** Identifica linha repetida no arquivo (data + valor + nome original após limpar parcela). */
export function csvRowImportDedupKey(row) {
  const d = row.rowDate?.isValid?.() ? row.rowDate.format('YYYY-MM-DD') : '';
  const cents = Math.round(Number(row.amount || 0) * 100);
  const name = normalizeImportDedupText(row.originalName ?? row.displayName ?? '');
  return `${d}|${cents}|${name}`;
}

/**
 * Monta um registro editável para o modal de importação.
 */
export function buildCsvImportRow({ dateStr, title, amountStr }) {
  const amount = parseAmount(amountStr);
  const rowDate = dayjs(dateStr);
  const { cleanTitle, installmentCurrent, installmentTotal } = analyzeTitle(title);
  const hasParcel = isValidParcel(installmentCurrent, installmentTotal);
  let planStartDate = null;
  let totalPlanAmount = null;
  if (hasParcel && rowDate.isValid()) {
    planStartDate = rowDate.subtract(installmentCurrent - 1, 'month').startOf('day');
    totalPlanAmount = Math.round(amount * installmentTotal * 100) / 100;
  }
  return {
    id: crypto.randomUUID(),
    selected: true,
    kind: hasParcel ? 'plan' : 'recurring',
    /** Título completo como veio no CSV (auditoria). */
    rawBankTitle: String(title).trim(),
    /** Nome após remover "Parcela X/Y"; não muda quando o usuário edita o nome exibido. */
    originalName: cleanTitle,
    displayName: cleanTitle,
    rowDate: rowDate.isValid() ? rowDate : dayjs(),
    amount: Number.isFinite(amount) ? amount : 0,
    installmentCurrent: hasParcel ? installmentCurrent : null,
    installmentTotal: hasParcel ? installmentTotal : null,
    planStartDate,
    totalPlanAmount: hasParcel ? totalPlanAmount : null,
    categoryId: null,
    creditCardId: null,
    paymentMethod: 2,
    dayOfMonth: rowDate.isValid() ? rowDate.date() : dayjs().date(),
    recurringType: 0,
    active: true,
  };
}

/**
 * CSV exportado no formato Nubank: date,title,amount
 */
export function parseNubankCsvExport(text) {
  const normalized = text.replace(/^\uFEFF/, '').trim();
  const lines = normalized.split(/\r?\n/).filter((l) => l.trim());
  if (lines.length < 2) return [];
  const h = lines[0].toLowerCase();
  if (!h.includes('date') || !h.includes('title') || !h.includes('amount')) {
    throw new Error('Cabeçalho esperado: date, title, amount (exportação Nubank).');
  }
  const rows = [];
  for (let i = 1; i < lines.length; i++) {
    const fields = parseCsvLineToFields(lines[i]);
    if (!fields) continue;
    rows.push(buildCsvImportRow(fields));
  }
  return rows;
}
