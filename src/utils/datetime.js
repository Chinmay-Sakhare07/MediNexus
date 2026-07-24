// D6 frontend edge: the API speaks UTC ISO-8601 ('Z'); the UI renders and
// collects times in hospital time (IST), independent of the viewer's browser
// timezone. See ARCHITECTURE.md §5.

const IST_OFFSET_MINUTES = 330; // UTC+5:30, no DST in India

const dtf = new Intl.DateTimeFormat('en-IN', {
  timeZone: 'Asia/Kolkata',
  dateStyle: 'medium',
  timeStyle: 'short',
});

const df = new Intl.DateTimeFormat('en-IN', {
  timeZone: 'Asia/Kolkata',
  dateStyle: 'medium',
});

export const formatIstDateTime = (value) =>
  value ? dtf.format(new Date(value)) : '';

export const formatIstDate = (value) =>
  value ? df.format(new Date(value)) : '';

// datetime-local gives "YYYY-MM-DDTHH:mm" meaning an IST wall-clock pick.
// Convert to a UTC ISO instant for the API.
export const istWallToUtcIso = (localValue) => {
  if (!localValue) return localValue;
  const [datePart, timePart] = localValue.split('T');
  const [y, m, d] = datePart.split('-').map(Number);
  const [hh, mm] = timePart.split(':').map(Number);
  const utcMs = Date.UTC(y, m - 1, d, hh, mm) - IST_OFFSET_MINUTES * 60000;
  return new Date(utcMs).toISOString();
};
