export function toDate(value?: string | null) {
  if (!value) {
    return null;
  }

  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, (month ?? 1) - 1, day ?? 1);
}

export function toDateOnlyString(value: Date | null | undefined) {
  if (!value) {
    return '';
  }

  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function formatDateOnly(value?: string | null) {
  if (!value) {
    return '';
  }

  return new Intl.DateTimeFormat('hr-HR').format(toDate(value) ?? new Date(value));
}

export function formatDateTime(value?: string | null) {
  if (!value) {
    return '';
  }

  return new Intl.DateTimeFormat('hr-HR', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(new Date(value));
}
