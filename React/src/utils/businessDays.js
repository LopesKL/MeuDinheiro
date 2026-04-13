/**
 * Conta dias úteis (segunda a sexta) em um mês civil.
 * @param {number} year
 * @param {number} month 1–12
 */
export function countWeekdaysInMonth(year, month) {
  const daysInMonth = new Date(year, month, 0).getDate();
  let n = 0;
  for (let d = 1; d <= daysInMonth; d += 1) {
    const wd = new Date(year, month - 1, d).getDay();
    if (wd !== 0 && wd !== 6) n += 1;
  }
  return n;
}
