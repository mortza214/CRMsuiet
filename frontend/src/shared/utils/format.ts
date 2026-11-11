export function formatNumber(value: number | string | null | undefined): string {
  const numeric = Number(value ?? 0);
  if (Number.isNaN(numeric)) return "۰";
  return new Intl.NumberFormat("fa-IR").format(numeric);
}

export function formatCurrency(value: number | string | null | undefined): string {
  return `${formatNumber(value)} تومان`;
}


export function resolveApiFileUrl(path: string | null | undefined): string | undefined {
  if (!path) return undefined;
  if (/^https?:\/\//i.test(path)) return path;
  const apiBase =
    import.meta.env.VITE_API_BASE_URL ?? "http://127.0.0.1:5080/api";
  return `${apiBase.replace(/\/api\/?$/, "")}${path}`;
}
