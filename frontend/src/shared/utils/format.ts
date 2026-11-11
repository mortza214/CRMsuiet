export function formatNumber(value: number | string | null | undefined): string {
  const numeric = Number(value ?? 0);
  if (Number.isNaN(numeric)) return "۰";
  return new Intl.NumberFormat("fa-IR").format(numeric);
}

export function formatCurrency(value: number | string | null | undefined): string {
  return `${formatNumber(value)} تومان`;
}
